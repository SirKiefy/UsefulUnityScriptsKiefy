using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UsefulScripts.Abilities
{
    /// <summary>
    /// Represents an ability/skill definition.
    /// Create as ScriptableObject assets.
    /// </summary>
    [CreateAssetMenu(fileName = "NewAbility", menuName = "UsefulScripts/Abilities/Ability")]
    public class AbilityData : ScriptableObject
    {
        [Header("Basic Info")]
        public string abilityId;
        public string abilityName;
        [TextArea(3, 5)]
        public string description;
        public Sprite icon;
        
        [Header("Cooldown & Cost")]
        public float cooldown = 5f;
        public float manaCost = 10f;
        public float staminaCost = 0f;
        public float healthCost = 0f;
        
        [Header("Casting")]
        public float castTime = 0f;
        public bool canMoveWhileCasting = true;
        public bool canBeCancelled = true;
        
        [Header("Targeting")]
        public TargetType targetType = TargetType.Self;
        public float range = 10f;
        public float areaOfEffect = 0f;
        public LayerMask targetLayers;
        
        [Header("Effects")]
        public List<AbilityEffect> effects = new List<AbilityEffect>();
        
        [Header("Requirements")]
        public int requiredLevel = 1;
        public List<string> requiredAbilityIds = new List<string>();
        
        [Header("Audio & Visual")]
        public AudioClip castSound;
        public AudioClip hitSound;
        public GameObject castVFX;
        public GameObject hitVFX;
    }
    
    public enum TargetType
    {
        Self,
        SingleEnemy,
        SingleAlly,
        PointOnGround,
        Direction,
        AllEnemiesInRange,
        AllAlliesInRange,
        Projectile
    }
    
    [Serializable]
    public class AbilityEffect
    {
        public EffectType effectType;
        public float value;
        public float duration;
        public float tickInterval;
        public bool isPercentage;
    }
    
    public enum EffectType
    {
        // Damage
        PhysicalDamage,
        MagicDamage,
        TrueDamage,
        DamageOverTime,
        
        // Healing
        InstantHeal,
        HealOverTime,
        Shield,
        
        // Crowd Control
        Stun,
        Slow,
        Root,
        Silence,
        Knockback,
        Pull,
        Fear,
        Taunt,
        
        // Buffs
        IncreaseAttack,
        IncreaseDefense,
        IncreaseSpeed,
        IncreaseCritChance,
        Haste,
        
        // Debuffs
        DecreaseAttack,
        DecreaseDefense,
        DecreaseSpeed,
        DecreaseCritChance,
        Vulnerable,
        
        // Utility
        Teleport,
        Dash,
        Invisibility,
        Cleanse,
        Dispel
    }
    
    /// <summary>
    /// Represents an active status effect (buff/debuff) on an entity.
    /// </summary>
    [Serializable]
    public class StatusEffect
    {
        public string id;
        public string name;
        public Sprite icon;
        public EffectType effectType;
        public float value;
        public float duration;
        public float remainingDuration;
        public float tickInterval;
        public float tickTimer;
        public bool isDebuff;
        public bool isStackable;
        public int stacks;
        public int maxStacks;
        public GameObject source;
        
        public event Action<StatusEffect> OnTick;
        public event Action<StatusEffect> OnExpire;
        public event Action<StatusEffect> OnStackAdded;
        
        public StatusEffect(string effectId, string effectName, EffectType type, float val, float dur, bool debuff = false)
        {
            id = effectId;
            name = effectName;
            effectType = type;
            value = val;
            duration = dur;
            remainingDuration = dur;
            isDebuff = debuff;
            stacks = 1;
            maxStacks = 1;
        }
        
        public void Update(float deltaTime)
        {
            remainingDuration -= deltaTime;
            
            if (tickInterval > 0)
            {
                tickTimer += deltaTime;
                while (tickTimer >= tickInterval)
                {
                    tickTimer -= tickInterval;
                    OnTick?.Invoke(this);
                }
            }
            
            if (remainingDuration <= 0)
            {
                OnExpire?.Invoke(this);
            }
        }
        
        public void AddStack()
        {
            if (stacks < maxStacks)
            {
                stacks++;
                remainingDuration = duration; // Refresh duration
                OnStackAdded?.Invoke(this);
            }
        }
        
        public float GetTotalValue()
        {
            return value * stacks;
        }
        
        public bool IsExpired => remainingDuration <= 0;
    }
    
    /// <summary>
    /// Represents an ability currently on cooldown.
    /// </summary>
    public class AbilityCooldown
    {
        public AbilityData ability;
        public float remainingCooldown;
        public float totalCooldown;
        
        public float CooldownProgress => 1f - (remainingCooldown / totalCooldown);
        public bool IsReady => remainingCooldown <= 0;
        
        public AbilityCooldown(AbilityData abilityData)
        {
            ability = abilityData;
            totalCooldown = abilityData.cooldown;
            remainingCooldown = totalCooldown;
        }
        
        public void Update(float deltaTime, float cooldownReduction = 0f)
        {
            float reduction = 1f + cooldownReduction;
            remainingCooldown -= deltaTime * reduction;
            remainingCooldown = Mathf.Max(0, remainingCooldown);
        }
    }
    
    /// <summary>
    /// Represents an ability currently being cast.
    /// </summary>
    public class AbilityCast
    {
        public AbilityData ability;
        public float castProgress;
        public float castTime;
        public Vector3 targetPosition;
        public GameObject targetObject;
        public Vector3 direction;
        public bool isCancelled;
        
        public float Progress => castTime > 0 ? castProgress / castTime : 1f;
        public bool IsComplete => castProgress >= castTime;
        
        public AbilityCast(AbilityData abilityData)
        {
            ability = abilityData;
            castTime = abilityData.castTime;
            castProgress = 0f;
        }
        
        public void Update(float deltaTime)
        {
            if (!isCancelled)
            {
                castProgress += deltaTime;
            }
        }
        
        public void Cancel()
        {
            isCancelled = true;
        }
    }
    
    /// <summary>
    /// Complete ability system managing cooldowns, casting, and status effects.
    /// Attach to any entity that can use abilities.
    /// </summary>
    public class AbilitySystem : MonoBehaviour
    {
        [Header("Resource Pools")]
        [SerializeField] private float maxMana = 100f;
        [SerializeField] private float currentMana = 100f;
        [SerializeField] private float manaRegen = 5f;
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float currentStamina = 100f;
        [SerializeField] private float staminaRegen = 10f;
        
        [Header("Ability Modifiers")]
        [SerializeField] private float cooldownReduction = 0f;
        [SerializeField] private float castSpeedBonus = 0f;
        [SerializeField] private float abilityPower = 1f;
        
        [Header("Known Abilities")]
        [SerializeField] private List<AbilityData> knownAbilities = new List<AbilityData>();
        
        private Dictionary<string, AbilityCooldown> cooldowns = new Dictionary<string, AbilityCooldown>();
        private List<StatusEffect> activeEffects = new List<StatusEffect>();
        private AbilityCast currentCast;
        private int currentLevel = 1;
        
        // Events
        public event Action<AbilityData> OnAbilityUsed;
        public event Action<AbilityData, float> OnAbilityCastStart;
        public event Action<AbilityData> OnAbilityCastComplete;
        public event Action<AbilityData> OnAbilityCastCancelled;
        public event Action<StatusEffect> OnEffectApplied;
        public event Action<StatusEffect> OnEffectRemoved;
        public event Action<StatusEffect> OnEffectTick;
        public event Action<float, float> OnManaChanged;
        public event Action<float, float> OnStaminaChanged;
        
        // Properties
        public float Mana => currentMana;
        public float MaxMana => maxMana;
        public float ManaPercent => currentMana / maxMana;
        public float Stamina => currentStamina;
        public float MaxStamina => maxStamina;
        public float StaminaPercent => currentStamina / maxStamina;
        public bool IsCasting => currentCast != null && !currentCast.IsComplete && !currentCast.isCancelled;
        public AbilityCast CurrentCast => currentCast;
        public IReadOnlyList<StatusEffect> ActiveEffects => activeEffects.AsReadOnly();
        public IReadOnlyList<AbilityData> KnownAbilities => knownAbilities.AsReadOnly();
        
        private void Update()
        {
            float deltaTime = Time.deltaTime;
            
            UpdateResources(deltaTime);
            UpdateCooldowns(deltaTime);
            UpdateEffects(deltaTime);
            UpdateCasting(deltaTime);
        }
        
        private void UpdateResources(float deltaTime)
        {
            // Mana regeneration
            if (currentMana < maxMana)
            {
                float previousMana = currentMana;
                currentMana = Mathf.Min(maxMana, currentMana + manaRegen * deltaTime);
                if (currentMana != previousMana)
                {
                    OnManaChanged?.Invoke(currentMana, maxMana);
                }
            }
            
            // Stamina regeneration
            if (currentStamina < maxStamina)
            {
                float previousStamina = currentStamina;
                currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegen * deltaTime);
                if (currentStamina != previousStamina)
                {
                    OnStaminaChanged?.Invoke(currentStamina, maxStamina);
                }
            }
        }
        
        private void UpdateCooldowns(float deltaTime)
        {
            var expiredCooldowns = new List<string>();
            
            foreach (var kvp in cooldowns)
            {
                kvp.Value.Update(deltaTime, cooldownReduction);
                if (kvp.Value.IsReady)
                {
                    expiredCooldowns.Add(kvp.Key);
                }
            }
            
            foreach (var id in expiredCooldowns)
            {
                cooldowns.Remove(id);
            }
        }
        
        private void UpdateEffects(float deltaTime)
        {
            var expiredEffects = new List<StatusEffect>();
            
            foreach (var effect in activeEffects)
            {
                effect.Update(deltaTime);
                if (effect.IsExpired)
                {
                    expiredEffects.Add(effect);
                }
            }
            
            foreach (var effect in expiredEffects)
            {
                RemoveEffect(effect);
            }
        }
        
        private void UpdateCasting(float deltaTime)
        {
            if (currentCast == null) return;
            
            if (currentCast.isCancelled)
            {
                OnAbilityCastCancelled?.Invoke(currentCast.ability);
                currentCast = null;
                return;
            }
            
            float castSpeed = 1f + castSpeedBonus;
            currentCast.Update(deltaTime * castSpeed);
            
            if (currentCast.IsComplete)
            {
                ExecuteAbility(currentCast);
                OnAbilityCastComplete?.Invoke(currentCast.ability);
                currentCast = null;
            }
        }
        
        /// <summary>
        /// Attempts to use an ability. Returns true if successful.
        /// </summary>
        public bool UseAbility(AbilityData ability, Vector3 targetPosition = default, GameObject targetObject = null, Vector3 direction = default)
        {
            if (!CanUseAbility(ability)) return false;
            
            // Consume resources
            currentMana -= ability.manaCost;
            currentStamina -= ability.staminaCost;
            
            OnManaChanged?.Invoke(currentMana, maxMana);
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
            
            // Start cooldown
            cooldowns[ability.abilityId] = new AbilityCooldown(ability);
            
            // Handle cast time
            if (ability.castTime > 0)
            {
                currentCast = new AbilityCast(ability)
                {
                    targetPosition = targetPosition,
                    targetObject = targetObject,
                    direction = direction
                };
                
                OnAbilityCastStart?.Invoke(ability, ability.castTime);
                
                // Play cast sound/VFX
                if (ability.castSound != null)
                {
                    AudioSource.PlayClipAtPoint(ability.castSound, transform.position);
                }
                
                if (ability.castVFX != null)
                {
                    Instantiate(ability.castVFX, transform.position, Quaternion.identity);
                }
            }
            else
            {
                // Instant cast
                var cast = new AbilityCast(ability)
                {
                    targetPosition = targetPosition,
                    targetObject = targetObject,
                    direction = direction
                };
                ExecuteAbility(cast);
            }
            
            OnAbilityUsed?.Invoke(ability);
            return true;
        }
        
        /// <summary>
        /// Attempts to use an ability by its ID.
        /// </summary>
        public bool UseAbilityById(string abilityId, Vector3 targetPosition = default, GameObject targetObject = null, Vector3 direction = default)
        {
            var ability = knownAbilities.FirstOrDefault(a => a.abilityId == abilityId);
            if (ability == null) return false;
            return UseAbility(ability, targetPosition, targetObject, direction);
        }
        
        /// <summary>
        /// Checks if an ability can be used.
        /// </summary>
        public bool CanUseAbility(AbilityData ability)
        {
            if (ability == null) return false;
            if (!knownAbilities.Contains(ability)) return false;
            if (IsOnCooldown(ability.abilityId)) return false;
            if (currentMana < ability.manaCost) return false;
            if (currentStamina < ability.staminaCost) return false;
            if (IsCasting && !currentCast.ability.canBeCancelled) return false;
            if (ability.requiredLevel > currentLevel) return false;
            if (IsSilenced() && ability.effects.Any(e => IsSpellEffect(e.effectType))) return false;
            
            // Check required abilities
            foreach (var requiredId in ability.requiredAbilityIds)
            {
                if (!knownAbilities.Any(a => a.abilityId == requiredId))
                {
                    return false;
                }
            }
            
            return true;
        }
        
        private bool IsSpellEffect(EffectType type)
        {
            return type == EffectType.MagicDamage || type == EffectType.InstantHeal || 
                   type == EffectType.HealOverTime || type == EffectType.Shield;
        }
        
        private void ExecuteAbility(AbilityCast cast)
        {
            var ability = cast.ability;
            
            // Get targets based on target type
            var targets = GetTargets(ability, cast.targetPosition, cast.targetObject, cast.direction);
            
            // Apply effects to each target
            foreach (var target in targets)
            {
                ApplyAbilityEffects(ability, target);
            }
            
            // Play hit sound/VFX
            if (ability.hitSound != null && targets.Count > 0)
            {
                AudioSource.PlayClipAtPoint(ability.hitSound, cast.targetPosition != default ? cast.targetPosition : transform.position);
            }
            
            if (ability.hitVFX != null)
            {
                var hitPos = cast.targetPosition != default ? cast.targetPosition : 
                            (targets.Count > 0 ? targets[0].transform.position : transform.position);
                Instantiate(ability.hitVFX, hitPos, Quaternion.identity);
            }
        }
        
        private List<GameObject> GetTargets(AbilityData ability, Vector3 targetPos, GameObject targetObj, Vector3 direction)
        {
            var targets = new List<GameObject>();
            
            switch (ability.targetType)
            {
                case TargetType.Self:
                    targets.Add(gameObject);
                    break;
                    
                case TargetType.SingleEnemy:
                case TargetType.SingleAlly:
                    if (targetObj != null)
                    {
                        targets.Add(targetObj);
                    }
                    break;
                    
                case TargetType.PointOnGround:
                case TargetType.AllEnemiesInRange:
                case TargetType.AllAlliesInRange:
                    var pos = targetPos != default ? targetPos : transform.position;
                    var colliders = Physics.OverlapSphere(pos, ability.areaOfEffect > 0 ? ability.areaOfEffect : ability.range, ability.targetLayers);
                    targets.AddRange(colliders.Select(c => c.gameObject));
                    break;
                    
                case TargetType.Direction:
                    var dir = direction != default ? direction : transform.forward;
                    var hits = Physics.RaycastAll(transform.position, dir, ability.range, ability.targetLayers);
                    targets.AddRange(hits.Select(h => h.collider.gameObject));
                    break;
            }
            
            return targets;
        }
        
        private void ApplyAbilityEffects(AbilityData ability, GameObject target)
        {
            var targetAbilitySystem = target.GetComponent<AbilitySystem>();
            
            foreach (var effect in ability.effects)
            {
                float effectValue = effect.value * abilityPower;
                
                switch (effect.effectType)
                {
                    // Instant damage/healing would integrate with HealthSystem
                    case EffectType.PhysicalDamage:
                    case EffectType.MagicDamage:
                    case EffectType.TrueDamage:
                        Debug.Log($"Dealing {effectValue} {effect.effectType} damage to {target.name}");
                        break;
                        
                    case EffectType.InstantHeal:
                        Debug.Log($"Healing {effectValue} to {target.name}");
                        break;
                        
                    // Status effects applied to target's ability system
                    default:
                        if (targetAbilitySystem != null && effect.duration > 0)
                        {
                            var statusEffect = new StatusEffect(
                                $"{ability.abilityId}_{effect.effectType}",
                                $"{ability.abilityName} - {effect.effectType}",
                                effect.effectType,
                                effectValue,
                                effect.duration,
                                IsDebuffEffect(effect.effectType)
                            )
                            {
                                tickInterval = effect.tickInterval,
                                source = gameObject
                            };
                            
                            targetAbilitySystem.ApplyEffect(statusEffect);
                        }
                        break;
                }
            }
        }
        
        private bool IsDebuffEffect(EffectType type)
        {
            return type switch
            {
                EffectType.Stun or EffectType.Slow or EffectType.Root or EffectType.Silence or
                EffectType.Fear or EffectType.Taunt or EffectType.DecreaseAttack or
                EffectType.DecreaseDefense or EffectType.DecreaseSpeed or
                EffectType.DecreaseCritChance or EffectType.Vulnerable or EffectType.DamageOverTime => true,
                _ => false
            };
        }
        
        /// <summary>
        /// Applies a status effect to this entity.
        /// </summary>
        public void ApplyEffect(StatusEffect effect)
        {
            // Check for existing effect
            var existing = activeEffects.FirstOrDefault(e => e.id == effect.id);
            
            if (existing != null)
            {
                if (effect.isStackable)
                {
                    existing.AddStack();
                }
                else
                {
                    existing.remainingDuration = effect.duration; // Refresh duration
                }
            }
            else
            {
                effect.OnTick += HandleEffectTick;
                effect.OnExpire += HandleEffectExpire;
                activeEffects.Add(effect);
                OnEffectApplied?.Invoke(effect);
            }
        }
        
        /// <summary>
        /// Removes a status effect from this entity.
        /// </summary>
        public void RemoveEffect(StatusEffect effect)
        {
            if (activeEffects.Remove(effect))
            {
                effect.OnTick -= HandleEffectTick;
                effect.OnExpire -= HandleEffectExpire;
                OnEffectRemoved?.Invoke(effect);
            }
        }
        
        /// <summary>
        /// Removes all effects of a specific type.
        /// </summary>
        public void RemoveEffectsOfType(EffectType type)
        {
            var toRemove = activeEffects.Where(e => e.effectType == type).ToList();
            foreach (var effect in toRemove)
            {
                RemoveEffect(effect);
            }
        }
        
        /// <summary>
        /// Removes all debuffs.
        /// </summary>
        public void Cleanse()
        {
            var debuffs = activeEffects.Where(e => e.isDebuff).ToList();
            foreach (var debuff in debuffs)
            {
                RemoveEffect(debuff);
            }
        }
        
        /// <summary>
        /// Removes all buffs (dispel).
        /// </summary>
        public void Dispel()
        {
            var buffs = activeEffects.Where(e => !e.isDebuff).ToList();
            foreach (var buff in buffs)
            {
                RemoveEffect(buff);
            }
        }
        
        private void HandleEffectTick(StatusEffect effect)
        {
            OnEffectTick?.Invoke(effect);
            
            // Apply tick effect (e.g., damage over time)
            switch (effect.effectType)
            {
                case EffectType.DamageOverTime:
                    Debug.Log($"DoT tick: {effect.GetTotalValue()} damage");
                    break;
                case EffectType.HealOverTime:
                    Debug.Log($"HoT tick: {effect.GetTotalValue()} healing");
                    break;
            }
        }
        
        private void HandleEffectExpire(StatusEffect effect)
        {
            RemoveEffect(effect);
        }
        
        /// <summary>
        /// Cancels the current cast if possible.
        /// </summary>
        public bool CancelCast()
        {
            if (currentCast == null) return false;
            if (!currentCast.ability.canBeCancelled) return false;
            
            currentCast.Cancel();
            return true;
        }
        
        /// <summary>
        /// Checks if an ability is on cooldown.
        /// </summary>
        public bool IsOnCooldown(string abilityId)
        {
            return cooldowns.ContainsKey(abilityId) && !cooldowns[abilityId].IsReady;
        }
        
        /// <summary>
        /// Gets the remaining cooldown for an ability.
        /// </summary>
        public float GetCooldownRemaining(string abilityId)
        {
            return cooldowns.TryGetValue(abilityId, out var cd) ? cd.remainingCooldown : 0f;
        }
        
        /// <summary>
        /// Gets the cooldown progress (0-1) for an ability.
        /// </summary>
        public float GetCooldownProgress(string abilityId)
        {
            return cooldowns.TryGetValue(abilityId, out var cd) ? cd.CooldownProgress : 1f;
        }
        
        /// <summary>
        /// Learns a new ability.
        /// </summary>
        public bool LearnAbility(AbilityData ability)
        {
            if (ability == null) return false;
            if (knownAbilities.Contains(ability)) return false;
            if (ability.requiredLevel > currentLevel) return false;
            
            knownAbilities.Add(ability);
            return true;
        }
        
        /// <summary>
        /// Forgets an ability.
        /// </summary>
        public bool ForgetAbility(AbilityData ability)
        {
            return knownAbilities.Remove(ability);
        }
        
        /// <summary>
        /// Sets the current level of this entity.
        /// </summary>
        public void SetLevel(int level)
        {
            currentLevel = Mathf.Max(1, level);
        }
        
        /// <summary>
        /// Checks if this entity is stunned.
        /// </summary>
        public bool IsStunned()
        {
            return activeEffects.Any(e => e.effectType == EffectType.Stun);
        }
        
        /// <summary>
        /// Checks if this entity is silenced.
        /// </summary>
        public bool IsSilenced()
        {
            return activeEffects.Any(e => e.effectType == EffectType.Silence);
        }
        
        /// <summary>
        /// Checks if this entity is rooted.
        /// </summary>
        public bool IsRooted()
        {
            return activeEffects.Any(e => e.effectType == EffectType.Root);
        }
        
        /// <summary>
        /// Gets the total modifier for a specific effect type.
        /// </summary>
        public float GetEffectModifier(EffectType type)
        {
            return activeEffects.Where(e => e.effectType == type).Sum(e => e.GetTotalValue());
        }
        
        /// <summary>
        /// Gets the speed multiplier from slow/haste effects.
        /// </summary>
        public float GetSpeedMultiplier()
        {
            float slowAmount = GetEffectModifier(EffectType.Slow);
            float hasteAmount = GetEffectModifier(EffectType.Haste);
            return Mathf.Max(0.1f, 1f - slowAmount + hasteAmount);
        }
        
        /// <summary>
        /// Restores mana.
        /// </summary>
        public void RestoreMana(float amount)
        {
            currentMana = Mathf.Min(maxMana, currentMana + amount);
            OnManaChanged?.Invoke(currentMana, maxMana);
        }
        
        /// <summary>
        /// Consumes mana.
        /// </summary>
        public bool ConsumeMana(float amount)
        {
            if (currentMana < amount) return false;
            currentMana -= amount;
            OnManaChanged?.Invoke(currentMana, maxMana);
            return true;
        }
        
        /// <summary>
        /// Restores stamina.
        /// </summary>
        public void RestoreStamina(float amount)
        {
            currentStamina = Mathf.Min(maxStamina, currentStamina + amount);
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        }
        
        /// <summary>
        /// Resets all cooldowns.
        /// </summary>
        public void ResetAllCooldowns()
        {
            cooldowns.Clear();
        }
    }
}
