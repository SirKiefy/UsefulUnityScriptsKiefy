using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UsefulScripts.Player
{
    #region Enums

    /// <summary>
    /// Defines the type of mount/tame.
    /// </summary>
    public enum CreatureType
    {
        Beast,
        Bird,
        Reptile,
        Aquatic,
        Dragon,
        Elemental,
        Spirit,
        Construct,
        Mythical,
        Insect,
        Plant
    }

    /// <summary>
    /// Defines the size category of a mount.
    /// </summary>
    public enum MountSize
    {
        Small,      // Cannot be ridden, companion only
        Medium,     // Can carry one rider
        Large,      // Can carry multiple riders
        Huge,       // Special mounts, flying etc.
        Colossal    // Epic mounts
    }

    /// <summary>
    /// Defines the movement capabilities of a mount.
    /// </summary>
    [Flags]
    public enum MovementCapability
    {
        None = 0,
        Ground = 1,
        Water = 2,
        Flying = 4,
        Climbing = 8,
        Burrowing = 16,
        Teleporting = 32,
        Phasing = 64
    }

    /// <summary>
    /// Defines the rarity of a mount/tame.
    /// </summary>
    public enum CreatureRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary,
        Mythic,
        Unique
    }

    /// <summary>
    /// Defines the behavior mode for tamed creatures.
    /// </summary>
    public enum TameBehavior
    {
        Follow,         // Follows owner
        Stay,           // Stays in place
        Guard,          // Guards an area
        Patrol,         // Patrols between points
        Aggressive,     // Attacks anything nearby
        Defensive,      // Only attacks if owner is attacked
        Passive         // Never attacks
    }

    /// <summary>
    /// Defines mount/tame mood states.
    /// </summary>
    public enum CreatureMood
    {
        Ecstatic,
        Happy,
        Content,
        Neutral,
        Unhappy,
        Angry,
        Hostile
    }

    #endregion

    #region Data Classes

    /// <summary>
    /// Represents a creature's base stats.
    /// </summary>
    [Serializable]
    public class CreatureStats
    {
        public float maxHealth = 100f;
        public float currentHealth = 100f;
        public float stamina = 100f;
        public float maxStamina = 100f;
        public float attack = 20f;
        public float defense = 10f;
        public float speed = 10f;

        [Header("Growth Rates")]
        public float healthGrowth = 10f;
        public float staminaGrowth = 5f;
        public float attackGrowth = 2f;
        public float defenseGrowth = 1.5f;
        public float speedGrowth = 0.5f;

        public float GetStatAtLevel(int level, string stat)
        {
            return stat switch
            {
                "Health" => maxHealth + (healthGrowth * (level - 1)),
                "Stamina" => maxStamina + (staminaGrowth * (level - 1)),
                "Attack" => attack + (attackGrowth * (level - 1)),
                "Defense" => defense + (defenseGrowth * (level - 1)),
                "Speed" => speed + (speedGrowth * (level - 1)),
                _ => 0f
            };
        }

        public CreatureStats Clone()
        {
            return new CreatureStats
            {
                maxHealth = maxHealth,
                currentHealth = currentHealth,
                stamina = stamina,
                maxStamina = maxStamina,
                attack = attack,
                defense = defense,
                speed = speed,
                healthGrowth = healthGrowth,
                staminaGrowth = staminaGrowth,
                attackGrowth = attackGrowth,
                defenseGrowth = defenseGrowth,
                speedGrowth = speedGrowth
            };
        }
    }

    /// <summary>
    /// Represents mount-specific properties.
    /// </summary>
    [Serializable]
    public class MountProperties
    {
        public MountSize size = MountSize.Medium;
        public MovementCapability movementCapabilities = MovementCapability.Ground;
        public float groundSpeed = 10f;
        public float waterSpeed = 8f;
        public float flyingSpeed = 15f;
        public float sprintMultiplier = 1.5f;
        public float jumpPower = 8f;
        public int maxPassengers = 1;
        public float staminaCostPerSecond = 5f;
        public float mountingTime = 1f;
        public float dismountingTime = 0.5f;
    }

    /// <summary>
    /// Represents a creature's ability.
    /// </summary>
    [Serializable]
    public class CreatureAbility
    {
        public string abilityId;
        public string abilityName;
        public string description;
        public Sprite icon;
        public float cooldown;
        public float staminaCost;
        public float manaCost;
        public int unlockLevel;
        public bool isPassive;

        public event Action OnAbilityUsed;

        public void UseAbility()
        {
            OnAbilityUsed?.Invoke();
        }
    }

    /// <summary>
    /// Represents taming requirements.
    /// </summary>
    [Serializable]
    public class TamingRequirements
    {
        public int requiredPlayerLevel = 1;
        public int requiredTamingSkill = 0;
        public List<string> requiredItems;
        public int requiredItemCount = 1;
        public float baseTameChance = 50f;
        public float tameChancePerSkill = 2f;
        public float tameTime = 10f;
        public float healthThresholdToTame = 30f; // Creature must be below this HP% to tame
        public bool requiresCombat = false;
        public string requiredLocation;
        public string requiredTimeOfDay;
    }

    /// <summary>
    /// Represents creature bond/happiness data.
    /// </summary>
    [Serializable]
    public class CreatureBond
    {
        public float bondLevel = 0f;
        public float maxBondLevel = 100f;
        public float happiness = 50f;
        public float loyalty = 50f;
        public float lastFedTime;
        public float lastPlayedTime;
        public float lastGroomedTime;
        public int timesMounted = 0;
        public int timesInCombat = 0;
        public int totalKills = 0;

        public CreatureMood CurrentMood
        {
            get
            {
                if (happiness >= 90f) return CreatureMood.Ecstatic;
                if (happiness >= 70f) return CreatureMood.Happy;
                if (happiness >= 50f) return CreatureMood.Content;
                if (happiness >= 30f) return CreatureMood.Neutral;
                if (happiness >= 15f) return CreatureMood.Unhappy;
                if (happiness >= 5f) return CreatureMood.Angry;
                return CreatureMood.Hostile;
            }
        }

        public float BondProgress => bondLevel / maxBondLevel;
        public bool IsMaxBond => bondLevel >= maxBondLevel;

        public void AddBond(float amount)
        {
            bondLevel = Mathf.Min(maxBondLevel, bondLevel + amount);
        }

        public void AddHappiness(float amount)
        {
            happiness = Mathf.Clamp(happiness + amount, 0f, 100f);
        }

        public void AddLoyalty(float amount)
        {
            loyalty = Mathf.Clamp(loyalty + amount, 0f, 100f);
        }
    }

    #endregion

    #region ScriptableObjects

    /// <summary>
    /// Creature definition as a ScriptableObject.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCreatureDefinition", menuName = "UsefulScripts/Player/Creature Definition")]
    public class CreatureDefinition : ScriptableObject
    {
        [Header("Basic Info")]
        public string creatureId;
        public string creatureName;
        [TextArea(3, 5)]
        public string description;
        public Sprite icon;
        public GameObject prefab;
        public CreatureType creatureType;
        public CreatureRarity rarity;

        [Header("Stats")]
        public CreatureStats baseStats = new CreatureStats();
        public int maxLevel = 50;
        public long baseExpToLevel = 100;
        public float expScaling = 1.5f;

        [Header("Mount Properties")]
        public bool canBeMounted = true;
        public MountProperties mountProperties = new MountProperties();

        [Header("Combat")]
        public bool canFight = true;
        public List<CreatureAbility> abilities = new List<CreatureAbility>();

        [Header("Taming")]
        public TamingRequirements tamingRequirements = new TamingRequirements();
        public List<string> favoriteItems;
        public float hungerDecayPerHour = 5f;
        public float happinessDecayPerHour = 2f;

        [Header("Elements")]
        public ElementType primaryElement = ElementType.Physical;
        public List<ElementType> weaknesses = new List<ElementType>();
        public List<ElementType> resistances = new List<ElementType>();

        public TamedCreature CreateInstance(string instanceId = null)
        {
            return new TamedCreature
            {
                instanceId = instanceId ?? Guid.NewGuid().ToString(),
                definitionId = creatureId,
                nickname = creatureName,
                definition = this,
                stats = baseStats.Clone(),
                level = 1,
                currentExp = 0,
                expToNextLevel = baseExpToLevel,
                bond = new CreatureBond(),
                isActive = false,
                behaviorMode = TameBehavior.Follow
            };
        }
    }

    #endregion

    /// <summary>
    /// Represents an instance of a tamed creature.
    /// </summary>
    [Serializable]
    public class TamedCreature
    {
        public string instanceId;
        public string definitionId;
        public string nickname;
        public CreatureDefinition definition;
        public CreatureStats stats;
        public int level;
        public long currentExp;
        public long expToNextLevel;
        public CreatureBond bond;
        public bool isActive;
        public TameBehavior behaviorMode;
        public List<CreatureAbility> learnedAbilities = new List<CreatureAbility>();
        public GameObject spawnedInstance;

        public event Action<int, int> OnLevelUp;
        public event Action OnStatsChanged;
        public event Action<CreatureAbility> OnAbilityLearned;

        public float HealthPercent => stats.currentHealth / stats.maxHealth;
        public float StaminaPercent => stats.stamina / stats.maxStamina;
        public bool IsAlive => stats.currentHealth > 0;
        public bool CanBeMounted => definition != null && definition.canBeMounted && IsAlive;
        public CreatureMood Mood => bond.CurrentMood;

        public void AddExperience(long exp)
        {
            if (definition == null || level >= definition.maxLevel) return;

            currentExp += exp;
            while (currentExp >= expToNextLevel && level < definition.maxLevel)
            {
                currentExp -= expToNextLevel;
                LevelUp();
            }
        }

        private void LevelUp()
        {
            int previousLevel = level;
            level++;
            expToNextLevel = (long)(definition.baseExpToLevel * Math.Pow(level, definition.expScaling));

            // Apply stat growth
            stats.maxHealth = definition.baseStats.GetStatAtLevel(level, "Health");
            stats.maxStamina = definition.baseStats.GetStatAtLevel(level, "Stamina");
            stats.attack = definition.baseStats.GetStatAtLevel(level, "Attack");
            stats.defense = definition.baseStats.GetStatAtLevel(level, "Defense");
            stats.speed = definition.baseStats.GetStatAtLevel(level, "Speed");

            // Heal on level up
            stats.currentHealth = stats.maxHealth;
            stats.stamina = stats.maxStamina;

            // Check for new abilities
            foreach (var ability in definition.abilities)
            {
                if (ability.unlockLevel == level && !learnedAbilities.Contains(ability))
                {
                    learnedAbilities.Add(ability);
                    OnAbilityLearned?.Invoke(ability);
                }
            }

            OnLevelUp?.Invoke(previousLevel, level);
            OnStatsChanged?.Invoke();
        }

        public void Heal(float amount)
        {
            stats.currentHealth = Mathf.Min(stats.maxHealth, stats.currentHealth + amount);
            OnStatsChanged?.Invoke();
        }

        public void RestoreStamina(float amount)
        {
            stats.stamina = Mathf.Min(stats.maxStamina, stats.stamina + amount);
            OnStatsChanged?.Invoke();
        }

        public void TakeDamage(float damage)
        {
            stats.currentHealth = Mathf.Max(0, stats.currentHealth - damage);
            OnStatsChanged?.Invoke();
        }

        public void ConsumeStamina(float amount)
        {
            stats.stamina = Mathf.Max(0, stats.stamina - amount);
            OnStatsChanged?.Invoke();
        }
    }

    /// <summary>
    /// Comprehensive mount and taming system.
    /// Manages tamed creatures, mounting, and creature care.
    /// </summary>
    public class MountSystem : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CharacterSheet characterSheet;
        [SerializeField] private Transform mountPoint;
        [SerializeField] private Transform creatureSpawnPoint;

        [Header("Taming Settings")]
        [SerializeField] private string tamingSkillId = "taming";
        [SerializeField] private int maxTamedCreatures = 10;
        [SerializeField] private int maxActiveCreatures = 3;

        [Header("Mount Settings")]
        [SerializeField] private float mountTransitionTime = 1f;
        [SerializeField] private float dismountTransitionTime = 0.5f;
        [SerializeField] private LayerMask mountableLayer;

        [Header("Creature Care")]
        [SerializeField] private float feedCooldown = 300f; // 5 minutes
        [SerializeField] private float feedBondBonus = 5f;
        [SerializeField] private float feedHappinessBonus = 10f;
        [SerializeField] private float groomeHappinessBonus = 5f;

        // State
        private List<TamedCreature> tamedCreatures = new List<TamedCreature>();
        private List<TamedCreature> activeCreatures = new List<TamedCreature>();
        private TamedCreature currentMount;
        private bool isMounted;
        private bool isMounting;
        private bool isDismounting;
        private float mountingProgress;

        // Taming in progress
        private bool isTaming;
        private CreatureDefinition tamingTarget;
        private GameObject tamingTargetObject;
        private float tamingProgress;
        private float tamingDuration;

        #region Events

        public event Action<TamedCreature> OnCreatureTamed;
        public event Action<TamedCreature> OnCreatureReleased;
        public event Action<TamedCreature> OnCreatureSummoned;
        public event Action<TamedCreature> OnCreatureDismissed;
        public event Action<TamedCreature> OnMountStarted;
        public event Action<TamedCreature> OnMountCompleted;
        public event Action<TamedCreature> OnDismountStarted;
        public event Action<TamedCreature> OnDismountCompleted;
        public event Action<TamedCreature> OnCreatureLevelUp;
        public event Action<TamedCreature> OnCreatureDied;
        public event Action<float> OnTamingProgress;
        public event Action<bool> OnTamingComplete;

        #endregion

        #region Properties

        public IReadOnlyList<TamedCreature> TamedCreatures => tamedCreatures.AsReadOnly();
        public IReadOnlyList<TamedCreature> ActiveCreatures => activeCreatures.AsReadOnly();
        public TamedCreature CurrentMount => currentMount;
        public bool IsMounted => isMounted;
        public bool IsMounting => isMounting;
        public bool IsDismounting => isDismounting;
        public bool IsTaming => isTaming;
        public float TamingProgress => isTaming ? tamingProgress / tamingDuration : 0f;
        public int TamedCount => tamedCreatures.Count;
        public int ActiveCount => activeCreatures.Count;
        public bool CanTameMore => tamedCreatures.Count < maxTamedCreatures;
        public bool CanSummonMore => activeCreatures.Count < maxActiveCreatures;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (characterSheet == null)
            {
                characterSheet = GetComponent<CharacterSheet>();
            }
        }

        private void Update()
        {
            UpdateTaming();
            UpdateMounting();
            UpdateActiveCreatures();
        }

        #endregion

        #region Taming

        /// <summary>
        /// Attempts to start taming a creature.
        /// </summary>
        public bool StartTaming(CreatureDefinition creatureDef, GameObject creatureObject)
        {
            if (isTaming || isMounted || !CanTameMore) return false;
            if (creatureDef == null || creatureObject == null) return false;

            var requirements = creatureDef.tamingRequirements;

            // Check player level
            if (characterSheet != null && characterSheet.Level < requirements.requiredPlayerLevel)
            {
                Debug.Log($"Need level {requirements.requiredPlayerLevel} to tame {creatureDef.creatureName}");
                return false;
            }

            // Check taming skill
            if (characterSheet != null && !string.IsNullOrEmpty(tamingSkillId))
            {
                var skill = characterSheet.GetSkill(tamingSkillId);
                if (skill == null || skill.currentLevel < requirements.requiredTamingSkill)
                {
                    Debug.Log($"Need taming skill level {requirements.requiredTamingSkill}");
                    return false;
                }
            }

            isTaming = true;
            tamingTarget = creatureDef;
            tamingTargetObject = creatureObject;
            tamingProgress = 0f;
            tamingDuration = requirements.tameTime;

            return true;
        }

        /// <summary>
        /// Cancels the current taming attempt.
        /// </summary>
        public void CancelTaming()
        {
            if (!isTaming) return;

            isTaming = false;
            tamingTarget = null;
            tamingTargetObject = null;
            tamingProgress = 0f;
            OnTamingComplete?.Invoke(false);
        }

        private void UpdateTaming()
        {
            if (!isTaming) return;

            tamingProgress += Time.deltaTime;
            OnTamingProgress?.Invoke(TamingProgress);

            if (tamingProgress >= tamingDuration)
            {
                CompleteTaming();
            }
        }

        private void CompleteTaming()
        {
            if (tamingTarget == null)
            {
                CancelTaming();
                return;
            }

            var requirements = tamingTarget.tamingRequirements;
            float tameChance = requirements.baseTameChance;

            // Add bonus from taming skill
            if (characterSheet != null && !string.IsNullOrEmpty(tamingSkillId))
            {
                var skill = characterSheet.GetSkill(tamingSkillId);
                if (skill != null)
                {
                    tameChance += skill.currentLevel * requirements.tameChancePerSkill;
                }
            }

            bool success = UnityEngine.Random.value * 100f < tameChance;

            if (success)
            {
                var creature = tamingTarget.CreateInstance();
                tamedCreatures.Add(creature);

                creature.OnLevelUp += (prev, cur) => OnCreatureLevelUp?.Invoke(creature);

                // Destroy wild creature
                if (tamingTargetObject != null)
                {
                    Destroy(tamingTargetObject);
                }

                OnCreatureTamed?.Invoke(creature);
            }

            isTaming = false;
            tamingTarget = null;
            tamingTargetObject = null;
            tamingProgress = 0f;
            OnTamingComplete?.Invoke(success);
        }

        #endregion

        #region Creature Management

        /// <summary>
        /// Adds a tamed creature directly (e.g., from save data or rewards).
        /// </summary>
        public bool AddTamedCreature(TamedCreature creature)
        {
            if (!CanTameMore) return false;
            if (tamedCreatures.Any(c => c.instanceId == creature.instanceId)) return false;

            tamedCreatures.Add(creature);
            creature.OnLevelUp += (prev, cur) => OnCreatureLevelUp?.Invoke(creature);
            return true;
        }

        /// <summary>
        /// Releases a tamed creature back into the wild.
        /// </summary>
        public bool ReleaseCreature(string instanceId)
        {
            var creature = tamedCreatures.FirstOrDefault(c => c.instanceId == instanceId);
            if (creature == null) return false;

            if (creature == currentMount)
            {
                Dismount();
            }

            if (activeCreatures.Contains(creature))
            {
                DismissCreature(instanceId);
            }

            tamedCreatures.Remove(creature);
            OnCreatureReleased?.Invoke(creature);
            return true;
        }

        /// <summary>
        /// Summons a creature to follow the player.
        /// </summary>
        public bool SummonCreature(string instanceId)
        {
            if (!CanSummonMore) return false;

            var creature = tamedCreatures.FirstOrDefault(c => c.instanceId == instanceId);
            if (creature == null || !creature.IsAlive) return false;
            if (activeCreatures.Contains(creature)) return false;

            creature.isActive = true;
            activeCreatures.Add(creature);

            // Spawn the creature
            if (creature.definition.prefab != null && creatureSpawnPoint != null)
            {
                creature.spawnedInstance = Instantiate(
                    creature.definition.prefab,
                    creatureSpawnPoint.position,
                    creatureSpawnPoint.rotation
                );
            }

            OnCreatureSummoned?.Invoke(creature);
            return true;
        }

        /// <summary>
        /// Dismisses an active creature.
        /// </summary>
        public bool DismissCreature(string instanceId)
        {
            var creature = activeCreatures.FirstOrDefault(c => c.instanceId == instanceId);
            if (creature == null) return false;

            if (creature == currentMount)
            {
                Dismount();
            }

            creature.isActive = false;
            activeCreatures.Remove(creature);

            if (creature.spawnedInstance != null)
            {
                Destroy(creature.spawnedInstance);
                creature.spawnedInstance = null;
            }

            OnCreatureDismissed?.Invoke(creature);
            return true;
        }

        /// <summary>
        /// Gets a creature by instance ID.
        /// </summary>
        public TamedCreature GetCreature(string instanceId)
        {
            return tamedCreatures.FirstOrDefault(c => c.instanceId == instanceId);
        }

        /// <summary>
        /// Gets creatures of a specific type.
        /// </summary>
        public List<TamedCreature> GetCreaturesByType(CreatureType type)
        {
            return tamedCreatures.Where(c => c.definition.creatureType == type).ToList();
        }

        /// <summary>
        /// Gets all mountable creatures.
        /// </summary>
        public List<TamedCreature> GetMountableCreatures()
        {
            return tamedCreatures.Where(c => c.CanBeMounted).ToList();
        }

        #endregion

        #region Mounting

        /// <summary>
        /// Mounts a creature.
        /// </summary>
        public bool Mount(string instanceId)
        {
            if (isMounted || isMounting || isDismounting) return false;

            var creature = activeCreatures.FirstOrDefault(c => c.instanceId == instanceId);
            if (creature == null || !creature.CanBeMounted) return false;

            isMounting = true;
            currentMount = creature;
            mountingProgress = 0f;

            OnMountStarted?.Invoke(creature);
            return true;
        }

        /// <summary>
        /// Dismounts from the current mount.
        /// </summary>
        public bool Dismount()
        {
            if (!isMounted || isDismounting || isMounting) return false;

            isDismounting = true;
            mountingProgress = 0f;

            OnDismountStarted?.Invoke(currentMount);
            return true;
        }

        private void UpdateMounting()
        {
            if (isMounting)
            {
                mountingProgress += Time.deltaTime;
                float duration = currentMount?.definition.mountProperties.mountingTime ?? mountTransitionTime;

                if (mountingProgress >= duration)
                {
                    isMounting = false;
                    isMounted = true;

                    currentMount.bond.timesMounted++;
                    currentMount.bond.AddBond(0.5f);

                    OnMountCompleted?.Invoke(currentMount);
                }
            }
            else if (isDismounting)
            {
                mountingProgress += Time.deltaTime;
                float duration = currentMount?.definition.mountProperties.dismountingTime ?? dismountTransitionTime;

                if (mountingProgress >= duration)
                {
                    isDismounting = false;
                    isMounted = false;

                    var mount = currentMount;
                    currentMount = null;

                    OnDismountCompleted?.Invoke(mount);
                }
            }
        }

        /// <summary>
        /// Gets the current mount's speed.
        /// </summary>
        public float GetMountSpeed(bool isSprinting = false)
        {
            if (currentMount == null || !isMounted) return 0f;

            var props = currentMount.definition.mountProperties;
            float speed = props.groundSpeed;

            if (isSprinting)
            {
                speed *= props.sprintMultiplier;
            }

            // Apply creature's speed stat modifier
            float speedMod = currentMount.stats.speed / currentMount.definition.baseStats.speed;
            speed *= speedMod;

            return speed;
        }

        /// <summary>
        /// Checks if the mount can fly.
        /// </summary>
        public bool CanMountFly()
        {
            if (currentMount == null) return false;
            return (currentMount.definition.mountProperties.movementCapabilities & MovementCapability.Flying) != 0;
        }

        /// <summary>
        /// Checks if the mount can swim.
        /// </summary>
        public bool CanMountSwim()
        {
            if (currentMount == null) return false;
            return (currentMount.definition.mountProperties.movementCapabilities & MovementCapability.Water) != 0;
        }

        #endregion

        #region Creature Care

        /// <summary>
        /// Feeds a creature with an item.
        /// </summary>
        public bool FeedCreature(string instanceId, string itemId)
        {
            var creature = GetCreature(instanceId);
            if (creature == null) return false;

            float timeSinceLastFed = Time.time - creature.bond.lastFedTime;
            if (timeSinceLastFed < feedCooldown) return false;

            // Check if it's a favorite food
            float bondBonus = feedBondBonus;
            float happinessBonus = feedHappinessBonus;

            if (creature.definition.favoriteItems != null && creature.definition.favoriteItems.Contains(itemId))
            {
                bondBonus *= 2f;
                happinessBonus *= 1.5f;
            }

            creature.bond.lastFedTime = Time.time;
            creature.bond.AddBond(bondBonus);
            creature.bond.AddHappiness(happinessBonus);

            // Restore some health
            creature.Heal(creature.stats.maxHealth * 0.1f);

            return true;
        }

        /// <summary>
        /// Grooms a creature.
        /// </summary>
        public bool GroomCreature(string instanceId)
        {
            var creature = GetCreature(instanceId);
            if (creature == null) return false;

            creature.bond.lastGroomedTime = Time.time;
            creature.bond.AddBond(2f);
            creature.bond.AddHappiness(groomeHappinessBonus);

            return true;
        }

        /// <summary>
        /// Plays with a creature.
        /// </summary>
        public bool PlayWithCreature(string instanceId)
        {
            var creature = GetCreature(instanceId);
            if (creature == null) return false;

            creature.bond.lastPlayedTime = Time.time;
            creature.bond.AddBond(3f);
            creature.bond.AddHappiness(15f);
            creature.bond.AddLoyalty(2f);

            return true;
        }

        /// <summary>
        /// Heals a creature.
        /// </summary>
        public void HealCreature(string instanceId, float amount)
        {
            var creature = GetCreature(instanceId);
            creature?.Heal(amount);
        }

        /// <summary>
        /// Revives a dead creature.
        /// </summary>
        public bool ReviveCreature(string instanceId, float healthPercent = 0.5f)
        {
            var creature = GetCreature(instanceId);
            if (creature == null || creature.IsAlive) return false;

            creature.stats.currentHealth = creature.stats.maxHealth * healthPercent;
            creature.bond.AddHappiness(-10f); // Death penalty
            return true;
        }

        /// <summary>
        /// Renames a creature.
        /// </summary>
        public void RenameCreature(string instanceId, string newName)
        {
            var creature = GetCreature(instanceId);
            if (creature != null)
            {
                creature.nickname = newName;
            }
        }

        /// <summary>
        /// Sets a creature's behavior mode.
        /// </summary>
        public void SetCreatureBehavior(string instanceId, TameBehavior behavior)
        {
            var creature = GetCreature(instanceId);
            if (creature != null)
            {
                creature.behaviorMode = behavior;
            }
        }

        #endregion

        #region Updates

        private void UpdateActiveCreatures()
        {
            float deltaTime = Time.deltaTime;

            foreach (var creature in activeCreatures)
            {
                // Stamina regeneration
                if (creature != currentMount || !isMounted)
                {
                    creature.RestoreStamina(1f * deltaTime);
                }
                else if (isMounted && creature == currentMount)
                {
                    // Consume stamina while mounted
                    float staminaCost = creature.definition.mountProperties.staminaCostPerSecond * deltaTime;
                    creature.ConsumeStamina(staminaCost);
                }

                // Health regeneration
                if (creature.IsAlive && creature.HealthPercent < 1f)
                {
                    creature.Heal(0.5f * deltaTime);
                }

                // Check for death
                if (!creature.IsAlive)
                {
                    if (creature == currentMount)
                    {
                        Dismount();
                    }
                    OnCreatureDied?.Invoke(creature);
                }
            }
        }

        #endregion

        #region Utility

        /// <summary>
        /// Gets a summary of all tamed creatures.
        /// </summary>
        public string GetCreatureSummary()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"=== Tamed Creatures ({TamedCount}/{maxTamedCreatures}) ===");

            foreach (var creature in tamedCreatures)
            {
                string status = creature.isActive ? "[ACTIVE]" : "";
                if (creature == currentMount && isMounted) status = "[MOUNTED]";

                sb.AppendLine($"{creature.nickname} (Lv.{creature.level}) - {creature.definition.creatureName} {status}");
                sb.AppendLine($"  HP: {creature.stats.currentHealth:F0}/{creature.stats.maxHealth:F0}");
                sb.AppendLine($"  Bond: {creature.bond.bondLevel:F0}/{creature.bond.maxBondLevel} | Mood: {creature.Mood}");
            }

            return sb.ToString();
        }

        #endregion
    }
}
