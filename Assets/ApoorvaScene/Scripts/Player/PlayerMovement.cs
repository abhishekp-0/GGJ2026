using UnityEngine;
using System;
using ApoorvaGame.Interfaces;

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

    [Header("Wall Stick Tuning (Vertical Walls Only)")]
    [SerializeField, Tooltip("Only surfaces with |normal.y| <= this count as vertical walls. 0.2 is good.")]
    private float maxWallNormalY = 0.2f;

    [SerializeField, Tooltip("Extra resistance against sliding down while sticking. Higher = more friction.")]
    private float wallFriction = 25f;

    [SerializeField, Tooltip("Push the controller slightly into the wall to keep contact stable.")]
    private float wallStickPush = 2f;

    [SerializeField, Tooltip("If true: almost no sliding. If false: controlled slide using wallSlideSpeed.")]
    private bool hardStick = true;

    [Header("Ball Movement (Ball)")]
    [SerializeField] private float ballAcceleration = 35f;
    [SerializeField] private float ballDeceleration = 10f;
    [SerializeField] private float ballStopThreshold = 0.05f;
    [SerializeField] private float ballRollRadius = 0.5f;

    [Header("Ball Landing Bounce (Ball Only)")]
    [SerializeField] private float ballBounceMinFallSpeed = 6f;
    [SerializeField] private float ballBounceVelocity = 10f;
    [SerializeField] private float minAirborneTimeForBounce = 0.06f;

    [Header("Rock (Heavy + Smash)")]
    [SerializeField, Tooltip("Rock jump is reduced by this multiplier (0.35 to 0.6 is typical).")]
    private float rockJumpHeightMultiplier = 0.45f;

    [SerializeField, Tooltip("If fall speed abs >= this, landing triggers smash.")]
    private float rockSmashMinFallSpeed = 12f;

    [SerializeField, Tooltip("Radius of the smash effect at landing.")]
    private float rockSmashRadius = 2.0f;

    [SerializeField, Tooltip("Layers affected by smash (enemies, breakable ground, etc.)")]
    private LayerMask rockSmashLayers;

    [SerializeField, Tooltip("Optional upward impulse after smash (0 = none).")]
    private float rockSmashRecoilUpVelocity = 0f;

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

    // ===== Ball bounce runtime =====
    private bool ballBounceActive;
    private bool ballBounceConsumedThisLanding;
    private float ballAirborneTimer;
    private float ballMaxFallSpeedAbs;

    // ===== Rock runtime =====
    private bool rockMode;
    private bool rockSmashActive;
    private float rockMaxFallSpeedAbs;
    private bool rockSmashConsumedThisLanding;

    // ===== Events (for separate animation system / VFX / SFX) =====
    public event Action OnJump;
    public event Action OnLand;
    public event Action<float> OnRockSmash; // impact speed

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

    public void SetStrategy(IMovementStrategy strategy)
    {
        if (ReferenceEquals(currentStrategy, strategy)) return;

        currentStrategy?.OnExit(this);
        currentStrategy = strategy;
        currentStrategy?.OnEnter(this);

        if (debugLogs)
            Debug.Log($"[PlayerMovement] Strategy set to {currentStrategy?.GetType().Name}");
    }

    // -------- Mode toggles from MaskController --------
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

    public void SetRockMode(bool active)
    {
        rockMode = active;

        if (!rockMode)
        {
            rockMaxFallSpeedAbs = 0f;
            rockSmashConsumedThisLanding = false;
        }
    }

    public void SetRockSmashActive(bool active)
    {
        rockSmashActive = active;

        if (!rockSmashActive)
        {
            rockMaxFallSpeedAbs = 0f;
            rockSmashConsumedThisLanding = false;
        }
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

        bool grounded = controller.isGrounded;

        // Coyote
        float coyote = (jumpStrategy != null) ? jumpStrategy.GetCoyoteTime(currentJumpProfile) : 0.1f;
        coyoteTimer = grounded ? coyote : (coyoteTimer - dt);

        // Buffer decay
        jumpBufferTimer -= dt;

        // Attempt jump
        TryConsumeJump();

        // Strategy tick
        currentStrategy?.Tick(this, dt, grounded);

        bool justLanded = grounded && !wasGrounded;
        if (justLanded)
        {
            if (playLandAnimation)
                OnLand?.Invoke();
        }

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

        // Normal jump height from profile
        float jumpHeight = Mathf.Max(0.01f, currentJumpProfile.jumpHeight);

        // ✅ Rock is heavy: reduce jump height
        if (rockMode)
            jumpHeight *= rockJumpHeightMultiplier;

        float v0 = (jumpStrategy != null)
            ? Mathf.Sqrt(2f * g * jumpHeight)
            : Mathf.Sqrt(2f * g * jumpHeight);

        velocity.y = v0;

        if (playBallJumpAnimation)
            OnJump?.Invoke();

        if (debugLogs)
            Debug.Log($"[PlayerMovement] Jump -> v0={velocity.y:0.00} (rockMode={rockMode})");
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

    // ================== Rock Smash (Rock Only) ==================
    public void UpdateRockSmash(float dt, bool grounded)
    {
        if (!rockSmashActive) return;

        bool justLanded = grounded && !wasGrounded;

        if (!grounded)
        {
            if (wasGrounded)
            {
                rockMaxFallSpeedAbs = 0f;
                rockSmashConsumedThisLanding = false;
            }

            if (velocity.y < 0f)
            {
                float fallAbs = Mathf.Abs(velocity.y);
                if (fallAbs > rockMaxFallSpeedAbs)
                    rockMaxFallSpeedAbs = fallAbs;
            }

            return;
        }

        if (justLanded && !rockSmashConsumedThisLanding)
        {
            if (rockMaxFallSpeedAbs >= rockSmashMinFallSpeed)
            {
                rockSmashConsumedThisLanding = true;

                if (debugLogs)
                    Debug.Log($"[RockSmash] IMPACT! fall={rockMaxFallSpeedAbs:0.00}");

                TriggerRockSmash(rockMaxFallSpeedAbs);

                if (rockSmashRecoilUpVelocity > 0f)
                    velocity.y = rockSmashRecoilUpVelocity;
            }

            rockMaxFallSpeedAbs = 0f;
        }
    }

    private void TriggerRockSmash(float impactSpeedAbs)
    {
        OnRockSmash?.Invoke(impactSpeedAbs);

        // Smash origin near feet
        Vector3 feet = transform.position + controller.center;
        feet.y -= (controller.height * 0.5f - controller.radius);

        Collider[] hits = Physics.OverlapSphere(
            feet,
            rockSmashRadius,
            rockSmashLayers,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null) continue;

            // Optional: enemy damage hook
            if (hits[i].TryGetComponent<IDamageable>(out var dmg))
                dmg.TakeDamage(1);

            // Optional: breakable ground hook
            if (hits[i].TryGetComponent<IBreakable>(out var br))
                br.Break();
        }
    }

    // ================== Wall Stick Helpers (Cube Strategy) ==================
    public void ClearWallStickState()
    {
        isSticking = false;
        wallSide = 0;
    }

    /// <summary>
    /// Vertical walls only + friction. Call from CubeMovementStrategy when airborne.
    /// </summary>
    public void HandleWallStick(float dt)
    {
        if (controller.isGrounded)
        {
            ClearWallStickState();
            return;
        }

        bool pushingLeft = moveInput.x < -0.1f;
        bool pushingRight = moveInput.x > 0.1f;

        if (!pushingLeft && !pushingRight)
        {
            ClearWallStickState();
            return;
        }

        if (CheckWall(out int side, out RaycastHit hit))
        {
            bool pushingIntoWall =
                (side == -1 && pushingLeft) ||
                (side == +1 && pushingRight);

            if (!pushingIntoWall)
            {
                ClearWallStickState();
                return;
            }

            isSticking = true;
            wallSide = side;

            // small push into wall to keep contact stable
            float pushX = -hit.normal.x * wallStickPush;
            controller.Move(new Vector3(pushX, 0f, 0f) * dt);

            // friction against sliding
            if (hardStick)
            {
                if (velocity.y < 0f)
                    velocity.y = Mathf.MoveTowards(velocity.y, 0f, wallFriction * dt);
            }
            else
            {
                if (velocity.y < -wallSlideSpeed)
                    velocity.y = -wallSlideSpeed;

                velocity.y = Mathf.MoveTowards(velocity.y, -wallSlideSpeed, (wallFriction * 0.2f) * dt);
            }

            return;
        }

        ClearWallStickState();
    }

    private bool CheckWall(out int side, out RaycastHit hit)
    {
        side = 0;
        hit = default;

        Vector3 origin = transform.position + controller.center;
        float dist = Mathf.Max(wallCheckDistance, controller.radius + 0.05f);

        if (Physics.Raycast(origin, Vector3.left, out RaycastHit leftHit, dist, wallLayers, QueryTriggerInteraction.Ignore))
        {
            if (Mathf.Abs(leftHit.normal.y) <= maxWallNormalY)
            {
                side = -1;
                hit = leftHit;
                return true;
            }
        }

        if (Physics.Raycast(origin, Vector3.right, out RaycastHit rightHit, dist, wallLayers, QueryTriggerInteraction.Ignore))
        {
            if (Mathf.Abs(rightHit.normal.y) <= maxWallNormalY)
            {
                side = +1;
                hit = rightHit;
                return true;
            }
        }

        return false;
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

// Optional simple interfaces (put in their own file if you want)
public interface IDamageable { void TakeDamage(int amount); }
public interface IBreakable { void Break(); }
