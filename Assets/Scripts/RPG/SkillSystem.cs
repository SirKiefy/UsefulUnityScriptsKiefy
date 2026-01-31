using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UsefulScripts.RPG
{
    /// <summary>
    /// Defines the category of a skill.
    /// </summary>
    public enum SkillCategory
    {
        Combat,         // Direct combat skills
        Magic,          // Spells and magical abilities
        Support,        // Healing, buffs, utility
        Passive,        // Always-active bonuses
        Crafting,       // Crafting-related skills
        Exploration,    // Movement, gathering, etc.
        Social,         // Charisma, persuasion
        Stealth,        // Sneaking, thievery
        Summon,         // Summoning creatures
        Ultimate        // Limit break/ultimate skills
    }

    /// <summary>
    /// Defines the targeting mode for a skill.
    /// </summary>
    public enum SkillTargeting
    {
        Self,
        SingleAlly,
        SingleEnemy,
        AllAllies,
        AllEnemies,
        AllTargets,
        AreaCircle,
        AreaCone,
        AreaLine,
        RandomEnemy,
        RandomAlly,
        DeadAlly
    }

    /// <summary>
    /// Represents a skill effect that modifies stats or applies conditions.
    /// </summary>
    [Serializable]
    public class SkillEffect
    {
        public SkillEffectType effectType;
        public float baseValue;
        public float valuePerLevel;
        public float duration;
        public float chance = 100f;
        public bool scalesWithStats;
        public PrimaryAttribute scalingStat;
        public float statScaling = 1f;
        public DamageType element = DamageType.Physical;

        public float GetValue(int skillLevel, float statValue = 0)
        {
            float value = baseValue + (valuePerLevel * (skillLevel - 1));
            if (scalesWithStats)
            {
                value += statValue * statScaling;
            }
            return value;
        }
    }

    public enum SkillEffectType
    {
        Damage,
        Heal,
        Shield,
        BuffAttack,
        BuffDefense,
        BuffSpeed,
        BuffMagic,
        BuffCritical,
        DebuffAttack,
        DebuffDefense,
        DebuffSpeed,
        DebuffMagic,
        Stun,
        Poison,
        Burn,
        Freeze,
        Paralyze,
        Sleep,
        Silence,
        Blind,
        Confuse,
        Charm,
        Fear,
        Cleanse,
        Dispel,
        Revive,
        DrainHP,
        DrainMP,
        Reflect,
        Counter,
        Barrier,
        Haste,
        Slow,
        Regen,
        Protect,
        Shell,
        Berserk,
        Invisibility,
        Taunt,
        Knockback,
        Pull,
        Teleport,
        Summon
    }

    /// <summary>
    /// Represents a skill that can be learned and used.
    /// </summary>
    [CreateAssetMenu(fileName = "NewSkill", menuName = "UsefulScripts/RPG/Skill Data")]
    public class SkillData : ScriptableObject
    {
        [Header("Basic Info")]
        public string skillId;
        public string skillName;
        [TextArea(3, 5)]
        public string description;
        public Sprite icon;
        public SkillCategory category;

        [Header("Targeting")]
        public SkillTargeting targeting = SkillTargeting.SingleEnemy;
        public float range = 10f;
        public float areaRadius = 0f;
        public int maxTargets = 1;

        [Header("Costs")]
        public float mpCost = 10f;
        public float mpCostPerLevel = 2f;
        public float spCost = 0f;
        public float hpCost = 0f;
        public float cooldown = 5f;
        public float cooldownReductionPerLevel = 0.1f;

        [Header("Casting")]
        public float castTime = 0f;
        public float animationDuration = 1f;
        public bool canMoveWhileCasting = false;
        public bool canCancelCast = true;

        [Header("Progression")]
        public int maxLevel = 10;
        public int[] expToLevelUp = new int[] { 100, 200, 400, 800, 1600, 3200, 6400, 12800, 25600 };
        public bool canUpgrade = true;

        [Header("Learning")]
        public int learnPointCost = 1;
        public int requiredCharacterLevel = 1;
        public List<string> prerequisiteSkills = new List<string>();
        public string requiredClass;

        [Header("Effects")]
        public List<SkillEffect> effects = new List<SkillEffect>();

        [Header("Inheritance")]
        public bool canInherit = true;
        public float inheritEffectiveness = 0.8f;
        public int inheritCost = 2;

        [Header("Audio/Visual")]
        public AudioClip castSound;
        public AudioClip hitSound;
        public GameObject castVFX;
        public GameObject hitVFX;
        public AnimationClip skillAnimation;

        /// <summary>
        /// Gets the MP cost at a specific level.
        /// </summary>
        public float GetMPCost(int level)
        {
            return mpCost + (mpCostPerLevel * (level - 1));
        }

        /// <summary>
        /// Gets the cooldown at a specific level.
        /// </summary>
        public float GetCooldown(int level)
        {
            return Mathf.Max(1f, cooldown - (cooldownReductionPerLevel * (level - 1)));
        }

        /// <summary>
        /// Gets the experience needed to reach a specific level.
        /// </summary>
        public int GetExpForLevel(int level)
        {
            if (level <= 1 || level > maxLevel) return 0;
            int index = level - 2;
            if (index < expToLevelUp.Length)
            {
                return expToLevelUp[index];
            }
            return expToLevelUp[expToLevelUp.Length - 1] * (level - expToLevelUp.Length);
        }
    }

    /// <summary>
    /// Represents a learned skill instance with level and experience.
    /// </summary>
    [Serializable]
    public class LearnedSkill
    {
        public string skillId;
        public SkillData skillData;
        public int currentLevel = 1;
        public int currentExp = 0;
        public int totalUsageCount = 0;
        public bool isInherited = false;
        public float inheritedEffectiveness = 1f;
        public DateTime learnedAt;
        public bool isEquipped = false;
        public int slotIndex = -1;

        public event Action<int> OnLevelUp;
        public event Action OnMaxLevel;

        public bool IsMaxLevel => currentLevel >= (skillData?.maxLevel ?? 10);
        public int ExpToNextLevel => skillData?.GetExpForLevel(currentLevel + 1) ?? int.MaxValue;
        public float LevelProgress => (float)currentExp / ExpToNextLevel;
        public float CurrentMPCost => skillData?.GetMPCost(currentLevel) ?? 0;
        public float CurrentCooldown => skillData?.GetCooldown(currentLevel) ?? 5f;

        public LearnedSkill(SkillData data)
        {
            skillId = data.skillId;
            skillData = data;
            learnedAt = DateTime.Now;
        }

        /// <summary>
        /// Adds experience to this skill.
        /// </summary>
        public int AddExperience(int amount)
        {
            if (IsMaxLevel) return 0;

            currentExp += amount;
            int levelsGained = 0;

            while (currentExp >= ExpToNextLevel && !IsMaxLevel)
            {
                currentExp -= ExpToNextLevel;
                currentLevel++;
                levelsGained++;
                OnLevelUp?.Invoke(currentLevel);

                if (IsMaxLevel)
                {
                    OnMaxLevel?.Invoke();
                }
            }

            return levelsGained;
        }

        /// <summary>
        /// Gets the effectiveness multiplier for this skill.
        /// </summary>
        public float GetEffectiveness()
        {
            return isInherited ? inheritedEffectiveness : 1f;
        }

        /// <summary>
        /// Gets the value of an effect at the current level.
        /// </summary>
        public float GetEffectValue(int effectIndex, float statValue = 0)
        {
            if (skillData == null || effectIndex >= skillData.effects.Count) return 0;
            return skillData.effects[effectIndex].GetValue(currentLevel, statValue) * GetEffectiveness();
        }
    }

    /// <summary>
    /// Represents a skill on cooldown.
    /// </summary>
    [Serializable]
    public class SkillCooldown
    {
        public string skillId;
        public float remainingTime;
        public float totalTime;

        public float Progress => 1f - (remainingTime / totalTime);
        public bool IsReady => remainingTime <= 0;

        public void Update(float deltaTime, float cooldownReduction = 0)
        {
            remainingTime -= deltaTime * (1f + cooldownReduction);
            remainingTime = Mathf.Max(0, remainingTime);
        }
    }

    /// <summary>
    /// Configuration for the skill system.
    /// </summary>
    [CreateAssetMenu(fileName = "SkillSystemConfig", menuName = "UsefulScripts/RPG/Skill System Config")]
    public class SkillSystemConfig : ScriptableObject
    {
        [Header("Skill Settings")]
        public int maxEquippedSkills = 8;
        public int maxPassiveSkills = 4;
        public int maxInheritedSkills = 3;
        public bool allowSkillOverwrite = true;

        [Header("Experience Settings")]
        public int baseExpPerUse = 10;
        public float expMultiplierOnKill = 2f;
        public float expMultiplierOnCrit = 1.5f;
        public bool gainExpOnMiss = false;

        [Header("Inheritance Settings")]
        public bool enableSkillInheritance = true;
        public float defaultInheritEffectiveness = 0.8f;
        public int inheritPointCost = 1;

        [Header("Available Skills")]
        public List<SkillData> allSkills = new List<SkillData>();
    }

    /// <summary>
    /// Complete skill management system handling learning, upgrading, and using skills.
    /// </summary>
    public class SkillSystem : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private SkillSystemConfig config;

        [Header("Skill Points")]
        [SerializeField] private int availableSkillPoints = 0;
        [SerializeField] private int totalSkillPointsEarned = 0;

        // Runtime data
        private Dictionary<string, LearnedSkill> learnedSkills = new Dictionary<string, LearnedSkill>();
        private List<string> equippedSkillIds = new List<string>();
        private List<string> equippedPassiveIds = new List<string>();
        private List<string> inheritedSkillIds = new List<string>();
        private Dictionary<string, SkillCooldown> cooldowns = new Dictionary<string, SkillCooldown>();

        // Reference to stats system
        private CharacterStatsSystem statsSystem;

        // Events
        public event Action<LearnedSkill> OnSkillLearned;
        public event Action<LearnedSkill, int> OnSkillLevelUp;
        public event Action<LearnedSkill> OnSkillMaxed;
        public event Action<string> OnSkillEquipped;
        public event Action<string> OnSkillUnequipped;
        public event Action<string> OnSkillUsed;
        public event Action<string, float> OnSkillCooldownStarted;
        public event Action<string> OnSkillCooldownEnded;
        public event Action<LearnedSkill> OnSkillInherited;
        public event Action<int> OnSkillPointsChanged;

        // Properties
        public int AvailableSkillPoints => availableSkillPoints;
        public int TotalSkillPointsEarned => totalSkillPointsEarned;
        public int EquippedSkillCount => equippedSkillIds.Count;
        public int MaxEquippedSkills => config?.maxEquippedSkills ?? 8;
        public int LearnedSkillCount => learnedSkills.Count;
        public IReadOnlyList<string> EquippedSkills => equippedSkillIds.AsReadOnly();
        public IReadOnlyList<string> EquippedPassives => equippedPassiveIds.AsReadOnly();

        private void Awake()
        {
            statsSystem = GetComponent<CharacterStatsSystem>();
        }

        private void Update()
        {
            UpdateCooldowns(Time.deltaTime);
        }

        private void UpdateCooldowns(float deltaTime)
        {
            var expiredCooldowns = new List<string>();
            float cooldownReduction = statsSystem?.GetDerivedStat(DerivedStat.CooldownReduction) ?? 0f;

            foreach (var kvp in cooldowns)
            {
                kvp.Value.Update(deltaTime, cooldownReduction / 100f);
                if (kvp.Value.IsReady)
                {
                    expiredCooldowns.Add(kvp.Key);
                }
            }

            foreach (var skillId in expiredCooldowns)
            {
                cooldowns.Remove(skillId);
                OnSkillCooldownEnded?.Invoke(skillId);
            }
        }

        #region Learning Skills

        /// <summary>
        /// Learns a new skill.
        /// </summary>
        public bool LearnSkill(string skillId)
        {
            var skillData = GetSkillData(skillId);
            if (skillData == null)
            {
                Debug.LogWarning($"Skill not found: {skillId}");
                return false;
            }

            return LearnSkill(skillData);
        }

        /// <summary>
        /// Learns a new skill from skill data.
        /// </summary>
        public bool LearnSkill(SkillData skillData)
        {
            if (skillData == null) return false;
            if (HasSkill(skillData.skillId))
            {
                Debug.Log($"Already learned: {skillData.skillName}");
                return false;
            }

            // Check requirements
            if (!CanLearnSkill(skillData, out string reason))
            {
                Debug.Log($"Cannot learn {skillData.skillName}: {reason}");
                return false;
            }

            // Check skill points
            if (availableSkillPoints < skillData.learnPointCost)
            {
                Debug.Log($"Not enough skill points. Need {skillData.learnPointCost}, have {availableSkillPoints}");
                return false;
            }

            // Consume skill points
            availableSkillPoints -= skillData.learnPointCost;
            OnSkillPointsChanged?.Invoke(availableSkillPoints);

            // Create learned skill
            var learnedSkill = new LearnedSkill(skillData);
            learnedSkill.OnLevelUp += level => OnSkillLevelUp?.Invoke(learnedSkill, level);
            learnedSkill.OnMaxLevel += () => OnSkillMaxed?.Invoke(learnedSkill);

            learnedSkills[skillData.skillId] = learnedSkill;

            // Auto-equip if there's room and it's not passive
            if (skillData.category != SkillCategory.Passive)
            {
                if (equippedSkillIds.Count < MaxEquippedSkills)
                {
                    EquipSkill(skillData.skillId);
                }
            }
            else
            {
                if (equippedPassiveIds.Count < (config?.maxPassiveSkills ?? 4))
                {
                    EquipPassive(skillData.skillId);
                }
            }

            OnSkillLearned?.Invoke(learnedSkill);
            return true;
        }

        /// <summary>
        /// Checks if a skill can be learned.
        /// </summary>
        public bool CanLearnSkill(SkillData skillData, out string reason)
        {
            reason = "";

            if (skillData == null)
            {
                reason = "Invalid skill data";
                return false;
            }

            // Check character level
            int charLevel = statsSystem?.Level ?? 1;
            if (charLevel < skillData.requiredCharacterLevel)
            {
                reason = $"Requires character level {skillData.requiredCharacterLevel}";
                return false;
            }

            // Check prerequisites
            foreach (var prereqId in skillData.prerequisiteSkills)
            {
                if (!HasSkill(prereqId))
                {
                    var prereq = GetSkillData(prereqId);
                    reason = $"Requires {prereq?.skillName ?? prereqId}";
                    return false;
                }
            }

            // Check skill points
            if (availableSkillPoints < skillData.learnPointCost)
            {
                reason = $"Need {skillData.learnPointCost} skill points";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if a skill has been learned.
        /// </summary>
        public bool HasSkill(string skillId)
        {
            return learnedSkills.ContainsKey(skillId);
        }

        /// <summary>
        /// Gets a learned skill by ID.
        /// </summary>
        public LearnedSkill GetLearnedSkill(string skillId)
        {
            learnedSkills.TryGetValue(skillId, out var skill);
            return skill;
        }

        /// <summary>
        /// Gets skill data by ID.
        /// </summary>
        public SkillData GetSkillData(string skillId)
        {
            return config?.allSkills.FirstOrDefault(s => s.skillId == skillId);
        }

        /// <summary>
        /// Gets all learned skills.
        /// </summary>
        public List<LearnedSkill> GetAllLearnedSkills()
        {
            return learnedSkills.Values.ToList();
        }

        /// <summary>
        /// Gets skills that can be learned.
        /// </summary>
        public List<SkillData> GetLearnableSkills()
        {
            if (config == null) return new List<SkillData>();

            return config.allSkills
                .Where(s => !HasSkill(s.skillId) && CanLearnSkill(s, out _))
                .ToList();
        }

        #endregion

        #region Skill Experience & Leveling

        /// <summary>
        /// Adds experience to a skill.
        /// </summary>
        public int AddSkillExperience(string skillId, int amount)
        {
            if (!learnedSkills.TryGetValue(skillId, out var skill)) return 0;
            return skill.AddExperience(amount);
        }

        /// <summary>
        /// Adds experience to a skill based on usage.
        /// </summary>
        public void OnSkillUsedForExp(string skillId, bool killedTarget = false, bool wasCritical = false)
        {
            if (!learnedSkills.TryGetValue(skillId, out var skill)) return;

            int baseExp = config?.baseExpPerUse ?? 10;
            float multiplier = 1f;

            if (killedTarget)
            {
                multiplier *= config?.expMultiplierOnKill ?? 2f;
            }
            if (wasCritical)
            {
                multiplier *= config?.expMultiplierOnCrit ?? 1.5f;
            }

            skill.AddExperience((int)(baseExp * multiplier));
        }

        /// <summary>
        /// Upgrades a skill to the next level using skill points.
        /// </summary>
        public bool UpgradeSkill(string skillId)
        {
            if (!learnedSkills.TryGetValue(skillId, out var skill)) return false;
            if (skill.IsMaxLevel) return false;
            if (availableSkillPoints < 1) return false;

            availableSkillPoints--;
            skill.currentExp = skill.ExpToNextLevel; // Fill to next level
            skill.AddExperience(1); // Trigger level up
            
            OnSkillPointsChanged?.Invoke(availableSkillPoints);
            return true;
        }

        #endregion

        #region Equipping Skills

        /// <summary>
        /// Equips a skill to the active skill bar.
        /// </summary>
        public bool EquipSkill(string skillId, int slotIndex = -1)
        {
            if (!learnedSkills.TryGetValue(skillId, out var skill)) return false;
            if (skill.skillData.category == SkillCategory.Passive)
            {
                return EquipPassive(skillId);
            }
            if (equippedSkillIds.Contains(skillId)) return false;

            if (slotIndex >= 0 && slotIndex < MaxEquippedSkills)
            {
                // Replace existing skill at slot
                if (slotIndex < equippedSkillIds.Count)
                {
                    var existingId = equippedSkillIds[slotIndex];
                    learnedSkills[existingId].isEquipped = false;
                    learnedSkills[existingId].slotIndex = -1;
                    equippedSkillIds[slotIndex] = skillId;
                }
                else
                {
                    // Fill slots up to the index
                    while (equippedSkillIds.Count <= slotIndex)
                    {
                        equippedSkillIds.Add(null);
                    }
                    equippedSkillIds[slotIndex] = skillId;
                }
            }
            else if (equippedSkillIds.Count < MaxEquippedSkills)
            {
                slotIndex = equippedSkillIds.Count;
                equippedSkillIds.Add(skillId);
            }
            else
            {
                return false; // No room
            }

            skill.isEquipped = true;
            skill.slotIndex = slotIndex;
            OnSkillEquipped?.Invoke(skillId);
            return true;
        }

        /// <summary>
        /// Unequips a skill from the active skill bar.
        /// </summary>
        public bool UnequipSkill(string skillId)
        {
            if (!equippedSkillIds.Contains(skillId)) return false;
            if (!learnedSkills.TryGetValue(skillId, out var skill)) return false;

            equippedSkillIds.Remove(skillId);
            skill.isEquipped = false;
            skill.slotIndex = -1;
            OnSkillUnequipped?.Invoke(skillId);
            return true;
        }

        /// <summary>
        /// Equips a passive skill.
        /// </summary>
        public bool EquipPassive(string skillId)
        {
            if (!learnedSkills.TryGetValue(skillId, out var skill)) return false;
            if (skill.skillData.category != SkillCategory.Passive) return false;
            if (equippedPassiveIds.Contains(skillId)) return false;
            if (equippedPassiveIds.Count >= (config?.maxPassiveSkills ?? 4)) return false;

            equippedPassiveIds.Add(skillId);
            skill.isEquipped = true;
            ApplyPassiveEffects(skill);
            OnSkillEquipped?.Invoke(skillId);
            return true;
        }

        /// <summary>
        /// Unequips a passive skill.
        /// </summary>
        public bool UnequipPassive(string skillId)
        {
            if (!equippedPassiveIds.Contains(skillId)) return false;
            if (!learnedSkills.TryGetValue(skillId, out var skill)) return false;

            equippedPassiveIds.Remove(skillId);
            skill.isEquipped = false;
            RemovePassiveEffects(skill);
            OnSkillUnequipped?.Invoke(skillId);
            return true;
        }

        /// <summary>
        /// Swaps two equipped skills.
        /// </summary>
        public void SwapSkillSlots(int slotA, int slotB)
        {
            if (slotA < 0 || slotA >= equippedSkillIds.Count) return;
            if (slotB < 0 || slotB >= equippedSkillIds.Count) return;

            (equippedSkillIds[slotA], equippedSkillIds[slotB]) = (equippedSkillIds[slotB], equippedSkillIds[slotA]);

            // Update slot indices
            if (learnedSkills.TryGetValue(equippedSkillIds[slotA], out var skillA))
            {
                skillA.slotIndex = slotA;
            }
            if (learnedSkills.TryGetValue(equippedSkillIds[slotB], out var skillB))
            {
                skillB.slotIndex = slotB;
            }
        }

        private void ApplyPassiveEffects(LearnedSkill skill)
        {
            if (statsSystem == null || skill.skillData == null) return;

            foreach (var effect in skill.skillData.effects)
            {
                float value = skill.GetEffectValue(skill.skillData.effects.IndexOf(effect));
                // Would apply stat modifiers based on effect type
            }
        }

        private void RemovePassiveEffects(LearnedSkill skill)
        {
            // Would remove stat modifiers applied by the passive
        }

        #endregion

        #region Using Skills

        /// <summary>
        /// Checks if a skill can be used.
        /// </summary>
        public bool CanUseSkill(string skillId, out string reason)
        {
            reason = "";

            if (!learnedSkills.TryGetValue(skillId, out var skill))
            {
                reason = "Skill not learned";
                return false;
            }

            if (!skill.isEquipped && skill.skillData.category != SkillCategory.Passive)
            {
                reason = "Skill not equipped";
                return false;
            }

            if (IsOnCooldown(skillId))
            {
                reason = $"On cooldown ({GetCooldownRemaining(skillId):F1}s)";
                return false;
            }

            float mpCost = skill.CurrentMPCost;
            if (statsSystem != null && !statsSystem.HasEnoughMana(mpCost))
            {
                reason = "Not enough MP";
                return false;
            }

            float spCost = skill.skillData.spCost;
            if (spCost > 0 && statsSystem != null && !statsSystem.HasEnoughStamina(spCost))
            {
                reason = "Not enough SP";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Uses a skill, consuming resources and starting cooldown.
        /// </summary>
        public bool UseSkill(string skillId)
        {
            if (!CanUseSkill(skillId, out string reason))
            {
                Debug.Log($"Cannot use {skillId}: {reason}");
                return false;
            }

            var skill = learnedSkills[skillId];

            // Consume resources
            if (statsSystem != null)
            {
                statsSystem.ModifyMana(-skill.CurrentMPCost);
                if (skill.skillData.spCost > 0)
                {
                    statsSystem.ModifyStamina(-skill.skillData.spCost);
                }
                if (skill.skillData.hpCost > 0)
                {
                    statsSystem.ModifyHealth(-skill.skillData.hpCost);
                }
            }

            // Start cooldown
            StartCooldown(skillId, skill.CurrentCooldown);

            // Track usage
            skill.totalUsageCount++;

            OnSkillUsed?.Invoke(skillId);
            return true;
        }

        /// <summary>
        /// Gets a skill by its equipped slot index.
        /// </summary>
        public LearnedSkill GetSkillAtSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= equippedSkillIds.Count) return null;
            var skillId = equippedSkillIds[slotIndex];
            return string.IsNullOrEmpty(skillId) ? null : GetLearnedSkill(skillId);
        }

        #endregion

        #region Cooldowns

        /// <summary>
        /// Starts a cooldown for a skill.
        /// </summary>
        public void StartCooldown(string skillId, float duration)
        {
            cooldowns[skillId] = new SkillCooldown
            {
                skillId = skillId,
                remainingTime = duration,
                totalTime = duration
            };
            OnSkillCooldownStarted?.Invoke(skillId, duration);
        }

        /// <summary>
        /// Checks if a skill is on cooldown.
        /// </summary>
        public bool IsOnCooldown(string skillId)
        {
            return cooldowns.ContainsKey(skillId) && !cooldowns[skillId].IsReady;
        }

        /// <summary>
        /// Gets the remaining cooldown time for a skill.
        /// </summary>
        public float GetCooldownRemaining(string skillId)
        {
            if (cooldowns.TryGetValue(skillId, out var cd))
            {
                return cd.remainingTime;
            }
            return 0f;
        }

        /// <summary>
        /// Gets the cooldown progress (0-1) for a skill.
        /// </summary>
        public float GetCooldownProgress(string skillId)
        {
            if (cooldowns.TryGetValue(skillId, out var cd))
            {
                return cd.Progress;
            }
            return 1f;
        }

        /// <summary>
        /// Resets all skill cooldowns.
        /// </summary>
        public void ResetAllCooldowns()
        {
            var skillIds = cooldowns.Keys.ToList();
            cooldowns.Clear();
            foreach (var skillId in skillIds)
            {
                OnSkillCooldownEnded?.Invoke(skillId);
            }
        }

        /// <summary>
        /// Reduces cooldown of a specific skill.
        /// </summary>
        public void ReduceCooldown(string skillId, float amount)
        {
            if (cooldowns.TryGetValue(skillId, out var cd))
            {
                cd.remainingTime = Mathf.Max(0, cd.remainingTime - amount);
                if (cd.IsReady)
                {
                    cooldowns.Remove(skillId);
                    OnSkillCooldownEnded?.Invoke(skillId);
                }
            }
        }

        #endregion

        #region Skill Inheritance

        /// <summary>
        /// Inherits a skill from another character/class.
        /// </summary>
        public bool InheritSkill(string skillId)
        {
            if (!config?.enableSkillInheritance ?? false) return false;

            var skillData = GetSkillData(skillId);
            if (skillData == null || !skillData.canInherit) return false;

            if (inheritedSkillIds.Count >= (config?.maxInheritedSkills ?? 3)) return false;
            if (learnedSkills.ContainsKey(skillId)) return false;

            int cost = skillData.inheritCost;
            if (availableSkillPoints < cost) return false;

            availableSkillPoints -= cost;
            OnSkillPointsChanged?.Invoke(availableSkillPoints);

            var inheritedSkill = new LearnedSkill(skillData)
            {
                isInherited = true,
                inheritedEffectiveness = skillData.inheritEffectiveness
            };

            learnedSkills[skillId] = inheritedSkill;
            inheritedSkillIds.Add(skillId);

            OnSkillInherited?.Invoke(inheritedSkill);
            return true;
        }

        /// <summary>
        /// Removes an inherited skill.
        /// </summary>
        public bool ForgetInheritedSkill(string skillId)
        {
            if (!inheritedSkillIds.Contains(skillId)) return false;

            UnequipSkill(skillId);
            inheritedSkillIds.Remove(skillId);
            learnedSkills.Remove(skillId);
            return true;
        }

        /// <summary>
        /// Gets all inherited skills.
        /// </summary>
        public List<LearnedSkill> GetInheritedSkills()
        {
            return inheritedSkillIds
                .Where(id => learnedSkills.ContainsKey(id))
                .Select(id => learnedSkills[id])
                .ToList();
        }

        #endregion

        #region Skill Points

        /// <summary>
        /// Adds skill points.
        /// </summary>
        public void AddSkillPoints(int amount)
        {
            availableSkillPoints += amount;
            totalSkillPointsEarned += amount;
            OnSkillPointsChanged?.Invoke(availableSkillPoints);
        }

        /// <summary>
        /// Resets all skill points (respec).
        /// </summary>
        public void RespecSkills()
        {
            // Unequip all skills
            foreach (var skillId in equippedSkillIds.ToList())
            {
                UnequipSkill(skillId);
            }
            foreach (var skillId in equippedPassiveIds.ToList())
            {
                UnequipPassive(skillId);
            }

            // Refund skill points
            int refund = 0;
            foreach (var skill in learnedSkills.Values.Where(s => !s.isInherited))
            {
                refund += skill.skillData?.learnPointCost ?? 1;
                refund += skill.currentLevel - 1; // Extra points from upgrades
            }

            // Clear learned skills (except inherited)
            var toRemove = learnedSkills.Keys.Where(id => !inheritedSkillIds.Contains(id)).ToList();
            foreach (var id in toRemove)
            {
                learnedSkills.Remove(id);
            }

            availableSkillPoints = totalSkillPointsEarned;
            OnSkillPointsChanged?.Invoke(availableSkillPoints);
        }

        #endregion

        #region Utility

        /// <summary>
        /// Gets a summary of the skill system state.
        /// </summary>
        public string GetSkillSummary()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== Skill Summary ===");
            sb.AppendLine($"Skill Points: {availableSkillPoints}");
            sb.AppendLine($"Total Earned: {totalSkillPointsEarned}");
            sb.AppendLine($"Learned Skills: {learnedSkills.Count}");
            sb.AppendLine($"Equipped: {equippedSkillIds.Count}/{MaxEquippedSkills}");
            sb.AppendLine($"Passives: {equippedPassiveIds.Count}/{config?.maxPassiveSkills ?? 4}");
            sb.AppendLine($"Inherited: {inheritedSkillIds.Count}/{config?.maxInheritedSkills ?? 3}");
            sb.AppendLine();
            sb.AppendLine("--- Equipped Skills ---");
            foreach (var skillId in equippedSkillIds)
            {
                if (learnedSkills.TryGetValue(skillId, out var skill))
                {
                    sb.AppendLine($"  [{skill.slotIndex}] {skill.skillData.skillName} Lv.{skill.currentLevel}");
                }
            }
            sb.AppendLine();
            sb.AppendLine("--- Equipped Passives ---");
            foreach (var skillId in equippedPassiveIds)
            {
                if (learnedSkills.TryGetValue(skillId, out var skill))
                {
                    sb.AppendLine($"  {skill.skillData.skillName} Lv.{skill.currentLevel}");
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Creates save data for the skill system.
        /// </summary>
        public SkillSystemSaveData CreateSaveData()
        {
            return new SkillSystemSaveData
            {
                availableSkillPoints = availableSkillPoints,
                totalSkillPointsEarned = totalSkillPointsEarned,
                learnedSkills = learnedSkills.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new LearnedSkillSaveData
                    {
                        currentLevel = kvp.Value.currentLevel,
                        currentExp = kvp.Value.currentExp,
                        totalUsageCount = kvp.Value.totalUsageCount,
                        isInherited = kvp.Value.isInherited,
                        inheritedEffectiveness = kvp.Value.inheritedEffectiveness,
                        isEquipped = kvp.Value.isEquipped,
                        slotIndex = kvp.Value.slotIndex
                    }
                ),
                equippedSkillIds = new List<string>(equippedSkillIds),
                equippedPassiveIds = new List<string>(equippedPassiveIds),
                inheritedSkillIds = new List<string>(inheritedSkillIds)
            };
        }

        /// <summary>
        /// Loads skill system state from save data.
        /// </summary>
        public void LoadSaveData(SkillSystemSaveData saveData)
        {
            if (saveData == null) return;

            availableSkillPoints = saveData.availableSkillPoints;
            totalSkillPointsEarned = saveData.totalSkillPointsEarned;
            equippedSkillIds = new List<string>(saveData.equippedSkillIds);
            equippedPassiveIds = new List<string>(saveData.equippedPassiveIds);
            inheritedSkillIds = new List<string>(saveData.inheritedSkillIds);

            foreach (var kvp in saveData.learnedSkills)
            {
                var skillData = GetSkillData(kvp.Key);
                if (skillData == null) continue;

                var skill = new LearnedSkill(skillData)
                {
                    currentLevel = kvp.Value.currentLevel,
                    currentExp = kvp.Value.currentExp,
                    totalUsageCount = kvp.Value.totalUsageCount,
                    isInherited = kvp.Value.isInherited,
                    inheritedEffectiveness = kvp.Value.inheritedEffectiveness,
                    isEquipped = kvp.Value.isEquipped,
                    slotIndex = kvp.Value.slotIndex
                };

                learnedSkills[kvp.Key] = skill;
            }
        }

        #endregion
    }

    /// <summary>
    /// Serializable save data for skill system.
    /// </summary>
    [Serializable]
    public class SkillSystemSaveData
    {
        public int availableSkillPoints;
        public int totalSkillPointsEarned;
        public Dictionary<string, LearnedSkillSaveData> learnedSkills;
        public List<string> equippedSkillIds;
        public List<string> equippedPassiveIds;
        public List<string> inheritedSkillIds;
    }

    /// <summary>
    /// Serializable save data for a learned skill.
    /// </summary>
    [Serializable]
    public class LearnedSkillSaveData
    {
        public int currentLevel;
        public int currentExp;
        public int totalUsageCount;
        public bool isInherited;
        public float inheritedEffectiveness;
        public bool isEquipped;
        public int slotIndex;
    }
}
