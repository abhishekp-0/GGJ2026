using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public sealed class PlayerMovement : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] private float walkSpeed = 4.5f;
    [SerializeField] private float sprintSpeed = 7.0f;

    [Header("Gravity")]
    [SerializeField] private float gravity = -25f;

    [Header("Jump Base")]
    [SerializeField] private float baseGravityMagnitude = 25f; // positive for math
    [SerializeField] private JumpStrategy_ConfigSO jumpStrategy;

    [Header("Default Jump Profile (Fallback)")]
    [SerializeField] private MaskDefinition.JumpProfile defaultJumpProfile;

    [Header("Block Axis")]
    [SerializeField] private bool lockWorldZ = true;

    private CharacterController controller;
    private Transform cameraTransform;

    private Vector2 moveInput;
    private bool sprintHeld;

    private Vector3 velocity;
    private float lockedZ;

    private float speedMultiplier = 1f;
    private float gravityMultiplier = 1f;

    private float coyoteTimer;
    private float jumpBufferTimer;

    private bool jumpHeld;
    private float lastFallSpeedAbs;
    private bool wasGrounded;

    private MaskDefinition.JumpProfile currentJumpProfile;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        lockedZ = transform.position.z;

        wasGrounded = controller.isGrounded;
        currentJumpProfile = defaultJumpProfile;
    }

    public void SetCameraTransform(Transform cam) => cameraTransform = cam;
    public void SetMoveInput(Vector2 move) => moveInput = move;
    public void SetSprintHeld(bool held) => sprintHeld = held;

    public void SetSpeedMultiplier(float multiplier) => speedMultiplier = multiplier;
    public void SetGravityMultiplier(float multiplier) => gravityMultiplier = multiplier;

    public void SetJumpProfile(MaskDefinition.JumpProfile profile) => currentJumpProfile = profile;

    public void JumpPressed()
    {
        Debug.Log($"[PlayerMovement] JumpPressed() called. Grounded={controller.isGrounded}");
        jumpHeld = true;

        // Start buffer
        float buffer = (jumpStrategy != null) ? jumpStrategy.GetBufferTime(currentJumpProfile) : 0.1f;
        jumpBufferTimer = buffer;

        // ✅ Try jump immediately (so running/edge grounding flicker won’t miss)
        TryConsumeJump();
    }

    public void JumpReleased()
    {
        jumpHeld = false;
    }

    private void Update()
    {
        // Cache grounded at frame start (more stable than checking after horizontal move)
        bool groundedAtStart = controller.isGrounded;

        // ----- Update coyote timer -----
        float coyote = (jumpStrategy != null) ? jumpStrategy.GetCoyoteTime(currentJumpProfile) : 0.1f;

        if (groundedAtStart)
            coyoteTimer = coyote;
        else
            coyoteTimer -= Time.deltaTime;

        // Decrease jump buffer
        jumpBufferTimer -= Time.deltaTime;

        // If player buffered jump earlier, try again here
        TryConsumeJump();

        // ----- Horizontal movement (no rotation) -----
        Vector3 moveDir = GetCameraRelativeDirection(moveInput);
        float speed = (sprintHeld ? sprintSpeed : walkSpeed) * speedMultiplier;

        if (lockWorldZ)
        {
            moveDir.z = 0f;
            moveDir = moveDir.sqrMagnitude > 0.0001f ? moveDir.normalized : Vector3.zero;
        }

        if (moveDir.sqrMagnitude > 0.0001f)
        {
            controller.Move(moveDir * speed * Time.deltaTime);
        }

        ApplyGravity();

        if (lockWorldZ)
        {
            Vector3 p = transform.position;
            p.z = lockedZ;
            transform.position = p;
        }
    }

    private void TryConsumeJump()
    {
        if (jumpBufferTimer <= 0f) return;
        if (coyoteTimer <= 0f) return;

        Debug.Log($"[PlayerMovement] Jump TRIGGERED. buffer={jumpBufferTimer:0.000}, coyote={coyoteTimer:0.000}");
        DoJump();

        jumpBufferTimer = 0f;
        coyoteTimer = 0f;
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

    private void DoJump()
    {
        float g = Mathf.Max(0.01f, baseGravityMagnitude);

        float v0 = (jumpStrategy != null)
            ? jumpStrategy.GetJumpVelocity(currentJumpProfile, g)
            : Mathf.Sqrt(2f * g * Mathf.Max(0.01f, currentJumpProfile.jumpHeight));

        velocity.y = v0;
        Debug.Log($"[PlayerMovement] DoJump() -> velocity.y set to {velocity.y:0.00}");
    }

    private void ApplyGravity()
    {
        bool grounded = controller.isGrounded;

        // Detect landing (air -> ground)
        bool justLanded = grounded && !wasGrounded;

        if (grounded)
        {
            if (velocity.y < 0f)
            {
                // Optional bounce on landing
                if (justLanded && jumpStrategy != null &&
                    jumpStrategy.ShouldBounceOnLanding(currentJumpProfile, lastFallSpeedAbs))
                {
                    velocity.y = jumpStrategy.GetBounceVelocity(currentJumpProfile);
                }
                else
                {
                    velocity.y = -2f;
                }
            }

            lastFallSpeedAbs = 0f;
        }
        else
        {
            if (velocity.y < 0f)
                lastFallSpeedAbs = Mathf.Abs(velocity.y);
        }

        float g = gravity * gravityMultiplier;
        float extraMultiplier = 1f;

        if (!grounded && jumpStrategy != null)
        {
            if (velocity.y < 0f)
                extraMultiplier = jumpStrategy.GetFallMultiplier(currentJumpProfile);
            else if (!jumpHeld)
                extraMultiplier = jumpStrategy.GetLowJumpMultiplier(currentJumpProfile);
        }

        velocity.y += g * extraMultiplier * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        wasGrounded = grounded;
    }
}
