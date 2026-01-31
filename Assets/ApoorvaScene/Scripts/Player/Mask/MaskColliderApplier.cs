using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public sealed class MaskColliderApplier: MonoBehaviour
{
    [SerializeField] private Transform colliderRoot; // where extra collider prefab is spawned (optional)

    private CharacterController cc;
    private GameObject spawnedExtra;

    private void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (colliderRoot == null) colliderRoot = transform;
    }

    public void Apply(MaskDefinition mask)
    {
        if (mask == null) return;

        // Clear previous extra collider
        if (spawnedExtra != null)
        {
            Destroy(spawnedExtra);
            spawnedExtra = null;
        }

        if (mask.colliderMode == MaskColliderMode.CharacterControllerOnly)
        {
            cc.radius = Mathf.Max(0.01f, mask.controllerRadius);
            cc.height = Mathf.Max(cc.radius * 2f, mask.controllerHeight);
            cc.center = mask.controllerCenter;
        }
        else
        {
            // Keep CC usable for movement, but optionally add extra collider for interaction
            // (CharacterController doesn't use other colliders for collision resolution)
            if (mask.colliderPrefab != null)
            {
                spawnedExtra = Instantiate(mask.colliderPrefab, colliderRoot);
                spawnedExtra.transform.localPosition = Vector3.zero;
                spawnedExtra.transform.localRotation = Quaternion.identity;
                spawnedExtra.transform.localScale = Vector3.one;
            }
        }
    }
}
