using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UsefulScripts.RPG
{
    /// <summary>
    /// Defines the type of summon or monster.
    /// </summary>
    public enum MonsterType
    {
        Beast,
        Dragon,
        Elemental,
        Undead,
        Demon,
        Spirit,
        Construct,
        Plant,
        Slime,
        Humanoid,
        Avian,
        Aquatic,
        Insect,
        Divine,
        Mythical
    }

    /// <summary>
    /// Defines the rarity of a monster.
    /// </summary>
    public enum MonsterRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary,
        Mythic,
        Divine
    }

    /// <summary>
    /// Defines the role of a summon in battle.
    /// </summary>
    public enum SummonRole
    {
        Attacker,       // High damage
        Tank,           // High defense, draws aggro
        Healer,         // Heals allies
        Support,        // Buffs, debuffs
        Balanced,       // All-rounder
        Speedster,      // Fast, multiple attacks
        Nuker           // Powerful single attacks
    }

    /// <summary>
    /// Defines the AI behavior of a summon in battle.
    /// </summary>
    public enum SummonAI
    {
        Aggressive,     // Attack nearest enemy
        Defensive,      // Protect owner
        Supportive,     // Focus on buffs/healing
        Balanced,       // Mix of offense/defense
        Manual,         // Player controlled
        Passive,        // Only attacks when commanded
        Berserker       // Attack randomly, high damage
    }

    /// <summary>
    /// Represents a monster's base stat spread.
    /// </summary>
    [Serializable]
    public class MonsterStats
    {
        public int baseHP = 100;
        public int baseMP = 50;
        public int baseAttack = 20;
        public int baseDefense = 15;
        public int baseMagicAttack = 20;
        public int baseMagicDefense = 15;
        public int baseSpeed = 10;

        [Header("Growth Rates (per level)")]
        public float hpGrowth = 10f;
        public float mpGrowth = 5f;
        public float attackGrowth = 2f;
        public float defenseGrowth = 1.5f;
        public float magicAttackGrowth = 2f;
        public float magicDefenseGrowth = 1.5f;
        public float speedGrowth = 1f;

        public float GetStatAtLevel(string stat, int level)
        {
            return stat switch
            {
                "HP" => baseHP + (hpGrowth * (level - 1)),
                "MP" => baseMP + (mpGrowth * (level - 1)),
                "ATK" => baseAttack + (attackGrowth * (level - 1)),
                "DEF" => baseDefense + (defenseGrowth * (level - 1)),
                "MATK" => baseMagicAttack + (magicAttackGrowth * (level - 1)),
                "MDEF" => baseMagicDefense + (magicDefenseGrowth * (level - 1)),
                "SPD" => baseSpeed + (speedGrowth * (level - 1)),
                _ => 0
            };
        }
    }

    /// <summary>
    /// Represents an evolution path for a monster.
    /// </summary>
    [Serializable]
    public class EvolutionPath
    {
        public string evolutionId;
        public string evolvedMonsterId;
        public string evolvedMonsterName;
        public Sprite evolvedIcon;

        [Header("Requirements")]
        public int requiredLevel = 20;
        public List<string> requiredItems = new List<string>();
        public int requiredItemCount = 1;
        public string requiredLocation;
        public int requiredBondLevel = 0;
        public string requiredTimeOfDay; // "Day", "Night", "Any"
        public string requiredWeather;   // "Sunny", "Rainy", "Any"
        public List<string> requiredSkills = new List<string>();

        [Header("Stat Modifiers")]
        public float hpMultiplier = 1.2f;
        public float attackMultiplier = 1.2f;
        public float defenseMultiplier = 1.2f;
        public float speedMultiplier = 1.1f;
        public List<string> newSkillsLearned = new List<string>();
        public MonsterType newType;
    }

    /// <summary>
    /// Represents a skill that a monster can learn.
    /// </summary>
    [Serializable]
    public class MonsterSkill
    {
        public string skillId;
        public string skillName;
        public string description;
        public Sprite icon;
        public int learnLevel;
        public float mpCost;
        public float cooldown;
        public float power = 1f;
        public DamageType element = DamageType.Physical;
        public SkillTargeting targeting = SkillTargeting.SingleEnemy;
        public List<SkillEffect> effects = new List<SkillEffect>();
        public AudioClip skillSound;
        public GameObject skillVFX;
    }

    /// <summary>
    /// Monster definition as a ScriptableObject.
    /// </summary>
    [CreateAssetMenu(fileName = "NewMonster", menuName = "UsefulScripts/RPG/Monster Data")]
    public class MonsterData : ScriptableObject
    {
        [Header("Basic Info")]
        public string monsterId;
        public string monsterName;
        [TextArea(3, 5)]
        public string description;
        public Sprite icon;
        public GameObject prefab;
        public MonsterType monsterType;
        public MonsterRarity rarity;
        public SummonRole role;

        [Header("Stats")]
        public MonsterStats baseStats = new MonsterStats();
        public int maxLevel = 99;

        [Header("Experience")]
        public int baseExpToLevel = 100;
        public float expScaling = 1.5f;
        public int expYieldOnDefeat = 50;

        [Header("Elements")]
        public DamageType primaryElement = DamageType.Physical;
        public List<DamageType> weaknesses = new List<DamageType>();
        public List<DamageType> resistances = new List<DamageType>();
        public List<DamageType> immunities = new List<DamageType>();
        public List<DamageType> absorptions = new List<DamageType>();

        [Header("Skills")]
        public List<MonsterSkill> learnableSkills = new List<MonsterSkill>();
        public int maxEquippedSkills = 4;

        [Header("Evolution")]
        public bool canEvolve = false;
        public List<EvolutionPath> evolutionPaths = new List<EvolutionPath>();
        public string preEvolutionId;

        [Header("Capture")]
        public float baseCaptureRate = 0.3f;
        public float captureRatePerMissingHP = 0.5f;
        public bool isBoss = false;
        public bool isUncapturable = false;

        [Header("Behavior")]
        public SummonAI defaultAI = SummonAI.Balanced;
        public List<string> favoriteTerrains = new List<string>();
        public List<string> dislikedTerrains = new List<string>();

        [Header("Bond")]
        public int maxBondLevel = 100;
        public float bondGainOnBattle = 1f;
        public float bondGainOnFeeding = 5f;
        public float bondGainOnPetting = 3f;

        /// <summary>
        /// Gets the experience required to reach a specific level.
        /// </summary>
        public int GetExpForLevel(int level)
        {
            return (int)(baseExpToLevel * Mathf.Pow(level, expScaling));
        }

        /// <summary>
        /// Gets skills available at a specific level.
        /// </summary>
        public List<MonsterSkill> GetSkillsAtLevel(int level)
        {
            return learnableSkills.Where(s => s.learnLevel <= level).ToList();
        }

        /// <summary>
        /// Gets the elemental multiplier for an incoming attack.
        /// </summary>
        public float GetElementalMultiplier(DamageType attackElement)
        {
            if (absorptions.Contains(attackElement)) return -1f; // Heal
            if (immunities.Contains(attackElement)) return 0f;
            if (resistances.Contains(attackElement)) return 0.5f;
            if (weaknesses.Contains(attackElement)) return 2f;
            return 1f;
        }
    }

    /// <summary>
    /// Represents a tamed/summoned monster instance.
    /// </summary>
    [Serializable]
    public class SummonedMonster
    {
        [Header("Identity")]
        public string instanceId;
        public string nickname;
        public MonsterData monsterData;
        public GameObject activeInstance;

        [Header("Stats")]
        public int currentLevel = 1;
        public int currentExp = 0;
        public int totalExpEarned = 0;
        public float currentHP;
        public float currentMP;

        [Header("Bond")]
        public int bondLevel = 0;
        public int bondPoints = 0;
        public int bondToNextLevel = 100;
        public DateTime capturedAt;
        public DateTime lastInteraction;
        public int battlesFought = 0;
        public int battlesWon = 0;

        [Header("Skills")]
        public List<string> learnedSkillIds = new List<string>();
        public List<string> equippedSkillIds = new List<string>();

        [Header("State")]
        public bool isActive = false;
        public bool isInParty = false;
        public bool isFainted = false;
        public SummonAI currentAI;

        [Header("Modifiers")]
        public float hpModifier = 1f;
        public float attackModifier = 1f;
        public float defenseModifier = 1f;
        public float speedModifier = 1f;

        // Events
        public event Action<int> OnLevelUp;
        public event Action<int> OnBondLevelUp;
        public event Action<string> OnSkillLearned;
        public event Action<EvolutionPath> OnEvolutionReady;
        public event Action OnFainted;
        public event Action OnRevived;

        // Properties
        public bool IsMaxLevel => currentLevel >= (monsterData?.maxLevel ?? 99);
        public int ExpToNextLevel => monsterData?.GetExpForLevel(currentLevel + 1) ?? int.MaxValue;
        public float ExpProgress => (float)currentExp / ExpToNextLevel;
        public float BondProgress => (float)bondPoints / bondToNextLevel;
        public float HPPercent => currentHP / GetMaxHP();
        public float MPPercent => currentMP / GetMaxMP();

        public SummonedMonster(MonsterData data, string name = null)
        {
            instanceId = Guid.NewGuid().ToString();
            monsterData = data;
            nickname = name ?? data.monsterName;
            currentAI = data.defaultAI;
            capturedAt = DateTime.Now;
            lastInteraction = DateTime.Now;
            
            // Initialize HP/MP
            currentHP = GetMaxHP();
            currentMP = GetMaxMP();

            // Learn starting skills
            foreach (var skill in data.GetSkillsAtLevel(1))
            {
                LearnSkill(skill.skillId);
            }
        }

        #region Stats

        public float GetMaxHP()
        {
            return monsterData.baseStats.GetStatAtLevel("HP", currentLevel) * hpModifier;
        }

        public float GetMaxMP()
        {
            return monsterData.baseStats.GetStatAtLevel("MP", currentLevel);
        }

        public float GetAttack()
        {
            return monsterData.baseStats.GetStatAtLevel("ATK", currentLevel) * attackModifier;
        }

        public float GetDefense()
        {
            return monsterData.baseStats.GetStatAtLevel("DEF", currentLevel) * defenseModifier;
        }

        public float GetMagicAttack()
        {
            return monsterData.baseStats.GetStatAtLevel("MATK", currentLevel);
        }

        public float GetMagicDefense()
        {
            return monsterData.baseStats.GetStatAtLevel("MDEF", currentLevel);
        }

        public float GetSpeed()
        {
            return monsterData.baseStats.GetStatAtLevel("SPD", currentLevel) * speedModifier;
        }

        #endregion

        #region Experience & Leveling

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
                levelsGained++;
                OnLevelUp?.Invoke(currentLevel);

                // Check for new skills
                CheckNewSkills();

                // Check evolution readiness
                CheckEvolutionReady();
            }

            return levelsGained;
        }

        private void CheckNewSkills()
        {
            foreach (var skill in monsterData.learnableSkills)
            {
                if (skill.learnLevel == currentLevel && !learnedSkillIds.Contains(skill.skillId))
                {
                    LearnSkill(skill.skillId);
                }
            }
        }

        private void CheckEvolutionReady()
        {
            if (!monsterData.canEvolve) return;

            foreach (var evolution in monsterData.evolutionPaths)
            {
                if (currentLevel >= evolution.requiredLevel)
                {
                    OnEvolutionReady?.Invoke(evolution);
                }
            }
        }

        #endregion

        #region Skills

        public bool LearnSkill(string skillId)
        {
            if (learnedSkillIds.Contains(skillId)) return false;

            learnedSkillIds.Add(skillId);

            // Auto-equip if there's room
            if (equippedSkillIds.Count < (monsterData?.maxEquippedSkills ?? 4))
            {
                equippedSkillIds.Add(skillId);
            }

            OnSkillLearned?.Invoke(skillId);
            return true;
        }

        public bool EquipSkill(string skillId)
        {
            if (!learnedSkillIds.Contains(skillId)) return false;
            if (equippedSkillIds.Contains(skillId)) return false;
            if (equippedSkillIds.Count >= (monsterData?.maxEquippedSkills ?? 4)) return false;

            equippedSkillIds.Add(skillId);
            return true;
        }

        public bool UnequipSkill(string skillId)
        {
            return equippedSkillIds.Remove(skillId);
        }

        public MonsterSkill GetSkill(string skillId)
        {
            return monsterData?.learnableSkills.FirstOrDefault(s => s.skillId == skillId);
        }

        #endregion

        #region Bond

        public int AddBondPoints(int amount)
        {
            bondPoints += amount;
            int levelsGained = 0;

            while (bondPoints >= bondToNextLevel && bondLevel < (monsterData?.maxBondLevel ?? 100))
            {
                bondPoints -= bondToNextLevel;
                bondLevel++;
                levelsGained++;
                bondToNextLevel = CalculateBondToNextLevel();
                OnBondLevelUp?.Invoke(bondLevel);
            }

            lastInteraction = DateTime.Now;
            return levelsGained;
        }

        private int CalculateBondToNextLevel()
        {
            return 100 + (bondLevel * 10);
        }

        public float GetBondMultiplier()
        {
            // Higher bond = better stats
            return 1f + (bondLevel * 0.005f); // +0.5% per bond level
        }

        #endregion

        #region Health & Combat

        public void TakeDamage(float amount, DamageType damageType)
        {
            float multiplier = monsterData.GetElementalMultiplier(damageType);
            
            if (multiplier < 0)
            {
                // Absorption - heal instead
                Heal(-amount * multiplier);
                return;
            }

            float finalDamage = amount * multiplier;
            currentHP = Mathf.Max(0, currentHP - finalDamage);

            if (currentHP <= 0)
            {
                Faint();
            }
        }

        public void Heal(float amount)
        {
            currentHP = Mathf.Min(GetMaxHP(), currentHP + amount);
        }

        public void RestoreMP(float amount)
        {
            currentMP = Mathf.Min(GetMaxMP(), currentMP + amount);
        }

        public bool UseMP(float amount)
        {
            if (currentMP < amount) return false;
            currentMP -= amount;
            return true;
        }

        public void Faint()
        {
            isFainted = true;
            isActive = false;
            OnFainted?.Invoke();
        }

        public void Revive(float hpPercent = 0.5f)
        {
            isFainted = false;
            currentHP = GetMaxHP() * hpPercent;
            OnRevived?.Invoke();
        }

        public void FullRestore()
        {
            isFainted = false;
            currentHP = GetMaxHP();
            currentMP = GetMaxMP();
        }

        #endregion
    }

    /// <summary>
    /// Configuration for the summon/monster system.
    /// </summary>
    [CreateAssetMenu(fileName = "SummonSystemConfig", menuName = "UsefulScripts/RPG/Summon System Config")]
    public class SummonSystemConfig : ScriptableObject
    {
        [Header("Party Settings")]
        public int maxPartySize = 6;
        public int maxStorageSize = 100;
        public int maxActiveSummons = 2;

        [Header("Capture Settings")]
        public float captureHPThreshold = 0.3f;
        public float statusEffectCaptureBonus = 0.2f;
        public float lowLevelCaptureBonus = 0.1f;

        [Header("Experience Settings")]
        public float expSharePercent = 0.5f;
        public bool reserveGetsExp = true;
        public float reserveExpPercent = 0.25f;

        [Header("Bond Settings")]
        public float bondDecayPerDay = 1f;
        public bool bondDecayEnabled = true;

        [Header("Available Monsters")]
        public List<MonsterData> allMonsters = new List<MonsterData>();
    }

    /// <summary>
    /// Complete summon/monster management system.
    /// Handles taming, evolution, party management, and monster battles.
    /// </summary>
    public class SummonSystem : MonoBehaviour
    {
        public static SummonSystem Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private SummonSystemConfig config;

        [Header("Current State")]
        [SerializeField] private int captureItemCount = 10;

        // Runtime data
        private List<SummonedMonster> monsterParty = new List<SummonedMonster>();
        private List<SummonedMonster> monsterStorage = new List<SummonedMonster>();
        private List<SummonedMonster> activeSummons = new List<SummonedMonster>();
        private Dictionary<string, int> capturedSpecies = new Dictionary<string, int>();
        private System.Random random = new System.Random();

        // Events
        public event Action<SummonedMonster> OnMonsterCaptured;
        public event Action<SummonedMonster> OnMonsterReleased;
        public event Action<SummonedMonster, int> OnMonsterLevelUp;
        public event Action<SummonedMonster, EvolutionPath> OnMonsterEvolved;
        public event Action<SummonedMonster> OnMonsterAddedToParty;
        public event Action<SummonedMonster> OnMonsterRemovedFromParty;
        public event Action<SummonedMonster> OnMonsterSummoned;
        public event Action<SummonedMonster> OnMonsterRecalled;
        public event Action<SummonedMonster, int> OnBondLevelUp;
        public event Action<bool> OnCaptureAttempt;

        // Properties
        public int PartyCount => monsterParty.Count;
        public int StorageCount => monsterStorage.Count;
        public int MaxPartySize => config?.maxPartySize ?? 6;
        public int MaxStorageSize => config?.maxStorageSize ?? 100;
        public int ActiveSummonCount => activeSummons.Count;
        public int MaxActiveSummons => config?.maxActiveSummons ?? 2;
        public int CaptureItemCount => captureItemCount;
        public IReadOnlyList<SummonedMonster> Party => monsterParty.AsReadOnly();
        public IReadOnlyList<SummonedMonster> Storage => monsterStorage.AsReadOnly();
        public IReadOnlyList<SummonedMonster> ActiveSummons => activeSummons.AsReadOnly();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            UpdateActiveSummons(Time.deltaTime);
        }

        private void UpdateActiveSummons(float deltaTime)
        {
            // Update AI behavior for active summons
            foreach (var summon in activeSummons)
            {
                if (summon.activeInstance == null) continue;
                
                // AI logic would go here
            }
        }

        #region Capture

        /// <summary>
        /// Attempts to capture a wild monster.
        /// </summary>
        public bool AttemptCapture(MonsterData monsterData, float currentHPPercent, bool hasStatusEffect = false, int playerLevel = 1)
        {
            if (monsterData == null) return false;
            if (monsterData.isUncapturable)
            {
                Debug.Log($"{monsterData.monsterName} cannot be captured!");
                return false;
            }

            if (captureItemCount <= 0)
            {
                Debug.Log("No capture items remaining!");
                return false;
            }

            if (monsterParty.Count >= MaxPartySize && monsterStorage.Count >= MaxStorageSize)
            {
                Debug.Log("Party and storage are full!");
                return false;
            }

            captureItemCount--;

            // Calculate capture chance
            float captureChance = monsterData.baseCaptureRate;

            // Lower HP = higher chance
            float missingHP = 1f - currentHPPercent;
            captureChance += missingHP * monsterData.captureRatePerMissingHP;

            // Status effect bonus
            if (hasStatusEffect)
            {
                captureChance += config?.statusEffectCaptureBonus ?? 0.2f;
            }

            // Level difference bonus (capturing lower level monsters is easier)
            int levelDiff = playerLevel - 1; // Assuming level 1 monster for simplicity
            if (levelDiff > 0)
            {
                captureChance += levelDiff * (config?.lowLevelCaptureBonus ?? 0.1f);
            }

            // Boss monsters are harder
            if (monsterData.isBoss)
            {
                captureChance *= 0.1f;
            }

            // Rarity affects capture rate
            float rarityModifier = monsterData.rarity switch
            {
                MonsterRarity.Common => 1f,
                MonsterRarity.Uncommon => 0.8f,
                MonsterRarity.Rare => 0.5f,
                MonsterRarity.Epic => 0.3f,
                MonsterRarity.Legendary => 0.1f,
                MonsterRarity.Mythic => 0.05f,
                MonsterRarity.Divine => 0.01f,
                _ => 1f
            };
            captureChance *= rarityModifier;

            // Roll for capture
            bool success = random.NextDouble() < captureChance;
            OnCaptureAttempt?.Invoke(success);

            if (success)
            {
                var capturedMonster = new SummonedMonster(monsterData);
                AddMonster(capturedMonster);
                
                // Track species capture count
                if (!capturedSpecies.ContainsKey(monsterData.monsterId))
                {
                    capturedSpecies[monsterData.monsterId] = 0;
                }
                capturedSpecies[monsterData.monsterId]++;

                OnMonsterCaptured?.Invoke(capturedMonster);
                Debug.Log($"Captured {monsterData.monsterName}!");
            }
            else
            {
                Debug.Log($"{monsterData.monsterName} broke free!");
            }

            return success;
        }

        /// <summary>
        /// Adds capture items.
        /// </summary>
        public void AddCaptureItems(int count)
        {
            captureItemCount += count;
        }

        #endregion

        #region Party Management

        /// <summary>
        /// Adds a monster to the party or storage.
        /// </summary>
        public bool AddMonster(SummonedMonster monster)
        {
            if (monster == null) return false;

            if (monsterParty.Count < MaxPartySize)
            {
                monsterParty.Add(monster);
                monster.isInParty = true;
                OnMonsterAddedToParty?.Invoke(monster);
            }
            else if (monsterStorage.Count < MaxStorageSize)
            {
                monsterStorage.Add(monster);
                monster.isInParty = false;
            }
            else
            {
                return false;
            }

            // Subscribe to events
            monster.OnLevelUp += level => OnMonsterLevelUp?.Invoke(monster, level);
            monster.OnBondLevelUp += level => OnBondLevelUp?.Invoke(monster, level);

            return true;
        }

        /// <summary>
        /// Removes a monster from party or storage.
        /// </summary>
        public bool RemoveMonster(string instanceId)
        {
            var monster = GetMonster(instanceId);
            if (monster == null) return false;

            // Recall if active
            if (monster.isActive)
            {
                RecallSummon(instanceId);
            }

            monsterParty.Remove(monster);
            monsterStorage.Remove(monster);
            monster.isInParty = false;

            return true;
        }

        /// <summary>
        /// Releases a monster back to the wild.
        /// </summary>
        public bool ReleaseMonster(string instanceId)
        {
            var monster = GetMonster(instanceId);
            if (monster == null) return false;

            // Can't release the last party member
            if (monster.isInParty && monsterParty.Count <= 1)
            {
                Debug.Log("Cannot release last party member!");
                return false;
            }

            RemoveMonster(instanceId);
            OnMonsterReleased?.Invoke(monster);
            return true;
        }

        /// <summary>
        /// Moves a monster from storage to party.
        /// </summary>
        public bool MoveToParty(string instanceId)
        {
            var monster = monsterStorage.FirstOrDefault(m => m.instanceId == instanceId);
            if (monster == null) return false;
            if (monsterParty.Count >= MaxPartySize) return false;

            monsterStorage.Remove(monster);
            monsterParty.Add(monster);
            monster.isInParty = true;

            OnMonsterAddedToParty?.Invoke(monster);
            return true;
        }

        /// <summary>
        /// Moves a monster from party to storage.
        /// </summary>
        public bool MoveToStorage(string instanceId)
        {
            var monster = monsterParty.FirstOrDefault(m => m.instanceId == instanceId);
            if (monster == null) return false;
            if (monsterParty.Count <= 1) return false; // Keep at least one
            if (monsterStorage.Count >= MaxStorageSize) return false;

            if (monster.isActive)
            {
                RecallSummon(instanceId);
            }

            monsterParty.Remove(monster);
            monsterStorage.Add(monster);
            monster.isInParty = false;

            OnMonsterRemovedFromParty?.Invoke(monster);
            return true;
        }

        /// <summary>
        /// Swaps two monsters between party and storage.
        /// </summary>
        public bool SwapMonsters(string partyMonsterId, string storageMonsterId)
        {
            var partyMonster = monsterParty.FirstOrDefault(m => m.instanceId == partyMonsterId);
            var storageMonster = monsterStorage.FirstOrDefault(m => m.instanceId == storageMonsterId);

            if (partyMonster == null || storageMonster == null) return false;

            if (partyMonster.isActive)
            {
                RecallSummon(partyMonsterId);
            }

            int partyIndex = monsterParty.IndexOf(partyMonster);
            int storageIndex = monsterStorage.IndexOf(storageMonster);

            monsterParty[partyIndex] = storageMonster;
            monsterStorage[storageIndex] = partyMonster;

            storageMonster.isInParty = true;
            partyMonster.isInParty = false;

            return true;
        }

        /// <summary>
        /// Reorders the party.
        /// </summary>
        public void ReorderParty(int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || fromIndex >= monsterParty.Count) return;
            if (toIndex < 0 || toIndex >= monsterParty.Count) return;

            var monster = monsterParty[fromIndex];
            monsterParty.RemoveAt(fromIndex);
            monsterParty.Insert(toIndex, monster);
        }

        /// <summary>
        /// Gets a monster by instance ID.
        /// </summary>
        public SummonedMonster GetMonster(string instanceId)
        {
            return monsterParty.FirstOrDefault(m => m.instanceId == instanceId) ??
                   monsterStorage.FirstOrDefault(m => m.instanceId == instanceId);
        }

        /// <summary>
        /// Gets all monsters of a specific species.
        /// </summary>
        public List<SummonedMonster> GetMonstersBySpecies(string monsterId)
        {
            return monsterParty.Concat(monsterStorage)
                              .Where(m => m.monsterData.monsterId == monsterId)
                              .ToList();
        }

        #endregion

        #region Summoning

        /// <summary>
        /// Summons a monster into the field.
        /// </summary>
        public bool SummonMonster(string instanceId, Vector3 position)
        {
            if (activeSummons.Count >= MaxActiveSummons)
            {
                Debug.Log("Maximum active summons reached!");
                return false;
            }

            var monster = monsterParty.FirstOrDefault(m => m.instanceId == instanceId);
            if (monster == null)
            {
                Debug.Log("Monster not in party!");
                return false;
            }

            if (monster.isFainted)
            {
                Debug.Log("Cannot summon fainted monster!");
                return false;
            }

            if (monster.isActive)
            {
                Debug.Log("Monster already summoned!");
                return false;
            }

            // Instantiate the monster prefab
            if (monster.monsterData.prefab != null)
            {
                monster.activeInstance = Instantiate(monster.monsterData.prefab, position, Quaternion.identity);
            }

            monster.isActive = true;
            activeSummons.Add(monster);

            OnMonsterSummoned?.Invoke(monster);
            return true;
        }

        /// <summary>
        /// Recalls a summoned monster.
        /// </summary>
        public bool RecallSummon(string instanceId)
        {
            var monster = activeSummons.FirstOrDefault(m => m.instanceId == instanceId);
            if (monster == null) return false;

            if (monster.activeInstance != null)
            {
                Destroy(monster.activeInstance);
                monster.activeInstance = null;
            }

            monster.isActive = false;
            activeSummons.Remove(monster);

            OnMonsterRecalled?.Invoke(monster);
            return true;
        }

        /// <summary>
        /// Recalls all summoned monsters.
        /// </summary>
        public void RecallAllSummons()
        {
            foreach (var monster in activeSummons.ToList())
            {
                RecallSummon(monster.instanceId);
            }
        }

        /// <summary>
        /// Swaps an active summon with another party member.
        /// </summary>
        public bool SwapSummon(string activeId, string replacementId)
        {
            var active = activeSummons.FirstOrDefault(m => m.instanceId == activeId);
            var replacement = monsterParty.FirstOrDefault(m => m.instanceId == replacementId);

            if (active == null || replacement == null) return false;
            if (replacement.isFainted) return false;

            Vector3 position = active.activeInstance?.transform.position ?? Vector3.zero;
            
            RecallSummon(activeId);
            SummonMonster(replacementId, position);

            return true;
        }

        #endregion

        #region Evolution

        /// <summary>
        /// Checks if a monster can evolve.
        /// </summary>
        public bool CanEvolve(string instanceId, out EvolutionPath evolution)
        {
            evolution = null;
            var monster = GetMonster(instanceId);
            if (monster == null || !monster.monsterData.canEvolve) return false;

            foreach (var path in monster.monsterData.evolutionPaths)
            {
                if (CheckEvolutionRequirements(monster, path))
                {
                    evolution = path;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Evolves a monster.
        /// </summary>
        public bool EvolveMonster(string instanceId, string evolutionId)
        {
            var monster = GetMonster(instanceId);
            if (monster == null) return false;

            var evolution = monster.monsterData.evolutionPaths.FirstOrDefault(e => e.evolutionId == evolutionId);
            if (evolution == null) return false;

            if (!CheckEvolutionRequirements(monster, evolution))
            {
                Debug.Log("Evolution requirements not met!");
                return false;
            }

            // Consume required items (would integrate with inventory)
            // ConsumeEvolutionItems(evolution);

            // Get new monster data
            var evolvedData = config?.allMonsters.FirstOrDefault(m => m.monsterId == evolution.evolvedMonsterId);
            if (evolvedData == null)
            {
                Debug.LogError($"Evolved monster data not found: {evolution.evolvedMonsterId}");
                return false;
            }

            // Apply evolution
            string previousName = monster.monsterData.monsterName;
            monster.monsterData = evolvedData;
            
            // Apply stat modifiers
            monster.hpModifier *= evolution.hpMultiplier;
            monster.attackModifier *= evolution.attackMultiplier;
            monster.defenseModifier *= evolution.defenseMultiplier;
            monster.speedModifier *= evolution.speedMultiplier;

            // Learn new skills
            foreach (var skillId in evolution.newSkillsLearned)
            {
                monster.LearnSkill(skillId);
            }

            // Update HP/MP to new maximums
            monster.currentHP = monster.GetMaxHP();
            monster.currentMP = monster.GetMaxMP();

            OnMonsterEvolved?.Invoke(monster, evolution);
            Debug.Log($"{previousName} evolved into {evolvedData.monsterName}!");

            return true;
        }

        private bool CheckEvolutionRequirements(SummonedMonster monster, EvolutionPath evolution)
        {
            if (monster.currentLevel < evolution.requiredLevel) return false;
            if (monster.bondLevel < evolution.requiredBondLevel) return false;
            
            // Check required skills
            foreach (var skillId in evolution.requiredSkills)
            {
                if (!monster.learnedSkillIds.Contains(skillId)) return false;
            }

            // Check items (would integrate with inventory)
            // if (!HasEvolutionItems(evolution)) return false;

            // Check time/weather (would integrate with time system)
            // if (!CheckTimeRequirement(evolution)) return false;

            return true;
        }

        #endregion

        #region Bond & Interaction

        /// <summary>
        /// Increases bond with a monster through interaction.
        /// </summary>
        public void Interact(string instanceId, string interactionType)
        {
            var monster = GetMonster(instanceId);
            if (monster == null) return;

            int bondGain = interactionType switch
            {
                "pet" => (int)monster.monsterData.bondGainOnPetting,
                "feed" => (int)monster.monsterData.bondGainOnFeeding,
                "battle" => (int)monster.monsterData.bondGainOnBattle,
                _ => 1
            };

            monster.AddBondPoints(bondGain);
        }

        /// <summary>
        /// Heals all party monsters.
        /// </summary>
        public void HealAllParty()
        {
            foreach (var monster in monsterParty)
            {
                monster.FullRestore();
            }
        }

        /// <summary>
        /// Renames a monster.
        /// </summary>
        public void RenameMonster(string instanceId, string newName)
        {
            var monster = GetMonster(instanceId);
            if (monster != null)
            {
                monster.nickname = newName;
            }
        }

        #endregion

        #region Experience Distribution

        /// <summary>
        /// Distributes experience to party monsters.
        /// </summary>
        public void DistributeExperience(int totalExp, List<string> participantIds)
        {
            // Active participants get full share
            int participantCount = participantIds.Count;
            int expPerParticipant = participantCount > 0 ? totalExp / participantCount : 0;

            foreach (var id in participantIds)
            {
                var monster = GetMonster(id);
                monster?.AddExperience(expPerParticipant);
            }

            // Reserve party members get partial exp if enabled
            if (config?.reserveGetsExp ?? true)
            {
                int reserveExp = (int)(expPerParticipant * (config?.reserveExpPercent ?? 0.25f));
                foreach (var monster in monsterParty.Where(m => !participantIds.Contains(m.instanceId)))
                {
                    monster.AddExperience(reserveExp);
                }
            }
        }

        #endregion

        #region Utility

        /// <summary>
        /// Gets statistics about captured monsters.
        /// </summary>
        public Dictionary<string, int> GetCaptureStatistics()
        {
            return new Dictionary<string, int>(capturedSpecies);
        }

        /// <summary>
        /// Gets the total number of unique species captured.
        /// </summary>
        public int GetUniqueSpeciesCount()
        {
            return capturedSpecies.Count;
        }

        /// <summary>
        /// Gets a summary of the summon system state.
        /// </summary>
        public string GetSummonSummary()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== Summon System Summary ===");
            sb.AppendLine($"Party: {monsterParty.Count}/{MaxPartySize}");
            sb.AppendLine($"Storage: {monsterStorage.Count}/{MaxStorageSize}");
            sb.AppendLine($"Active Summons: {activeSummons.Count}/{MaxActiveSummons}");
            sb.AppendLine($"Capture Items: {captureItemCount}");
            sb.AppendLine($"Unique Species: {capturedSpecies.Count}");
            sb.AppendLine();
            sb.AppendLine("--- Party ---");
            foreach (var monster in monsterParty)
            {
                string status = monster.isFainted ? "[KO]" : monster.isActive ? "[ACTIVE]" : "";
                sb.AppendLine($"  {status} {monster.nickname} ({monster.monsterData.monsterName}) Lv.{monster.currentLevel}");
            }
            return sb.ToString();
        }

        /// <summary>
        /// Creates save data for the summon system.
        /// </summary>
        public SummonSystemSaveData CreateSaveData()
        {
            return new SummonSystemSaveData
            {
                captureItemCount = captureItemCount,
                capturedSpecies = new Dictionary<string, int>(capturedSpecies),
                partyMonsters = monsterParty.Select(CreateMonsterSaveData).ToList(),
                storageMonsters = monsterStorage.Select(CreateMonsterSaveData).ToList()
            };
        }

        private MonsterSaveData CreateMonsterSaveData(SummonedMonster monster)
        {
            return new MonsterSaveData
            {
                instanceId = monster.instanceId,
                monsterId = monster.monsterData.monsterId,
                nickname = monster.nickname,
                currentLevel = monster.currentLevel,
                currentExp = monster.currentExp,
                bondLevel = monster.bondLevel,
                bondPoints = monster.bondPoints,
                currentHP = monster.currentHP,
                currentMP = monster.currentMP,
                learnedSkillIds = new List<string>(monster.learnedSkillIds),
                equippedSkillIds = new List<string>(monster.equippedSkillIds),
                isFainted = monster.isFainted,
                currentAI = monster.currentAI,
                hpModifier = monster.hpModifier,
                attackModifier = monster.attackModifier,
                defenseModifier = monster.defenseModifier,
                speedModifier = monster.speedModifier,
                battlesFought = monster.battlesFought,
                battlesWon = monster.battlesWon
            };
        }

        /// <summary>
        /// Loads summon system state from save data.
        /// </summary>
        public void LoadSaveData(SummonSystemSaveData saveData)
        {
            if (saveData == null) return;

            captureItemCount = saveData.captureItemCount;
            capturedSpecies = new Dictionary<string, int>(saveData.capturedSpecies);

            monsterParty.Clear();
            monsterStorage.Clear();

            foreach (var data in saveData.partyMonsters)
            {
                var monster = LoadMonsterFromSaveData(data);
                if (monster != null)
                {
                    monsterParty.Add(monster);
                    monster.isInParty = true;
                }
            }

            foreach (var data in saveData.storageMonsters)
            {
                var monster = LoadMonsterFromSaveData(data);
                if (monster != null)
                {
                    monsterStorage.Add(monster);
                }
            }
        }

        private SummonedMonster LoadMonsterFromSaveData(MonsterSaveData data)
        {
            var monsterData = config?.allMonsters.FirstOrDefault(m => m.monsterId == data.monsterId);
            if (monsterData == null) return null;

            var monster = new SummonedMonster(monsterData, data.nickname)
            {
                instanceId = data.instanceId,
                currentLevel = data.currentLevel,
                currentExp = data.currentExp,
                bondLevel = data.bondLevel,
                bondPoints = data.bondPoints,
                currentHP = data.currentHP,
                currentMP = data.currentMP,
                learnedSkillIds = new List<string>(data.learnedSkillIds),
                equippedSkillIds = new List<string>(data.equippedSkillIds),
                isFainted = data.isFainted,
                currentAI = data.currentAI,
                hpModifier = data.hpModifier,
                attackModifier = data.attackModifier,
                defenseModifier = data.defenseModifier,
                speedModifier = data.speedModifier,
                battlesFought = data.battlesFought,
                battlesWon = data.battlesWon
            };

            return monster;
        }

        #endregion
    }

    /// <summary>
    /// Serializable save data for summon system.
    /// </summary>
    [Serializable]
    public class SummonSystemSaveData
    {
        public int captureItemCount;
        public Dictionary<string, int> capturedSpecies;
        public List<MonsterSaveData> partyMonsters;
        public List<MonsterSaveData> storageMonsters;
    }

    /// <summary>
    /// Serializable save data for a monster.
    /// </summary>
    [Serializable]
    public class MonsterSaveData
    {
        public string instanceId;
        public string monsterId;
        public string nickname;
        public int currentLevel;
        public int currentExp;
        public int bondLevel;
        public int bondPoints;
        public float currentHP;
        public float currentMP;
        public List<string> learnedSkillIds;
        public List<string> equippedSkillIds;
        public bool isFainted;
        public SummonAI currentAI;
        public float hpModifier;
        public float attackModifier;
        public float defenseModifier;
        public float speedModifier;
        public int battlesFought;
        public int battlesWon;
    }
}
