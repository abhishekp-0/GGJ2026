using UnityEngine;

public class Collectible : MonoBehaviour
{
    public int value = 1;

    // Animation parameters
    public float floatAmplitude = 0.5f; // Height of the float
    public float floatFrequency = 1f;   // Speed of the float
    private Vector3 startPos;

    private void Start()
    {
        startPos = transform.position;
    }

    private void Update()
    {
        // Sine wave animation for floating effect
        float newY = startPos.y + Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        transform.position = new Vector3(startPos.x, newY, startPos.z);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Example: PlayerInventory.Instance.Add(value);
            Destroy(gameObject);
        }
    }
}
