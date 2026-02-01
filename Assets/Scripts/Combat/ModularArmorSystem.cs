using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UsefulScripts.Combat
{
    /// <summary>
    /// Defines armor zones that can be targeted on any entity (vehicle, mech, player, etc.).
    /// </summary>
    public enum ArmorZone
    {
        Front,
        Back,
        LeftSide,
        RightSide,
        Top,
        Bottom,
        Turret,
        Cockpit,
        Engine,
        Legs,
        Arms,
        Head,
        Core,
        Tracks,
        Wings,
        Tail
    }

    /// <summary>
    /// Defines different types of damage that armor can resist.
    /// </summary>
    public enum DamageType
    {
        Kinetic,        // Bullets, shells, physical impacts
        Explosive,      // Missiles, grenades, bombs
        Energy,         // Lasers, plasma, directed energy
        Fire,           // Incendiary, flamethrowers
        Ice,            // Cryo weapons
        Electric,       // EMP, lightning
        Corrosive,      // Acid, chemical weapons
        Piercing,       // Armor-piercing rounds
        Concussive      // Shockwaves, sonic weapons
    }

    /// <summary>
    /// Defines the current state of an armor plate.
    /// </summary>
    public enum ArmorState
    {
        Pristine,       // 100% - Full armor integrity
        Damaged,        // 75-99% - Minor damage
        Compromised,    // 50-74% - Moderate damage
        Critical,       // 25-49% - Severe damage
        Breached,       // 1-24% - Nearly destroyed
        Destroyed       // 0% - No protection
    }

    /// <summary>
    /// Represents a damage resistance modifier for a specific damage type.
    /// </summary>
    [Serializable]
    public class DamageResistance
    {
        public DamageType damageType;
        [Range(0f, 2f)]
        [Tooltip("1.0 = normal damage, 0.5 = 50% reduction, 1.5 = 50% extra damage")]
        public float multiplier = 1f;

        public DamageResistance(DamageType type, float mult)
        {
            damageType = type;
            multiplier = mult;
        }
    }

    /// <summary>
    /// Represents a single armor plate that can be attached to an armor zone.
    /// </summary>
    [Serializable]
    public class ArmorPlate
    {
        [Header("Identification")]
        public string plateId;
        public string plateName;

        [Header("Health")]
        public float maxHealth = 100f;
        public float currentHealth = 100f;

        [Header("Protection")]
        [Tooltip("Base damage reduction percentage (0-1)")]
        [Range(0f, 1f)]
        public float damageReduction = 0.3f;
        
        [Tooltip("Thickness affects penetration calculations")]
        public float thickness = 50f;
        
        [Tooltip("Angle of the armor plate (affects ricochet chance)")]
        [Range(0f, 90f)]
        public float slopeAngle = 0f;

        [Header("Resistances")]
        public List<DamageResistance> resistances = new List<DamageResistance>();

        // State
        private ArmorState state = ArmorState.Pristine;

        // Events
        public event Action<ArmorPlate, float> OnDamageTaken;
        public event Action<ArmorPlate, float> OnRepaired;
        public event Action<ArmorPlate, ArmorState, ArmorState> OnStateChanged;
        public event Action<ArmorPlate> OnDestroyed;

        // Properties
        public float HealthPercent => maxHealth > 0 ? currentHealth / maxHealth : 0f;
        public bool IsDestroyed => currentHealth <= 0;
        public ArmorState State => state;

        public ArmorPlate()
        {
            plateId = Guid.NewGuid().ToString();
            InitializeDefaultResistances();
        }

        public ArmorPlate(string name, float health, float reduction, float thick = 50f)
        {
            plateId = Guid.NewGuid().ToString();
            plateName = name;
            maxHealth = health;
            currentHealth = health;
            damageReduction = reduction;
            thickness = thick;
            InitializeDefaultResistances();
        }

        private void InitializeDefaultResistances()
        {
            if (resistances.Count == 0)
            {
                foreach (DamageType type in Enum.GetValues(typeof(DamageType)))
                {
                    resistances.Add(new DamageResistance(type, 1f));
                }
            }
        }

        /// <summary>
        /// Gets the resistance multiplier for a specific damage type.
        /// </summary>
        public float GetResistance(DamageType damageType)
        {
            var resistance = resistances.Find(r => r.damageType == damageType);
            return resistance?.multiplier ?? 1f;
        }

        /// <summary>
        /// Sets the resistance multiplier for a specific damage type.
        /// </summary>
        public void SetResistance(DamageType damageType, float multiplier)
        {
            var resistance = resistances.Find(r => r.damageType == damageType);
            if (resistance != null)
            {
                resistance.multiplier = multiplier;
            }
            else
            {
                resistances.Add(new DamageResistance(damageType, multiplier));
            }
        }

        /// <summary>
        /// Calculates the effective damage after armor reduction.
        /// </summary>
        public float CalculateEffectiveDamage(float incomingDamage, DamageType damageType, float impactAngle = 0f)
        {
            if (IsDestroyed) return incomingDamage;

            float damage = incomingDamage;

            // Apply damage type resistance
            damage *= GetResistance(damageType);

            // Apply base damage reduction based on armor health
            float effectiveReduction = damageReduction * HealthPercent;
            damage *= (1f - effectiveReduction);

            // Apply angle-based ricochet chance for kinetic/piercing damage
            if (damageType == DamageType.Kinetic || damageType == DamageType.Piercing)
            {
                float effectiveAngle = slopeAngle + impactAngle;
                if (effectiveAngle > 60f)
                {
                    // High chance of ricochet - significantly reduce damage
                    float ricochetReduction = Mathf.Clamp01((effectiveAngle - 60f) / 30f);
                    damage *= (1f - ricochetReduction * 0.8f);
                }
            }

            return Mathf.Max(0f, damage);
        }

        /// <summary>
        /// Takes damage and returns the amount of damage that penetrated through.
        /// </summary>
        public float TakeDamage(float damage, DamageType damageType = DamageType.Kinetic, float impactAngle = 0f)
        {
            if (IsDestroyed) return damage;

            float effectiveDamage = CalculateEffectiveDamage(damage, damageType, impactAngle);
            float penetratingDamage = 0f;

            // Apply damage to armor
            float armorDamage = effectiveDamage * 0.5f; // Half damage goes to armor degradation
            ArmorState previousState = state;
            currentHealth = Mathf.Max(0f, currentHealth - armorDamage);

            // Calculate penetrating damage based on armor health
            if (currentHealth <= 0)
            {
                penetratingDamage = effectiveDamage * (1f - HealthPercent);
                currentHealth = 0f;
            }

            UpdateState();

            OnDamageTaken?.Invoke(this, armorDamage);

            if (previousState != state)
            {
                OnStateChanged?.Invoke(this, previousState, state);
            }

            if (IsDestroyed && previousState != ArmorState.Destroyed)
            {
                OnDestroyed?.Invoke(this);
            }

            return penetratingDamage;
        }

        /// <summary>
        /// Repairs the armor plate by the specified amount.
        /// </summary>
        public void Repair(float amount)
        {
            if (amount <= 0) return;

            ArmorState previousState = state;
            float actualRepair = Mathf.Min(amount, maxHealth - currentHealth);
            currentHealth += actualRepair;

            UpdateState();

            if (actualRepair > 0)
            {
                OnRepaired?.Invoke(this, actualRepair);
            }

            if (previousState != state)
            {
                OnStateChanged?.Invoke(this, previousState, state);
            }
        }

        /// <summary>
        /// Fully repairs the armor plate.
        /// </summary>
        public void FullRepair()
        {
            Repair(maxHealth - currentHealth);
        }

        private void UpdateState()
        {
            float percent = HealthPercent;
            state = percent switch
            {
                >= 1f => ArmorState.Pristine,
                >= 0.75f => ArmorState.Damaged,
                >= 0.5f => ArmorState.Compromised,
                >= 0.25f => ArmorState.Critical,
                > 0f => ArmorState.Breached,
                _ => ArmorState.Destroyed
            };
        }

        /// <summary>
        /// Creates a copy of this armor plate.
        /// </summary>
        public ArmorPlate Clone()
        {
            var clone = new ArmorPlate
            {
                plateId = Guid.NewGuid().ToString(),
                plateName = plateName,
                maxHealth = maxHealth,
                currentHealth = currentHealth,
                damageReduction = damageReduction,
                thickness = thickness,
                slopeAngle = slopeAngle,
                resistances = resistances.Select(r => new DamageResistance(r.damageType, r.multiplier)).ToList()
            };
            clone.UpdateState();
            return clone;
        }
    }

    /// <summary>
    /// Represents an armor zone configuration with attached armor plates.
    /// </summary>
    [Serializable]
    public class ArmorZoneData
    {
        public ArmorZone zone;
        public List<ArmorPlate> plates = new List<ArmorPlate>();
        public bool isEnabled = true;
        
        [Tooltip("Chance this zone gets hit when under attack (0-1)")]
        [Range(0f, 1f)]
        public float hitChance = 0.2f;

        // Events
        public event Action<ArmorZoneData, ArmorPlate, float> OnZoneDamaged;
        public event Action<ArmorZoneData> OnZoneBreached;

        // Properties
        public float TotalHealth => plates.Sum(p => p.currentHealth);
        public float MaxTotalHealth => plates.Sum(p => p.maxHealth);
        public float HealthPercent => MaxTotalHealth > 0 ? TotalHealth / MaxTotalHealth : 0f;
        public bool IsBreached => plates.All(p => p.IsDestroyed);
        public bool HasProtection => plates.Any(p => !p.IsDestroyed);

        public ArmorZoneData()
        {
        }

        public ArmorZoneData(ArmorZone zoneType, float hitProb = 0.2f)
        {
            zone = zoneType;
            hitChance = hitProb;
        }

        /// <summary>
        /// Adds an armor plate to this zone.
        /// </summary>
        public void AddPlate(ArmorPlate plate)
        {
            if (plate != null && !plates.Contains(plate))
            {
                plates.Add(plate);
            }
        }

        /// <summary>
        /// Removes an armor plate from this zone.
        /// </summary>
        public bool RemovePlate(ArmorPlate plate)
        {
            return plates.Remove(plate);
        }

        /// <summary>
        /// Takes damage to this zone, distributing it across plates.
        /// </summary>
        public float TakeDamage(float damage, DamageType damageType = DamageType.Kinetic, float impactAngle = 0f)
        {
            if (!isEnabled || !HasProtection) return damage;

            float remainingDamage = damage;
            bool wasBreached = IsBreached;

            // Damage passes through plates in order (outer to inner)
            foreach (var plate in plates.Where(p => !p.IsDestroyed))
            {
                float penetrating = plate.TakeDamage(remainingDamage, damageType, impactAngle);
                OnZoneDamaged?.Invoke(this, plate, remainingDamage - penetrating);
                remainingDamage = penetrating;

                if (remainingDamage <= 0) break;
            }

            if (!wasBreached && IsBreached)
            {
                OnZoneBreached?.Invoke(this);
            }

            return remainingDamage;
        }

        /// <summary>
        /// Repairs all plates in this zone.
        /// </summary>
        public void RepairAll(float amount)
        {
            float remainingRepair = amount;
            foreach (var plate in plates.OrderBy(p => p.HealthPercent))
            {
                float needed = plate.maxHealth - plate.currentHealth;
                float toRepair = Mathf.Min(needed, remainingRepair);
                plate.Repair(toRepair);
                remainingRepair -= toRepair;

                if (remainingRepair <= 0) break;
            }
        }

        /// <summary>
        /// Fully repairs all plates in this zone.
        /// </summary>
        public void FullRepair()
        {
            foreach (var plate in plates)
            {
                plate.FullRepair();
            }
        }

        /// <summary>
        /// Gets the best armor state across all plates in this zone.
        /// </summary>
        public ArmorState GetBestState()
        {
            if (!plates.Any()) return ArmorState.Destroyed;
            return plates.Min(p => p.State);
        }

        /// <summary>
        /// Gets the worst armor state across all plates in this zone.
        /// </summary>
        public ArmorState GetWorstState()
        {
            if (!plates.Any()) return ArmorState.Destroyed;
            return plates.Max(p => p.State);
        }
    }

    /// <summary>
    /// ScriptableObject configuration for modular armor setups.
    /// </summary>
    [CreateAssetMenu(fileName = "NewArmorConfig", menuName = "UsefulScripts/Combat/Modular Armor Config")]
    public class ModularArmorConfig : ScriptableObject
    {
        [Header("Armor Identity")]
        public string armorSetId;
        public string armorSetName;
        [TextArea(2, 4)]
        public string description;

        [Header("Default Zones")]
        public List<ArmorZoneConfig> defaultZones = new List<ArmorZoneConfig>();

        [Header("Global Settings")]
        [Tooltip("Global damage multiplier applied to all incoming damage")]
        public float globalDamageMultiplier = 1f;
        
        [Tooltip("Whether armor can be repaired")]
        public bool canRepair = true;
        
        [Tooltip("Repair rate per second during auto-repair")]
        public float autoRepairRate = 0f;
        
        [Tooltip("Delay before auto-repair begins")]
        public float autoRepairDelay = 5f;

        /// <summary>
        /// Creates armor zone data from this configuration.
        /// </summary>
        public List<ArmorZoneData> CreateArmorZones()
        {
            var zones = new List<ArmorZoneData>();
            foreach (var config in defaultZones)
            {
                var zoneData = new ArmorZoneData(config.zone, config.hitChance);
                zoneData.isEnabled = config.isEnabled;

                foreach (var plateConfig in config.plateConfigs)
                {
                    var plate = new ArmorPlate(
                        plateConfig.plateName,
                        plateConfig.maxHealth,
                        plateConfig.damageReduction,
                        plateConfig.thickness
                    );
                    plate.slopeAngle = plateConfig.slopeAngle;

                    foreach (var resistance in plateConfig.resistances)
                    {
                        plate.SetResistance(resistance.damageType, resistance.multiplier);
                    }

                    zoneData.AddPlate(plate);
                }

                zones.Add(zoneData);
            }
            return zones;
        }
    }

    /// <summary>
    /// Configuration for a single armor zone.
    /// </summary>
    [Serializable]
    public class ArmorZoneConfig
    {
        public ArmorZone zone;
        public bool isEnabled = true;
        [Range(0f, 1f)]
        public float hitChance = 0.2f;
        public List<ArmorPlateConfig> plateConfigs = new List<ArmorPlateConfig>();
    }

    /// <summary>
    /// Configuration for a single armor plate.
    /// </summary>
    [Serializable]
    public class ArmorPlateConfig
    {
        public string plateName = "Armor Plate";
        public float maxHealth = 100f;
        [Range(0f, 1f)]
        public float damageReduction = 0.3f;
        public float thickness = 50f;
        [Range(0f, 90f)]
        public float slopeAngle = 0f;
        public List<DamageResistance> resistances = new List<DamageResistance>();
    }

    /// <summary>
    /// Contains the result of a damage calculation.
    /// </summary>
    public struct DamageResult
    {
        public float originalDamage;
        public float absorbedDamage;
        public float penetratingDamage;
        public ArmorZone hitZone;
        public DamageType damageType;
        public bool wasBlocked;
        public bool causedBreach;
        public ArmorPlate hitPlate;

        public float TotalDamageReduction => originalDamage > 0 ? (originalDamage - penetratingDamage) / originalDamage : 0f;
    }

    /// <summary>
    /// Complete modular armor system for vehicles, mechs, players, and other entities.
    /// Attach to any entity that needs modular armor protection.
    /// </summary>
    public class ModularArmorSystem : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private ModularArmorConfig armorConfig;
        [SerializeField] private string entityName = "Entity";

        [Header("Armor Zones")]
        [SerializeField] private List<ArmorZoneData> armorZones = new List<ArmorZoneData>();

        [Header("Settings")]
        [SerializeField] private float globalDamageMultiplier = 1f;
        [SerializeField] private bool canRepair = true;
        [SerializeField] private float autoRepairRate = 0f;
        [SerializeField] private float autoRepairDelay = 5f;
        [SerializeField] private bool useRandomZoneSelection = true;

        // State
        private float lastDamageTime;
        private bool isInitialized;

        // Events
        public event Action<DamageResult> OnDamageReceived;
        public event Action<ArmorZone, ArmorPlate> OnArmorDestroyed;
        public event Action<ArmorZone> OnZoneBreached;
        public event Action OnAllArmorDestroyed;
        public event Action<ArmorZone, float> OnArmorRepaired;
        public event Action<float, float> OnTotalArmorChanged;

        // Properties
        public string EntityName => entityName;
        public float TotalCurrentArmor => armorZones.Sum(z => z.TotalHealth);
        public float TotalMaxArmor => armorZones.Sum(z => z.MaxTotalHealth);
        public float TotalArmorPercent => TotalMaxArmor > 0 ? TotalCurrentArmor / TotalMaxArmor : 0f;
        public bool HasAnyArmor => armorZones.Any(z => z.HasProtection);
        public bool IsFullyBreached => armorZones.All(z => z.IsBreached);
        public int ActiveZoneCount => armorZones.Count(z => z.isEnabled && z.HasProtection);
        public IReadOnlyList<ArmorZoneData> ArmorZones => armorZones.AsReadOnly();

        private void Awake()
        {
            Initialize();
        }

        private void Update()
        {
            if (autoRepairRate > 0 && canRepair)
            {
                UpdateAutoRepair();
            }
        }

        /// <summary>
        /// Initializes the armor system from configuration or default values.
        /// </summary>
        public void Initialize()
        {
            if (isInitialized) return;

            if (armorConfig != null)
            {
                armorZones = armorConfig.CreateArmorZones();
                globalDamageMultiplier = armorConfig.globalDamageMultiplier;
                canRepair = armorConfig.canRepair;
                autoRepairRate = armorConfig.autoRepairRate;
                autoRepairDelay = armorConfig.autoRepairDelay;
            }

            // Subscribe to zone events
            foreach (var zone in armorZones)
            {
                zone.OnZoneDamaged += HandleZoneDamaged;
                zone.OnZoneBreached += HandleZoneBreached;

                foreach (var plate in zone.plates)
                {
                    plate.OnDestroyed += (p) => HandlePlateDestroyed(zone.zone, p);
                }
            }

            isInitialized = true;
        }

        /// <summary>
        /// Takes damage to a specific armor zone.
        /// </summary>
        public DamageResult TakeDamage(float damage, ArmorZone zone, DamageType damageType = DamageType.Kinetic, float impactAngle = 0f)
        {
            var result = new DamageResult
            {
                originalDamage = damage,
                hitZone = zone,
                damageType = damageType
            };

            float previousTotal = TotalCurrentArmor;
            float adjustedDamage = damage * globalDamageMultiplier;

            var zoneData = GetZone(zone);
            if (zoneData != null && zoneData.isEnabled && zoneData.HasProtection)
            {
                result.hitPlate = zoneData.plates.FirstOrDefault(p => !p.IsDestroyed);
                result.penetratingDamage = zoneData.TakeDamage(adjustedDamage, damageType, impactAngle);
                result.absorbedDamage = adjustedDamage - result.penetratingDamage;
                result.wasBlocked = result.penetratingDamage <= 0;
                result.causedBreach = zoneData.IsBreached;
            }
            else
            {
                // No armor protection - full damage penetrates
                result.penetratingDamage = adjustedDamage;
                result.absorbedDamage = 0f;
                result.wasBlocked = false;
            }

            lastDamageTime = Time.time;
            float currentTotal = TotalCurrentArmor;

            OnDamageReceived?.Invoke(result);
            OnTotalArmorChanged?.Invoke(currentTotal, TotalMaxArmor);

            if (!HasAnyArmor)
            {
                OnAllArmorDestroyed?.Invoke();
            }

            return result;
        }

        /// <summary>
        /// Takes damage and automatically selects a random zone based on hit probabilities.
        /// </summary>
        public DamageResult TakeDamageRandom(float damage, DamageType damageType = DamageType.Kinetic, float impactAngle = 0f)
        {
            ArmorZone selectedZone = SelectRandomZone();
            return TakeDamage(damage, selectedZone, damageType, impactAngle);
        }

        /// <summary>
        /// Selects a random armor zone based on hit probabilities.
        /// </summary>
        public ArmorZone SelectRandomZone()
        {
            var enabledZones = armorZones.Where(z => z.isEnabled).ToList();
            if (!enabledZones.Any())
            {
                return ArmorZone.Front; // Default fallback
            }

            float totalChance = enabledZones.Sum(z => z.hitChance);
            float random = UnityEngine.Random.Range(0f, totalChance);
            float cumulative = 0f;

            foreach (var zone in enabledZones)
            {
                cumulative += zone.hitChance;
                if (random <= cumulative)
                {
                    return zone.zone;
                }
            }

            return enabledZones.Last().zone;
        }

        /// <summary>
        /// Gets data for a specific armor zone.
        /// </summary>
        public ArmorZoneData GetZone(ArmorZone zone)
        {
            return armorZones.Find(z => z.zone == zone);
        }

        /// <summary>
        /// Adds a new armor zone.
        /// </summary>
        public void AddZone(ArmorZoneData zone)
        {
            if (zone != null && !armorZones.Any(z => z.zone == zone.zone))
            {
                zone.OnZoneDamaged += HandleZoneDamaged;
                zone.OnZoneBreached += HandleZoneBreached;

                foreach (var plate in zone.plates)
                {
                    plate.OnDestroyed += (p) => HandlePlateDestroyed(zone.zone, p);
                }

                armorZones.Add(zone);
            }
        }

        /// <summary>
        /// Removes an armor zone.
        /// </summary>
        public bool RemoveZone(ArmorZone zone)
        {
            var zoneData = GetZone(zone);
            if (zoneData != null)
            {
                zoneData.OnZoneDamaged -= HandleZoneDamaged;
                zoneData.OnZoneBreached -= HandleZoneBreached;
                return armorZones.Remove(zoneData);
            }
            return false;
        }

        /// <summary>
        /// Adds an armor plate to a specific zone.
        /// </summary>
        public bool AddPlateToZone(ArmorZone zone, ArmorPlate plate)
        {
            var zoneData = GetZone(zone);
            if (zoneData != null)
            {
                zoneData.AddPlate(plate);
                plate.OnDestroyed += (p) => HandlePlateDestroyed(zone, p);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes an armor plate from a specific zone.
        /// </summary>
        public bool RemovePlateFromZone(ArmorZone zone, ArmorPlate plate)
        {
            var zoneData = GetZone(zone);
            return zoneData?.RemovePlate(plate) ?? false;
        }

        /// <summary>
        /// Repairs a specific armor zone.
        /// </summary>
        public void RepairZone(ArmorZone zone, float amount)
        {
            if (!canRepair) return;

            var zoneData = GetZone(zone);
            if (zoneData != null)
            {
                float before = zoneData.TotalHealth;
                zoneData.RepairAll(amount);
                float repaired = zoneData.TotalHealth - before;

                if (repaired > 0)
                {
                    OnArmorRepaired?.Invoke(zone, repaired);
                    OnTotalArmorChanged?.Invoke(TotalCurrentArmor, TotalMaxArmor);
                }
            }
        }

        /// <summary>
        /// Fully repairs a specific armor zone.
        /// </summary>
        public void FullRepairZone(ArmorZone zone)
        {
            if (!canRepair) return;

            var zoneData = GetZone(zone);
            if (zoneData != null)
            {
                float before = zoneData.TotalHealth;
                zoneData.FullRepair();
                float repaired = zoneData.TotalHealth - before;

                if (repaired > 0)
                {
                    OnArmorRepaired?.Invoke(zone, repaired);
                    OnTotalArmorChanged?.Invoke(TotalCurrentArmor, TotalMaxArmor);
                }
            }
        }

        /// <summary>
        /// Repairs all armor zones.
        /// </summary>
        public void RepairAll(float amount)
        {
            if (!canRepair) return;

            float perZone = amount / Mathf.Max(1, armorZones.Count);
            foreach (var zone in armorZones)
            {
                RepairZone(zone.zone, perZone);
            }
        }

        /// <summary>
        /// Fully repairs all armor zones.
        /// </summary>
        public void FullRepairAll()
        {
            if (!canRepair) return;

            foreach (var zone in armorZones)
            {
                FullRepairZone(zone.zone);
            }
        }

        /// <summary>
        /// Gets the health percentage of a specific zone.
        /// </summary>
        public float GetZoneHealthPercent(ArmorZone zone)
        {
            var zoneData = GetZone(zone);
            return zoneData?.HealthPercent ?? 0f;
        }

        /// <summary>
        /// Gets the armor state of a specific zone.
        /// </summary>
        public ArmorState GetZoneState(ArmorZone zone)
        {
            var zoneData = GetZone(zone);
            return zoneData?.GetWorstState() ?? ArmorState.Destroyed;
        }

        /// <summary>
        /// Gets a summary of all zone states.
        /// </summary>
        public Dictionary<ArmorZone, ArmorState> GetAllZoneStates()
        {
            return armorZones.ToDictionary(z => z.zone, z => z.GetWorstState());
        }

        /// <summary>
        /// Sets whether a zone is enabled.
        /// </summary>
        public void SetZoneEnabled(ArmorZone zone, bool enabled)
        {
            var zoneData = GetZone(zone);
            if (zoneData != null)
            {
                zoneData.isEnabled = enabled;
            }
        }

        /// <summary>
        /// Sets the global damage multiplier.
        /// </summary>
        public void SetGlobalDamageMultiplier(float multiplier)
        {
            globalDamageMultiplier = Mathf.Max(0f, multiplier);
        }

        private void UpdateAutoRepair()
        {
            if (Time.time - lastDamageTime < autoRepairDelay) return;
            if (TotalArmorPercent >= 1f) return;

            float repairAmount = autoRepairRate * Time.deltaTime;
            RepairAll(repairAmount);
        }

        private void HandleZoneDamaged(ArmorZoneData zone, ArmorPlate plate, float damage)
        {
            // Additional handling can be added here
        }

        private void HandleZoneBreached(ArmorZoneData zone)
        {
            OnZoneBreached?.Invoke(zone.zone);
        }

        private void HandlePlateDestroyed(ArmorZone zone, ArmorPlate plate)
        {
            OnArmorDestroyed?.Invoke(zone, plate);
        }

        /// <summary>
        /// Resets the armor system to initial state.
        /// </summary>
        public void Reset()
        {
            isInitialized = false;
            armorZones.Clear();
            Initialize();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            globalDamageMultiplier = Mathf.Max(0f, globalDamageMultiplier);
            autoRepairRate = Mathf.Max(0f, autoRepairRate);
            autoRepairDelay = Mathf.Max(0f, autoRepairDelay);
        }
#endif
    }
}
