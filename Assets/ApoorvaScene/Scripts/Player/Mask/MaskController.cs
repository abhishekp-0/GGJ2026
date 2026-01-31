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

    [Tooltip("Optional fallback mesh if mask.visualPrefab is null")]
    [SerializeField] private GameObject capsule;

    [Header("Unlocks (Jam Simple)")]
    [Min(1)]
    [SerializeField] private int unlockedCount = 2;

    [Header("Debug")]
    [SerializeField] private bool equipOnStart = true;

    [Header("Ball Spawn Rotation")]
    [SerializeField] private Vector3 ballSpawnEuler = new Vector3(0f, 180f, 0f);

    public MaskDefinition Current { get; private set; }
    public int CurrentIndex { get; private set; }

    public event Action<MaskDefinition> OnMaskChanged;

    private void Awake()
    {
        if (visual == null) visual = GetComponentInChildren<MaskVisual>(true);
        if (colliderApplier == null) colliderApplier = GetComponent<MaskColliderApplier>();
        if (movement == null) movement = GetComponent<PlayerMovement>();
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

        // clamp unlocks to library size
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

        // Capsule fallback:
        // If this mask has NO visual prefab => show capsule
        // If it DOES => hide capsule
        if (capsule != null)
            capsule.SetActive(mask.visualPrefab == null);

        // Spawn / apply
        visual?.Apply(mask);
        colliderApplier?.Apply(mask);

        // ✅ Ball facing direction (do after visual spawn)
        if (mask.enableBounce && visual != null && visual.CurrentVisualTransform != null)
        {
            visual.CurrentVisualTransform.localRotation = Quaternion.Euler(ballSpawnEuler);
        }

        if (movement != null)
        {
            // Movement properties
            movement.SetSpeedMultiplier(mask.speedMultiplier);
            movement.SetGravityMultiplier(mask.gravityMultiplier);
            movement.SetJumpProfile(mask.jump);

            // ✅ Animator hook (do after visual spawn)
            // visual.CurrentAnimator should come from MaskVisual.Apply()
            if (visual != null)
                movement.SetAnimator(visual.CurrentAnimator);
            else
                movement.SetAnimator(null);

            // Strategy selection
            if (mask.enableBounce)
                movement.SetStrategy(new BallMovementStrategy());
            else if (mask.enableWallStick)
                movement.SetStrategy(new CubeMovementStrategy());
            else
                movement.SetStrategy(new DefaultMovementStrategy());

            // Roll target (ball rotation visual)
            Transform rollTarget =
                (visual != null && visual.CurrentVisualTransform != null)
                    ? visual.CurrentVisualTransform
                    : (capsule != null ? capsule.transform : null);

            movement.SetBallVisual(rollTarget);

            // Optional: enable/disable ball landing bounce system if you have it in PlayerMovement
            // If you don't have SetBallBounceActive(), remove this line.
            movement.SetBallBounceActive(mask.enableBounce);
        }

        Debug.Log($"[MaskController] Equipped: {mask.displayName} ({mask.id}) index={index}");
        OnMaskChanged?.Invoke(mask);
        return true;
    }

    // ✅ Safer equip by type (prevents order mismatch bugs)
    public bool EquipMask(MaskType type)
    {
        if (library == null || library.masks == null || library.masks.Length == 0)
            return false;

        for (int i = 0; i < library.masks.Length; i++)
        {
            MaskDefinition m = library.masks[i];
            if (m != null && m.id == type)
                return EquipIndex(i);
        }

        Debug.LogWarning($"[MaskController] No mask found for type: {type}");
        return false;
    }
}
