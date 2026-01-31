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
    [SerializeField] private float baseGravityMagnitude = 25f; // positive for jump math
    [SerializeField] private JumpStrategy_ConfigSO jumpStrategy;

    [Header("Default Jump Profile (Fallback)")]
    [SerializeField] private MaskDefinition.JumpProfile defaultJumpProfile;

    [Header("2.5D Axis Lock")]
    [SerializeField] private bool lockWorldZ = true;

    [Header("Wall Stick (Cube)")]
    [SerializeField] private LayerMask wallLayers;
    [Tooltip("How far to raycast for a wall. Should be slightly > controller.radius.")]
    [SerializeField] private float wallCheckDistance = 0.6f;
    [Tooltip("Max downward speed while sticking to a wall.")]
    [SerializeField] private float wallSlideSpeed = 1.0f;
    [Tooltip("Upward velocity used for wall jump.")]
    [SerializeField] private float wallJumpUpVelocity = 8f;
    [Tooltip("Side push away from wall on wall jump (world X).")]
    [SerializeField] private float wallJumpPush = 10f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

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

    // Wall stick runtime state
    private bool wallStickEnabled;
    private bool isSticking;
    private int wallSide; // -1 = wall on left, +1 = wall on right, 0 = none

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        lockedZ = transform.position.z;
        wasGrounded = controller.isGrounded;

        // fallback so jump works even before first mask is equipped
        currentJumpProfile = defaultJumpProfile;
    }

    // ===== External Setters (called from PlayerController/MaskController) =====
    public void SetCameraTransform(Transform cam) => cameraTransform = cam;
    public void SetMoveInput(Vector2 move) => moveInput = move;
    public void SetSprintHeld(bool held) => sprintHeld = held;

    public void SetSpeedMultiplier(float multiplier) => speedMultiplier = multiplier;
    public void SetGravityMultiplier(float multiplier) => gravityMultiplier = multiplier;

    public void SetJumpProfile(MaskDefinition.JumpProfile profile) => currentJumpProfile = profile;

    public void SetWallStickEnabled(bool enabled)
    {
        wallStickEnabled = enabled;

        if (!enabled)
        {
            isSticking = false;
            wallSide = 0;
        }

        if (debugLogs)
            Debug.Log($"[PlayerMovement] WallStickEnabled={wallStickEnabled}");
    }

    // Called by input (space down)
    public void JumpPressed()
    {
        if (debugLogs)
            Debug.Log($"[PlayerMovement] JumpPressed(). grounded={controller.isGrounded}, sticking={isSticking}, wallSide={wallSide}");

        jumpHeld = true;

        // Wall jump if cube is sticking
        if (wallStickEnabled && isSticking && wallSide != 0)
        {
            DoWallJump();
            return;
        }

        // Buffer jump
        float buffer = (jumpStrategy != null) ? jumpStrategy.GetBufferTime(currentJumpProfile) : 0.1f;
        jumpBufferTimer = buffer;

        // Try immediately (fixes grounding flicker while running/edges)
        TryConsumeJump();
    }

    // Called by input (space up)
    public void JumpReleased()
    {
        jumpHeld = false;
    }

    private void Update()
    {
        // Cache grounded early for stable coyote while moving
        bool groundedAtStart = controller.isGrounded;

        // ----- Update coyote timer -----
        float coyote = (jumpStrategy != null) ? jumpStrategy.GetCoyoteTime(currentJumpProfile) : 0.1f;

        if (groundedAtStart)
            coyoteTimer = coyote;
        else
            coyoteTimer -= Time.deltaTime;

        // Buffer decays
        jumpBufferTimer -= Time.deltaTime;

        // Try buffered jump
        TryConsumeJump();

        // ----- Horizontal movement (NO ROTATION) -----
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

        // ----- Wall stick handling (cube) -----
        HandleWallStick();

        // ----- Gravity + vertical motion -----
        ApplyGravity();

        // Keep in 2.5D lane
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

        if (debugLogs)
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

        if (debugLogs)
            Debug.Log($"[PlayerMovement] DoJump() -> velocity.y={velocity.y:0.00}");
    }

    private void DoWallJump()
    {
        if (debugLogs)
            Debug.Log("[PlayerMovement] WALL JUMP!");

        // Jump up
        velocity.y = wallJumpUpVelocity;

        // Push away from wall
        float pushDir = -wallSide;
        controller.Move(new Vector3(pushDir * wallJumpPush, 0f, 0f) * Time.deltaTime);

        // Exit sticking
        isSticking = false;
        wallSide = 0;

        // Clear timers to prevent double-trigger
        jumpBufferTimer = 0f;
        coyoteTimer = 0f;
    }

    private void HandleWallStick()
    {
        if (!wallStickEnabled)
        {
            isSticking = false;
            wallSide = 0;
            return;
        }

        // No sticking if grounded
        if (controller.isGrounded)
        {
            isSticking = false;
            wallSide = 0;
            return;
        }

        // Stick only if pushing into wall (feels better)
        bool pushingLeft = moveInput.x < -0.1f;
        bool pushingRight = moveInput.x > 0.1f;

        if (CheckWall(out int side))
        {
            bool pushingIntoWall =
                (side == -1 && pushingLeft) ||
                (side == +1 && pushingRight);

            if (pushingIntoWall)
            {
                if (!isSticking && debugLogs)
                    Debug.Log($"[PlayerMovement] STICKING to wall side={side}");

                isSticking = true;
                wallSide = side;

                // Cap fall speed (slide)
                if (velocity.y < -wallSlideSpeed)
                    velocity.y = -wallSlideSpeed;

                return;
            }
        }

        // Not touching or not pushing into wall
        isSticking = false;
        wallSide = 0;
    }

    private bool CheckWall(out int side)
    {
        side = 0;

        Vector3 origin = transform.position + controller.center;
        float dist = Mathf.Max(wallCheckDistance, controller.radius + 0.05f);

        bool hitLeft = Physics.Raycast(origin, Vector3.left, dist, wallLayers, QueryTriggerInteraction.Ignore);
        bool hitRight = Physics.Raycast(origin, Vector3.right, dist, wallLayers, QueryTriggerInteraction.Ignore);

        if (hitLeft) side = -1;
        else if (hitRight) side = +1;

        return side != 0;
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
                // Optional landing bounce (ball)
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

        // If sticking: gravity effectively paused (we already cap slide speed)
        if (isSticking)
        {
            controller.Move(velocity * Time.deltaTime);
            wasGrounded = grounded;
            return;
        }

        float g = gravity * gravityMultiplier; // negative
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
