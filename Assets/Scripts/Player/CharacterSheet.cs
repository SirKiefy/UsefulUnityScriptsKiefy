using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UsefulScripts.Player
{
    #region Enums

    /// <summary>
    /// Defines core character attributes.
    /// </summary>
    public enum CharacterAttribute
    {
        Strength,
        Dexterity,
        Constitution,
        Intelligence,
        Wisdom,
        Charisma,
        Luck,
        Perception,
        Willpower,
        Endurance
    }

    /// <summary>
    /// Defines character secondary stats derived from attributes.
    /// </summary>
    public enum SecondaryStat
    {
        MaxHealth,
        MaxMana,
        MaxStamina,
        HealthRegen,
        ManaRegen,
        StaminaRegen,
        PhysicalAttack,
        MagicalAttack,
        PhysicalDefense,
        MagicalDefense,
        Speed,
        Evasion,
        Accuracy,
        CriticalChance,
        CriticalDamage,
        BlockChance,
        ParryChance,
        CarryCapacity,
        MovementSpeed,
        AttackSpeed,
        CastSpeed
    }

    /// <summary>
    /// Defines visibility states for skills and stats.
    /// </summary>
    public enum VisibilityState
    {
        Visible,
        Hidden,
        Locked,
        Unknown,
        Discovered
    }

    /// <summary>
    /// Defines skill categories.
    /// </summary>
    public enum SkillCategoryType
    {
        Combat,
        Magic,
        Stealth,
        Crafting,
        Survival,
        Social,
        Knowledge,
        Movement,
        Passive,
        Ultimate,
        Secret,
        Innate
    }

    /// <summary>
    /// Defines elemental types for damage and resistance.
    /// </summary>
    public enum ElementType
    {
        Physical,
        Fire,
        Ice,
        Lightning,
        Earth,
        Water,
        Wind,
        Light,
        Dark,
        Poison,
        Nature,
        Arcane,
        Holy,
        Void,
        Psychic
    }

    /// <summary>
    /// Defines condition immunities.
    /// </summary>
    public enum ConditionType
    {
        Poison,
        Burn,
        Freeze,
        Stun,
        Paralyze,
        Sleep,
        Silence,
        Blind,
        Confuse,
        Charm,
        Fear,
        Slow,
        Bleed,
        Curse,
        Petrify
    }

    #endregion

    #region Data Classes

    /// <summary>
    /// Represents a single attribute with base value, bonuses, and modifiers.
    /// </summary>
    [Serializable]
    public class AttributeValue
    {
        public CharacterAttribute attribute;
        public float baseValue;
        public float bonusValue;
        public float growthPerLevel;
        public List<AttributeModifier> modifiers = new List<AttributeModifier>();

        public float TotalValue => CalculateTotal();
        public float BaseWithBonus => baseValue + bonusValue;

        public AttributeValue(CharacterAttribute attr, float baseVal = 10f, float growth = 1f)
        {
            attribute = attr;
            baseValue = baseVal;
            bonusValue = 0f;
            growthPerLevel = growth;
        }

        private float CalculateTotal()
        {
            float flat = modifiers.Where(m => m.modifierType == ModifierOperationType.Flat).Sum(m => m.value);
            float percentAdd = modifiers.Where(m => m.modifierType == ModifierOperationType.PercentAdd).Sum(m => m.value);
            float percentMult = 1f;
            foreach (var mod in modifiers.Where(m => m.modifierType == ModifierOperationType.PercentMultiply))
            {
                percentMult *= (1f + mod.value / 100f);
            }

            return (BaseWithBonus + flat) * (1f + percentAdd / 100f) * percentMult;
        }

        public void AddModifier(AttributeModifier modifier)
        {
            modifiers.Add(modifier);
        }

        public bool RemoveModifier(string modifierId)
        {
            return modifiers.RemoveAll(m => m.modifierId == modifierId) > 0;
        }
    }

    /// <summary>
    /// Represents a modifier applied to an attribute.
    /// </summary>
    [Serializable]
    public class AttributeModifier
    {
        public string modifierId;
        public string sourceName;
        public ModifierOperationType modifierType;
        public float value;
        public float duration;
        public float remainingTime;
        public bool isPermanent;

        public bool IsExpired => !isPermanent && remainingTime <= 0f;

        public AttributeModifier(string id, string source, ModifierOperationType type, float val, float dur = -1f)
        {
            modifierId = id;
            sourceName = source;
            modifierType = type;
            value = val;
            duration = dur;
            remainingTime = dur;
            isPermanent = dur < 0f;
        }
    }

    /// <summary>
    /// Defines modifier operation types.
    /// </summary>
    public enum ModifierOperationType
    {
        Flat,
        PercentAdd,
        PercentMultiply
    }

    /// <summary>
    /// Represents a skill with visibility, level, and discovery mechanics.
    /// </summary>
    [Serializable]
    public class CharacterSkill
    {
        public string skillId;
        public string skillName;
        public string description;
        public string hiddenDescription;
        public Sprite icon;
        public SkillCategoryType category;

        [Header("Progression")]
        public int currentLevel;
        public int maxLevel;
        public long currentExp;
        public long expToNextLevel;
        public float proficiency; // 0-100% mastery

        [Header("Visibility")]
        public VisibilityState visibilityState;
        public bool isHiddenSkill;
        public string discoveryHint;
        public List<string> discoveryConditions;

        [Header("Requirements")]
        public int requiredCharacterLevel;
        public List<SkillRequirement> prerequisites;

        [Header("Effects")]
        public List<SkillEffectData> effects;
        public List<SkillScaling> scalings;

        [Header("Passives")]
        public bool isPassive;
        public List<PassiveBonus> passiveBonuses;

        public event Action<CharacterSkill> OnSkillLevelUp;
        public event Action<CharacterSkill> OnSkillDiscovered;
        public event Action<CharacterSkill> OnSkillMastered;

        public bool IsDiscovered => visibilityState == VisibilityState.Discovered || visibilityState == VisibilityState.Visible;
        public bool IsMastered => currentLevel >= maxLevel && proficiency >= 100f;
        public bool IsUnlocked => visibilityState != VisibilityState.Locked && visibilityState != VisibilityState.Unknown;

        public string GetDisplayDescription()
        {
            if (!IsDiscovered && isHiddenSkill)
            {
                return discoveryHint ?? "???";
            }
            return description;
        }

        public void AddExperience(long exp)
        {
            if (currentLevel >= maxLevel) return;

            currentExp += exp;
            while (currentExp >= expToNextLevel && currentLevel < maxLevel)
            {
                currentExp -= expToNextLevel;
                LevelUp();
            }
        }

        private void LevelUp()
        {
            int previousLevel = currentLevel;
            currentLevel++;
            expToNextLevel = CalculateExpForLevel(currentLevel + 1);
            OnSkillLevelUp?.Invoke(this);

            if (currentLevel >= maxLevel)
            {
                OnSkillMastered?.Invoke(this);
            }
        }

        public void Discover()
        {
            if (visibilityState == VisibilityState.Unknown || visibilityState == VisibilityState.Hidden)
            {
                visibilityState = VisibilityState.Discovered;
                OnSkillDiscovered?.Invoke(this);
            }
        }

        private long CalculateExpForLevel(int level)
        {
            return (long)(100 * Math.Pow(level, 2.0f) + 50 * level);
        }

        public CharacterSkill Clone()
        {
            return new CharacterSkill
            {
                skillId = skillId,
                skillName = skillName,
                description = description,
                hiddenDescription = hiddenDescription,
                icon = icon,
                category = category,
                currentLevel = currentLevel,
                maxLevel = maxLevel,
                currentExp = currentExp,
                expToNextLevel = expToNextLevel,
                proficiency = proficiency,
                visibilityState = visibilityState,
                isHiddenSkill = isHiddenSkill,
                discoveryHint = discoveryHint,
                discoveryConditions = new List<string>(discoveryConditions ?? new List<string>()),
                requiredCharacterLevel = requiredCharacterLevel,
                prerequisites = new List<SkillRequirement>(prerequisites ?? new List<SkillRequirement>()),
                effects = new List<SkillEffectData>(effects ?? new List<SkillEffectData>()),
                scalings = new List<SkillScaling>(scalings ?? new List<SkillScaling>()),
                isPassive = isPassive,
                passiveBonuses = new List<PassiveBonus>(passiveBonuses ?? new List<PassiveBonus>())
            };
        }
    }

    [Serializable]
    public class SkillRequirement
    {
        public string skillId;
        public int requiredLevel;
    }

    [Serializable]
    public class SkillEffectData
    {
        public string effectName;
        public float baseValue;
        public float valuePerLevel;
        public ElementType element;
        public float duration;
    }

    [Serializable]
    public class SkillScaling
    {
        public CharacterAttribute attribute;
        public float scalingPercent;
    }

    [Serializable]
    public class PassiveBonus
    {
        public SecondaryStat stat;
        public float value;
        public bool isPercentage;
    }

    /// <summary>
    /// Represents elemental resistance data.
    /// </summary>
    [Serializable]
    public class ElementalResistance
    {
        public ElementType element;
        public float baseResistance;
        public float bonusResistance;
        public float maxResistance = 90f;
        public float minResistance = -100f; // Negative = weakness
        public List<AttributeModifier> modifiers = new List<AttributeModifier>();

        public float TotalResistance
        {
            get
            {
                float total = baseResistance + bonusResistance;
                foreach (var mod in modifiers)
                {
                    if (mod.modifierType == ModifierOperationType.Flat)
                        total += mod.value;
                }
                return Mathf.Clamp(total, minResistance, maxResistance);
            }
        }

        public bool IsImmune => TotalResistance >= 100f;
        public bool IsWeak => TotalResistance < 0f;
        public bool Absorbs => TotalResistance > 100f;

        public ElementalResistance(ElementType elem, float baseRes = 0f)
        {
            element = elem;
            baseResistance = baseRes;
            bonusResistance = 0f;
        }

        public float CalculateDamageMultiplier()
        {
            float resistance = TotalResistance;
            if (resistance > 100f)
            {
                // Absorb damage as healing
                return -(resistance - 100f) / 100f;
            }
            return 1f - (resistance / 100f);
        }
    }

    /// <summary>
    /// Represents condition immunity/resistance data.
    /// </summary>
    [Serializable]
    public class ConditionResistance
    {
        public ConditionType condition;
        public float resistance;
        public bool isImmune;
        public float durationReduction;

        public ConditionResistance(ConditionType cond, float res = 0f, bool immune = false)
        {
            condition = cond;
            resistance = res;
            isImmune = immune;
            durationReduction = 0f;
        }
    }

    #endregion

    #region ScriptableObjects

    /// <summary>
    /// Configuration for character sheet defaults.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCharacterSheetConfig", menuName = "UsefulScripts/Player/Character Sheet Config")]
    public class CharacterSheetConfig : ScriptableObject
    {
        [Header("Attribute Defaults")]
        public int defaultStrength = 10;
        public int defaultDexterity = 10;
        public int defaultConstitution = 10;
        public int defaultIntelligence = 10;
        public int defaultWisdom = 10;
        public int defaultCharisma = 10;
        public int defaultLuck = 10;
        public int defaultPerception = 10;
        public int defaultWillpower = 10;
        public int defaultEndurance = 10;

        [Header("Growth Per Level")]
        public float strengthGrowth = 1f;
        public float dexterityGrowth = 1f;
        public float constitutionGrowth = 1f;
        public float intelligenceGrowth = 1f;
        public float wisdomGrowth = 0.8f;
        public float charismaGrowth = 0.5f;
        public float luckGrowth = 0.5f;
        public float perceptionGrowth = 0.8f;
        public float willpowerGrowth = 0.8f;
        public float enduranceGrowth = 1f;

        [Header("Secondary Stat Multipliers")]
        public float healthPerConstitution = 10f;
        public float manaPerIntelligence = 5f;
        public float staminaPerEndurance = 8f;
        public float attackPerStrength = 2f;
        public float magicAttackPerIntelligence = 2f;
        public float defensePerConstitution = 1.5f;
        public float magicDefensePerWisdom = 1.5f;
        public float speedPerDexterity = 1f;
        public float evasionPerDexterity = 0.5f;
        public float accuracyPerPerception = 0.5f;
        public float critChancePerLuck = 0.5f;
        public float critDamagePerLuck = 1f;
        public float carryCapacityPerStrength = 5f;

        [Header("Leveling")]
        public int maxLevel = 100;
        public int attributePointsPerLevel = 5;
        public int skillPointsPerLevel = 2;
        public float baseExpForLevel = 100f;
        public float expScalingExponent = 2.5f;

        [Header("Elemental Resistances")]
        public float defaultElementalResistance = 0f;
        public float maxElementalResistance = 90f;

        public float GetDefaultAttribute(CharacterAttribute attr)
        {
            return attr switch
            {
                CharacterAttribute.Strength => defaultStrength,
                CharacterAttribute.Dexterity => defaultDexterity,
                CharacterAttribute.Constitution => defaultConstitution,
                CharacterAttribute.Intelligence => defaultIntelligence,
                CharacterAttribute.Wisdom => defaultWisdom,
                CharacterAttribute.Charisma => defaultCharisma,
                CharacterAttribute.Luck => defaultLuck,
                CharacterAttribute.Perception => defaultPerception,
                CharacterAttribute.Willpower => defaultWillpower,
                CharacterAttribute.Endurance => defaultEndurance,
                _ => 10f
            };
        }

        public float GetAttributeGrowth(CharacterAttribute attr)
        {
            return attr switch
            {
                CharacterAttribute.Strength => strengthGrowth,
                CharacterAttribute.Dexterity => dexterityGrowth,
                CharacterAttribute.Constitution => constitutionGrowth,
                CharacterAttribute.Intelligence => intelligenceGrowth,
                CharacterAttribute.Wisdom => wisdomGrowth,
                CharacterAttribute.Charisma => charismaGrowth,
                CharacterAttribute.Luck => luckGrowth,
                CharacterAttribute.Perception => perceptionGrowth,
                CharacterAttribute.Willpower => willpowerGrowth,
                CharacterAttribute.Endurance => enduranceGrowth,
                _ => 1f
            };
        }
    }

    /// <summary>
    /// Skill definition as a ScriptableObject.
    /// </summary>
    [CreateAssetMenu(fileName = "NewSkillDefinition", menuName = "UsefulScripts/Player/Skill Definition")]
    public class SkillDefinition : ScriptableObject
    {
        [Header("Basic Info")]
        public string skillId;
        public string skillName;
        [TextArea(3, 5)]
        public string description;
        [TextArea(2, 4)]
        public string hiddenDescription;
        public Sprite icon;
        public SkillCategoryType category;

        [Header("Progression")]
        public int maxLevel = 10;
        public int[] expPerLevel;

        [Header("Hidden Skill Settings")]
        public bool isHiddenSkill;
        public string discoveryHint = "???";
        public List<string> discoveryConditions;

        [Header("Requirements")]
        public int requiredCharacterLevel = 1;
        public List<SkillRequirement> prerequisites;

        [Header("Effects")]
        public List<SkillEffectData> effects;
        public List<SkillScaling> scalings;

        [Header("Passive")]
        public bool isPassive;
        public List<PassiveBonus> passiveBonuses;

        public CharacterSkill CreateInstance()
        {
            return new CharacterSkill
            {
                skillId = skillId,
                skillName = skillName,
                description = description,
                hiddenDescription = hiddenDescription,
                icon = icon,
                category = category,
                currentLevel = 0,
                maxLevel = maxLevel,
                currentExp = 0,
                expToNextLevel = expPerLevel != null && expPerLevel.Length > 0 ? expPerLevel[0] : 100,
                proficiency = 0f,
                visibilityState = isHiddenSkill ? VisibilityState.Hidden : VisibilityState.Visible,
                isHiddenSkill = isHiddenSkill,
                discoveryHint = discoveryHint,
                discoveryConditions = new List<string>(discoveryConditions ?? new List<string>()),
                requiredCharacterLevel = requiredCharacterLevel,
                prerequisites = new List<SkillRequirement>(prerequisites ?? new List<SkillRequirement>()),
                effects = new List<SkillEffectData>(effects ?? new List<SkillEffectData>()),
                scalings = new List<SkillScaling>(scalings ?? new List<SkillScaling>()),
                isPassive = isPassive,
                passiveBonuses = new List<PassiveBonus>(passiveBonuses ?? new List<PassiveBonus>())
            };
        }
    }

    #endregion

    /// <summary>
    /// Comprehensive character sheet managing all character data, skills, resistances, and progression.
    /// This is the central hub for character information.
    /// </summary>
    public class CharacterSheet : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private CharacterSheetConfig config;

        [Header("Identity")]
        [SerializeField] private string characterName = "Hero";
        [SerializeField] private string characterTitle = "";
        [SerializeField] private string characterClass = "Adventurer";
        [SerializeField] private string characterRace = "Human";

        [Header("Progression")]
        [SerializeField] private int currentLevel = 1;
        [SerializeField] private long currentExperience = 0;
        [SerializeField] private long experienceToNextLevel = 100;
        [SerializeField] private int unspentAttributePoints = 0;
        [SerializeField] private int unspentSkillPoints = 0;

        [Header("Available Skills")]
        [SerializeField] private List<SkillDefinition> skillDefinitions = new List<SkillDefinition>();

        // Attributes
        private Dictionary<CharacterAttribute, AttributeValue> attributes = new Dictionary<CharacterAttribute, AttributeValue>();

        // Secondary Stats Cache
        private Dictionary<SecondaryStat, float> secondaryStats = new Dictionary<SecondaryStat, float>();
        private bool statsAreDirty = true;

        // Skills
        private Dictionary<string, CharacterSkill> skills = new Dictionary<string, CharacterSkill>();
        private Dictionary<string, CharacterSkill> hiddenSkills = new Dictionary<string, CharacterSkill>();
        private Dictionary<string, CharacterSkill> discoveredHiddenSkills = new Dictionary<string, CharacterSkill>();

        // Elemental Resistances
        private Dictionary<ElementType, ElementalResistance> elementalResistances = new Dictionary<ElementType, ElementalResistance>();

        // Condition Resistances
        private Dictionary<ConditionType, ConditionResistance> conditionResistances = new Dictionary<ConditionType, ConditionResistance>();

        #region Events

        public event Action<int, int> OnLevelUp;
        public event Action<long, long> OnExperienceGained;
        public event Action<CharacterAttribute, float, float> OnAttributeChanged;
        public event Action<SecondaryStat, float, float> OnSecondaryStatChanged;
        public event Action<CharacterSkill> OnSkillLearned;
        public event Action<CharacterSkill> OnSkillLevelUp;
        public event Action<CharacterSkill> OnHiddenSkillDiscovered;
        public event Action<ElementType, float, float> OnResistanceChanged;
        public event Action OnStatsRecalculated;

        #endregion

        #region Properties

        public string CharacterName => characterName;
        public string CharacterTitle => characterTitle;
        public string FullName => string.IsNullOrEmpty(characterTitle) ? characterName : $"{characterName} {characterTitle}";
        public string CharacterClass => characterClass;
        public string CharacterRace => characterRace;
        public int Level => currentLevel;
        public int MaxLevel => config != null ? config.maxLevel : 100;
        public long Experience => currentExperience;
        public long ExperienceToLevel => experienceToNextLevel;
        public float ExperienceProgress => experienceToNextLevel > 0 ? (float)currentExperience / experienceToNextLevel : 1f;
        public int UnspentAttributePoints => unspentAttributePoints;
        public int UnspentSkillPoints => unspentSkillPoints;
        public bool IsMaxLevel => currentLevel >= MaxLevel;

        public IReadOnlyDictionary<CharacterAttribute, AttributeValue> Attributes => attributes;
        public IReadOnlyDictionary<string, CharacterSkill> Skills => skills;
        public IReadOnlyDictionary<string, CharacterSkill> DiscoveredHiddenSkills => discoveredHiddenSkills;
        public IReadOnlyDictionary<ElementType, ElementalResistance> ElementalResistances => elementalResistances;
        public IReadOnlyDictionary<ConditionType, ConditionResistance> ConditionResistances => conditionResistances;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeAttributes();
            InitializeElementalResistances();
            InitializeConditionResistances();
            InitializeSkills();
        }

        private void Start()
        {
            RecalculateSecondaryStats();
        }

        private void Update()
        {
            UpdateModifiers(Time.deltaTime);
            if (statsAreDirty)
            {
                RecalculateSecondaryStats();
            }
        }

        #endregion

        #region Initialization

        private void InitializeAttributes()
        {
            foreach (CharacterAttribute attr in Enum.GetValues(typeof(CharacterAttribute)))
            {
                float baseValue = config != null ? config.GetDefaultAttribute(attr) : 10f;
                float growth = config != null ? config.GetAttributeGrowth(attr) : 1f;
                attributes[attr] = new AttributeValue(attr, baseValue, growth);
            }
        }

        private void InitializeElementalResistances()
        {
            foreach (ElementType elem in Enum.GetValues(typeof(ElementType)))
            {
                float baseRes = config != null ? config.defaultElementalResistance : 0f;
                elementalResistances[elem] = new ElementalResistance(elem, baseRes);
            }
        }

        private void InitializeConditionResistances()
        {
            foreach (ConditionType cond in Enum.GetValues(typeof(ConditionType)))
            {
                conditionResistances[cond] = new ConditionResistance(cond, 0f, false);
            }
        }

        private void InitializeSkills()
        {
            foreach (var skillDef in skillDefinitions)
            {
                if (skillDef == null) continue;

                var skill = skillDef.CreateInstance();
                skill.OnSkillLevelUp += HandleSkillLevelUp;
                skill.OnSkillDiscovered += HandleSkillDiscovered;

                if (skill.isHiddenSkill)
                {
                    hiddenSkills[skill.skillId] = skill;
                }
                else
                {
                    skills[skill.skillId] = skill;
                }
            }
        }

        #endregion

        #region Attribute Management

        /// <summary>
        /// Gets the total value of an attribute including all modifiers.
        /// </summary>
        public float GetAttribute(CharacterAttribute attr)
        {
            if (!attributes.TryGetValue(attr, out var attrValue))
                return 0f;

            // Add level-based growth
            float levelBonus = (currentLevel - 1) * attrValue.growthPerLevel;
            return attrValue.TotalValue + levelBonus;
        }

        /// <summary>
        /// Gets the base value of an attribute (without modifiers or level growth).
        /// </summary>
        public float GetBaseAttribute(CharacterAttribute attr)
        {
            return attributes.TryGetValue(attr, out var attrValue) ? attrValue.BaseWithBonus : 0f;
        }

        /// <summary>
        /// Allocates attribute points to an attribute.
        /// </summary>
        public bool AllocateAttributePoints(CharacterAttribute attr, int points = 1)
        {
            if (points > unspentAttributePoints || points <= 0) return false;
            if (!attributes.TryGetValue(attr, out var attrValue)) return false;

            float previousValue = GetAttribute(attr);
            attrValue.bonusValue += points;
            unspentAttributePoints -= points;

            MarkStatsDirty();
            OnAttributeChanged?.Invoke(attr, previousValue, GetAttribute(attr));
            return true;
        }

        /// <summary>
        /// Adds a temporary or permanent modifier to an attribute.
        /// </summary>
        public void AddAttributeModifier(CharacterAttribute attr, AttributeModifier modifier)
        {
            if (!attributes.TryGetValue(attr, out var attrValue)) return;

            float previousValue = GetAttribute(attr);
            attrValue.AddModifier(modifier);
            MarkStatsDirty();
            OnAttributeChanged?.Invoke(attr, previousValue, GetAttribute(attr));
        }

        /// <summary>
        /// Removes a modifier from an attribute.
        /// </summary>
        public bool RemoveAttributeModifier(CharacterAttribute attr, string modifierId)
        {
            if (!attributes.TryGetValue(attr, out var attrValue)) return false;

            float previousValue = GetAttribute(attr);
            bool removed = attrValue.RemoveModifier(modifierId);
            if (removed)
            {
                MarkStatsDirty();
                OnAttributeChanged?.Invoke(attr, previousValue, GetAttribute(attr));
            }
            return removed;
        }

        /// <summary>
        /// Resets all allocated attribute points.
        /// </summary>
        public void ResetAttributePoints()
        {
            int totalAllocated = 0;
            foreach (var kvp in attributes)
            {
                totalAllocated += (int)kvp.Value.bonusValue;
                kvp.Value.bonusValue = 0;
            }
            unspentAttributePoints += totalAllocated;
            MarkStatsDirty();
        }

        #endregion

        #region Secondary Stats

        /// <summary>
        /// Gets a secondary stat value.
        /// </summary>
        public float GetSecondaryStat(SecondaryStat stat)
        {
            if (statsAreDirty)
            {
                RecalculateSecondaryStats();
            }
            return secondaryStats.TryGetValue(stat, out float value) ? value : 0f;
        }

        private void RecalculateSecondaryStats()
        {
            var previousStats = new Dictionary<SecondaryStat, float>(secondaryStats);

            // Health, Mana, Stamina
            float conMult = config != null ? config.healthPerConstitution : 10f;
            float intMult = config != null ? config.manaPerIntelligence : 5f;
            float endMult = config != null ? config.staminaPerEndurance : 8f;

            secondaryStats[SecondaryStat.MaxHealth] = GetAttribute(CharacterAttribute.Constitution) * conMult + 50f;
            secondaryStats[SecondaryStat.MaxMana] = GetAttribute(CharacterAttribute.Intelligence) * intMult + 30f;
            secondaryStats[SecondaryStat.MaxStamina] = GetAttribute(CharacterAttribute.Endurance) * endMult + 40f;

            // Regeneration
            secondaryStats[SecondaryStat.HealthRegen] = GetAttribute(CharacterAttribute.Constitution) * 0.1f;
            secondaryStats[SecondaryStat.ManaRegen] = GetAttribute(CharacterAttribute.Wisdom) * 0.2f;
            secondaryStats[SecondaryStat.StaminaRegen] = GetAttribute(CharacterAttribute.Endurance) * 0.3f;

            // Attack
            float atkMult = config != null ? config.attackPerStrength : 2f;
            float matkMult = config != null ? config.magicAttackPerIntelligence : 2f;
            secondaryStats[SecondaryStat.PhysicalAttack] = GetAttribute(CharacterAttribute.Strength) * atkMult;
            secondaryStats[SecondaryStat.MagicalAttack] = GetAttribute(CharacterAttribute.Intelligence) * matkMult;

            // Defense
            float defMult = config != null ? config.defensePerConstitution : 1.5f;
            float mdefMult = config != null ? config.magicDefensePerWisdom : 1.5f;
            secondaryStats[SecondaryStat.PhysicalDefense] = GetAttribute(CharacterAttribute.Constitution) * defMult;
            secondaryStats[SecondaryStat.MagicalDefense] = GetAttribute(CharacterAttribute.Wisdom) * mdefMult;

            // Speed and Evasion
            float spdMult = config != null ? config.speedPerDexterity : 1f;
            float evaMult = config != null ? config.evasionPerDexterity : 0.5f;
            secondaryStats[SecondaryStat.Speed] = GetAttribute(CharacterAttribute.Dexterity) * spdMult;
            secondaryStats[SecondaryStat.Evasion] = GetAttribute(CharacterAttribute.Dexterity) * evaMult;

            // Accuracy
            float accMult = config != null ? config.accuracyPerPerception : 0.5f;
            secondaryStats[SecondaryStat.Accuracy] = 100f + GetAttribute(CharacterAttribute.Perception) * accMult;

            // Critical
            float critMult = config != null ? config.critChancePerLuck : 0.5f;
            float critDmgMult = config != null ? config.critDamagePerLuck : 1f;
            secondaryStats[SecondaryStat.CriticalChance] = 5f + GetAttribute(CharacterAttribute.Luck) * critMult + GetAttribute(CharacterAttribute.Dexterity) * 0.25f;
            secondaryStats[SecondaryStat.CriticalDamage] = 150f + GetAttribute(CharacterAttribute.Luck) * critDmgMult;

            // Block and Parry
            secondaryStats[SecondaryStat.BlockChance] = GetAttribute(CharacterAttribute.Strength) * 0.25f;
            secondaryStats[SecondaryStat.ParryChance] = GetAttribute(CharacterAttribute.Dexterity) * 0.2f;

            // Carry Capacity
            float carryMult = config != null ? config.carryCapacityPerStrength : 5f;
            secondaryStats[SecondaryStat.CarryCapacity] = GetAttribute(CharacterAttribute.Strength) * carryMult;

            // Movement and Attack Speed
            secondaryStats[SecondaryStat.MovementSpeed] = 100f + GetAttribute(CharacterAttribute.Dexterity) * 0.25f;
            secondaryStats[SecondaryStat.AttackSpeed] = 100f + GetAttribute(CharacterAttribute.Dexterity) * 0.5f;
            secondaryStats[SecondaryStat.CastSpeed] = 100f + GetAttribute(CharacterAttribute.Intelligence) * 0.3f;

            // Apply passive skill bonuses
            ApplyPassiveSkillBonuses();

            statsAreDirty = false;
            OnStatsRecalculated?.Invoke();

            // Fire changed events
            foreach (var kvp in secondaryStats)
            {
                if (previousStats.TryGetValue(kvp.Key, out float prev) && Math.Abs(prev - kvp.Value) > 0.01f)
                {
                    OnSecondaryStatChanged?.Invoke(kvp.Key, prev, kvp.Value);
                }
            }
        }

        private void ApplyPassiveSkillBonuses()
        {
            // Apply bonuses from passive skills
            foreach (var skill in skills.Values.Where(s => s.isPassive && s.IsUnlocked && s.currentLevel > 0))
            {
                foreach (var bonus in skill.passiveBonuses)
                {
                    if (secondaryStats.ContainsKey(bonus.stat))
                    {
                        float bonusValue = bonus.value * skill.currentLevel;
                        if (bonus.isPercentage)
                        {
                            secondaryStats[bonus.stat] *= (1f + bonusValue / 100f);
                        }
                        else
                        {
                            secondaryStats[bonus.stat] += bonusValue;
                        }
                    }
                }
            }

            // Apply discovered hidden skill bonuses
            foreach (var skill in discoveredHiddenSkills.Values.Where(s => s.isPassive && s.currentLevel > 0))
            {
                foreach (var bonus in skill.passiveBonuses)
                {
                    if (secondaryStats.ContainsKey(bonus.stat))
                    {
                        float bonusValue = bonus.value * skill.currentLevel;
                        if (bonus.isPercentage)
                        {
                            secondaryStats[bonus.stat] *= (1f + bonusValue / 100f);
                        }
                        else
                        {
                            secondaryStats[bonus.stat] += bonusValue;
                        }
                    }
                }
            }
        }

        private void MarkStatsDirty()
        {
            statsAreDirty = true;
        }

        #endregion

        #region Leveling

        /// <summary>
        /// Adds experience to the character.
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
                LevelUp();
                levelsGained++;
            }

            if (IsMaxLevel)
            {
                currentExperience = 0;
            }

            OnExperienceGained?.Invoke(previousExp, currentExperience);
            return levelsGained;
        }

        private void LevelUp()
        {
            int previousLevel = currentLevel;
            currentLevel++;

            int attrPoints = config != null ? config.attributePointsPerLevel : 5;
            int skillPts = config != null ? config.skillPointsPerLevel : 2;

            unspentAttributePoints += attrPoints;
            unspentSkillPoints += skillPts;

            experienceToNextLevel = CalculateExperienceForLevel(currentLevel + 1);
            MarkStatsDirty();

            OnLevelUp?.Invoke(previousLevel, currentLevel);

            // Check for hidden skill unlocks
            CheckHiddenSkillDiscoveries("level_up", currentLevel);
        }

        private long CalculateExperienceForLevel(int level)
        {
            float baseExp = config != null ? config.baseExpForLevel : 100f;
            float exponent = config != null ? config.expScalingExponent : 2.5f;
            return (long)(baseExp * Math.Pow(level, exponent) + 50 * level);
        }

        /// <summary>
        /// Sets the character level directly.
        /// </summary>
        public void SetLevel(int level)
        {
            int targetLevel = Mathf.Clamp(level, 1, MaxLevel);
            while (currentLevel < targetLevel)
            {
                LevelUp();
            }
        }

        #endregion

        #region Skills

        /// <summary>
        /// Gets a skill by ID.
        /// </summary>
        public CharacterSkill GetSkill(string skillId)
        {
            if (skills.TryGetValue(skillId, out var skill))
                return skill;
            if (discoveredHiddenSkills.TryGetValue(skillId, out skill))
                return skill;
            return null;
        }

        /// <summary>
        /// Learns a skill if requirements are met.
        /// </summary>
        public bool LearnSkill(string skillId)
        {
            if (!skills.TryGetValue(skillId, out var skill)) return false;
            if (skill.currentLevel > 0) return false;
            if (skill.requiredCharacterLevel > currentLevel) return false;

            // Check prerequisites
            foreach (var prereq in skill.prerequisites)
            {
                var prereqSkill = GetSkill(prereq.skillId);
                if (prereqSkill == null || prereqSkill.currentLevel < prereq.requiredLevel)
                {
                    return false;
                }
            }

            skill.currentLevel = 1;
            MarkStatsDirty();
            OnSkillLearned?.Invoke(skill);
            return true;
        }

        /// <summary>
        /// Levels up a skill using skill points.
        /// </summary>
        public bool LevelUpSkill(string skillId, int pointCost = 1)
        {
            var skill = GetSkill(skillId);
            if (skill == null) return false;
            if (skill.currentLevel >= skill.maxLevel) return false;
            if (unspentSkillPoints < pointCost) return false;

            unspentSkillPoints -= pointCost;
            skill.currentLevel++;
            MarkStatsDirty();
            OnSkillLevelUp?.Invoke(skill);
            return true;
        }

        /// <summary>
        /// Gets all skills of a specific category.
        /// </summary>
        public List<CharacterSkill> GetSkillsByCategory(SkillCategoryType category)
        {
            var result = new List<CharacterSkill>();
            result.AddRange(skills.Values.Where(s => s.category == category));
            result.AddRange(discoveredHiddenSkills.Values.Where(s => s.category == category));
            return result;
        }

        /// <summary>
        /// Gets all visible skills (excludes undiscovered hidden skills).
        /// </summary>
        public List<CharacterSkill> GetVisibleSkills()
        {
            var result = new List<CharacterSkill>();
            result.AddRange(skills.Values.Where(s => s.IsDiscovered));
            result.AddRange(discoveredHiddenSkills.Values);
            return result;
        }

        /// <summary>
        /// Attempts to discover a hidden skill by ID.
        /// </summary>
        public bool TryDiscoverHiddenSkill(string skillId)
        {
            if (!hiddenSkills.TryGetValue(skillId, out var skill)) return false;
            if (skill.IsDiscovered) return false;

            skill.Discover();
            hiddenSkills.Remove(skillId);
            discoveredHiddenSkills[skillId] = skill;

            OnHiddenSkillDiscovered?.Invoke(skill);
            return true;
        }

        /// <summary>
        /// Checks if any hidden skills should be discovered based on conditions.
        /// </summary>
        public void CheckHiddenSkillDiscoveries(string conditionType, object conditionValue)
        {
            var toDiscover = new List<string>();

            foreach (var kvp in hiddenSkills)
            {
                var skill = kvp.Value;
                if (skill.discoveryConditions == null) continue;

                string condition = $"{conditionType}:{conditionValue}";
                if (skill.discoveryConditions.Contains(condition))
                {
                    toDiscover.Add(kvp.Key);
                }
            }

            foreach (var skillId in toDiscover)
            {
                TryDiscoverHiddenSkill(skillId);
            }
        }

        private void HandleSkillLevelUp(CharacterSkill skill)
        {
            MarkStatsDirty();
            OnSkillLevelUp?.Invoke(skill);
        }

        private void HandleSkillDiscovered(CharacterSkill skill)
        {
            OnHiddenSkillDiscovered?.Invoke(skill);
        }

        #endregion

        #region Elemental Resistances

        /// <summary>
        /// Gets the resistance value for an element.
        /// </summary>
        public float GetElementalResistance(ElementType element)
        {
            return elementalResistances.TryGetValue(element, out var res) ? res.TotalResistance : 0f;
        }

        /// <summary>
        /// Calculates the damage multiplier for an element (considering resistance).
        /// </summary>
        public float GetElementalDamageMultiplier(ElementType element)
        {
            return elementalResistances.TryGetValue(element, out var res) ? res.CalculateDamageMultiplier() : 1f;
        }

        /// <summary>
        /// Modifies the base resistance for an element.
        /// </summary>
        public void SetElementalResistance(ElementType element, float value)
        {
            if (!elementalResistances.TryGetValue(element, out var res)) return;

            float previous = res.TotalResistance;
            res.baseResistance = value;
            OnResistanceChanged?.Invoke(element, previous, res.TotalResistance);
        }

        /// <summary>
        /// Adds a modifier to elemental resistance.
        /// </summary>
        public void AddElementalResistanceModifier(ElementType element, AttributeModifier modifier)
        {
            if (!elementalResistances.TryGetValue(element, out var res)) return;

            float previous = res.TotalResistance;
            res.modifiers.Add(modifier);
            OnResistanceChanged?.Invoke(element, previous, res.TotalResistance);
        }

        /// <summary>
        /// Checks if the character is immune to an element.
        /// </summary>
        public bool IsImmuneToElement(ElementType element)
        {
            return elementalResistances.TryGetValue(element, out var res) && res.IsImmune;
        }

        /// <summary>
        /// Checks if the character is weak to an element.
        /// </summary>
        public bool IsWeakToElement(ElementType element)
        {
            return elementalResistances.TryGetValue(element, out var res) && res.IsWeak;
        }

        #endregion

        #region Condition Resistances

        /// <summary>
        /// Gets the resistance to a condition.
        /// </summary>
        public float GetConditionResistance(ConditionType condition)
        {
            return conditionResistances.TryGetValue(condition, out var res) ? res.resistance : 0f;
        }

        /// <summary>
        /// Checks if the character is immune to a condition.
        /// </summary>
        public bool IsImmuneToCondition(ConditionType condition)
        {
            return conditionResistances.TryGetValue(condition, out var res) && res.isImmune;
        }

        /// <summary>
        /// Sets immunity to a condition.
        /// </summary>
        public void SetConditionImmunity(ConditionType condition, bool immune)
        {
            if (conditionResistances.TryGetValue(condition, out var res))
            {
                res.isImmune = immune;
            }
        }

        /// <summary>
        /// Sets resistance to a condition.
        /// </summary>
        public void SetConditionResistance(ConditionType condition, float value)
        {
            if (conditionResistances.TryGetValue(condition, out var res))
            {
                res.resistance = Mathf.Clamp(value, 0f, 100f);
            }
        }

        #endregion

        #region Modifiers Update

        private void UpdateModifiers(float deltaTime)
        {
            bool needsRecalc = false;

            // Update attribute modifiers
            foreach (var attr in attributes.Values)
            {
                var expired = new List<AttributeModifier>();
                foreach (var mod in attr.modifiers)
                {
                    if (!mod.isPermanent)
                    {
                        mod.remainingTime -= deltaTime;
                        if (mod.IsExpired)
                        {
                            expired.Add(mod);
                        }
                    }
                }
                foreach (var mod in expired)
                {
                    attr.modifiers.Remove(mod);
                    needsRecalc = true;
                }
            }

            // Update elemental resistance modifiers
            foreach (var res in elementalResistances.Values)
            {
                var expired = new List<AttributeModifier>();
                foreach (var mod in res.modifiers)
                {
                    if (!mod.isPermanent)
                    {
                        mod.remainingTime -= deltaTime;
                        if (mod.IsExpired)
                        {
                            expired.Add(mod);
                        }
                    }
                }
                foreach (var mod in expired)
                {
                    res.modifiers.Remove(mod);
                    needsRecalc = true;
                }
            }

            if (needsRecalc)
            {
                MarkStatsDirty();
            }
        }

        #endregion

        #region Utility

        /// <summary>
        /// Sets the character's name.
        /// </summary>
        public void SetCharacterName(string name)
        {
            characterName = name;
        }

        /// <summary>
        /// Sets the character's title.
        /// </summary>
        public void SetCharacterTitle(string title)
        {
            characterTitle = title;
        }

        /// <summary>
        /// Sets the character's class.
        /// </summary>
        public void SetCharacterClass(string className)
        {
            characterClass = className;
        }

        /// <summary>
        /// Gets a summary of the character's stats.
        /// </summary>
        public string GetStatsSummary()
        {
            var summary = new System.Text.StringBuilder();
            summary.AppendLine($"=== {FullName} ===");
            summary.AppendLine($"Class: {characterClass} | Race: {characterRace}");
            summary.AppendLine($"Level: {currentLevel}/{MaxLevel}");
            summary.AppendLine($"Experience: {currentExperience}/{experienceToNextLevel}");
            summary.AppendLine();
            summary.AppendLine("--- Attributes ---");
            foreach (var attr in attributes)
            {
                summary.AppendLine($"{attr.Key}: {GetAttribute(attr.Key):F1}");
            }
            summary.AppendLine();
            summary.AppendLine("--- Secondary Stats ---");
            foreach (var stat in secondaryStats)
            {
                summary.AppendLine($"{stat.Key}: {stat.Value:F1}");
            }
            return summary.ToString();
        }

        #endregion
    }
}
