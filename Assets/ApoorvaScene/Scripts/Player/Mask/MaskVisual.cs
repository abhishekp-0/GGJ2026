using UnityEngine;

public sealed class MaskVisual : MonoBehaviour
{
    [SerializeField] private Transform visualRoot;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private GameObject current;

    public Transform CurrentVisualTransform { get; private set; }
    public Animator CurrentAnimator { get; private set; }

    private static readonly Vector3 SpawnLocalPos = new Vector3(0f, 1f, 0f);

    private void Awake()
    {
        if (visualRoot == null) visualRoot = transform;
    }

    public void Apply(MaskDefinition mask)
    {
        if (current != null)
        {
            Destroy(current);
            current = null;
        }

        CurrentVisualTransform = null;
        CurrentAnimator = null;

        if (mask == null || mask.visualPrefab == null)
            return;

        current = Instantiate(mask.visualPrefab, visualRoot);
        CurrentVisualTransform = current.transform;

        CurrentVisualTransform.localPosition = SpawnLocalPos;
        CurrentVisualTransform.localRotation = Quaternion.identity;
        CurrentVisualTransform.localScale = Vector3.one;

        // ✅ Animator could be on root or children
        CurrentAnimator = current.GetComponentInChildren<Animator>(true);

        if (debugLogs)
            Debug.Log($"[MaskVisual] Spawned: {current.name} | Animator={(CurrentAnimator ? CurrentAnimator.name : "NONE")}");
    }
}
