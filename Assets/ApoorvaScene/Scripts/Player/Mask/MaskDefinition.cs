using UnityEngine;

public enum MaskColliderMode
{
    CharacterControllerOnly,
    ExtraColliderSwap
}

[CreateAssetMenu(menuName = "GGJ/Masks/Mask Definition", fileName = "Mask_")]
public sealed class MaskDefinition : ScriptableObject
{
    [Header("Identity")]
    public MaskType id;
    public string displayName;
    public Sprite icon;

    [Header("Visual")]
    public GameObject visualPrefab;

    [Header("Movement Modifiers")]
    public float speedMultiplier = 1f;
    public float gravityMultiplier = 1f;

    [Header("CharacterController Shape")]
    public MaskColliderMode colliderMode = MaskColliderMode.CharacterControllerOnly;
    public float controllerRadius = 0.5f;
    public float controllerHeight = 2f;
    public Vector3 controllerCenter = new Vector3(0f, 1f, 0f);

    [Header("Extra Collider Swap (Optional)")]
    public GameObject colliderPrefab;

    [Header("Ability Flags (Jam-simple)")]
    public bool enableBounce;       // Ball mask identifier (keep)
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
        lowJumpGravityMultiplier = 2f
    };

    [System.Serializable]
    public struct JumpProfile
    {
        [Header("Jump")]
        public float jumpHeight;
        public float coyoteTime;
        public float jumpBufferTime;
        public float fallGravityMultiplier;
        public float lowJumpGravityMultiplier;
    }
}
