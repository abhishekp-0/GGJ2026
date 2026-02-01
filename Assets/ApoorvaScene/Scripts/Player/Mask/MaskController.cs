using System;
using UnityEngine;

public sealed class MaskController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private MaskLibrary library;
    [SerializeField] private int startMaskIndex = 0;

    [Header("Refs")]
    [SerializeField] private MaskVisual visual;
    [SerializeField] private MaskColliderApplier colliderApplier;
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private PlayerAnimationDriver animDriver;

    [Tooltip("Optional fallback mesh if mask.visualPrefab is null")]
    [SerializeField] private GameObject capsule;

    [Header("Unlocks (Jam Simple)")]
    [Min(1)]
    [SerializeField] private int unlockedCount = 2;

    [Header("Debug")]
    [SerializeField] private bool equipOnStart = true;

    [Header("Ball Spawn Rotation")]
    [SerializeField] private Vector3 ballSpawnEuler = new Vector3(0f, 180f, 0f);

    [Header("Rock Spawn Transform")]
    [SerializeField] private Vector3 rockSpawnEuler = Vector3.zero;
    [SerializeField] private Vector3 rockSpawnScale = Vector3.one;

    public MaskDefinition Current { get; private set; }
    public int CurrentIndex { get; private set; }

    public event Action<MaskDefinition> OnMaskChanged;

    private IMovementStrategy defaultStrategy;
    private IMovementStrategy ballStrategy;
    private IMovementStrategy cubeStrategy;
    private IMovementStrategy rockStrategy;

    private void Awake()
    {
        if (visual == null) visual = GetComponentInChildren<MaskVisual>(true);
        if (colliderApplier == null) colliderApplier = GetComponent<MaskColliderApplier>();
        if (movement == null) movement = GetComponent<PlayerMovement>();
        if (animDriver == null) animDriver = GetComponent<PlayerAnimationDriver>();

        defaultStrategy = new DefaultMovementStrategy();
        ballStrategy = new BallMovementStrategy();
        cubeStrategy = new CubeMovementStrategy();
        rockStrategy = new RockMovementStrategy();
    }

    private void Start()
    {
        if (equipOnStart)
            EquipIndex(startMaskIndex);
    }

    public bool EquipIndex(int index)
    {
        if (library == null || library.masks == null || library.masks.Length == 0)
        {
            Debug.LogWarning("[MaskController] Library or masks array is missing/empty.");
            return false;
        }

        unlockedCount = Mathf.Clamp(unlockedCount, 1, library.masks.Length);

        index = Mathf.Clamp(index, 0, library.masks.Length - 1);
        if (index >= unlockedCount)
        {
            Debug.LogWarning($"[MaskController] Mask index {index} is locked. unlockedCount={unlockedCount}");
            return false;
        }

        MaskDefinition mask = library.GetByIndex(index);
        if (mask == null)
        {
            Debug.LogWarning($"[MaskController] Mask at index {index} is NULL.");
            return false;
        }

        Current = mask;
        CurrentIndex = index;

        // Capsule fallback
        if (capsule != null)
            capsule.SetActive(mask.visualPrefab == null);

        // Spawn / apply
        visual?.Apply(mask);
        colliderApplier?.Apply(mask);

        // Animation wiring (driver owns animator)
        if (animDriver != null)
            animDriver.SetAnimator(visual != null ? visual.CurrentAnimator : null);

        // Ball facing direction after visual spawn
        if (mask.enableBounce && visual != null && visual.CurrentVisualTransform != null)
        {
            visual.CurrentVisualTransform.localRotation = Quaternion.Euler(ballSpawnEuler);
        }

        // ✅ Rock rotation + scale after visual spawn
        if (mask.enableSmash && visual != null && visual.CurrentVisualTransform != null)
        {
            visual.CurrentVisualTransform.localRotation = Quaternion.Euler(rockSpawnEuler);
            visual.CurrentVisualTransform.localScale = rockSpawnScale;
        }

        if (movement != null)
        {
            movement.SetSpeedMultiplier(mask.speedMultiplier);
            movement.SetGravityMultiplier(mask.gravityMultiplier);
            movement.SetJumpProfile(mask.jump);

            // Strategy selection (Ball > Cube > Rock > Default)
            if (mask.enableBounce)
                movement.SetStrategy(ballStrategy);
            else if (mask.enableWallStick)
                movement.SetStrategy(cubeStrategy);
            else if (mask.enableSmash)
                movement.SetStrategy(rockStrategy);
            else
                movement.SetStrategy(defaultStrategy);

            // Roll target (ball visual rotation)
            Transform rollTarget =
                (visual != null && visual.CurrentVisualTransform != null)
                    ? visual.CurrentVisualTransform
                    : (capsule != null ? capsule.transform : null);

            movement.SetBallVisual(rollTarget);

            // Mode toggles
            movement.SetBallBounceActive(mask.enableBounce);
            movement.SetRockMode(mask.enableSmash);
            movement.SetRockSmashActive(mask.enableSmash);
        }

        Debug.Log($"[MaskController] Equipped: {mask.displayName} ({mask.id}) index={index}");
        OnMaskChanged?.Invoke(mask);
        return true;
    }

    public bool EquipMask(MaskType type)
    {
        if (library == null || library.masks == null) return false;

        for (int i = 0; i < library.masks.Length; i++)
        {
            var m = library.masks[i];
            if (m != null && m.id == type)
                return EquipIndex(i);
        }

        Debug.LogWarning($"[MaskController] No mask found for type: {type}");
        return false;
    }
}
