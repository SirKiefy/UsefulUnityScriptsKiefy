using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UsefulScripts.AnimeEldenRingJRPG
{
    #region Enums

    /// <summary>
    /// Defines the type of rune the player can acquire.
    /// </summary>
    public enum RuneType
    {
        Common,             // Standard runes from enemies
        Boss,               // Great runes from bosses
        Ancient,            // Found in the world, rare
        Crafted,            // Created via rune inscription
        Divine,             // Endgame currency from divine creatures
        Corrupted           // Dropped in abyss areas, risky to use
    }

    /// <summary>
    /// Defines the stat that can be leveled using runes.
    /// </summary>
    public enum LevelUpStat
    {
        Vigor,              // Increases max HP
        Mind,               // Increases max MP
        Endurance,          // Increases stamina and equip load
        Strength,           // Physical attack power
        Dexterity,          // Speed, critical chance
        Intelligence,       // Magic attack power
        Faith,              // Healing, support ability power
        Arcane,             // Luck, item find, special effects
        Spirit              // Bond power with tamed creatures
    }

    /// <summary>
    /// Defines the category of rune-purchasable items.
    /// </summary>
    public enum RuneShopCategory
    {
        Consumables,
        Equipment,
        Materials,
        Recipes,
        CreatureItems,
        KeyItems,
        Upgrades
    }

    #endregion

    #region Data Classes

    /// <summary>
    /// Represents a rune drop from an enemy or world source.
    /// </summary>
    [Serializable]
    public class RuneDrop
    {
        public RuneType runeType;
        public int amount;
        public string sourceId;
        public string sourceName;

        public RuneDrop(RuneType type, int amount, string sourceId, string sourceName)
        {
            runeType = type;
            this.amount = amount;
            this.sourceId = sourceId;
            this.sourceName = sourceName;
        }
    }

    /// <summary>
    /// Represents the cost of leveling up a stat.
    /// </summary>
    [Serializable]
    public class LevelUpCost
    {
        public LevelUpStat stat;
        public int currentLevel;
        public int runeCost;

        /// <summary>
        /// Calculates the rune cost for leveling a stat from a given level.
        /// Uses a scaling formula inspired by Elden Ring's leveling cost curve.
        /// </summary>
        public static int CalculateCost(int currentStatLevel, float costScaling = 1.15f, int baseCost = 100)
        {
            return Mathf.RoundToInt(baseCost * Mathf.Pow(costScaling, currentStatLevel - 1));
        }
    }

    /// <summary>
    /// Represents an item for sale at a rune shop.
    /// </summary>
    [Serializable]
    public class RuneShopItem
    {
        public string itemId;
        public string itemName;
        [TextArea(1, 3)]
        public string description;
        public Sprite icon;
        public RuneShopCategory category;
        public int runeCost;
        public int stock = -1;          // -1 = infinite
        public int currentStock;
        public bool isAvailable = true;
        public string requiredBossDefeatId;
        public int requiredPlayerLevel;

        public bool CanPurchase(int playerRunes, int playerLevel, List<string> defeatedBosses)
        {
            if (!isAvailable) return false;
            if (stock >= 0 && currentStock <= 0) return false;
            if (playerRunes < runeCost) return false;
            if (playerLevel < requiredPlayerLevel) return false;
            if (!string.IsNullOrEmpty(requiredBossDefeatId) && !defeatedBosses.Contains(requiredBossDefeatId)) return false;
            return true;
        }
    }

    /// <summary>
    /// Represents the player's rune-based stat allocation.
    /// </summary>
    [Serializable]
    public class StatAllocation
    {
        public LevelUpStat stat;
        public int level = 1;
        public int timesLeveled;

        public float GetBonusValue()
        {
            return stat switch
            {
                LevelUpStat.Vigor => level * 15f,           // +15 HP per level
                LevelUpStat.Mind => level * 8f,             // +8 MP per level
                LevelUpStat.Endurance => level * 5f,        // +5 Stamina per level
                LevelUpStat.Strength => level * 3f,         // +3 ATK per level
                LevelUpStat.Dexterity => level * 2f,        // +2 SPD per level
                LevelUpStat.Intelligence => level * 3f,     // +3 MATK per level
                LevelUpStat.Faith => level * 2.5f,          // +2.5 Heal power per level
                LevelUpStat.Arcane => level * 1.5f,         // +1.5 Luck per level
                LevelUpStat.Spirit => level * 2f,           // +2 Bond power per level
                _ => 0
            };
        }
    }

    /// <summary>
    /// Represents the player's held runes that are at risk upon death.
    /// </summary>
    [Serializable]
    public class RuneStash
    {
        public int currentRunes;
        public int lifetimeRunesEarned;
        public int lifetimeRunesSpent;
        public int lifetimeRunesLost;

        [Header("Dropped Runes")]
        public bool hasDroppedRunes;
        public int droppedRuneAmount;
        public Vector3 droppedRunePosition;
        public string droppedRuneRegionId;
    }

    #endregion

    #region ScriptableObjects

    /// <summary>
    /// Configuration for the rune system.
    /// </summary>
    [CreateAssetMenu(fileName = "RuneConfig", menuName = "UsefulScripts/AnimeEldenRingJRPG/Rune Config")]
    public class RuneConfig : ScriptableObject
    {
        [Header("Leveling")]
        public int baseLevelUpCost = 100;
        public float levelUpCostScaling = 1.15f;
        public int maxStatLevel = 99;
        public int softCapLevel = 40;           // Diminishing returns after this
        public int hardCapLevel = 60;           // Very diminishing returns after this

        [Header("Death Penalty")]
        public bool dropRunesOnDeath = true;
        public float runeRecoveryTimeLimit = 0f;    // 0 = no time limit
        public bool destroyOnSecondDeath = true;

        [Header("Corruption")]
        public float corruptedRuneRiskChance = 0.1f;
        public float corruptedRuneLossPercent = 0.5f;
        public float corruptedRuneBonusPercent = 2f;

        [Header("Shop")]
        public List<RuneShopItem> shopItems = new List<RuneShopItem>();
    }

    #endregion

    /// <summary>
    /// Manages the Elden Ring–style rune currency system for leveling up stats,
    /// purchasing items, and risk/reward mechanics on player death.
    /// </summary>
    public class RuneSystem : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private RuneConfig config;

        [Header("Player Runes")]
        [SerializeField] private RuneStash runeStash = new RuneStash();

        [Header("Stats")]
        [SerializeField] private List<StatAllocation> statAllocations = new List<StatAllocation>();
        [SerializeField] private int playerLevel = 1;

        // Events
        public event Action<int, int> OnRunesChanged;           // oldAmount, newAmount
        public event Action<RuneDrop> OnRunesGained;
        public event Action<int> OnRunesSpent;
        public event Action<int> OnRunesLost;
        public event Action<LevelUpStat, int> OnStatLeveledUp;
        public event Action<int> OnPlayerLevelUp;
        public event Action<int, Vector3> OnRunesDropped;
        public event Action<int> OnRunesRecovered;
        public event Action<RuneShopItem> OnItemPurchased;

        // Properties
        public int CurrentRunes => runeStash.currentRunes;
        public int PlayerLevel => playerLevel;
        public int LifetimeRunesEarned => runeStash.lifetimeRunesEarned;
        public bool HasDroppedRunes => runeStash.hasDroppedRunes;
        public Vector3 DroppedRunePosition => runeStash.droppedRunePosition;

        private void Awake()
        {
            InitializeStatAllocations();
        }

        #region Initialization

        private void InitializeStatAllocations()
        {
            if (statAllocations.Count > 0) return;

            foreach (LevelUpStat stat in Enum.GetValues(typeof(LevelUpStat)))
            {
                statAllocations.Add(new StatAllocation
                {
                    stat = stat,
                    level = 1,
                    timesLeveled = 0
                });
            }
        }

        #endregion

        #region Rune Acquisition

        /// <summary>
        /// Adds runes from an enemy defeat or world pickup.
        /// </summary>
        public void GainRunes(RuneDrop drop)
        {
            if (drop == null) return;

            int amount = drop.amount;

            // Handle corrupted runes: risk/reward — on failure, lose a portion of held runes
            // and forfeit the drop; on success, gain bonus runes
            if (drop.runeType == RuneType.Corrupted && config != null)
            {
                float riskRoll = UnityEngine.Random.Range(0f, 1f);
                if (riskRoll < config.corruptedRuneRiskChance)
                {
                    int loss = Mathf.RoundToInt(runeStash.currentRunes * config.corruptedRuneLossPercent);
                    runeStash.currentRunes -= loss;
                    runeStash.lifetimeRunesLost += loss;
                    OnRunesLost?.Invoke(loss);
                    return;
                }
                else
                {
                    amount = Mathf.RoundToInt(amount * config.corruptedRuneBonusPercent);
                }
            }

            int oldAmount = runeStash.currentRunes;
            runeStash.currentRunes += amount;
            runeStash.lifetimeRunesEarned += amount;

            OnRunesGained?.Invoke(drop);
            OnRunesChanged?.Invoke(oldAmount, runeStash.currentRunes);
        }

        /// <summary>
        /// Adds a flat amount of runes (for rewards, etc.).
        /// </summary>
        public void AddRunes(int amount)
        {
            if (amount <= 0) return;

            int oldAmount = runeStash.currentRunes;
            runeStash.currentRunes += amount;
            runeStash.lifetimeRunesEarned += amount;

            OnRunesChanged?.Invoke(oldAmount, runeStash.currentRunes);
        }

        #endregion

        #region Leveling Up

        /// <summary>
        /// Levels up a stat by spending runes.
        /// </summary>
        public bool LevelUpStat(LevelUpStat stat)
        {
            var allocation = statAllocations.FirstOrDefault(s => s.stat == stat);
            if (allocation == null) return false;

            int maxLevel = config != null ? config.maxStatLevel : 99;
            if (allocation.level >= maxLevel) return false;

            int cost = GetLevelUpCost(stat);
            if (runeStash.currentRunes < cost) return false;

            int oldRunes = runeStash.currentRunes;
            runeStash.currentRunes -= cost;
            runeStash.lifetimeRunesSpent += cost;

            allocation.level++;
            allocation.timesLeveled++;
            playerLevel++;

            OnRunesSpent?.Invoke(cost);
            OnRunesChanged?.Invoke(oldRunes, runeStash.currentRunes);
            OnStatLeveledUp?.Invoke(stat, allocation.level);
            OnPlayerLevelUp?.Invoke(playerLevel);

            return true;
        }

        /// <summary>
        /// Gets the cost to level up a specific stat.
        /// </summary>
        public int GetLevelUpCost(LevelUpStat stat)
        {
            var allocation = statAllocations.FirstOrDefault(s => s.stat == stat);
            if (allocation == null) return int.MaxValue;

            int baseCost = config != null ? config.baseLevelUpCost : 100;
            float scaling = config != null ? config.levelUpCostScaling : 1.15f;

            int cost = LevelUpCost.CalculateCost(allocation.level, scaling, baseCost);

            // Apply soft/hard cap scaling
            int softCap = config != null ? config.softCapLevel : 40;
            int hardCap = config != null ? config.hardCapLevel : 60;

            if (allocation.level >= hardCap)
            {
                cost = Mathf.RoundToInt(cost * 3f);
            }
            else if (allocation.level >= softCap)
            {
                cost = Mathf.RoundToInt(cost * 1.5f);
            }

            return cost;
        }

        /// <summary>
        /// Gets the current level of a stat.
        /// </summary>
        public int GetStatLevel(LevelUpStat stat)
        {
            var allocation = statAllocations.FirstOrDefault(s => s.stat == stat);
            return allocation?.level ?? 1;
        }

        /// <summary>
        /// Gets the bonus value provided by a stat.
        /// </summary>
        public float GetStatBonus(LevelUpStat stat)
        {
            var allocation = statAllocations.FirstOrDefault(s => s.stat == stat);
            return allocation?.GetBonusValue() ?? 0;
        }

        /// <summary>
        /// Checks if the player can afford to level a stat.
        /// </summary>
        public bool CanLevelUp(LevelUpStat stat)
        {
            var allocation = statAllocations.FirstOrDefault(s => s.stat == stat);
            if (allocation == null) return false;

            int maxLevel = config != null ? config.maxStatLevel : 99;
            if (allocation.level >= maxLevel) return false;

            return runeStash.currentRunes >= GetLevelUpCost(stat);
        }

        #endregion

        #region Death & Recovery

        /// <summary>
        /// Called when the player dies. Drops runes at the death location.
        /// </summary>
        public void OnPlayerDeath(Vector3 deathPosition, string regionId)
        {
            if (config != null && !config.dropRunesOnDeath) return;

            // If already has dropped runes, those are lost
            if (runeStash.hasDroppedRunes && config != null && config.destroyOnSecondDeath)
            {
                runeStash.lifetimeRunesLost += runeStash.droppedRuneAmount;
                OnRunesLost?.Invoke(runeStash.droppedRuneAmount);
                runeStash.droppedRuneAmount = 0;
            }

            // Drop current runes
            if (runeStash.currentRunes > 0)
            {
                runeStash.hasDroppedRunes = true;
                runeStash.droppedRuneAmount = runeStash.currentRunes;
                runeStash.droppedRunePosition = deathPosition;
                runeStash.droppedRuneRegionId = regionId;

                int oldAmount = runeStash.currentRunes;
                runeStash.currentRunes = 0;

                OnRunesDropped?.Invoke(runeStash.droppedRuneAmount, deathPosition);
                OnRunesChanged?.Invoke(oldAmount, 0);
            }
        }

        /// <summary>
        /// Called when the player reaches their dropped runes.
        /// </summary>
        public bool RecoverDroppedRunes()
        {
            if (!runeStash.hasDroppedRunes) return false;

            int recovered = runeStash.droppedRuneAmount;
            int oldAmount = runeStash.currentRunes;

            runeStash.currentRunes += recovered;
            runeStash.hasDroppedRunes = false;
            runeStash.droppedRuneAmount = 0;

            OnRunesRecovered?.Invoke(recovered);
            OnRunesChanged?.Invoke(oldAmount, runeStash.currentRunes);
            return true;
        }

        #endregion

        #region Shop

        /// <summary>
        /// Purchases an item from the rune shop.
        /// </summary>
        public bool PurchaseItem(string itemId, List<string> defeatedBosses)
        {
            if (config == null) return false;

            var item = config.shopItems.FirstOrDefault(i => i.itemId == itemId);
            if (item == null) return false;

            if (!item.CanPurchase(runeStash.currentRunes, playerLevel, defeatedBosses)) return false;

            int oldRunes = runeStash.currentRunes;
            runeStash.currentRunes -= item.runeCost;
            runeStash.lifetimeRunesSpent += item.runeCost;

            if (item.stock > 0)
            {
                item.currentStock--;
            }

            OnRunesSpent?.Invoke(item.runeCost);
            OnRunesChanged?.Invoke(oldRunes, runeStash.currentRunes);
            OnItemPurchased?.Invoke(item);
            return true;
        }

        /// <summary>
        /// Gets all available shop items for a category.
        /// </summary>
        public List<RuneShopItem> GetShopItems(RuneShopCategory category, List<string> defeatedBosses)
        {
            if (config == null) return new List<RuneShopItem>();

            return config.shopItems
                .Where(i => i.category == category && i.isAvailable)
                .Where(i => string.IsNullOrEmpty(i.requiredBossDefeatId) || defeatedBosses.Contains(i.requiredBossDefeatId))
                .ToList();
        }

        #endregion

        #region Save Data

        [Serializable]
        public class RuneSaveData
        {
            public int currentRunes;
            public int lifetimeEarned;
            public int lifetimeSpent;
            public int lifetimeLost;
            public bool hasDroppedRunes;
            public int droppedAmount;
            public float droppedX, droppedY, droppedZ;
            public string droppedRegionId;
            public int playerLevel;
            public List<StatSave> stats = new List<StatSave>();
        }

        [Serializable]
        public class StatSave
        {
            public int stat;
            public int level;
            public int timesLeveled;
        }

        public RuneSaveData GetSaveData()
        {
            var data = new RuneSaveData
            {
                currentRunes = runeStash.currentRunes,
                lifetimeEarned = runeStash.lifetimeRunesEarned,
                lifetimeSpent = runeStash.lifetimeRunesSpent,
                lifetimeLost = runeStash.lifetimeRunesLost,
                hasDroppedRunes = runeStash.hasDroppedRunes,
                droppedAmount = runeStash.droppedRuneAmount,
                droppedX = runeStash.droppedRunePosition.x,
                droppedY = runeStash.droppedRunePosition.y,
                droppedZ = runeStash.droppedRunePosition.z,
                droppedRegionId = runeStash.droppedRuneRegionId,
                playerLevel = playerLevel
            };

            foreach (var stat in statAllocations)
            {
                data.stats.Add(new StatSave
                {
                    stat = (int)stat.stat,
                    level = stat.level,
                    timesLeveled = stat.timesLeveled
                });
            }

            return data;
        }

        public void LoadSaveData(RuneSaveData data)
        {
            if (data == null) return;

            runeStash.currentRunes = data.currentRunes;
            runeStash.lifetimeRunesEarned = data.lifetimeEarned;
            runeStash.lifetimeRunesSpent = data.lifetimeSpent;
            runeStash.lifetimeRunesLost = data.lifetimeLost;
            runeStash.hasDroppedRunes = data.hasDroppedRunes;
            runeStash.droppedRuneAmount = data.droppedAmount;
            runeStash.droppedRunePosition = new Vector3(data.droppedX, data.droppedY, data.droppedZ);
            runeStash.droppedRuneRegionId = data.droppedRegionId;
            playerLevel = data.playerLevel;

            foreach (var statSave in data.stats)
            {
                var allocation = statAllocations.FirstOrDefault(s => (int)s.stat == statSave.stat);
                if (allocation != null)
                {
                    allocation.level = statSave.level;
                    allocation.timesLeveled = statSave.timesLeveled;
                }
            }
        }

        #endregion
    }
}
