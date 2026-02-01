using System;
using System.Collections.Generic;
using UnityEngine;

namespace UsefulScripts.Player
{
    /// <summary>
    /// Runtime resource type for the player.
    /// </summary>
    public enum ResourceType
    {
        Health,
        Mana,
        Stamina,
        Rage,
        Energy,
        Focus,
        Shield
    }

    /// <summary>
    /// Represents a regenerating resource pool.
    /// </summary>
    [Serializable]
    public class ResourcePool
    {
        public ResourceType resourceType;
        public float currentValue;
        public float maxValue;
        public float regenRate;
        public float regenDelay;
        private float regenTimer;
        public bool canRegenerate = true;
        public bool isDepleted;

        public float Percent => maxValue > 0 ? currentValue / maxValue : 0f;
        public bool IsFull => currentValue >= maxValue;
        public bool IsEmpty => currentValue <= 0f;

        public event Action<float, float> OnValueChanged;
        public event Action OnDepleted;
        public event Action OnRefilled;

        public ResourcePool(ResourceType type, float max, float regen = 0f, float delay = 0f)
        {
            resourceType = type;
            maxValue = max;
            currentValue = max;
            regenRate = regen;
            regenDelay = delay;
            regenTimer = 0f;
            isDepleted = false;
        }

        public void Update(float deltaTime)
        {
            if (!canRegenerate || regenRate <= 0) return;

            if (regenTimer > 0)
            {
                regenTimer -= deltaTime;
                return;
            }

            if (currentValue < maxValue)
            {
                float previousValue = currentValue;
                currentValue = Mathf.Min(maxValue, currentValue + regenRate * deltaTime);

                if (currentValue != previousValue)
                {
                    OnValueChanged?.Invoke(currentValue, maxValue);
                }

                if (isDepleted && currentValue > 0)
                {
                    isDepleted = false;
                    OnRefilled?.Invoke();
                }
            }
        }

        public float Consume(float amount)
        {
            if (amount <= 0) return 0f;

            float consumed = Mathf.Min(currentValue, amount);
            currentValue -= consumed;
            regenTimer = regenDelay;

            OnValueChanged?.Invoke(currentValue, maxValue);

            if (currentValue <= 0 && !isDepleted)
            {
                isDepleted = true;
                OnDepleted?.Invoke();
            }

            return consumed;
        }

        public float Restore(float amount)
        {
            if (amount <= 0) return 0f;

            float previousValue = currentValue;
            float restored = Mathf.Min(amount, maxValue - currentValue);
            currentValue += restored;

            if (restored > 0)
            {
                OnValueChanged?.Invoke(currentValue, maxValue);

                if (isDepleted && currentValue > 0)
                {
                    isDepleted = false;
                    OnRefilled?.Invoke();
                }
            }

            return restored;
        }

        public void SetMax(float newMax, bool healToMax = false)
        {
            maxValue = Mathf.Max(1f, newMax);
            currentValue = Mathf.Min(currentValue, maxValue);

            if (healToMax)
            {
                currentValue = maxValue;
            }

            OnValueChanged?.Invoke(currentValue, maxValue);
        }

        public void RefillFull()
        {
            currentValue = maxValue;
            isDepleted = false;
            regenTimer = 0f;
            OnValueChanged?.Invoke(currentValue, maxValue);
            OnRefilled?.Invoke();
        }

        public void DepleteFull()
        {
            currentValue = 0f;
            isDepleted = true;
            OnValueChanged?.Invoke(currentValue, maxValue);
            OnDepleted?.Invoke();
        }

        public void ResetRegenDelay()
        {
            regenTimer = regenDelay;
        }
    }

    /// <summary>
    /// Tracks damage taken with source information.
    /// </summary>
    [Serializable]
    public class DamageRecord
    {
        public float damage;
        public ElementType element;
        public GameObject source;
        public float timestamp;
        public bool wasCritical;
        public bool wasBlocked;
        public bool wasEvaded;

        public DamageRecord(float dmg, ElementType elem, GameObject src, bool critical = false)
        {
            damage = dmg;
            element = elem;
            source = src;
            timestamp = Time.time;
            wasCritical = critical;
            wasBlocked = false;
            wasEvaded = false;
        }
    }

    /// <summary>
    /// Comprehensive player stat controller managing resources, damage, and stat integration.
    /// Works with CharacterSheet for attribute-based calculations.
    /// </summary>
    public class PlayerStatController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CharacterSheet characterSheet;

        [Header("Resource Pools")]
        [SerializeField] private float baseMaxHealth = 100f;
        [SerializeField] private float baseMaxMana = 50f;
        [SerializeField] private float baseMaxStamina = 100f;

        [Header("Regeneration Settings")]
        [SerializeField] private float baseHealthRegen = 1f;
        [SerializeField] private float baseManaRegen = 5f;
        [SerializeField] private float baseStaminaRegen = 10f;
        [SerializeField] private float healthRegenDelay = 5f;
        [SerializeField] private float manaRegenDelay = 2f;
        [SerializeField] private float staminaRegenDelay = 1f;

        [Header("Invincibility")]
        [SerializeField] private bool enableInvincibility = true;
        [SerializeField] private float invincibilityDuration = 1f;

        [Header("Combat Modifiers")]
        [SerializeField] private float damageMultiplier = 1f;
        [SerializeField] private float defenseMultiplier = 1f;
        [SerializeField] private float healingMultiplier = 1f;

        [Header("Death Settings")]
        [SerializeField] private bool canRevive = true;
        [SerializeField] private float reviveHealthPercent = 0.5f;

        // Resource pools
        private Dictionary<ResourceType, ResourcePool> resources = new Dictionary<ResourceType, ResourcePool>();

        // State
        private bool isInvincible;
        private float invincibilityTimer;
        private bool isDead;
        private List<DamageRecord> recentDamage = new List<DamageRecord>();
        private float damageRecordRetention = 10f;

        #region Events

        public event Action<float, float> OnHealthChanged;
        public event Action<float, float> OnManaChanged;
        public event Action<float, float> OnStaminaChanged;
        public event Action<float, DamageRecord> OnDamageTaken;
        public event Action<float> OnHealed;
        public event Action OnDeath;
        public event Action OnRevive;
        public event Action OnInvincibilityStart;
        public event Action OnInvincibilityEnd;
        public event Action<ResourceType> OnResourceDepleted;
        public event Action<ResourceType> OnResourceRefilled;

        #endregion

        #region Properties

        public float CurrentHealth => GetResource(ResourceType.Health)?.currentValue ?? 0f;
        public float MaxHealth => GetResource(ResourceType.Health)?.maxValue ?? 1f;
        public float HealthPercent => GetResource(ResourceType.Health)?.Percent ?? 0f;

        public float CurrentMana => GetResource(ResourceType.Mana)?.currentValue ?? 0f;
        public float MaxMana => GetResource(ResourceType.Mana)?.maxValue ?? 1f;
        public float ManaPercent => GetResource(ResourceType.Mana)?.Percent ?? 0f;

        public float CurrentStamina => GetResource(ResourceType.Stamina)?.currentValue ?? 0f;
        public float MaxStamina => GetResource(ResourceType.Stamina)?.maxValue ?? 1f;
        public float StaminaPercent => GetResource(ResourceType.Stamina)?.Percent ?? 0f;

        public bool IsDead => isDead;
        public bool IsInvincible => isInvincible;
        public bool IsAlive => !isDead;
        public bool IsHealthFull => GetResource(ResourceType.Health)?.IsFull ?? false;
        public bool IsManaFull => GetResource(ResourceType.Mana)?.IsFull ?? false;
        public bool IsStaminaFull => GetResource(ResourceType.Stamina)?.IsFull ?? false;

        public CharacterSheet CharacterSheet => characterSheet;
        public IReadOnlyList<DamageRecord> RecentDamage => recentDamage.AsReadOnly();

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (characterSheet == null)
            {
                characterSheet = GetComponent<CharacterSheet>();
            }

            InitializeResources();
        }

        private void Start()
        {
            SyncWithCharacterSheet();

            if (characterSheet != null)
            {
                characterSheet.OnStatsRecalculated += SyncWithCharacterSheet;
            }
        }

        private void Update()
        {
            if (isDead) return;

            UpdateInvincibility();
            UpdateResources();
            CleanupDamageRecords();
        }

        private void OnDestroy()
        {
            if (characterSheet != null)
            {
                characterSheet.OnStatsRecalculated -= SyncWithCharacterSheet;
            }
        }

        #endregion

        #region Initialization

        private void InitializeResources()
        {
            resources[ResourceType.Health] = new ResourcePool(ResourceType.Health, baseMaxHealth, baseHealthRegen, healthRegenDelay);
            resources[ResourceType.Mana] = new ResourcePool(ResourceType.Mana, baseMaxMana, baseManaRegen, manaRegenDelay);
            resources[ResourceType.Stamina] = new ResourcePool(ResourceType.Stamina, baseMaxStamina, baseStaminaRegen, staminaRegenDelay);
            resources[ResourceType.Shield] = new ResourcePool(ResourceType.Shield, 0f, 0f, 0f);

            // Subscribe to events
            resources[ResourceType.Health].OnValueChanged += (cur, max) => OnHealthChanged?.Invoke(cur, max);
            resources[ResourceType.Mana].OnValueChanged += (cur, max) => OnManaChanged?.Invoke(cur, max);
            resources[ResourceType.Stamina].OnValueChanged += (cur, max) => OnStaminaChanged?.Invoke(cur, max);

            resources[ResourceType.Health].OnDepleted += HandleHealthDepleted;

            foreach (var resource in resources.Values)
            {
                resource.OnDepleted += () => OnResourceDepleted?.Invoke(resource.resourceType);
                resource.OnRefilled += () => OnResourceRefilled?.Invoke(resource.resourceType);
            }
        }

        private void SyncWithCharacterSheet()
        {
            if (characterSheet == null) return;

            // Update max values from character sheet
            float maxHP = characterSheet.GetSecondaryStat(SecondaryStat.MaxHealth);
            float maxMP = characterSheet.GetSecondaryStat(SecondaryStat.MaxMana);
            float maxSP = characterSheet.GetSecondaryStat(SecondaryStat.MaxStamina);
            float hpRegen = characterSheet.GetSecondaryStat(SecondaryStat.HealthRegen);
            float mpRegen = characterSheet.GetSecondaryStat(SecondaryStat.ManaRegen);
            float spRegen = characterSheet.GetSecondaryStat(SecondaryStat.StaminaRegen);

            if (maxHP > 0) resources[ResourceType.Health].SetMax(maxHP);
            if (maxMP > 0) resources[ResourceType.Mana].SetMax(maxMP);
            if (maxSP > 0) resources[ResourceType.Stamina].SetMax(maxSP);

            resources[ResourceType.Health].regenRate = hpRegen > 0 ? hpRegen : baseHealthRegen;
            resources[ResourceType.Mana].regenRate = mpRegen > 0 ? mpRegen : baseManaRegen;
            resources[ResourceType.Stamina].regenRate = spRegen > 0 ? spRegen : baseStaminaRegen;
        }

        #endregion

        #region Resource Management

        /// <summary>
        /// Gets a resource pool by type.
        /// </summary>
        public ResourcePool GetResource(ResourceType type)
        {
            return resources.TryGetValue(type, out var pool) ? pool : null;
        }

        /// <summary>
        /// Adds a new resource type.
        /// </summary>
        public void AddResource(ResourceType type, float max, float regen = 0f, float delay = 0f)
        {
            if (!resources.ContainsKey(type))
            {
                resources[type] = new ResourcePool(type, max, regen, delay);
            }
        }

        /// <summary>
        /// Consumes a resource, returning the amount actually consumed.
        /// </summary>
        public float ConsumeResource(ResourceType type, float amount)
        {
            return GetResource(type)?.Consume(amount) ?? 0f;
        }

        /// <summary>
        /// Restores a resource, returning the amount actually restored.
        /// </summary>
        public float RestoreResource(ResourceType type, float amount)
        {
            return GetResource(type)?.Restore(amount) ?? 0f;
        }

        /// <summary>
        /// Checks if enough of a resource is available.
        /// </summary>
        public bool HasResource(ResourceType type, float amount)
        {
            var resource = GetResource(type);
            return resource != null && resource.currentValue >= amount;
        }

        private void UpdateResources()
        {
            float deltaTime = Time.deltaTime;
            foreach (var resource in resources.Values)
            {
                resource.Update(deltaTime);
            }
        }

        #endregion

        #region Damage System

        /// <summary>
        /// Takes damage with optional elemental type and source.
        /// </summary>
        public DamageRecord TakeDamage(float damage, ElementType element = ElementType.Physical, GameObject source = null, bool isCritical = false)
        {
            if (isDead || isInvincible || damage <= 0)
            {
                return null;
            }

            var record = new DamageRecord(damage, element, source, isCritical);

            // Apply elemental resistance
            float damageMultiplierFromResistance = 1f;
            if (characterSheet != null)
            {
                damageMultiplierFromResistance = characterSheet.GetElementalDamageMultiplier(element);
            }

            float finalDamage = damage * damageMultiplierFromResistance * damageMultiplier;

            // Check for absorption (negative multiplier means heal)
            if (finalDamage < 0)
            {
                Heal(-finalDamage);
                return record;
            }

            // Apply defense reduction
            float defense = characterSheet != null ? characterSheet.GetSecondaryStat(SecondaryStat.PhysicalDefense) : 0f;
            if (element != ElementType.Physical)
            {
                defense = characterSheet != null ? characterSheet.GetSecondaryStat(SecondaryStat.MagicalDefense) : 0f;
            }

            float defenseReduction = defense * defenseMultiplier / (defense * defenseMultiplier + 100f);
            finalDamage *= (1f - defenseReduction);

            // Check for evasion
            if (characterSheet != null)
            {
                float evasion = characterSheet.GetSecondaryStat(SecondaryStat.Evasion);
                if (UnityEngine.Random.value * 100f < evasion)
                {
                    record.wasEvaded = true;
                    return record;
                }
            }

            // Check for block
            if (characterSheet != null)
            {
                float blockChance = characterSheet.GetSecondaryStat(SecondaryStat.BlockChance);
                if (UnityEngine.Random.value * 100f < blockChance)
                {
                    finalDamage *= 0.5f; // Blocked damage is halved
                    record.wasBlocked = true;
                }
            }

            // Shield absorbs damage first
            var shield = GetResource(ResourceType.Shield);
            if (shield != null && shield.currentValue > 0)
            {
                float shieldAbsorb = Mathf.Min(shield.currentValue, finalDamage);
                shield.Consume(shieldAbsorb);
                finalDamage -= shieldAbsorb;
            }

            if (finalDamage > 0)
            {
                record.damage = finalDamage;
                var health = GetResource(ResourceType.Health);
                health?.Consume(finalDamage);

                recentDamage.Add(record);
                OnDamageTaken?.Invoke(finalDamage, record);

                if (health != null && health.IsEmpty)
                {
                    Die();
                }
                else if (enableInvincibility)
                {
                    StartInvincibility(invincibilityDuration);
                }
            }

            return record;
        }

        /// <summary>
        /// Takes true damage (ignores defense and resistance).
        /// </summary>
        public void TakeTrueDamage(float damage, GameObject source = null)
        {
            if (isDead || isInvincible || damage <= 0) return;

            var record = new DamageRecord(damage, ElementType.Physical, source);
            recentDamage.Add(record);

            var health = GetResource(ResourceType.Health);
            health?.Consume(damage);

            OnDamageTaken?.Invoke(damage, record);

            if (health != null && health.IsEmpty)
            {
                Die();
            }
            else if (enableInvincibility)
            {
                StartInvincibility(invincibilityDuration);
            }
        }

        /// <summary>
        /// Heals the player.
        /// </summary>
        public float Heal(float amount)
        {
            if (isDead || amount <= 0) return 0f;

            float actualHeal = amount * healingMultiplier;
            float healed = RestoreResource(ResourceType.Health, actualHeal);

            if (healed > 0)
            {
                OnHealed?.Invoke(healed);
            }

            return healed;
        }

        /// <summary>
        /// Heals the player by a percentage of max health.
        /// </summary>
        public float HealPercent(float percent)
        {
            return Heal(MaxHealth * Mathf.Clamp01(percent));
        }

        /// <summary>
        /// Adds a shield that absorbs damage.
        /// </summary>
        public void AddShield(float amount, float maxShield = -1f)
        {
            var shield = GetResource(ResourceType.Shield);
            if (shield == null) return;

            if (maxShield > 0)
            {
                shield.SetMax(maxShield);
            }
            else if (shield.maxValue < shield.currentValue + amount)
            {
                shield.SetMax(shield.currentValue + amount);
            }

            shield.Restore(amount);
        }

        /// <summary>
        /// Removes all shield.
        /// </summary>
        public void RemoveShield()
        {
            var shield = GetResource(ResourceType.Shield);
            shield?.DepleteFull();
        }

        #endregion

        #region Invincibility

        /// <summary>
        /// Starts invincibility for a duration.
        /// </summary>
        public void StartInvincibility(float duration)
        {
            isInvincible = true;
            invincibilityTimer = duration;
            OnInvincibilityStart?.Invoke();
        }

        /// <summary>
        /// Ends invincibility immediately.
        /// </summary>
        public void EndInvincibility()
        {
            if (isInvincible)
            {
                isInvincible = false;
                invincibilityTimer = 0f;
                OnInvincibilityEnd?.Invoke();
            }
        }

        private void UpdateInvincibility()
        {
            if (!isInvincible) return;

            invincibilityTimer -= Time.deltaTime;
            if (invincibilityTimer <= 0)
            {
                EndInvincibility();
            }
        }

        #endregion

        #region Death and Revival

        private void HandleHealthDepleted()
        {
            if (!isDead)
            {
                Die();
            }
        }

        /// <summary>
        /// Kills the player.
        /// </summary>
        public void Die()
        {
            if (isDead) return;

            isDead = true;
            OnDeath?.Invoke();
        }

        /// <summary>
        /// Revives the player with optional health percentage.
        /// </summary>
        public void Revive(float healthPercent = -1f)
        {
            if (!isDead || !canRevive) return;

            float reviveHP = healthPercent >= 0 ? healthPercent : reviveHealthPercent;
            isDead = false;

            var health = GetResource(ResourceType.Health);
            health?.Restore(health.maxValue * reviveHP);

            var mana = GetResource(ResourceType.Mana);
            mana?.RefillFull();

            var stamina = GetResource(ResourceType.Stamina);
            stamina?.RefillFull();

            OnRevive?.Invoke();
        }

        /// <summary>
        /// Instantly kills the player (bypasses invincibility).
        /// </summary>
        public void InstantKill()
        {
            isInvincible = false;
            var health = GetResource(ResourceType.Health);
            health?.DepleteFull();
            Die();
        }

        /// <summary>
        /// Fully restores all resources.
        /// </summary>
        public void FullRestore()
        {
            foreach (var resource in resources.Values)
            {
                resource.RefillFull();
            }
        }

        #endregion

        #region Mana and Stamina

        /// <summary>
        /// Consumes mana if available.
        /// </summary>
        public bool ConsumeMana(float amount)
        {
            if (!HasResource(ResourceType.Mana, amount)) return false;
            ConsumeResource(ResourceType.Mana, amount);
            return true;
        }

        /// <summary>
        /// Restores mana.
        /// </summary>
        public float RestoreMana(float amount)
        {
            return RestoreResource(ResourceType.Mana, amount);
        }

        /// <summary>
        /// Consumes stamina if available.
        /// </summary>
        public bool ConsumeStamina(float amount)
        {
            if (!HasResource(ResourceType.Stamina, amount)) return false;
            ConsumeResource(ResourceType.Stamina, amount);
            return true;
        }

        /// <summary>
        /// Restores stamina.
        /// </summary>
        public float RestoreStamina(float amount)
        {
            return RestoreResource(ResourceType.Stamina, amount);
        }

        #endregion

        #region Combat Modifiers

        /// <summary>
        /// Sets the damage multiplier.
        /// </summary>
        public void SetDamageMultiplier(float multiplier)
        {
            damageMultiplier = Mathf.Max(0f, multiplier);
        }

        /// <summary>
        /// Sets the defense multiplier.
        /// </summary>
        public void SetDefenseMultiplier(float multiplier)
        {
            defenseMultiplier = Mathf.Max(0f, multiplier);
        }

        /// <summary>
        /// Sets the healing multiplier.
        /// </summary>
        public void SetHealingMultiplier(float multiplier)
        {
            healingMultiplier = Mathf.Max(0f, multiplier);
        }

        /// <summary>
        /// Resets all combat modifiers to default.
        /// </summary>
        public void ResetCombatModifiers()
        {
            damageMultiplier = 1f;
            defenseMultiplier = 1f;
            healingMultiplier = 1f;
        }

        #endregion

        #region Damage Records

        private void CleanupDamageRecords()
        {
            float cutoff = Time.time - damageRecordRetention;
            recentDamage.RemoveAll(r => r.timestamp < cutoff);
        }

        /// <summary>
        /// Gets total damage taken in the last X seconds.
        /// </summary>
        public float GetRecentDamage(float seconds)
        {
            float cutoff = Time.time - seconds;
            float total = 0f;
            foreach (var record in recentDamage)
            {
                if (record.timestamp >= cutoff)
                {
                    total += record.damage;
                }
            }
            return total;
        }

        /// <summary>
        /// Gets damage taken from a specific source.
        /// </summary>
        public float GetDamageFromSource(GameObject source)
        {
            float total = 0f;
            foreach (var record in recentDamage)
            {
                if (record.source == source)
                {
                    total += record.damage;
                }
            }
            return total;
        }

        /// <summary>
        /// Clears all damage records.
        /// </summary>
        public void ClearDamageRecords()
        {
            recentDamage.Clear();
        }

        #endregion

        #region Utility

        /// <summary>
        /// Gets a status summary string.
        /// </summary>
        public string GetStatusSummary()
        {
            return $"HP: {CurrentHealth:F0}/{MaxHealth:F0} | " +
                   $"MP: {CurrentMana:F0}/{MaxMana:F0} | " +
                   $"SP: {CurrentStamina:F0}/{MaxStamina:F0}";
        }

        /// <summary>
        /// Enables or disables regeneration for a resource.
        /// </summary>
        public void SetRegeneration(ResourceType type, bool enabled)
        {
            var resource = GetResource(type);
            if (resource != null)
            {
                resource.canRegenerate = enabled;
            }
        }

        /// <summary>
        /// Sets the regeneration rate for a resource.
        /// </summary>
        public void SetRegenerationRate(ResourceType type, float rate)
        {
            var resource = GetResource(type);
            if (resource != null)
            {
                resource.regenRate = rate;
            }
        }

        #endregion
    }
}
