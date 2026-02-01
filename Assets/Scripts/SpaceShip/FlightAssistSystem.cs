using UnityEngine;

namespace UsefulScripts.SpaceShip
{
    /// <summary>
    /// Advanced flight assist system with multiple modes.
    /// Provides automatic velocity matching, drift cancellation, and approach assist.
    /// Inspired by Elite Dangerous flight assist mechanics.
    /// </summary>
    [RequireComponent(typeof(SpaceShipController))]
    public class FlightAssistSystem : MonoBehaviour
    {
        [Header("Linear Assist")]
        [SerializeField] private float linearDampening = 3f;
        [SerializeField] private float linearDeadzone = 0.1f;
        [SerializeField] private bool dampenForwardVelocity = false;

        [Header("Rotational Assist")]
        [SerializeField] private float rotationalDampening = 5f;
        [SerializeField] private float rotationalDeadzone = 0.1f;

        [Header("Velocity Matching")]
        [SerializeField] private bool enableVelocityMatching = true;
        [SerializeField] private float velocityMatchStrength = 2f;
        [SerializeField] private float velocityMatchThreshold = 5f;

        [Header("Approach Assist")]
        [SerializeField] private bool enableApproachAssist = true;
        [SerializeField] private float approachSlowdownDistance = 1000f;
        [SerializeField] private float approachMinSpeed = 50f;

        [Header("Drift Cancellation")]
        [SerializeField] private bool enableDriftCancellation = true;
        [SerializeField] private float driftCancellationStrength = 2f;
        [SerializeField] private float maxDriftAngle = 30f;

        [Header("Auto-Orient")]
        [SerializeField] private bool enableAutoOrient = false;
        [SerializeField] private Transform autoOrientTarget;
        [SerializeField] private float autoOrientStrength = 1f;

        // References
        private SpaceShipController shipController;
        private Rigidbody rb;

        // Velocity matching
        private Transform velocityMatchTarget;
        private Rigidbody velocityMatchRigidbody;
        private bool isMatchingVelocity;

        // Approach assist
        private Transform approachTarget;
        private float desiredApproachSpeed;
        private bool isApproaching;

        // State
        private FlightAssistMode currentMode = FlightAssistMode.Full;

        // Events
        public event System.Action<bool> OnVelocityMatchingChanged;
        public event System.Action<bool> OnApproachAssistChanged;
        public event System.Action OnDriftCancelled;
        public event System.Action<float> OnApproachSpeedChanged;

        // Properties
        public bool IsMatchingVelocity => isMatchingVelocity;
        public bool IsApproaching => isApproaching;
        public Transform VelocityMatchTarget => velocityMatchTarget;
        public Transform ApproachTarget => approachTarget;
        public float DesiredApproachSpeed => desiredApproachSpeed;
        public FlightAssistMode CurrentMode => currentMode;

        private void Awake()
        {
            shipController = GetComponent<SpaceShipController>();
            rb = GetComponent<Rigidbody>();
        }

        /// <summary>
        /// Process flight assist based on current mode and inputs
        /// </summary>
        public void ProcessFlightAssist(Rigidbody shipRb, Vector3 thrustInput, Vector3 rotationInput, float maxSpeed)
        {
            rb = shipRb;
            currentMode = shipController.FlightAssist;

            switch (currentMode)
            {
                case FlightAssistMode.Full:
                    ProcessFullAssist(thrustInput, rotationInput, maxSpeed);
                    break;
                case FlightAssistMode.Rotational:
                    ProcessRotationalAssist(rotationInput);
                    break;
                case FlightAssistMode.Off:
                    // No assist
                    break;
            }

            // Additional features that can be active regardless of mode
            if (isMatchingVelocity && enableVelocityMatching)
            {
                ProcessVelocityMatching();
            }

            if (isApproaching && enableApproachAssist)
            {
                ProcessApproachAssist();
            }
        }

        #region Full Assist Mode

        private void ProcessFullAssist(Vector3 thrustInput, Vector3 rotationInput, float maxSpeed)
        {
            Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);

            // Dampen lateral velocity when not strafing
            if (Mathf.Abs(thrustInput.x) < linearDeadzone)
            {
                DampenAxisVelocity(transform.right, linearDampening);
            }

            // Dampen vertical velocity when not using vertical thrust
            if (Mathf.Abs(thrustInput.y) < linearDeadzone)
            {
                DampenAxisVelocity(transform.up, linearDampening);
            }

            // Optionally dampen forward velocity when not thrusting
            if (dampenForwardVelocity && Mathf.Abs(thrustInput.z) < linearDeadzone)
            {
                DampenAxisVelocity(transform.forward, linearDampening * 0.5f);
            }

            // Drift cancellation
            if (enableDriftCancellation)
            {
                ProcessDriftCancellation();
            }

            // Rotational assist
            ProcessRotationalAssist(rotationInput);
        }

        private void DampenAxisVelocity(Vector3 axis, float dampening)
        {
            Vector3 axisVel = Vector3.Project(rb.linearVelocity, axis);
            float dampFactor = Mathf.Exp(-dampening * Time.fixedDeltaTime);
            rb.linearVelocity -= axisVel * (1f - dampFactor);
        }

        #endregion

        #region Rotational Assist

        private void ProcessRotationalAssist(Vector3 rotationInput)
        {
            // Dampen rotation when not actively rotating
            if (rotationInput.magnitude < rotationalDeadzone)
            {
                float rotDamp = Mathf.Exp(-rotationalDampening * Time.fixedDeltaTime);
                rb.angularVelocity *= rotDamp;
            }

            // Auto-orient if enabled
            if (enableAutoOrient && autoOrientTarget != null)
            {
                ProcessAutoOrient();
            }
        }

        private void ProcessAutoOrient()
        {
            Vector3 targetDirection = (autoOrientTarget.position - transform.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            Quaternion deltaRotation = targetRotation * Quaternion.Inverse(transform.rotation);

            deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);

            if (angle > 180f)
            {
                angle -= 360f;
            }

            Vector3 angularVelocityCorrection = axis * (angle * Mathf.Deg2Rad * autoOrientStrength);
            rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, angularVelocityCorrection, autoOrientStrength * Time.fixedDeltaTime);
        }

        #endregion

        #region Drift Cancellation

        private void ProcessDriftCancellation()
        {
            Vector3 velocity = rb.linearVelocity;
            if (velocity.magnitude < 1f) return;

            Vector3 velocityDir = velocity.normalized;
            Vector3 forward = transform.forward;

            float angle = Vector3.Angle(forward, velocityDir);

            // Only cancel drift if angle is within threshold
            if (angle < maxDriftAngle && angle > 5f)
            {
                // Apply counter-force to cancel drift
                Vector3 driftComponent = velocity - Vector3.Project(velocity, forward);
                Vector3 counterForce = -driftComponent * driftCancellationStrength * rb.mass;
                rb.AddForce(counterForce, ForceMode.Force);

                OnDriftCancelled?.Invoke();
            }
        }

        #endregion

        #region Velocity Matching

        /// <summary>
        /// Start matching velocity with a target
        /// </summary>
        public void StartVelocityMatch(Transform target)
        {
            if (target == null) return;

            velocityMatchTarget = target;
            velocityMatchRigidbody = target.GetComponent<Rigidbody>();
            isMatchingVelocity = true;
            OnVelocityMatchingChanged?.Invoke(true);
        }

        /// <summary>
        /// Stop velocity matching
        /// </summary>
        public void StopVelocityMatch()
        {
            velocityMatchTarget = null;
            velocityMatchRigidbody = null;
            isMatchingVelocity = false;
            OnVelocityMatchingChanged?.Invoke(false);
        }

        /// <summary>
        /// Toggle velocity matching
        /// </summary>
        public void ToggleVelocityMatch(Transform target)
        {
            if (isMatchingVelocity)
            {
                StopVelocityMatch();
            }
            else
            {
                StartVelocityMatch(target);
            }
        }

        private void ProcessVelocityMatching()
        {
            if (velocityMatchTarget == null)
            {
                StopVelocityMatch();
                return;
            }

            Vector3 targetVelocity = velocityMatchRigidbody != null ? velocityMatchRigidbody.linearVelocity : Vector3.zero;
            Vector3 velocityDiff = targetVelocity - rb.linearVelocity;

            if (velocityDiff.magnitude > velocityMatchThreshold)
            {
                // Apply corrective force
                Vector3 correctiveForce = velocityDiff.normalized * velocityMatchStrength * rb.mass;
                rb.AddForce(correctiveForce, ForceMode.Force);
            }
            else if (velocityDiff.magnitude < 1f)
            {
                // Snap to target velocity when close enough
                rb.linearVelocity = targetVelocity;
            }
        }

        #endregion

        #region Approach Assist

        /// <summary>
        /// Start approach assist to automatically slow down when approaching a target
        /// </summary>
        public void StartApproachAssist(Transform target, float desiredSpeed = 0f)
        {
            if (target == null) return;

            approachTarget = target;
            desiredApproachSpeed = Mathf.Max(0f, desiredSpeed);
            isApproaching = true;
            OnApproachAssistChanged?.Invoke(true);
        }

        /// <summary>
        /// Stop approach assist
        /// </summary>
        public void StopApproachAssist()
        {
            approachTarget = null;
            isApproaching = false;
            OnApproachAssistChanged?.Invoke(false);
        }

        /// <summary>
        /// Set desired approach speed
        /// </summary>
        public void SetApproachSpeed(float speed)
        {
            desiredApproachSpeed = Mathf.Max(0f, speed);
            OnApproachSpeedChanged?.Invoke(desiredApproachSpeed);
        }

        private void ProcessApproachAssist()
        {
            if (approachTarget == null)
            {
                StopApproachAssist();
                return;
            }

            float distance = Vector3.Distance(transform.position, approachTarget.position);
            float currentSpeed = rb.linearVelocity.magnitude;

            // Calculate desired speed based on distance
            float targetSpeed;
            if (distance < approachSlowdownDistance)
            {
                float t = distance / approachSlowdownDistance;
                targetSpeed = Mathf.Lerp(desiredApproachSpeed, currentSpeed, t);
                targetSpeed = Mathf.Max(targetSpeed, approachMinSpeed);
            }
            else
            {
                return; // Too far, no slowdown needed
            }

            // Apply braking if going too fast
            if (currentSpeed > targetSpeed && currentSpeed > desiredApproachSpeed)
            {
                Vector3 brakingForce = -rb.linearVelocity.normalized * linearDampening * rb.mass;
                rb.AddForce(brakingForce, ForceMode.Force);
            }
        }

        #endregion

        #region Auto-Orient

        /// <summary>
        /// Enable auto-orient toward a target
        /// </summary>
        public void SetAutoOrientTarget(Transform target)
        {
            autoOrientTarget = target;
            enableAutoOrient = target != null;
        }

        /// <summary>
        /// Clear auto-orient target
        /// </summary>
        public void ClearAutoOrient()
        {
            autoOrientTarget = null;
            enableAutoOrient = false;
        }

        /// <summary>
        /// Set auto-orient strength
        /// </summary>
        public void SetAutoOrientStrength(float strength)
        {
            autoOrientStrength = Mathf.Max(0f, strength);
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Configure linear dampening parameters
        /// </summary>
        public void SetLinearDampening(float dampening, float deadzone)
        {
            linearDampening = Mathf.Max(0f, dampening);
            linearDeadzone = Mathf.Clamp01(deadzone);
        }

        /// <summary>
        /// Configure rotational dampening parameters
        /// </summary>
        public void SetRotationalDampening(float dampening, float deadzone)
        {
            rotationalDampening = Mathf.Max(0f, dampening);
            rotationalDeadzone = Mathf.Clamp01(deadzone);
        }

        /// <summary>
        /// Configure drift cancellation
        /// </summary>
        public void SetDriftCancellation(bool enabled, float strength, float maxAngle)
        {
            enableDriftCancellation = enabled;
            driftCancellationStrength = Mathf.Max(0f, strength);
            maxDriftAngle = Mathf.Clamp(maxAngle, 0f, 90f);
        }

        /// <summary>
        /// Enable or disable forward velocity dampening
        /// </summary>
        public void SetForwardDampening(bool enabled)
        {
            dampenForwardVelocity = enabled;
        }

        #endregion
    }
}
