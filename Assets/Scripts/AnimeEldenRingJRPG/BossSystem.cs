using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UsefulScripts.AnimeEldenRingJRPG
{
    #region Enums

    /// <summary>
    /// Defines the boss difficulty tier.
    /// </summary>
    public enum BossTier
    {
        MiniBoss,       // Optional, small arenas
        RegionBoss,     // Guards region progression
        DemigodBoss,    // Major story bosses (Elden Ring style)
        SecretBoss,     // Hidden optional bosses
        FinalBoss,      // End-game boss
        RaidBoss        // Co-op required or extreme challenge
    }

    /// <summary>
    /// Defines the current phase of a boss fight.
    /// </summary>
    public enum BossPhase
    {
        Phase1,
        Phase2,
        Phase3,
        Enrage,
        Desperation,    // Final stand, anime power-up
        Defeated
    }

    /// <summary>
    /// Defines the attack pattern type for a boss.
    /// </summary>
    public enum BossAttackType
    {
        Melee,
        Ranged,
        AreaOfEffect,
        Grab,
        Charge,
        Summon,
        UltimateAttack, // Cinematic anime attack
        EnvironmentHazard,
        PhaseTransition
    }

    /// <summary>
    /// Defines a boss's combat stance or behavior mode.
    /// </summary>
    public enum BossBehavior
    {
        Aggressive,     // Constant pressure
        Defensive,      // Counters and punishes
        Balanced,       // Mix of offense and defense
        Berserker,      // Low health, high damage
        Tactical,       // Uses environment and summons
        Passive         // Dialogue or cutscene phase
    }

    /// <summary>
    /// Defines a weak point location on a boss.
    /// </summary>
    public enum WeakPointLocation
    {
        Head,
        Chest,
        Back,
        Tail,
        Wings,
        Legs,
        Core,           // Exposed during specific attacks
        Horn,
        Eye,
        WeaponArm
    }

    #endregion

    #region Data Classes

    /// <summary>
    /// Represents a boss attack pattern.
    /// </summary>
    [Serializable]
    public class BossAttack
    {
        public string attackId;
        public string attackName;
        [TextArea(2, 3)]
        public string description;
        public BossAttackType attackType;
        public float damage;
        public float range;
        public float cooldown;
        public float windupTime;        // Telegraph time before attack
        public float recoveryTime;      // Punish window after attack
        public bool isBlockable;
        public bool isDodgeable = true;
        public bool isParriable;

        [Header("AoE")]
        public float aoeRadius;
        public bool leavesHazard;
        public float hazardDuration;

        [Header("Phase")]
        public BossPhase availableInPhase = BossPhase.Phase1;
        public float useChance = 1f;

        [Header("Visuals")]
        public AudioClip attackSound;
        public GameObject attackVFX;
        public string animationTrigger;
    }

    /// <summary>
    /// Represents a weak point on a boss that can be targeted for extra damage.
    /// </summary>
    [Serializable]
    public class BossWeakPoint
    {
        public string weakPointId;
        public WeakPointLocation location;
        public float damageMultiplier = 2f;
        public bool isExposed = true;
        public float exposureDuration;      // 0 = always exposed
        public int hitsToBreak = 3;
        public int currentHits;
        public bool isBroken;

        [Header("Break Rewards")]
        public float staggerDuration = 3f;
        public string bonusItemDropId;
        public int bonusRuneReward;

        public void Hit()
        {
            if (isBroken) return;
            currentHits++;
            if (currentHits >= hitsToBreak)
            {
                isBroken = true;
            }
        }

        public void Reset()
        {
            currentHits = 0;
            isBroken = false;
        }
    }

    /// <summary>
    /// Represents the phase transition configuration for a boss.
    /// </summary>
    [Serializable]
    public class PhaseTransition
    {
        public BossPhase fromPhase;
        public BossPhase toPhase;
        public float healthThreshold;          // Transition at this HP %
        public bool hasCutscene;
        public float invulnerabilityDuration = 3f;

        [Header("Phase Changes")]
        public float damageMultiplier = 1.2f;
        public float speedMultiplier = 1.1f;
        public float defenseMultiplier = 1f;
        public List<string> newAttackIds = new List<string>();
        public List<string> newWeakPointIds = new List<string>();

        [Header("Visuals")]
        public AudioClip transitionSound;
        public GameObject transitionVFX;
        public string animationTrigger;
    }

    /// <summary>
    /// Represents the rewards for defeating a boss.
    /// </summary>
    [Serializable]
    public class BossRewards
    {
        public int runeReward = 10000;
        public int expReward = 5000;
        public List<string> guaranteedItemIds = new List<string>();
        public List<string> possibleItemIds = new List<string>();
        public float rareDropChance = 0.1f;
        public string unlockedCreatureId;       // Creature available to tame after defeat
        public string unlockedRegionId;         // Region that becomes accessible
        public string unlockedRecipeId;         // Crafting recipe reward
        public string achievementId;
    }

    #endregion

    #region ScriptableObjects

    /// <summary>
    /// Boss definition as a ScriptableObject.
    /// </summary>
    [CreateAssetMenu(fileName = "NewBoss", menuName = "UsefulScripts/AnimeEldenRingJRPG/Boss Data")]
    public class BossData : ScriptableObject
    {
        [Header("Identity")]
        public string bossId;
        public string bossName;
        public string bossTitle;           // e.g., "Margit, the Fell Omen"
        [TextArea(3, 5)]
        public string loreDescription;
        public Sprite portrait;
        public GameObject prefab;
        public BossTier tier;

        [Header("Stats")]
        public float maxHealth = 10000f;
        public float attack = 100f;
        public float defense = 50f;
        public float magicDefense = 40f;
        public float speed = 10f;
        public int level = 30;

        [Header("Elemental Profile")]
        public CreatureElement primaryElement;
        public List<CreatureElement> weaknesses = new List<CreatureElement>();
        public List<CreatureElement> resistances = new List<CreatureElement>();
        public List<CreatureElement> immunities = new List<CreatureElement>();

        [Header("Attacks")]
        public List<BossAttack> attacks = new List<BossAttack>();

        [Header("Weak Points")]
        public List<BossWeakPoint> weakPoints = new List<BossWeakPoint>();

        [Header("Phases")]
        public List<PhaseTransition> phaseTransitions = new List<PhaseTransition>();
        public int totalPhases = 2;

        [Header("Behavior")]
        public BossBehavior defaultBehavior = BossBehavior.Balanced;

        [Header("Rewards")]
        public BossRewards rewards = new BossRewards();

        [Header("Arena")]
        public string arenaSceneName;
        public Vector3 spawnPosition;
        public float arenaRadius = 30f;

        [Header("Audio")]
        public AudioClip bossMusic;
        public AudioClip phaseTransitionMusic;
        public AudioClip defeatMusic;
    }

    /// <summary>
    /// Configuration for the boss encounter system.
    /// </summary>
    [CreateAssetMenu(fileName = "BossConfig", menuName = "UsefulScripts/AnimeEldenRingJRPG/Boss Config")]
    public class BossConfig : ScriptableObject
    {
        [Header("Difficulty")]
        public float globalDamageMultiplier = 1f;
        public float globalHealthMultiplier = 1f;
        public float enrageTimerSeconds = 600f;

        [Header("Stagger")]
        public float staggerThreshold = 100f;
        public float staggerRecoveryRate = 5f;
        public float staggerDuration = 3f;

        [Header("Rewards")]
        public float firstKillBonusMultiplier = 2f;
        public bool scaleRewardsWithLevel = true;

        [Header("Boss List")]
        public List<BossData> allBosses = new List<BossData>();
    }

    #endregion

    /// <summary>
    /// Manages boss encounters with phases, weak points, stagger mechanics,
    /// and rewards. Designed for Elden Ring–style soulslike bosses with anime flair.
    /// </summary>
    public class BossSystem : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private BossConfig config;

        [Header("Current Fight")]
        [SerializeField] private BossData currentBoss;
        [SerializeField] private BossPhase currentPhase = BossPhase.Phase1;
        [SerializeField] private float currentHealth;
        [SerializeField] private float staggerMeter;
        [SerializeField] private bool isStaggered;
        [SerializeField] private float staggerTimer;
        [SerializeField] private bool isFightActive;
        [SerializeField] private float fightTimer;
        [SerializeField] private bool isInvulnerable;
        [SerializeField] private float invulnerabilityTimer;

        [Header("Fight Stats")]
        [SerializeField] private int playerDeathCount;
        [SerializeField] private float totalDamageDealt;
        [SerializeField] private float totalDamageTaken;
        [SerializeField] private int weakPointsHit;

        [Header("History")]
        [SerializeField] private List<string> defeatedBossIds = new List<string>();

        // Events
        public event Action<BossData> OnBossFightStarted;
        public event Action<BossData, BossRewards> OnBossDefeated;
        public event Action<BossData> OnBossFightFailed;
        public event Action<BossPhase, BossPhase> OnPhaseTransition;
        public event Action<float> OnBossHealthChanged;
        public event Action<BossWeakPoint> OnWeakPointHit;
        public event Action<BossWeakPoint> OnWeakPointBroken;
        public event Action OnBossStaggered;
        public event Action OnBossRecoveredFromStagger;
        public event Action<BossAttack> OnBossAttackWindup;
        public event Action OnBossEnraged;

        // Properties
        public bool IsFightActive => isFightActive;
        public BossData CurrentBoss => currentBoss;
        public BossPhase CurrentPhase => currentPhase;
        public float CurrentHealth => currentHealth;
        public float CurrentHealthPercent => currentBoss != null ? currentHealth / (currentBoss.maxHealth * GetHealthMultiplier()) : 0f;
        public bool IsStaggered => isStaggered;
        public bool IsInvulnerable => isInvulnerable;

        private void Update()
        {
            if (!isFightActive) return;

            UpdateStagger();
            UpdateInvulnerability();
            UpdateEnrageTimer();
        }

        #region Fight Lifecycle

        /// <summary>
        /// Starts a boss fight encounter.
        /// </summary>
        public void StartBossFight(string bossId)
        {
            if (config == null) return;

            var boss = config.allBosses.FirstOrDefault(b => b.bossId == bossId);
            if (boss == null) return;

            StartBossFight(boss);
        }

        /// <summary>
        /// Starts a boss fight encounter with a BossData reference.
        /// </summary>
        public void StartBossFight(BossData boss)
        {
            if (boss == null) return;

            currentBoss = boss;
            currentHealth = boss.maxHealth * GetHealthMultiplier();
            currentPhase = BossPhase.Phase1;
            staggerMeter = 0;
            isStaggered = false;
            isFightActive = true;
            fightTimer = 0;
            isInvulnerable = false;
            playerDeathCount = 0;
            totalDamageDealt = 0;
            totalDamageTaken = 0;
            weakPointsHit = 0;

            // Reset weak points
            foreach (var wp in boss.weakPoints)
            {
                wp.Reset();
            }

            OnBossFightStarted?.Invoke(boss);
        }

        /// <summary>
        /// Ends the fight due to player death.
        /// </summary>
        public void FailBossFight()
        {
            if (!isFightActive) return;

            playerDeathCount++;
            isFightActive = false;
            OnBossFightFailed?.Invoke(currentBoss);
        }

        /// <summary>
        /// Retries the boss fight.
        /// </summary>
        public void RetryBossFight()
        {
            if (currentBoss != null)
            {
                int deaths = playerDeathCount;
                StartBossFight(currentBoss);
                playerDeathCount = deaths;
            }
        }

        #endregion

        #region Damage & Combat

        /// <summary>
        /// Deals damage to the boss. Returns actual damage dealt.
        /// </summary>
        public float DealDamageToBoss(float damage, CreatureElement element = CreatureElement.Fire, string weakPointId = null)
        {
            if (!isFightActive || isInvulnerable || currentBoss == null) return 0;

            float actualDamage = damage;

            // Elemental modifiers
            if (currentBoss.weaknesses.Contains(element))
            {
                actualDamage *= 1.5f;
            }
            else if (currentBoss.resistances.Contains(element))
            {
                actualDamage *= 0.5f;
            }
            else if (currentBoss.immunities.Contains(element))
            {
                actualDamage = 0;
                return 0;
            }

            // Defense reduction
            float defense = currentBoss.defense;
            actualDamage = Mathf.Max(1, actualDamage - (defense * 0.5f));

            // Weak point multiplier
            if (!string.IsNullOrEmpty(weakPointId))
            {
                var weakPoint = currentBoss.weakPoints.FirstOrDefault(wp => wp.weakPointId == weakPointId);
                if (weakPoint != null && weakPoint.isExposed && !weakPoint.isBroken)
                {
                    actualDamage *= weakPoint.damageMultiplier;
                    weakPoint.Hit();
                    weakPointsHit++;
                    OnWeakPointHit?.Invoke(weakPoint);

                    if (weakPoint.isBroken)
                    {
                        OnWeakPointBroken?.Invoke(weakPoint);
                        ApplyStagger(weakPoint.staggerDuration);
                    }
                }
            }

            // Stagger bonus
            if (isStaggered) actualDamage *= 1.5f;

            // Apply global multiplier
            float globalMult = config != null ? config.globalDamageMultiplier : 1f;

            currentHealth -= actualDamage * globalMult;
            totalDamageDealt += actualDamage * globalMult;
            OnBossHealthChanged?.Invoke(CurrentHealthPercent);

            // Add stagger buildup
            staggerMeter += actualDamage * 0.1f;

            // Check phase transitions
            CheckPhaseTransitions();

            // Check defeat
            if (currentHealth <= 0)
            {
                DefeatBoss();
            }

            return actualDamage * globalMult;
        }

        /// <summary>
        /// Records damage dealt to the player by the boss.
        /// </summary>
        public void RecordPlayerDamage(float damage)
        {
            totalDamageTaken += damage;
        }

        #endregion

        #region Phase Transitions

        private void CheckPhaseTransitions()
        {
            if (currentBoss == null) return;

            float healthPercent = CurrentHealthPercent;

            foreach (var transition in currentBoss.phaseTransitions)
            {
                if (transition.fromPhase == currentPhase && healthPercent <= transition.healthThreshold)
                {
                    ExecutePhaseTransition(transition);
                    break;
                }
            }
        }

        private void ExecutePhaseTransition(PhaseTransition transition)
        {
            var previousPhase = currentPhase;
            currentPhase = transition.toPhase;

            if (transition.invulnerabilityDuration > 0)
            {
                isInvulnerable = true;
                invulnerabilityTimer = transition.invulnerabilityDuration;
            }

            OnPhaseTransition?.Invoke(previousPhase, transition.toPhase);
        }

        #endregion

        #region Stagger

        private void UpdateStagger()
        {
            if (isStaggered)
            {
                staggerTimer -= Time.deltaTime;
                if (staggerTimer <= 0)
                {
                    isStaggered = false;
                    staggerMeter = 0;
                    OnBossRecoveredFromStagger?.Invoke();
                }
            }
            else
            {
                float threshold = config != null ? config.staggerThreshold : 100f;
                if (staggerMeter >= threshold)
                {
                    float duration = config != null ? config.staggerDuration : 3f;
                    ApplyStagger(duration);
                }

                // Natural recovery
                float recovery = config != null ? config.staggerRecoveryRate : 5f;
                staggerMeter = Mathf.Max(0, staggerMeter - recovery * Time.deltaTime);
            }
        }

        private void ApplyStagger(float duration)
        {
            isStaggered = true;
            staggerTimer = duration;
            staggerMeter = 0;
            OnBossStaggered?.Invoke();
        }

        private void UpdateInvulnerability()
        {
            if (!isInvulnerable) return;

            invulnerabilityTimer -= Time.deltaTime;
            if (invulnerabilityTimer <= 0)
            {
                isInvulnerable = false;
            }
        }

        private void UpdateEnrageTimer()
        {
            fightTimer += Time.deltaTime;
            float enrageTime = config != null ? config.enrageTimerSeconds : 600f;

            if (fightTimer >= enrageTime && currentPhase != BossPhase.Enrage)
            {
                currentPhase = BossPhase.Enrage;
                OnBossEnraged?.Invoke();
            }
        }

        #endregion

        #region Boss Defeat

        private void DefeatBoss()
        {
            isFightActive = false;
            currentPhase = BossPhase.Defeated;
            currentHealth = 0;

            if (currentBoss != null)
            {
                var rewards = CalculateRewards();
                if (!defeatedBossIds.Contains(currentBoss.bossId))
                {
                    defeatedBossIds.Add(currentBoss.bossId);
                }
                OnBossDefeated?.Invoke(currentBoss, rewards);
            }
        }

        private BossRewards CalculateRewards()
        {
            if (currentBoss == null) return new BossRewards();

            var baseRewards = currentBoss.rewards;
            var finalRewards = new BossRewards
            {
                runeReward = baseRewards.runeReward,
                expReward = baseRewards.expReward,
                guaranteedItemIds = new List<string>(baseRewards.guaranteedItemIds),
                possibleItemIds = new List<string>(),
                unlockedCreatureId = baseRewards.unlockedCreatureId,
                unlockedRegionId = baseRewards.unlockedRegionId,
                unlockedRecipeId = baseRewards.unlockedRecipeId,
                achievementId = baseRewards.achievementId
            };

            // First kill bonus
            if (!defeatedBossIds.Contains(currentBoss.bossId) && config != null)
            {
                float bonus = config.firstKillBonusMultiplier;
                finalRewards.runeReward = Mathf.RoundToInt(finalRewards.runeReward * bonus);
                finalRewards.expReward = Mathf.RoundToInt(finalRewards.expReward * bonus);
            }

            // Roll for rare drops
            foreach (var itemId in baseRewards.possibleItemIds)
            {
                if (UnityEngine.Random.Range(0f, 1f) <= baseRewards.rareDropChance)
                {
                    finalRewards.possibleItemIds.Add(itemId);
                }
            }

            return finalRewards;
        }

        #endregion

        #region Queries

        /// <summary>
        /// Gets a list of available boss attacks for the current phase.
        /// </summary>
        public List<BossAttack> GetCurrentPhaseAttacks()
        {
            if (currentBoss == null) return new List<BossAttack>();
            return currentBoss.attacks.Where(a => a.availableInPhase <= currentPhase).ToList();
        }

        /// <summary>
        /// Gets all exposed, unbroken weak points.
        /// </summary>
        public List<BossWeakPoint> GetExposedWeakPoints()
        {
            if (currentBoss == null) return new List<BossWeakPoint>();
            return currentBoss.weakPoints.Where(wp => wp.isExposed && !wp.isBroken).ToList();
        }

        /// <summary>
        /// Checks if a specific boss has been defeated.
        /// </summary>
        public bool IsBossDefeated(string bossId)
        {
            return defeatedBossIds.Contains(bossId);
        }

        /// <summary>
        /// Gets all defeated boss IDs.
        /// </summary>
        public List<string> GetDefeatedBosses()
        {
            return new List<string>(defeatedBossIds);
        }

        /// <summary>
        /// Gets the number of times the player died to the current boss.
        /// </summary>
        public int GetDeathCount()
        {
            return playerDeathCount;
        }

        private float GetHealthMultiplier()
        {
            return config != null ? config.globalHealthMultiplier : 1f;
        }

        #endregion

        #region Save Data

        [Serializable]
        public class BossSaveData
        {
            public List<string> defeatedBossIds = new List<string>();
            public Dictionary<string, int> deathCounts = new Dictionary<string, int>();
        }

        public BossSaveData GetSaveData()
        {
            var data = new BossSaveData
            {
                defeatedBossIds = new List<string>(defeatedBossIds)
            };

            if (currentBoss != null)
            {
                data.deathCounts[currentBoss.bossId] = playerDeathCount;
            }

            return data;
        }

        public void LoadSaveData(BossSaveData data)
        {
            if (data == null) return;
            defeatedBossIds = new List<string>(data.defeatedBossIds);
        }

        #endregion
    }
}
