using UnityEngine;

public sealed class MaskVisual : MonoBehaviour
{
    [SerializeField] private Transform visualRoot;

    private GameObject current;

    public Transform CurrentVisualTransform { get; private set; }
    public Animator CurrentAnimator { get; private set; }

    private void Awake()
    {
        if (visualRoot == null) visualRoot = transform;
    }

    public void Apply(MaskDefinition mask)
    {
        if (current != null)
            Destroy(current);

        CurrentVisualTransform = null;
        CurrentAnimator = null;

        if (mask == null || mask.visualPrefab == null)
            return;

        current = Instantiate(mask.visualPrefab, visualRoot);
        CurrentVisualTransform = current.transform;

        current.transform.localPosition = new Vector3(0f, 1f, 0f);
        current.transform.localRotation = Quaternion.identity;
        current.transform.localScale = Vector3.one;

        // ✅ Find animator on spawned prefab (including children)
        CurrentAnimator = current.GetComponentInChildren<Animator>(true);

        Debug.Log($"Spawned visual: {current.name} | Animator = {(CurrentAnimator ? CurrentAnimator.name : "NONE")}");
    }
}
