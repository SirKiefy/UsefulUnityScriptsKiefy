using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UsefulScripts.RPG
{
    /// <summary>
    /// Defines the primary attributes used in RPG character stats.
    /// </summary>
    public enum PrimaryAttribute
    {
        Strength,       // Physical damage, carry capacity
        Dexterity,      // Speed, evasion, critical chance
        Constitution,   // Health, defense, stamina
        Intelligence,   // Magic damage, mana pool
        Wisdom,         // Magic defense, mana regen, skill effects
        Charisma,       // NPC relations, prices, party buffs
        Luck            // Critical damage, drop rates, random bonuses
    }

    /// <summary>
    /// Defines derived/secondary stats calculated from primary attributes.
    /// </summary>
    public enum DerivedStat
    {
        MaxHealth,
        MaxMana,
        MaxStamina,
        PhysicalAttack,
        MagicalAttack,
        PhysicalDefense,
        MagicalDefense,
        Speed,
        Evasion,
        Accuracy,
        CriticalChance,
        CriticalDamage,
        HealthRegen,
        ManaRegen,
        StaminaRegen,
        BlockChance,
        ParryChance,
        LifeSteal,
        ManaSteal,
        ExperienceBonus,
        GoldBonus,
        DropRateBonus,
        CooldownReduction,
        CastSpeed,
        AttackSpeed,
        MovementSpeed,
        ResistFire,
        ResistIce,
        ResistLightning,
        ResistEarth,
        ResistLight,
        ResistDark,
        ResistPoison,
        ResistStun,
        ResistSilence,
        ResistSlow
    }

    /// <summary>
    /// Represents a stat modifier with source tracking and expiration.
    /// </summary>
    [Serializable]
    public class StatModifier
    {
        public string modifierId;
        public string sourceName;
        public ModifierType modifierType;
        public float value;
        public float duration;
        public float remainingDuration;
        public int priority;
        public bool isPermanent;
        public object source;

        public event Action<StatModifier> OnExpire;

        public bool IsExpired => !isPermanent && remainingDuration <= 0;

        public StatModifier(string id, string source, ModifierType type, float val, float dur = -1f, int prio = 0)
        {
            modifierId = id;
            sourceName = source;
            modifierType = type;
            value = val;
            duration = dur;
            remainingDuration = dur;
            priority = prio;
            isPermanent = dur < 0;
        }

        public void Update(float deltaTime)
        {
            if (!isPermanent)
            {
                remainingDuration -= deltaTime;
                if (remainingDuration <= 0)
                {
                    OnExpire?.Invoke(this);
                }
            }
        }
    }

    public enum ModifierType
    {
        Flat,               // +10
        PercentAdd,         // +10% (additive with other percent adds)
        PercentMultiply     // *1.1 (multiplicative)
    }

    /// <summary>
    /// Represents a character's experience and level progression.
    /// </summary>
    [Serializable]
    public class LevelProgression
    {
        public int currentLevel = 1;
        public int maxLevel = 100;
        public long currentExperience = 0;
        public long experienceToNextLevel = 100;
        public int unspentAttributePoints = 0;
        public int attributePointsPerLevel = 5;

        public event Action<int, int> OnLevelUp;
        public event Action<long, long> OnExperienceGained;

        public float ExperienceProgress => (float)currentExperience / experienceToNextLevel;
        public bool IsMaxLevel => currentLevel >= maxLevel;

        /// <summary>
        /// Adds experience and handles level ups.
        /// </summary>
        public int AddExperience(long amount)
        {
            if (IsMaxLevel) return 0;

            long previousExp = currentExperience;
            currentExperience += amount;
            int levelsGained = 0;

            while (currentExperience >= experienceToNextLevel && !IsMaxLevel)
            {
                currentExperience -= experienceToNextLevel;
                int previousLevel = currentLevel;
                currentLevel++;
                levelsGained++;
                unspentAttributePoints += attributePointsPerLevel;
                experienceToNextLevel = CalculateExpForLevel(currentLevel + 1);
                OnLevelUp?.Invoke(previousLevel, currentLevel);
            }

            if (IsMaxLevel)
            {
                currentExperience = experienceToNextLevel;
            }

            OnExperienceGained?.Invoke(previousExp, currentExperience);
            return levelsGained;
        }

        /// <summary>
        /// Calculates the experience required to reach a specific level.
        /// Uses a scaling formula: base * level^exponent
        /// </summary>
        public long CalculateExpForLevel(int level)
        {
            // Standard RPG exponential curve with some adjustments
            return (long)(100 * Math.Pow(level, 2.5f) + 50 * level);
        }

        /// <summary>
        /// Gets total experience earned across all levels.
        /// </summary>
        public long GetTotalExperience()
        {
            long total = currentExperience;
            for (int i = 1; i < currentLevel; i++)
            {
                total += CalculateExpForLevel(i + 1);
            }
            return total;
        }
    }

    /// <summary>
    /// Comprehensive character stats configuration as a ScriptableObject.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCharacterStatsConfig", menuName = "UsefulScripts/RPG/Character Stats Config")]
    public class CharacterStatsConfig : ScriptableObject
    {
        [Header("Base Stat Values")]
        [SerializeField] private int baseStrength = 10;
        [SerializeField] private int baseDexterity = 10;
        [SerializeField] private int baseConstitution = 10;
        [SerializeField] private int baseIntelligence = 10;
        [SerializeField] private int baseWisdom = 10;
        [SerializeField] private int baseCharisma = 10;
        [SerializeField] private int baseLuck = 10;

        [Header("Stat Growth Per Level")]
        [SerializeField] private float strengthPerLevel = 1f;
        [SerializeField] private float dexterityPerLevel = 1f;
        [SerializeField] private float constitutionPerLevel = 1f;
        [SerializeField] private float intelligencePerLevel = 1f;
        [SerializeField] private float wisdomPerLevel = 1f;
        [SerializeField] private float charismaPerLevel = 0.5f;
        [SerializeField] private float luckPerLevel = 0.5f;

        [Header("Derived Stat Formulas")]
        [Tooltip("Health = Constitution * this value")]
        [SerializeField] private float healthPerConstitution = 10f;
        [Tooltip("Mana = Intelligence * this value")]
        [SerializeField] private float manaPerIntelligence = 5f;
        [Tooltip("Stamina = Constitution * this value")]
        [SerializeField] private float staminaPerConstitution = 5f;
        [Tooltip("Physical Attack = Strength * this value")]
        [SerializeField] private float attackPerStrength = 2f;
        [Tooltip("Magical Attack = Intelligence * this value")]
        [SerializeField] private float magicAttackPerIntelligence = 2f;
        [Tooltip("Physical Defense = Constitution * this value")]
        [SerializeField] private float defensePerConstitution = 1.5f;
        [Tooltip("Magic Defense = Wisdom * this value")]
        [SerializeField] private float magicDefensePerWisdom = 1.5f;
        [Tooltip("Speed = Dexterity * this value")]
        [SerializeField] private float speedPerDexterity = 1f;
        [Tooltip("Evasion percent per dexterity point")]
        [SerializeField] private float evasionPerDexterity = 0.5f;
        [Tooltip("Critical chance percent per dexterity point")]
        [SerializeField] private float critChancePerDexterity = 0.25f;
        [Tooltip("Critical chance percent per luck point")]
        [SerializeField] private float critChancePerLuck = 0.5f;
        [Tooltip("Critical damage percent per luck point")]
        [SerializeField] private float critDamagePerLuck = 1f;

        public int GetBaseAttribute(PrimaryAttribute attribute)
        {
            return attribute switch
            {
                PrimaryAttribute.Strength => baseStrength,
                PrimaryAttribute.Dexterity => baseDexterity,
                PrimaryAttribute.Constitution => baseConstitution,
                PrimaryAttribute.Intelligence => baseIntelligence,
                PrimaryAttribute.Wisdom => baseWisdom,
                PrimaryAttribute.Charisma => baseCharisma,
                PrimaryAttribute.Luck => baseLuck,
                _ => 10
            };
        }

        public float GetAttributeGrowthPerLevel(PrimaryAttribute attribute)
        {
            return attribute switch
            {
                PrimaryAttribute.Strength => strengthPerLevel,
                PrimaryAttribute.Dexterity => dexterityPerLevel,
                PrimaryAttribute.Constitution => constitutionPerLevel,
                PrimaryAttribute.Intelligence => intelligencePerLevel,
                PrimaryAttribute.Wisdom => wisdomPerLevel,
                PrimaryAttribute.Charisma => charismaPerLevel,
                PrimaryAttribute.Luck => luckPerLevel,
                _ => 1f
            };
        }

        public float GetDerivedStatMultiplier(DerivedStat stat)
        {
            return stat switch
            {
                DerivedStat.MaxHealth => healthPerConstitution,
                DerivedStat.MaxMana => manaPerIntelligence,
                DerivedStat.MaxStamina => staminaPerConstitution,
                DerivedStat.PhysicalAttack => attackPerStrength,
                DerivedStat.MagicalAttack => magicAttackPerIntelligence,
                DerivedStat.PhysicalDefense => defensePerConstitution,
                DerivedStat.MagicalDefense => magicDefensePerWisdom,
                DerivedStat.Speed => speedPerDexterity,
                DerivedStat.Evasion => evasionPerDexterity,
                DerivedStat.CriticalChance => critChancePerDexterity,
                DerivedStat.CriticalDamage => critDamagePerLuck,
                _ => 1f
            };
        }
    }

    /// <summary>
    /// Complete character stats system managing attributes, derived stats, modifiers, and leveling.
    /// Attach to any entity that needs RPG-style stats.
    /// </summary>
    public class CharacterStatsSystem : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private CharacterStatsConfig statsConfig;
        [SerializeField] private string characterName = "Character";
        [SerializeField] private string characterClass = "Adventurer";

        [Header("Current Resources")]
        [SerializeField] private float currentHealth;
        [SerializeField] private float currentMana;
        [SerializeField] private float currentStamina;

        [Header("Level Progression")]
        [SerializeField] private LevelProgression levelProgression = new LevelProgression();

        // Base attributes (before modifiers)
        private Dictionary<PrimaryAttribute, float> baseAttributes = new Dictionary<PrimaryAttribute, float>();
        private Dictionary<PrimaryAttribute, float> allocatedPoints = new Dictionary<PrimaryAttribute, float>();
        
        // Derived stats cache
        private Dictionary<DerivedStat, float> derivedStatsCache = new Dictionary<DerivedStat, float>();
        private bool isDirty = true;

        // Modifiers
        private Dictionary<PrimaryAttribute, List<StatModifier>> attributeModifiers = new Dictionary<PrimaryAttribute, List<StatModifier>>();
        private Dictionary<DerivedStat, List<StatModifier>> derivedModifiers = new Dictionary<DerivedStat, List<StatModifier>>();

        // Events
        public event Action<PrimaryAttribute, float, float> OnAttributeChanged;
        public event Action<DerivedStat, float, float> OnDerivedStatChanged;
        public event Action<float, float> OnHealthChanged;
        public event Action<float, float> OnManaChanged;
        public event Action<float, float> OnStaminaChanged;
        public event Action<int, int> OnLevelUp;
        public event Action<long, long> OnExperienceGained;
        public event Action OnDeath;
        public event Action<StatModifier> OnModifierAdded;
        public event Action<StatModifier> OnModifierRemoved;

        // Properties
        public string CharacterName => characterName;
        public string CharacterClass => characterClass;
        public int Level => levelProgression.currentLevel;
        public long CurrentExperience => levelProgression.currentExperience;
        public long ExperienceToNextLevel => levelProgression.experienceToNextLevel;
        public float ExperienceProgress => levelProgression.ExperienceProgress;
        public int UnspentAttributePoints => levelProgression.unspentAttributePoints;
        public bool IsAlive => currentHealth > 0;
        public float HealthPercent => currentHealth / GetDerivedStat(DerivedStat.MaxHealth);
        public float ManaPercent => currentMana / GetDerivedStat(DerivedStat.MaxMana);
        public float StaminaPercent => currentStamina / GetDerivedStat(DerivedStat.MaxStamina);

        private void Awake()
        {
            InitializeAttributes();
            InitializeModifierDictionaries();
            
            levelProgression.OnLevelUp += HandleLevelUp;
            levelProgression.OnExperienceGained += HandleExperienceGained;
        }

        private void Start()
        {
            RecalculateDerivedStats();
            InitializeResources();
        }

        private void Update()
        {
            UpdateModifiers(Time.deltaTime);
            RegenerateResources(Time.deltaTime);
        }

        private void OnDestroy()
        {
            levelProgression.OnLevelUp -= HandleLevelUp;
            levelProgression.OnExperienceGained -= HandleExperienceGained;
        }

        private void InitializeAttributes()
        {
            foreach (PrimaryAttribute attr in Enum.GetValues(typeof(PrimaryAttribute)))
            {
                int baseValue = statsConfig != null ? statsConfig.GetBaseAttribute(attr) : 10;
                baseAttributes[attr] = baseValue;
                allocatedPoints[attr] = 0;
            }
        }

        private void InitializeModifierDictionaries()
        {
            foreach (PrimaryAttribute attr in Enum.GetValues(typeof(PrimaryAttribute)))
            {
                attributeModifiers[attr] = new List<StatModifier>();
            }
            foreach (DerivedStat stat in Enum.GetValues(typeof(DerivedStat)))
            {
                derivedModifiers[stat] = new List<StatModifier>();
                derivedStatsCache[stat] = 0;
            }
        }

        private void InitializeResources()
        {
            currentHealth = GetDerivedStat(DerivedStat.MaxHealth);
            currentMana = GetDerivedStat(DerivedStat.MaxMana);
            currentStamina = GetDerivedStat(DerivedStat.MaxStamina);
        }

        #region Attribute Management

        /// <summary>
        /// Gets the total value of a primary attribute (base + allocated + modifiers).
        /// </summary>
        public float GetAttribute(PrimaryAttribute attribute)
        {
            float baseValue = baseAttributes[attribute] + allocatedPoints[attribute];
            
            // Add level-based growth
            if (statsConfig != null)
            {
                baseValue += (Level - 1) * statsConfig.GetAttributeGrowthPerLevel(attribute);
            }

            return ApplyModifiers(baseValue, attributeModifiers[attribute]);
        }

        /// <summary>
        /// Gets the base value of an attribute (without modifiers).
        /// </summary>
        public float GetBaseAttribute(PrimaryAttribute attribute)
        {
            return baseAttributes[attribute] + allocatedPoints[attribute];
        }

        /// <summary>
        /// Allocates attribute points to a primary attribute.
        /// </summary>
        public bool AllocateAttributePoint(PrimaryAttribute attribute, int points = 1)
        {
            if (levelProgression.unspentAttributePoints < points) return false;

            float previousValue = GetAttribute(attribute);
            allocatedPoints[attribute] += points;
            levelProgression.unspentAttributePoints -= points;
            
            MarkDirty();
            OnAttributeChanged?.Invoke(attribute, previousValue, GetAttribute(attribute));
            return true;
        }

        /// <summary>
        /// Resets all allocated attribute points.
        /// </summary>
        public void ResetAllocatedPoints()
        {
            int totalAllocated = (int)allocatedPoints.Values.Sum();
            
            foreach (var attr in allocatedPoints.Keys.ToList())
            {
                allocatedPoints[attr] = 0;
            }
            
            levelProgression.unspentAttributePoints += totalAllocated;
            MarkDirty();
        }

        #endregion

        #region Derived Stats

        /// <summary>
        /// Gets the calculated value of a derived stat.
        /// </summary>
        public float GetDerivedStat(DerivedStat stat)
        {
            if (isDirty)
            {
                RecalculateDerivedStats();
            }
            return derivedStatsCache[stat];
        }

        private void RecalculateDerivedStats()
        {
            Dictionary<DerivedStat, float> previousValues = new Dictionary<DerivedStat, float>(derivedStatsCache);

            // Calculate base derived stats from attributes
            derivedStatsCache[DerivedStat.MaxHealth] = CalculateDerivedStat(DerivedStat.MaxHealth);
            derivedStatsCache[DerivedStat.MaxMana] = CalculateDerivedStat(DerivedStat.MaxMana);
            derivedStatsCache[DerivedStat.MaxStamina] = CalculateDerivedStat(DerivedStat.MaxStamina);
            derivedStatsCache[DerivedStat.PhysicalAttack] = CalculateDerivedStat(DerivedStat.PhysicalAttack);
            derivedStatsCache[DerivedStat.MagicalAttack] = CalculateDerivedStat(DerivedStat.MagicalAttack);
            derivedStatsCache[DerivedStat.PhysicalDefense] = CalculateDerivedStat(DerivedStat.PhysicalDefense);
            derivedStatsCache[DerivedStat.MagicalDefense] = CalculateDerivedStat(DerivedStat.MagicalDefense);
            derivedStatsCache[DerivedStat.Speed] = CalculateDerivedStat(DerivedStat.Speed);
            derivedStatsCache[DerivedStat.Evasion] = CalculateDerivedStat(DerivedStat.Evasion);
            derivedStatsCache[DerivedStat.Accuracy] = 100f + GetAttribute(PrimaryAttribute.Dexterity) * 0.5f;
            derivedStatsCache[DerivedStat.CriticalChance] = CalculateCriticalChance();
            derivedStatsCache[DerivedStat.CriticalDamage] = 150f + GetAttribute(PrimaryAttribute.Luck) * 1f;
            derivedStatsCache[DerivedStat.HealthRegen] = GetAttribute(PrimaryAttribute.Constitution) * 0.1f;
            derivedStatsCache[DerivedStat.ManaRegen] = GetAttribute(PrimaryAttribute.Wisdom) * 0.2f;
            derivedStatsCache[DerivedStat.StaminaRegen] = GetAttribute(PrimaryAttribute.Constitution) * 0.5f;
            derivedStatsCache[DerivedStat.BlockChance] = GetAttribute(PrimaryAttribute.Strength) * 0.25f;
            derivedStatsCache[DerivedStat.ParryChance] = GetAttribute(PrimaryAttribute.Dexterity) * 0.2f;
            derivedStatsCache[DerivedStat.LifeSteal] = 0f;
            derivedStatsCache[DerivedStat.ManaSteal] = 0f;
            derivedStatsCache[DerivedStat.ExperienceBonus] = GetAttribute(PrimaryAttribute.Wisdom) * 0.5f;
            derivedStatsCache[DerivedStat.GoldBonus] = GetAttribute(PrimaryAttribute.Charisma) * 1f;
            derivedStatsCache[DerivedStat.DropRateBonus] = GetAttribute(PrimaryAttribute.Luck) * 0.5f;
            derivedStatsCache[DerivedStat.CooldownReduction] = GetAttribute(PrimaryAttribute.Intelligence) * 0.1f;
            derivedStatsCache[DerivedStat.CastSpeed] = 100f + GetAttribute(PrimaryAttribute.Intelligence) * 0.5f;
            derivedStatsCache[DerivedStat.AttackSpeed] = 100f + GetAttribute(PrimaryAttribute.Dexterity) * 0.5f;
            derivedStatsCache[DerivedStat.MovementSpeed] = 100f + GetAttribute(PrimaryAttribute.Dexterity) * 0.25f;

            // Elemental resistances
            derivedStatsCache[DerivedStat.ResistFire] = GetAttribute(PrimaryAttribute.Constitution) * 0.2f;
            derivedStatsCache[DerivedStat.ResistIce] = GetAttribute(PrimaryAttribute.Constitution) * 0.2f;
            derivedStatsCache[DerivedStat.ResistLightning] = GetAttribute(PrimaryAttribute.Constitution) * 0.2f;
            derivedStatsCache[DerivedStat.ResistEarth] = GetAttribute(PrimaryAttribute.Constitution) * 0.2f;
            derivedStatsCache[DerivedStat.ResistLight] = GetAttribute(PrimaryAttribute.Wisdom) * 0.3f;
            derivedStatsCache[DerivedStat.ResistDark] = GetAttribute(PrimaryAttribute.Wisdom) * 0.3f;
            derivedStatsCache[DerivedStat.ResistPoison] = GetAttribute(PrimaryAttribute.Constitution) * 0.25f;
            derivedStatsCache[DerivedStat.ResistStun] = GetAttribute(PrimaryAttribute.Constitution) * 0.15f;
            derivedStatsCache[DerivedStat.ResistSilence] = GetAttribute(PrimaryAttribute.Wisdom) * 0.2f;
            derivedStatsCache[DerivedStat.ResistSlow] = GetAttribute(PrimaryAttribute.Dexterity) * 0.2f;

            // Apply modifiers to derived stats
            foreach (DerivedStat stat in Enum.GetValues(typeof(DerivedStat)))
            {
                float baseValue = derivedStatsCache[stat];
                derivedStatsCache[stat] = ApplyModifiers(baseValue, derivedModifiers[stat]);
                
                if (previousValues.ContainsKey(stat) && Math.Abs(previousValues[stat] - derivedStatsCache[stat]) > 0.01f)
                {
                    OnDerivedStatChanged?.Invoke(stat, previousValues[stat], derivedStatsCache[stat]);
                }
            }

            isDirty = false;
        }

        private float CalculateDerivedStat(DerivedStat stat)
        {
            float multiplier = statsConfig != null ? statsConfig.GetDerivedStatMultiplier(stat) : 1f;
            
            return stat switch
            {
                DerivedStat.MaxHealth => GetAttribute(PrimaryAttribute.Constitution) * multiplier + 50f,
                DerivedStat.MaxMana => GetAttribute(PrimaryAttribute.Intelligence) * multiplier + 30f,
                DerivedStat.MaxStamina => GetAttribute(PrimaryAttribute.Constitution) * multiplier + 30f,
                DerivedStat.PhysicalAttack => GetAttribute(PrimaryAttribute.Strength) * multiplier,
                DerivedStat.MagicalAttack => GetAttribute(PrimaryAttribute.Intelligence) * multiplier,
                DerivedStat.PhysicalDefense => GetAttribute(PrimaryAttribute.Constitution) * multiplier,
                DerivedStat.MagicalDefense => GetAttribute(PrimaryAttribute.Wisdom) * multiplier,
                DerivedStat.Speed => GetAttribute(PrimaryAttribute.Dexterity) * multiplier,
                DerivedStat.Evasion => GetAttribute(PrimaryAttribute.Dexterity) * multiplier,
                _ => 0f
            };
        }

        private float CalculateCriticalChance()
        {
            float fromDex = GetAttribute(PrimaryAttribute.Dexterity) * 0.25f;
            float fromLuck = GetAttribute(PrimaryAttribute.Luck) * 0.5f;
            return 5f + fromDex + fromLuck; // Base 5% crit chance
        }

        private float ApplyModifiers(float baseValue, List<StatModifier> modifiers)
        {
            if (modifiers == null || modifiers.Count == 0) return baseValue;

            // Sort by priority
            var sorted = modifiers.OrderBy(m => m.priority).ToList();

            float flatSum = 0f;
            float percentAddSum = 0f;
            float percentMultiply = 1f;

            foreach (var mod in sorted)
            {
                switch (mod.modifierType)
                {
                    case ModifierType.Flat:
                        flatSum += mod.value;
                        break;
                    case ModifierType.PercentAdd:
                        percentAddSum += mod.value;
                        break;
                    case ModifierType.PercentMultiply:
                        percentMultiply *= (1f + mod.value / 100f);
                        break;
                }
            }

            // Formula: (base + flat) * (1 + percentAdd%) * percentMultiply
            float result = (baseValue + flatSum) * (1f + percentAddSum / 100f) * percentMultiply;
            return Mathf.Max(0, result);
        }

        private void MarkDirty()
        {
            isDirty = true;
        }

        #endregion

        #region Modifiers

        /// <summary>
        /// Adds a modifier to a primary attribute.
        /// </summary>
        public void AddAttributeModifier(PrimaryAttribute attribute, StatModifier modifier)
        {
            attributeModifiers[attribute].Add(modifier);
            modifier.OnExpire += m => RemoveAttributeModifier(attribute, m);
            MarkDirty();
            OnModifierAdded?.Invoke(modifier);
        }

        /// <summary>
        /// Removes a modifier from a primary attribute.
        /// </summary>
        public bool RemoveAttributeModifier(PrimaryAttribute attribute, StatModifier modifier)
        {
            bool removed = attributeModifiers[attribute].Remove(modifier);
            if (removed)
            {
                MarkDirty();
                OnModifierRemoved?.Invoke(modifier);
            }
            return removed;
        }

        /// <summary>
        /// Adds a modifier to a derived stat.
        /// </summary>
        public void AddDerivedStatModifier(DerivedStat stat, StatModifier modifier)
        {
            derivedModifiers[stat].Add(modifier);
            modifier.OnExpire += m => RemoveDerivedStatModifier(stat, m);
            MarkDirty();
            OnModifierAdded?.Invoke(modifier);
        }

        /// <summary>
        /// Removes a modifier from a derived stat.
        /// </summary>
        public bool RemoveDerivedStatModifier(DerivedStat stat, StatModifier modifier)
        {
            bool removed = derivedModifiers[stat].Remove(modifier);
            if (removed)
            {
                MarkDirty();
                OnModifierRemoved?.Invoke(modifier);
            }
            return removed;
        }

        /// <summary>
        /// Removes all modifiers from a specific source.
        /// </summary>
        public void RemoveAllModifiersFromSource(object source)
        {
            foreach (var list in attributeModifiers.Values)
            {
                list.RemoveAll(m => m.source == source);
            }
            foreach (var list in derivedModifiers.Values)
            {
                list.RemoveAll(m => m.source == source);
            }
            MarkDirty();
        }

        /// <summary>
        /// Removes all temporary modifiers.
        /// </summary>
        public void ClearTemporaryModifiers()
        {
            foreach (var list in attributeModifiers.Values)
            {
                list.RemoveAll(m => !m.isPermanent);
            }
            foreach (var list in derivedModifiers.Values)
            {
                list.RemoveAll(m => !m.isPermanent);
            }
            MarkDirty();
        }

        private void UpdateModifiers(float deltaTime)
        {
            bool anyExpired = false;

            foreach (var list in attributeModifiers.Values)
            {
                foreach (var mod in list.ToList())
                {
                    mod.Update(deltaTime);
                    if (mod.IsExpired) anyExpired = true;
                }
                list.RemoveAll(m => m.IsExpired);
            }

            foreach (var list in derivedModifiers.Values)
            {
                foreach (var mod in list.ToList())
                {
                    mod.Update(deltaTime);
                    if (mod.IsExpired) anyExpired = true;
                }
                list.RemoveAll(m => m.IsExpired);
            }

            if (anyExpired) MarkDirty();
        }

        /// <summary>
        /// Gets all active modifiers.
        /// </summary>
        public List<StatModifier> GetAllActiveModifiers()
        {
            var allModifiers = new List<StatModifier>();
            foreach (var list in attributeModifiers.Values)
            {
                allModifiers.AddRange(list);
            }
            foreach (var list in derivedModifiers.Values)
            {
                allModifiers.AddRange(list);
            }
            return allModifiers;
        }

        #endregion

        #region Resources

        /// <summary>
        /// Modifies current health by the specified amount.
        /// </summary>
        public void ModifyHealth(float amount)
        {
            float previousHealth = currentHealth;
            float maxHealth = GetDerivedStat(DerivedStat.MaxHealth);
            currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);

            if (currentHealth != previousHealth)
            {
                OnHealthChanged?.Invoke(previousHealth, currentHealth);
                
                if (currentHealth <= 0 && previousHealth > 0)
                {
                    OnDeath?.Invoke();
                }
            }
        }

        /// <summary>
        /// Modifies current mana by the specified amount.
        /// </summary>
        public void ModifyMana(float amount)
        {
            float previousMana = currentMana;
            float maxMana = GetDerivedStat(DerivedStat.MaxMana);
            currentMana = Mathf.Clamp(currentMana + amount, 0, maxMana);

            if (currentMana != previousMana)
            {
                OnManaChanged?.Invoke(previousMana, currentMana);
            }
        }

        /// <summary>
        /// Modifies current stamina by the specified amount.
        /// </summary>
        public void ModifyStamina(float amount)
        {
            float previousStamina = currentStamina;
            float maxStamina = GetDerivedStat(DerivedStat.MaxStamina);
            currentStamina = Mathf.Clamp(currentStamina + amount, 0, maxStamina);

            if (currentStamina != previousStamina)
            {
                OnStaminaChanged?.Invoke(previousStamina, currentStamina);
            }
        }

        /// <summary>
        /// Checks if the entity has enough mana.
        /// </summary>
        public bool HasEnoughMana(float amount) => currentMana >= amount;

        /// <summary>
        /// Checks if the entity has enough stamina.
        /// </summary>
        public bool HasEnoughStamina(float amount) => currentStamina >= amount;

        /// <summary>
        /// Fully restores all resources.
        /// </summary>
        public void FullRestore()
        {
            currentHealth = GetDerivedStat(DerivedStat.MaxHealth);
            currentMana = GetDerivedStat(DerivedStat.MaxMana);
            currentStamina = GetDerivedStat(DerivedStat.MaxStamina);
            
            OnHealthChanged?.Invoke(0, currentHealth);
            OnManaChanged?.Invoke(0, currentMana);
            OnStaminaChanged?.Invoke(0, currentStamina);
        }

        private void RegenerateResources(float deltaTime)
        {
            if (!IsAlive) return;

            float healthRegen = GetDerivedStat(DerivedStat.HealthRegen);
            float manaRegen = GetDerivedStat(DerivedStat.ManaRegen);
            float staminaRegen = GetDerivedStat(DerivedStat.StaminaRegen);

            if (currentHealth < GetDerivedStat(DerivedStat.MaxHealth))
            {
                ModifyHealth(healthRegen * deltaTime);
            }
            if (currentMana < GetDerivedStat(DerivedStat.MaxMana))
            {
                ModifyMana(manaRegen * deltaTime);
            }
            if (currentStamina < GetDerivedStat(DerivedStat.MaxStamina))
            {
                ModifyStamina(staminaRegen * deltaTime);
            }
        }

        #endregion

        #region Experience & Leveling

        /// <summary>
        /// Adds experience to the character.
        /// </summary>
        public void AddExperience(long amount)
        {
            // Apply experience bonus
            float bonus = GetDerivedStat(DerivedStat.ExperienceBonus);
            long modifiedAmount = (long)(amount * (1f + bonus / 100f));
            levelProgression.AddExperience(modifiedAmount);
        }

        /// <summary>
        /// Sets the character's level directly (for testing or special cases).
        /// </summary>
        public void SetLevel(int level)
        {
            int previousLevel = levelProgression.currentLevel;
            levelProgression.currentLevel = Mathf.Clamp(level, 1, levelProgression.maxLevel);
            levelProgression.currentExperience = 0;
            levelProgression.experienceToNextLevel = levelProgression.CalculateExpForLevel(levelProgression.currentLevel + 1);
            
            MarkDirty();
            
            if (previousLevel != levelProgression.currentLevel)
            {
                OnLevelUp?.Invoke(previousLevel, levelProgression.currentLevel);
            }
        }

        private void HandleLevelUp(int previousLevel, int newLevel)
        {
            MarkDirty();
            OnLevelUp?.Invoke(previousLevel, newLevel);
            
            // Restore resources on level up
            FullRestore();
        }

        private void HandleExperienceGained(long previousExp, long currentExp)
        {
            OnExperienceGained?.Invoke(previousExp, currentExp);
        }

        #endregion

        #region Utility

        /// <summary>
        /// Gets a formatted string of all stats.
        /// </summary>
        public string GetStatsDisplay()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"=== {characterName} ({characterClass}) Lv.{Level} ===");
            sb.AppendLine($"EXP: {CurrentExperience}/{ExperienceToNextLevel}");
            sb.AppendLine($"HP: {currentHealth:F0}/{GetDerivedStat(DerivedStat.MaxHealth):F0}");
            sb.AppendLine($"MP: {currentMana:F0}/{GetDerivedStat(DerivedStat.MaxMana):F0}");
            sb.AppendLine($"SP: {currentStamina:F0}/{GetDerivedStat(DerivedStat.MaxStamina):F0}");
            sb.AppendLine("\n--- Primary Attributes ---");
            foreach (PrimaryAttribute attr in Enum.GetValues(typeof(PrimaryAttribute)))
            {
                sb.AppendLine($"{attr}: {GetAttribute(attr):F1}");
            }
            sb.AppendLine($"\nUnspent Points: {UnspentAttributePoints}");
            return sb.ToString();
        }

        /// <summary>
        /// Creates a save data snapshot of the current stats.
        /// </summary>
        public CharacterStatsSaveData CreateSaveData()
        {
            return new CharacterStatsSaveData
            {
                characterName = characterName,
                characterClass = characterClass,
                level = levelProgression.currentLevel,
                currentExperience = levelProgression.currentExperience,
                unspentAttributePoints = levelProgression.unspentAttributePoints,
                allocatedPoints = new Dictionary<PrimaryAttribute, float>(allocatedPoints),
                currentHealth = currentHealth,
                currentMana = currentMana,
                currentStamina = currentStamina
            };
        }

        /// <summary>
        /// Loads stats from save data.
        /// </summary>
        public void LoadSaveData(CharacterStatsSaveData saveData)
        {
            characterName = saveData.characterName;
            characterClass = saveData.characterClass;
            levelProgression.currentLevel = saveData.level;
            levelProgression.currentExperience = saveData.currentExperience;
            levelProgression.unspentAttributePoints = saveData.unspentAttributePoints;
            levelProgression.experienceToNextLevel = levelProgression.CalculateExpForLevel(saveData.level + 1);
            allocatedPoints = new Dictionary<PrimaryAttribute, float>(saveData.allocatedPoints);
            
            MarkDirty();
            RecalculateDerivedStats();
            
            currentHealth = saveData.currentHealth;
            currentMana = saveData.currentMana;
            currentStamina = saveData.currentStamina;
        }

        #endregion
    }

    /// <summary>
    /// Serializable save data for character stats.
    /// </summary>
    [Serializable]
    public class CharacterStatsSaveData
    {
        public string characterName;
        public string characterClass;
        public int level;
        public long currentExperience;
        public int unspentAttributePoints;
        public Dictionary<PrimaryAttribute, float> allocatedPoints;
        public float currentHealth;
        public float currentMana;
        public float currentStamina;
    }
}
