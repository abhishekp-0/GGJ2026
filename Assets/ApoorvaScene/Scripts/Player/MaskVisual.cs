using UnityEngine;

public sealed class MaskVisual : MonoBehaviour
{
    [SerializeField] private Transform visualRoot;

    private GameObject current;

    private void Awake()
    {
        if (visualRoot == null) visualRoot = transform;
    }

    public void Apply(MaskDefinition mask)
    {
        if (current != null)
            Destroy(current);

        if (mask == null || mask.visualPrefab == null)
            return;

        current = Instantiate(mask.visualPrefab, visualRoot);
        current.transform.localPosition = new Vector3(0f, 1f, 0f); // lift it up
        current.transform.localRotation = Quaternion.identity;
        current.transform.localScale = Vector3.one;

        Debug.Log($"Spawned visual: {current.name} under {visualRoot.name}");
    }

}
