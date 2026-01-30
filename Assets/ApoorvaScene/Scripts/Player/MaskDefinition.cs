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
}
