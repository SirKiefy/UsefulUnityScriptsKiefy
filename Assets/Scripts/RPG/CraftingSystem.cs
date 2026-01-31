using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UsefulScripts.RPG
{
    /// <summary>
    /// Defines the category of crafting profession.
    /// </summary>
    public enum CraftingProfession
    {
        Blacksmithing,      // Weapons, armor, tools
        Alchemy,            // Potions, elixirs, transmutation
        Enchanting,         // Magic enhancements
        Cooking,            // Food, buffs
        Tailoring,          // Cloth armor, bags
        Leatherworking,     // Leather armor
        Jewelcrafting,      // Rings, necklaces, gems
        Woodworking,        // Bows, staves, furniture
        Engineering,        // Gadgets, machines
        Inscription,        // Scrolls, runes
        Synthesis           // Combine multiple items
    }

    /// <summary>
    /// Defines the quality tier of crafted items.
    /// </summary>
    public enum CraftingQuality
    {
        Poor,           // 0-19% success roll
        Normal,         // 20-39%
        Fine,           // 40-59%
        Superior,       // 60-79%
        Exceptional,    // 80-94%
        Masterwork,     // 95-99%
        Legendary       // 100% + bonuses
    }

    /// <summary>
    /// Represents a material required for crafting.
    /// </summary>
    [Serializable]
    public class CraftingMaterial
    {
        public string materialId;
        public string materialName;
        public int quantity;
        public bool isOptional;
        public float qualityBonus;      // Bonus to quality roll if used
        public List<string> alternatives = new List<string>();  // Alternative materials

        public CraftingMaterial(string id, string name, int qty)
        {
            materialId = id;
            materialName = name;
            quantity = qty;
        }
    }

    /// <summary>
    /// Represents a crafting recipe.
    /// </summary>
    [Serializable]
    public class CraftingRecipe
    {
        [Header("Basic Info")]
        public string recipeId;
        public string recipeName;
        [TextArea(2, 4)]
        public string description;
        public Sprite icon;
        public CraftingProfession profession;

        [Header("Requirements")]
        public int requiredLevel = 1;
        public int experienceGained = 10;
        public List<CraftingMaterial> materials = new List<CraftingMaterial>();
        public List<string> requiredTools = new List<string>();

        [Header("Output")]
        public string resultItemId;
        public string resultItemName;
        public int resultQuantity = 1;
        public List<string> possibleBonusItems = new List<string>();
        public float bonusItemChance = 0.1f;

        [Header("Crafting")]
        public float craftingTime = 2f;
        public float baseDifficulty = 1f;
        public bool canCritical = true;
        public float criticalChanceBonus = 0f;
        public int maxCraftQuantity = 10;

        [Header("Unlock")]
        public bool isUnlockedByDefault = false;
        public string unlockSource;     // Quest, purchase, discovery, etc.
        public int unlockCost = 0;

        /// <summary>
        /// Checks if the recipe can be crafted with available materials.
        /// </summary>
        public bool CanCraft(Dictionary<string, int> availableMaterials, int quantity = 1)
        {
            foreach (var material in materials.Where(m => !m.isOptional))
            {
                int required = material.quantity * quantity;
                bool hasMaterial = availableMaterials.TryGetValue(material.materialId, out int available) 
                                  && available >= required;
                
                // Check alternatives
                if (!hasMaterial && material.alternatives.Count > 0)
                {
                    hasMaterial = material.alternatives.Any(alt =>
                        availableMaterials.TryGetValue(alt, out int altAvailable) && altAvailable >= required
                    );
                }

                if (!hasMaterial) return false;
            }
            return true;
        }

        /// <summary>
        /// Gets the maximum quantity that can be crafted with available materials.
        /// </summary>
        public int GetMaxCraftableQuantity(Dictionary<string, int> availableMaterials)
        {
            int maxQty = maxCraftQuantity;

            foreach (var material in materials.Where(m => !m.isOptional))
            {
                if (availableMaterials.TryGetValue(material.materialId, out int available))
                {
                    int possible = available / material.quantity;
                    maxQty = Mathf.Min(maxQty, possible);
                }
                else
                {
                    // Check alternatives
                    int altMax = 0;
                    foreach (var alt in material.alternatives)
                    {
                        if (availableMaterials.TryGetValue(alt, out int altAvailable))
                        {
                            altMax = Mathf.Max(altMax, altAvailable / material.quantity);
                        }
                    }
                    maxQty = Mathf.Min(maxQty, altMax);
                }
            }

            return Mathf.Max(0, maxQty);
        }
    }

    /// <summary>
    /// Represents a crafting station/workbench.
    /// </summary>
    [Serializable]
    public class CraftingStation
    {
        public string stationId;
        public string stationName;
        public Sprite stationIcon;
        public List<CraftingProfession> supportedProfessions = new List<CraftingProfession>();
        
        [Header("Bonuses")]
        public float qualityBonus = 0f;
        public float speedBonus = 0f;
        public float experienceBonus = 0f;
        public float criticalBonus = 0f;
        public float materialSaveChance = 0f;
        
        [Header("Requirements")]
        public int requiredLevel = 1;
        public bool requiresFuel = false;
        public string fuelItemId;
        public float fuelConsumptionRate = 1f;

        public bool SupportsRecipe(CraftingRecipe recipe)
        {
            return supportedProfessions.Contains(recipe.profession);
        }
    }

    /// <summary>
    /// Represents the result of a crafting attempt.
    /// </summary>
    [Serializable]
    public class CraftingResult
    {
        public bool success;
        public CraftingRecipe recipe;
        public int quantityCrafted;
        public CraftingQuality quality;
        public bool wasCritical;
        public int experienceGained;
        public List<string> bonusItemsReceived = new List<string>();
        public List<string> consumedMaterials = new List<string>();
        public bool materialsSaved;
        public string failureReason;
        public float qualityRoll;
        public DateTime craftedAt;

        public CraftingResult()
        {
            craftedAt = DateTime.Now;
        }
    }

    /// <summary>
    /// Represents progress in a crafting profession.
    /// </summary>
    [Serializable]
    public class ProfessionProgress
    {
        public CraftingProfession profession;
        public int currentLevel = 1;
        public int maxLevel = 100;
        public int currentExperience = 0;
        public int experienceToNextLevel = 100;
        public int totalItemsCrafted = 0;
        public int criticalCrafts = 0;
        public int legendaryItemsCrafted = 0;
        public List<string> learnedRecipes = new List<string>();
        public List<string> discoveredRecipes = new List<string>();

        public event Action<int, int> OnLevelUp;
        public event Action<string> OnRecipeLearned;

        public float ExperienceProgress => (float)currentExperience / experienceToNextLevel;
        public bool IsMaxLevel => currentLevel >= maxLevel;

        /// <summary>
        /// Adds experience to this profession.
        /// </summary>
        public int AddExperience(int amount)
        {
            if (IsMaxLevel) return 0;

            currentExperience += amount;
            int levelsGained = 0;

            while (currentExperience >= experienceToNextLevel && !IsMaxLevel)
            {
                currentExperience -= experienceToNextLevel;
                int previousLevel = currentLevel;
                currentLevel++;
                levelsGained++;
                experienceToNextLevel = CalculateExpForLevel(currentLevel + 1);
                OnLevelUp?.Invoke(previousLevel, currentLevel);
            }

            return levelsGained;
        }

        /// <summary>
        /// Calculates experience required for a level.
        /// </summary>
        public int CalculateExpForLevel(int level)
        {
            return (int)(100 * Mathf.Pow(level, 1.5f));
        }

        /// <summary>
        /// Learns a new recipe.
        /// </summary>
        public bool LearnRecipe(string recipeId)
        {
            if (learnedRecipes.Contains(recipeId)) return false;

            learnedRecipes.Add(recipeId);
            OnRecipeLearned?.Invoke(recipeId);
            return true;
        }

        /// <summary>
        /// Checks if a recipe has been learned.
        /// </summary>
        public bool HasRecipe(string recipeId)
        {
            return learnedRecipes.Contains(recipeId);
        }
    }

    /// <summary>
    /// Configuration for the crafting system.
    /// </summary>
    [CreateAssetMenu(fileName = "CraftingConfig", menuName = "UsefulScripts/RPG/Crafting Config")]
    public class CraftingConfig : ScriptableObject
    {
        [Header("General Settings")]
        public float baseCraftingTime = 2f;
        public bool allowQueueCrafting = true;
        public int maxQueueSize = 10;
        public bool allowCraftingWhileMoving = false;

        [Header("Quality Settings")]
        public float baseQualityChance = 50f;
        public float qualityPerSkillLevel = 0.5f;
        public float criticalQualityBonus = 20f;

        [Header("Experience Settings")]
        public float experienceMultiplier = 1f;
        public float bonusExpForHighQuality = 0.5f;
        public float bonusExpForFirstCraft = 2f;

        [Header("Material Settings")]
        public float baseMaterialSaveChance = 0f;
        public float materialSaveChancePerLevel = 0.1f;
        public float criticalMaterialSaveChance = 0.25f;

        [Header("Discovery")]
        public bool enableRecipeDiscovery = true;
        public float discoveryChance = 0.05f;
        public float discoveryChancePerLevel = 0.001f;

        [Header("Recipes")]
        public List<CraftingRecipe> allRecipes = new List<CraftingRecipe>();

        [Header("Stations")]
        public List<CraftingStation> craftingStations = new List<CraftingStation>();
    }

    /// <summary>
    /// Represents a crafting queue item.
    /// </summary>
    [Serializable]
    public class CraftingQueueItem
    {
        public CraftingRecipe recipe;
        public int quantity;
        public int quantityCrafted;
        public float progress;
        public bool isPaused;
        public CraftingStation station;
        public Dictionary<string, int> optionalMaterials = new Dictionary<string, int>();
        public DateTime queuedAt;

        public float ProgressPercent => (float)quantityCrafted / quantity + (progress / recipe.craftingTime) / quantity;
        public bool IsComplete => quantityCrafted >= quantity;
        public float TimeRemaining => (quantity - quantityCrafted - 1) * recipe.craftingTime + (recipe.craftingTime - progress);
    }

    /// <summary>
    /// Complete crafting system managing recipes, stations, and profession progress.
    /// </summary>
    public class CraftingSystem : MonoBehaviour
    {
        public static CraftingSystem Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private CraftingConfig config;

        [Header("Current State")]
        [SerializeField] private CraftingStation currentStation;
        [SerializeField] private bool isCrafting;

        // Runtime data
        private Dictionary<CraftingProfession, ProfessionProgress> professionProgress = new Dictionary<CraftingProfession, ProfessionProgress>();
        private List<CraftingQueueItem> craftingQueue = new List<CraftingQueueItem>();
        private Dictionary<string, int> craftingHistory = new Dictionary<string, int>();
        private System.Random random = new System.Random();

        // Material check delegate (connect to inventory)
        public Func<string, int> GetMaterialCount;
        public Action<string, int> ConsumeMaterial;
        public Action<string, int> AddItem;

        // Events
        public event Action<CraftingResult> OnCraftingComplete;
        public event Action<CraftingRecipe> OnCraftingStarted;
        public event Action<CraftingRecipe> OnCraftingFailed;
        public event Action<CraftingRecipe, float> OnCraftingProgress;
        public event Action<CraftingProfession, int> OnProfessionLevelUp;
        public event Action<CraftingRecipe> OnRecipeLearned;
        public event Action<CraftingRecipe> OnRecipeDiscovered;
        public event Action<CraftingQueueItem> OnItemAddedToQueue;
        public event Action<CraftingQueueItem> OnItemRemovedFromQueue;

        // Properties
        public bool IsCrafting => isCrafting;
        public CraftingStation CurrentStation => currentStation;
        public IReadOnlyList<CraftingQueueItem> CraftingQueue => craftingQueue.AsReadOnly();
        public int QueueSize => craftingQueue.Count;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeProfessions();
        }

        private void Update()
        {
            if (isCrafting && craftingQueue.Count > 0)
            {
                ProcessCraftingQueue(Time.deltaTime);
            }
        }

        private void InitializeProfessions()
        {
            foreach (CraftingProfession profession in Enum.GetValues(typeof(CraftingProfession)))
            {
                professionProgress[profession] = new ProfessionProgress { profession = profession };
                professionProgress[profession].OnLevelUp += (prev, curr) => 
                    OnProfessionLevelUp?.Invoke(profession, curr);
            }

            // Learn default recipes
            if (config != null)
            {
                foreach (var recipe in config.allRecipes.Where(r => r.isUnlockedByDefault))
                {
                    LearnRecipe(recipe.recipeId);
                }
            }
        }

        #region Station Management

        /// <summary>
        /// Sets the current crafting station.
        /// </summary>
        public void SetStation(CraftingStation station)
        {
            currentStation = station;
        }

        /// <summary>
        /// Clears the current crafting station.
        /// </summary>
        public void LeaveStation()
        {
            currentStation = null;
        }

        /// <summary>
        /// Gets available stations for a profession.
        /// </summary>
        public List<CraftingStation> GetStationsForProfession(CraftingProfession profession)
        {
            if (config == null) return new List<CraftingStation>();
            return config.craftingStations.Where(s => s.supportedProfessions.Contains(profession)).ToList();
        }

        /// <summary>
        /// Checks if the current station can craft a recipe.
        /// </summary>
        public bool CanUseStation(CraftingRecipe recipe)
        {
            if (currentStation == null) return false;
            return currentStation.SupportsRecipe(recipe);
        }

        #endregion

        #region Recipe Management

        /// <summary>
        /// Learns a new recipe.
        /// </summary>
        public bool LearnRecipe(string recipeId)
        {
            var recipe = GetRecipe(recipeId);
            if (recipe == null) return false;

            var progress = professionProgress[recipe.profession];
            if (progress.HasRecipe(recipeId)) return false;

            progress.LearnRecipe(recipeId);
            OnRecipeLearned?.Invoke(recipe);
            return true;
        }

        /// <summary>
        /// Gets a recipe by ID.
        /// </summary>
        public CraftingRecipe GetRecipe(string recipeId)
        {
            return config?.allRecipes.FirstOrDefault(r => r.recipeId == recipeId);
        }

        /// <summary>
        /// Gets all learned recipes for a profession.
        /// </summary>
        public List<CraftingRecipe> GetLearnedRecipes(CraftingProfession profession)
        {
            var progress = professionProgress[profession];
            return config?.allRecipes
                .Where(r => r.profession == profession && progress.HasRecipe(r.recipeId))
                .ToList() ?? new List<CraftingRecipe>();
        }

        /// <summary>
        /// Gets all recipes that can be crafted with current materials.
        /// </summary>
        public List<CraftingRecipe> GetCraftableRecipes(CraftingProfession profession)
        {
            var learned = GetLearnedRecipes(profession);
            var materials = GetAvailableMaterials();
            
            return learned.Where(r => 
                r.CanCraft(materials) && 
                GetProfessionLevel(profession) >= r.requiredLevel &&
                (currentStation == null || currentStation.SupportsRecipe(r))
            ).ToList();
        }

        /// <summary>
        /// Gets recipes that are close to being craftable (missing few materials).
        /// </summary>
        public List<(CraftingRecipe recipe, List<CraftingMaterial> missing)> GetAlmostCraftableRecipes(CraftingProfession profession, int maxMissing = 2)
        {
            var learned = GetLearnedRecipes(profession);
            var materials = GetAvailableMaterials();
            var results = new List<(CraftingRecipe, List<CraftingMaterial>)>();

            foreach (var recipe in learned)
            {
                var missing = new List<CraftingMaterial>();
                foreach (var mat in recipe.materials.Where(m => !m.isOptional))
                {
                    if (!materials.TryGetValue(mat.materialId, out int available) || available < mat.quantity)
                    {
                        bool hasAlt = mat.alternatives.Any(alt =>
                            materials.TryGetValue(alt, out int altAvailable) && altAvailable >= mat.quantity);
                        
                        if (!hasAlt)
                        {
                            missing.Add(new CraftingMaterial(mat.materialId, mat.materialName, 
                                mat.quantity - (available)));
                        }
                    }
                }

                if (missing.Count > 0 && missing.Count <= maxMissing)
                {
                    results.Add((recipe, missing));
                }
            }

            return results.OrderBy(r => r.missing.Count).ToList();
        }

        /// <summary>
        /// Searches for recipes by name or ingredient.
        /// </summary>
        public List<CraftingRecipe> SearchRecipes(string query, CraftingProfession? profession = null)
        {
            if (config == null) return new List<CraftingRecipe>();

            query = query.ToLower();
            return config.allRecipes
                .Where(r => 
                    (!profession.HasValue || r.profession == profession.Value) &&
                    (r.recipeName.ToLower().Contains(query) ||
                     r.resultItemName.ToLower().Contains(query) ||
                     r.materials.Any(m => m.materialName.ToLower().Contains(query)))
                ).ToList();
        }

        private Dictionary<string, int> GetAvailableMaterials()
        {
            var materials = new Dictionary<string, int>();
            
            if (GetMaterialCount != null && config != null)
            {
                // Get unique material IDs from all recipes
                var allMaterialIds = config.allRecipes
                    .SelectMany(r => r.materials.Select(m => m.materialId))
                    .Distinct();

                foreach (var materialId in allMaterialIds)
                {
                    materials[materialId] = GetMaterialCount(materialId);
                }
            }

            return materials;
        }

        #endregion

        #region Crafting

        /// <summary>
        /// Crafts an item immediately (single item, no queue).
        /// </summary>
        public CraftingResult CraftItem(string recipeId, Dictionary<string, int> optionalMaterials = null)
        {
            var recipe = GetRecipe(recipeId);
            if (recipe == null)
            {
                return new CraftingResult { success = false, failureReason = "Recipe not found" };
            }

            return CraftItem(recipe, 1, optionalMaterials);
        }

        /// <summary>
        /// Crafts items immediately.
        /// </summary>
        public CraftingResult CraftItem(CraftingRecipe recipe, int quantity = 1, Dictionary<string, int> optionalMaterials = null)
        {
            var result = new CraftingResult { recipe = recipe };

            // Validate
            var progress = professionProgress[recipe.profession];
            if (!progress.HasRecipe(recipe.recipeId))
            {
                result.failureReason = "Recipe not learned";
                return result;
            }

            if (progress.currentLevel < recipe.requiredLevel)
            {
                result.failureReason = $"Requires level {recipe.requiredLevel}";
                return result;
            }

            var materials = GetAvailableMaterials();
            if (!recipe.CanCraft(materials, quantity))
            {
                result.failureReason = "Insufficient materials";
                return result;
            }

            // Consume materials
            foreach (var material in recipe.materials.Where(m => !m.isOptional))
            {
                int toConsume = material.quantity * quantity;
                ConsumeMaterial?.Invoke(material.materialId, toConsume);
                result.consumedMaterials.Add($"{material.materialName} x{toConsume}");
            }

            // Consume optional materials
            if (optionalMaterials != null)
            {
                foreach (var kvp in optionalMaterials)
                {
                    ConsumeMaterial?.Invoke(kvp.Key, kvp.Value);
                }
            }

            // Calculate quality
            float qualityRoll = CalculateQualityRoll(recipe, progress, optionalMaterials);
            result.qualityRoll = qualityRoll;
            result.quality = DetermineQuality(qualityRoll);
            result.wasCritical = qualityRoll >= 100f;

            // Check for material save on critical
            if (result.wasCritical && random.NextDouble() < GetMaterialSaveChance(progress))
            {
                result.materialsSaved = true;
                // Refund some materials
                var refundMaterial = recipe.materials.First(m => !m.isOptional);
                AddItem?.Invoke(refundMaterial.materialId, refundMaterial.quantity);
            }

            // Add result items
            int totalQuantity = recipe.resultQuantity * quantity;
            if (result.wasCritical)
            {
                totalQuantity = (int)(totalQuantity * 1.5f);
            }
            
            AddItem?.Invoke(recipe.resultItemId, totalQuantity);
            result.quantityCrafted = totalQuantity;

            // Check for bonus items
            foreach (var bonusItem in recipe.possibleBonusItems)
            {
                if (random.NextDouble() < recipe.bonusItemChance + (result.wasCritical ? 0.1f : 0))
                {
                    AddItem?.Invoke(bonusItem, 1);
                    result.bonusItemsReceived.Add(bonusItem);
                }
            }

            // Award experience
            float expMultiplier = config?.experienceMultiplier ?? 1f;
            if (result.quality >= CraftingQuality.Superior)
            {
                expMultiplier *= 1f + (config?.bonusExpForHighQuality ?? 0.5f);
            }
            if (!craftingHistory.ContainsKey(recipe.recipeId))
            {
                expMultiplier *= config?.bonusExpForFirstCraft ?? 2f;
                craftingHistory[recipe.recipeId] = 0;
            }
            craftingHistory[recipe.recipeId]++;

            int expGained = (int)(recipe.experienceGained * quantity * expMultiplier);
            if (currentStation != null)
            {
                expGained = (int)(expGained * (1f + currentStation.experienceBonus));
            }

            progress.AddExperience(expGained);
            result.experienceGained = expGained;

            // Update stats
            progress.totalItemsCrafted += result.quantityCrafted;
            if (result.wasCritical) progress.criticalCrafts++;
            if (result.quality == CraftingQuality.Legendary) progress.legendaryItemsCrafted++;

            // Check for recipe discovery
            if (config?.enableRecipeDiscovery == true)
            {
                TryDiscoverRecipe(recipe.profession, progress);
            }

            result.success = true;
            OnCraftingComplete?.Invoke(result);
            return result;
        }

        /// <summary>
        /// Adds items to the crafting queue.
        /// </summary>
        public bool AddToQueue(string recipeId, int quantity, Dictionary<string, int> optionalMaterials = null)
        {
            if (!config?.allowQueueCrafting ?? true) return false;
            if (craftingQueue.Count >= (config?.maxQueueSize ?? 10)) return false;

            var recipe = GetRecipe(recipeId);
            if (recipe == null) return false;

            var materials = GetAvailableMaterials();
            if (!recipe.CanCraft(materials, quantity)) return false;

            // Reserve materials
            foreach (var material in recipe.materials.Where(m => !m.isOptional))
            {
                ConsumeMaterial?.Invoke(material.materialId, material.quantity * quantity);
            }

            var queueItem = new CraftingQueueItem
            {
                recipe = recipe,
                quantity = quantity,
                station = currentStation,
                optionalMaterials = optionalMaterials ?? new Dictionary<string, int>(),
                queuedAt = DateTime.Now
            };

            craftingQueue.Add(queueItem);
            OnItemAddedToQueue?.Invoke(queueItem);

            if (!isCrafting)
            {
                StartCrafting();
            }

            return true;
        }

        /// <summary>
        /// Removes an item from the crafting queue.
        /// </summary>
        public bool RemoveFromQueue(int index)
        {
            if (index < 0 || index >= craftingQueue.Count) return false;

            var item = craftingQueue[index];
            
            // Refund materials for uncrafted quantity
            int uncraftedQty = item.quantity - item.quantityCrafted;
            foreach (var material in item.recipe.materials.Where(m => !m.isOptional))
            {
                AddItem?.Invoke(material.materialId, material.quantity * uncraftedQty);
            }

            craftingQueue.RemoveAt(index);
            OnItemRemovedFromQueue?.Invoke(item);

            if (craftingQueue.Count == 0)
            {
                StopCrafting();
            }

            return true;
        }

        /// <summary>
        /// Clears the entire crafting queue.
        /// </summary>
        public void ClearQueue()
        {
            while (craftingQueue.Count > 0)
            {
                RemoveFromQueue(0);
            }
        }

        /// <summary>
        /// Pauses crafting.
        /// </summary>
        public void PauseCrafting()
        {
            if (craftingQueue.Count > 0)
            {
                craftingQueue[0].isPaused = true;
            }
            isCrafting = false;
        }

        /// <summary>
        /// Resumes crafting.
        /// </summary>
        public void ResumeCrafting()
        {
            if (craftingQueue.Count > 0)
            {
                craftingQueue[0].isPaused = false;
            }
            isCrafting = craftingQueue.Count > 0;
        }

        private void StartCrafting()
        {
            isCrafting = true;
            if (craftingQueue.Count > 0)
            {
                OnCraftingStarted?.Invoke(craftingQueue[0].recipe);
            }
        }

        private void StopCrafting()
        {
            isCrafting = false;
        }

        private void ProcessCraftingQueue(float deltaTime)
        {
            if (craftingQueue.Count == 0) return;

            var currentItem = craftingQueue[0];
            if (currentItem.isPaused) return;

            float speedMultiplier = 1f;
            if (currentItem.station != null)
            {
                speedMultiplier *= 1f + currentItem.station.speedBonus;
            }

            currentItem.progress += deltaTime * speedMultiplier;
            OnCraftingProgress?.Invoke(currentItem.recipe, currentItem.ProgressPercent);

            // Check if one item is complete
            if (currentItem.progress >= currentItem.recipe.craftingTime)
            {
                currentItem.progress = 0;
                
                // Craft one item
                var result = ProcessSingleCraft(currentItem);
                currentItem.quantityCrafted++;

                // Check if all items are complete
                if (currentItem.IsComplete)
                {
                    craftingQueue.RemoveAt(0);
                    
                    if (craftingQueue.Count > 0)
                    {
                        OnCraftingStarted?.Invoke(craftingQueue[0].recipe);
                    }
                    else
                    {
                        StopCrafting();
                    }
                }
            }
        }

        private CraftingResult ProcessSingleCraft(CraftingQueueItem item)
        {
            var result = new CraftingResult
            {
                recipe = item.recipe,
                quantityCrafted = item.recipe.resultQuantity
            };

            var progress = professionProgress[item.recipe.profession];

            // Calculate quality
            float qualityRoll = CalculateQualityRoll(item.recipe, progress, item.optionalMaterials);
            result.qualityRoll = qualityRoll;
            result.quality = DetermineQuality(qualityRoll);
            result.wasCritical = qualityRoll >= 100f;

            // Add result
            int qty = item.recipe.resultQuantity;
            if (result.wasCritical) qty = (int)(qty * 1.5f);
            
            AddItem?.Invoke(item.recipe.resultItemId, qty);

            // Award experience
            int exp = item.recipe.experienceGained;
            if (item.station != null) exp = (int)(exp * (1f + item.station.experienceBonus));
            progress.AddExperience(exp);
            result.experienceGained = exp;

            result.success = true;
            OnCraftingComplete?.Invoke(result);
            return result;
        }

        #endregion

        #region Quality Calculation

        private float CalculateQualityRoll(CraftingRecipe recipe, ProfessionProgress progress, Dictionary<string, int> optionalMaterials)
        {
            float baseChance = config?.baseQualityChance ?? 50f;
            float skillBonus = progress.currentLevel * (config?.qualityPerSkillLevel ?? 0.5f);
            float difficultyPenalty = (recipe.baseDifficulty - 1f) * 10f;
            
            float stationBonus = 0f;
            if (currentStation != null)
            {
                stationBonus = currentStation.qualityBonus;
            }

            // Bonus from optional materials
            float materialBonus = 0f;
            if (optionalMaterials != null)
            {
                foreach (var mat in recipe.materials.Where(m => m.isOptional))
                {
                    if (optionalMaterials.ContainsKey(mat.materialId))
                    {
                        materialBonus += mat.qualityBonus;
                    }
                }
            }

            float totalChance = baseChance + skillBonus - difficultyPenalty + stationBonus + materialBonus;
            
            // Add randomness
            float roll = (float)(random.NextDouble() * 40f - 20f); // -20 to +20 variance
            totalChance += roll;

            // Critical check
            float critChance = recipe.criticalChanceBonus + (currentStation?.criticalBonus ?? 0f);
            if (recipe.canCritical && random.NextDouble() * 100 < critChance + 5f)
            {
                totalChance += config?.criticalQualityBonus ?? 20f;
            }

            return Mathf.Clamp(totalChance, 0f, 120f);
        }

        private CraftingQuality DetermineQuality(float roll)
        {
            if (roll >= 100f) return CraftingQuality.Legendary;
            if (roll >= 95f) return CraftingQuality.Masterwork;
            if (roll >= 80f) return CraftingQuality.Exceptional;
            if (roll >= 60f) return CraftingQuality.Superior;
            if (roll >= 40f) return CraftingQuality.Fine;
            if (roll >= 20f) return CraftingQuality.Normal;
            return CraftingQuality.Poor;
        }

        private float GetMaterialSaveChance(ProfessionProgress progress)
        {
            float baseChance = config?.baseMaterialSaveChance ?? 0f;
            float levelBonus = progress.currentLevel * (config?.materialSaveChancePerLevel ?? 0.1f);
            float stationBonus = currentStation?.materialSaveChance ?? 0f;
            return Mathf.Min(0.5f, baseChance + levelBonus + stationBonus);
        }

        #endregion

        #region Discovery

        private void TryDiscoverRecipe(CraftingProfession profession, ProfessionProgress progress)
        {
            float chance = (config?.discoveryChance ?? 0.05f) + 
                          progress.currentLevel * (config?.discoveryChancePerLevel ?? 0.001f);

            if (random.NextDouble() > chance) return;

            // Find an undiscovered recipe
            var undiscovered = config?.allRecipes
                .Where(r => r.profession == profession && 
                           !progress.HasRecipe(r.recipeId) &&
                           !progress.discoveredRecipes.Contains(r.recipeId) &&
                           r.requiredLevel <= progress.currentLevel + 5)
                .ToList();

            if (undiscovered == null || undiscovered.Count == 0) return;

            var discovered = undiscovered[random.Next(undiscovered.Count)];
            progress.discoveredRecipes.Add(discovered.recipeId);
            progress.LearnRecipe(discovered.recipeId);
            OnRecipeDiscovered?.Invoke(discovered);
        }

        #endregion

        #region Profession Progress

        /// <summary>
        /// Gets the progress for a profession.
        /// </summary>
        public ProfessionProgress GetProfessionProgress(CraftingProfession profession)
        {
            return professionProgress[profession];
        }

        /// <summary>
        /// Gets the current level of a profession.
        /// </summary>
        public int GetProfessionLevel(CraftingProfession profession)
        {
            return professionProgress[profession].currentLevel;
        }

        /// <summary>
        /// Gets all professions with their levels.
        /// </summary>
        public Dictionary<CraftingProfession, int> GetAllProfessionLevels()
        {
            return professionProgress.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.currentLevel);
        }

        /// <summary>
        /// Gets the total crafting level across all professions.
        /// </summary>
        public int GetTotalCraftingLevel()
        {
            return professionProgress.Values.Sum(p => p.currentLevel);
        }

        #endregion

        #region Utility

        /// <summary>
        /// Gets a summary of crafting statistics.
        /// </summary>
        public string GetCraftingSummary()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== Crafting Summary ===");
            sb.AppendLine();

            foreach (var kvp in professionProgress)
            {
                var p = kvp.Value;
                sb.AppendLine($"{kvp.Key}:");
                sb.AppendLine($"  Level: {p.currentLevel}/{p.maxLevel}");
                sb.AppendLine($"  EXP: {p.currentExperience}/{p.experienceToNextLevel}");
                sb.AppendLine($"  Recipes: {p.learnedRecipes.Count}");
                sb.AppendLine($"  Items Crafted: {p.totalItemsCrafted}");
                sb.AppendLine($"  Critical Crafts: {p.criticalCrafts}");
                sb.AppendLine($"  Legendary Items: {p.legendaryItemsCrafted}");
                sb.AppendLine();
            }

            if (currentStation != null)
            {
                sb.AppendLine($"Current Station: {currentStation.stationName}");
                sb.AppendLine($"  Quality Bonus: +{currentStation.qualityBonus:F1}%");
                sb.AppendLine($"  Speed Bonus: +{currentStation.speedBonus:F1}%");
            }

            sb.AppendLine($"\nQueue: {craftingQueue.Count} items");
            sb.AppendLine($"Total Recipes Crafted: {craftingHistory.Count}");

            return sb.ToString();
        }

        /// <summary>
        /// Creates save data for the crafting system.
        /// </summary>
        public CraftingSystemSaveData CreateSaveData()
        {
            return new CraftingSystemSaveData
            {
                professionProgress = professionProgress.ToDictionary(
                    kvp => kvp.Key.ToString(),
                    kvp => new ProfessionProgressSaveData
                    {
                        currentLevel = kvp.Value.currentLevel,
                        currentExperience = kvp.Value.currentExperience,
                        totalItemsCrafted = kvp.Value.totalItemsCrafted,
                        criticalCrafts = kvp.Value.criticalCrafts,
                        legendaryItemsCrafted = kvp.Value.legendaryItemsCrafted,
                        learnedRecipes = new List<string>(kvp.Value.learnedRecipes),
                        discoveredRecipes = new List<string>(kvp.Value.discoveredRecipes)
                    }
                ),
                craftingHistory = new Dictionary<string, int>(craftingHistory)
            };
        }

        /// <summary>
        /// Loads crafting system state from save data.
        /// </summary>
        public void LoadSaveData(CraftingSystemSaveData saveData)
        {
            if (saveData == null) return;

            foreach (var kvp in saveData.professionProgress)
            {
                if (Enum.TryParse<CraftingProfession>(kvp.Key, out var profession))
                {
                    var progress = professionProgress[profession];
                    progress.currentLevel = kvp.Value.currentLevel;
                    progress.currentExperience = kvp.Value.currentExperience;
                    progress.experienceToNextLevel = progress.CalculateExpForLevel(progress.currentLevel + 1);
                    progress.totalItemsCrafted = kvp.Value.totalItemsCrafted;
                    progress.criticalCrafts = kvp.Value.criticalCrafts;
                    progress.legendaryItemsCrafted = kvp.Value.legendaryItemsCrafted;
                    progress.learnedRecipes = new List<string>(kvp.Value.learnedRecipes);
                    progress.discoveredRecipes = new List<string>(kvp.Value.discoveredRecipes);
                }
            }

            craftingHistory = new Dictionary<string, int>(saveData.craftingHistory);
        }

        #endregion
    }

    /// <summary>
    /// Serializable save data for crafting system.
    /// </summary>
    [Serializable]
    public class CraftingSystemSaveData
    {
        public Dictionary<string, ProfessionProgressSaveData> professionProgress;
        public Dictionary<string, int> craftingHistory;
    }

    /// <summary>
    /// Serializable save data for profession progress.
    /// </summary>
    [Serializable]
    public class ProfessionProgressSaveData
    {
        public int currentLevel;
        public int currentExperience;
        public int totalItemsCrafted;
        public int criticalCrafts;
        public int legendaryItemsCrafted;
        public List<string> learnedRecipes;
        public List<string> discoveredRecipes;
    }
}
