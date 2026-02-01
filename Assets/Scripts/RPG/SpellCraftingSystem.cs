using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UsefulScripts.RPG
{
    /// <summary>
    /// Defines the style of spell crafting.
    /// </summary>
    public enum SpellCraftingStyle
    {
        ThreeSlot,      // Fixed 3-slot crafting (Element + Form + Modifier)
        Freestyle,      // Any number of compatible components
        RuneBased       // Pattern-based rune combinations
    }

    /// <summary>
    /// Defines the category of a spell component.
    /// </summary>
    public enum SpellComponentType
    {
        // Element types (primary damage/effect type)
        ElementFire,
        ElementIce,
        ElementLightning,
        ElementEarth,
        ElementWater,
        ElementWind,
        ElementLight,
        ElementDark,
        ElementArcane,
        ElementNature,

        // Form types (how the spell manifests)
        FormProjectile,
        FormBeam,
        FormAura,
        FormExplosion,
        FormWall,
        FormSummon,
        FormBuff,
        FormDebuff,
        FormHeal,
        FormTrap,

        // Modifier types (affects spell behavior)
        ModifierPower,
        ModifierSpeed,
        ModifierRange,
        ModifierDuration,
        ModifierArea,
        ModifierPiercing,
        ModifierHoming,
        ModifierChain,
        ModifierSplit,
        ModifierVampirism,

        // Rune types (for rune-based crafting)
        RuneAlpha,
        RuneBeta,
        RuneGamma,
        RuneDelta,
        RuneOmega,
        RunePrimal,
        RuneVoid,
        RuneAncient,
        RuneCelestial,
        RuneInfernal
    }

    /// <summary>
    /// Represents a spell component used in crafting.
    /// </summary>
    [Serializable]
    public class SpellComponent
    {
        public string componentId;
        public string componentName;
        [TextArea(2, 4)]
        public string description;
        public Sprite icon;
        public SpellComponentType componentType;

        [Header("Stats")]
        public float basePower = 10f;
        public float baseManaCost = 5f;
        public float powerMultiplier = 1f;
        public float manaMultiplier = 1f;

        [Header("Compatibility")]
        public List<SpellComponentType> compatibleWith = new List<SpellComponentType>();
        public List<SpellComponentType> incompatibleWith = new List<SpellComponentType>();

        [Header("Rarity & Unlock")]
        public SpellComponentRarity rarity = SpellComponentRarity.Common;
        public int requiredLevel = 1;
        public bool isUnlockedByDefault = true;

        /// <summary>
        /// Checks if this component is compatible with another.
        /// </summary>
        public bool IsCompatibleWith(SpellComponent other)
        {
            if (other == null) return false;
            if (incompatibleWith.Contains(other.componentType)) return false;
            if (compatibleWith.Count > 0 && !compatibleWith.Contains(other.componentType)) return false;
            return true;
        }

        /// <summary>
        /// Gets the component category (Element, Form, Modifier, or Rune).
        /// </summary>
        public SpellComponentCategory GetCategory()
        {
            string typeName = componentType.ToString();
            if (typeName.StartsWith("Element")) return SpellComponentCategory.Element;
            if (typeName.StartsWith("Form")) return SpellComponentCategory.Form;
            if (typeName.StartsWith("Modifier")) return SpellComponentCategory.Modifier;
            if (typeName.StartsWith("Rune")) return SpellComponentCategory.Rune;
            return SpellComponentCategory.Unknown;
        }
    }

    public enum SpellComponentCategory
    {
        Unknown,
        Element,
        Form,
        Modifier,
        Rune
    }

    public enum SpellComponentRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary,
        Mythic
    }

    /// <summary>
    /// Represents a rune pattern for rune-based spell crafting.
    /// </summary>
    [Serializable]
    public class RunePattern
    {
        public string patternId;
        public string patternName;
        public List<SpellComponentType> runeSequence = new List<SpellComponentType>();
        public bool orderMatters = true;
        public int minRunesRequired = 2;
        public int maxRunesAllowed = 5;

        [Header("Result")]
        public string resultSpellId;
        public float powerBonus = 0f;
        public float manaReduction = 0f;

        /// <summary>
        /// Checks if the provided runes match this pattern.
        /// </summary>
        public bool MatchesPattern(List<SpellComponentType> runes)
        {
            if (runes.Count < minRunesRequired || runes.Count > maxRunesAllowed) return false;

            if (orderMatters)
            {
                if (runes.Count != runeSequence.Count) return false;
                for (int i = 0; i < runes.Count; i++)
                {
                    if (runes[i] != runeSequence[i]) return false;
                }
                return true;
            }
            else
            {
                // Order doesn't matter, just check all required runes are present
                var required = new List<SpellComponentType>(runeSequence);
                foreach (var rune in runes)
                {
                    if (!required.Remove(rune)) return false;
                }
                return required.Count == 0;
            }
        }
    }

    /// <summary>
    /// Represents a recipe for spell crafting.
    /// </summary>
    [Serializable]
    public class SpellRecipe
    {
        [Header("Basic Info")]
        public string recipeId;
        public string recipeName;
        [TextArea(2, 4)]
        public string description;
        public Sprite icon;
        public SpellCraftingStyle craftingStyle;

        [Header("Requirements")]
        public List<string> requiredComponentIds = new List<string>();
        public List<SpellComponentType> requiredComponentTypes = new List<SpellComponentType>();
        public int requiredLevel = 1;
        public bool isUnlockedByDefault = false;

        [Header("Result")]
        public string resultSpellId;
        public string resultSpellName;
        public float basePower = 50f;
        public float baseManaCost = 20f;
        public float baseCooldown = 5f;
        public DamageType damageType = DamageType.Physical;

        [Header("Progression")]
        public int experienceGained = 25;
        public bool canCritical = true;
        public float discoveryChance = 0.05f;

        /// <summary>
        /// Checks if the recipe requirements are met.
        /// </summary>
        public bool CanCraft(List<SpellComponent> components, int playerLevel)
        {
            if (playerLevel < requiredLevel) return false;

            // Check required component IDs
            foreach (var id in requiredComponentIds)
            {
                if (!components.Any(c => c.componentId == id)) return false;
            }

            // Check required component types
            var typesCopy = new List<SpellComponentType>(requiredComponentTypes);
            foreach (var component in components)
            {
                typesCopy.Remove(component.componentType);
            }
            if (typesCopy.Count > 0) return false;

            return true;
        }
    }

    /// <summary>
    /// Represents a crafted spell.
    /// </summary>
    [Serializable]
    public class CraftedSpell
    {
        public string spellId;
        public string spellName;
        [TextArea(2, 4)]
        public string description;
        public Sprite icon;

        [Header("Stats")]
        public float power = 50f;
        public float manaCost = 20f;
        public float cooldown = 5f;
        public float range = 10f;
        public float areaOfEffect = 0f;
        public float duration = 0f;
        public DamageType damageType = DamageType.Physical;

        [Header("Quality")]
        public SpellQuality quality = SpellQuality.Normal;
        public bool isMasterwork = false;
        public float qualityMultiplier = 1f;

        [Header("Components Used")]
        public List<string> componentIds = new List<string>();
        public SpellCraftingStyle craftingStyle;
        public DateTime createdAt;

        [Header("Visual/Audio")]
        public Color spellColor = Color.white;
        public string vfxId;
        public string sfxId;

        public CraftedSpell()
        {
            createdAt = DateTime.Now;
        }

        /// <summary>
        /// Gets the effective power after quality modifiers.
        /// </summary>
        public float GetEffectivePower()
        {
            return power * qualityMultiplier;
        }

        /// <summary>
        /// Generates a description based on spell properties.
        /// </summary>
        public string GenerateDescription()
        {
            return $"{spellName}: Deals {GetEffectivePower():F0} {damageType} damage. " +
                   $"Costs {manaCost:F0} mana. Cooldown: {cooldown:F1}s. " +
                   $"Quality: {quality}{(isMasterwork ? " (Masterwork)" : "")}";
        }
    }

    /// <summary>
    /// Quality tier of a crafted spell.
    /// </summary>
    public enum SpellQuality
    {
        Poor,           // 0.6x multiplier
        Normal,         // 1.0x multiplier
        Fine,           // 1.1x multiplier
        Superior,       // 1.25x multiplier
        Exceptional,    // 1.5x multiplier
        Masterwork,     // 1.75x multiplier
        Legendary       // 2.0x multiplier
    }

    /// <summary>
    /// Result of a spell crafting attempt.
    /// </summary>
    [Serializable]
    public class SpellCraftingResult
    {
        public bool success;
        public CraftedSpell craftedSpell;
        public SpellQuality quality;
        public bool wasCritical;
        public int experienceGained;
        public float qualityRoll;
        public List<string> usedComponentIds = new List<string>();
        public SpellCraftingStyle styleUsed;
        public string failureReason;
        public DateTime craftedAt;
        public bool discoveredNewRecipe;
        public string discoveredRecipeId;

        public SpellCraftingResult()
        {
            craftedAt = DateTime.Now;
        }
    }

    /// <summary>
    /// Represents the three-slot crafting state.
    /// </summary>
    [Serializable]
    public class ThreeSlotCraftingState
    {
        public SpellComponent elementSlot;
        public SpellComponent formSlot;
        public SpellComponent modifierSlot;

        public bool IsComplete => elementSlot != null && formSlot != null && modifierSlot != null;
        public bool IsEmpty => elementSlot == null && formSlot == null && modifierSlot == null;

        public List<SpellComponent> GetComponents()
        {
            var list = new List<SpellComponent>();
            if (elementSlot != null) list.Add(elementSlot);
            if (formSlot != null) list.Add(formSlot);
            if (modifierSlot != null) list.Add(modifierSlot);
            return list;
        }

        public void Clear()
        {
            elementSlot = null;
            formSlot = null;
            modifierSlot = null;
        }
    }

    /// <summary>
    /// Represents the freestyle crafting state.
    /// </summary>
    [Serializable]
    public class FreestyleCraftingState
    {
        public List<SpellComponent> components = new List<SpellComponent>();
        public int maxComponents = 6;

        public bool IsEmpty => components.Count == 0;
        public bool IsFull => components.Count >= maxComponents;

        public bool AddComponent(SpellComponent component)
        {
            if (IsFull) return false;
            components.Add(component);
            return true;
        }

        public bool RemoveComponent(SpellComponent component)
        {
            return components.Remove(component);
        }

        public void Clear()
        {
            components.Clear();
        }
    }

    /// <summary>
    /// Represents the rune-based crafting state.
    /// </summary>
    [Serializable]
    public class RuneCraftingState
    {
        public List<SpellComponent> placedRunes = new List<SpellComponent>();
        public int maxRunes = 5;

        public bool IsEmpty => placedRunes.Count == 0;
        public bool IsFull => placedRunes.Count >= maxRunes;

        public bool AddRune(SpellComponent rune)
        {
            if (IsFull) return false;
            if (rune.GetCategory() != SpellComponentCategory.Rune) return false;
            placedRunes.Add(rune);
            return true;
        }

        public bool RemoveLastRune()
        {
            if (IsEmpty) return false;
            placedRunes.RemoveAt(placedRunes.Count - 1);
            return true;
        }

        public void Clear()
        {
            placedRunes.Clear();
        }

        public List<SpellComponentType> GetRuneSequence()
        {
            return placedRunes.Select(r => r.componentType).ToList();
        }
    }

    /// <summary>
    /// Configuration for the spell crafting system.
    /// </summary>
    [CreateAssetMenu(fileName = "SpellCraftingConfig", menuName = "UsefulScripts/RPG/Spell Crafting Config")]
    public class SpellCraftingConfig : ScriptableObject
    {
        [Header("General Settings")]
        public float baseCraftingTime = 1.5f;
        public bool allowMultipleCraftingStyles = true;
        public int maxInventorySpells = 50;

        [Header("Quality Settings")]
        public float baseQualityChance = 50f;
        public float qualityPerLevel = 0.5f;
        public float criticalChanceBase = 5f;
        public float criticalQualityBonus = 25f;

        [Header("Experience Settings")]
        public float experienceMultiplier = 1f;
        public float bonusExpForQuality = 0.5f;
        public float bonusExpForDiscovery = 2f;

        [Header("Discovery Settings")]
        public bool enableRecipeDiscovery = true;
        public float discoveryBaseChance = 0.03f;
        public float discoveryChancePerLevel = 0.002f;

        [Header("Freestyle Settings")]
        public int freestyleMinComponents = 2;
        public int freestyleMaxComponents = 6;
        public float freestylePowerVariance = 0.2f;

        [Header("Components")]
        public List<SpellComponent> allComponents = new List<SpellComponent>();

        [Header("Recipes")]
        public List<SpellRecipe> allRecipes = new List<SpellRecipe>();

        [Header("Rune Patterns")]
        public List<RunePattern> runePatterns = new List<RunePattern>();
    }

    /// <summary>
    /// Progress tracking for spell crafting.
    /// </summary>
    [Serializable]
    public class SpellCraftingProgress
    {
        public int currentLevel = 1;
        public int maxLevel = 100;
        public int currentExperience = 0;
        public int experienceToNextLevel = 100;
        public int totalSpellsCrafted = 0;
        public int criticalCrafts = 0;
        public int legendarySpellsCrafted = 0;
        public int recipesDiscovered = 0;
        public List<string> learnedRecipes = new List<string>();
        public List<string> discoveredRecipes = new List<string>();
        public List<string> unlockedComponents = new List<string>();

        public event Action<int, int> OnLevelUp;
        public event Action<string> OnRecipeLearned;
        public event Action<string> OnComponentUnlocked;

        public float ExperienceProgress => (float)currentExperience / experienceToNextLevel;
        public bool IsMaxLevel => currentLevel >= maxLevel;

        /// <summary>
        /// Adds experience.
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
            return (int)(100 * Mathf.Pow(level, 1.4f));
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
        /// Unlocks a component.
        /// </summary>
        public bool UnlockComponent(string componentId)
        {
            if (unlockedComponents.Contains(componentId)) return false;
            unlockedComponents.Add(componentId);
            OnComponentUnlocked?.Invoke(componentId);
            return true;
        }

        /// <summary>
        /// Checks if a recipe has been learned.
        /// </summary>
        public bool HasRecipe(string recipeId)
        {
            return learnedRecipes.Contains(recipeId);
        }

        /// <summary>
        /// Checks if a component is unlocked.
        /// </summary>
        public bool HasComponent(string componentId)
        {
            return unlockedComponents.Contains(componentId);
        }
    }

    /// <summary>
    /// Complete spell crafting system supporting multiple crafting styles.
    /// </summary>
    public class SpellCraftingSystem : MonoBehaviour
    {
        public static SpellCraftingSystem Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private SpellCraftingConfig config;

        [Header("Current State")]
        [SerializeField] private SpellCraftingStyle currentStyle = SpellCraftingStyle.ThreeSlot;
        [SerializeField] private bool isCrafting;

        // Crafting states for different styles
        private ThreeSlotCraftingState threeSlotState = new ThreeSlotCraftingState();
        private FreestyleCraftingState freestyleState = new FreestyleCraftingState();
        private RuneCraftingState runeState = new RuneCraftingState();

        // Runtime data
        private SpellCraftingProgress progress = new SpellCraftingProgress();
        private List<CraftedSpell> craftedSpells = new List<CraftedSpell>();
        private Dictionary<string, int> craftingHistory = new Dictionary<string, int>();
        private System.Random random = new System.Random();

        // Delegates for inventory integration
        public Func<string, int> GetComponentCount;
        public Action<string, int> ConsumeComponent;
        public Action<CraftedSpell> AddSpellToInventory;

        // Events
        public event Action<SpellCraftingResult> OnSpellCrafted;
        public event Action<SpellCraftingStyle> OnCraftingStyleChanged;
        public event Action<SpellComponent, SpellComponentCategory> OnComponentAdded;
        public event Action<SpellComponent, SpellComponentCategory> OnComponentRemoved;
        public event Action<SpellRecipe> OnRecipeLearned;
        public event Action<SpellRecipe> OnRecipeDiscovered;
        public event Action<int, int> OnLevelUp;
        public event Action<SpellComponent> OnComponentUnlocked;

        // Properties
        public bool IsCrafting => isCrafting;
        public SpellCraftingStyle CurrentStyle => currentStyle;
        public SpellCraftingProgress Progress => progress;
        public IReadOnlyList<CraftedSpell> CraftedSpells => craftedSpells.AsReadOnly();
        public ThreeSlotCraftingState ThreeSlotState => threeSlotState;
        public FreestyleCraftingState FreestyleState => freestyleState;
        public RuneCraftingState RuneState => runeState;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Initialize();
        }

        private void Initialize()
        {
            // Set up progress event handlers
            progress.OnLevelUp += (prev, curr) => OnLevelUp?.Invoke(prev, curr);
            progress.OnRecipeLearned += (id) =>
            {
                var recipe = GetRecipe(id);
                if (recipe != null) OnRecipeLearned?.Invoke(recipe);
            };
            progress.OnComponentUnlocked += (id) =>
            {
                var component = GetComponent(id);
                if (component != null) OnComponentUnlocked?.Invoke(component);
            };

            // Initialize freestyle state
            if (config != null)
            {
                freestyleState.maxComponents = config.freestyleMaxComponents;
            }

            // Unlock default components and recipes
            if (config != null)
            {
                foreach (var component in config.allComponents.Where(c => c.isUnlockedByDefault))
                {
                    progress.UnlockComponent(component.componentId);
                }

                foreach (var recipe in config.allRecipes.Where(r => r.isUnlockedByDefault))
                {
                    progress.LearnRecipe(recipe.recipeId);
                }
            }
        }

        #region Style Management

        /// <summary>
        /// Sets the current crafting style.
        /// </summary>
        public void SetCraftingStyle(SpellCraftingStyle style)
        {
            if (currentStyle == style) return;
            if (!config?.allowMultipleCraftingStyles ?? false) return;

            currentStyle = style;
            OnCraftingStyleChanged?.Invoke(style);
        }

        /// <summary>
        /// Gets the current crafting state based on active style.
        /// </summary>
        public List<SpellComponent> GetCurrentComponents()
        {
            return currentStyle switch
            {
                SpellCraftingStyle.ThreeSlot => threeSlotState.GetComponents(),
                SpellCraftingStyle.Freestyle => new List<SpellComponent>(freestyleState.components),
                SpellCraftingStyle.RuneBased => new List<SpellComponent>(runeState.placedRunes),
                _ => new List<SpellComponent>()
            };
        }

        /// <summary>
        /// Clears the current crafting state.
        /// </summary>
        public void ClearCurrentState()
        {
            switch (currentStyle)
            {
                case SpellCraftingStyle.ThreeSlot:
                    threeSlotState.Clear();
                    break;
                case SpellCraftingStyle.Freestyle:
                    freestyleState.Clear();
                    break;
                case SpellCraftingStyle.RuneBased:
                    runeState.Clear();
                    break;
            }
        }

        #endregion

        #region Component Management

        /// <summary>
        /// Gets a component by ID.
        /// </summary>
        public SpellComponent GetComponent(string componentId)
        {
            return config?.allComponents.FirstOrDefault(c => c.componentId == componentId);
        }

        /// <summary>
        /// Gets all components of a specific category.
        /// </summary>
        public List<SpellComponent> GetComponentsByCategory(SpellComponentCategory category)
        {
            return config?.allComponents.Where(c => c.GetCategory() == category).ToList()
                   ?? new List<SpellComponent>();
        }

        /// <summary>
        /// Gets all unlocked components.
        /// </summary>
        public List<SpellComponent> GetUnlockedComponents()
        {
            if (config == null) return new List<SpellComponent>();
            return config.allComponents.Where(c => progress.HasComponent(c.componentId)).ToList();
        }

        /// <summary>
        /// Gets all available components (unlocked and meeting level requirement).
        /// </summary>
        public List<SpellComponent> GetAvailableComponents()
        {
            if (config == null) return new List<SpellComponent>();
            return config.allComponents.Where(c =>
                progress.HasComponent(c.componentId) &&
                c.requiredLevel <= progress.currentLevel
            ).ToList();
        }

        /// <summary>
        /// Adds a component to the current crafting state.
        /// </summary>
        public bool AddComponent(SpellComponent component)
        {
            if (component == null) return false;
            if (!progress.HasComponent(component.componentId)) return false;

            bool added = false;
            SpellComponentCategory category = component.GetCategory();

            switch (currentStyle)
            {
                case SpellCraftingStyle.ThreeSlot:
                    added = AddToThreeSlot(component);
                    break;
                case SpellCraftingStyle.Freestyle:
                    added = AddToFreestyle(component);
                    break;
                case SpellCraftingStyle.RuneBased:
                    added = AddToRuneState(component);
                    break;
            }

            if (added)
            {
                OnComponentAdded?.Invoke(component, category);
            }

            return added;
        }

        /// <summary>
        /// Adds a component by ID.
        /// </summary>
        public bool AddComponentById(string componentId)
        {
            var component = GetComponent(componentId);
            return AddComponent(component);
        }

        private bool AddToThreeSlot(SpellComponent component)
        {
            var category = component.GetCategory();

            switch (category)
            {
                case SpellComponentCategory.Element:
                    if (threeSlotState.elementSlot != null) return false;
                    threeSlotState.elementSlot = component;
                    return true;
                case SpellComponentCategory.Form:
                    if (threeSlotState.formSlot != null) return false;
                    threeSlotState.formSlot = component;
                    return true;
                case SpellComponentCategory.Modifier:
                    if (threeSlotState.modifierSlot != null) return false;
                    threeSlotState.modifierSlot = component;
                    return true;
                default:
                    return false;
            }
        }

        private bool AddToFreestyle(SpellComponent component)
        {
            if (freestyleState.IsFull) return false;

            // Check compatibility with existing components
            foreach (var existing in freestyleState.components)
            {
                if (!component.IsCompatibleWith(existing) || !existing.IsCompatibleWith(component))
                {
                    return false;
                }
            }

            return freestyleState.AddComponent(component);
        }

        private bool AddToRuneState(SpellComponent component)
        {
            if (component.GetCategory() != SpellComponentCategory.Rune) return false;
            return runeState.AddRune(component);
        }

        /// <summary>
        /// Removes a component from the current crafting state.
        /// </summary>
        public bool RemoveComponent(SpellComponent component)
        {
            if (component == null) return false;

            bool removed = false;
            SpellComponentCategory category = component.GetCategory();

            switch (currentStyle)
            {
                case SpellCraftingStyle.ThreeSlot:
                    removed = RemoveFromThreeSlot(component);
                    break;
                case SpellCraftingStyle.Freestyle:
                    removed = freestyleState.RemoveComponent(component);
                    break;
                case SpellCraftingStyle.RuneBased:
                    // Rune crafting only allows removing the last rune
                    if (runeState.placedRunes.LastOrDefault() == component)
                    {
                        removed = runeState.RemoveLastRune();
                    }
                    break;
            }

            if (removed)
            {
                OnComponentRemoved?.Invoke(component, category);
            }

            return removed;
        }

        private bool RemoveFromThreeSlot(SpellComponent component)
        {
            if (threeSlotState.elementSlot == component)
            {
                threeSlotState.elementSlot = null;
                return true;
            }
            if (threeSlotState.formSlot == component)
            {
                threeSlotState.formSlot = null;
                return true;
            }
            if (threeSlotState.modifierSlot == component)
            {
                threeSlotState.modifierSlot = null;
                return true;
            }
            return false;
        }

        #endregion

        #region Recipe Management

        /// <summary>
        /// Gets a recipe by ID.
        /// </summary>
        public SpellRecipe GetRecipe(string recipeId)
        {
            return config?.allRecipes.FirstOrDefault(r => r.recipeId == recipeId);
        }

        /// <summary>
        /// Gets all learned recipes.
        /// </summary>
        public List<SpellRecipe> GetLearnedRecipes()
        {
            if (config == null) return new List<SpellRecipe>();
            return config.allRecipes.Where(r => progress.HasRecipe(r.recipeId)).ToList();
        }

        /// <summary>
        /// Gets recipes that match the current components.
        /// </summary>
        public List<SpellRecipe> GetMatchingRecipes()
        {
            var components = GetCurrentComponents();
            if (components.Count == 0) return new List<SpellRecipe>();

            return GetLearnedRecipes().Where(r =>
                r.craftingStyle == currentStyle &&
                r.CanCraft(components, progress.currentLevel)
            ).ToList();
        }

        /// <summary>
        /// Learns a recipe.
        /// </summary>
        public bool LearnRecipe(string recipeId)
        {
            var recipe = GetRecipe(recipeId);
            if (recipe == null) return false;

            return progress.LearnRecipe(recipeId);
        }

        #endregion

        #region Spell Crafting

        /// <summary>
        /// Checks if crafting is possible with current components.
        /// </summary>
        public bool CanCraft()
        {
            var components = GetCurrentComponents();

            switch (currentStyle)
            {
                case SpellCraftingStyle.ThreeSlot:
                    return threeSlotState.IsComplete;

                case SpellCraftingStyle.Freestyle:
                    int minComponents = config?.freestyleMinComponents ?? 2;
                    return components.Count >= minComponents && AreAllCompatible(components);

                case SpellCraftingStyle.RuneBased:
                    return FindMatchingRunePattern() != null;

                default:
                    return false;
            }
        }

        private bool AreAllCompatible(List<SpellComponent> components)
        {
            for (int i = 0; i < components.Count; i++)
            {
                for (int j = i + 1; j < components.Count; j++)
                {
                    if (!components[i].IsCompatibleWith(components[j]) ||
                        !components[j].IsCompatibleWith(components[i]))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private RunePattern FindMatchingRunePattern()
        {
            if (config == null) return null;
            var sequence = runeState.GetRuneSequence();
            return config.runePatterns.FirstOrDefault(p => p.MatchesPattern(sequence));
        }

        /// <summary>
        /// Crafts a spell from the current components.
        /// </summary>
        public SpellCraftingResult CraftSpell()
        {
            var result = new SpellCraftingResult { styleUsed = currentStyle };

            if (!CanCraft())
            {
                result.success = false;
                result.failureReason = "Invalid crafting configuration";
                return result;
            }

            var components = GetCurrentComponents();
            result.usedComponentIds = components.Select(c => c.componentId).ToList();

            // Create the spell based on crafting style
            CraftedSpell spell = currentStyle switch
            {
                SpellCraftingStyle.ThreeSlot => CraftThreeSlotSpell(),
                SpellCraftingStyle.Freestyle => CraftFreestyleSpell(),
                SpellCraftingStyle.RuneBased => CraftRuneSpell(),
                _ => null
            };

            if (spell == null)
            {
                result.success = false;
                result.failureReason = "Failed to create spell";
                return result;
            }

            // Calculate quality
            float qualityRoll = CalculateQualityRoll(components);
            result.qualityRoll = qualityRoll;
            result.quality = DetermineQuality(qualityRoll);
            result.wasCritical = qualityRoll >= 100f;

            // Apply quality to spell
            spell.quality = result.quality;
            spell.qualityMultiplier = GetQualityMultiplier(result.quality);
            spell.isMasterwork = result.wasCritical;
            spell.description = spell.GenerateDescription();

            // Consume components
            foreach (var component in components)
            {
                ConsumeComponent?.Invoke(component.componentId, 1);
            }

            // Calculate experience
            int baseExp = GetBaseExperience(components);
            float expMultiplier = config?.experienceMultiplier ?? 1f;

            if (result.quality >= SpellQuality.Superior)
            {
                expMultiplier *= 1f + (config?.bonusExpForQuality ?? 0.5f);
            }

            // First time crafting this combination?
            string comboKey = string.Join("_", result.usedComponentIds.OrderBy(x => x));
            if (!craftingHistory.ContainsKey(comboKey))
            {
                expMultiplier *= 2f; // Bonus for new combination
                craftingHistory[comboKey] = 0;
            }
            craftingHistory[comboKey]++;

            int expGained = (int)(baseExp * expMultiplier);
            progress.AddExperience(expGained);
            result.experienceGained = expGained;

            // Update stats
            progress.totalSpellsCrafted++;
            if (result.wasCritical) progress.criticalCrafts++;
            if (result.quality == SpellQuality.Legendary) progress.legendarySpellsCrafted++;

            // Try recipe discovery
            if (config?.enableRecipeDiscovery == true)
            {
                var discovered = TryDiscoverRecipe(components);
                if (discovered != null)
                {
                    result.discoveredNewRecipe = true;
                    result.discoveredRecipeId = discovered.recipeId;
                    progress.recipesDiscovered++;
                }
            }

            // Add to inventory
            craftedSpells.Add(spell);
            AddSpellToInventory?.Invoke(spell);

            result.success = true;
            result.craftedSpell = spell;

            // Clear crafting state
            ClearCurrentState();

            OnSpellCrafted?.Invoke(result);
            return result;
        }

        private CraftedSpell CraftThreeSlotSpell()
        {
            var element = threeSlotState.elementSlot;
            var form = threeSlotState.formSlot;
            var modifier = threeSlotState.modifierSlot;

            var spell = new CraftedSpell
            {
                spellId = Guid.NewGuid().ToString(),
                spellName = GenerateSpellName(element, form, modifier),
                power = CalculatePower(new List<SpellComponent> { element, form, modifier }),
                manaCost = CalculateManaCost(new List<SpellComponent> { element, form, modifier }),
                cooldown = 5f,
                range = 10f,
                damageType = GetDamageTypeFromElement(element.componentType),
                craftingStyle = SpellCraftingStyle.ThreeSlot,
                spellColor = GetSpellColor(element.componentType)
            };

            spell.componentIds = new List<string> { element.componentId, form.componentId, modifier.componentId };

            // Apply form modifiers
            ApplyFormModifiers(spell, form);

            // Apply modifier bonuses
            ApplyModifierBonuses(spell, modifier);

            return spell;
        }

        private CraftedSpell CraftFreestyleSpell()
        {
            var components = freestyleState.components;

            // Find dominant element
            var elements = components.Where(c => c.GetCategory() == SpellComponentCategory.Element).ToList();
            var forms = components.Where(c => c.GetCategory() == SpellComponentCategory.Form).ToList();
            var modifiers = components.Where(c => c.GetCategory() == SpellComponentCategory.Modifier).ToList();

            var primaryElement = elements.OrderByDescending(e => e.basePower).FirstOrDefault();
            var primaryForm = forms.FirstOrDefault();

            var spell = new CraftedSpell
            {
                spellId = Guid.NewGuid().ToString(),
                spellName = GenerateFreestyleName(components),
                power = CalculatePower(components),
                manaCost = CalculateManaCost(components),
                cooldown = 5f + (components.Count * 0.5f),
                range = 10f,
                damageType = primaryElement != null ? GetDamageTypeFromElement(primaryElement.componentType) : DamageType.Physical,
                craftingStyle = SpellCraftingStyle.Freestyle,
                spellColor = primaryElement != null ? GetSpellColor(primaryElement.componentType) : Color.white
            };

            spell.componentIds = components.Select(c => c.componentId).ToList();

            // Apply power variance for freestyle
            float variance = config?.freestylePowerVariance ?? 0.2f;
            float varianceRoll = 1f + ((float)(random.NextDouble() * 2 - 1) * variance);
            spell.power *= varianceRoll;

            // Apply form effects
            if (primaryForm != null)
            {
                ApplyFormModifiers(spell, primaryForm);
            }

            // Apply all modifier bonuses
            foreach (var mod in modifiers)
            {
                ApplyModifierBonuses(spell, mod);
            }

            return spell;
        }

        private CraftedSpell CraftRuneSpell()
        {
            var pattern = FindMatchingRunePattern();
            if (pattern == null) return null;

            var runes = runeState.placedRunes;

            var spell = new CraftedSpell
            {
                spellId = pattern.resultSpellId ?? Guid.NewGuid().ToString(),
                spellName = pattern.patternName,
                power = CalculatePower(runes) * (1f + pattern.powerBonus),
                manaCost = CalculateManaCost(runes) * (1f - pattern.manaReduction),
                cooldown = 5f,
                range = 10f,
                damageType = DamageType.Arcane,
                craftingStyle = SpellCraftingStyle.RuneBased,
                spellColor = new Color(0.5f, 0f, 1f) // Purple for rune magic
            };

            spell.componentIds = runes.Select(r => r.componentId).ToList();

            return spell;
        }

        private string GenerateSpellName(SpellComponent element, SpellComponent form, SpellComponent modifier)
        {
            string elementName = element.componentType.ToString().Replace("Element", "");
            string formName = form.componentType.ToString().Replace("Form", "");
            return $"{elementName} {formName}";
        }

        private string GenerateFreestyleName(List<SpellComponent> components)
        {
            var elements = components.Where(c => c.GetCategory() == SpellComponentCategory.Element).ToList();
            var forms = components.Where(c => c.GetCategory() == SpellComponentCategory.Form).ToList();

            string name = "Spell of ";

            if (elements.Count > 0)
            {
                name += string.Join(" and ", elements.Select(e =>
                    e.componentType.ToString().Replace("Element", "")));
            }

            if (forms.Count > 0)
            {
                name += " " + forms[0].componentType.ToString().Replace("Form", "");
            }

            return name;
        }

        private float CalculatePower(List<SpellComponent> components)
        {
            float power = 0f;
            float multiplier = 1f;

            foreach (var comp in components)
            {
                power += comp.basePower;
                multiplier *= comp.powerMultiplier;
            }

            return power * multiplier;
        }

        private float CalculateManaCost(List<SpellComponent> components)
        {
            float cost = 0f;
            float multiplier = 1f;

            foreach (var comp in components)
            {
                cost += comp.baseManaCost;
                multiplier *= comp.manaMultiplier;
            }

            return cost * multiplier;
        }

        private DamageType GetDamageTypeFromElement(SpellComponentType elementType)
        {
            return elementType switch
            {
                SpellComponentType.ElementFire => DamageType.Fire,
                SpellComponentType.ElementIce => DamageType.Ice,
                SpellComponentType.ElementLightning => DamageType.Lightning,
                SpellComponentType.ElementEarth => DamageType.Physical,
                SpellComponentType.ElementWater => DamageType.Water,
                SpellComponentType.ElementWind => DamageType.Wind,
                SpellComponentType.ElementLight => DamageType.Holy,
                SpellComponentType.ElementDark => DamageType.Dark,
                SpellComponentType.ElementArcane => DamageType.Arcane,
                SpellComponentType.ElementNature => DamageType.Poison,
                _ => DamageType.Physical
            };
        }

        private Color GetSpellColor(SpellComponentType elementType)
        {
            return elementType switch
            {
                SpellComponentType.ElementFire => new Color(1f, 0.3f, 0f),
                SpellComponentType.ElementIce => new Color(0.5f, 0.8f, 1f),
                SpellComponentType.ElementLightning => new Color(1f, 1f, 0.3f),
                SpellComponentType.ElementEarth => new Color(0.6f, 0.4f, 0.2f),
                SpellComponentType.ElementWater => new Color(0f, 0.5f, 1f),
                SpellComponentType.ElementWind => new Color(0.8f, 1f, 0.8f),
                SpellComponentType.ElementLight => new Color(1f, 1f, 0.8f),
                SpellComponentType.ElementDark => new Color(0.3f, 0f, 0.3f),
                SpellComponentType.ElementArcane => new Color(0.8f, 0.3f, 1f),
                SpellComponentType.ElementNature => new Color(0.3f, 0.8f, 0.3f),
                _ => Color.white
            };
        }

        private void ApplyFormModifiers(CraftedSpell spell, SpellComponent form)
        {
            switch (form.componentType)
            {
                case SpellComponentType.FormProjectile:
                    spell.range = 20f;
                    break;
                case SpellComponentType.FormBeam:
                    spell.range = 15f;
                    spell.power *= 1.2f;
                    break;
                case SpellComponentType.FormAura:
                    spell.areaOfEffect = 5f;
                    spell.range = 0f;
                    break;
                case SpellComponentType.FormExplosion:
                    spell.areaOfEffect = 8f;
                    spell.power *= 1.5f;
                    spell.manaCost *= 1.3f;
                    break;
                case SpellComponentType.FormWall:
                    spell.duration = 10f;
                    spell.range = 5f;
                    break;
                case SpellComponentType.FormBuff:
                    spell.duration = 30f;
                    spell.range = 0f;
                    break;
                case SpellComponentType.FormDebuff:
                    spell.duration = 15f;
                    spell.range = 15f;
                    break;
                case SpellComponentType.FormHeal:
                    spell.range = 10f;
                    break;
                case SpellComponentType.FormTrap:
                    spell.duration = 60f;
                    spell.range = 5f;
                    break;
                case SpellComponentType.FormSummon:
                    spell.duration = 120f;
                    spell.manaCost *= 2f;
                    break;
            }
        }

        private void ApplyModifierBonuses(CraftedSpell spell, SpellComponent modifier)
        {
            switch (modifier.componentType)
            {
                case SpellComponentType.ModifierPower:
                    spell.power *= 1.5f;
                    spell.manaCost *= 1.2f;
                    break;
                case SpellComponentType.ModifierSpeed:
                    spell.cooldown *= 0.7f;
                    break;
                case SpellComponentType.ModifierRange:
                    spell.range *= 1.5f;
                    break;
                case SpellComponentType.ModifierDuration:
                    spell.duration *= 2f;
                    break;
                case SpellComponentType.ModifierArea:
                    spell.areaOfEffect *= 1.5f;
                    break;
                case SpellComponentType.ModifierPiercing:
                    spell.power *= 1.2f;
                    break;
                case SpellComponentType.ModifierHoming:
                    // Handled by spell behavior
                    break;
                case SpellComponentType.ModifierChain:
                    spell.power *= 0.8f; // Reduced power but chains
                    break;
                case SpellComponentType.ModifierSplit:
                    spell.power *= 0.6f; // Split reduces individual power
                    break;
                case SpellComponentType.ModifierVampirism:
                    spell.manaCost *= 1.4f;
                    break;
            }
        }

        private int GetBaseExperience(List<SpellComponent> components)
        {
            int exp = 10;

            foreach (var comp in components)
            {
                exp += comp.rarity switch
                {
                    SpellComponentRarity.Common => 5,
                    SpellComponentRarity.Uncommon => 10,
                    SpellComponentRarity.Rare => 20,
                    SpellComponentRarity.Epic => 40,
                    SpellComponentRarity.Legendary => 80,
                    SpellComponentRarity.Mythic => 150,
                    _ => 5
                };
            }

            return exp;
        }

        #endregion

        #region Quality Calculation

        private float CalculateQualityRoll(List<SpellComponent> components)
        {
            float baseChance = config?.baseQualityChance ?? 50f;
            float levelBonus = progress.currentLevel * (config?.qualityPerLevel ?? 0.5f);

            // Rarity bonus
            float rarityBonus = components.Sum(c => c.rarity switch
            {
                SpellComponentRarity.Uncommon => 2f,
                SpellComponentRarity.Rare => 5f,
                SpellComponentRarity.Epic => 10f,
                SpellComponentRarity.Legendary => 20f,
                SpellComponentRarity.Mythic => 35f,
                _ => 0f
            });

            float totalChance = baseChance + levelBonus + rarityBonus;

            // Random variance
            float roll = (float)(random.NextDouble() * 40f - 20f);
            totalChance += roll;

            // Critical check
            float critChance = config?.criticalChanceBase ?? 5f;
            if (random.NextDouble() * 100 < critChance)
            {
                totalChance += config?.criticalQualityBonus ?? 25f;
            }

            return Mathf.Clamp(totalChance, 0f, 120f);
        }

        private SpellQuality DetermineQuality(float roll)
        {
            if (roll >= 100f) return SpellQuality.Legendary;
            if (roll >= 90f) return SpellQuality.Masterwork;
            if (roll >= 75f) return SpellQuality.Exceptional;
            if (roll >= 60f) return SpellQuality.Superior;
            if (roll >= 40f) return SpellQuality.Fine;
            if (roll >= 20f) return SpellQuality.Normal;
            return SpellQuality.Poor;
        }

        private float GetQualityMultiplier(SpellQuality quality)
        {
            return quality switch
            {
                SpellQuality.Poor => 0.6f,
                SpellQuality.Normal => 1f,
                SpellQuality.Fine => 1.1f,
                SpellQuality.Superior => 1.25f,
                SpellQuality.Exceptional => 1.5f,
                SpellQuality.Masterwork => 1.75f,
                SpellQuality.Legendary => 2f,
                _ => 1f
            };
        }

        #endregion

        #region Discovery

        private SpellRecipe TryDiscoverRecipe(List<SpellComponent> components)
        {
            float chance = (config?.discoveryBaseChance ?? 0.03f) +
                          progress.currentLevel * (config?.discoveryChancePerLevel ?? 0.002f);

            if (random.NextDouble() > chance) return null;

            // Find an undiscovered recipe that could be made with similar components
            var undiscovered = config?.allRecipes
                .Where(r =>
                    !progress.HasRecipe(r.recipeId) &&
                    !progress.discoveredRecipes.Contains(r.recipeId) &&
                    r.requiredLevel <= progress.currentLevel + 5 &&
                    r.craftingStyle == currentStyle)
                .ToList();

            if (undiscovered == null || undiscovered.Count == 0) return null;

            var discovered = undiscovered[random.Next(undiscovered.Count)];
            progress.discoveredRecipes.Add(discovered.recipeId);
            progress.LearnRecipe(discovered.recipeId);
            OnRecipeDiscovered?.Invoke(discovered);

            return discovered;
        }

        #endregion

        #region Preview

        /// <summary>
        /// Gets a preview of what spell would be created without actually crafting.
        /// </summary>
        public CraftedSpell PreviewSpell()
        {
            if (!CanCraft()) return null;

            return currentStyle switch
            {
                SpellCraftingStyle.ThreeSlot => CraftThreeSlotSpell(),
                SpellCraftingStyle.Freestyle => CraftFreestyleSpell(),
                SpellCraftingStyle.RuneBased => CraftRuneSpell(),
                _ => null
            };
        }

        /// <summary>
        /// Gets potential quality range for current components.
        /// </summary>
        public (SpellQuality min, SpellQuality max, SpellQuality likely) GetQualityPrediction()
        {
            var components = GetCurrentComponents();
            if (components.Count == 0)
            {
                return (SpellQuality.Poor, SpellQuality.Poor, SpellQuality.Poor);
            }

            float baseChance = config?.baseQualityChance ?? 50f;
            float levelBonus = progress.currentLevel * (config?.qualityPerLevel ?? 0.5f);

            float rarityBonus = components.Sum(c => c.rarity switch
            {
                SpellComponentRarity.Uncommon => 2f,
                SpellComponentRarity.Rare => 5f,
                SpellComponentRarity.Epic => 10f,
                SpellComponentRarity.Legendary => 20f,
                SpellComponentRarity.Mythic => 35f,
                _ => 0f
            });

            float baseTotal = baseChance + levelBonus + rarityBonus;

            float minRoll = baseTotal - 20f;
            float maxRoll = baseTotal + 20f + (config?.criticalQualityBonus ?? 25f);
            float likelyRoll = baseTotal;

            return (DetermineQuality(minRoll), DetermineQuality(maxRoll), DetermineQuality(likelyRoll));
        }

        #endregion

        #region Utility

        /// <summary>
        /// Gets a summary of spell crafting statistics.
        /// </summary>
        public string GetCraftingSummary()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== Spell Crafting Summary ===");
            sb.AppendLine();
            sb.AppendLine($"Level: {progress.currentLevel}/{progress.maxLevel}");
            sb.AppendLine($"EXP: {progress.currentExperience}/{progress.experienceToNextLevel}");
            sb.AppendLine($"Recipes Learned: {progress.learnedRecipes.Count}");
            sb.AppendLine($"Recipes Discovered: {progress.recipesDiscovered}");
            sb.AppendLine($"Components Unlocked: {progress.unlockedComponents.Count}");
            sb.AppendLine();
            sb.AppendLine($"Total Spells Crafted: {progress.totalSpellsCrafted}");
            sb.AppendLine($"Critical Crafts: {progress.criticalCrafts}");
            sb.AppendLine($"Legendary Spells: {progress.legendarySpellsCrafted}");
            sb.AppendLine();
            sb.AppendLine($"Current Style: {currentStyle}");
            sb.AppendLine($"Spells in Inventory: {craftedSpells.Count}");

            return sb.ToString();
        }

        /// <summary>
        /// Creates save data.
        /// </summary>
        public SpellCraftingSystemSaveData CreateSaveData()
        {
            return new SpellCraftingSystemSaveData
            {
                currentLevel = progress.currentLevel,
                currentExperience = progress.currentExperience,
                totalSpellsCrafted = progress.totalSpellsCrafted,
                criticalCrafts = progress.criticalCrafts,
                legendarySpellsCrafted = progress.legendarySpellsCrafted,
                recipesDiscovered = progress.recipesDiscovered,
                learnedRecipes = new List<string>(progress.learnedRecipes),
                discoveredRecipes = new List<string>(progress.discoveredRecipes),
                unlockedComponents = new List<string>(progress.unlockedComponents),
                craftingHistory = new Dictionary<string, int>(craftingHistory),
                craftedSpells = craftedSpells.Select(s => new CraftedSpellSaveData
                {
                    spellId = s.spellId,
                    spellName = s.spellName,
                    description = s.description,
                    power = s.power,
                    manaCost = s.manaCost,
                    cooldown = s.cooldown,
                    range = s.range,
                    areaOfEffect = s.areaOfEffect,
                    duration = s.duration,
                    damageType = s.damageType.ToString(),
                    quality = s.quality.ToString(),
                    isMasterwork = s.isMasterwork,
                    qualityMultiplier = s.qualityMultiplier,
                    componentIds = new List<string>(s.componentIds),
                    craftingStyle = s.craftingStyle.ToString()
                }).ToList()
            };
        }

        /// <summary>
        /// Loads from save data.
        /// </summary>
        public void LoadSaveData(SpellCraftingSystemSaveData saveData)
        {
            if (saveData == null) return;

            progress.currentLevel = saveData.currentLevel;
            progress.currentExperience = saveData.currentExperience;
            progress.experienceToNextLevel = progress.CalculateExpForLevel(progress.currentLevel + 1);
            progress.totalSpellsCrafted = saveData.totalSpellsCrafted;
            progress.criticalCrafts = saveData.criticalCrafts;
            progress.legendarySpellsCrafted = saveData.legendarySpellsCrafted;
            progress.recipesDiscovered = saveData.recipesDiscovered;
            progress.learnedRecipes = new List<string>(saveData.learnedRecipes);
            progress.discoveredRecipes = new List<string>(saveData.discoveredRecipes);
            progress.unlockedComponents = new List<string>(saveData.unlockedComponents);

            craftingHistory = new Dictionary<string, int>(saveData.craftingHistory);

            craftedSpells.Clear();
            foreach (var spellData in saveData.craftedSpells)
            {
                var spell = new CraftedSpell
                {
                    spellId = spellData.spellId,
                    spellName = spellData.spellName,
                    description = spellData.description,
                    power = spellData.power,
                    manaCost = spellData.manaCost,
                    cooldown = spellData.cooldown,
                    range = spellData.range,
                    areaOfEffect = spellData.areaOfEffect,
                    duration = spellData.duration,
                    isMasterwork = spellData.isMasterwork,
                    qualityMultiplier = spellData.qualityMultiplier,
                    componentIds = new List<string>(spellData.componentIds)
                };

                if (Enum.TryParse<DamageType>(spellData.damageType, out var damageType))
                {
                    spell.damageType = damageType;
                }
                if (Enum.TryParse<SpellQuality>(spellData.quality, out var quality))
                {
                    spell.quality = quality;
                }
                if (Enum.TryParse<SpellCraftingStyle>(spellData.craftingStyle, out var style))
                {
                    spell.craftingStyle = style;
                }

                craftedSpells.Add(spell);
            }
        }

        #endregion
    }

    /// <summary>
    /// Serializable save data for spell crafting system.
    /// </summary>
    [Serializable]
    public class SpellCraftingSystemSaveData
    {
        public int currentLevel;
        public int currentExperience;
        public int totalSpellsCrafted;
        public int criticalCrafts;
        public int legendarySpellsCrafted;
        public int recipesDiscovered;
        public List<string> learnedRecipes;
        public List<string> discoveredRecipes;
        public List<string> unlockedComponents;
        public Dictionary<string, int> craftingHistory;
        public List<CraftedSpellSaveData> craftedSpells;
    }

    /// <summary>
    /// Serializable save data for a crafted spell.
    /// </summary>
    [Serializable]
    public class CraftedSpellSaveData
    {
        public string spellId;
        public string spellName;
        public string description;
        public float power;
        public float manaCost;
        public float cooldown;
        public float range;
        public float areaOfEffect;
        public float duration;
        public string damageType;
        public string quality;
        public bool isMasterwork;
        public float qualityMultiplier;
        public List<string> componentIds;
        public string craftingStyle;
    }
}
