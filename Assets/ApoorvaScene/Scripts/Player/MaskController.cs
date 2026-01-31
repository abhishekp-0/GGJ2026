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
    [SerializeField] private GameObject capsule;

    [Header("Unlocks (Jam Simple)")]
    [SerializeField] private int unlockedCount = 1;

    [Header("Debug")]
    [SerializeField] private bool equipOnStart = false;

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

    public void UnlockNext()
    {
        unlockedCount = Mathf.Clamp(unlockedCount + 1, 1, library != null && library.masks != null ? library.masks.Length : 4);
    }

    public bool EquipIndex(int index)
    {
        if (library == null) return false;
        if (library.masks == null || library.masks.Length == 0) return false;

        index = Mathf.Clamp(index, 0, library.masks.Length - 1);
        if (index >= unlockedCount) return false;

        MaskDefinition mask = library.GetByIndex(index);
        if (mask == null) return false;

        // Disable capsule when swapping
        DisableCapsuleChildren();

        Current = mask;
        CurrentIndex = index;

        if (visual != null) visual.Apply(mask);
        if (colliderApplier != null) colliderApplier.Apply(mask);

        if (movement != null)
        {
            movement.SetSpeedMultiplier(mask.speedMultiplier);
            movement.SetGravityMultiplier(mask.gravityMultiplier);
            movement.SetJumpProfile(mask.jump);   // ✅ passive jump behavior changes here
            movement.SetWallStickEnabled(mask.enableWallStick);
        }

        Debug.Log($"Equipped Mask: {mask.displayName} ({mask.id})");
        OnMaskChanged?.Invoke(mask);
        return true;
    }

    private void DisableCapsuleChildren()
    {
        if (capsule == null)
        {
            Debug.LogWarning("Capsule is not assigned!");
            return;
        }

        capsule.SetActive(false);
        Debug.Log($"Disabled: {capsule.name}");
    }

    public bool EquipNext()
    {
        return EquipIndex((CurrentIndex + 1) % Mathf.Max(1, unlockedCount));
    }

    public bool EquipPrev()
    {
        int count = Mathf.Max(1, unlockedCount);
        int next = (CurrentIndex - 1 + count) % count;
        return EquipIndex(next);
    }
}