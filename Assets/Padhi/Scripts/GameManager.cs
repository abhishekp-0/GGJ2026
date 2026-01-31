using UnityEngine;
using System;

public sealed class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Player")]
    [SerializeField] private MaskController playerMasks;

    [Header("Health")]
    [SerializeField] private int maxHealth = 3;
    public int CurrentHealth { get; private set; }

    [Header("State")]
    public GameState CurrentState { get; private set; }
    public MaskType CurrentMask { get; private set; } = MaskType.None;

    // Events
    public Action<int> OnHealthChanged;
    public Action<MaskType> OnMaskChanged;
    public Action<GameState> OnGameStateChanged;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (playerMasks == null)
            playerMasks = FindFirstObjectByType<MaskController>();

        if (playerMasks != null)
        {
            // ✅ keep GameManager synced with real equipped mask
            playerMasks.OnMaskChanged += HandleMaskChanged;

            // initialize CurrentMask if player already has a mask equipped
            if (playerMasks.Current != null)
                CurrentMask = playerMasks.Current.id;
        }

        CurrentHealth = maxHealth;
        SetGameState(GameState.Playing);
    }

    private void OnDestroy()
    {
        if (playerMasks != null)
            playerMasks.OnMaskChanged -= HandleMaskChanged;
    }

    private void HandleMaskChanged(MaskDefinition mask)
    {
        CurrentMask = (mask != null) ? mask.id : MaskType.None;
        OnMaskChanged?.Invoke(CurrentMask);
    }

    // ================= MASK API =================

    /// <summary>
    /// Called by UI or PlayerController (debug keys)
    /// </summary>
    public bool RequestEquipMask(MaskType id)
    {
        if (CurrentState != GameState.Playing) return false;
        if (playerMasks == null) return false;

        // ✅ safer: equip by MaskType, not by index
        bool success = playerMasks.EquipMask(id); // you add this method in MaskController
        return success;
    }

    // ================= HEALTH =================

    public void TakeDamage(int amount)
    {
        if (CurrentState != GameState.Playing) return;

        CurrentHealth = Mathf.Clamp(CurrentHealth - amount, 0, maxHealth);
        OnHealthChanged?.Invoke(CurrentHealth);

        if (CurrentHealth <= 0)
            SetGameState(GameState.GameOver);
    }

    // ================= STATE =================

    public void SetGameState(GameState state)
    {
        CurrentState = state;
        OnGameStateChanged?.Invoke(CurrentState);
    }
}
