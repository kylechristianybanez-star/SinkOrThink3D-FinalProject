using System;
using System.Collections.Generic;
using UnityEngine;

public enum ChestRewardType
{
    Coins,
    Planks,
    Potions,
    GoldenKey,
    MagicCompass,
    GoldenCrown
}

[Serializable]
public class ChestRewardAssignment
{
    public string chestId;
    public int islandIndex;
    public ChestRewardType rewardType;
    public int amount;

    public ChestRewardAssignment(string chestId, int islandIndex, ChestRewardType rewardType, int amount)
    {
        this.chestId = chestId;
        this.islandIndex = islandIndex;
        this.rewardType = rewardType;
        this.amount = amount;
    }
}

/// <summary>
/// Keeps player progress, boat health, randomized chest rewards, and inventory across scenes.
/// Uses a simple DontDestroyOnLoad singleton pattern without duplicates.
/// </summary>
public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }
    public static event Action OnStateChanged;

    [Header("Progression")]
    public int currentIslandIndex = 0; // 0 = Discovery, 1 = Viking, 2 = Final Treasure
    public bool returningFromIsland = false;
    public int[] oceanRoundsCompletedForIsland = new int[3];
    public bool[] completedIslandSpecialChest = new bool[3];

    [Header("Boat Health")]
    public int maxBoatHealth = 100;
    public int boatHealth = 100;

    [Header("Inventory & Chest Rewards")]
    public int coins = 0;
    public int planks = 0;
    public int potions = 0;
    public bool hasMagicCompass = false;
    public bool hasGoldenCrown = false;
    public bool[] hasIslandKey = new bool[3];
    public bool[] islandRewardsAssigned = new bool[3];
    public List<string> openedChestIds = new List<string>();
    public List<ChestRewardAssignment> chestRewards = new List<ChestRewardAssignment>();

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

        if (islandRewardsAssigned == null || islandRewardsAssigned.Length != 3)
            islandRewardsAssigned = new bool[3];

        if (openedChestIds == null)
            openedChestIds = new List<string>();

        if (chestRewards == null)
            chestRewards = new List<ChestRewardAssignment>();
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
            islandRewardsAssigned[i] = false;
        }

        openedChestIds.Clear();
        chestRewards.Clear();
        NotifyStateChanged();
    }

    public void NotifyStateChanged()
    {
        if (OnStateChanged != null)
            OnStateChanged.Invoke();
    }

    public void SetBoatHealth(int value)
    {
        boatHealth = Mathf.Clamp(value, 0, maxBoatHealth);
        NotifyStateChanged();
    }

    public int DamageBoat(int amount)
    {
        if (amount <= 0)
            return boatHealth;

        boatHealth = Mathf.Clamp(boatHealth - amount, 0, maxBoatHealth);
        NotifyStateChanged();
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
        NotifyStateChanged();
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
        {
            openedChestIds.Add(chestId);
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Assigns exactly 1 Golden Key and randomized Coins/Planks/Potions once per island.
    /// Never rerolls once assigned for that island.
    /// </summary>
    public void EnsureIslandRewardsAssigned(int islandIndex, string[] regularChestIds)
    {
        EnsureArraySizes();

        if (islandIndex < 0 || islandIndex >= 3 || islandRewardsAssigned[islandIndex])
            return;

        if (regularChestIds == null || regularChestIds.Length == 0)
            return;

        int keyChestIndex = UnityEngine.Random.Range(0, regularChestIds.Length);

        for (int i = 0; i < regularChestIds.Length; i++)
        {
            string id = regularChestIds[i];
            if (GetChestReward(id) != null)
                continue;

            if (i == keyChestIndex)
            {
                chestRewards.Add(new ChestRewardAssignment(id, islandIndex, ChestRewardType.GoldenKey, 1));
                continue;
            }

            int poolRoll = UnityEngine.Random.Range(0, 3);
            ChestRewardType rType;
            int amount;

            if (poolRoll == 0)
            {
                rType = ChestRewardType.Coins;
                amount = islandIndex == 0 ? UnityEngine.Random.Range(5, 16)
                    : islandIndex == 1 ? UnityEngine.Random.Range(15, 31)
                    : UnityEngine.Random.Range(25, 51);
            }
            else if (poolRoll == 1)
            {
                rType = ChestRewardType.Planks;
                amount = islandIndex == 0 ? UnityEngine.Random.Range(1, 4)
                    : islandIndex == 1 ? UnityEngine.Random.Range(2, 5)
                    : UnityEngine.Random.Range(3, 6);
            }
            else
            {
                rType = ChestRewardType.Potions;
                amount = islandIndex == 0 ? 1
                    : islandIndex == 1 ? UnityEngine.Random.Range(1, 3)
                    : UnityEngine.Random.Range(2, 4);
            }

            chestRewards.Add(new ChestRewardAssignment(id, islandIndex, rType, amount));
        }

        islandRewardsAssigned[islandIndex] = true;
    }

    public ChestRewardAssignment GetChestReward(string chestId)
    {
        if (string.IsNullOrEmpty(chestId) || chestRewards == null)
            return null;

        for (int i = 0; i < chestRewards.Count; i++)
        {
            if (chestRewards[i].chestId == chestId)
                return chestRewards[i];
        }

        return null;
    }

    public string GrantRegularReward(ChestRewardAssignment assignment)
    {
        if (assignment == null)
        {
            coins += 10;
            NotifyStateChanged();
            return "+10 Coins";
        }

        string label;
        switch (assignment.rewardType)
        {
            case ChestRewardType.GoldenKey:
                EnsureArraySizes();
                if (assignment.islandIndex >= 0 && assignment.islandIndex < 3)
                    hasIslandKey[assignment.islandIndex] = true;
                label = "Golden Key Found! (" + GetIslandDisplayName(assignment.islandIndex) + ")";
                break;

            case ChestRewardType.Planks:
                planks = Mathf.Max(0, planks + assignment.amount);
                label = "+" + assignment.amount + " Wooden Plank" + (assignment.amount > 1 ? "s" : "");
                break;

            case ChestRewardType.Potions:
                potions = Mathf.Max(0, potions + assignment.amount);
                label = "+" + assignment.amount + " Repair Potion" + (assignment.amount > 1 ? "s" : "");
                break;

            default:
                coins = Mathf.Max(0, coins + assignment.amount);
                label = "+" + assignment.amount + " Coins";
                break;
        }

        NotifyStateChanged();
        return label;
    }

    public string GrantSpecialChestReward(int islandIndex)
    {
        EnsureArraySizes();
        if (islandIndex >= 0 && islandIndex < 3)
        {
            hasIslandKey[islandIndex] = false;
            completedIslandSpecialChest[islandIndex] = true;
        }

        string message;
        if (islandIndex == 0)
        {
            coins += 30;
            hasMagicCompass = true;
            message = "Special Chest: +30 Coins & Magic Compass Unlocked!";
        }
        else if (islandIndex == 1)
        {
            coins += 50;
            potions += 2;
            planks += 3;
            message = "Special Chest: +50 Coins, +2 Potions & +3 Planks!";
        }
        else
        {
            coins += 100;
            hasGoldenCrown = true;
            message = "Final Treasure: +100 Coins & Legendary Golden Crown!";
        }

        NotifyStateChanged();
        return message;
    }

    public bool TryUsePlank(out int restoredHp, out string feedback)
    {
        restoredHp = 0;

        if (boatHealth <= 0)
        {
            feedback = "Cannot repair — your boat has already sunk!";
            return false;
        }

        if (planks <= 0)
        {
            feedback = "No Wooden Planks left!";
            return false;
        }

        if (boatHealth >= maxBoatHealth)
        {
            feedback = "Boat health is already full (" + boatHealth + "/" + maxBoatHealth + ").";
            return false;
        }

        planks = Mathf.Max(0, planks - 1);
        restoredHp = HealBoat(PlankHealAmount);
        feedback = "Used 1 Wooden Plank: +" + restoredHp + " Boat HP (" + boatHealth + "/" + maxBoatHealth + ")";
        return true;
    }

    public bool TryUsePotion(out int restoredHp, out string feedback)
    {
        restoredHp = 0;

        if (boatHealth <= 0)
        {
            feedback = "Cannot repair — your boat has already sunk!";
            return false;
        }

        if (potions <= 0)
        {
            feedback = "No Repair Potions left!";
            return false;
        }

        if (boatHealth >= maxBoatHealth)
        {
            feedback = "Boat health is already full (" + boatHealth + "/" + maxBoatHealth + ").";
            return false;
        }

        potions = Mathf.Max(0, potions - 1);
        restoredHp = HealBoat(PotionHealAmount);
        feedback = "Used 1 Repair Potion: +" + restoredHp + " Boat HP (" + boatHealth + "/" + maxBoatHealth + ")";
        return true;
    }

    public bool TryCoinRepair(out int restoredHp, out string feedback)
    {
        restoredHp = 0;

        if (boatHealth <= 0)
        {
            feedback = "Cannot repair — your boat has already sunk!";
            return false;
        }

        if (boatHealth >= maxBoatHealth)
        {
            feedback = "Boat health is already full (" + boatHealth + "/" + maxBoatHealth + ").";
            return false;
        }

        if (coins < CoinRepairCost)
        {
            feedback = "Need " + CoinRepairCost + " Coins to repair (You have " + coins + ").";
            return false;
        }

        coins = Mathf.Max(0, coins - CoinRepairCost);
        restoredHp = HealBoat(CoinRepairHealAmount);
        feedback = "Dock Repair (-" + CoinRepairCost + " Coins): +" + restoredHp + " Boat HP (" + boatHealth + "/" + maxBoatHealth + ")";
        return true;
    }

    public string GetVoyageLabel(int islandIndex)
    {
        switch (islandIndex)
        {
            case 1: return "Viking Voyage (Medium)";
            case 2: return "Final Voyage (Hard)";
            default: return "Discovery Voyage (Easy)";
        }
    }

    public int GetOpenedChestCountForIsland(int islandIndex)
    {
        if (openedChestIds == null)
            return 0;

        string prefix = islandIndex == 1 ? "viking_" : (islandIndex == 2 ? "final_" : "discovery_");
        int count = 0;
        for (int i = 0; i < openedChestIds.Count; i++)
        {
            if (openedChestIds[i] != null && openedChestIds[i].StartsWith(prefix))
                count++;
        }
        return count;
    }

    public string GetCurrentObjectiveText()
    {
        EnsureArraySizes();
        int idx = Mathf.Clamp(currentIslandIndex, 0, 2);

        if (hasGoldenCrown)
            return "Victory! Legendary Golden Crown recovered!";

        if (completedIslandSpecialChest[idx])
        {
            if (idx < 2)
                return "Objective: Board Boat at Dock to Sail to " + GetIslandDisplayName(idx + 1) + "!";
            return "Objective: Victory Achieved!";
        }

        if (hasIslandKey[idx])
            return "Objective: Unlock " + GetIslandDisplayName(idx) + "'s Special Chest with your Golden Key!";

        return "Objective: Explore chests to find " + GetIslandDisplayName(idx) + "'s Golden Key.";
    }
}
