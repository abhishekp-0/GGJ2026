using UnityEngine;
using UnityEngine.UI; // Required for handling UI images

public class FixButtonHitbox : MonoBehaviour
{
    void Start()
    {
        // 0.1f means the alpha must be at least 0.1 to register a click
        this.GetComponent<Image>().alphaHitTestMinimumThreshold = 0.1f;
    }
}