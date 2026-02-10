using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UsefulScripts.AnimeEldenRingJRPG
{
    #region Enums

    /// <summary>
    /// Defines the crafting discipline available to the player.
    /// </summary>
    public enum CraftingDiscipline
    {
        SoulForging,        // Weapons imbued with creature souls
        RuneInscription,    // Runes and enchantments
        Alchemy,            // Potions, elixirs, bombs
        Cooking,            // Stat-boosting meals
        ArmorSmithing,      // Defensive gear
        Tailoring,          // Light armor, cloaks, accessories
        JewelCrafting,      // Rings, amulets, gems
        SpiritWeaving,      // Spirit-bound items, summon tokens
        Engineering         // Gadgets, traps, tools
    }

    /// <summary>
    /// Defines the quality tier of a crafted item.
    /// </summary>
    public enum CraftQuality
    {
        Crude,          // Failed or very low roll
        Common,         // Standard result
        Fine,           // Above average
        Superior,       // High skill
        Masterwork,     // Near-perfect
        Legendary,      // Perfect + bonus
        Mythic          // Divine-touched, extremely rare
    }

    /// <summary>
    /// Defines the type of crafting material.
    /// </summary>
    public enum MaterialType
    {
        Ore,            // Metals, stones
        Herb,           // Plants, flowers
        Hide,           // Creature leather, pelts
        Essence,        // Elemental essences
        Crystal,        // Gemstones, crystals
        Bone,           // Creature bones, shells
        Fabric,         // Cloth, silk, thread
        Rune,           // Rune fragments, inscriptions
        SoulFragment,   // Dropped by defeated creatures
        DivineShard,    // Boss drops, rare finds
        Catalyst        // Crafting reagents
    }

    /// <summary>
    /// Defines the slot type for equipment crafting.
    /// </summary>
    public enum EquipmentSlot
    {
        Weapon,
        Shield,
        Head,
        Chest,
        Legs,
        Boots,
        Gloves,
        Ring,
        Amulet,
        Cloak,
        Accessory
    }

    #endregion

    #region Data Classes

    /// <summary>
    /// Represents a single crafting material with quantity.
    /// </summary>
    [Serializable]
    public class CraftingMaterialEntry
    {
        public string materialId;
        public string materialName;
        public MaterialType materialType;
        public int quantity;
        public bool isOptional;
        public float qualityBonus;
        public Sprite icon;

        [TextArea(1, 3)]
        public string description;
    }

    /// <summary>
    /// Represents a crafting recipe that can be learned and used.
    /// </summary>
    [Serializable]
    public class CraftingRecipe
    {
        public string recipeId;
        public string recipeName;
        [TextArea(2, 4)]
        public string description;
        public Sprite icon;
        public CraftingDiscipline discipline;
        public int requiredLevel = 1;

        [Header("Materials")]
        public List<CraftingMaterialEntry> requiredMaterials = new List<CraftingMaterialEntry>();
        public List<CraftingMaterialEntry> optionalMaterials = new List<CraftingMaterialEntry>();

        [Header("Output")]
        public string resultItemId;
        public string resultItemName;
        public int resultQuantity = 1;
        public EquipmentSlot equipmentSlot;

        [Header("Crafting")]
        public float craftingTime = 2f;
        public float baseSuccessChance = 0.8f;
        public int expReward = 25;
        public bool requiresCraftingStation;
        public string requiredStationId;

        [Header("Soul Forging")]
        public bool requiresCreatureSoul;
        public CreatureElement requiredElement;
        public int soulPowerRequired;

        [Header("Visuals")]
        public AudioClip craftingSound;
        public GameObject craftingVFX;
    }

    /// <summary>
    /// Represents a crafting station in the world where recipes can be crafted.
    /// </summary>
    [Serializable]
    public class CraftingStation
    {
        public string stationId;
        public string stationName;
        [TextArea(2, 3)]
        public string description;
        public CraftingDiscipline discipline;
        public int stationTier = 1;
        public float qualityBonus;
        public float speedBonus;
        public Vector3 worldPosition;

        public List<CraftingDiscipline> supportedDisciplines = new List<CraftingDiscipline>();

        public bool SupportsDiscipline(CraftingDiscipline disc)
        {
            return discipline == disc || supportedDisciplines.Contains(disc);
        }
    }

    /// <summary>
    /// Represents the result of a crafting attempt.
    /// </summary>
    [Serializable]
    public class CraftingResult
    {
        public bool success;
        public string resultItemId;
        public string resultItemName;
        public int quantity;
        public CraftQuality quality;
        public float qualityRoll;
        public string failureReason;
        public int expGained;
        public List<string> bonusEffects = new List<string>();
    }

    /// <summary>
    /// Tracks player progress in a crafting discipline.
    /// </summary>
    [Serializable]
    public class DisciplineProgress
    {
        public CraftingDiscipline discipline;
        public int level = 1;
        public int currentExp;
        public int expToNextLevel = 100;
        public float expScaling = 1.4f;
        public List<string> learnedRecipeIds = new List<string>();
        public int itemsCrafted;
        public int masterworksCrafted;

        // Events
        public event Action<CraftingDiscipline, int> OnLevelUp;
        public event Action<CraftingDiscipline, string> OnRecipeLearned;

        public int GetExpToNextLevel()
        {
            return Mathf.RoundToInt(expToNextLevel * Mathf.Pow(expScaling, level - 1));
        }

        public void AddExperience(int amount)
        {
            currentExp += amount;
            int required = GetExpToNextLevel();
            while (currentExp >= required)
            {
                currentExp -= required;
                level++;
                required = GetExpToNextLevel();
                OnLevelUp?.Invoke(discipline, level);
            }
        }

        public void LearnRecipe(string recipeId)
        {
            if (!learnedRecipeIds.Contains(recipeId))
            {
                learnedRecipeIds.Add(recipeId);
                OnRecipeLearned?.Invoke(discipline, recipeId);
            }
        }

        public bool KnowsRecipe(string recipeId)
        {
            return learnedRecipeIds.Contains(recipeId);
        }
    }

    /// <summary>
    /// Represents an item in the crafting queue.
    /// </summary>
    [Serializable]
    public class CraftingQueueItem
    {
        public CraftingRecipe recipe;
        public float timeRemaining;
        public float totalTime;
        public CraftingStation station;
        public bool isComplete;
    }

    #endregion

    #region ScriptableObjects

    /// <summary>
    /// Configuration for the anime crafting system.
    /// </summary>
    [CreateAssetMenu(fileName = "CraftingConfig", menuName = "UsefulScripts/AnimeEldenRingJRPG/Crafting Config")]
    public class AnimeCraftingConfig : ScriptableObject
    {
        [Header("General")]
        public int maxQueueSize = 5;
        public float globalCraftingSpeedMultiplier = 1f;

        [Header("Quality")]
        public float criticalCraftChance = 0.05f;
        public float criticalQualityBoost = 2f;
        public float stationTierQualityBonus = 0.1f;

        [Header("Soul Forging")]
        public float soulForgeBonusDamage = 0.15f;
        public float soulForgeElementalChance = 0.5f;

        [Header("Recipes")]
        public List<CraftingRecipe> allRecipes = new List<CraftingRecipe>();

        [Header("Stations")]
        public List<CraftingStation> registeredStations = new List<CraftingStation>();
    }

    #endregion

    /// <summary>
    /// Manages the anime JRPG crafting system with soul forging, rune inscription,
    /// alchemy, cooking, and equipment crafting inspired by Elden Ring's item depth.
    /// </summary>
    public class AnimeCraftingSystem : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private AnimeCraftingConfig config;

        [Header("Player Progress")]
        [SerializeField] private List<DisciplineProgress> disciplineProgress = new List<DisciplineProgress>();

        [Header("Queue")]
        [SerializeField] private List<CraftingQueueItem> craftingQueue = new List<CraftingQueueItem>();

        [Header("Inventory Reference")]
        [SerializeField] private Dictionary<string, int> materialInventory = new Dictionary<string, int>();

        // Events
        public event Action<CraftingResult> OnCraftingComplete;
        public event Action<CraftingRecipe> OnCraftingStarted;
        public event Action<CraftingResult> OnCraftingFailed;
        public event Action<CraftingQueueItem, float> OnCraftingProgress;
        public event Action<CraftingDiscipline, int> OnDisciplineLevelUp;
        public event Action<CraftingDiscipline, string> OnRecipeLearned;
        public event Action<CraftingDiscipline, string> OnRecipeDiscovered;

        // Properties
        public List<DisciplineProgress> AllDisciplines => disciplineProgress;
        public List<CraftingQueueItem> CraftingQueue => craftingQueue;

        private void Awake()
        {
            InitializeDisciplines();
        }

        private void Update()
        {
            UpdateCraftingQueue();
        }

        #region Initialization

        private void InitializeDisciplines()
        {
            if (disciplineProgress.Count > 0) return;

            foreach (CraftingDiscipline discipline in Enum.GetValues(typeof(CraftingDiscipline)))
            {
                disciplineProgress.Add(new DisciplineProgress
                {
                    discipline = discipline,
                    level = 1,
                    currentExp = 0
                });
            }
        }

        #endregion

        #region Crafting

        /// <summary>
        /// Starts crafting a recipe immediately or adds it to the queue.
        /// </summary>
        public bool StartCrafting(string recipeId, string stationId = null)
        {
            var recipe = FindRecipe(recipeId);
            if (recipe == null) return false;

            var progress = GetDisciplineProgress(recipe.discipline);
            if (progress == null || progress.level < recipe.requiredLevel) return false;

            if (!progress.KnowsRecipe(recipeId)) return false;

            if (!HasRequiredMaterials(recipe)) return false;

            if (recipe.requiresCraftingStation && string.IsNullOrEmpty(stationId)) return false;

            CraftingStation station = null;
            if (!string.IsNullOrEmpty(stationId))
            {
                station = FindStation(stationId);
                if (station != null && !station.SupportsDiscipline(recipe.discipline)) return false;
            }

            int maxQueue = config != null ? config.maxQueueSize : 5;
            if (craftingQueue.Count >= maxQueue) return false;

            // Consume materials
            ConsumeMaterials(recipe);

            // Calculate crafting time
            float craftTime = recipe.craftingTime;
            float speedMult = config != null ? config.globalCraftingSpeedMultiplier : 1f;
            if (station != null) craftTime *= (1f - station.speedBonus);
            craftTime /= speedMult;
            craftTime = Mathf.Max(0.1f, craftTime);

            var queueItem = new CraftingQueueItem
            {
                recipe = recipe,
                timeRemaining = craftTime,
                totalTime = craftTime,
                station = station,
                isComplete = false
            };

            craftingQueue.Add(queueItem);
            OnCraftingStarted?.Invoke(recipe);
            return true;
        }

        /// <summary>
        /// Instantly crafts a recipe (for simple recipes or debug).
        /// </summary>
        public CraftingResult InstantCraft(string recipeId, string stationId = null)
        {
            var recipe = FindRecipe(recipeId);
            if (recipe == null) return new CraftingResult { failureReason = "Recipe not found." };

            var progress = GetDisciplineProgress(recipe.discipline);
            if (progress == null || progress.level < recipe.requiredLevel)
            {
                return new CraftingResult { failureReason = "Discipline level too low." };
            }

            if (!progress.KnowsRecipe(recipeId))
            {
                return new CraftingResult { failureReason = "Recipe not learned." };
            }

            if (!HasRequiredMaterials(recipe))
            {
                return new CraftingResult { failureReason = "Insufficient materials." };
            }

            CraftingStation station = null;
            if (!string.IsNullOrEmpty(stationId))
            {
                station = FindStation(stationId);
            }

            ConsumeMaterials(recipe);
            return CompleteCraft(recipe, progress, station);
        }

        private void UpdateCraftingQueue()
        {
            for (int i = craftingQueue.Count - 1; i >= 0; i--)
            {
                var item = craftingQueue[i];
                if (item.isComplete) continue;

                item.timeRemaining -= Time.deltaTime;
                OnCraftingProgress?.Invoke(item, 1f - (item.timeRemaining / item.totalTime));

                if (item.timeRemaining <= 0)
                {
                    item.isComplete = true;
                    var progress = GetDisciplineProgress(item.recipe.discipline);
                    var result = CompleteCraft(item.recipe, progress, item.station);

                    if (result.success)
                    {
                        OnCraftingComplete?.Invoke(result);
                    }
                    else
                    {
                        OnCraftingFailed?.Invoke(result);
                    }

                    craftingQueue.RemoveAt(i);
                }
            }
        }

        private CraftingResult CompleteCraft(CraftingRecipe recipe, DisciplineProgress progress, CraftingStation station)
        {
            var result = new CraftingResult
            {
                resultItemId = recipe.resultItemId,
                resultItemName = recipe.resultItemName,
                quantity = recipe.resultQuantity
            };

            // Calculate success
            float successChance = recipe.baseSuccessChance;
            successChance += progress.level * 0.02f;
            if (station != null) successChance += station.qualityBonus;
            successChance = Mathf.Clamp01(successChance);

            float roll = UnityEngine.Random.Range(0f, 1f);

            if (roll > successChance)
            {
                result.success = false;
                result.failureReason = "Crafting failed! Materials lost.";
                result.expGained = Mathf.RoundToInt(recipe.expReward * 0.25f);
                progress.AddExperience(result.expGained);
                return result;
            }

            result.success = true;

            // Determine quality
            float qualityRoll = UnityEngine.Random.Range(0f, 1f);
            qualityRoll += progress.level * 0.01f;
            if (station != null) qualityRoll += station.qualityBonus;

            float critChance = config != null ? config.criticalCraftChance : 0.05f;
            if (UnityEngine.Random.Range(0f, 1f) < critChance)
            {
                float critBoost = config != null ? config.criticalQualityBoost : 2f;
                qualityRoll *= critBoost;
            }

            result.qualityRoll = qualityRoll;
            result.quality = DetermineQuality(qualityRoll);

            // Soul forge bonuses
            if (recipe.requiresCreatureSoul)
            {
                result.bonusEffects.Add($"Soul Forged: +{(config != null ? config.soulForgeBonusDamage : 0.15f) * 100}% elemental damage");
                result.bonusEffects.Add($"Element: {recipe.requiredElement}");
            }

            // Award experience
            float qualityMultiplier = 1f + ((int)result.quality * 0.2f);
            result.expGained = Mathf.RoundToInt(recipe.expReward * qualityMultiplier);
            progress.AddExperience(result.expGained);
            progress.itemsCrafted++;

            if (result.quality >= CraftQuality.Masterwork)
            {
                progress.masterworksCrafted++;
            }

            return result;
        }

        private CraftQuality DetermineQuality(float roll)
        {
            if (roll >= 1.5f) return CraftQuality.Mythic;
            if (roll >= 1.2f) return CraftQuality.Legendary;
            if (roll >= 0.95f) return CraftQuality.Masterwork;
            if (roll >= 0.75f) return CraftQuality.Superior;
            if (roll >= 0.5f) return CraftQuality.Fine;
            if (roll >= 0.2f) return CraftQuality.Common;
            return CraftQuality.Crude;
        }

        #endregion

        #region Material Management

        /// <summary>
        /// Adds a material to the player's crafting inventory.
        /// </summary>
        public void AddMaterial(string materialId, int amount)
        {
            if (materialInventory.ContainsKey(materialId))
            {
                materialInventory[materialId] += amount;
            }
            else
            {
                materialInventory[materialId] = amount;
            }
        }

        /// <summary>
        /// Removes a material from the crafting inventory.
        /// </summary>
        public bool RemoveMaterial(string materialId, int amount)
        {
            if (!materialInventory.ContainsKey(materialId)) return false;
            if (materialInventory[materialId] < amount) return false;

            materialInventory[materialId] -= amount;
            if (materialInventory[materialId] <= 0)
            {
                materialInventory.Remove(materialId);
            }
            return true;
        }

        /// <summary>
        /// Gets the current amount of a material.
        /// </summary>
        public int GetMaterialCount(string materialId)
        {
            return materialInventory.ContainsKey(materialId) ? materialInventory[materialId] : 0;
        }

        /// <summary>
        /// Checks if the player has all required materials for a recipe.
        /// </summary>
        public bool HasRequiredMaterials(CraftingRecipe recipe)
        {
            foreach (var mat in recipe.requiredMaterials)
            {
                if (GetMaterialCount(mat.materialId) < mat.quantity) return false;
            }
            return true;
        }

        private void ConsumeMaterials(CraftingRecipe recipe)
        {
            foreach (var mat in recipe.requiredMaterials)
            {
                RemoveMaterial(mat.materialId, mat.quantity);
            }
        }

        #endregion

        #region Recipes & Disciplines

        /// <summary>
        /// Learns a new recipe for a discipline.
        /// </summary>
        public bool LearnRecipe(string recipeId)
        {
            var recipe = FindRecipe(recipeId);
            if (recipe == null) return false;

            var progress = GetDisciplineProgress(recipe.discipline);
            if (progress == null) return false;

            if (progress.KnowsRecipe(recipeId)) return false;

            progress.LearnRecipe(recipeId);
            OnRecipeLearned?.Invoke(recipe.discipline, recipeId);
            return true;
        }

        /// <summary>
        /// Discovers a recipe (adds it to the known list with a notification).
        /// </summary>
        public bool DiscoverRecipe(string recipeId)
        {
            var recipe = FindRecipe(recipeId);
            if (recipe == null) return false;

            var progress = GetDisciplineProgress(recipe.discipline);
            if (progress == null || progress.KnowsRecipe(recipeId)) return false;

            progress.LearnRecipe(recipeId);
            OnRecipeDiscovered?.Invoke(recipe.discipline, recipeId);
            return true;
        }

        /// <summary>
        /// Gets all recipes the player knows for a specific discipline.
        /// </summary>
        public List<CraftingRecipe> GetKnownRecipes(CraftingDiscipline discipline)
        {
            var progress = GetDisciplineProgress(discipline);
            if (progress == null || config == null) return new List<CraftingRecipe>();

            return config.allRecipes
                .Where(r => r.discipline == discipline && progress.KnowsRecipe(r.recipeId))
                .ToList();
        }

        /// <summary>
        /// Gets all recipes the player can currently craft (has materials and level).
        /// </summary>
        public List<CraftingRecipe> GetCraftableRecipes(CraftingDiscipline discipline)
        {
            var progress = GetDisciplineProgress(discipline);
            if (progress == null || config == null) return new List<CraftingRecipe>();

            return config.allRecipes
                .Where(r => r.discipline == discipline
                    && progress.KnowsRecipe(r.recipeId)
                    && progress.level >= r.requiredLevel
                    && HasRequiredMaterials(r))
                .ToList();
        }

        public DisciplineProgress GetDisciplineProgress(CraftingDiscipline discipline)
        {
            return disciplineProgress.FirstOrDefault(d => d.discipline == discipline);
        }

        public int GetDisciplineLevel(CraftingDiscipline discipline)
        {
            var progress = GetDisciplineProgress(discipline);
            return progress?.level ?? 0;
        }

        private CraftingRecipe FindRecipe(string recipeId)
        {
            return config?.allRecipes.FirstOrDefault(r => r.recipeId == recipeId);
        }

        private CraftingStation FindStation(string stationId)
        {
            return config?.registeredStations.FirstOrDefault(s => s.stationId == stationId);
        }

        #endregion

        #region Save Data

        [Serializable]
        public class CraftingSaveData
        {
            public List<DisciplineProgressSave> disciplines = new List<DisciplineProgressSave>();
            public Dictionary<string, int> materials = new Dictionary<string, int>();
        }

        [Serializable]
        public class DisciplineProgressSave
        {
            public int discipline;
            public int level;
            public int currentExp;
            public List<string> learnedRecipeIds;
            public int itemsCrafted;
            public int masterworksCrafted;
        }

        public CraftingSaveData GetSaveData()
        {
            var data = new CraftingSaveData
            {
                materials = new Dictionary<string, int>(materialInventory)
            };

            foreach (var dp in disciplineProgress)
            {
                data.disciplines.Add(new DisciplineProgressSave
                {
                    discipline = (int)dp.discipline,
                    level = dp.level,
                    currentExp = dp.currentExp,
                    learnedRecipeIds = new List<string>(dp.learnedRecipeIds),
                    itemsCrafted = dp.itemsCrafted,
                    masterworksCrafted = dp.masterworksCrafted
                });
            }

            return data;
        }

        #endregion
    }
}
