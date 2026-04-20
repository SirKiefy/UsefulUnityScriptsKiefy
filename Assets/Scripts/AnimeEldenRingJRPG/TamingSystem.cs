using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UsefulScripts.AnimeEldenRingJRPG
{
    #region Enums

    /// <summary>
    /// Defines the elemental affinity of a tameable creature.
    /// </summary>
    public enum CreatureElement
    {
        Fire,
        Ice,
        Lightning,
        Earth,
        Wind,
        Water,
        Light,
        Dark,
        Void,
        Spirit,
        Nature,
        Arcane
    }

    /// <summary>
    /// Defines the temperament of a wild creature, affecting taming difficulty.
    /// </summary>
    public enum CreatureTemperament
    {
        Docile,         // Easy to tame
        Cautious,       // Moderate difficulty
        Aggressive,     // Hard to tame, must weaken first
        Proud,          // Requires specific items or conditions
        Mythical,       // Extremely rare, special ritual needed
        Corrupted,      // Must be purified before taming
        Divine          // Legendary, story-related taming
    }

    /// <summary>
    /// Defines the creature's role in the player's party during battle.
    /// </summary>
    public enum TamedCreatureRole
    {
        Striker,        // High damage output
        Guardian,       // Absorbs damage for the party
        Healer,         // Restores HP/MP to allies
        Buffer,         // Provides stat boosts
        Debuffer,       // Weakens enemies
        Speedster,      // Acts first, multiple hits
        Wildcard        // Unpredictable, high risk/reward
    }

    /// <summary>
    /// Defines the bond level between the player and a tamed creature.
    /// </summary>
    public enum BondRank
    {
        Stranger,       // Just captured
        Acquaintance,   // Basic obedience
        Companion,      // Follows commands reliably
        Partner,        // Unlocks combo attacks
        Soulbound,      // Shares buffs, transformation sync
        Awakened        // Ultimate bond, unlocks final evolution
    }

    /// <summary>
    /// Defines the evolution tier of a creature.
    /// </summary>
    public enum EvolutionTier
    {
        Base,
        Evolved,
        Ascended,
        Mythic,
        Divine
    }

    /// <summary>
    /// Defines the behavior AI mode for tamed creatures in the field.
    /// </summary>
    public enum FieldBehavior
    {
        FollowClose,    // Stays near the player
        Scout,          // Explores ahead, finds items
        Guard,          // Watches for enemies
        Gather,         // Collects nearby materials
        Rest,           // Recovers HP/stamina
        Free            // Roams freely, increases happiness
    }

    #endregion

    #region Data Classes

    /// <summary>
    /// Represents the base stats of a creature species.
    /// </summary>
    [Serializable]
    public class CreatureBaseStats
    {
        public int baseHP = 100;
        public int baseMP = 40;
        public int baseAttack = 20;
        public int baseDefense = 15;
        public int baseMagicAttack = 18;
        public int baseMagicDefense = 15;
        public int baseSpeed = 12;
        public int baseLuck = 10;

        [Header("Growth Rates (per level)")]
        public float hpGrowth = 12f;
        public float mpGrowth = 5f;
        public float attackGrowth = 2.5f;
        public float defenseGrowth = 2f;
        public float magicAttackGrowth = 2.2f;
        public float magicDefenseGrowth = 1.8f;
        public float speedGrowth = 1.2f;
        public float luckGrowth = 0.5f;

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
                "LCK" => baseLuck + (luckGrowth * (level - 1)),
                _ => 0
            };
        }
    }

    /// <summary>
    /// Represents a skill a tamed creature can learn and use in battle.
    /// </summary>
    [Serializable]
    public class CreatureSkill
    {
        public string skillId;
        public string skillName;
        [TextArea(2, 4)]
        public string description;
        public Sprite icon;
        public int learnLevel;
        public float mpCost;
        public float cooldown;
        public float power = 1f;
        public CreatureElement element = CreatureElement.Fire;
        public bool isAreaOfEffect;
        public int maxTargets = 1;
        public AudioClip skillSound;
        public GameObject skillVFX;

        [Header("Combo")]
        public bool canComboWithPlayer;
        public string comboSkillId;
    }

    /// <summary>
    /// Defines an evolution path for a creature species.
    /// </summary>
    [Serializable]
    public class CreatureEvolution
    {
        public string evolutionId;
        public string evolvedCreatureId;
        public string evolvedCreatureName;
        public Sprite evolvedIcon;
        public EvolutionTier targetTier;

        [Header("Requirements")]
        public int requiredLevel = 20;
        public BondRank requiredBondRank = BondRank.Companion;
        public List<string> requiredItems = new List<string>();
        public CreatureElement requiredElementalExposure;
        public bool requiresBossDefeat;
        public string requiredBossId;

        [Header("Stat Changes")]
        public float hpMultiplier = 1.3f;
        public float attackMultiplier = 1.25f;
        public float defenseMultiplier = 1.2f;
        public float speedMultiplier = 1.1f;
        public List<string> newSkillIds = new List<string>();
        public CreatureElement newElement;
    }

    /// <summary>
    /// Represents the taming requirements and difficulty for a creature.
    /// </summary>
    [Serializable]
    public class TamingRequirements
    {
        public float baseCaptureChance = 0.3f;
        public float healthThresholdForCapture = 0.3f;
        public List<string> preferredBaitItems = new List<string>();
        public float baitBonusChance = 0.15f;
        public CreatureTemperament temperament = CreatureTemperament.Cautious;
        public int minimumPlayerLevel = 1;
        public bool requiresSpecialItem;
        public string specialItemId;
        public bool requiresPurification;
        public int purificationCost = 0;
    }

    #endregion

    #region ScriptableObjects

    /// <summary>
    /// Creature species definition as a ScriptableObject.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCreature", menuName = "UsefulScripts/AnimeEldenRingJRPG/Creature Data")]
    public class CreatureData : ScriptableObject
    {
        [Header("Basic Info")]
        public string creatureId;
        public string creatureName;
        [TextArea(3, 5)]
        public string description;
        public Sprite icon;
        public GameObject prefab;
        public CreatureElement primaryElement;
        public CreatureTemperament temperament;
        public TamedCreatureRole preferredRole;

        [Header("Stats")]
        public CreatureBaseStats baseStats = new CreatureBaseStats();
        public int maxLevel = 99;

        [Header("Experience")]
        public int baseExpToLevel = 100;
        public float expScaling = 1.5f;
        public int expYieldOnDefeat = 50;

        [Header("Elemental Profile")]
        public List<CreatureElement> weaknesses = new List<CreatureElement>();
        public List<CreatureElement> resistances = new List<CreatureElement>();
        public List<CreatureElement> immunities = new List<CreatureElement>();

        [Header("Skills")]
        public List<CreatureSkill> learnableSkills = new List<CreatureSkill>();
        public int maxEquippedSkills = 4;

        [Header("Evolution")]
        public bool canEvolve;
        public List<CreatureEvolution> evolutionPaths = new List<CreatureEvolution>();

        [Header("Taming")]
        public TamingRequirements tamingRequirements = new TamingRequirements();

        [Header("Visuals")]
        public RuntimeAnimatorController animatorController;
        public Color auraColor = Color.white;
        public GameObject evolutionVFX;
    }

    /// <summary>
    /// Configuration for the taming system.
    /// </summary>
    [CreateAssetMenu(fileName = "TamingConfig", menuName = "UsefulScripts/AnimeEldenRingJRPG/Taming Config")]
    public class TamingConfig : ScriptableObject
    {
        [Header("General")]
        public int maxPartySize = 4;
        public int maxStorageSize = 100;
        public float bondExpPerBattle = 10f;
        public float bondExpPerFeed = 5f;
        public float bondExpPerPet = 2f;

        [Header("Capture")]
        public float captureBaseChance = 0.2f;
        public float lowHealthBonus = 0.3f;
        public float statusEffectBonus = 0.1f;
        public float bondItemBonus = 0.15f;
        public int maxCaptureAttempts = 3;

        [Header("Experience")]
        public float partyExpShareRatio = 0.75f;
        public float storageExpShareRatio = 0.1f;
        public float bondMultiplierForExp = 1.5f;

        [Header("Evolution")]
        public float evolutionAnimationDuration = 3f;
        public GameObject defaultEvolutionVFX;
        public AudioClip evolutionSound;

        [Header("Available Creatures")]
        public List<CreatureData> allCreatures = new List<CreatureData>();
    }

    #endregion

    #region Runtime Classes

    /// <summary>
    /// Represents an individual tamed creature instance with its own stats, level, and bond.
    /// </summary>
    [Serializable]
    public class TamedCreature
    {
        public string instanceId;
        public CreatureData creatureData;
        public string nickname;
        public int level = 1;
        public int currentExp;
        public EvolutionTier currentTier = EvolutionTier.Base;
        public TamedCreatureRole assignedRole;
        public FieldBehavior fieldBehavior = FieldBehavior.FollowClose;

        [Header("Current Stats")]
        public float currentHP;
        public float currentMP;
        public float happiness = 50f;
        public float loyalty = 0f;
        public bool isFainted;

        [Header("Bond")]
        public BondRank bondRank = BondRank.Stranger;
        public float bondExp;
        public float bondExpToNext = 100f;

        [Header("Skills")]
        public List<CreatureSkill> equippedSkills = new List<CreatureSkill>();
        public List<string> learnedSkillIds = new List<string>();

        [Header("Battle Stats")]
        public int battlesWon;
        public int battlesFought;
        public int totalDamageDealt;
        public int totalDamageTaken;

        // Events
        public event Action<TamedCreature, int> OnLevelUp;
        public event Action<TamedCreature, BondRank> OnBondRankUp;
        public event Action<TamedCreature, CreatureSkill> OnSkillLearned;
        public event Action<TamedCreature, EvolutionTier> OnEvolutionReady;
        public event Action<TamedCreature> OnFainted;
        public event Action<TamedCreature> OnRevived;

        public float MaxHP => creatureData != null ? creatureData.baseStats.GetStatAtLevel("HP", level) : 100;
        public float MaxMP => creatureData != null ? creatureData.baseStats.GetStatAtLevel("MP", level) : 40;
        public float Attack => creatureData != null ? creatureData.baseStats.GetStatAtLevel("ATK", level) : 20;
        public float Defense => creatureData != null ? creatureData.baseStats.GetStatAtLevel("DEF", level) : 15;
        public float MagicAttack => creatureData != null ? creatureData.baseStats.GetStatAtLevel("MATK", level) : 18;
        public float MagicDefense => creatureData != null ? creatureData.baseStats.GetStatAtLevel("MDEF", level) : 15;
        public float Speed => creatureData != null ? creatureData.baseStats.GetStatAtLevel("SPD", level) : 12;

        public int ExpToNextLevel => creatureData != null
            ? Mathf.RoundToInt(creatureData.baseExpToLevel * Mathf.Pow(creatureData.expScaling, level - 1))
            : 100;

        public void AddExperience(int amount)
        {
            if (isFainted || creatureData == null) return;
            if (level >= creatureData.maxLevel) return;

            currentExp += amount;
            while (currentExp >= ExpToNextLevel && level < creatureData.maxLevel)
            {
                currentExp -= ExpToNextLevel;
                level++;
                currentHP = MaxHP;
                currentMP = MaxMP;
                CheckNewSkills();
                OnLevelUp?.Invoke(this, level);
            }
        }

        public void AddBondExp(float amount)
        {
            bondExp += amount;
            while (bondExp >= bondExpToNext && bondRank < BondRank.Awakened)
            {
                bondExp -= bondExpToNext;
                bondRank++;
                bondExpToNext *= 1.5f;
                OnBondRankUp?.Invoke(this, bondRank);
            }
        }

        public void TakeDamage(float damage)
        {
            currentHP = Mathf.Max(0, currentHP - damage);
            totalDamageTaken += Mathf.RoundToInt(damage);
            if (currentHP <= 0 && !isFainted)
            {
                isFainted = true;
                OnFainted?.Invoke(this);
            }
        }

        public void Heal(float amount)
        {
            if (isFainted) return;
            currentHP = Mathf.Min(MaxHP, currentHP + amount);
        }

        public void Revive(float healthPercent = 0.5f)
        {
            if (!isFainted) return;
            isFainted = false;
            currentHP = MaxHP * healthPercent;
            OnRevived?.Invoke(this);
        }

        public bool CanEvolve()
        {
            if (creatureData == null || !creatureData.canEvolve) return false;
            return creatureData.evolutionPaths.Any(e =>
                level >= e.requiredLevel && bondRank >= e.requiredBondRank);
        }

        private void CheckNewSkills()
        {
            if (creatureData == null) return;
            foreach (var skill in creatureData.learnableSkills)
            {
                if (skill.learnLevel == level && !learnedSkillIds.Contains(skill.skillId))
                {
                    learnedSkillIds.Add(skill.skillId);
                    if (equippedSkills.Count < creatureData.maxEquippedSkills)
                    {
                        equippedSkills.Add(skill);
                    }
                    OnSkillLearned?.Invoke(this, skill);
                }
            }
        }

        public void Initialize()
        {
            instanceId = Guid.NewGuid().ToString();
            currentHP = MaxHP;
            currentMP = MaxMP;
            if (creatureData != null)
            {
                assignedRole = creatureData.preferredRole;
                foreach (var skill in creatureData.learnableSkills)
                {
                    if (skill.learnLevel <= level)
                    {
                        learnedSkillIds.Add(skill.skillId);
                        if (equippedSkills.Count < creatureData.maxEquippedSkills)
                        {
                            equippedSkills.Add(skill);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Represents the result of a capture attempt.
    /// </summary>
    [Serializable]
    public class CaptureResult
    {
        public bool success;
        public float captureRoll;
        public float requiredRoll;
        public string failureReason;
        public TamedCreature capturedCreature;
    }

    #endregion

    /// <summary>
    /// Manages creature taming, capturing, bonding, evolution, and party management.
    /// Provides an anime JRPG–style taming system inspired by Elden Ring's exploration and creature variety.
    /// </summary>
    public class TamingSystem : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private TamingConfig config;

        [Header("Party")]
        [SerializeField] private List<TamedCreature> activeParty = new List<TamedCreature>();
        [SerializeField] private List<TamedCreature> storage = new List<TamedCreature>();

        [Header("State")]
        [SerializeField] private TamedCreature currentFieldCreature;
        [SerializeField] private int captureAttemptsMade;

        // Events
        public event Action<TamedCreature> OnCreatureCaptured;
        public event Action<TamedCreature> OnCreatureReleased;
        public event Action<TamedCreature, int> OnCreatureLevelUp;
        public event Action<TamedCreature, EvolutionTier> OnCreatureEvolved;
        public event Action<TamedCreature> OnCreatureAddedToParty;
        public event Action<TamedCreature> OnCreatureRemovedFromParty;
        public event Action<TamedCreature> OnCreatureSummoned;
        public event Action<TamedCreature> OnCreatureRecalled;
        public event Action<TamedCreature, BondRank> OnBondRankUp;
        public event Action<CaptureResult> OnCaptureAttempt;

        // Properties
        public List<TamedCreature> ActiveParty => activeParty;
        public List<TamedCreature> Storage => storage;
        public TamedCreature CurrentFieldCreature => currentFieldCreature;
        public int PartyCount => activeParty.Count;
        public int StorageCount => storage.Count;

        #region Capture

        /// <summary>
        /// Attempts to capture a wild creature. Lower health and correct bait increase success chance.
        /// </summary>
        public CaptureResult AttemptCapture(CreatureData creatureData, float targetHealthPercent, string baitItemId = null)
        {
            var result = new CaptureResult();

            if (creatureData == null)
            {
                result.failureReason = "Invalid creature data.";
                OnCaptureAttempt?.Invoke(result);
                return result;
            }

            var tamingReq = creatureData.tamingRequirements;

            // Check if special item is required
            if (tamingReq.requiresSpecialItem)
            {
                result.failureReason = $"Requires special item: {tamingReq.specialItemId}";
                OnCaptureAttempt?.Invoke(result);
                return result;
            }

            // Check if creature needs purification
            if (tamingReq.requiresPurification)
            {
                result.failureReason = "This creature must be purified first.";
                OnCaptureAttempt?.Invoke(result);
                return result;
            }

            // Check max attempts
            if (config != null && captureAttemptsMade >= config.maxCaptureAttempts)
            {
                result.failureReason = "Maximum capture attempts reached.";
                OnCaptureAttempt?.Invoke(result);
                return result;
            }

            // Calculate capture chance
            float chance = config != null ? config.captureBaseChance : tamingReq.baseCaptureChance;

            // Low health bonus
            if (targetHealthPercent <= tamingReq.healthThresholdForCapture)
            {
                chance += config != null ? config.lowHealthBonus : 0.3f;
            }

            // Bait bonus
            if (!string.IsNullOrEmpty(baitItemId) && tamingReq.preferredBaitItems.Contains(baitItemId))
            {
                chance += tamingReq.baitBonusChance;
            }

            // Temperament modifier
            chance *= GetTemperamentModifier(tamingReq.temperament);

            chance = Mathf.Clamp01(chance);

            // Roll
            float roll = UnityEngine.Random.Range(0f, 1f);
            result.captureRoll = roll;
            result.requiredRoll = 1f - chance;

            captureAttemptsMade++;

            if (roll <= chance)
            {
                result.success = true;
                var newCreature = new TamedCreature { creatureData = creatureData };
                newCreature.Initialize();
                result.capturedCreature = newCreature;

                AddCreatureToStorage(newCreature);
                OnCreatureCaptured?.Invoke(newCreature);
            }
            else
            {
                result.failureReason = "Capture failed. The creature resisted!";
            }

            OnCaptureAttempt?.Invoke(result);
            return result;
        }

        /// <summary>
        /// Attempts to capture using a special ritual item (for Divine/Mythical creatures).
        /// </summary>
        public CaptureResult AttemptRitualCapture(CreatureData creatureData, string ritualItemId, float playerBondPower)
        {
            var result = new CaptureResult();

            if (creatureData == null)
            {
                result.failureReason = "Invalid creature data.";
                OnCaptureAttempt?.Invoke(result);
                return result;
            }

            var tamingReq = creatureData.tamingRequirements;

            if (tamingReq.requiresSpecialItem && tamingReq.specialItemId != ritualItemId)
            {
                result.failureReason = $"Wrong ritual item. Requires: {tamingReq.specialItemId}";
                OnCaptureAttempt?.Invoke(result);
                return result;
            }

            float chance = tamingReq.baseCaptureChance + (playerBondPower * 0.01f);
            chance = Mathf.Clamp01(chance);

            float roll = UnityEngine.Random.Range(0f, 1f);
            result.captureRoll = roll;
            result.requiredRoll = 1f - chance;

            if (roll <= chance)
            {
                result.success = true;
                var newCreature = new TamedCreature { creatureData = creatureData };
                newCreature.Initialize();
                result.capturedCreature = newCreature;

                AddCreatureToStorage(newCreature);
                OnCreatureCaptured?.Invoke(newCreature);
            }
            else
            {
                result.failureReason = "The ritual failed. The creature's will is too strong!";
            }

            OnCaptureAttempt?.Invoke(result);
            return result;
        }

        public void ResetCaptureAttempts()
        {
            captureAttemptsMade = 0;
        }

        private float GetTemperamentModifier(CreatureTemperament temperament)
        {
            return temperament switch
            {
                CreatureTemperament.Docile => 1.5f,
                CreatureTemperament.Cautious => 1.0f,
                CreatureTemperament.Aggressive => 0.6f,
                CreatureTemperament.Proud => 0.4f,
                CreatureTemperament.Mythical => 0.2f,
                CreatureTemperament.Corrupted => 0.3f,
                CreatureTemperament.Divine => 0.1f,
                _ => 1.0f
            };
        }

        #endregion

        #region Party Management

        /// <summary>
        /// Adds a creature from storage to the active party.
        /// </summary>
        public bool AddToParty(TamedCreature creature)
        {
            int maxParty = config != null ? config.maxPartySize : 4;
            if (activeParty.Count >= maxParty) return false;
            if (activeParty.Contains(creature)) return false;

            storage.Remove(creature);
            activeParty.Add(creature);
            OnCreatureAddedToParty?.Invoke(creature);
            return true;
        }

        /// <summary>
        /// Removes a creature from the active party back to storage.
        /// </summary>
        public bool RemoveFromParty(TamedCreature creature)
        {
            if (!activeParty.Contains(creature)) return false;

            activeParty.Remove(creature);
            storage.Add(creature);
            OnCreatureRemovedFromParty?.Invoke(creature);
            return true;
        }

        /// <summary>
        /// Swaps two creatures between party and storage.
        /// </summary>
        public bool SwapCreature(TamedCreature partyCreature, TamedCreature storageCreature)
        {
            if (!activeParty.Contains(partyCreature) || !storage.Contains(storageCreature)) return false;

            int index = activeParty.IndexOf(partyCreature);
            activeParty[index] = storageCreature;
            storage.Remove(storageCreature);
            storage.Add(partyCreature);

            OnCreatureRemovedFromParty?.Invoke(partyCreature);
            OnCreatureAddedToParty?.Invoke(storageCreature);
            return true;
        }

        /// <summary>
        /// Releases a creature permanently.
        /// </summary>
        public void ReleaseCreature(TamedCreature creature)
        {
            activeParty.Remove(creature);
            storage.Remove(creature);
            OnCreatureReleased?.Invoke(creature);
        }

        private void AddCreatureToStorage(TamedCreature creature)
        {
            int maxStorage = config != null ? config.maxStorageSize : 100;
            if (storage.Count < maxStorage)
            {
                storage.Add(creature);
            }
        }

        #endregion

        #region Field & Summoning

        /// <summary>
        /// Summons a party creature into the game world.
        /// </summary>
        public void SummonCreature(int partyIndex)
        {
            if (partyIndex < 0 || partyIndex >= activeParty.Count) return;
            var creature = activeParty[partyIndex];
            if (creature.isFainted) return;

            currentFieldCreature = creature;
            OnCreatureSummoned?.Invoke(creature);
        }

        /// <summary>
        /// Recalls the current field creature.
        /// </summary>
        public void RecallCreature()
        {
            if (currentFieldCreature == null) return;
            var creature = currentFieldCreature;
            currentFieldCreature = null;
            OnCreatureRecalled?.Invoke(creature);
        }

        /// <summary>
        /// Sets the field behavior mode for a creature.
        /// </summary>
        public void SetFieldBehavior(TamedCreature creature, FieldBehavior behavior)
        {
            if (creature != null)
            {
                creature.fieldBehavior = behavior;
            }
        }

        #endregion

        #region Bonding

        /// <summary>
        /// Feeds a creature to increase bond and happiness.
        /// </summary>
        public void FeedCreature(TamedCreature creature, string itemId)
        {
            if (creature == null || creature.isFainted) return;

            float bondGain = config != null ? config.bondExpPerFeed : 5f;
            creature.AddBondExp(bondGain);
            creature.happiness = Mathf.Min(100f, creature.happiness + 5f);
        }

        /// <summary>
        /// Pets a creature to slightly increase bond.
        /// </summary>
        public void PetCreature(TamedCreature creature)
        {
            if (creature == null || creature.isFainted) return;

            float bondGain = config != null ? config.bondExpPerPet : 2f;
            creature.AddBondExp(bondGain);
            creature.happiness = Mathf.Min(100f, creature.happiness + 2f);
        }

        /// <summary>
        /// Awards bond experience after a battle.
        /// </summary>
        public void AwardBattleBondExp(TamedCreature creature)
        {
            if (creature == null) return;

            float bondGain = config != null ? config.bondExpPerBattle : 10f;
            creature.AddBondExp(bondGain);
            creature.battlesFought++;
        }

        #endregion

        #region Evolution

        /// <summary>
        /// Evolves a creature if it meets the requirements.
        /// </summary>
        public bool EvolveCreature(TamedCreature creature, string evolutionId)
        {
            if (creature == null || creature.creatureData == null) return false;
            if (!creature.creatureData.canEvolve) return false;

            var evolution = creature.creatureData.evolutionPaths
                .FirstOrDefault(e => e.evolutionId == evolutionId);

            if (evolution == null) return false;
            if (creature.level < evolution.requiredLevel) return false;
            if (creature.bondRank < evolution.requiredBondRank) return false;

            // Apply evolution stat changes
            creature.currentTier = evolution.targetTier;

            // Learn new skills
            foreach (var skillId in evolution.newSkillIds)
            {
                if (!creature.learnedSkillIds.Contains(skillId))
                {
                    creature.learnedSkillIds.Add(skillId);
                }
            }

            creature.currentHP = creature.MaxHP;
            creature.currentMP = creature.MaxMP;

            OnCreatureEvolved?.Invoke(creature, evolution.targetTier);
            return true;
        }

        #endregion

        #region Experience Distribution

        /// <summary>
        /// Distributes experience to the active party after a battle.
        /// </summary>
        public void DistributeBattleExp(int totalExp)
        {
            if (activeParty.Count == 0) return;

            float partyShare = config != null ? config.partyExpShareRatio : 0.75f;
            float storageShare = config != null ? config.storageExpShareRatio : 0.1f;

            int partyExp = Mathf.RoundToInt(totalExp * partyShare / activeParty.Count);
            foreach (var creature in activeParty)
            {
                float bondMultiplier = creature.bondRank >= BondRank.Partner
                    ? (config != null ? config.bondMultiplierForExp : 1.5f)
                    : 1f;
                creature.AddExperience(Mathf.RoundToInt(partyExp * bondMultiplier));
            }

            if (storage.Count > 0)
            {
                int storageExp = Mathf.RoundToInt(totalExp * storageShare / storage.Count);
                foreach (var creature in storage)
                {
                    creature.AddExperience(storageExp);
                }
            }
        }

        #endregion

        #region Queries

        /// <summary>
        /// Gets all creatures of a specific element.
        /// </summary>
        public List<TamedCreature> GetCreaturesByElement(CreatureElement element)
        {
            var all = new List<TamedCreature>(activeParty);
            all.AddRange(storage);
            return all.Where(c => c.creatureData != null && c.creatureData.primaryElement == element).ToList();
        }

        /// <summary>
        /// Gets all creatures that can evolve.
        /// </summary>
        public List<TamedCreature> GetEvolvableCreatures()
        {
            var all = new List<TamedCreature>(activeParty);
            all.AddRange(storage);
            return all.Where(c => c.CanEvolve()).ToList();
        }

        /// <summary>
        /// Gets the total bond power across all party members (used for ritual captures).
        /// </summary>
        public float GetTotalBondPower()
        {
            return activeParty.Sum(c => (int)c.bondRank * 20f);
        }

        /// <summary>
        /// Gets all fainted party creatures.
        /// </summary>
        public List<TamedCreature> GetFaintedCreatures()
        {
            return activeParty.Where(c => c.isFainted).ToList();
        }

        #endregion

        #region Save Data

        /// <summary>
        /// Save data structure for the taming system.
        /// </summary>
        [Serializable]
        public class TamingSaveData
        {
            public List<CreatureSaveData> partyData = new List<CreatureSaveData>();
            public List<CreatureSaveData> storageData = new List<CreatureSaveData>();
        }

        [Serializable]
        public class CreatureSaveData
        {
            public string instanceId;
            public string creatureDataId;
            public string nickname;
            public int level;
            public int currentExp;
            public int currentTier;
            public int assignedRole;
            public float currentHP;
            public float currentMP;
            public float happiness;
            public float loyalty;
            public bool isFainted;
            public int bondRank;
            public float bondExp;
            public float bondExpToNext;
            public List<string> learnedSkillIds;
            public int battlesWon;
            public int battlesFought;
        }

        public TamingSaveData GetSaveData()
        {
            var data = new TamingSaveData();
            foreach (var creature in activeParty) data.partyData.Add(CreateSaveData(creature));
            foreach (var creature in storage) data.storageData.Add(CreateSaveData(creature));
            return data;
        }

        private CreatureSaveData CreateSaveData(TamedCreature creature)
        {
            return new CreatureSaveData
            {
                instanceId = creature.instanceId,
                creatureDataId = creature.creatureData != null ? creature.creatureData.creatureId : "",
                nickname = creature.nickname,
                level = creature.level,
                currentExp = creature.currentExp,
                currentTier = (int)creature.currentTier,
                assignedRole = (int)creature.assignedRole,
                currentHP = creature.currentHP,
                currentMP = creature.currentMP,
                happiness = creature.happiness,
                loyalty = creature.loyalty,
                isFainted = creature.isFainted,
                bondRank = (int)creature.bondRank,
                bondExp = creature.bondExp,
                bondExpToNext = creature.bondExpToNext,
                learnedSkillIds = new List<string>(creature.learnedSkillIds),
                battlesWon = creature.battlesWon,
                battlesFought = creature.battlesFought
            };
        }

        #endregion
    }
}
