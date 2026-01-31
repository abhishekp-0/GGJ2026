using UnityEngine;

public sealed class PlayerAnimationDriver : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerMovement movement;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private Animator animator;

    private static readonly int AnimJump = Animator.StringToHash("Jump");
    private static readonly int AnimLand = Animator.StringToHash("Land");

    private bool warnedMissingJump;
    private bool warnedMissingLand;

    private void Awake()
    {
        if (movement == null)
            movement = GetComponent<PlayerMovement>();
    }

    private void OnEnable()
    {
        if (movement == null) return;

        movement.OnJump += HandleJump;
        movement.OnLand += HandleLand;
        movement.OnAnimatorChanged += HandleAnimatorChanged;
    }

    private void OnDisable()
    {
        if (movement == null) return;

        movement.OnJump -= HandleJump;
        movement.OnLand -= HandleLand;
        movement.OnAnimatorChanged -= HandleAnimatorChanged;
    }

    private void HandleAnimatorChanged(Animator a)
    {
        animator = a;
        warnedMissingJump = false;
        warnedMissingLand = false;

        if (debugLogs)
            Debug.Log($"[AnimDriver] Animator changed to: {(animator ? animator.gameObject.name : "NULL")}");
    }

    private void HandleJump()
    {
        SafeSetTrigger(AnimJump, "Jump", ref warnedMissingJump);
    }

    private void HandleLand()
    {
        SafeSetTrigger(AnimLand, "Land", ref warnedMissingLand);
    }

    private void SafeSetTrigger(int hash, string name, ref bool warnedFlag)
    {
        if (animator == null) return;

        for (int i = 0; i < animator.parameterCount; i++)
        {
            var p = animator.GetParameter(i);
            if (p.type == AnimatorControllerParameterType.Trigger && (p.nameHash == hash || p.name == name))
            {
                animator.ResetTrigger(hash);
                animator.SetTrigger(hash);
                return;
            }
        }

        if (!warnedFlag)
        {
            warnedFlag = true;
            Debug.LogWarning($"[AnimDriver] Animator '{animator.gameObject.name}' missing Trigger '{name}'");
        }
    }
}
