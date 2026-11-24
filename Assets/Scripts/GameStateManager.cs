using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Keeps player progress, boat health, and inventory across scenes.
/// Uses a simple DontDestroyOnLoad singleton pattern without duplicates.
/// </summary>
public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    [Header("Progression")]
    public int currentIslandIndex = 0; // 0 = Discovery, 1 = Viking, 2 = Final Treasure
    public bool returningFromIsland = false;
    public int[] oceanRoundsCompletedForIsland = new int[3];
    public bool[] completedIslandSpecialChest = new bool[3];

    [Header("Boat Health")]
    public int maxBoatHealth = 100;
    public int boatHealth = 100;

    [Header("Inventory")]
    public int coins = 0;
    public int planks = 0;
    public int potions = 0;
    public bool hasMagicCompass = false;
    public bool hasGoldenCrown = false;
    public bool[] hasIslandKey = new bool[3];
    public List<string> openedChestIds = new List<string>();

    // Approved boat repair values (Master Prompt Section 22)
    public const int PlankHealAmount = 10;
    public const int PotionHealAmount = 25;
    public const int CoinRepairCost = 10;
    public const int CoinRepairHealAmount = 20;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureArraySizes();
    }

    /// <summary>
    /// Returns the active GameStateManager or creates one if starting directly from a scene.
    /// </summary>
    public static GameStateManager EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        GameStateManager existing = FindFirstObjectByType<GameStateManager>();
        if (existing != null)
        {
            Instance = existing;
            return Instance;
        }

        GameObject managerObject = new GameObject("GameStateManager");
        Instance = managerObject.AddComponent<GameStateManager>();
        return Instance;
    }

    private void EnsureArraySizes()
    {
        if (oceanRoundsCompletedForIsland == null || oceanRoundsCompletedForIsland.Length != 3)
            oceanRoundsCompletedForIsland = new int[3];

        if (completedIslandSpecialChest == null || completedIslandSpecialChest.Length != 3)
            completedIslandSpecialChest = new bool[3];

        if (hasIslandKey == null || hasIslandKey.Length != 3)
            hasIslandKey = new bool[3];
    }

    /// <summary>
    /// Resets all health, progression, keys, and inventory for a fresh New Game.
    /// </summary>
    public void StartNewGame()
    {
        EnsureArraySizes();

        currentIslandIndex = 0;
        returningFromIsland = false;
        boatHealth = maxBoatHealth;
        coins = 0;
        planks = 0;
        potions = 0;
        hasMagicCompass = false;
        hasGoldenCrown = false;

        for (int i = 0; i < 3; i++)
        {
            oceanRoundsCompletedForIsland[i] = 0;
            completedIslandSpecialChest[i] = false;
            hasIslandKey[i] = false;
        }

        if (openedChestIds == null)
            openedChestIds = new List<string>();
        else
            openedChestIds.Clear();
    }

    public void SetBoatHealth(int value)
    {
        boatHealth = Mathf.Clamp(value, 0, maxBoatHealth);
    }

    public int DamageBoat(int amount)
    {
        if (amount <= 0)
            return boatHealth;

        boatHealth = Mathf.Clamp(boatHealth - amount, 0, maxBoatHealth);
        return boatHealth;
    }

    /// <summary>
    /// Restores boat health if alive and below max. Returns actual HP restored.
    /// </summary>
    public int HealBoat(int amount)
    {
        if (amount <= 0 || boatHealth <= 0 || boatHealth >= maxBoatHealth)
            return 0;

        int previousHealth = boatHealth;
        boatHealth = Mathf.Clamp(boatHealth + amount, 0, maxBoatHealth);
        return boatHealth - previousHealth;
    }

    public void MarkOceanRoundCompleted(int islandIndex)
    {
        EnsureArraySizes();

        if (islandIndex < 0 || islandIndex >= 3)
            return;

        oceanRoundsCompletedForIsland[islandIndex]++;
    }

    public bool HasCompletedOceanRound(int islandIndex, int requiredRounds = 1)
    {
        EnsureArraySizes();

        if (islandIndex < 0 || islandIndex >= 3)
            return false;

        return oceanRoundsCompletedForIsland[islandIndex] >= requiredRounds;
    }

    public string GetIslandDisplayName(int islandIndex)
    {
        switch (islandIndex)
        {
            case 1: return "Viking Island";
            case 2: return "Final Treasure Island";
            default: return "Discovery Island";
        }
    }

    public string GetIslandSceneName(int islandIndex)
    {
        switch (islandIndex)
        {
            case 1: return "VikingIslandScene";
            case 2: return "FinalIslandScene";
            default: return "IslandScene";
        }
    }

    public bool TryAdvanceToNextIsland()
    {
        EnsureArraySizes();
        if (currentIslandIndex < 0 || currentIslandIndex >= 2)
            return false;

        if (!completedIslandSpecialChest[currentIslandIndex])
            return false;

        currentIslandIndex++;
        return true;
    }

    public bool IsChestOpened(string chestId)
    {
        if (string.IsNullOrEmpty(chestId) || openedChestIds == null)
            return false;

        return openedChestIds.Contains(chestId);
    }

    public void MarkChestOpened(string chestId)
    {
        if (string.IsNullOrEmpty(chestId))
            return;

        if (openedChestIds == null)
            openedChestIds = new List<string>();

        if (!openedChestIds.Contains(chestId))
            openedChestIds.Add(chestId);
    }
}
