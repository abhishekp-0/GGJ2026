using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public sealed class PlayerMovement : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] private float walkSpeed = 4.5f;
    [SerializeField] private float sprintSpeed = 7.0f;
    [SerializeField] private float rotationSpeed = 12f;

    [Header("Gravity")]
    [SerializeField] private float gravity = -25f;

    [Header("Block Axis")]
    [SerializeField] private bool lockWorldZ = true;

    private CharacterController controller;

    private Transform cameraTransform;

    private Vector2 moveInput;
    private bool sprintHeld;

    private Vector3 velocity;

    private float lockedZ;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        lockedZ = transform.position.z;
    }

    public void SetCameraTransform(Transform cam)
    {
        cameraTransform = cam;
    }

    public void SetMoveInput(Vector2 move)
    {
        moveInput = move;
    }

    public void SetSprintHeld(bool held)
    {
        sprintHeld = held;
    }

    private void Update()
    {
        Vector3 moveDir = GetCameraRelativeDirection(moveInput);
        float speed = sprintHeld ? sprintSpeed : walkSpeed;

        if (lockWorldZ)
        {
            moveDir.z = 0f;
            moveDir = moveDir.sqrMagnitude > 0.0001f ? moveDir.normalized : Vector3.zero;
        }

        if (moveDir.sqrMagnitude > 0.0001f)
        {
            controller.Move(moveDir * speed * Time.deltaTime);

            Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        ApplyGravity();

        if (lockWorldZ)
        {
            Vector3 p = transform.position;
            p.z = lockedZ;
            transform.position = p;
        }
    }

    private Vector3 GetCameraRelativeDirection(Vector2 move)
    {
        Vector3 inputDir = new Vector3(move.x, 0f, move.y);

        if (cameraTransform == null)
            return inputDir.normalized;

        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        Vector3 worldDir = forward * inputDir.z + right * inputDir.x;
        return worldDir.normalized;
    }

    private void ApplyGravity()
    {
        if (controller.isGrounded && velocity.y < 0f)
            velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
