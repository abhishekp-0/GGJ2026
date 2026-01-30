using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public sealed class MaskColliderApplier : MonoBehaviour
{
    [SerializeField] private Transform colliderRoot;

    private CharacterController controller;
    private GameObject extraColliderObj;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (colliderRoot == null) colliderRoot = transform;
    }

    public void Apply(MaskDefinition mask)
    {
        if (mask == null) return;

        // ✅ Safely resize CharacterController
        controller.enabled = false;

        controller.radius = Mathf.Max(0.05f, mask.controllerRadius);
        controller.height = Mathf.Max(controller.radius * 2f, mask.controllerHeight);
        controller.center = mask.controllerCenter;

        controller.enabled = true;

        // ✅ Handle extra collider swap if desired
        if (mask.colliderMode == MaskColliderMode.ExtraColliderSwap)
            SwapExtraCollider(mask.colliderPrefab);
        else
            ClearExtraCollider();
    }

    private void SwapExtraCollider(GameObject prefab)
    {
        ClearExtraCollider();
        if (prefab == null) return;

        extraColliderObj = Instantiate(prefab, colliderRoot);
        extraColliderObj.transform.localPosition = Vector3.zero;
        extraColliderObj.transform.localRotation = Quaternion.identity;
        extraColliderObj.transform.localScale = Vector3.one;
    }

    private void ClearExtraCollider()
    {
        if (extraColliderObj != null)
        {
            Destroy(extraColliderObj);
            extraColliderObj = null;
        }
    }
}
