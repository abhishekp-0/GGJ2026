using UnityEngine;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Player Stats")]
    [SerializeField] private int maxHealth = 3;
    public int CurrentHealth { get; private set; }

    public MaskType CurrentMask { get; private set; }
    public GameState CurrentState { get; private set; }

    // Events (Decoupling)
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
        InitializeGame();
    }

    private void InitializeGame()
    {
        CurrentHealth = maxHealth;
        CurrentMask = MaskType.None;
        SetGameState(GameState.Playing);
    }

    /* ---------------- HEALTH ---------------- */

    public void TakeDamage(int amount)
    {
        if (CurrentState != GameState.Playing) return;

        CurrentHealth -= amount;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, maxHealth);
        OnHealthChanged?.Invoke(CurrentHealth);

        if (CurrentHealth <= 0)
        {
            SetGameState(GameState.GameOver);
        }
    }

    public void Heal(int amount)
    {
        if (CurrentState != GameState.Playing) return;

        CurrentHealth = Mathf.Clamp(CurrentHealth + amount, 0, maxHealth);
        OnHealthChanged?.Invoke(CurrentHealth);
    }

    /* ---------------- MASK ---------------- */

    public void EquipMask(MaskType newMask)
    {
        CurrentMask = newMask;
        OnMaskChanged?.Invoke(CurrentMask);
    }

    public void ClearMask()
    {
        EquipMask(MaskType.None);
    }

    /* ---------------- GAME STATE ---------------- */

    public void SetGameState(GameState newState)
    {
        CurrentState = newState;
        OnGameStateChanged?.Invoke(CurrentState);
    }
}
    