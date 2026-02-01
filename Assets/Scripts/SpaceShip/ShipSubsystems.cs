using UnityEngine;
using System.Collections.Generic;

namespace UsefulScripts.SpaceShip
{
    /// <summary>
    /// Ship subsystems management for spaceships.
    /// Handles power distribution, module health, and subsystem functionality.
    /// Inspired by Elite Dangerous, Star Citizen, and 4X strategy games.
    /// </summary>
    public class ShipSubsystems : MonoBehaviour
    {
        [Header("Power Generation")]
        [SerializeField] private float maxPowerOutput = 100f;
        [SerializeField] private float currentPowerOutput = 100f;
        [SerializeField] private float powerRegenRate = 20f;

        [Header("Power Distribution")]
        [SerializeField] [Range(0f, 1f)] private float weaponsPower = 0.33f;
        [SerializeField] [Range(0f, 1f)] private float shieldsPower = 0.33f;
        [SerializeField] [Range(0f, 1f)] private float enginesPower = 0.34f;
        [SerializeField] private float powerScalingFactor = 3f;

        [Header("Subsystem List")]
        [SerializeField] private List<ShipSubsystem> subsystems = new List<ShipSubsystem>();

        [Header("Subsystem Presets")]
        [SerializeField] private bool useDefaultSubsystems = true;

        // State
        private float totalPowerDraw;
        private bool isOverloaded;
        private Dictionary<SubsystemType, ShipSubsystem> subsystemLookup = new Dictionary<SubsystemType, ShipSubsystem>();

        // Events
        public event System.Action<SubsystemType, ShipSubsystem> OnSubsystemDamaged;
        public event System.Action<SubsystemType, ShipSubsystem> OnSubsystemDestroyed;
        public event System.Action<SubsystemType, ShipSubsystem> OnSubsystemRepaired;
        public event System.Action<float, float> OnPowerChanged; // current, max
        public event System.Action<float, float, float> OnPowerDistributionChanged; // weapons, shields, engines
        public event System.Action OnPowerOverload;
        public event System.Action OnPowerRestored;

        // Properties
        public float MaxPowerOutput => maxPowerOutput;
        public float CurrentPowerOutput => currentPowerOutput;
        public float PowerPercent => currentPowerOutput / maxPowerOutput;
        public float WeaponsPower => weaponsPower;
        public float ShieldsPower => shieldsPower;
        public float EnginesPower => enginesPower;
        public bool IsOverloaded => isOverloaded;
        public float TotalPowerDraw => totalPowerDraw;
        public List<ShipSubsystem> AllSubsystems => subsystems;

        private void Awake()
        {
            if (useDefaultSubsystems && subsystems.Count == 0)
            {
                InitializeDefaultSubsystems();
            }

            BuildSubsystemLookup();
        }

        private void Update()
        {
            UpdatePowerGeneration();
            UpdateSubsystems();
            UpdatePowerDraw();
        }

        #region Initialization

        private void InitializeDefaultSubsystems()
        {
            subsystems.Add(new ShipSubsystem
            {
                name = "Power Plant",
                type = SubsystemType.PowerPlant,
                maxHealth = 100f,
                currentHealth = 100f,
                powerDraw = 0f,
                isEssential = true,
                efficiency = 1f
            });

            subsystems.Add(new ShipSubsystem
            {
                name = "Shield Generator",
                type = SubsystemType.ShieldGenerator,
                maxHealth = 100f,
                currentHealth = 100f,
                powerDraw = 15f,
                isEssential = false,
                efficiency = 1f
            });

            subsystems.Add(new ShipSubsystem
            {
                name = "Engines",
                type = SubsystemType.Engines,
                maxHealth = 100f,
                currentHealth = 100f,
                powerDraw = 20f,
                isEssential = true,
                efficiency = 1f
            });

            subsystems.Add(new ShipSubsystem
            {
                name = "Thrusters",
                type = SubsystemType.Thrusters,
                maxHealth = 100f,
                currentHealth = 100f,
                powerDraw = 10f,
                isEssential = false,
                efficiency = 1f
            });

            subsystems.Add(new ShipSubsystem
            {
                name = "Weapon Systems",
                type = SubsystemType.Weapons,
                maxHealth = 100f,
                currentHealth = 100f,
                powerDraw = 25f,
                isEssential = false,
                efficiency = 1f
            });

            subsystems.Add(new ShipSubsystem
            {
                name = "Life Support",
                type = SubsystemType.LifeSupport,
                maxHealth = 100f,
                currentHealth = 100f,
                powerDraw = 5f,
                isEssential = true,
                efficiency = 1f
            });

            subsystems.Add(new ShipSubsystem
            {
                name = "Sensors",
                type = SubsystemType.Sensors,
                maxHealth = 100f,
                currentHealth = 100f,
                powerDraw = 8f,
                isEssential = false,
                efficiency = 1f
            });

            subsystems.Add(new ShipSubsystem
            {
                name = "FTL Drive",
                type = SubsystemType.FTLDrive,
                maxHealth = 100f,
                currentHealth = 100f,
                powerDraw = 30f,
                isEssential = false,
                efficiency = 1f
            });

            subsystems.Add(new ShipSubsystem
            {
                name = "Cargo Hatch",
                type = SubsystemType.CargoHatch,
                maxHealth = 50f,
                currentHealth = 50f,
                powerDraw = 2f,
                isEssential = false,
                efficiency = 1f
            });

            subsystems.Add(new ShipSubsystem
            {
                name = "Canopy",
                type = SubsystemType.Canopy,
                maxHealth = 30f,
                currentHealth = 30f,
                powerDraw = 0f,
                isEssential = false,
                efficiency = 1f
            });
        }

        private void BuildSubsystemLookup()
        {
            subsystemLookup.Clear();
            foreach (var subsystem in subsystems)
            {
                if (!subsystemLookup.ContainsKey(subsystem.type))
                {
                    subsystemLookup[subsystem.type] = subsystem;
                }
            }
        }

        #endregion

        #region Power Management

        private void UpdatePowerGeneration()
        {
            // Regenerate power
            if (currentPowerOutput < maxPowerOutput)
            {
                // Power generation depends on power plant health
                var powerPlant = GetSubsystem(SubsystemType.PowerPlant);
                float genMultiplier = powerPlant != null ? powerPlant.efficiency : 1f;
                
                currentPowerOutput += powerRegenRate * genMultiplier * Time.deltaTime;
                currentPowerOutput = Mathf.Min(currentPowerOutput, maxPowerOutput);
                OnPowerChanged?.Invoke(currentPowerOutput, maxPowerOutput);
            }
        }

        private void UpdatePowerDraw()
        {
            totalPowerDraw = 0f;

            foreach (var subsystem in subsystems)
            {
                if (subsystem.isOnline)
                {
                    // Apply power distribution multipliers
                    float powerMultiplier = GetPowerMultiplier(subsystem.type);
                    totalPowerDraw += subsystem.powerDraw * powerMultiplier;
                }
            }

            // Check for power overload
            bool wasOverloaded = isOverloaded;
            isOverloaded = totalPowerDraw > currentPowerOutput;

            if (isOverloaded && !wasOverloaded)
            {
                OnPowerOverload?.Invoke();
            }
            else if (!isOverloaded && wasOverloaded)
            {
                OnPowerRestored?.Invoke();
            }
        }

        private float GetPowerMultiplier(SubsystemType type)
        {
            switch (type)
            {
                case SubsystemType.Weapons:
                    return weaponsPower * powerScalingFactor;
                case SubsystemType.ShieldGenerator:
                    return shieldsPower * powerScalingFactor;
                case SubsystemType.Engines:
                case SubsystemType.Thrusters:
                    return enginesPower * powerScalingFactor;
                default:
                    return 1f;
            }
        }

        /// <summary>
        /// Set power distribution (values should sum to 1)
        /// </summary>
        public void SetPowerDistribution(float weapons, float shields, float engines)
        {
            float total = weapons + shields + engines;
            if (total > 0)
            {
                weaponsPower = weapons / total;
                shieldsPower = shields / total;
                enginesPower = engines / total;
            }
            else
            {
                weaponsPower = 0.33f;
                shieldsPower = 0.33f;
                enginesPower = 0.34f;
            }

            OnPowerDistributionChanged?.Invoke(weaponsPower, shieldsPower, enginesPower);
        }

        /// <summary>
        /// Increase power to a specific category
        /// </summary>
        public void IncreasePower(PowerCategory category)
        {
            float delta = 0.1f;

            switch (category)
            {
                case PowerCategory.Weapons:
                    weaponsPower = Mathf.Min(1f, weaponsPower + delta);
                    break;
                case PowerCategory.Shields:
                    shieldsPower = Mathf.Min(1f, shieldsPower + delta);
                    break;
                case PowerCategory.Engines:
                    enginesPower = Mathf.Min(1f, enginesPower + delta);
                    break;
            }

            NormalizePowerDistribution();
            OnPowerDistributionChanged?.Invoke(weaponsPower, shieldsPower, enginesPower);
        }

        /// <summary>
        /// Decrease power to a specific category
        /// </summary>
        public void DecreasePower(PowerCategory category)
        {
            float delta = 0.1f;

            switch (category)
            {
                case PowerCategory.Weapons:
                    weaponsPower = Mathf.Max(0f, weaponsPower - delta);
                    break;
                case PowerCategory.Shields:
                    shieldsPower = Mathf.Max(0f, shieldsPower - delta);
                    break;
                case PowerCategory.Engines:
                    enginesPower = Mathf.Max(0f, enginesPower - delta);
                    break;
            }

            NormalizePowerDistribution();
            OnPowerDistributionChanged?.Invoke(weaponsPower, shieldsPower, enginesPower);
        }

        private void NormalizePowerDistribution()
        {
            float total = weaponsPower + shieldsPower + enginesPower;
            if (total > 0)
            {
                weaponsPower /= total;
                shieldsPower /= total;
                enginesPower /= total;
            }
        }

        /// <summary>
        /// Reset power distribution to balanced
        /// </summary>
        public void ResetPowerDistribution()
        {
            SetPowerDistribution(0.33f, 0.33f, 0.34f);
        }

        /// <summary>
        /// Consume power
        /// </summary>
        public bool ConsumePower(float amount)
        {
            if (currentPowerOutput < amount) return false;
            
            currentPowerOutput -= amount;
            OnPowerChanged?.Invoke(currentPowerOutput, maxPowerOutput);
            return true;
        }

        #endregion

        #region Subsystem Management

        private void UpdateSubsystems()
        {
            foreach (var subsystem in subsystems)
            {
                // Update efficiency based on health
                subsystem.efficiency = subsystem.currentHealth / subsystem.maxHealth;

                // Disable subsystem if destroyed
                if (subsystem.currentHealth <= 0 && subsystem.isOnline)
                {
                    subsystem.isOnline = false;
                    OnSubsystemDestroyed?.Invoke(subsystem.type, subsystem);
                }

                // Update status
                if (subsystem.currentHealth <= 0)
                {
                    subsystem.status = SubsystemStatus.Destroyed;
                }
                else if (subsystem.currentHealth < subsystem.maxHealth * 0.25f)
                {
                    subsystem.status = SubsystemStatus.Critical;
                }
                else if (subsystem.currentHealth < subsystem.maxHealth * 0.5f)
                {
                    subsystem.status = SubsystemStatus.Damaged;
                }
                else if (subsystem.currentHealth < subsystem.maxHealth)
                {
                    subsystem.status = SubsystemStatus.Impaired;
                }
                else
                {
                    subsystem.status = SubsystemStatus.Operational;
                }
            }
        }

        /// <summary>
        /// Get subsystem by type
        /// </summary>
        public ShipSubsystem GetSubsystem(SubsystemType type)
        {
            if (subsystemLookup.TryGetValue(type, out ShipSubsystem subsystem))
            {
                return subsystem;
            }
            return null;
        }

        /// <summary>
        /// Get all subsystems
        /// </summary>
        public List<ShipSubsystem> GetAllSubsystems()
        {
            return new List<ShipSubsystem>(subsystems);
        }

        /// <summary>
        /// Get subsystems by status
        /// </summary>
        public List<ShipSubsystem> GetSubsystemsByStatus(SubsystemStatus status)
        {
            return subsystems.FindAll(s => s.status == status);
        }

        /// <summary>
        /// Toggle subsystem online/offline
        /// </summary>
        public void ToggleSubsystem(SubsystemType type)
        {
            var subsystem = GetSubsystem(type);
            if (subsystem != null && subsystem.currentHealth > 0)
            {
                subsystem.isOnline = !subsystem.isOnline;
            }
        }

        /// <summary>
        /// Set subsystem online state
        /// </summary>
        public void SetSubsystemOnline(SubsystemType type, bool online)
        {
            var subsystem = GetSubsystem(type);
            if (subsystem != null && subsystem.currentHealth > 0)
            {
                subsystem.isOnline = online;
            }
        }

        /// <summary>
        /// Get efficiency of a subsystem type
        /// </summary>
        public float GetSubsystemEfficiency(SubsystemType type)
        {
            var subsystem = GetSubsystem(type);
            if (subsystem != null && subsystem.isOnline)
            {
                return subsystem.efficiency * GetPowerMultiplier(type);
            }
            return 0f;
        }

        #endregion

        #region Damage and Repair

        /// <summary>
        /// Damage a specific subsystem
        /// </summary>
        public void DamageSubsystem(SubsystemType type, float damage)
        {
            var subsystem = GetSubsystem(type);
            if (subsystem != null)
            {
                subsystem.TakeDamage(damage);
                OnSubsystemDamaged?.Invoke(type, subsystem);

                if (subsystem.currentHealth <= 0)
                {
                    OnSubsystemDestroyed?.Invoke(type, subsystem);
                }
            }
        }

        /// <summary>
        /// Repair a specific subsystem
        /// </summary>
        public void RepairSubsystem(SubsystemType type, float amount)
        {
            var subsystem = GetSubsystem(type);
            if (subsystem != null)
            {
                subsystem.Repair(amount);
                OnSubsystemRepaired?.Invoke(type, subsystem);
            }
        }

        /// <summary>
        /// Repair all subsystems
        /// </summary>
        public void RepairAllSubsystems(float amount)
        {
            foreach (var subsystem in subsystems)
            {
                subsystem.Repair(amount);
            }
        }

        /// <summary>
        /// Fully repair all subsystems
        /// </summary>
        public void FullRepair()
        {
            foreach (var subsystem in subsystems)
            {
                subsystem.currentHealth = subsystem.maxHealth;
                subsystem.isOnline = true;
            }
        }

        /// <summary>
        /// Get total subsystem health percentage
        /// </summary>
        public float GetTotalSubsystemHealth()
        {
            if (subsystems.Count == 0) return 1f;

            float totalHealth = 0f;
            float totalMaxHealth = 0f;

            foreach (var subsystem in subsystems)
            {
                totalHealth += subsystem.currentHealth;
                totalMaxHealth += subsystem.maxHealth;
            }

            return totalMaxHealth > 0 ? totalHealth / totalMaxHealth : 1f;
        }

        /// <summary>
        /// Check if any essential subsystem is destroyed
        /// </summary>
        public bool HasCriticalFailure()
        {
            foreach (var subsystem in subsystems)
            {
                if (subsystem.isEssential && subsystem.currentHealth <= 0)
                {
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region Module Effects

        /// <summary>
        /// Get engine efficiency based on subsystem health
        /// </summary>
        public float GetEngineEfficiency()
        {
            var engines = GetSubsystem(SubsystemType.Engines);
            var thrusters = GetSubsystem(SubsystemType.Thrusters);

            float engineEff = engines != null && engines.isOnline ? engines.efficiency : 0f;
            float thrusterEff = thrusters != null && thrusters.isOnline ? thrusters.efficiency : 0f;

            return (engineEff * 0.7f + thrusterEff * 0.3f) * enginesPower * 3f;
        }

        /// <summary>
        /// Get shield efficiency based on subsystem health
        /// </summary>
        public float GetShieldEfficiency()
        {
            var shields = GetSubsystem(SubsystemType.ShieldGenerator);
            if (shields == null || !shields.isOnline) return 0f;
            
            return shields.efficiency * shieldsPower * 3f;
        }

        /// <summary>
        /// Get weapon efficiency based on subsystem health
        /// </summary>
        public float GetWeaponEfficiency()
        {
            var weapons = GetSubsystem(SubsystemType.Weapons);
            if (weapons == null || !weapons.isOnline) return 0f;
            
            return weapons.efficiency * weaponsPower * 3f;
        }

        /// <summary>
        /// Get sensor range based on subsystem health
        /// </summary>
        public float GetSensorEfficiency()
        {
            var sensors = GetSubsystem(SubsystemType.Sensors);
            if (sensors == null || !sensors.isOnline) return 0f;
            
            return sensors.efficiency;
        }

        /// <summary>
        /// Check if FTL is available
        /// </summary>
        public bool IsFTLAvailable()
        {
            var ftl = GetSubsystem(SubsystemType.FTLDrive);
            return ftl != null && ftl.isOnline && ftl.efficiency > 0.5f;
        }

        #endregion

        private void OnDrawGizmosSelected()
        {
            // Draw subsystem locations
            foreach (var subsystem in subsystems)
            {
                if (subsystem.location != null)
                {
                    // Color based on health
                    if (subsystem.status == SubsystemStatus.Destroyed)
                        Gizmos.color = Color.black;
                    else if (subsystem.status == SubsystemStatus.Critical)
                        Gizmos.color = Color.red;
                    else if (subsystem.status == SubsystemStatus.Damaged)
                        Gizmos.color = Color.yellow;
                    else
                        Gizmos.color = Color.green;

                    Gizmos.DrawWireSphere(subsystem.location.position, 1f);
                }
            }
        }
    }

    /// <summary>
    /// Represents a ship subsystem/module
    /// </summary>
    [System.Serializable]
    public class ShipSubsystem
    {
        public string name = "Subsystem";
        public SubsystemType type = SubsystemType.Generic;
        public float maxHealth = 100f;
        public float currentHealth = 100f;
        public float powerDraw = 10f;
        public bool isEssential = false;
        public bool isOnline = true;
        public float efficiency = 1f;
        public SubsystemStatus status = SubsystemStatus.Operational;
        public Transform location;

        // Properties
        public float HealthPercent => currentHealth / maxHealth;
        public bool IsDestroyed => currentHealth <= 0;
        public bool IsOperational => currentHealth > 0 && isOnline;

        /// <summary>
        /// Take damage to this subsystem
        /// </summary>
        public void TakeDamage(float damage)
        {
            currentHealth = Mathf.Max(0f, currentHealth - damage);
            efficiency = currentHealth / maxHealth;

            if (currentHealth <= 0)
            {
                isOnline = false;
                status = SubsystemStatus.Destroyed;
            }
        }

        /// <summary>
        /// Repair this subsystem
        /// </summary>
        public void Repair(float amount)
        {
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            efficiency = currentHealth / maxHealth;

            if (currentHealth > 0 && status == SubsystemStatus.Destroyed)
            {
                status = SubsystemStatus.Critical;
            }
        }
    }

    /// <summary>
    /// Types of ship subsystems
    /// </summary>
    public enum SubsystemType
    {
        Generic,
        PowerPlant,
        ShieldGenerator,
        Engines,
        Thrusters,
        Weapons,
        LifeSupport,
        Sensors,
        FTLDrive,
        CargoHatch,
        Canopy,
        FuelTank,
        HeatSink,
        Communications
    }

    /// <summary>
    /// Status of a subsystem
    /// </summary>
    public enum SubsystemStatus
    {
        Operational,
        Impaired,
        Damaged,
        Critical,
        Destroyed
    }

    /// <summary>
    /// Power distribution categories
    /// </summary>
    public enum PowerCategory
    {
        Weapons,
        Shields,
        Engines
    }
}
