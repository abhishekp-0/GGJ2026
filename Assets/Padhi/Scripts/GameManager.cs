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

        CurrentHealth = maxHealth;
        SetGameState(GameState.Playing);
    }

    // ================= MASK API =================

    /// <summary>
    /// Called by UI or PlayerController (debug keys)
    /// </summary>
    public bool RequestEquipMask(MaskType id)
    {
        if (CurrentState != GameState.Playing) return false;
        if (playerMasks == null) return false;

        bool success = playerMasks.EquipIndex((int)id);
        if (!success) return false;

        CurrentMask = id;
        OnMaskChanged?.Invoke(CurrentMask);
        return true;
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