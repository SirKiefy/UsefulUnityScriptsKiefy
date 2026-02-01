using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UsefulScripts.RPG
{
    /// <summary>
    /// Defines the category of a character class.
    /// </summary>
    public enum ClassCategory
    {
        Basic,          // Starting classes
        Advanced,       // Requires basic class mastery
        Elite,          // Requires advanced class mastery
        Legendary,      // Special unlock conditions
        Hybrid          // Combines multiple class types
    }

    /// <summary>
    /// Defines the archetype/role of a class.
    /// </summary>
    public enum ClassArchetype
    {
        Tank,           // High defense, aggro management
        Warrior,        // Physical DPS
        Mage,           // Magical DPS
        Healer,         // Support, healing
        Assassin,       // Burst damage, stealth
        Ranger,         // Ranged attacks
        Support,        // Buffs, debuffs
        Summoner,       // Pet/summon based
        Hybrid          // Multiple roles
    }

    /// <summary>
    /// Represents a requirement for unlocking a class.
    /// </summary>
    [Serializable]
    public class ClassRequirement
    {
        public ClassRequirementType requirementType;
        public string targetId;         // Class ID, skill ID, item ID, etc.
        public int requiredValue;       // Level, count, etc.
        public string description;

        public bool IsMet(ClassSystem classSystem)
        {
            return requirementType switch
            {
                ClassRequirementType.CharacterLevel => 
                    classSystem.CharacterLevel >= requiredValue,
                ClassRequirementType.ClassLevel => 
                    classSystem.GetClassLevel(targetId) >= requiredValue,
                ClassRequirementType.ClassMastered => 
                    classSystem.IsClassMastered(targetId),
                ClassRequirementType.MultipleClassesMastered => 
                    classSystem.GetMasteredClassCount() >= requiredValue,
                ClassRequirementType.SkillLearned => 
                    classSystem.HasSkill(targetId),
                ClassRequirementType.StatRequirement => 
                    CheckStatRequirement(classSystem),
                ClassRequirementType.QuestCompleted => 
                    true, // Would check quest system
                ClassRequirementType.ItemOwned => 
                    true, // Would check inventory
                _ => false
            };
        }

        private bool CheckStatRequirement(ClassSystem classSystem)
        {
            // Would integrate with CharacterStatsSystem
            return true;
        }
    }

    public enum ClassRequirementType
    {
        CharacterLevel,
        ClassLevel,
        ClassMastered,
        MultipleClassesMastered,
        SkillLearned,
        StatRequirement,
        QuestCompleted,
        ItemOwned
    }

    /// <summary>
    /// Represents stat bonuses from a class.
    /// </summary>
    [Serializable]
    public class ClassStatBonus
    {
        public PrimaryAttribute attribute;
        public float flatBonus;
        public float percentBonus;
        public float bonusPerLevel;
    }

    /// <summary>
    /// Represents a skill that can be learned from a class.
    /// </summary>
    [Serializable]
    public class ClassSkill
    {
        public string skillId;
        public string skillName;
        [TextArea(2, 4)]
        public string description;
        public Sprite icon;
        
        [Header("Learning Requirements")]
        public int requiredClassLevel = 1;
        public int skillPointCost = 1;
        public List<string> prerequisiteSkills = new List<string>();
        
        [Header("Skill Properties")]
        public SkillType skillType = SkillType.Active;
        public bool isClassExclusive = false;
        public bool canInherit = true;
        public float inheritEffectiveness = 0.8f;
        
        [Header("Combat Data")]
        public CombatAction combatAction;
        
        [Header("Passive Effects")]
        public List<PassiveEffect> passiveEffects = new List<PassiveEffect>();
    }

    public enum SkillType
    {
        Active,         // Usable in combat
        Passive,        // Always active
        Reaction,       // Triggers on conditions
        Command,        // Battle menu command
        Support         // Equippable passive
    }

    /// <summary>
    /// Represents a passive effect from a skill.
    /// </summary>
    [Serializable]
    public class PassiveEffect
    {
        public PassiveEffectType effectType;
        public float value;
        public bool isPercentage;
        public string condition;
    }

    public enum PassiveEffectType
    {
        IncreaseHP,
        IncreaseMP,
        IncreaseAttack,
        IncreaseDefense,
        IncreaseMagicAttack,
        IncreaseMagicDefense,
        IncreaseSpeed,
        IncreaseCritChance,
        IncreaseCritDamage,
        IncreaseEvasion,
        IncreaseAccuracy,
        ReduceManaCost,
        ReduceCooldowns,
        CounterAttackChance,
        AutoRevive,
        ElementalResistance,
        StatusResistance,
        ExperienceBonus,
        GoldBonus,
        DropRateBonus,
        DualWield,
        TwoHandBonus,
        EquipShield,
        EquipHeavyArmor
    }

    /// <summary>
    /// Character class definition as a ScriptableObject.
    /// </summary>
    [CreateAssetMenu(fileName = "NewClass", menuName = "UsefulScripts/RPG/Character Class")]
    public class CharacterClassData : ScriptableObject
    {
        [Header("Basic Info")]
        public string classId;
        public string className;
        [TextArea(3, 5)]
        public string description;
        public Sprite classIcon;
        public ClassCategory category = ClassCategory.Basic;
        public ClassArchetype archetype = ClassArchetype.Warrior;

        [Header("Leveling")]
        public int maxClassLevel = 20;
        public int baseExpPerLevel = 100;
        public float expScaling = 1.5f;

        [Header("Stats")]
        public List<ClassStatBonus> statBonuses = new List<ClassStatBonus>();
        public List<DerivedStat> favoredStats = new List<DerivedStat>();

        [Header("Skills")]
        public List<ClassSkill> classSkills = new List<ClassSkill>();
        public int masterySkillCount = 5;

        [Header("Requirements")]
        public List<ClassRequirement> requirements = new List<ClassRequirement>();
        public List<string> incompatibleClasses = new List<string>();

        [Header("Mastery")]
        public List<ClassSkill> masteryBonuses = new List<ClassSkill>();
        public string masteryTitle;

        [Header("Equipment")]
        public List<string> allowedWeaponTypes = new List<string>();
        public List<string> allowedArmorTypes = new List<string>();

        [Header("Visual")]
        public GameObject classPrefab;
        public RuntimeAnimatorController classAnimator;

        /// <summary>
        /// Gets the experience required to reach a specific class level.
        /// </summary>
        public int GetExpForLevel(int level)
        {
            return (int)(baseExpPerLevel * Mathf.Pow(level, expScaling));
        }

        /// <summary>
        /// Gets all skills available at a specific class level.
        /// </summary>
        public List<ClassSkill> GetSkillsAtLevel(int level)
        {
            return classSkills.Where(s => s.requiredClassLevel <= level).ToList();
        }
    }

    /// <summary>
    /// Represents a character's progress in a specific class.
    /// </summary>
    [Serializable]
    public class ClassProgress
    {
        public string classId;
        public CharacterClassData classData;
        public int currentLevel = 1;
        public int currentExp = 0;
        public int totalExpEarned = 0;
        public bool isMastered = false;
        public DateTime? masteredDate;
        public List<string> learnedSkills = new List<string>();
        public int skillPointsEarned = 0;
        public int skillPointsSpent = 0;

        public int ExpToNextLevel => classData?.GetExpForLevel(currentLevel + 1) ?? int.MaxValue;
        public float LevelProgress => (float)currentExp / ExpToNextLevel;
        public bool IsMaxLevel => currentLevel >= (classData?.maxClassLevel ?? 20);
        public int AvailableSkillPoints => skillPointsEarned - skillPointsSpent;

        /// <summary>
        /// Adds experience to this class.
        /// </summary>
        public int AddExperience(int amount)
        {
            if (IsMaxLevel) return 0;

            currentExp += amount;
            totalExpEarned += amount;
            int levelsGained = 0;

            while (currentExp >= ExpToNextLevel && !IsMaxLevel)
            {
                currentExp -= ExpToNextLevel;
                currentLevel++;
                skillPointsEarned++;
                levelsGained++;
            }

            // Check mastery
            if (IsMaxLevel && !isMastered)
            {
                CheckMastery();
            }

            return levelsGained;
        }

        private void CheckMastery()
        {
            if (classData == null) return;

            int masteryCount = classData.masterySkillCount;
            if (learnedSkills.Count >= masteryCount)
            {
                isMastered = true;
                masteredDate = DateTime.Now;
            }
        }
    }

    /// <summary>
    /// Represents a skill node in a skill tree.
    /// </summary>
    [Serializable]
    public class SkillTreeNode
    {
        public string nodeId;
        public ClassSkill skill;
        public Vector2 position;
        public List<string> parentNodeIds = new List<string>();
        public bool isUnlocked;
        public bool isLearned;
        public int tier;
    }

    /// <summary>
    /// Represents a complete skill tree for a class.
    /// </summary>
    [Serializable]
    public class SkillTree
    {
        public string classId;
        public List<SkillTreeNode> nodes = new List<SkillTreeNode>();
        public int maxTiers = 5;

        public SkillTreeNode GetNode(string nodeId)
        {
            return nodes.FirstOrDefault(n => n.nodeId == nodeId);
        }

        public List<SkillTreeNode> GetNodesAtTier(int tier)
        {
            return nodes.Where(n => n.tier == tier).ToList();
        }

        public bool CanLearnNode(string nodeId, List<string> learnedSkills)
        {
            var node = GetNode(nodeId);
            if (node == null || node.isLearned) return false;

            // Check parent requirements
            return node.parentNodeIds.All(parentId =>
            {
                var parentNode = GetNode(parentId);
                return parentNode?.isLearned == true || learnedSkills.Contains(parentId);
            });
        }
    }

    /// <summary>
    /// Complete class/job system managing class changes, skill learning, and mastery.
    /// </summary>
    public class ClassSystem : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private List<CharacterClassData> availableClasses = new List<CharacterClassData>();
        [SerializeField] private int maxEquippedPassives = 5;
        [SerializeField] private int maxInheritedSkills = 3;
        [SerializeField] private bool allowMulticlassing = true;
        [SerializeField] private float secondaryClassExpRate = 0.5f;

        [Header("Current State")]
        [SerializeField] private CharacterClassData currentClass;
        [SerializeField] private CharacterClassData secondaryClass;
        [SerializeField] private int characterLevel = 1;

        // Runtime data
        private Dictionary<string, ClassProgress> classProgressMap = new Dictionary<string, ClassProgress>();
        private List<string> activeSkills = new List<string>();
        private List<string> equippedPassives = new List<string>();
        private List<string> inheritedSkills = new List<string>();
        private Dictionary<string, SkillTree> skillTrees = new Dictionary<string, SkillTree>();

        // Reference to stats system
        private CharacterStatsSystem statsSystem;

        // Events
        public event Action<CharacterClassData, CharacterClassData> OnClassChanged;
        public event Action<string, int> OnClassLevelUp;
        public event Action<string> OnClassMastered;
        public event Action<ClassSkill> OnSkillLearned;
        public event Action<ClassSkill> OnSkillEquipped;
        public event Action<ClassSkill> OnSkillUnequipped;
        public event Action<List<ClassRequirement>> OnRequirementsNotMet;

        // Properties
        public CharacterClassData CurrentClass => currentClass;
        public CharacterClassData SecondaryClass => secondaryClass;
        public int CharacterLevel => characterLevel;
        public ClassProgress CurrentClassProgress => GetClassProgress(currentClass?.classId);
        public IReadOnlyList<CharacterClassData> AvailableClasses => availableClasses.AsReadOnly();
        public IReadOnlyList<string> ActiveSkills => activeSkills.AsReadOnly();
        public IReadOnlyList<string> EquippedPassives => equippedPassives.AsReadOnly();
        public IReadOnlyList<string> InheritedSkills => inheritedSkills.AsReadOnly();

        private void Awake()
        {
            statsSystem = GetComponent<CharacterStatsSystem>();
            InitializeClassProgress();
        }

        private void InitializeClassProgress()
        {
            foreach (var classData in availableClasses)
            {
                if (!classProgressMap.ContainsKey(classData.classId))
                {
                    classProgressMap[classData.classId] = new ClassProgress
                    {
                        classId = classData.classId,
                        classData = classData
                    };
                }

                // Build skill tree for this class
                BuildSkillTree(classData);
            }
        }

        private void BuildSkillTree(CharacterClassData classData)
        {
            var tree = new SkillTree { classId = classData.classId };
            int tier = 0;
            
            foreach (var skill in classData.classSkills.OrderBy(s => s.requiredClassLevel))
            {
                var node = new SkillTreeNode
                {
                    nodeId = skill.skillId,
                    skill = skill,
                    tier = skill.requiredClassLevel / 4,
                    parentNodeIds = new List<string>(skill.prerequisiteSkills)
                };
                tree.nodes.Add(node);
            }

            skillTrees[classData.classId] = tree;
        }

        #region Class Management

        /// <summary>
        /// Changes to a new class.
        /// </summary>
        public bool ChangeClass(string classId)
        {
            var newClass = availableClasses.FirstOrDefault(c => c.classId == classId);
            if (newClass == null) return false;

            // Check requirements
            if (!CanUnlockClass(classId))
            {
                var unmetRequirements = GetUnmetRequirements(classId);
                OnRequirementsNotMet?.Invoke(unmetRequirements);
                return false;
            }

            var previousClass = currentClass;
            
            // Handle secondary class if multiclassing
            if (allowMulticlassing && currentClass != null)
            {
                secondaryClass = previousClass;
            }

            currentClass = newClass;

            // Initialize progress if new
            if (!classProgressMap.ContainsKey(classId))
            {
                classProgressMap[classId] = new ClassProgress
                {
                    classId = classId,
                    classData = newClass
                };
            }

            // Apply class stat bonuses
            ApplyClassStatBonuses(newClass);

            // Update available skills
            UpdateActiveSkills();

            OnClassChanged?.Invoke(previousClass, currentClass);
            return true;
        }

        /// <summary>
        /// Sets the secondary class for dual-classing.
        /// </summary>
        public bool SetSecondaryClass(string classId)
        {
            if (!allowMulticlassing) return false;
            if (classId == currentClass?.classId) return false;

            var newSecondary = availableClasses.FirstOrDefault(c => c.classId == classId);
            if (newSecondary == null) return false;

            if (!CanUnlockClass(classId)) return false;

            secondaryClass = newSecondary;
            UpdateActiveSkills();
            return true;
        }

        /// <summary>
        /// Clears the secondary class.
        /// </summary>
        public void ClearSecondaryClass()
        {
            secondaryClass = null;
            UpdateActiveSkills();
        }

        /// <summary>
        /// Checks if a class can be unlocked.
        /// </summary>
        public bool CanUnlockClass(string classId)
        {
            var classData = availableClasses.FirstOrDefault(c => c.classId == classId);
            if (classData == null) return false;

            // Check all requirements
            return classData.requirements.All(req => req.IsMet(this));
        }

        /// <summary>
        /// Gets the requirements that haven't been met for a class.
        /// </summary>
        public List<ClassRequirement> GetUnmetRequirements(string classId)
        {
            var classData = availableClasses.FirstOrDefault(c => c.classId == classId);
            if (classData == null) return new List<ClassRequirement>();

            return classData.requirements.Where(req => !req.IsMet(this)).ToList();
        }

        /// <summary>
        /// Gets all classes that can currently be unlocked.
        /// </summary>
        public List<CharacterClassData> GetUnlockableClasses()
        {
            return availableClasses.Where(c => CanUnlockClass(c.classId)).ToList();
        }

        /// <summary>
        /// Gets all classes that have been mastered.
        /// </summary>
        public List<CharacterClassData> GetMasteredClasses()
        {
            return availableClasses.Where(c =>
                classProgressMap.TryGetValue(c.classId, out var progress) && progress.isMastered
            ).ToList();
        }

        /// <summary>
        /// Gets the count of mastered classes.
        /// </summary>
        public int GetMasteredClassCount()
        {
            return classProgressMap.Values.Count(p => p.isMastered);
        }

        /// <summary>
        /// Checks if a specific class has been mastered.
        /// </summary>
        public bool IsClassMastered(string classId)
        {
            return classProgressMap.TryGetValue(classId, out var progress) && progress.isMastered;
        }

        #endregion

        #region Experience & Leveling

        /// <summary>
        /// Adds class experience to the current class.
        /// </summary>
        public void AddClassExperience(int amount)
        {
            if (currentClass == null) return;

            var progress = GetClassProgress(currentClass.classId);
            if (progress == null) return;

            int levelsGained = progress.AddExperience(amount);

            if (levelsGained > 0)
            {
                OnClassLevelUp?.Invoke(currentClass.classId, progress.currentLevel);

                if (progress.isMastered)
                {
                    OnClassMastered?.Invoke(currentClass.classId);
                }
            }

            // Add experience to secondary class at reduced rate
            if (secondaryClass != null && allowMulticlassing)
            {
                var secondaryProgress = GetClassProgress(secondaryClass.classId);
                if (secondaryProgress != null)
                {
                    int secondaryAmount = (int)(amount * secondaryClassExpRate);
                    secondaryProgress.AddExperience(secondaryAmount);
                }
            }

            UpdateActiveSkills();
        }

        /// <summary>
        /// Gets the current level of a class.
        /// </summary>
        public int GetClassLevel(string classId)
        {
            return classProgressMap.TryGetValue(classId, out var progress) ? progress.currentLevel : 0;
        }

        /// <summary>
        /// Gets the progress data for a class.
        /// </summary>
        public ClassProgress GetClassProgress(string classId)
        {
            classProgressMap.TryGetValue(classId, out var progress);
            return progress;
        }

        /// <summary>
        /// Sets the character's base level.
        /// </summary>
        public void SetCharacterLevel(int level)
        {
            characterLevel = Mathf.Max(1, level);
        }

        #endregion

        #region Skills

        /// <summary>
        /// Learns a skill from the current class.
        /// </summary>
        public bool LearnSkill(string skillId)
        {
            if (currentClass == null) return false;

            var skill = currentClass.classSkills.FirstOrDefault(s => s.skillId == skillId);
            if (skill == null) return false;

            var progress = GetClassProgress(currentClass.classId);
            if (progress == null) return false;

            // Check if already learned
            if (progress.learnedSkills.Contains(skillId)) return false;

            // Check requirements
            if (progress.currentLevel < skill.requiredClassLevel) return false;
            if (progress.AvailableSkillPoints < skill.skillPointCost) return false;

            // Check prerequisites
            if (!skill.prerequisiteSkills.All(prereq => progress.learnedSkills.Contains(prereq)))
            {
                return false;
            }

            // Learn the skill
            progress.learnedSkills.Add(skillId);
            progress.skillPointsSpent += skill.skillPointCost;

            // Update skill tree
            if (skillTrees.TryGetValue(currentClass.classId, out var tree))
            {
                var node = tree.GetNode(skillId);
                if (node != null)
                {
                    node.isLearned = true;
                }
            }

            UpdateActiveSkills();
            OnSkillLearned?.Invoke(skill);
            return true;
        }

        /// <summary>
        /// Checks if a skill has been learned.
        /// </summary>
        public bool HasSkill(string skillId)
        {
            // Check current class
            if (currentClass != null)
            {
                var progress = GetClassProgress(currentClass.classId);
                if (progress?.learnedSkills.Contains(skillId) == true) return true;
            }

            // Check secondary class
            if (secondaryClass != null && allowMulticlassing)
            {
                var progress = GetClassProgress(secondaryClass.classId);
                if (progress?.learnedSkills.Contains(skillId) == true) return true;
            }

            // Check inherited skills
            if (inheritedSkills.Contains(skillId)) return true;

            return false;
        }

        /// <summary>
        /// Gets all learned skills from all classes.
        /// </summary>
        public List<ClassSkill> GetAllLearnedSkills()
        {
            var skills = new List<ClassSkill>();

            foreach (var classData in availableClasses)
            {
                if (classProgressMap.TryGetValue(classData.classId, out var progress))
                {
                    foreach (var skillId in progress.learnedSkills)
                    {
                        var skill = classData.classSkills.FirstOrDefault(s => s.skillId == skillId);
                        if (skill != null)
                        {
                            skills.Add(skill);
                        }
                    }
                }
            }

            return skills;
        }

        /// <summary>
        /// Equips a passive skill.
        /// </summary>
        public bool EquipPassive(string skillId)
        {
            if (equippedPassives.Count >= maxEquippedPassives) return false;
            if (equippedPassives.Contains(skillId)) return false;

            var skill = GetSkillById(skillId);
            if (skill == null || skill.skillType != SkillType.Passive) return false;
            if (!HasSkill(skillId)) return false;

            equippedPassives.Add(skillId);
            ApplyPassiveEffects(skill);
            OnSkillEquipped?.Invoke(skill);
            return true;
        }

        /// <summary>
        /// Unequips a passive skill.
        /// </summary>
        public bool UnequipPassive(string skillId)
        {
            if (!equippedPassives.Contains(skillId)) return false;

            var skill = GetSkillById(skillId);
            if (skill != null)
            {
                RemovePassiveEffects(skill);
                OnSkillUnequipped?.Invoke(skill);
            }

            equippedPassives.Remove(skillId);
            return true;
        }

        /// <summary>
        /// Inherits a skill from a mastered class.
        /// </summary>
        public bool InheritSkill(string skillId)
        {
            if (inheritedSkills.Count >= maxInheritedSkills) return false;
            if (inheritedSkills.Contains(skillId)) return false;

            var skill = GetSkillById(skillId);
            if (skill == null || !skill.canInherit) return false;

            // Check if the skill is from a mastered class
            foreach (var classData in availableClasses)
            {
                var skillFromClass = classData.classSkills.FirstOrDefault(s => s.skillId == skillId);
                if (skillFromClass != null)
                {
                    if (!IsClassMastered(classData.classId)) return false;
                    break;
                }
            }

            inheritedSkills.Add(skillId);
            UpdateActiveSkills();
            return true;
        }

        /// <summary>
        /// Removes an inherited skill.
        /// </summary>
        public bool RemoveInheritedSkill(string skillId)
        {
            if (!inheritedSkills.Contains(skillId)) return false;
            inheritedSkills.Remove(skillId);
            UpdateActiveSkills();
            return true;
        }

        /// <summary>
        /// Gets a skill by ID from any class.
        /// </summary>
        public ClassSkill GetSkillById(string skillId)
        {
            foreach (var classData in availableClasses)
            {
                var skill = classData.classSkills.FirstOrDefault(s => s.skillId == skillId);
                if (skill != null) return skill;
            }
            return null;
        }

        /// <summary>
        /// Gets the skill tree for a class.
        /// </summary>
        public SkillTree GetSkillTree(string classId)
        {
            skillTrees.TryGetValue(classId, out var tree);
            return tree;
        }

        private void UpdateActiveSkills()
        {
            activeSkills.Clear();

            // Add skills from current class
            if (currentClass != null)
            {
                var progress = GetClassProgress(currentClass.classId);
                if (progress != null)
                {
                    foreach (var skillId in progress.learnedSkills)
                    {
                        var skill = currentClass.classSkills.FirstOrDefault(s => s.skillId == skillId);
                        if (skill?.skillType == SkillType.Active)
                        {
                            activeSkills.Add(skillId);
                        }
                    }
                }
            }

            // Add skills from secondary class
            if (secondaryClass != null && allowMulticlassing)
            {
                var progress = GetClassProgress(secondaryClass.classId);
                if (progress != null)
                {
                    foreach (var skillId in progress.learnedSkills)
                    {
                        var skill = secondaryClass.classSkills.FirstOrDefault(s => s.skillId == skillId);
                        if (skill?.skillType == SkillType.Active && !skill.isClassExclusive)
                        {
                            if (!activeSkills.Contains(skillId))
                            {
                                activeSkills.Add(skillId);
                            }
                        }
                    }
                }
            }

            // Add inherited skills
            foreach (var skillId in inheritedSkills)
            {
                if (!activeSkills.Contains(skillId))
                {
                    activeSkills.Add(skillId);
                }
            }
        }

        #endregion

        #region Stat Bonuses

        private void ApplyClassStatBonuses(CharacterClassData classData)
        {
            if (statsSystem == null || classData == null) return;

            // Remove previous class bonuses (would need to track them)
            // Apply new bonuses as modifiers
            foreach (var bonus in classData.statBonuses)
            {
                float value = bonus.flatBonus + (bonus.bonusPerLevel * GetClassLevel(classData.classId));
                
                if (value != 0)
                {
                    var modifier = new StatModifier(
                        $"class_{classData.classId}_{bonus.attribute}",
                        classData.className,
                        ModifierType.Flat,
                        value,
                        -1f  // Permanent
                    );
                    statsSystem.AddAttributeModifier(bonus.attribute, modifier);
                }

                if (bonus.percentBonus != 0)
                {
                    var percentModifier = new StatModifier(
                        $"class_{classData.classId}_{bonus.attribute}_percent",
                        classData.className,
                        ModifierType.PercentAdd,
                        bonus.percentBonus,
                        -1f
                    );
                    statsSystem.AddAttributeModifier(bonus.attribute, percentModifier);
                }
            }
        }

        private void ApplyPassiveEffects(ClassSkill skill)
        {
            if (statsSystem == null || skill == null) return;

            foreach (var effect in skill.passiveEffects)
            {
                var stat = PassiveEffectToDerivedStat(effect.effectType);
                if (stat != null)
                {
                    var modifier = new StatModifier(
                        $"passive_{skill.skillId}_{effect.effectType}",
                        skill.skillName,
                        effect.isPercentage ? ModifierType.PercentAdd : ModifierType.Flat,
                        effect.value,
                        -1f
                    );
                    statsSystem.AddDerivedStatModifier(stat.Value, modifier);
                }
            }
        }

        private void RemovePassiveEffects(ClassSkill skill)
        {
            if (statsSystem == null || skill == null) return;

            // Would need to track and remove modifiers by skill ID
            // This is simplified - a real implementation would track modifier references
        }

        private DerivedStat? PassiveEffectToDerivedStat(PassiveEffectType effectType)
        {
            return effectType switch
            {
                PassiveEffectType.IncreaseHP => DerivedStat.MaxHealth,
                PassiveEffectType.IncreaseMP => DerivedStat.MaxMana,
                PassiveEffectType.IncreaseAttack => DerivedStat.PhysicalAttack,
                PassiveEffectType.IncreaseDefense => DerivedStat.PhysicalDefense,
                PassiveEffectType.IncreaseMagicAttack => DerivedStat.MagicalAttack,
                PassiveEffectType.IncreaseMagicDefense => DerivedStat.MagicalDefense,
                PassiveEffectType.IncreaseSpeed => DerivedStat.Speed,
                PassiveEffectType.IncreaseCritChance => DerivedStat.CriticalChance,
                PassiveEffectType.IncreaseCritDamage => DerivedStat.CriticalDamage,
                PassiveEffectType.IncreaseEvasion => DerivedStat.Evasion,
                PassiveEffectType.IncreaseAccuracy => DerivedStat.Accuracy,
                PassiveEffectType.ReduceCooldowns => DerivedStat.CooldownReduction,
                PassiveEffectType.ExperienceBonus => DerivedStat.ExperienceBonus,
                PassiveEffectType.GoldBonus => DerivedStat.GoldBonus,
                PassiveEffectType.DropRateBonus => DerivedStat.DropRateBonus,
                _ => null
            };
        }

        #endregion

        #region Utility

        /// <summary>
        /// Gets a formatted summary of the class system state.
        /// </summary>
        public string GetClassSummary()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== Class Summary ===");
            sb.AppendLine($"Character Level: {characterLevel}");
            sb.AppendLine();
            sb.AppendLine($"Current Class: {currentClass?.className ?? "None"}");
            if (currentClass != null)
            {
                var progress = GetClassProgress(currentClass.classId);
                sb.AppendLine($"  Level: {progress?.currentLevel ?? 0} / {currentClass.maxClassLevel}");
                sb.AppendLine($"  EXP: {progress?.currentExp ?? 0} / {progress?.ExpToNextLevel ?? 0}");
                sb.AppendLine($"  Mastered: {progress?.isMastered ?? false}");
                sb.AppendLine($"  Skills Learned: {progress?.learnedSkills.Count ?? 0}");
            }
            if (secondaryClass != null && allowMulticlassing)
            {
                sb.AppendLine();
                sb.AppendLine($"Secondary Class: {secondaryClass.className}");
                var progress = GetClassProgress(secondaryClass.classId);
                sb.AppendLine($"  Level: {progress?.currentLevel ?? 0}");
            }
            sb.AppendLine();
            sb.AppendLine($"Mastered Classes: {GetMasteredClassCount()}");
            sb.AppendLine($"Active Skills: {activeSkills.Count}");
            sb.AppendLine($"Equipped Passives: {equippedPassives.Count}/{maxEquippedPassives}");
            sb.AppendLine($"Inherited Skills: {inheritedSkills.Count}/{maxInheritedSkills}");
            return sb.ToString();
        }

        /// <summary>
        /// Creates save data for the class system.
        /// </summary>
        public ClassSystemSaveData CreateSaveData()
        {
            return new ClassSystemSaveData
            {
                currentClassId = currentClass?.classId,
                secondaryClassId = secondaryClass?.classId,
                characterLevel = characterLevel,
                classProgress = new Dictionary<string, ClassProgressSaveData>(
                    classProgressMap.ToDictionary(
                        kvp => kvp.Key,
                        kvp => new ClassProgressSaveData
                        {
                            currentLevel = kvp.Value.currentLevel,
                            currentExp = kvp.Value.currentExp,
                            totalExpEarned = kvp.Value.totalExpEarned,
                            isMastered = kvp.Value.isMastered,
                            learnedSkills = new List<string>(kvp.Value.learnedSkills),
                            skillPointsEarned = kvp.Value.skillPointsEarned,
                            skillPointsSpent = kvp.Value.skillPointsSpent
                        }
                    )
                ),
                equippedPassives = new List<string>(equippedPassives),
                inheritedSkills = new List<string>(inheritedSkills)
            };
        }

        /// <summary>
        /// Loads class system state from save data.
        /// </summary>
        public void LoadSaveData(ClassSystemSaveData saveData)
        {
            if (saveData == null) return;

            characterLevel = saveData.characterLevel;

            // Restore class progress
            foreach (var kvp in saveData.classProgress)
            {
                if (classProgressMap.TryGetValue(kvp.Key, out var progress))
                {
                    progress.currentLevel = kvp.Value.currentLevel;
                    progress.currentExp = kvp.Value.currentExp;
                    progress.totalExpEarned = kvp.Value.totalExpEarned;
                    progress.isMastered = kvp.Value.isMastered;
                    progress.learnedSkills = new List<string>(kvp.Value.learnedSkills);
                    progress.skillPointsEarned = kvp.Value.skillPointsEarned;
                    progress.skillPointsSpent = kvp.Value.skillPointsSpent;
                }
            }

            // Restore current and secondary class
            if (!string.IsNullOrEmpty(saveData.currentClassId))
            {
                currentClass = availableClasses.FirstOrDefault(c => c.classId == saveData.currentClassId);
            }
            if (!string.IsNullOrEmpty(saveData.secondaryClassId) && allowMulticlassing)
            {
                secondaryClass = availableClasses.FirstOrDefault(c => c.classId == saveData.secondaryClassId);
            }

            // Restore equipped passives and inherited skills
            equippedPassives = new List<string>(saveData.equippedPassives);
            inheritedSkills = new List<string>(saveData.inheritedSkills);

            UpdateActiveSkills();
        }

        /// <summary>
        /// Resets all skill points for respec.
        /// </summary>
        public void RespecClass(string classId)
        {
            if (!classProgressMap.TryGetValue(classId, out var progress)) return;

            // Unequip all passives from this class
            var classData = availableClasses.FirstOrDefault(c => c.classId == classId);
            if (classData != null)
            {
                foreach (var skill in classData.classSkills.Where(s => s.skillType == SkillType.Passive))
                {
                    UnequipPassive(skill.skillId);
                }
            }

            // Reset learned skills
            progress.learnedSkills.Clear();
            progress.skillPointsSpent = 0;

            // Reset skill tree nodes
            if (skillTrees.TryGetValue(classId, out var tree))
            {
                foreach (var node in tree.nodes)
                {
                    node.isLearned = false;
                }
            }

            UpdateActiveSkills();
        }

        #endregion
    }

    /// <summary>
    /// Serializable save data for class system.
    /// </summary>
    [Serializable]
    public class ClassSystemSaveData
    {
        public string currentClassId;
        public string secondaryClassId;
        public int characterLevel;
        public Dictionary<string, ClassProgressSaveData> classProgress;
        public List<string> equippedPassives;
        public List<string> inheritedSkills;
    }

    /// <summary>
    /// Serializable save data for class progress.
    /// </summary>
    [Serializable]
    public class ClassProgressSaveData
    {
        public int currentLevel;
        public int currentExp;
        public int totalExpEarned;
        public bool isMastered;
        public List<string> learnedSkills;
        public int skillPointsEarned;
        public int skillPointsSpent;
    }
}
