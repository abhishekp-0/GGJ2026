using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-10)]
public sealed class PlayerController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private InputReader input;
    [SerializeField] private PlayerMovement movement;

    [Header("Debug")]
    [SerializeField] private bool enableDebugMaskKeys = true;

    private void OnEnable()
    {
        input.JumpPressed += movement.JumpPressed;
        input.JumpReleased += movement.JumpReleased;
    }

    private void OnDisable()
    {
        input.JumpPressed -= movement.JumpPressed;
        input.JumpReleased -= movement.JumpReleased;
    }

    private void Update()
    {
        movement.SetMoveInput(input.MoveValue);
        movement.SetSprintHeld(input.SprintHeld);

        if (enableDebugMaskKeys)
            HandleMaskDebugInput();
    }

    private void HandleMaskDebugInput()
    {
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
            GameManager.Instance.RequestEquipMask(MaskType.Ball);

        if (Keyboard.current.digit2Key.wasPressedThisFrame)
            GameManager.Instance.RequestEquipMask(MaskType.Pyramid);

        if (Keyboard.current.digit3Key.wasPressedThisFrame)
            GameManager.Instance.RequestEquipMask(MaskType.Cube);

        if (Keyboard.current.digit4Key.wasPressedThisFrame)
            GameManager.Instance.RequestEquipMask(MaskType.Rock);
    }
}