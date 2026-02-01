using UnityEngine;

namespace UsefulScripts.SpaceShip
{
    /// <summary>
    /// Health system specifically designed for spaceships.
    /// Handles hull integrity, shields, armor, and destruction.
    /// Inspired by Elite Dangerous, Star Citizen, and 4X strategy games.
    /// </summary>
    public class ShipHealthSystem : MonoBehaviour
    {
        [Header("Hull")]
        [SerializeField] private float maxHull = 1000f;
        [SerializeField] private float currentHull = 1000f;

        [Header("Armor")]
        [SerializeField] private float maxArmor = 500f;
        [SerializeField] private float currentArmor = 500f;
        [SerializeField] private float armorDamageReduction = 0.3f;
        [SerializeField] private bool armorRegenerates = false;
        [SerializeField] private float armorRegenRate = 1f;

        [Header("Shields")]
        [SerializeField] private float maxShield = 500f;
        [SerializeField] private float currentShield = 500f;
        [SerializeField] private float shieldRegenRate = 10f;
        [SerializeField] private float shieldRegenDelay = 5f;
        [SerializeField] private bool shieldBroken = false;
        [SerializeField] private float shieldRebootTime = 10f;

        [Header("Shield Types")]
        [SerializeField] private ShieldType shieldType = ShieldType.BiWeave;
        [SerializeField] private float shieldResistanceKinetic = 0f;
        [SerializeField] private float shieldResistanceThermal = 0f;
        [SerializeField] private float shieldResistanceExplosive = 0f;

        [Header("Critical Damage")]
        [SerializeField] private float criticalHullThreshold = 0.2f;
        [SerializeField] private bool enableCriticalEffects = true;

        [Header("Destruction")]
        [SerializeField] private bool isDestroyed = false;
        [SerializeField] private float destroyDelay = 2f;
        [SerializeField] private GameObject explosionPrefab;
        [SerializeField] private GameObject wreckagePrefab;

        // State
        private float shieldRegenTimer;
        private float shieldRebootTimer;
        private float lastDamageTime;
        private bool isCritical;

        // References
        private ShipSubsystems subsystems;

        // Events
        public event System.Action<float, float> OnHullChanged; // current, max
        public event System.Action<float, float> OnArmorChanged; // current, max
        public event System.Action<float, float> OnShieldChanged; // current, max
        public event System.Action OnShieldDepleted;
        public event System.Action OnShieldRestored;
        public event System.Action OnShieldRebooting;
        public event System.Action<float> OnDamageTaken;
        public event System.Action OnCriticalDamage;
        public event System.Action OnDestruction;
        public event System.Action OnRepaired;

        // Properties
        public float MaxHull => maxHull;
        public float CurrentHull => currentHull;
        public float HullPercent => currentHull / maxHull;
        public float MaxArmor => maxArmor;
        public float CurrentArmor => currentArmor;
        public float ArmorPercent => maxArmor > 0 ? currentArmor / maxArmor : 0f;
        public float MaxShield => maxShield;
        public float CurrentShield => currentShield;
        public float ShieldPercent => maxShield > 0 ? currentShield / maxShield : 0f;
        public bool HasShield => currentShield > 0;
        public bool ShieldBroken => shieldBroken;
        public bool IsCritical => isCritical;
        public bool IsDestroyed => isDestroyed;
        public float TotalHealth => currentHull + currentArmor + currentShield;
        public float TotalMaxHealth => maxHull + maxArmor + maxShield;
        public float TotalHealthPercent => TotalHealth / TotalMaxHealth;
        public ShieldType CurrentShieldType => shieldType;

        private void Awake()
        {
            subsystems = GetComponent<ShipSubsystems>();
            ApplyShieldTypeModifiers();
        }

        private void Update()
        {
            UpdateShields();
            UpdateArmor();
            UpdateCriticalState();
        }

        #region Shield Management

        private void ApplyShieldTypeModifiers()
        {
            switch (shieldType)
            {
                case ShieldType.Standard:
                    // Balanced stats
                    break;
                case ShieldType.BiWeave:
                    // Fast regen, lower capacity
                    shieldRegenRate *= 1.5f;
                    maxShield *= 0.8f;
                    break;
                case ShieldType.Prismatic:
                    // High capacity, slow regen
                    maxShield *= 1.4f;
                    shieldRegenRate *= 0.6f;
                    break;
                case ShieldType.Reinforced:
                    // Higher resistance, slower regen
                    shieldResistanceKinetic += 0.1f;
                    shieldResistanceThermal += 0.1f;
                    shieldResistanceExplosive += 0.1f;
                    shieldRegenRate *= 0.8f;
                    break;
            }

            currentShield = Mathf.Min(currentShield, maxShield);
        }

        private void UpdateShields()
        {
            if (maxShield <= 0) return;

            // Handle shield rebooting
            if (shieldBroken)
            {
                shieldRebootTimer -= Time.deltaTime;
                if (shieldRebootTimer <= 0)
                {
                    shieldBroken = false;
                    currentShield = maxShield * 0.5f; // Restore 50% on reboot
                    OnShieldRestored?.Invoke();
                }
                return;
            }

            // Regenerate shields
            if (currentShield < maxShield && !shieldBroken)
            {
                shieldRegenTimer -= Time.deltaTime;
                if (shieldRegenTimer <= 0)
                {
                    float regenMultiplier = GetShieldRegenMultiplier();
                    currentShield += shieldRegenRate * regenMultiplier * Time.deltaTime;
                    currentShield = Mathf.Min(currentShield, maxShield);
                    OnShieldChanged?.Invoke(currentShield, maxShield);
                }
            }
        }

        private float GetShieldRegenMultiplier()
        {
            if (subsystems == null) return 1f;
            return subsystems.GetShieldEfficiency();
        }

        /// <summary>
        /// Damage shields directly
        /// </summary>
        public float DamageShield(float damage, DamageType damageType = DamageType.Kinetic)
        {
            if (shieldBroken || maxShield <= 0) return damage;

            // Apply resistance
            float resistance = GetShieldResistance(damageType);
            float actualDamage = damage * (1f - resistance);

            float shieldDamage = Mathf.Min(actualDamage, currentShield);
            currentShield -= shieldDamage;
            shieldRegenTimer = shieldRegenDelay;
            lastDamageTime = Time.time;

            OnShieldChanged?.Invoke(currentShield, maxShield);

            if (currentShield <= 0)
            {
                BreakShield();
            }

            return actualDamage - shieldDamage;
        }

        private float GetShieldResistance(DamageType type)
        {
            switch (type)
            {
                case DamageType.Kinetic:
                    return shieldResistanceKinetic;
                case DamageType.Thermal:
                    return shieldResistanceThermal;
                case DamageType.Explosive:
                    return shieldResistanceExplosive;
                default:
                    return 0f;
            }
        }

        private void BreakShield()
        {
            shieldBroken = true;
            currentShield = 0f;
            shieldRebootTimer = shieldRebootTime;
            OnShieldDepleted?.Invoke();
            OnShieldRebooting?.Invoke();
        }

        /// <summary>
        /// Boost shields (e.g., from shield cell bank)
        /// </summary>
        public void BoostShield(float amount)
        {
            if (shieldBroken) return;

            currentShield = Mathf.Min(currentShield + amount, maxShield);
            OnShieldChanged?.Invoke(currentShield, maxShield);
        }

        /// <summary>
        /// Force reboot shields
        /// </summary>
        public void RebootShields()
        {
            if (shieldBroken)
            {
                shieldRebootTimer = 0f;
            }
        }

        #endregion

        #region Armor Management

        private void UpdateArmor()
        {
            if (!armorRegenerates || maxArmor <= 0) return;

            if (currentArmor < maxArmor && Time.time - lastDamageTime > 10f)
            {
                currentArmor += armorRegenRate * Time.deltaTime;
                currentArmor = Mathf.Min(currentArmor, maxArmor);
                OnArmorChanged?.Invoke(currentArmor, maxArmor);
            }
        }

        /// <summary>
        /// Damage armor directly
        /// </summary>
        public float DamageArmor(float damage)
        {
            if (maxArmor <= 0) return damage;

            // Armor provides damage reduction
            float reducedDamage = damage * (1f - armorDamageReduction);
            float armorDamage = Mathf.Min(reducedDamage, currentArmor);
            currentArmor -= armorDamage;
            lastDamageTime = Time.time;

            OnArmorChanged?.Invoke(currentArmor, maxArmor);

            return reducedDamage - armorDamage;
        }

        /// <summary>
        /// Repair armor
        /// </summary>
        public void RepairArmor(float amount)
        {
            currentArmor = Mathf.Min(currentArmor + amount, maxArmor);
            OnArmorChanged?.Invoke(currentArmor, maxArmor);
        }

        #endregion

        #region Hull Management

        /// <summary>
        /// Damage hull directly
        /// </summary>
        public void DamageHull(float damage)
        {
            if (isDestroyed) return;

            currentHull -= damage;
            lastDamageTime = Time.time;

            OnDamageTaken?.Invoke(damage);
            OnHullChanged?.Invoke(currentHull, maxHull);

            if (currentHull <= 0)
            {
                Destroy();
            }
        }

        /// <summary>
        /// Repair hull
        /// </summary>
        public void RepairHull(float amount)
        {
            if (isDestroyed) return;

            currentHull = Mathf.Min(currentHull + amount, maxHull);
            OnHullChanged?.Invoke(currentHull, maxHull);

            if (currentHull > maxHull * criticalHullThreshold && isCritical)
            {
                isCritical = false;
            }
        }

        #endregion

        #region General Damage

        /// <summary>
        /// Take damage with full damage pipeline (shields -> armor -> hull)
        /// </summary>
        public void TakeDamage(float damage, DamageType damageType = DamageType.Kinetic, Vector3 hitPoint = default, Transform attacker = null)
        {
            if (isDestroyed) return;

            float remainingDamage = damage;

            // Shields first
            remainingDamage = DamageShield(remainingDamage, damageType);

            // Then armor
            if (remainingDamage > 0)
            {
                remainingDamage = DamageArmor(remainingDamage);
            }

            // Finally hull
            if (remainingDamage > 0)
            {
                DamageHull(remainingDamage);
            }
        }

        /// <summary>
        /// Take absolute damage (bypasses shields and armor)
        /// </summary>
        public void TakeAbsoluteDamage(float damage)
        {
            DamageHull(damage);
        }

        #endregion

        #region Critical State

        private void UpdateCriticalState()
        {
            if (isDestroyed) return;

            bool wasCritical = isCritical;
            isCritical = currentHull <= maxHull * criticalHullThreshold;

            if (isCritical && !wasCritical && enableCriticalEffects)
            {
                OnCriticalDamage?.Invoke();
            }
        }

        #endregion

        #region Destruction

        private void Destroy()
        {
            if (isDestroyed) return;

            isDestroyed = true;
            currentHull = 0f;

            OnDestruction?.Invoke();

            // Spawn explosion
            if (explosionPrefab != null)
            {
                GameObject explosion = Instantiate(explosionPrefab, transform.position, transform.rotation);
                Destroy(explosion, 5f);
            }

            // Spawn wreckage
            if (wreckagePrefab != null)
            {
                Instantiate(wreckagePrefab, transform.position, transform.rotation);
            }

            // Destroy or disable ship
            Invoke(nameof(DestroyShip), destroyDelay);
        }

        private void DestroyShip()
        {
            Destroy(gameObject);
        }

        #endregion

        #region Repair and Restoration

        /// <summary>
        /// Full repair of all systems
        /// </summary>
        public void FullRepair()
        {
            currentHull = maxHull;
            currentArmor = maxArmor;
            currentShield = maxShield;
            shieldBroken = false;
            isCritical = false;
            isDestroyed = false;

            OnHullChanged?.Invoke(currentHull, maxHull);
            OnArmorChanged?.Invoke(currentArmor, maxArmor);
            OnShieldChanged?.Invoke(currentShield, maxShield);
            OnRepaired?.Invoke();
        }

        /// <summary>
        /// Partial repair
        /// </summary>
        public void Repair(float hullAmount, float armorAmount, float shieldAmount)
        {
            RepairHull(hullAmount);
            RepairArmor(armorAmount);
            BoostShield(shieldAmount);
            OnRepaired?.Invoke();
        }

        /// <summary>
        /// Revive a destroyed ship
        /// </summary>
        public void Revive(float healthPercent = 0.5f)
        {
            if (!isDestroyed) return;

            isDestroyed = false;
            currentHull = maxHull * healthPercent;
            currentArmor = maxArmor * healthPercent;
            currentShield = maxShield * healthPercent;
            shieldBroken = false;
            isCritical = currentHull <= maxHull * criticalHullThreshold;

            OnHullChanged?.Invoke(currentHull, maxHull);
            OnArmorChanged?.Invoke(currentArmor, maxArmor);
            OnShieldChanged?.Invoke(currentShield, maxShield);
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Set maximum hull value
        /// </summary>
        public void SetMaxHull(float max, bool healToMax = false)
        {
            maxHull = Mathf.Max(1f, max);
            if (healToMax)
            {
                currentHull = maxHull;
            }
            else
            {
                currentHull = Mathf.Min(currentHull, maxHull);
            }
            OnHullChanged?.Invoke(currentHull, maxHull);
        }

        /// <summary>
        /// Set maximum armor value
        /// </summary>
        public void SetMaxArmor(float max, bool healToMax = false)
        {
            maxArmor = Mathf.Max(0f, max);
            if (healToMax)
            {
                currentArmor = maxArmor;
            }
            else
            {
                currentArmor = Mathf.Min(currentArmor, maxArmor);
            }
            OnArmorChanged?.Invoke(currentArmor, maxArmor);
        }

        /// <summary>
        /// Set maximum shield value
        /// </summary>
        public void SetMaxShield(float max, bool healToMax = false)
        {
            maxShield = Mathf.Max(0f, max);
            if (healToMax)
            {
                currentShield = maxShield;
            }
            else
            {
                currentShield = Mathf.Min(currentShield, maxShield);
            }
            OnShieldChanged?.Invoke(currentShield, maxShield);
        }

        /// <summary>
        /// Set shield type
        /// </summary>
        public void SetShieldType(ShieldType type)
        {
            shieldType = type;
            ApplyShieldTypeModifiers();
        }

        /// <summary>
        /// Set shield resistances
        /// </summary>
        public void SetShieldResistances(float kinetic, float thermal, float explosive)
        {
            shieldResistanceKinetic = Mathf.Clamp01(kinetic);
            shieldResistanceThermal = Mathf.Clamp01(thermal);
            shieldResistanceExplosive = Mathf.Clamp01(explosive);
        }

        #endregion

        private void OnDrawGizmosSelected()
        {
            // Draw health bar above ship
            Vector3 barPos = transform.position + Vector3.up * 5f;
            float barWidth = 5f;
            float barHeight = 0.5f;

            // Hull bar (red/green)
            Gizmos.color = Color.Lerp(Color.red, Color.green, HullPercent);
            Gizmos.DrawCube(barPos, new Vector3(barWidth * HullPercent, barHeight, 0.1f));

            // Armor bar (orange)
            if (maxArmor > 0)
            {
                barPos += Vector3.up * barHeight * 1.5f;
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
                Gizmos.DrawCube(barPos, new Vector3(barWidth * ArmorPercent, barHeight, 0.1f));
            }

            // Shield bar (cyan)
            if (maxShield > 0)
            {
                barPos += Vector3.up * barHeight * 1.5f;
                Gizmos.color = shieldBroken ? new Color(0f, 0.5f, 1f, 0.3f) : new Color(0f, 0.5f, 1f, 0.8f);
                Gizmos.DrawCube(barPos, new Vector3(barWidth * ShieldPercent, barHeight, 0.1f));
            }
        }
    }

    /// <summary>
    /// Types of shield generators
    /// </summary>
    public enum ShieldType
    {
        /// <summary>
        /// Balanced shield type
        /// </summary>
        Standard,

        /// <summary>
        /// Fast regenerating shields with lower capacity
        /// </summary>
        BiWeave,

        /// <summary>
        /// High capacity shields with slow regeneration
        /// </summary>
        Prismatic,

        /// <summary>
        /// Shields with higher resistance but slower regen
        /// </summary>
        Reinforced
    }
}
