using UnityEngine;

public class GameModeIndicator : MonoBehaviour
{
    public RectTransform iconStrip;
    public CanvasGroup canvasGroup; 
    public float iconWidth = 300f;
    public float slideSpeed = 12f;
    public float fadeSpeed = 8f;

    private Vector2 targetPosition;
    private bool isMenuOpen = false;

    void Update()
    {
        // Smoothly slide the icons
        iconStrip.anchoredPosition = Vector2.Lerp(iconStrip.anchoredPosition, targetPosition, Time.deltaTime * slideSpeed);

        // Fade in if open, fade out if closed
        float targetAlpha = isMenuOpen ? 1f : 0f;
        canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
    }

    public void SetMenuState(bool open)
    {
        isMenuOpen = open;
    }

    public void UpdateDisplay(int modeIndex)
    {
        float newX = -(modeIndex * iconWidth);
        targetPosition = new Vector2(newX, 0);
    }
}