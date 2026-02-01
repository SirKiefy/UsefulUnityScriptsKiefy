using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UsefulScripts.RPG
{
    /// <summary>
    /// Defines the type of combat system being used.
    /// </summary>
    public enum CombatType
    {
        TurnBased,      // Classic JRPG turn-based
        ActiveTime,     // ATB (Active Time Battle)
        RealTime,       // Action RPG
        TacticalGrid    // Strategy RPG
    }

    /// <summary>
    /// Defines the type of damage.
    /// </summary>
    public enum DamageType
    {
        Physical,
        Magical,
        True,           // Ignores defense
        Fire,
        Ice,
        Lightning,
        Earth,
        Wind,
        Water,
        Light,
        Dark,
        Poison
    }

    /// <summary>
    /// Defines the attack result types.
    /// </summary>
    public enum AttackResult
    {
        Hit,
        CriticalHit,
        Miss,
        Blocked,
        Parried,
        Absorbed,
        Reflected,
        Immune
    }

    /// <summary>
    /// Represents a combat action that can be performed.
    /// </summary>
    [Serializable]
    public class CombatAction
    {
        public string actionId;
        public string actionName;
        public CombatActionType actionType;
        public DamageType damageType = DamageType.Physical;
        public float basePower = 1f;
        public float manaCost;
        public float staminaCost;
        public float castTime;
        public float cooldown;
        public float range = 1f;
        public int targetCount = 1;
        public bool targetsAllies;
        public bool targetsSelf;
        public List<CombatEffect> effects = new List<CombatEffect>();
        public AudioClip actionSound;
        public GameObject actionVFX;
    }

    public enum CombatActionType
    {
        Attack,
        Skill,
        Magic,
        Item,
        Defend,
        Flee,
        Summon,
        LimitBreak
    }

    /// <summary>
    /// Represents an effect applied during combat.
    /// </summary>
    [Serializable]
    public class CombatEffect
    {
        public CombatEffectType effectType;
        public float value;
        public float duration;
        public float chance = 100f;
    }

    public enum CombatEffectType
    {
        Damage,
        Heal,
        Stun,
        Poison,
        Burn,
        Freeze,
        Paralyze,
        Sleep,
        Confuse,
        Blind,
        Silence,
        BuffAttack,
        BuffDefense,
        BuffSpeed,
        BuffMagicAttack,
        BuffMagicDefense,
        DebuffAttack,
        DebuffDefense,
        DebuffSpeed,
        DebuffMagicAttack,
        DebuffMagicDefense,
        Regeneration,
        ManaRegen,
        Shield,
        Reflect,
        Counter,
        Absorb,
        InstantKill,
        Revive
    }

    /// <summary>
    /// Represents the result of a combat calculation.
    /// </summary>
    [Serializable]
    public class CombatResult
    {
        public ICombatant attacker;
        public ICombatant defender;
        public CombatAction action;
        public AttackResult result;
        public float damageDealt;
        public float healingDone;
        public bool wasBackstab;
        public bool wasCountered;
        public int comboCount;
        public float comboMultiplier;
        public List<CombatEffect> appliedEffects = new List<CombatEffect>();
        public string resultMessage;

        public bool WasSuccessful => result == AttackResult.Hit || result == AttackResult.CriticalHit;
    }

    /// <summary>
    /// Interface for any entity that can participate in combat.
    /// </summary>
    public interface ICombatant
    {
        string CombatantName { get; }
        int Level { get; }
        bool IsAlive { get; }
        float CurrentHealth { get; }
        float MaxHealth { get; }
        float CurrentMana { get; }
        float MaxMana { get; }
        
        // Combat stats
        float GetAttackPower();
        float GetDefensePower();
        float GetMagicAttack();
        float GetMagicDefense();
        float GetSpeed();
        float GetEvasion();
        float GetAccuracy();
        float GetCritChance();
        float GetCritDamage();
        float GetElementalResistance(DamageType element);
        
        // Combat actions
        void TakeDamage(float amount, DamageType damageType);
        void Heal(float amount);
        void ConsumeMana(float amount);
        void ConsumeStamina(float amount);
        void ApplyCombatEffect(CombatEffect effect);
        List<CombatAction> GetAvailableActions();
    }

    /// <summary>
    /// Represents a combatant in turn-based combat.
    /// </summary>
    [Serializable]
    public class TurnCombatant
    {
        public ICombatant combatant;
        public float turnGauge;
        public float turnThreshold = 100f;
        public int turnOrder;
        public bool hasActed;
        public bool isDefending;
        public int guardTurns;

        public bool IsReady => turnGauge >= turnThreshold;
        public float TurnProgress => turnGauge / turnThreshold;

        public void UpdateTurnGauge(float speedMultiplier = 1f)
        {
            if (!combatant.IsAlive) return;
            turnGauge += combatant.GetSpeed() * speedMultiplier;
        }

        public void ResetTurn()
        {
            turnGauge = 0;
            hasActed = false;
        }
    }

    /// <summary>
    /// Combo system for action-based combat.
    /// </summary>
    [Serializable]
    public class ComboSystem
    {
        public int currentComboCount;
        public int maxComboCount = 999;
        public float comboTimer;
        public float comboDuration = 2f;
        public float comboMultiplierPerHit = 0.1f;
        public float maxComboMultiplier = 5f;
        public List<string> comboSequence = new List<string>();

        public event Action<int> OnComboIncreased;
        public event Action<int> OnComboEnded;

        public float CurrentMultiplier => Mathf.Min(1f + currentComboCount * comboMultiplierPerHit, maxComboMultiplier);

        public void AddHit(string attackId = null)
        {
            if (currentComboCount < maxComboCount)
            {
                currentComboCount++;
                comboTimer = comboDuration;
                if (!string.IsNullOrEmpty(attackId))
                {
                    comboSequence.Add(attackId);
                }
                OnComboIncreased?.Invoke(currentComboCount);
            }
        }

        public void Update(float deltaTime)
        {
            if (currentComboCount > 0)
            {
                comboTimer -= deltaTime;
                if (comboTimer <= 0)
                {
                    EndCombo();
                }
            }
        }

        public void EndCombo()
        {
            if (currentComboCount > 0)
            {
                int finalCount = currentComboCount;
                currentComboCount = 0;
                comboSequence.Clear();
                OnComboEnded?.Invoke(finalCount);
            }
        }

        public void Reset()
        {
            currentComboCount = 0;
            comboTimer = 0;
            comboSequence.Clear();
        }
    }

    /// <summary>
    /// Elemental system for elemental interactions and weakness/resistance.
    /// </summary>
    public static class ElementalSystem
    {
        private static readonly Dictionary<DamageType, DamageType> Weaknesses = new Dictionary<DamageType, DamageType>
        {
            { DamageType.Fire, DamageType.Ice },
            { DamageType.Ice, DamageType.Fire },
            { DamageType.Lightning, DamageType.Earth },
            { DamageType.Earth, DamageType.Wind },
            { DamageType.Wind, DamageType.Lightning },
            { DamageType.Water, DamageType.Lightning },
            { DamageType.Light, DamageType.Dark },
            { DamageType.Dark, DamageType.Light }
        };

        private static readonly Dictionary<DamageType, DamageType> Absorptions = new Dictionary<DamageType, DamageType>
        {
            { DamageType.Fire, DamageType.Fire },
            { DamageType.Ice, DamageType.Ice }
        };

        /// <summary>
        /// Gets the weakness multiplier for an element against a target element.
        /// </summary>
        public static float GetElementalMultiplier(DamageType attackElement, DamageType targetAffinity)
        {
            if (Weaknesses.TryGetValue(targetAffinity, out DamageType weakness) && weakness == attackElement)
            {
                return 2f; // Double damage on weakness
            }
            if (attackElement == targetAffinity)
            {
                return 0.5f; // Half damage on resistance
            }
            return 1f;
        }

        /// <summary>
        /// Checks if an element should be absorbed.
        /// </summary>
        public static bool ShouldAbsorb(DamageType attackElement, DamageType targetAbsorption)
        {
            return attackElement == targetAbsorption;
        }

        /// <summary>
        /// Gets the opposite element.
        /// </summary>
        public static DamageType GetOppositeElement(DamageType element)
        {
            return element switch
            {
                DamageType.Fire => DamageType.Ice,
                DamageType.Ice => DamageType.Fire,
                DamageType.Lightning => DamageType.Earth,
                DamageType.Earth => DamageType.Wind,
                DamageType.Wind => DamageType.Lightning,
                DamageType.Water => DamageType.Earth,
                DamageType.Light => DamageType.Dark,
                DamageType.Dark => DamageType.Light,
                _ => element
            };
        }

        /// <summary>
        /// Combines two elements for fusion attacks.
        /// </summary>
        public static DamageType CombineElements(DamageType element1, DamageType element2)
        {
            // Special combinations
            if ((element1 == DamageType.Fire && element2 == DamageType.Wind) ||
                (element1 == DamageType.Wind && element2 == DamageType.Fire))
            {
                return DamageType.Fire; // Fire + Wind = Stronger Fire
            }
            if ((element1 == DamageType.Water && element2 == DamageType.Lightning) ||
                (element1 == DamageType.Lightning && element2 == DamageType.Water))
            {
                return DamageType.Lightning; // Water + Lightning = Stronger Lightning
            }
            if ((element1 == DamageType.Ice && element2 == DamageType.Water) ||
                (element1 == DamageType.Water && element2 == DamageType.Ice))
            {
                return DamageType.Ice; // Ice + Water = Stronger Ice
            }

            // Default to first element
            return element1;
        }
    }

    /// <summary>
    /// Manages the Limit Break / Ultimate / Overdrive meter.
    /// </summary>
    [Serializable]
    public class LimitBreakSystem
    {
        public float currentGauge;
        public float maxGauge = 100f;
        public float gaugeGainOnDamageDealt = 5f;
        public float gaugeGainOnDamageTaken = 10f;
        public float gaugeGainOnKill = 25f;
        public float gaugeGainOnCritical = 15f;
        public float gaugeDecayRate = 0f;
        public int limitLevel = 1;
        public int maxLimitLevel = 3;

        public event Action OnLimitReady;
        public event Action<int> OnLimitLevelChanged;
        public event Action OnLimitUsed;

        public bool IsReady => currentGauge >= maxGauge;
        public float GaugePercent => currentGauge / maxGauge;

        public void AddGauge(float amount)
        {
            float previousGauge = currentGauge;
            currentGauge = Mathf.Min(currentGauge + amount, maxGauge * limitLevel);

            if (!IsReady && currentGauge >= maxGauge)
            {
                OnLimitReady?.Invoke();
            }
        }

        public void OnDealtDamage(float damageAmount, bool wasCritical)
        {
            AddGauge(gaugeGainOnDamageDealt);
            if (wasCritical)
            {
                AddGauge(gaugeGainOnCritical);
            }
        }

        public void OnTookDamage(float damageAmount, float maxHealth)
        {
            float percentDamage = damageAmount / maxHealth;
            AddGauge(gaugeGainOnDamageTaken * (1f + percentDamage));
        }

        public void OnKilledEnemy()
        {
            AddGauge(gaugeGainOnKill);
        }

        public bool UseLimit()
        {
            if (!IsReady) return false;

            currentGauge = 0;
            OnLimitUsed?.Invoke();
            return true;
        }

        public void IncreaseLimitLevel()
        {
            if (limitLevel < maxLimitLevel)
            {
                limitLevel++;
                OnLimitLevelChanged?.Invoke(limitLevel);
            }
        }

        public void Update(float deltaTime)
        {
            if (gaugeDecayRate > 0 && currentGauge > 0)
            {
                currentGauge = Mathf.Max(0, currentGauge - gaugeDecayRate * deltaTime);
            }
        }

        public void Reset()
        {
            currentGauge = 0;
            limitLevel = 1;
        }
    }

    /// <summary>
    /// Main combat system handling damage calculations, turn order, and combat flow.
    /// </summary>
    public class CombatSystem : MonoBehaviour
    {
        public static CombatSystem Instance { get; private set; }

        [Header("Combat Settings")]
        [SerializeField] private CombatType combatType = CombatType.TurnBased;
        [SerializeField] private float baseDamageMultiplier = 1f;
        [SerializeField] private float defenseReductionFactor = 0.5f;
        [SerializeField] private float criticalHitMultiplier = 1.5f;
        [SerializeField] private float backstabMultiplier = 1.5f;
        [SerializeField] private float defendDamageReduction = 0.5f;
        [SerializeField] private bool allowFleeing = true;
        [SerializeField] private float fleeSuccessBaseChance = 0.5f;

        [Header("Turn-Based Settings")]
        [SerializeField] private float turnSpeed = 1f;
        [SerializeField] private int maxTurnsPerRound = 10;

        [Header("Active Time Battle Settings")]
        [SerializeField] private float atbSpeed = 1f;
        [SerializeField] private bool pauseOnAction = true;

        [Header("Combo Settings")]
        [SerializeField] private bool enableComboSystem = true;

        // Combat state
        private List<TurnCombatant> combatants = new List<TurnCombatant>();
        private TurnCombatant currentTurnCombatant;
        private bool isCombatActive;
        private int currentRound = 0;
        private int currentTurnInRound = 0;
        private ComboSystem comboSystem = new ComboSystem();
        private System.Random random = new System.Random();

        // Events
        public event Action OnCombatStart;
        public event Action OnCombatEnd;
        public event Action<TurnCombatant> OnTurnStart;
        public event Action<TurnCombatant> OnTurnEnd;
        public event Action<int> OnRoundStart;
        public event Action<int> OnRoundEnd;
        public event Action<CombatResult> OnAttackPerformed;
        public event Action<ICombatant> OnCombatantDied;
        public event Action<ICombatant> OnCombatantFled;
        public event Action<bool> OnFleeAttempt;

        // Properties
        public CombatType CurrentCombatType => combatType;
        public bool IsCombatActive => isCombatActive;
        public int CurrentRound => currentRound;
        public TurnCombatant CurrentTurn => currentTurnCombatant;
        public IReadOnlyList<TurnCombatant> Combatants => combatants.AsReadOnly();
        public ComboSystem Combo => comboSystem;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (!isCombatActive) return;

            switch (combatType)
            {
                case CombatType.TurnBased:
                    UpdateTurnBased();
                    break;
                case CombatType.ActiveTime:
                    UpdateActiveTimeBattle(Time.deltaTime);
                    break;
                case CombatType.RealTime:
                    UpdateRealTime(Time.deltaTime);
                    break;
            }

            if (enableComboSystem)
            {
                comboSystem.Update(Time.deltaTime);
            }
        }

        #region Combat Initialization

        /// <summary>
        /// Starts a combat encounter with the specified combatants.
        /// </summary>
        public void StartCombat(List<ICombatant> allies, List<ICombatant> enemies)
        {
            if (isCombatActive) return;

            combatants.Clear();
            currentRound = 0;
            currentTurnInRound = 0;

            // Add allies
            foreach (var ally in allies.Where(a => a.IsAlive))
            {
                combatants.Add(new TurnCombatant { combatant = ally });
            }

            // Add enemies
            foreach (var enemy in enemies.Where(e => e.IsAlive))
            {
                combatants.Add(new TurnCombatant { combatant = enemy });
            }

            isCombatActive = true;
            DetermineTurnOrder();
            OnCombatStart?.Invoke();
            StartNextRound();
        }

        /// <summary>
        /// Ends the current combat.
        /// </summary>
        public void EndCombat()
        {
            if (!isCombatActive) return;

            isCombatActive = false;
            combatants.Clear();
            currentTurnCombatant = null;
            comboSystem.Reset();
            OnCombatEnd?.Invoke();
        }

        private void DetermineTurnOrder()
        {
            combatants = combatants.OrderByDescending(c => c.combatant.GetSpeed()).ToList();
            for (int i = 0; i < combatants.Count; i++)
            {
                combatants[i].turnOrder = i;
            }
        }

        private void StartNextRound()
        {
            currentRound++;
            currentTurnInRound = 0;

            // Reset turn states
            foreach (var combatant in combatants)
            {
                combatant.hasActed = false;
                if (combatant.guardTurns > 0)
                {
                    combatant.guardTurns--;
                    if (combatant.guardTurns <= 0)
                    {
                        combatant.isDefending = false;
                    }
                }
            }

            OnRoundStart?.Invoke(currentRound);

            if (combatType == CombatType.TurnBased)
            {
                StartNextTurn();
            }
        }

        #endregion

        #region Turn Management

        private void UpdateTurnBased()
        {
            // In pure turn-based, we wait for player input or AI decision
            // This is handled externally through ExecuteAction
        }

        private void UpdateActiveTimeBattle(float deltaTime)
        {
            foreach (var combatant in combatants.Where(c => c.combatant.IsAlive && !c.hasActed))
            {
                combatant.UpdateTurnGauge(atbSpeed * deltaTime);

                if (combatant.IsReady && currentTurnCombatant == null)
                {
                    StartTurn(combatant);
                }
            }
        }

        private void UpdateRealTime(float deltaTime)
        {
            // Real-time combat doesn't use turns in the same way
            // Combat actions are executed immediately when triggered
        }

        private void StartNextTurn()
        {
            if (!isCombatActive) return;

            var aliveCombatants = combatants.Where(c => c.combatant.IsAlive && !c.hasActed).ToList();
            
            if (aliveCombatants.Count == 0)
            {
                EndRound();
                return;
            }

            var nextCombatant = aliveCombatants.OrderByDescending(c => c.combatant.GetSpeed()).First();
            StartTurn(nextCombatant);
        }

        private void StartTurn(TurnCombatant combatant)
        {
            currentTurnCombatant = combatant;
            currentTurnInRound++;
            OnTurnStart?.Invoke(combatant);
        }

        /// <summary>
        /// Ends the current turn and moves to the next.
        /// </summary>
        public void EndCurrentTurn()
        {
            if (currentTurnCombatant == null) return;

            currentTurnCombatant.hasActed = true;
            currentTurnCombatant.ResetTurn();
            
            OnTurnEnd?.Invoke(currentTurnCombatant);
            currentTurnCombatant = null;

            CheckCombatEnd();

            if (isCombatActive && combatType == CombatType.TurnBased)
            {
                StartNextTurn();
            }
        }

        private void EndRound()
        {
            OnRoundEnd?.Invoke(currentRound);
            
            if (isCombatActive)
            {
                StartNextRound();
            }
        }

        private void CheckCombatEnd()
        {
            var allies = combatants.Where(c => !IsEnemy(c)).ToList();
            var enemies = combatants.Where(c => IsEnemy(c)).ToList();

            bool alliesDefeated = allies.All(c => !c.combatant.IsAlive);
            bool enemiesDefeated = enemies.All(c => !c.combatant.IsAlive);

            if (alliesDefeated || enemiesDefeated)
            {
                EndCombat();
            }
        }

        private bool IsEnemy(TurnCombatant combatant)
        {
            // This would typically check team/faction - simplified here
            return combatant.turnOrder >= combatants.Count / 2;
        }

        #endregion

        #region Combat Actions

        /// <summary>
        /// Executes a combat action from the current turn's combatant.
        /// </summary>
        public CombatResult ExecuteAction(CombatAction action, List<ICombatant> targets)
        {
            if (currentTurnCombatant == null && combatType != CombatType.RealTime)
            {
                Debug.LogWarning("No current turn combatant");
                return null;
            }

            var attacker = combatType == CombatType.RealTime ? 
                           targets.FirstOrDefault() : 
                           currentTurnCombatant.combatant;

            if (attacker == null) return null;

            // Consume resources
            attacker.ConsumeMana(action.manaCost);
            attacker.ConsumeStamina(action.staminaCost);

            CombatResult result = null;

            switch (action.actionType)
            {
                case CombatActionType.Attack:
                case CombatActionType.Skill:
                case CombatActionType.Magic:
                    foreach (var target in targets.Take(action.targetCount))
                    {
                        result = CalculateAttack(attacker, target, action);
                        ApplyDamage(result);
                        OnAttackPerformed?.Invoke(result);

                        if (!target.IsAlive)
                        {
                            OnCombatantDied?.Invoke(target);
                        }
                    }
                    break;

                case CombatActionType.Defend:
                    if (currentTurnCombatant != null)
                    {
                        currentTurnCombatant.isDefending = true;
                        currentTurnCombatant.guardTurns = 1;
                    }
                    result = new CombatResult
                    {
                        attacker = attacker,
                        action = action,
                        result = AttackResult.Hit,
                        resultMessage = $"{attacker.CombatantName} is defending!"
                    };
                    break;

                case CombatActionType.Item:
                    // Item usage would be handled by inventory system
                    result = new CombatResult
                    {
                        attacker = attacker,
                        action = action,
                        result = AttackResult.Hit,
                        resultMessage = $"{attacker.CombatantName} used an item!"
                    };
                    break;

                case CombatActionType.Flee:
                    bool success = TryFlee(attacker);
                    OnFleeAttempt?.Invoke(success);
                    if (success)
                    {
                        OnCombatantFled?.Invoke(attacker);
                    }
                    result = new CombatResult
                    {
                        attacker = attacker,
                        action = action,
                        result = success ? AttackResult.Hit : AttackResult.Miss,
                        resultMessage = success ? 
                            $"{attacker.CombatantName} fled!" : 
                            $"{attacker.CombatantName} couldn't escape!"
                    };
                    break;
            }

            if (combatType != CombatType.RealTime)
            {
                EndCurrentTurn();
            }

            return result;
        }

        /// <summary>
        /// Performs a real-time attack (for action RPG).
        /// </summary>
        public CombatResult PerformRealTimeAttack(ICombatant attacker, ICombatant target, CombatAction action)
        {
            var result = CalculateAttack(attacker, target, action);
            ApplyDamage(result);

            if (enableComboSystem && result.WasSuccessful)
            {
                comboSystem.AddHit(action.actionId);
                result.comboCount = comboSystem.currentComboCount;
                result.comboMultiplier = comboSystem.CurrentMultiplier;
            }

            OnAttackPerformed?.Invoke(result);

            if (!target.IsAlive)
            {
                OnCombatantDied?.Invoke(target);
            }

            return result;
        }

        #endregion

        #region Damage Calculation

        /// <summary>
        /// Calculates the result of an attack.
        /// </summary>
        public CombatResult CalculateAttack(ICombatant attacker, ICombatant defender, CombatAction action)
        {
            var result = new CombatResult
            {
                attacker = attacker,
                defender = defender,
                action = action
            };

            // Check for hit/miss
            float hitChance = CalculateHitChance(attacker, defender);
            if (random.NextDouble() * 100 > hitChance)
            {
                result.result = AttackResult.Miss;
                result.resultMessage = $"{attacker.CombatantName}'s attack missed!";
                return result;
            }

            // Check for block/parry (if defender is defending)
            var defenderTurn = combatants.FirstOrDefault(c => c.combatant == defender);
            if (defenderTurn != null && defenderTurn.isDefending)
            {
                result.result = AttackResult.Blocked;
                result.damageDealt = CalculateBaseDamage(attacker, defender, action) * (1f - defendDamageReduction);
                result.resultMessage = $"{defender.CombatantName} blocked the attack!";
                return result;
            }

            // Check for critical hit
            float critChance = attacker.GetCritChance();
            bool isCritical = random.NextDouble() * 100 < critChance;

            // Calculate base damage
            float damage = CalculateBaseDamage(attacker, defender, action);

            // Apply critical multiplier
            if (isCritical)
            {
                float critMultiplier = criticalHitMultiplier * (1f + attacker.GetCritDamage() / 100f);
                damage *= critMultiplier;
                result.result = AttackResult.CriticalHit;
            }
            else
            {
                result.result = AttackResult.Hit;
            }

            // Apply combo multiplier
            if (enableComboSystem)
            {
                damage *= comboSystem.CurrentMultiplier;
            }

            // Apply elemental multipliers
            float elementalMultiplier = defender.GetElementalResistance(action.damageType);
            damage *= elementalMultiplier;

            // Check for absorption
            if (elementalMultiplier < 0)
            {
                result.result = AttackResult.Absorbed;
                result.healingDone = -damage;
                result.damageDealt = 0;
            }
            else
            {
                result.damageDealt = Mathf.Max(1, damage);
            }

            // Apply effects
            foreach (var effect in action.effects)
            {
                if (random.NextDouble() * 100 < effect.chance)
                {
                    result.appliedEffects.Add(effect);
                }
            }

            // Build result message
            if (result.result == AttackResult.CriticalHit)
            {
                result.resultMessage = $"Critical! {attacker.CombatantName} deals {result.damageDealt:F0} damage to {defender.CombatantName}!";
            }
            else if (result.result == AttackResult.Absorbed)
            {
                result.resultMessage = $"{defender.CombatantName} absorbed the attack and healed {result.healingDone:F0} HP!";
            }
            else
            {
                result.resultMessage = $"{attacker.CombatantName} deals {result.damageDealt:F0} damage to {defender.CombatantName}!";
            }

            return result;
        }

        private float CalculateBaseDamage(ICombatant attacker, ICombatant defender, CombatAction action)
        {
            float attack;
            float defense;

            if (action.damageType == DamageType.Physical)
            {
                attack = attacker.GetAttackPower();
                defense = defender.GetDefensePower();
            }
            else if (action.damageType == DamageType.Magical || 
                     action.damageType >= DamageType.Fire)
            {
                attack = attacker.GetMagicAttack();
                defense = defender.GetMagicDefense();
            }
            else // True damage
            {
                attack = attacker.GetAttackPower();
                defense = 0;
            }

            // Base formula: (Attack * Power - Defense * Factor) * Multiplier
            float rawDamage = (attack * action.basePower) - (defense * defenseReductionFactor);
            rawDamage = Mathf.Max(1, rawDamage) * baseDamageMultiplier;

            // Add level scaling
            int levelDiff = attacker.Level - defender.Level;
            float levelMultiplier = 1f + (levelDiff * 0.02f);
            rawDamage *= Mathf.Clamp(levelMultiplier, 0.5f, 2f);

            return rawDamage;
        }

        private float CalculateHitChance(ICombatant attacker, ICombatant defender)
        {
            float accuracy = attacker.GetAccuracy();
            float evasion = defender.GetEvasion();
            return Mathf.Clamp(accuracy - evasion, 5f, 100f);
        }

        private void ApplyDamage(CombatResult result)
        {
            if (result.damageDealt > 0)
            {
                result.defender.TakeDamage(result.damageDealt, result.action.damageType);
            }
            else if (result.healingDone > 0)
            {
                result.defender.Heal(result.healingDone);
            }

            foreach (var effect in result.appliedEffects)
            {
                result.defender.ApplyCombatEffect(effect);
            }
        }

        #endregion

        #region Special Actions

        /// <summary>
        /// Attempts to flee from combat.
        /// </summary>
        public bool TryFlee(ICombatant combatant)
        {
            if (!allowFleeing) return false;

            // Calculate flee chance based on speed
            float avgEnemySpeed = combatants
                .Where(c => IsEnemy(combatants.First(tc => tc.combatant == c)) != IsEnemy(combatants.First(tc => tc.combatant == combatant)))
                .Average(c => c.combatant.GetSpeed());

            float speedRatio = combatant.GetSpeed() / avgEnemySpeed;
            float fleeChance = fleeSuccessBaseChance * speedRatio;

            return random.NextDouble() < fleeChance;
        }

        /// <summary>
        /// Performs a counter-attack.
        /// </summary>
        public CombatResult PerformCounterAttack(ICombatant counter, ICombatant target, CombatAction counterAction)
        {
            var result = CalculateAttack(counter, target, counterAction);
            result.wasCountered = true;
            ApplyDamage(result);
            OnAttackPerformed?.Invoke(result);
            return result;
        }

        /// <summary>
        /// Performs a backstab attack (bonus damage from behind).
        /// </summary>
        public CombatResult PerformBackstab(ICombatant attacker, ICombatant target, CombatAction action)
        {
            var result = CalculateAttack(attacker, target, action);
            if (result.WasSuccessful)
            {
                result.damageDealt *= backstabMultiplier;
                result.wasBackstab = true;
                result.resultMessage = $"Backstab! " + result.resultMessage;
            }
            ApplyDamage(result);
            OnAttackPerformed?.Invoke(result);
            return result;
        }

        #endregion

        #region Utility

        /// <summary>
        /// Gets all alive combatants on a specific side.
        /// </summary>
        public List<ICombatant> GetAliveCombatants(bool enemies)
        {
            return combatants
                .Where(c => c.combatant.IsAlive && IsEnemy(c) == enemies)
                .Select(c => c.combatant)
                .ToList();
        }

        /// <summary>
        /// Gets the turn order display.
        /// </summary>
        public List<ICombatant> GetTurnOrder(int count = 5)
        {
            if (combatType == CombatType.ActiveTime)
            {
                // Predict upcoming turns based on ATB gauge
                return combatants
                    .Where(c => c.combatant.IsAlive)
                    .OrderByDescending(c => c.turnGauge + c.combatant.GetSpeed())
                    .Take(count)
                    .Select(c => c.combatant)
                    .ToList();
            }
            else
            {
                return combatants
                    .Where(c => c.combatant.IsAlive && !c.hasActed)
                    .OrderByDescending(c => c.combatant.GetSpeed())
                    .Take(count)
                    .Select(c => c.combatant)
                    .ToList();
            }
        }

        /// <summary>
        /// Checks if combat should auto-resolve (all enemies can be defeated easily).
        /// </summary>
        public bool CanAutoResolve()
        {
            var allies = GetAliveCombatants(false);
            var enemies = GetAliveCombatants(true);

            float allyPower = allies.Sum(a => a.GetAttackPower() + a.GetMagicAttack());
            float enemyPower = enemies.Sum(e => e.GetAttackPower() + e.GetMagicAttack());
            float enemyHealth = enemies.Sum(e => e.CurrentHealth);

            return allyPower > enemyPower * 3 && allyPower > enemyHealth * 2;
        }

        /// <summary>
        /// Auto-resolves combat (skip battle animation).
        /// </summary>
        public void AutoResolveCombat()
        {
            if (!CanAutoResolve()) return;

            var enemies = GetAliveCombatants(true);
            foreach (var enemy in enemies)
            {
                enemy.TakeDamage(enemy.MaxHealth, DamageType.True);
                OnCombatantDied?.Invoke(enemy);
            }

            EndCombat();
        }

        #endregion
    }
}
