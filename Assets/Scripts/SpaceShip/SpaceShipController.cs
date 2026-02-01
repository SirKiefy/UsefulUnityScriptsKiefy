using UnityEngine;

namespace UsefulScripts.SpaceShip
{
    /// <summary>
    /// Realistic space ship movement controller with 6 degrees of freedom.
    /// Inspired by Elite Dangerous, Star Citizen, and No Man's Sky physics.
    /// Features Newtonian physics with optional flight assist.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class SpaceShipController : MonoBehaviour
    {
        [Header("Thrust Settings")]
        [SerializeField] private float mainThrustForce = 50000f;
        [SerializeField] private float reverseThrustForce = 25000f;
        [SerializeField] private float lateralThrustForce = 30000f;
        [SerializeField] private float verticalThrustForce = 30000f;
        [SerializeField] private float boostMultiplier = 2f;
        [SerializeField] private float boostDuration = 5f;
        [SerializeField] private float boostCooldown = 10f;

        [Header("Rotation Settings")]
        [SerializeField] private float pitchTorque = 5000f;
        [SerializeField] private float yawTorque = 3000f;
        [SerializeField] private float rollTorque = 8000f;
        [SerializeField] private float rotationDampening = 0.95f;

        [Header("Speed Limits")]
        [SerializeField] private float maxNormalSpeed = 300f;
        [SerializeField] private float maxBoostSpeed = 500f;
        [SerializeField] private float maxRotationSpeed = 90f;

        [Header("Flight Assist")]
        [SerializeField] private FlightAssistMode flightAssistMode = FlightAssistMode.Full;
        [SerializeField] private float assistDampening = 3f;
        [SerializeField] private float assistRotationDampening = 5f;

        [Header("Throttle")]
        [SerializeField] private bool useThrottleControl = true;
        [SerializeField] private float throttleSensitivity = 1f;
        [Range(-1f, 1f)]
        [SerializeField] private float currentThrottle = 0f;

        [Header("Mass & Inertia")]
        [SerializeField] private float shipMass = 10000f;
        [SerializeField] private float inertiaTensorMultiplier = 1f;

        [Header("Engine Effects")]
        [SerializeField] private ParticleSystem[] mainEngineParticles;
        [SerializeField] private ParticleSystem[] boostEngineParticles;
        [SerializeField] private float engineParticleIntensityMultiplier = 1f;

        // Components
        private Rigidbody rb;
        private FlightAssistSystem flightAssist;

        // State
        private Vector3 thrustInput;
        private Vector3 rotationInput;
        private bool isBoosting;
        private float boostTimeRemaining;
        private float boostCooldownRemaining;
        private float currentSpeedLimit;
        private bool enginesEnabled = true;

        // Cached values
        private Vector3 localVelocity;
        private Vector3 localAngularVelocity;
        private float currentSpeed;
        private Vector3 thrustDirection;

        // Events
        public event System.Action OnBoostStart;
        public event System.Action OnBoostEnd;
        public event System.Action OnBoostReady;
        public event System.Action<FlightAssistMode> OnFlightAssistChanged;
        public event System.Action OnEnginesDisabled;
        public event System.Action OnEnginesEnabled;
        public event System.Action<float, float> OnSpeedChanged; // current, max

        // Properties
        public float CurrentSpeed => currentSpeed;
        public float MaxSpeed => currentSpeedLimit;
        public float SpeedPercent => currentSpeed / currentSpeedLimit;
        public Vector3 Velocity => rb != null ? rb.linearVelocity : Vector3.zero;
        public Vector3 LocalVelocity => localVelocity;
        public Vector3 AngularVelocity => rb != null ? rb.angularVelocity : Vector3.zero;
        public float Throttle => currentThrottle;
        public bool IsBoosting => isBoosting;
        public float BoostTimeRemaining => boostTimeRemaining;
        public float BoostCooldownRemaining => boostCooldownRemaining;
        public bool CanBoost => boostCooldownRemaining <= 0 && !isBoosting;
        public bool EnginesEnabled => enginesEnabled;
        public FlightAssistMode FlightAssist => flightAssistMode;
        public Vector3 ThrustDirection => thrustDirection;
        public Rigidbody ShipRigidbody => rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            flightAssist = GetComponent<FlightAssistSystem>();
            
            ConfigureRigidbody();
            currentSpeedLimit = maxNormalSpeed;
            boostTimeRemaining = boostDuration;
        }

        private void ConfigureRigidbody()
        {
            rb.mass = shipMass;
            rb.useGravity = false;
            rb.linearDamping = 0f;
            rb.angularDamping = 0f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // Set inertia tensor for realistic rotation
            Vector3 inertiaTensor = new Vector3(
                shipMass * inertiaTensorMultiplier * 0.5f,
                shipMass * inertiaTensorMultiplier * 0.8f,
                shipMass * inertiaTensorMultiplier * 0.3f
            );
            rb.inertiaTensor = inertiaTensor;
        }

        private void Update()
        {
            UpdateBoost();
            UpdateEngineEffects();
            CacheValues();
        }

        private void FixedUpdate()
        {
            if (!enginesEnabled) return;

            ApplyThrust();
            ApplyRotation();
            ApplyFlightAssist();
            ClampSpeed();
        }

        private void CacheValues()
        {
            localVelocity = transform.InverseTransformDirection(rb.linearVelocity);
            localAngularVelocity = transform.InverseTransformDirection(rb.angularVelocity);
            float newSpeed = rb.linearVelocity.magnitude;
            
            if (!Mathf.Approximately(newSpeed, currentSpeed))
            {
                currentSpeed = newSpeed;
                OnSpeedChanged?.Invoke(currentSpeed, currentSpeedLimit);
            }
        }

        #region Thrust

        private void ApplyThrust()
        {
            float thrustMultiplier = isBoosting ? boostMultiplier : 1f;
            Vector3 totalForce = Vector3.zero;

            // Main/Reverse thrust (Z axis)
            float forwardThrust = 0f;
            if (useThrottleControl)
            {
                forwardThrust = currentThrottle;
            }
            else
            {
                forwardThrust = thrustInput.z;
            }

            if (forwardThrust > 0)
            {
                totalForce += transform.forward * forwardThrust * mainThrustForce * thrustMultiplier;
            }
            else if (forwardThrust < 0)
            {
                totalForce += transform.forward * forwardThrust * reverseThrustForce * thrustMultiplier;
            }

            // Lateral thrust (X axis - strafe)
            totalForce += transform.right * thrustInput.x * lateralThrustForce * thrustMultiplier;

            // Vertical thrust (Y axis)
            totalForce += transform.up * thrustInput.y * verticalThrustForce * thrustMultiplier;

            rb.AddForce(totalForce, ForceMode.Force);

            // Store thrust direction for effects
            thrustDirection = totalForce.normalized;
        }

        private void ApplyRotation()
        {
            Vector3 torque = Vector3.zero;

            // Pitch (X axis rotation)
            torque += transform.right * -rotationInput.x * pitchTorque;

            // Yaw (Y axis rotation)
            torque += transform.up * rotationInput.y * yawTorque;

            // Roll (Z axis rotation)
            torque += transform.forward * -rotationInput.z * rollTorque;

            rb.AddTorque(torque, ForceMode.Force);

            // Apply rotation dampening when no input
            if (rotationInput.magnitude < 0.1f)
            {
                rb.angularVelocity *= rotationDampening;
            }

            // Clamp rotation speed
            if (rb.angularVelocity.magnitude > maxRotationSpeed * Mathf.Deg2Rad)
            {
                rb.angularVelocity = rb.angularVelocity.normalized * maxRotationSpeed * Mathf.Deg2Rad;
            }
        }

        #endregion

        #region Flight Assist

        private void ApplyFlightAssist()
        {
            if (flightAssist != null)
            {
                flightAssist.ProcessFlightAssist(rb, thrustInput, rotationInput, currentSpeedLimit);
                return;
            }

            // Built-in flight assist if no FlightAssistSystem component
            switch (flightAssistMode)
            {
                case FlightAssistMode.Full:
                    ApplyFullAssist();
                    break;
                case FlightAssistMode.Rotational:
                    ApplyRotationalAssist();
                    break;
                case FlightAssistMode.Off:
                    // Pure Newtonian - no assist
                    break;
            }
        }

        private void ApplyFullAssist()
        {
            // Dampen lateral and vertical velocity when not actively thrusting
            if (Mathf.Abs(thrustInput.x) < 0.1f)
            {
                float lateralDamp = Mathf.Exp(-assistDampening * Time.fixedDeltaTime);
                Vector3 lateralVel = Vector3.Project(rb.linearVelocity, transform.right);
                rb.linearVelocity -= lateralVel * (1f - lateralDamp);
            }

            if (Mathf.Abs(thrustInput.y) < 0.1f)
            {
                float verticalDamp = Mathf.Exp(-assistDampening * Time.fixedDeltaTime);
                Vector3 verticalVel = Vector3.Project(rb.linearVelocity, transform.up);
                rb.linearVelocity -= verticalVel * (1f - verticalDamp);
            }

            ApplyRotationalAssist();
        }

        private void ApplyRotationalAssist()
        {
            // Dampen rotation when not actively rotating
            if (rotationInput.magnitude < 0.1f)
            {
                float rotDamp = Mathf.Exp(-assistRotationDampening * Time.fixedDeltaTime);
                rb.angularVelocity *= rotDamp;
            }
        }

        #endregion

        #region Speed Management

        private void ClampSpeed()
        {
            if (rb.linearVelocity.magnitude > currentSpeedLimit)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * currentSpeedLimit;
            }
        }

        #endregion

        #region Boost

        private void UpdateBoost()
        {
            if (isBoosting)
            {
                boostTimeRemaining -= Time.deltaTime;
                if (boostTimeRemaining <= 0)
                {
                    EndBoost();
                }
            }
            else if (boostCooldownRemaining > 0)
            {
                boostCooldownRemaining -= Time.deltaTime;
                if (boostCooldownRemaining <= 0)
                {
                    boostCooldownRemaining = 0;
                    boostTimeRemaining = boostDuration;
                    OnBoostReady?.Invoke();
                }
            }
        }

        /// <summary>
        /// Activate boost if available
        /// </summary>
        public void StartBoost()
        {
            if (!CanBoost || !enginesEnabled) return;

            isBoosting = true;
            currentSpeedLimit = maxBoostSpeed;
            OnBoostStart?.Invoke();

            // Activate boost particles
            if (boostEngineParticles != null)
            {
                foreach (var particles in boostEngineParticles)
                {
                    if (particles != null) particles.Play();
                }
            }
        }

        /// <summary>
        /// Deactivate boost
        /// </summary>
        public void EndBoost()
        {
            if (!isBoosting) return;

            isBoosting = false;
            currentSpeedLimit = maxNormalSpeed;
            boostCooldownRemaining = boostCooldown;
            OnBoostEnd?.Invoke();

            // Deactivate boost particles
            if (boostEngineParticles != null)
            {
                foreach (var particles in boostEngineParticles)
                {
                    if (particles != null) particles.Stop();
                }
            }
        }

        #endregion

        #region Engine Effects

        private void UpdateEngineEffects()
        {
            if (mainEngineParticles == null) return;

            float thrustMagnitude = Mathf.Abs(useThrottleControl ? currentThrottle : thrustInput.z);
            float intensity = thrustMagnitude * engineParticleIntensityMultiplier * (isBoosting ? boostMultiplier : 1f);

            foreach (var particles in mainEngineParticles)
            {
                if (particles == null) continue;

                var emission = particles.emission;
                emission.rateOverTimeMultiplier = intensity * 50f;

                var main = particles.main;
                main.startSpeedMultiplier = intensity * 10f;
            }
        }

        #endregion

        #region Input Methods

        /// <summary>
        /// Set thrust input (X: strafe, Y: vertical, Z: forward/back)
        /// Values should be in range -1 to 1
        /// </summary>
        public void SetThrustInput(Vector3 input)
        {
            thrustInput = Vector3.ClampMagnitude(input, 1f);
        }

        /// <summary>
        /// Set thrust input from separate axes
        /// </summary>
        public void SetThrustInput(float strafe, float vertical, float forward)
        {
            thrustInput = new Vector3(
                Mathf.Clamp(strafe, -1f, 1f),
                Mathf.Clamp(vertical, -1f, 1f),
                Mathf.Clamp(forward, -1f, 1f)
            );
        }

        /// <summary>
        /// Set rotation input (X: pitch, Y: yaw, Z: roll)
        /// Values should be in range -1 to 1
        /// </summary>
        public void SetRotationInput(Vector3 input)
        {
            rotationInput = Vector3.ClampMagnitude(input, 1f);
        }

        /// <summary>
        /// Set rotation input from separate axes
        /// </summary>
        public void SetRotationInput(float pitch, float yaw, float roll)
        {
            rotationInput = new Vector3(
                Mathf.Clamp(pitch, -1f, 1f),
                Mathf.Clamp(yaw, -1f, 1f),
                Mathf.Clamp(roll, -1f, 1f)
            );
        }

        /// <summary>
        /// Set throttle value (-1 to 1)
        /// </summary>
        public void SetThrottle(float value)
        {
            currentThrottle = Mathf.Clamp(value, -1f, 1f);
        }

        /// <summary>
        /// Adjust throttle by delta
        /// </summary>
        public void AdjustThrottle(float delta)
        {
            currentThrottle = Mathf.Clamp(currentThrottle + delta * throttleSensitivity * Time.deltaTime, -1f, 1f);
        }

        /// <summary>
        /// Set throttle to specific preset
        /// </summary>
        public void SetThrottlePreset(ThrottlePreset preset)
        {
            switch (preset)
            {
                case ThrottlePreset.Zero:
                    currentThrottle = 0f;
                    break;
                case ThrottlePreset.Quarter:
                    currentThrottle = 0.25f;
                    break;
                case ThrottlePreset.Half:
                    currentThrottle = 0.5f;
                    break;
                case ThrottlePreset.ThreeQuarter:
                    currentThrottle = 0.75f;
                    break;
                case ThrottlePreset.Full:
                    currentThrottle = 1f;
                    break;
                case ThrottlePreset.FullReverse:
                    currentThrottle = -1f;
                    break;
            }
        }

        #endregion

        #region Flight Assist Control

        /// <summary>
        /// Set flight assist mode
        /// </summary>
        public void SetFlightAssist(FlightAssistMode mode)
        {
            if (flightAssistMode != mode)
            {
                flightAssistMode = mode;
                OnFlightAssistChanged?.Invoke(mode);
            }
        }

        /// <summary>
        /// Cycle through flight assist modes
        /// </summary>
        public void CycleFlightAssist()
        {
            int next = ((int)flightAssistMode + 1) % 3;
            SetFlightAssist((FlightAssistMode)next);
        }

        #endregion

        #region Engine Control

        /// <summary>
        /// Enable/disable engines
        /// </summary>
        public void SetEnginesEnabled(bool enabled)
        {
            if (enginesEnabled == enabled) return;

            enginesEnabled = enabled;

            if (enabled)
            {
                OnEnginesEnabled?.Invoke();
            }
            else
            {
                if (isBoosting) EndBoost();
                OnEnginesDisabled?.Invoke();
            }
        }

        /// <summary>
        /// Toggle engines
        /// </summary>
        public void ToggleEngines()
        {
            SetEnginesEnabled(!enginesEnabled);
        }

        #endregion

        #region Physics Helpers

        /// <summary>
        /// Apply external force to ship
        /// </summary>
        public void ApplyExternalForce(Vector3 force, ForceMode mode = ForceMode.Force)
        {
            rb.AddForce(force, mode);
        }

        /// <summary>
        /// Apply external torque to ship
        /// </summary>
        public void ApplyExternalTorque(Vector3 torque, ForceMode mode = ForceMode.Force)
        {
            rb.AddTorque(torque, mode);
        }

        /// <summary>
        /// Kill all velocity (emergency stop)
        /// </summary>
        public void FullStop()
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            currentThrottle = 0f;
        }

        /// <summary>
        /// Get velocity relative to another transform
        /// </summary>
        public Vector3 GetRelativeVelocity(Transform reference)
        {
            if (reference == null) return rb.linearVelocity;
            
            Rigidbody refRb = reference.GetComponent<Rigidbody>();
            if (refRb != null)
            {
                return rb.linearVelocity - refRb.linearVelocity;
            }
            return rb.linearVelocity;
        }

        /// <summary>
        /// Match velocity with target
        /// </summary>
        public void MatchVelocity(Rigidbody target)
        {
            if (target == null) return;
            rb.linearVelocity = target.linearVelocity;
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Update ship mass (affects handling)
        /// </summary>
        public void SetMass(float mass)
        {
            shipMass = Mathf.Max(100f, mass);
            ConfigureRigidbody();
        }

        /// <summary>
        /// Update maximum speeds
        /// </summary>
        public void SetSpeedLimits(float normalSpeed, float boostSpeed)
        {
            maxNormalSpeed = Mathf.Max(10f, normalSpeed);
            maxBoostSpeed = Mathf.Max(maxNormalSpeed, boostSpeed);
            currentSpeedLimit = isBoosting ? maxBoostSpeed : maxNormalSpeed;
        }

        /// <summary>
        /// Update thrust power
        /// </summary>
        public void SetThrustPower(float main, float reverse, float lateral, float vertical)
        {
            mainThrustForce = Mathf.Max(0f, main);
            reverseThrustForce = Mathf.Max(0f, reverse);
            lateralThrustForce = Mathf.Max(0f, lateral);
            verticalThrustForce = Mathf.Max(0f, vertical);
        }

        #endregion

        private void OnDrawGizmosSelected()
        {
            if (rb == null) return;

            // Draw velocity vector
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, rb.linearVelocity.normalized * 5f);

            // Draw thrust direction
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, thrustDirection * 3f);

            // Draw facing direction
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, transform.forward * 10f);

            // Draw speed limit sphere
            Gizmos.color = new Color(1f, 1f, 0f, 0.1f);
            Gizmos.DrawWireSphere(transform.position, currentSpeedLimit * 0.1f);
        }
    }

    /// <summary>
    /// Flight assist modes for space ship movement
    /// </summary>
    public enum FlightAssistMode
    {
        /// <summary>
        /// Full flight assist - dampens all unwanted movement
        /// </summary>
        Full,

        /// <summary>
        /// Only rotation is dampened, velocity is Newtonian
        /// </summary>
        Rotational,

        /// <summary>
        /// No assist - pure Newtonian physics
        /// </summary>
        Off
    }

    /// <summary>
    /// Throttle presets for quick speed control
    /// </summary>
    public enum ThrottlePreset
    {
        Zero,
        Quarter,
        Half,
        ThreeQuarter,
        Full,
        FullReverse
    }
}
