using UnityEngine;
using System;

[RequireComponent(typeof(CharacterController))]
public sealed class PlayerMovement : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] private float walkSpeed = 4.5f;
    [SerializeField] private float sprintSpeed = 7.0f;

    [Header("Gravity")]
    [SerializeField] private float gravity = -25f;
    [SerializeField] private float baseGravityMagnitude = 25f;

    [Header("Jump Strategy")]
    [SerializeField] private JumpStrategy_ConfigSO jumpStrategy;

    [Header("Default Jump Profile (Fallback)")]
    [SerializeField] private MaskDefinition.JumpProfile defaultJumpProfile;

    [Header("2.5D Axis Lock")]
    [SerializeField] private bool lockWorldZ = true;

    [Header("Wall Stick (Cube)")]
    [SerializeField] private LayerMask wallLayers;
    [SerializeField] private float wallCheckDistance = 0.6f;
    [SerializeField] private float wallSlideSpeed = 1.0f;
    [SerializeField] private float wallJumpUpVelocity = 8f;
    [SerializeField] private float wallJumpPush = 10f;

    [Header("Ball Movement (Ball)")]
    [SerializeField] private float ballAcceleration = 35f;
    [SerializeField] private float ballDeceleration = 10f;
    [SerializeField] private float ballStopThreshold = 0.05f;
    [SerializeField] private float ballRollRadius = 0.5f;

    [Header("Ball Landing Bounce (Ball Only)")]
    [SerializeField] private float ballBounceMinFallSpeed = 6f;
    [SerializeField] private float ballBounceVelocity = 10f;
    [SerializeField] private float minAirborneTimeForBounce = 0.06f;

    [Header("Animation")]
    [SerializeField] private bool playBallJumpAnimation = true;
    [SerializeField] private bool playLandAnimation = true;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private CharacterController controller;

    private Vector2 moveInput;
    private bool sprintHeld;

    private Vector3 velocity;
    private float lockedZ;

    private float speedMultiplier = 1f;
    private float gravityMultiplier = 1f;

    private float coyoteTimer;
    private float jumpBufferTimer;

    private bool jumpHeld;
    private bool wasGrounded;

    private MaskDefinition.JumpProfile currentJumpProfile;

    // ball horizontal momentum
    private float currentVelX;

    // rolling visual target
    private Transform ballVisual;
    private bool rollVisualActive;

    // wall stick runtime
    private bool isSticking;
    private int wallSide; // -1 left, +1 right

    private IMovementStrategy currentStrategy;

    // ===== Animation =====
    private Animator currentAnimator;
    private static readonly int AnimJump = Animator.StringToHash("Jump");
    private static readonly int AnimLand = Animator.StringToHash("Land");

    private bool warnedMissingJump;
    private bool warnedMissingLand;

    // ===== Ball bounce runtime =====
    private bool ballBounceActive;
    private bool ballBounceConsumedThisLanding;
    private float ballAirborneTimer;
    private float ballMaxFallSpeedAbs;

    // ===== Events =====
    public event Action OnJump;
    public event Action OnLand;
    public event Action<Animator> OnAnimatorChanged;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        lockedZ = transform.position.z;
        wasGrounded = controller.isGrounded;
        currentJumpProfile = defaultJumpProfile;
    }

    // ================== API called by Controller/MaskController ==================
    public void SetMoveInput(Vector2 move) => moveInput = move;
    public void SetSprintHeld(bool held) => sprintHeld = held;

    public void SetSpeedMultiplier(float m) => speedMultiplier = m;
    public void SetGravityMultiplier(float m) => gravityMultiplier = m;

    public void SetJumpProfile(MaskDefinition.JumpProfile profile) => currentJumpProfile = profile;

    public void SetBallVisual(Transform t) => ballVisual = t;

    public void SetAnimator(Animator anim)
    {
        currentAnimator = anim;
        warnedMissingJump = false;
        warnedMissingLand = false;

        OnAnimatorChanged?.Invoke(anim);

        if (debugLogs)
        {
            string name = currentAnimator != null ? currentAnimator.gameObject.name : "NULL";
            Debug.Log($"[PlayerMovement] Animator set to: {name}");
        }
    }

    public void SetStrategy(IMovementStrategy strategy)
    {
        if (ReferenceEquals(currentStrategy, strategy)) return;

        currentStrategy?.OnExit(this);
        currentStrategy = strategy;
        currentStrategy?.OnEnter(this);

        if (debugLogs)
            Debug.Log($"[PlayerMovement] Strategy set to {currentStrategy?.GetType().Name}");
    }

    // ================== Jump input (space down/up) ==================
    public void JumpPressed()
    {
        jumpHeld = true;

        float buffer = (jumpStrategy != null) ? jumpStrategy.GetBufferTime(currentJumpProfile) : 0.1f;
        jumpBufferTimer = buffer;

        // Wall jump immediately if sticking
        if (isSticking && wallSide != 0)
        {
            DoWallJump();
            return;
        }

        TryConsumeJump();
    }

    public void JumpReleased() => jumpHeld = false;

    private void Update()
    {
        float dt = Time.deltaTime;

        // Read grounded once per frame
        bool grounded = controller.isGrounded;

        // Coyote timer
        float coyote = (jumpStrategy != null) ? jumpStrategy.GetCoyoteTime(currentJumpProfile) : 0.1f;
        coyoteTimer = grounded ? coyote : (coyoteTimer - dt);

        // Buffer decay
        jumpBufferTimer -= dt;

        // Attempt jump
        TryConsumeJump();

        // Strategy tick
        currentStrategy?.Tick(this, dt, grounded);

        // Landing anim
        bool justLanded = grounded && !wasGrounded;
        if (playLandAnimation && justLanded)
            OnLand?.Invoke();

        // Lane lock
        if (lockWorldZ)
        {
            Vector3 p = transform.position;
            p.z = lockedZ;
            transform.position = p;
        }

        wasGrounded = grounded;
    }

    // ================== Movement Helpers for Strategies ==================
    public void MoveHorizontalImmediate(float dt)
    {
        float speed = (sprintHeld ? sprintSpeed : walkSpeed) * speedMultiplier;
        float x = moveInput.x * speed;

        if (Mathf.Abs(moveInput.x) > 0.01f)
            controller.Move(new Vector3(x, 0f, 0f) * dt);
    }

    public void MoveHorizontalBallMomentum(float dt)
    {
        float speed = (sprintHeld ? sprintSpeed : walkSpeed) * speedMultiplier;
        float desiredVelX = moveInput.x * speed;

        if (Mathf.Abs(moveInput.x) > 0.01f)
            currentVelX = Mathf.MoveTowards(currentVelX, desiredVelX, ballAcceleration * dt);
        else
            currentVelX = Mathf.MoveTowards(currentVelX, 0f, ballDeceleration * dt);

        if (Mathf.Abs(currentVelX) < ballStopThreshold)
            currentVelX = 0f;

        controller.Move(new Vector3(currentVelX, 0f, 0f) * dt);
    }

    public void ResetHorizontalMomentum() => currentVelX = 0f;

    public void SetRollVisualActive(bool active) => rollVisualActive = active;

    public void ApplyBallRollVisual(float dt)
    {
        if (!rollVisualActive) return;
        if (ballVisual == null) return;
        if (ballRollRadius <= 0.0001f) return;

        float radiansPerSec = currentVelX / ballRollRadius;
        float degrees = radiansPerSec * Mathf.Rad2Deg * dt;

        ballVisual.Rotate(0f, 0f, -degrees, Space.Self);
    }

    // ================== Jump / Gravity ==================
    private void TryConsumeJump()
    {
        if (jumpBufferTimer <= 0f) return;
        if (coyoteTimer <= 0f) return;

        DoJump();
        jumpBufferTimer = 0f;
        coyoteTimer = 0f;
    }

    private void DoJump()
    {
        float g = Mathf.Max(0.01f, baseGravityMagnitude);

        float v0 = (jumpStrategy != null)
            ? jumpStrategy.GetJumpVelocity(currentJumpProfile, g)
            : Mathf.Sqrt(2f * g * Mathf.Max(0.01f, currentJumpProfile.jumpHeight));

        velocity.y = v0;

        if (playBallJumpAnimation)
            OnJump?.Invoke();

        if (debugLogs)
            Debug.Log($"[PlayerMovement] Jump -> v0={velocity.y:0.00}");
    }

    public void ApplyGravityOptimized(float dt, bool grounded, bool allowStickOverride = false)
    {
        // grounded snap
        if (grounded && velocity.y < 0f)
            velocity.y = -2f;

        // wall stick override
        if (allowStickOverride && isSticking)
        {
            controller.Move(velocity * dt);
            return;
        }

        float gFinal = gravity * gravityMultiplier; // negative
        float extra = 1f;

        if (!grounded && jumpStrategy != null)
        {
            if (velocity.y < 0f)
                extra = jumpStrategy.GetFallMultiplier(currentJumpProfile);
            else if (!jumpHeld)
                extra = jumpStrategy.GetLowJumpMultiplier(currentJumpProfile);
        }

        velocity.y += gFinal * extra * dt;
        controller.Move(velocity * dt);
    }

    // ================== Ball Landing Bounce (Ball Only) ==================
    public void SetBallBounceActive(bool active)
    {
        ballBounceActive = active;

        if (!ballBounceActive)
        {
            ballBounceConsumedThisLanding = false;
            ballAirborneTimer = 0f;
            ballMaxFallSpeedAbs = 0f;
        }
    }

    /// <summary>
    /// Call ONLY in Ball strategy BEFORE ApplyGravityOptimized().
    /// </summary>
    public void UpdateBallLandingBounce(float dt, bool grounded)
    {
        if (!ballBounceActive) return;

        bool justLanded = grounded && !wasGrounded;

        if (!grounded)
        {
            if (wasGrounded)
            {
                ballAirborneTimer = 0f;
                ballMaxFallSpeedAbs = 0f;
                ballBounceConsumedThisLanding = false;
            }

            ballAirborneTimer += dt;

            if (velocity.y < 0f)
            {
                float fallAbs = Mathf.Abs(velocity.y);
                if (fallAbs > ballMaxFallSpeedAbs)
                    ballMaxFallSpeedAbs = fallAbs;
            }
            return;
        }

        if (justLanded && !ballBounceConsumedThisLanding)
        {
            bool enoughAirTime = ballAirborneTimer >= minAirborneTimeForBounce;
            bool enoughImpact = ballMaxFallSpeedAbs >= ballBounceMinFallSpeed;

            if (enoughAirTime && enoughImpact)
            {
                velocity.y = ballBounceVelocity;
                ballBounceConsumedThisLanding = true;

                if (debugLogs)
                    Debug.Log($"[BallBounce] bounce! air={ballAirborneTimer:0.000}, fall={ballMaxFallSpeedAbs:0.00}");
            }

            ballAirborneTimer = 0f;
            ballMaxFallSpeedAbs = 0f;
        }
    }

    // ================== Wall Stick Helpers (Cube Strategy) ==================
    public void ClearWallStickState()
    {
        isSticking = false;
        wallSide = 0;
    }

    public void HandleWallStick(float dt)
    {
        bool pushingLeft = moveInput.x < -0.1f;
        bool pushingRight = moveInput.x > 0.1f;

        if (!pushingLeft && !pushingRight)
        {
            ClearWallStickState();
            return;
        }

        if (CheckWall(out int side))
        {
            bool pushingIntoWall =
                (side == -1 && pushingLeft) ||
                (side == +1 && pushingRight);

            if (pushingIntoWall)
            {
                isSticking = true;
                wallSide = side;

                if (velocity.y < -wallSlideSpeed)
                    velocity.y = -wallSlideSpeed;

                return;
            }
        }

        ClearWallStickState();
    }

    // ✅ ADD THIS HERE (right under HandleWallStick)
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

    private void DoWallJump()
    {
        velocity.y = wallJumpUpVelocity;

        float pushDir = -wallSide;
        currentVelX = pushDir * wallJumpPush;

        ClearWallStickState();
        jumpBufferTimer = 0f;
        coyoteTimer = 0f;
    }
}