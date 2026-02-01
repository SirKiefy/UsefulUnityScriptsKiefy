using UnityEngine;
using System.Collections;

namespace UsefulScripts.SpaceShip
{
    /// <summary>
    /// Warp drive system with multiple FTL travel modes.
    /// Features supercruise, hyperspace jumps, and quantum travel.
    /// Inspired by Elite Dangerous, Star Citizen, and No Man's Sky.
    /// </summary>
    [RequireComponent(typeof(SpaceShipController))]
    public class WarpDriveSystem : MonoBehaviour
    {
        [Header("Supercruise Settings")]
        [SerializeField] private bool enableSupercruise = true;
        [SerializeField] private float supercruiseMinSpeed = 30f; // km/s
        [SerializeField] private float supercruiseMaxSpeed = 2001f; // c (speed of light multiplier)
        [SerializeField] private float supercruiseAcceleration = 50f;
        [SerializeField] private float supercruiseDeceleration = 100f;
        [SerializeField] private float supercruiseChargeTime = 5f;
        [SerializeField] private float supercruiseCooldown = 10f;

        [Header("Hyperspace Jump Settings")]
        [SerializeField] private bool enableHyperspaceJump = true;
        [SerializeField] private float hyperspaceRange = 50f; // Light years
        [SerializeField] private float hyperspaceChargeTime = 15f;
        [SerializeField] private float hyperspaceCooldown = 30f;
        [SerializeField] private float hyperspaceJumpDuration = 10f;
        [SerializeField] private float hyperspaceFuelPerLightYear = 1f;

        [Header("Quantum Travel Settings")]
        [SerializeField] private bool enableQuantumTravel = true;
        [SerializeField] private float quantumSpeed = 0.2f; // Speed of light multiplier
        [SerializeField] private float quantumChargeTime = 3f;
        [SerializeField] private float quantumCooldown = 5f;
        [SerializeField] private float quantumMinDistance = 10000f;
        [SerializeField] private float quantumFuelPerSecond = 0.5f;

        [Header("Energy Requirements")]
        [SerializeField] private float supercruisePowerDrain = 10f;
        [SerializeField] private float hyperspacePowerDrain = 50f;
        [SerializeField] private float quantumPowerDrain = 25f;
        [SerializeField] private float fuelCapacity = 100f;
        [SerializeField] private float currentFuel = 100f;

        [Header("Safety Systems")]
        [SerializeField] private float emergencyDropSpeed = 1000f; // Normal space speed that triggers emergency drop
        [SerializeField] private float massLockRange = 5000f;
        [SerializeField] private LayerMask massLockMask;
        [SerializeField] private bool enableEmergencyDrop = true;

        [Header("Effects")]
        [SerializeField] private ParticleSystem warpChargeParticles;
        [SerializeField] private ParticleSystem warpTunnelParticles;
        [SerializeField] private ParticleSystem hyperspaceParticles;
        [SerializeField] private float fovDuringWarp = 90f;
        [SerializeField] private float normalFov = 60f;

        // Components
        private SpaceShipController shipController;
        private Camera shipCamera;

        // State
        private WarpState currentState = WarpState.Normal;
        private WarpMode currentMode = WarpMode.None;
        private float chargeProgress;
        private float cooldownRemaining;
        private float currentWarpSpeed;
        private float targetWarpSpeed;
        private Vector3 hyperspaceDestination;
        private Transform quantumTarget;
        private bool isCharging;
        private bool isMassLocked;
        private float warpStartTime;

        // Events
        public event System.Action<WarpState> OnWarpStateChanged;
        public event System.Action<WarpMode> OnWarpModeChanged;
        public event System.Action<float> OnChargeProgress;
        public event System.Action OnWarpEngaged;
        public event System.Action OnWarpDisengaged;
        public event System.Action OnEmergencyDrop;
        public event System.Action OnHyperspaceJumpStart;
        public event System.Action OnHyperspaceJumpComplete;
        public event System.Action<float, float> OnFuelChanged; // current, max
        public event System.Action OnMassLockStart;
        public event System.Action OnMassLockEnd;
        public event System.Action OnInsufficientFuel;

        // Properties
        public WarpState CurrentState => currentState;
        public WarpMode CurrentMode => currentMode;
        public float ChargeProgress => chargeProgress;
        public float CooldownRemaining => cooldownRemaining;
        public float CurrentWarpSpeed => currentWarpSpeed;
        public float CurrentFuel => currentFuel;
        public float FuelCapacity => fuelCapacity;
        public float FuelPercent => currentFuel / fuelCapacity;
        public bool IsCharging => isCharging;
        public bool IsMassLocked => isMassLocked;
        public bool IsInWarp => currentState == WarpState.Supercruise || currentState == WarpState.Hyperspace || currentState == WarpState.QuantumTravel;
        public bool CanEngageSupercruise => enableSupercruise && currentState == WarpState.Normal && cooldownRemaining <= 0 && !isMassLocked;
        public bool CanEngageHyperspace => enableHyperspaceJump && currentState == WarpState.Normal && cooldownRemaining <= 0 && !isMassLocked;
        public bool CanEngageQuantum => enableQuantumTravel && currentState == WarpState.Normal && cooldownRemaining <= 0 && quantumTarget != null;

        private void Awake()
        {
            shipController = GetComponent<SpaceShipController>();
            shipCamera = Camera.main;
        }

        private void Update()
        {
            UpdateCooldown();
            UpdateMassLock();
            UpdateWarpState();
        }

        private void UpdateCooldown()
        {
            if (cooldownRemaining > 0)
            {
                cooldownRemaining -= Time.deltaTime;
            }
        }

        private void UpdateMassLock()
        {
            bool wasMassLocked = isMassLocked;
            isMassLocked = Physics.CheckSphere(transform.position, massLockRange, massLockMask);

            if (isMassLocked && !wasMassLocked)
            {
                OnMassLockStart?.Invoke();

                // Emergency drop if in supercruise
                if (enableEmergencyDrop && currentState == WarpState.Supercruise)
                {
                    EmergencyDrop();
                }
            }
            else if (!isMassLocked && wasMassLocked)
            {
                OnMassLockEnd?.Invoke();
            }
        }

        private void UpdateWarpState()
        {
            switch (currentState)
            {
                case WarpState.Charging:
                    UpdateCharging();
                    break;
                case WarpState.Supercruise:
                    UpdateSupercruise();
                    break;
                case WarpState.Hyperspace:
                    UpdateHyperspace();
                    break;
                case WarpState.QuantumTravel:
                    UpdateQuantumTravel();
                    break;
            }
        }

        #region Charging

        private void UpdateCharging()
        {
            float chargeTime = GetChargeTimeForMode(currentMode);
            chargeProgress += Time.deltaTime / chargeTime;
            OnChargeProgress?.Invoke(chargeProgress);

            if (chargeProgress >= 1f)
            {
                CompleteCharge();
            }

            // Update charge effects
            if (warpChargeParticles != null && !warpChargeParticles.isPlaying)
            {
                warpChargeParticles.Play();
            }
        }

        private float GetChargeTimeForMode(WarpMode mode)
        {
            switch (mode)
            {
                case WarpMode.Supercruise: return supercruiseChargeTime;
                case WarpMode.Hyperspace: return hyperspaceChargeTime;
                case WarpMode.Quantum: return quantumChargeTime;
                default: return 1f;
            }
        }

        private void CompleteCharge()
        {
            chargeProgress = 0f;
            isCharging = false;

            if (warpChargeParticles != null)
            {
                warpChargeParticles.Stop();
            }

            switch (currentMode)
            {
                case WarpMode.Supercruise:
                    EngageSupercruise();
                    break;
                case WarpMode.Hyperspace:
                    EngageHyperspace();
                    break;
                case WarpMode.Quantum:
                    EngageQuantumTravel();
                    break;
            }
        }

        /// <summary>
        /// Cancel current warp charge
        /// </summary>
        public void CancelCharge()
        {
            if (!isCharging) return;

            isCharging = false;
            chargeProgress = 0f;
            currentMode = WarpMode.None;
            SetState(WarpState.Normal);

            if (warpChargeParticles != null)
            {
                warpChargeParticles.Stop();
            }
        }

        #endregion

        #region Supercruise

        /// <summary>
        /// Start charging supercruise
        /// </summary>
        public void StartSupercruiseCharge()
        {
            if (!CanEngageSupercruise) return;

            isCharging = true;
            chargeProgress = 0f;
            currentMode = WarpMode.Supercruise;
            SetState(WarpState.Charging);
        }

        private void EngageSupercruise()
        {
            SetState(WarpState.Supercruise);
            currentWarpSpeed = supercruiseMinSpeed;
            targetWarpSpeed = supercruiseMaxSpeed;
            warpStartTime = Time.time;

            // Disable normal ship controls
            shipController.SetEnginesEnabled(false);

            if (warpTunnelParticles != null)
            {
                warpTunnelParticles.Play();
            }

            OnWarpEngaged?.Invoke();
        }

        private void UpdateSupercruise()
        {
            // Accelerate to target speed
            if (currentWarpSpeed < targetWarpSpeed)
            {
                currentWarpSpeed += supercruiseAcceleration * Time.deltaTime;
                currentWarpSpeed = Mathf.Min(currentWarpSpeed, targetWarpSpeed);
            }
            else if (currentWarpSpeed > targetWarpSpeed)
            {
                currentWarpSpeed -= supercruiseDeceleration * Time.deltaTime;
                currentWarpSpeed = Mathf.Max(currentWarpSpeed, targetWarpSpeed);
            }

            // Move ship in supercruise
            transform.position += transform.forward * currentWarpSpeed * Time.deltaTime;

            // Drain fuel
            float fuelDrain = supercruisePowerDrain * Time.deltaTime;
            ConsumeFuel(fuelDrain);

            // Update camera FOV
            if (shipCamera != null)
            {
                float speedRatio = currentWarpSpeed / supercruiseMaxSpeed;
                shipCamera.fieldOfView = Mathf.Lerp(normalFov, fovDuringWarp, speedRatio);
            }
        }

        /// <summary>
        /// Set target supercruise speed
        /// </summary>
        public void SetSupercruiseThrottle(float throttle)
        {
            if (currentState != WarpState.Supercruise) return;
            targetWarpSpeed = Mathf.Lerp(supercruiseMinSpeed, supercruiseMaxSpeed, Mathf.Clamp01(throttle));
        }

        /// <summary>
        /// Disengage supercruise
        /// </summary>
        public void DisengageSupercruise()
        {
            if (currentState != WarpState.Supercruise) return;

            SetState(WarpState.Normal);
            currentMode = WarpMode.None;
            currentWarpSpeed = 0f;
            cooldownRemaining = supercruiseCooldown;

            shipController.SetEnginesEnabled(true);
            shipController.FullStop();

            if (warpTunnelParticles != null)
            {
                warpTunnelParticles.Stop();
            }

            if (shipCamera != null)
            {
                shipCamera.fieldOfView = normalFov;
            }

            OnWarpDisengaged?.Invoke();
        }

        /// <summary>
        /// Emergency drop from supercruise
        /// </summary>
        public void EmergencyDrop()
        {
            if (currentState != WarpState.Supercruise) return;

            DisengageSupercruise();
            cooldownRemaining = supercruiseCooldown * 2f; // Longer cooldown for emergency drop

            OnEmergencyDrop?.Invoke();
        }

        #endregion

        #region Hyperspace

        /// <summary>
        /// Start charging hyperspace jump to destination
        /// </summary>
        public void StartHyperspaceCharge(Vector3 destination)
        {
            if (!CanEngageHyperspace) return;

            float distance = Vector3.Distance(transform.position, destination);
            float requiredFuel = distance * hyperspaceFuelPerLightYear;

            if (currentFuel < requiredFuel)
            {
                OnInsufficientFuel?.Invoke();
                return;
            }

            hyperspaceDestination = destination;
            isCharging = true;
            chargeProgress = 0f;
            currentMode = WarpMode.Hyperspace;
            SetState(WarpState.Charging);
        }

        private void EngageHyperspace()
        {
            SetState(WarpState.Hyperspace);
            warpStartTime = Time.time;

            shipController.SetEnginesEnabled(false);

            if (hyperspaceParticles != null)
            {
                hyperspaceParticles.Play();
            }

            OnHyperspaceJumpStart?.Invoke();
            StartCoroutine(HyperspaceJumpSequence());
        }

        private IEnumerator HyperspaceJumpSequence()
        {
            float elapsedTime = 0f;
            Vector3 startPosition = transform.position;

            // Consume fuel
            float distance = Vector3.Distance(startPosition, hyperspaceDestination);
            float requiredFuel = distance * hyperspaceFuelPerLightYear;
            ConsumeFuel(requiredFuel);

            while (elapsedTime < hyperspaceJumpDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / hyperspaceJumpDuration;

                // Update position (lerp through hyperspace from start to destination)
                transform.position = Vector3.Lerp(startPosition, hyperspaceDestination, t * t);

                // Camera effects
                if (shipCamera != null)
                {
                    shipCamera.fieldOfView = Mathf.Lerp(normalFov, fovDuringWarp * 1.5f, Mathf.Sin(t * Mathf.PI));
                }

                yield return null;
            }

            // Complete jump
            transform.position = hyperspaceDestination;
            CompleteHyperspaceJump();
        }

        private void CompleteHyperspaceJump()
        {
            SetState(WarpState.Normal);
            currentMode = WarpMode.None;
            cooldownRemaining = hyperspaceCooldown;

            shipController.SetEnginesEnabled(true);
            shipController.FullStop();

            if (hyperspaceParticles != null)
            {
                hyperspaceParticles.Stop();
            }

            if (shipCamera != null)
            {
                shipCamera.fieldOfView = normalFov;
            }

            OnHyperspaceJumpComplete?.Invoke();
        }

        private void UpdateHyperspace()
        {
            // Hyperspace is handled by coroutine
        }

        #endregion

        #region Quantum Travel

        /// <summary>
        /// Set quantum travel target
        /// </summary>
        public void SetQuantumTarget(Transform target)
        {
            quantumTarget = target;
        }

        /// <summary>
        /// Clear quantum travel target
        /// </summary>
        public void ClearQuantumTarget()
        {
            quantumTarget = null;
        }

        /// <summary>
        /// Start charging quantum travel
        /// </summary>
        public void StartQuantumCharge()
        {
            if (!CanEngageQuantum) return;

            float distance = Vector3.Distance(transform.position, quantumTarget.position);
            if (distance < quantumMinDistance)
            {
                return; // Too close for quantum travel
            }

            isCharging = true;
            chargeProgress = 0f;
            currentMode = WarpMode.Quantum;
            SetState(WarpState.Charging);
        }

        private void EngageQuantumTravel()
        {
            SetState(WarpState.QuantumTravel);
            warpStartTime = Time.time;
            currentWarpSpeed = quantumSpeed * 299792f; // km/s (fraction of light speed)

            shipController.SetEnginesEnabled(false);

            if (warpTunnelParticles != null)
            {
                warpTunnelParticles.Play();
            }

            OnWarpEngaged?.Invoke();
        }

        private void UpdateQuantumTravel()
        {
            if (quantumTarget == null)
            {
                DisengageQuantumTravel();
                return;
            }

            Vector3 direction = (quantumTarget.position - transform.position).normalized;
            float distance = Vector3.Distance(transform.position, quantumTarget.position);

            // Orient toward target
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 5f * Time.deltaTime);

            // Move toward target
            transform.position += direction * currentWarpSpeed * Time.deltaTime;

            // Drain fuel
            float fuelDrain = quantumFuelPerSecond * Time.deltaTime;
            if (!ConsumeFuel(fuelDrain))
            {
                DisengageQuantumTravel();
                return;
            }

            // Check if arrived
            if (distance < quantumMinDistance * 0.1f)
            {
                DisengageQuantumTravel();
            }

            // Camera effects
            if (shipCamera != null)
            {
                shipCamera.fieldOfView = fovDuringWarp;
            }
        }

        /// <summary>
        /// Disengage quantum travel
        /// </summary>
        public void DisengageQuantumTravel()
        {
            if (currentState != WarpState.QuantumTravel) return;

            SetState(WarpState.Normal);
            currentMode = WarpMode.None;
            currentWarpSpeed = 0f;
            cooldownRemaining = quantumCooldown;

            shipController.SetEnginesEnabled(true);
            shipController.FullStop();

            if (warpTunnelParticles != null)
            {
                warpTunnelParticles.Stop();
            }

            if (shipCamera != null)
            {
                shipCamera.fieldOfView = normalFov;
            }

            OnWarpDisengaged?.Invoke();
        }

        #endregion

        #region State Management

        private void SetState(WarpState newState)
        {
            if (currentState != newState)
            {
                currentState = newState;
                OnWarpStateChanged?.Invoke(newState);
            }
        }

        /// <summary>
        /// Disengage any active warp mode
        /// </summary>
        public void DisengageWarp()
        {
            switch (currentState)
            {
                case WarpState.Charging:
                    CancelCharge();
                    break;
                case WarpState.Supercruise:
                    DisengageSupercruise();
                    break;
                case WarpState.QuantumTravel:
                    DisengageQuantumTravel();
                    break;
            }
        }

        #endregion

        #region Fuel Management

        /// <summary>
        /// Consume fuel
        /// </summary>
        public bool ConsumeFuel(float amount)
        {
            if (currentFuel < amount)
            {
                OnInsufficientFuel?.Invoke();
                return false;
            }

            currentFuel -= amount;
            OnFuelChanged?.Invoke(currentFuel, fuelCapacity);
            return true;
        }

        /// <summary>
        /// Add fuel
        /// </summary>
        public void AddFuel(float amount)
        {
            currentFuel = Mathf.Min(currentFuel + amount, fuelCapacity);
            OnFuelChanged?.Invoke(currentFuel, fuelCapacity);
        }

        /// <summary>
        /// Refuel to full
        /// </summary>
        public void Refuel()
        {
            currentFuel = fuelCapacity;
            OnFuelChanged?.Invoke(currentFuel, fuelCapacity);
        }

        /// <summary>
        /// Set fuel capacity
        /// </summary>
        public void SetFuelCapacity(float capacity)
        {
            fuelCapacity = Mathf.Max(1f, capacity);
            currentFuel = Mathf.Min(currentFuel, fuelCapacity);
            OnFuelChanged?.Invoke(currentFuel, fuelCapacity);
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Configure supercruise parameters
        /// </summary>
        public void ConfigureSupercruise(float minSpeed, float maxSpeed, float acceleration, float chargeTime)
        {
            supercruiseMinSpeed = Mathf.Max(1f, minSpeed);
            supercruiseMaxSpeed = Mathf.Max(supercruiseMinSpeed, maxSpeed);
            supercruiseAcceleration = Mathf.Max(1f, acceleration);
            supercruiseChargeTime = Mathf.Max(0.1f, chargeTime);
        }

        /// <summary>
        /// Configure hyperspace parameters
        /// </summary>
        public void ConfigureHyperspace(float range, float chargeTime, float jumpDuration)
        {
            hyperspaceRange = Mathf.Max(1f, range);
            hyperspaceChargeTime = Mathf.Max(0.1f, chargeTime);
            hyperspaceJumpDuration = Mathf.Max(0.1f, jumpDuration);
        }

        /// <summary>
        /// Configure quantum travel parameters
        /// </summary>
        public void ConfigureQuantumTravel(float speed, float chargeTime, float minDistance)
        {
            quantumSpeed = Mathf.Clamp(speed, 0.01f, 1f);
            quantumChargeTime = Mathf.Max(0.1f, chargeTime);
            quantumMinDistance = Mathf.Max(100f, minDistance);
        }

        #endregion

        private void OnDrawGizmosSelected()
        {
            // Mass lock range
            Gizmos.color = new Color(1f, 0f, 0f, 0.1f);
            Gizmos.DrawWireSphere(transform.position, massLockRange);

            // Quantum target
            if (quantumTarget != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, quantumTarget.position);
                Gizmos.DrawWireSphere(quantumTarget.position, 100f);
            }

            // Hyperspace destination
            if (currentState == WarpState.Hyperspace)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(transform.position, hyperspaceDestination);
                Gizmos.DrawWireSphere(hyperspaceDestination, 500f);
            }
        }
    }

    /// <summary>
    /// Current warp drive state
    /// </summary>
    public enum WarpState
    {
        Normal,
        Charging,
        Supercruise,
        Hyperspace,
        QuantumTravel
    }

    /// <summary>
    /// Active warp mode
    /// </summary>
    public enum WarpMode
    {
        None,
        Supercruise,
        Hyperspace,
        Quantum
    }
}
