using UnityEngine;

public sealed class PlayerAnimationDriver : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerMovement movement;

    [Header("Animator (Optional Override)")]
    [Tooltip("If empty, we will auto-find an Animator in children.")]
    [SerializeField] private Animator animator;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private static readonly int AnimJump = Animator.StringToHash("Jump");
    private static readonly int AnimLand = Animator.StringToHash("Land");

    private bool warnedMissingJump;
    private bool warnedMissingLand;

    private void Awake()
    {
        if (movement == null)
            movement = GetComponent<PlayerMovement>();

        // Initial fallback find
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        if (debugLogs)
            Debug.Log($"[AnimDriver] Awake. movement={(movement ? movement.name : "NULL")}, animator={(animator ? animator.gameObject.name : "NULL")}");
    }

    private void OnEnable()
    {
        if (movement == null) return;

        movement.OnJump += HandleJump;
        movement.OnLand += HandleLand;

        // In case animator got swapped while disabled, re-resolve
        EnsureAnimator();
    }

    private void OnDisable()
    {
        if (movement == null) return;

        movement.OnJump -= HandleJump;
        movement.OnLand -= HandleLand;
    }

    /// <summary>
    /// Call this from MaskController after spawning the new mask visual.
    /// </summary>
    public void SetAnimator(Animator a)
    {
        animator = a;
        warnedMissingJump = false;
        warnedMissingLand = false;

        if (debugLogs)
            Debug.Log($"[AnimDriver] Animator set to: {(animator ? animator.gameObject.name : "NULL")}");
    }

    private void HandleJump()
    {
        EnsureAnimator();
        SafeSetTrigger(AnimJump, "Jump", ref warnedMissingJump);
    }

    private void HandleLand()
    {
        EnsureAnimator();
        SafeSetTrigger(AnimLand, "Land", ref warnedMissingLand);
    }

    private void EnsureAnimator()
    {
        if (animator != null) return;

        // Try to find one on the spawned visual hierarchy
        animator = GetComponentInChildren<Animator>(true);

        if (animator != null)
        {
            warnedMissingJump = false;
            warnedMissingLand = false;

            if (debugLogs)
                Debug.Log($"[AnimDriver] Auto-found animator: {animator.gameObject.name}");
        }
    }

    private void SafeSetTrigger(int hash, string name, ref bool warnedFlag)
    {
        if (animator == null) return;

        for (int i = 0; i < animator.parameterCount; i++)
        {
            var p = animator.GetParameter(i);
            if (p.type == AnimatorControllerParameterType.Trigger &&
                (p.nameHash == hash || p.name == name))
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
