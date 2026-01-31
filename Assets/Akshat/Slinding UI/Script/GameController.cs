using UnityEngine;
using UnityEngine.InputSystem;

public class GameController : MonoBehaviour
{
    public GameModeIndicator uiIndicator;
    private int currentMode = 0;
    private int totalModes = 3;

    void Update()
    {
        // 1. Check if Tab is being held
        bool isHoldingTab = Keyboard.current.tabKey.isPressed;
        uiIndicator.SetMenuState(isHoldingTab);

        // 2. Only allow selection if Tab is held
        if (isHoldingTab)
        {
            // Use 'D' to move Right, 'A' to move Left
            if (Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame) 
            {
                ChangeMode(1);
            }
            else if (Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.leftArrowKey.wasPressedThisFrame)
            {
                ChangeMode(-1);
            }
        }
    }

    void ChangeMode(int direction)
    {
        // Cycle through modes (0, 1, 2)
        currentMode = (currentMode + direction + totalModes) % totalModes;
        
        if (uiIndicator != null)
        {
            uiIndicator.UpdateDisplay(currentMode);
        }

        // Add your actual Game Mode transformation logic here
        Debug.Log("Active Mode: " + currentMode);
    }
}