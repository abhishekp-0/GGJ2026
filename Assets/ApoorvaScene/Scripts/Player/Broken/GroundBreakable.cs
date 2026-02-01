using UnityEngine;

public sealed class GroundBreakable : MonoBehaviour
{
    [SerializeField] private GameObject brokenPrefab;

    public void Break()
    {
        if (brokenPrefab != null)
            Instantiate(brokenPrefab, transform.position, transform.rotation);

        Destroy(gameObject);
    }
}
