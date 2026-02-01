using UnityEngine;

namespace UsefulScripts.SpaceShip
{
    /// <summary>
    /// Turret system for spaceship with aiming, tracking, and firing controls.
    /// Supports fixed, gimballed, and turreted weapon mounts.
    /// Inspired by Elite Dangerous and Star Citizen turret mechanics.
    /// </summary>
    public class TurretSystem : MonoBehaviour
    {
        [Header("Turret Configuration")]
        [SerializeField] private TurretType turretType = TurretType.Gimballed;
        [SerializeField] private float rotationSpeed = 90f;
        [SerializeField] private float aimSmoothing = 5f;

        [Header("Rotation Limits")]
        [SerializeField] private float minYaw = -180f;
        [SerializeField] private float maxYaw = 180f;
        [SerializeField] private float minPitch = -30f;
        [SerializeField] private float maxPitch = 60f;
        [SerializeField] private bool unlimited360Yaw = true;

        [Header("Turret Parts")]
        [SerializeField] private Transform turretBase;
        [SerializeField] private Transform turretBarrel;
        [SerializeField] private Transform[] firePoints;

        [Header("Targeting")]
        [SerializeField] private float maxTrackingRange = 5000f;
        [SerializeField] private float trackingSpeed = 120f;
        [SerializeField] private float leadPrediction = 1f;
        [SerializeField] private LayerMask targetMask;
        [SerializeField] private bool autoTrack = false;

        [Header("Weapon Settings")]
        [SerializeField] private float fireRate = 5f;
        [SerializeField] private float projectileSpeed = 1500f;
        [SerializeField] private float damage = 25f;
        [SerializeField] private float heat = 5f;
        [SerializeField] private GameObject projectilePrefab;

        [Header("Heat Management")]
        [SerializeField] private float maxHeat = 100f;
        [SerializeField] private float heatDissipation = 10f;
        [SerializeField] private float overheatThreshold = 80f;
        [SerializeField] private float overheatCooldownTime = 3f;

        [Header("Ammunition")]
        [SerializeField] private bool hasAmmo = true;
        [SerializeField] private int maxAmmo = 500;
        [SerializeField] private int currentAmmo = 500;
        [SerializeField] private int ammoPerShot = 1;

        [Header("Effects")]
        [SerializeField] private ParticleSystem[] muzzleFlashParticles;
        [SerializeField] private AudioSource fireAudioSource;
        [SerializeField] private AudioClip fireSound;
        [SerializeField] private AudioClip overheatSound;

        // State
        private Transform currentTarget;
        private Vector3 aimPoint;
        private Vector3 currentAimDirection;
        private float currentYaw;
        private float currentPitch;
        private float lastFireTime;
        private float currentHeat;
        private bool isOverheated;
        private float overheatTimer;
        private bool isFiring;
        private int currentFirePointIndex;

        // Events
        public event System.Action OnFire;
        public event System.Action OnOverheat;
        public event System.Action OnCooldownComplete;
        public event System.Action OnAmmoEmpty;
        public event System.Action<Transform> OnTargetAcquired;
        public event System.Action OnTargetLost;
        public event System.Action<float, float> OnHeatChanged; // current, max
        public event System.Action<int, int> OnAmmoChanged; // current, max

        // Properties
        public Transform CurrentTarget => currentTarget;
        public Vector3 AimPoint => aimPoint;
        public Vector3 AimDirection => currentAimDirection;
        public float CurrentYaw => currentYaw;
        public float CurrentPitch => currentPitch;
        public float CurrentHeat => currentHeat;
        public float HeatPercent => currentHeat / maxHeat;
        public bool IsOverheated => isOverheated;
        public bool IsFiring => isFiring;
        public int CurrentAmmo => currentAmmo;
        public int MaxAmmo => maxAmmo;
        public float AmmoPercent => hasAmmo ? (float)currentAmmo / maxAmmo : 1f;
        public bool CanFire => !isOverheated && (!hasAmmo || currentAmmo >= ammoPerShot) && Time.time - lastFireTime >= 1f / fireRate;
        public TurretType Type => turretType;
        public float ProjectileSpeed => projectileSpeed;
        public float Damage => damage;

        private void Awake()
        {
            if (turretBase == null) turretBase = transform;
            if (turretBarrel == null) turretBarrel = transform;
            
            currentAimDirection = turretBarrel.forward;
        }

        private void Update()
        {
            UpdateHeat();
            UpdateOverheat();
            UpdateTracking();
            UpdateTurretRotation();
        }

        #region Heat Management

        private void UpdateHeat()
        {
            if (currentHeat > 0)
            {
                currentHeat -= heatDissipation * Time.deltaTime;
                currentHeat = Mathf.Max(0f, currentHeat);
                OnHeatChanged?.Invoke(currentHeat, maxHeat);
            }
        }

        private void UpdateOverheat()
        {
            if (isOverheated)
            {
                overheatTimer -= Time.deltaTime;
                if (overheatTimer <= 0 && currentHeat < overheatThreshold * 0.5f)
                {
                    isOverheated = false;
                    OnCooldownComplete?.Invoke();
                }
            }
        }

        private void AddHeat(float amount)
        {
            currentHeat += amount;
            OnHeatChanged?.Invoke(currentHeat, maxHeat);

            if (currentHeat >= overheatThreshold && !isOverheated)
            {
                TriggerOverheat();
            }
        }

        private void TriggerOverheat()
        {
            isOverheated = true;
            overheatTimer = overheatCooldownTime;
            
            if (fireAudioSource != null && overheatSound != null)
            {
                fireAudioSource.PlayOneShot(overheatSound);
            }
            
            OnOverheat?.Invoke();
        }

        #endregion

        #region Targeting

        private void UpdateTracking()
        {
            if (turretType == TurretType.Fixed) return;

            if (autoTrack && currentTarget != null)
            {
                TrackTarget(currentTarget);
            }
        }

        /// <summary>
        /// Set current target for tracking
        /// </summary>
        public void SetTarget(Transform target)
        {
            if (currentTarget != target)
            {
                currentTarget = target;
                if (target != null)
                {
                    OnTargetAcquired?.Invoke(target);
                }
                else
                {
                    OnTargetLost?.Invoke();
                }
            }
        }

        /// <summary>
        /// Clear current target
        /// </summary>
        public void ClearTarget()
        {
            if (currentTarget != null)
            {
                currentTarget = null;
                OnTargetLost?.Invoke();
            }
        }

        /// <summary>
        /// Track a specific target with lead prediction
        /// </summary>
        public void TrackTarget(Transform target)
        {
            if (target == null) return;

            Vector3 targetPosition = target.position;

            // Lead prediction for moving targets
            if (leadPrediction > 0f)
            {
                Rigidbody targetRb = target.GetComponent<Rigidbody>();
                if (targetRb != null)
                {
                    float distance = Vector3.Distance(transform.position, targetPosition);
                    float timeToTarget = distance / projectileSpeed;
                    targetPosition += targetRb.linearVelocity * timeToTarget * leadPrediction;
                }
            }

            SetAimPoint(targetPosition);
        }

        /// <summary>
        /// Calculate lead position for a target
        /// </summary>
        public Vector3 CalculateLeadPosition(Transform target)
        {
            if (target == null) return Vector3.zero;

            Vector3 targetPosition = target.position;
            Rigidbody targetRb = target.GetComponent<Rigidbody>();
            
            if (targetRb != null)
            {
                float distance = Vector3.Distance(transform.position, targetPosition);
                float timeToTarget = distance / projectileSpeed;
                targetPosition += targetRb.linearVelocity * timeToTarget * leadPrediction;
            }

            return targetPosition;
        }

        /// <summary>
        /// Check if target is in firing arc
        /// </summary>
        public bool IsTargetInArc(Transform target)
        {
            if (target == null) return false;

            Vector3 dirToTarget = (target.position - turretBase.position).normalized;
            float angle = Vector3.Angle(turretBase.forward, dirToTarget);
            
            return angle <= Mathf.Max(Mathf.Abs(maxYaw), Mathf.Abs(maxPitch));
        }

        /// <summary>
        /// Check if target is in range
        /// </summary>
        public bool IsTargetInRange(Transform target)
        {
            if (target == null) return false;
            return Vector3.Distance(transform.position, target.position) <= maxTrackingRange;
        }

        #endregion

        #region Aim Control

        /// <summary>
        /// Set aim point in world space
        /// </summary>
        public void SetAimPoint(Vector3 worldPoint)
        {
            aimPoint = worldPoint;
        }

        /// <summary>
        /// Set aim direction
        /// </summary>
        public void SetAimDirection(Vector3 direction)
        {
            aimPoint = turretBarrel.position + direction.normalized * 1000f;
        }

        /// <summary>
        /// Set aim using yaw and pitch values directly
        /// </summary>
        public void SetAimAngles(float yaw, float pitch)
        {
            currentYaw = ClampYaw(yaw);
            currentPitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        /// <summary>
        /// Adjust aim by delta values
        /// </summary>
        public void AdjustAim(float yawDelta, float pitchDelta)
        {
            currentYaw = ClampYaw(currentYaw + yawDelta * rotationSpeed * Time.deltaTime);
            currentPitch = Mathf.Clamp(currentPitch + pitchDelta * rotationSpeed * Time.deltaTime, minPitch, maxPitch);
        }

        private float ClampYaw(float yaw)
        {
            if (unlimited360Yaw)
            {
                return Mathf.Repeat(yaw + 180f, 360f) - 180f;
            }
            return Mathf.Clamp(yaw, minYaw, maxYaw);
        }

        private void UpdateTurretRotation()
        {
            if (turretType == TurretType.Fixed) return;

            // Calculate target rotation from aim point
            Vector3 targetDir = (aimPoint - turretBase.position).normalized;
            Vector3 localDir = turretBase.parent != null ? 
                turretBase.parent.InverseTransformDirection(targetDir) : targetDir;

            float targetYaw = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
            float targetPitch = -Mathf.Asin(localDir.y) * Mathf.Rad2Deg;

            // Clamp to limits
            targetYaw = ClampYaw(targetYaw);
            targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);

            // Smooth rotation based on turret type
            float speed = turretType == TurretType.Turreted ? trackingSpeed : rotationSpeed;
            
            currentYaw = Mathf.MoveTowardsAngle(currentYaw, targetYaw, speed * Time.deltaTime);
            currentPitch = Mathf.MoveTowards(currentPitch, targetPitch, speed * Time.deltaTime);

            // Apply rotation
            if (turretBase != null)
            {
                turretBase.localRotation = Quaternion.Euler(0, currentYaw, 0);
            }

            if (turretBarrel != null)
            {
                turretBarrel.localRotation = Quaternion.Euler(currentPitch, 0, 0);
            }

            currentAimDirection = turretBarrel.forward;
        }

        /// <summary>
        /// Reset turret to forward position
        /// </summary>
        public void ResetToCenter()
        {
            currentYaw = 0f;
            currentPitch = 0f;
            
            if (turretBase != null)
            {
                turretBase.localRotation = Quaternion.identity;
            }
            if (turretBarrel != null)
            {
                turretBarrel.localRotation = Quaternion.identity;
            }
        }

        #endregion

        #region Firing

        /// <summary>
        /// Start continuous firing
        /// </summary>
        public void StartFiring()
        {
            isFiring = true;
        }

        /// <summary>
        /// Stop continuous firing
        /// </summary>
        public void StopFiring()
        {
            isFiring = false;
        }

        /// <summary>
        /// Fire single shot
        /// </summary>
        public bool Fire()
        {
            if (!CanFire) return false;

            // Get fire point
            Transform firePoint = GetNextFirePoint();
            Vector3 position = firePoint != null ? firePoint.position : turretBarrel.position;
            Vector3 direction = turretBarrel.forward;

            // Create projectile
            if (projectilePrefab != null)
            {
                GameObject projectile = Instantiate(projectilePrefab, position, Quaternion.LookRotation(direction));
                
                // Initialize projectile
                Rigidbody projectileRb = projectile.GetComponent<Rigidbody>();
                if (projectileRb != null)
                {
                    // Inherit ship velocity
                    Rigidbody shipRb = GetComponentInParent<Rigidbody>();
                    Vector3 inheritedVelocity = shipRb != null ? shipRb.linearVelocity : Vector3.zero;
                    projectileRb.linearVelocity = inheritedVelocity + direction * projectileSpeed;
                }

                // Set projectile damage if applicable
                ShipProjectile shipProjectile = projectile.GetComponent<ShipProjectile>();
                if (shipProjectile != null)
                {
                    shipProjectile.SetDamage(damage);
                }
            }

            // Consume ammo
            if (hasAmmo)
            {
                currentAmmo -= ammoPerShot;
                OnAmmoChanged?.Invoke(currentAmmo, maxAmmo);

                if (currentAmmo <= 0)
                {
                    currentAmmo = 0;
                    OnAmmoEmpty?.Invoke();
                }
            }

            // Add heat
            AddHeat(heat);

            // Effects
            PlayFireEffects(firePoint);

            lastFireTime = Time.time;
            OnFire?.Invoke();

            return true;
        }

        private Transform GetNextFirePoint()
        {
            if (firePoints == null || firePoints.Length == 0)
            {
                return turretBarrel;
            }

            Transform point = firePoints[currentFirePointIndex];
            currentFirePointIndex = (currentFirePointIndex + 1) % firePoints.Length;
            return point;
        }

        private void PlayFireEffects(Transform firePoint)
        {
            // Muzzle flash
            if (muzzleFlashParticles != null)
            {
                foreach (var particles in muzzleFlashParticles)
                {
                    if (particles != null)
                    {
                        particles.transform.position = firePoint != null ? firePoint.position : turretBarrel.position;
                        particles.Play();
                    }
                }
            }

            // Fire sound
            if (fireAudioSource != null && fireSound != null)
            {
                fireAudioSource.PlayOneShot(fireSound);
            }
        }

        private void LateUpdate()
        {
            // Handle continuous firing
            if (isFiring && CanFire)
            {
                Fire();
            }
        }

        #endregion

        #region Ammunition

        /// <summary>
        /// Reload ammunition
        /// </summary>
        public void Reload(int amount)
        {
            currentAmmo = Mathf.Min(currentAmmo + amount, maxAmmo);
            OnAmmoChanged?.Invoke(currentAmmo, maxAmmo);
        }

        /// <summary>
        /// Full reload
        /// </summary>
        public void FullReload()
        {
            currentAmmo = maxAmmo;
            OnAmmoChanged?.Invoke(currentAmmo, maxAmmo);
        }

        /// <summary>
        /// Set maximum ammo capacity
        /// </summary>
        public void SetMaxAmmo(int max)
        {
            maxAmmo = Mathf.Max(1, max);
            currentAmmo = Mathf.Min(currentAmmo, maxAmmo);
            OnAmmoChanged?.Invoke(currentAmmo, maxAmmo);
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Set turret type
        /// </summary>
        public void SetTurretType(TurretType type)
        {
            turretType = type;
        }

        /// <summary>
        /// Set rotation limits
        /// </summary>
        public void SetRotationLimits(float minYaw, float maxYaw, float minPitch, float maxPitch)
        {
            this.minYaw = minYaw;
            this.maxYaw = maxYaw;
            this.minPitch = minPitch;
            this.maxPitch = maxPitch;
        }

        /// <summary>
        /// Set weapon stats
        /// </summary>
        public void SetWeaponStats(float fireRate, float projectileSpeed, float damage)
        {
            this.fireRate = Mathf.Max(0.1f, fireRate);
            this.projectileSpeed = Mathf.Max(1f, projectileSpeed);
            this.damage = Mathf.Max(0f, damage);
        }

        /// <summary>
        /// Enable or disable auto-tracking
        /// </summary>
        public void SetAutoTrack(bool enabled)
        {
            autoTrack = enabled;
        }

        #endregion

        private void OnDrawGizmosSelected()
        {
            // Draw firing arc
            Gizmos.color = Color.yellow;
            if (turretBarrel != null)
            {
                Gizmos.DrawRay(turretBarrel.position, turretBarrel.forward * 50f);
            }

            // Draw aim point
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(aimPoint, 2f);

            // Draw tracking range
            Gizmos.color = new Color(0f, 1f, 0f, 0.1f);
            Gizmos.DrawWireSphere(transform.position, maxTrackingRange);

            // Draw rotation limits
            if (turretBase != null)
            {
                Gizmos.color = Color.cyan;
                Vector3 leftLimit = Quaternion.Euler(0, minYaw, 0) * turretBase.parent.forward;
                Vector3 rightLimit = Quaternion.Euler(0, maxYaw, 0) * turretBase.parent.forward;
                Gizmos.DrawRay(turretBase.position, leftLimit * 30f);
                Gizmos.DrawRay(turretBase.position, rightLimit * 30f);
            }
        }
    }

    /// <summary>
    /// Type of turret mount
    /// </summary>
    public enum TurretType
    {
        /// <summary>
        /// Fixed forward-facing weapon
        /// </summary>
        Fixed,

        /// <summary>
        /// Gimballed weapon with limited tracking
        /// </summary>
        Gimballed,

        /// <summary>
        /// Full turret with wide rotation arc
        /// </summary>
        Turreted
    }
}
