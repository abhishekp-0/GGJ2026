6. How Other Scripts MUST Use It
Taking Damage (Enemy / Trap)
GameManager.Instance.TakeDamage(1);


No health variables outside the GameManager.

Equipping a Mask
GameManager.Instance.EquipMask(MaskType.Shadow);


Do not track mask state locally.

Checking Game Over
if (GameManager.Instance.CurrentState == GameState.GameOver)
{
    // Disable movement, input, etc.
}

7. UI Example (Decoupled, Clean)
using UnityEngine;

public class HealthUI : MonoBehaviour
{
    private void OnEnable()
    {
        GameManager.Instance.OnHealthChanged += UpdateHealth;
    }

    private void OnDisable()
    {
        GameManager.Instance.OnHealthChanged -= UpdateHealth;
    }

    void UpdateHealth(int currentHealth)
    {
        // Update hearts / bar
    }
}


UI listens, it does not poll.

8. What Else Belongs in GameManager (Jam-Safe)

✔ Player lives / retries
✔ Level completion flag
✔ Score / collectibles count
✔ Pause state
✔ Win condition