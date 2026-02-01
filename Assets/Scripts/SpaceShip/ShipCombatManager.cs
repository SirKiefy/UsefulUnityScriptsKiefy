using UnityEngine;
using System.Collections.Generic;

namespace UsefulScripts.SpaceShip
{
    /// <summary>
    /// Combat manager for spaceships.
    /// Handles damage distribution, combat state, and engagement management.
    /// Inspired by Elite Dangerous, Star Citizen, and 4X strategy games.
    /// </summary>
    public class ShipCombatManager : MonoBehaviour
    {
        [Header("Combat State")]
        [SerializeField] private CombatState currentState = CombatState.Idle;
        [SerializeField] private float combatTimeout = 30f;
        [SerializeField] private float engagementRange = 5000f;

        [Header("Damage Distribution")]
        [SerializeField] private DamageDistributionMode damageMode = DamageDistributionMode.ShieldsFirst;
        [SerializeField] private float armorDamageReduction = 0.5f;
        [SerializeField] private float subsystemDamageChance = 0.2f;

        [Header("Defensive Systems")]
        [SerializeField] private bool enablePointDefense = true;
        [SerializeField] private float pointDefenseRange = 500f;
        [SerializeField] private float pointDefenseFireRate = 10f;
        [SerializeField] private LayerMask missileLayer;

        [Header("Countermeasures")]
        [SerializeField] private int chaffCount = 10;
        [SerializeField] private int flareCount = 10;
        [SerializeField] private int maxChaffCount = 10;
        [SerializeField] private int maxFlareCount = 10;
        [SerializeField] private float countermeasureCooldown = 5f;
        [SerializeField] private float chaffDuration = 5f;
        [SerializeField] private float flareDuration = 3f;
        [SerializeField] private GameObject chaffPrefab;
        [SerializeField] private GameObject flarePrefab;

        [Header("Evasive Maneuvers")]
        [SerializeField] private bool enableEvasiveManeuvers = true;
        [SerializeField] private float evasiveBoostMultiplier = 1.5f;
        [SerializeField] private float evasiveCooldown = 10f;

        [Header("References")]
        [SerializeField] private ShipHealthSystem healthSystem;
        [SerializeField] private ShipSubsystems subsystems;
        [SerializeField] private ShipWeaponSystem weaponSystem;
        [SerializeField] private ShipTargetingSystem targetingSystem;
        [SerializeField] private SpaceShipController shipController;

        // Combat state tracking
        private float lastCombatTime;
        private float lastCountermeasureTime;
        private float lastEvasiveTime;
        private float lastPointDefenseTime;
        private List<Transform> engagedEnemies = new List<Transform>();
        private List<Transform> incomingMissiles = new List<Transform>();
        private bool isEvading;

        // Events
        public event System.Action<CombatState> OnCombatStateChanged;
        public event System.Action<DamageReport> OnDamageReceived;
        public event System.Action<Transform> OnEnemyEngaged;
        public event System.Action<Transform> OnEnemyDisengaged;
        public event System.Action OnMissileWarning;
        public event System.Action OnChaffDeployed;
        public event System.Action OnFlareDeployed;
        public event System.Action OnEvasiveStart;
        public event System.Action OnEvasiveEnd;
        public event System.Action<int, int> OnChaffCountChanged; // current, max
        public event System.Action<int, int> OnFlareCountChanged; // current, max

        // Properties
        public CombatState CurrentState => currentState;
        public bool InCombat => currentState == CombatState.Combat || currentState == CombatState.Evading;
        public int EngagedEnemyCount => engagedEnemies.Count;
        public List<Transform> EngagedEnemies => engagedEnemies;
        public int ChaffRemaining => chaffCount;
        public int FlareRemaining => flareCount;
        public bool CanDeployCountermeasures => Time.time - lastCountermeasureTime >= countermeasureCooldown;
        public bool CanEvade => enableEvasiveManeuvers && Time.time - lastEvasiveTime >= evasiveCooldown;
        public bool IsEvading => isEvading;
        public bool HasMissileWarning => incomingMissiles.Count > 0;

        private void Awake()
        {
            // Get components if not assigned
            if (healthSystem == null) healthSystem = GetComponent<ShipHealthSystem>();
            if (subsystems == null) subsystems = GetComponent<ShipSubsystems>();
            if (weaponSystem == null) weaponSystem = GetComponent<ShipWeaponSystem>();
            if (targetingSystem == null) targetingSystem = GetComponent<ShipTargetingSystem>();
            if (shipController == null) shipController = GetComponent<SpaceShipController>();
        }

        private void Update()
        {
            UpdateCombatState();
            UpdatePointDefense();
            UpdateMissileTracking();
            UpdateEvasive();
        }

        #region Combat State

        private void UpdateCombatState()
        {
            // Clean up destroyed enemies
            engagedEnemies.RemoveAll(e => e == null);

            // Check for combat timeout
            if (currentState == CombatState.Combat && engagedEnemies.Count == 0)
            {
                if (Time.time - lastCombatTime > combatTimeout)
                {
                    SetCombatState(CombatState.Idle);
                }
            }
        }

        /// <summary>
        /// Set combat state
        /// </summary>
        public void SetCombatState(CombatState state)
        {
            if (currentState != state)
            {
                currentState = state;
                OnCombatStateChanged?.Invoke(state);
            }
        }

        /// <summary>
        /// Enter combat with an enemy
        /// </summary>
        public void EngageEnemy(Transform enemy)
        {
            if (enemy == null) return;

            if (!engagedEnemies.Contains(enemy))
            {
                engagedEnemies.Add(enemy);
                OnEnemyEngaged?.Invoke(enemy);
            }

            lastCombatTime = Time.time;

            if (currentState != CombatState.Combat && currentState != CombatState.Evading)
            {
                SetCombatState(CombatState.Combat);
            }
        }

        /// <summary>
        /// Disengage from an enemy
        /// </summary>
        public void DisengageEnemy(Transform enemy)
        {
            if (engagedEnemies.Remove(enemy))
            {
                OnEnemyDisengaged?.Invoke(enemy);
            }
        }

        /// <summary>
        /// Disengage from all enemies
        /// </summary>
        public void DisengageAll()
        {
            engagedEnemies.Clear();
            SetCombatState(CombatState.Disengaging);
        }

        #endregion

        #region Damage Handling

        /// <summary>
        /// Process incoming damage
        /// </summary>
        public DamageReport ProcessDamage(float rawDamage, DamageType damageType, Vector3 hitPoint, Transform attacker = null)
        {
            DamageReport report = new DamageReport
            {
                rawDamage = rawDamage,
                damageType = damageType,
                hitPoint = hitPoint,
                attacker = attacker
            };

            // Enter combat
            if (attacker != null)
            {
                EngageEnemy(attacker);
            }

            // Apply damage based on distribution mode
            float remainingDamage = rawDamage;

            switch (damageMode)
            {
                case DamageDistributionMode.ShieldsFirst:
                    remainingDamage = ApplyShieldDamage(remainingDamage, ref report);
                    remainingDamage = ApplyArmorDamage(remainingDamage, ref report);
                    ApplyHullDamage(remainingDamage, ref report);
                    break;

                case DamageDistributionMode.Distributed:
                    float shieldShare = remainingDamage * 0.6f;
                    float armorShare = remainingDamage * 0.3f;
                    float hullShare = remainingDamage * 0.1f;
                    ApplyShieldDamage(shieldShare, ref report);
                    ApplyArmorDamage(armorShare, ref report);
                    ApplyHullDamage(hullShare, ref report);
                    break;

                case DamageDistributionMode.DirectToHull:
                    ApplyHullDamage(remainingDamage, ref report);
                    break;
            }

            // Check for subsystem damage
            if (subsystems != null && Random.value < subsystemDamageChance)
            {
                ShipSubsystem hitSubsystem = GetSubsystemAtPoint(hitPoint);
                if (hitSubsystem != null)
                {
                    hitSubsystem.TakeDamage(rawDamage * 0.5f);
                    report.damagedSubsystem = hitSubsystem;
                }
            }

            OnDamageReceived?.Invoke(report);
            return report;
        }

        private float ApplyShieldDamage(float damage, ref DamageReport report)
        {
            if (healthSystem == null) return damage;

            float shieldDamage = Mathf.Min(damage, healthSystem.CurrentShield);
            if (shieldDamage > 0)
            {
                healthSystem.DamageShield(shieldDamage);
                report.shieldDamage = shieldDamage;
            }

            return damage - shieldDamage;
        }

        private float ApplyArmorDamage(float damage, ref DamageReport report)
        {
            if (healthSystem == null) return damage;

            // Armor reduces damage
            float reducedDamage = damage * (1f - armorDamageReduction);
            float armorDamage = Mathf.Min(reducedDamage, healthSystem.CurrentArmor);
            
            if (armorDamage > 0)
            {
                healthSystem.DamageArmor(armorDamage);
                report.armorDamage = armorDamage;
            }

            // Return damage that penetrated armor
            return reducedDamage - armorDamage;
        }

        private void ApplyHullDamage(float damage, ref DamageReport report)
        {
            if (healthSystem == null || damage <= 0) return;

            healthSystem.DamageHull(damage);
            report.hullDamage = damage;
        }

        private ShipSubsystem GetSubsystemAtPoint(Vector3 point)
        {
            if (subsystems == null) return null;

            // Find nearest subsystem to hit point
            var allSystems = subsystems.GetAllSubsystems();
            ShipSubsystem nearest = null;
            float nearestDist = float.MaxValue;

            foreach (var system in allSystems)
            {
                if (system.location != null)
                {
                    float dist = Vector3.Distance(point, system.location.position);
                    if (dist < nearestDist)
                    {
                        nearestDist = dist;
                        nearest = system;
                    }
                }
            }

            return nearest;
        }

        #endregion

        #region Point Defense

        private void UpdatePointDefense()
        {
            if (!enablePointDefense) return;
            if (Time.time - lastPointDefenseTime < 1f / pointDefenseFireRate) return;

            // Check for incoming missiles
            foreach (var missile in incomingMissiles)
            {
                if (missile == null) continue;

                float distance = Vector3.Distance(transform.position, missile.position);
                if (distance <= pointDefenseRange)
                {
                    // Attempt to destroy missile
                    if (FirePointDefense(missile))
                    {
                        lastPointDefenseTime = Time.time;
                        break;
                    }
                }
            }
        }

        private bool FirePointDefense(Transform target)
        {
            // Attempt to destroy missile (implement hit chance)
            float hitChance = 0.3f; // 30% chance per shot
            if (Random.value < hitChance)
            {
                ShipProjectile projectile = target.GetComponent<ShipProjectile>();
                if (projectile != null)
                {
                    projectile.Destroy();
                    incomingMissiles.Remove(target);
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region Missile Tracking

        private void UpdateMissileTracking()
        {
            // Clean up destroyed missiles
            incomingMissiles.RemoveAll(m => m == null);

            // Scan for new incoming missiles
            Collider[] missiles = Physics.OverlapSphere(transform.position, engagementRange, missileLayer);
            foreach (var col in missiles)
            {
                ShipProjectile projectile = col.GetComponent<ShipProjectile>();
                if (projectile != null && projectile.Target == transform)
                {
                    if (!incomingMissiles.Contains(col.transform))
                    {
                        incomingMissiles.Add(col.transform);
                        OnMissileWarning?.Invoke();
                    }
                }
            }
        }

        /// <summary>
        /// Register an incoming missile
        /// </summary>
        public void RegisterIncomingMissile(Transform missile)
        {
            if (!incomingMissiles.Contains(missile))
            {
                incomingMissiles.Add(missile);
                OnMissileWarning?.Invoke();
            }
        }

        #endregion

        #region Countermeasures

        /// <summary>
        /// Deploy chaff
        /// </summary>
        public bool DeployChaff()
        {
            if (chaffCount <= 0 || !CanDeployCountermeasures) return false;

            chaffCount--;
            lastCountermeasureTime = Time.time;

            if (chaffPrefab != null)
            {
                GameObject chaff = Instantiate(chaffPrefab, transform.position - transform.forward * 5f, transform.rotation);
                Destroy(chaff, chaffDuration);
            }

            OnChaffDeployed?.Invoke();
            OnChaffCountChanged?.Invoke(chaffCount, maxChaffCount);
            return true;
        }

        /// <summary>
        /// Deploy flare
        /// </summary>
        public bool DeployFlare()
        {
            if (flareCount <= 0 || !CanDeployCountermeasures) return false;

            flareCount--;
            lastCountermeasureTime = Time.time;

            if (flarePrefab != null)
            {
                GameObject flare = Instantiate(flarePrefab, transform.position - transform.forward * 5f, transform.rotation);
                Destroy(flare, flareDuration);
            }

            OnFlareDeployed?.Invoke();
            OnFlareCountChanged?.Invoke(flareCount, maxFlareCount);
            return true;
        }

        /// <summary>
        /// Deploy all countermeasures
        /// </summary>
        public void DeployAllCountermeasures()
        {
            DeployChaff();
            DeployFlare();
        }

        /// <summary>
        /// Reload countermeasures
        /// </summary>
        public void ReloadCountermeasures(int chaff, int flares)
        {
            chaffCount = Mathf.Min(chaffCount + chaff, maxChaffCount);
            flareCount = Mathf.Min(flareCount + flares, maxFlareCount);
            OnChaffCountChanged?.Invoke(chaffCount, maxChaffCount);
            OnFlareCountChanged?.Invoke(flareCount, maxFlareCount);
        }

        #endregion

        #region Evasive Maneuvers

        private void UpdateEvasive()
        {
            if (isEvading && currentState != CombatState.Evading)
            {
                EndEvasive();
            }
        }

        /// <summary>
        /// Start evasive maneuvers
        /// </summary>
        public bool StartEvasive()
        {
            if (!CanEvade) return false;

            isEvading = true;
            lastEvasiveTime = Time.time;
            SetCombatState(CombatState.Evading);

            // Boost ship performance temporarily
            if (shipController != null)
            {
                shipController.StartBoost();
            }

            OnEvasiveStart?.Invoke();
            return true;
        }

        /// <summary>
        /// End evasive maneuvers
        /// </summary>
        public void EndEvasive()
        {
            if (!isEvading) return;

            isEvading = false;
            SetCombatState(engagedEnemies.Count > 0 ? CombatState.Combat : CombatState.Idle);
            OnEvasiveEnd?.Invoke();
        }

        /// <summary>
        /// Get evasive direction suggestion
        /// </summary>
        public Vector3 GetEvasiveDirection()
        {
            if (engagedEnemies.Count == 0) return transform.forward;

            // Calculate average enemy direction
            Vector3 avgEnemyDir = Vector3.zero;
            foreach (var enemy in engagedEnemies)
            {
                if (enemy != null)
                {
                    avgEnemyDir += (enemy.position - transform.position).normalized;
                }
            }
            avgEnemyDir /= engagedEnemies.Count;

            // Evade perpendicular to average enemy direction
            Vector3 perpendicular = Vector3.Cross(avgEnemyDir, Vector3.up).normalized;
            
            // Add some randomness
            perpendicular += Random.insideUnitSphere * 0.3f;
            
            return perpendicular.normalized;
        }

        #endregion

        #region Power Distribution

        /// <summary>
        /// Set power distribution priority
        /// </summary>
        public void SetPowerPriority(PowerPriority priority)
        {
            if (subsystems == null) return;

            switch (priority)
            {
                case PowerPriority.Weapons:
                    subsystems.SetPowerDistribution(0.5f, 0.3f, 0.2f);
                    break;
                case PowerPriority.Engines:
                    subsystems.SetPowerDistribution(0.2f, 0.3f, 0.5f);
                    break;
                case PowerPriority.Shields:
                    subsystems.SetPowerDistribution(0.2f, 0.5f, 0.3f);
                    break;
                case PowerPriority.Balanced:
                    subsystems.SetPowerDistribution(0.33f, 0.33f, 0.34f);
                    break;
            }
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Set damage distribution mode
        /// </summary>
        public void SetDamageMode(DamageDistributionMode mode)
        {
            damageMode = mode;
        }

        /// <summary>
        /// Set engagement range
        /// </summary>
        public void SetEngagementRange(float range)
        {
            engagementRange = Mathf.Max(100f, range);
        }

        /// <summary>
        /// Enable or disable point defense
        /// </summary>
        public void SetPointDefense(bool enabled)
        {
            enablePointDefense = enabled;
        }

        /// <summary>
        /// Enable or disable evasive maneuvers
        /// </summary>
        public void SetEvasiveEnabled(bool enabled)
        {
            enableEvasiveManeuvers = enabled;
        }

        #endregion

        private void OnDrawGizmosSelected()
        {
            // Draw engagement range
            Gizmos.color = new Color(1f, 0f, 0f, 0.1f);
            Gizmos.DrawWireSphere(transform.position, engagementRange);

            // Draw point defense range
            if (enablePointDefense)
            {
                Gizmos.color = new Color(0f, 0f, 1f, 0.2f);
                Gizmos.DrawWireSphere(transform.position, pointDefenseRange);
            }

            // Draw engaged enemies
            Gizmos.color = Color.red;
            foreach (var enemy in engagedEnemies)
            {
                if (enemy != null)
                {
                    Gizmos.DrawLine(transform.position, enemy.position);
                }
            }

            // Draw incoming missiles
            Gizmos.color = Color.yellow;
            foreach (var missile in incomingMissiles)
            {
                if (missile != null)
                {
                    Gizmos.DrawWireSphere(missile.position, 5f);
                }
            }
        }
    }

    /// <summary>
    /// Combat state of the ship
    /// </summary>
    public enum CombatState
    {
        Idle,
        Combat,
        Evading,
        Disengaging,
        Retreating
    }

    /// <summary>
    /// How damage is distributed between shields, armor, and hull
    /// </summary>
    public enum DamageDistributionMode
    {
        /// <summary>
        /// Damage hits shields first, then armor, then hull
        /// </summary>
        ShieldsFirst,

        /// <summary>
        /// Damage is distributed across all layers
        /// </summary>
        Distributed,

        /// <summary>
        /// Damage bypasses shields and armor
        /// </summary>
        DirectToHull
    }

    /// <summary>
    /// Types of damage
    /// </summary>
    public enum DamageType
    {
        Kinetic,
        Thermal,
        Explosive,
        Electromagnetic,
        Absolute
    }

    /// <summary>
    /// Power distribution priority
    /// </summary>
    public enum PowerPriority
    {
        Balanced,
        Weapons,
        Shields,
        Engines
    }

    /// <summary>
    /// Report of damage dealt to a ship
    /// </summary>
    [System.Serializable]
    public struct DamageReport
    {
        public float rawDamage;
        public float shieldDamage;
        public float armorDamage;
        public float hullDamage;
        public DamageType damageType;
        public Vector3 hitPoint;
        public Transform attacker;
        public ShipSubsystem damagedSubsystem;

        public float TotalDamage => shieldDamage + armorDamage + hullDamage;
    }
}
