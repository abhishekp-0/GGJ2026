using UnityEngine;

public enum MaskId
{
    Ball,
    Pyramid,
    Cube,
    Rock
}

public enum MaskColliderMode
{
    CharacterControllerOnly,
    ExtraColliderSwap
}

[CreateAssetMenu(menuName = "GGJ/Masks/Mask Definition", fileName = "Mask_")]
public sealed class MaskDefinition : ScriptableObject
{
    [Header("Identity")]
    public MaskId id;
    public string displayName;
    public Sprite icon;

    [Header("Visual")]
    public GameObject visualPrefab; // spawned under PlayerVisual/Masks

    [Header("Movement Modifiers")]
    [Tooltip("Multiplier applied to walk/sprint speed in PlayerMovement")]
    public float speedMultiplier = 1f;

    [Tooltip("Gravity multiplier (1 = normal). If rock is heavier, use >1")]
    public float gravityMultiplier = 1f;

    [Header("CharacterController Shape")]
    public MaskColliderMode colliderMode = MaskColliderMode.CharacterControllerOnly;

    [Tooltip("Used when colliderMode = CharacterControllerOnly")]
    public float controllerRadius = 0.5f;
    public float controllerHeight = 2f;
    public Vector3 controllerCenter = new Vector3(0f, 1f, 0f);

    [Header("Extra Collider Swap (Optional)")]
    [Tooltip("Used when colliderMode = ExtraColliderSwap. Put SphereCollider/BoxCollider on this prefab.")]
    public GameObject colliderPrefab;

    [Header("Ability Flags (Jam-simple)")]
    public bool enableBounce;       // Ball
    public bool enableAerialDash;   // Pyramid
    public bool enableWallStick;    // Cube
    public bool enableSmash;        // Rock

    [Header("SFX/VFX (Optional)")]
    public AudioClip equipSfx;
    public GameObject equipVfxPrefab;

    [Header("Jump Profile")]
    public JumpProfile jump = new JumpProfile
    {
        jumpHeight = 1.5f,
        coyoteTime = 0.15f,
        jumpBufferTime = 0.1f,
        fallGravityMultiplier = 1.5f,
        lowJumpGravityMultiplier = 2f,
        enableLandingBounce = false,
        bounceMinFallSpeed = 5f,
        bounceVelocity = 10f
    };

    [System.Serializable]
    public struct JumpProfile
    {
        [Header("Jump")]
        public float jumpHeight;              // meters
        public float coyoteTime;              // seconds
        public float jumpBufferTime;          // seconds
        public float fallGravityMultiplier;   // >1 = snappier fall
        public float lowJumpGravityMultiplier;// >1 = short hop when jump released

        [Header("Ball Bounce (Optional Passive)")]
        public bool enableLandingBounce;
        public float bounceMinFallSpeed;      // if falling faster than this, bounce
        public float bounceVelocity;          // upward velocity after bounce
    }
}