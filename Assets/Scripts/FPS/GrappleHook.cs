using UnityEngine;

namespace UsefulScripts.FPS
{
    /// <summary>
    /// Titanfall-inspired grappling hook system.
    /// Allows the player to grapple to surfaces for fast traversal.
    /// Supports swinging physics and momentum-based movement.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class GrappleHook : MonoBehaviour
    {
        [Header("Grapple Settings")]
        [SerializeField] private float maxGrappleDistance = 50f;
        [SerializeField] private float grappleSpeed = 20f;
        [SerializeField] private float grapplePullForce = 15f;
        [SerializeField] private float grappleCooldown = 3f;
        [SerializeField] private float minGrappleDistance = 3f;
        [SerializeField] private LayerMask grappleMask = ~0;

        [Header("Grapple Behavior")]
        [SerializeField] private GrappleMode grappleMode = GrappleMode.PullToPoint;
        [SerializeField] private float swingGravity = -15f;
        [SerializeField] private float ropeLength = 20f;
        [SerializeField] private float ropeTightness = 10f;
        [SerializeField] private float momentumPreservation = 0.8f;

        [Header("Launch Settings")]
        [SerializeField] private float launchBoost = 1.3f;
        [SerializeField] private float detachDistance = 2f;
        [SerializeField] private bool autoDetachAtPoint = true;

        [Header("Visual")]
        [SerializeField] private LineRenderer ropeRenderer;
        [SerializeField] private Transform grappleOrigin;
        [SerializeField] private int ropeResolution = 20;
        [SerializeField] private float ropeWaveAmplitude = 0.5f;
        [SerializeField] private float ropeWaveFrequency = 3f;
        [SerializeField] private float ropeEndWaveReduction = 2f;
        [SerializeField] private AnimationCurve ropeCurve = AnimationCurve.Linear(0, 0, 1, 0);

        [Header("Input")]
        [SerializeField] private KeyCode grappleKey = KeyCode.E;
        [SerializeField] private Camera playerCamera;

        // Components
        private CharacterController controller;

        // Grapple state
        private bool isGrappling;
        private Vector3 grapplePoint;
        private Vector3 grappleVelocity;
        private float grappleProgress;
        private float lastGrappleTime;
        private float currentRopeLength;
        private Vector3 swingVelocity;

        // Events
        public event System.Action<Vector3> OnGrappleStart;
        public event System.Action OnGrappleEnd;
        public event System.Action OnGrappleLaunch;

        // Properties
        public bool IsGrappling => isGrappling;
        public Vector3 GrapplePoint => grapplePoint;
        public float CooldownRemaining => Mathf.Max(0, grappleCooldown - (Time.time - lastGrappleTime));
        public bool IsOnCooldown => CooldownRemaining > 0;
        public float GrappleProgress => grappleProgress;
        public Vector3 GrappleVelocity => grappleVelocity;

        public enum GrappleMode
        {
            PullToPoint,    // Pull directly toward the grapple point
            Swing,          // Swing on the rope like a pendulum
            Hybrid          // Pull initially, then swing when close
        }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();

            if (grappleOrigin == null)
            {
                grappleOrigin = transform;
            }

            if (ropeRenderer != null)
            {
                ropeRenderer.positionCount = ropeResolution;
                ropeRenderer.enabled = false;
            }

            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }
        }

        private void Update()
        {
            HandleGrappleInput();
            UpdateGrapple();
            UpdateRopeVisual();
        }

        private void HandleGrappleInput()
        {
            if (Input.GetKeyDown(grappleKey))
            {
                if (!isGrappling)
                {
                    TryStartGrapple();
                }
            }

            if (Input.GetKeyUp(grappleKey))
            {
                if (isGrappling)
                {
                    EndGrapple(true);
                }
            }
        }

        private void TryStartGrapple()
        {
            if (IsOnCooldown) return;

            // Raycast from camera to find grapple point
            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            
            if (Physics.Raycast(ray, out RaycastHit hit, maxGrappleDistance, grappleMask))
            {
                float distance = Vector3.Distance(grappleOrigin.position, hit.point);
                
                if (distance >= minGrappleDistance)
                {
                    StartGrapple(hit.point);
                }
            }
        }

        private void StartGrapple(Vector3 point)
        {
            isGrappling = true;
            grapplePoint = point;
            grappleProgress = 0f;
            currentRopeLength = Vector3.Distance(grappleOrigin.position, point);
            
            // Initialize swing velocity based on current movement
            swingVelocity = controller.velocity;

            if (ropeRenderer != null)
            {
                ropeRenderer.enabled = true;
            }

            OnGrappleStart?.Invoke(point);
        }

        private void EndGrapple(bool preserveMomentum)
        {
            if (!isGrappling) return;

            isGrappling = false;
            lastGrappleTime = Time.time;

            if (ropeRenderer != null)
            {
                ropeRenderer.enabled = false;
            }

            // Calculate launch velocity
            if (preserveMomentum)
            {
                grappleVelocity = swingVelocity * momentumPreservation;
                
                // Add boost if releasing while moving toward the point
                Vector3 toPoint = (grapplePoint - transform.position).normalized;
                float dotProduct = Vector3.Dot(swingVelocity.normalized, toPoint);
                
                if (dotProduct > 0.5f)
                {
                    grappleVelocity *= launchBoost;
                    OnGrappleLaunch?.Invoke();
                }
            }
            else
            {
                grappleVelocity = Vector3.zero;
            }

            OnGrappleEnd?.Invoke();
        }

        private void UpdateGrapple()
        {
            if (!isGrappling) return;

            Vector3 toGrapplePoint = grapplePoint - transform.position;
            float distance = toGrapplePoint.magnitude;

            // Auto-detach when close enough
            if (autoDetachAtPoint && distance <= detachDistance)
            {
                EndGrapple(true);
                return;
            }

            // Update grapple progress
            grappleProgress = 1f - (distance / currentRopeLength);

            switch (grappleMode)
            {
                case GrappleMode.PullToPoint:
                    UpdatePullGrapple(toGrapplePoint, distance);
                    break;
                case GrappleMode.Swing:
                    UpdateSwingGrapple(toGrapplePoint, distance);
                    break;
                case GrappleMode.Hybrid:
                    UpdateHybridGrapple(toGrapplePoint, distance);
                    break;
            }
        }

        private void UpdatePullGrapple(Vector3 toGrapplePoint, float distance)
        {
            // Calculate pull direction
            Vector3 pullDirection = toGrapplePoint.normalized;
            
            // Calculate velocity
            swingVelocity = pullDirection * grappleSpeed;
            
            // Add slight gravity effect
            swingVelocity.y += swingGravity * 0.3f * Time.deltaTime;

            // Apply movement
            controller.Move(swingVelocity * Time.deltaTime);
        }

        private void UpdateSwingGrapple(Vector3 toGrapplePoint, float distance)
        {
            // Apply gravity
            swingVelocity.y += swingGravity * Time.deltaTime;

            // Calculate swing physics
            Vector3 ropeDirection = toGrapplePoint.normalized;
            
            // Constrain to rope length (elastic rope)
            if (distance > ropeLength)
            {
                // Pull back toward the rope length
                float excess = distance - ropeLength;
                Vector3 correction = ropeDirection * excess * ropeTightness * Time.deltaTime;
                swingVelocity += correction;
                
                // Remove velocity component going away from grapple point
                float awayComponent = Vector3.Dot(swingVelocity, -ropeDirection);
                if (awayComponent > 0)
                {
                    swingVelocity += ropeDirection * awayComponent * 0.5f;
                }
            }

            // Add player input influence
            float horizontalInput = Input.GetAxisRaw("Horizontal");
            float verticalInput = Input.GetAxisRaw("Vertical");
            
            Vector3 inputDir = transform.right * horizontalInput + transform.forward * verticalInput;
            swingVelocity += inputDir * grapplePullForce * 0.3f * Time.deltaTime;

            // Apply movement
            controller.Move(swingVelocity * Time.deltaTime);
        }

        private void UpdateHybridGrapple(Vector3 toGrapplePoint, float distance)
        {
            float transitionDistance = currentRopeLength * 0.5f;
            float t = Mathf.Clamp01((currentRopeLength - distance) / transitionDistance);

            // Blend between pull and swing based on distance
            Vector3 pullVelocity = toGrapplePoint.normalized * grappleSpeed;
            
            // Swing calculation
            swingVelocity.y += swingGravity * Time.deltaTime;
            
            Vector3 ropeDirection = toGrapplePoint.normalized;
            if (distance > ropeLength * 0.8f)
            {
                float excess = distance - ropeLength * 0.8f;
                swingVelocity += ropeDirection * excess * ropeTightness * Time.deltaTime;
            }

            // Blend velocities
            Vector3 finalVelocity = Vector3.Lerp(pullVelocity, swingVelocity, t);
            swingVelocity = finalVelocity;

            controller.Move(finalVelocity * Time.deltaTime);
        }

        private void UpdateRopeVisual()
        {
            if (ropeRenderer == null || !isGrappling) return;

            Vector3 startPoint = grappleOrigin.position;
            Vector3 endPoint = grapplePoint;

            // Calculate rope wave based on time
            float wavePhase = Time.time * ropeWaveFrequency;
            
            // Reduce wave amplitude as grapple progresses (rope tightens)
            float adjustedAmplitude = ropeWaveAmplitude * (1f - grappleProgress * 0.8f);

            for (int i = 0; i < ropeResolution; i++)
            {
                float t = (float)i / (ropeResolution - 1);
                Vector3 point = Vector3.Lerp(startPoint, endPoint, t);

                // Add wave effect
                if (adjustedAmplitude > 0.01f)
                {
                    float wave = Mathf.Sin(t * Mathf.PI * 2 + wavePhase) * adjustedAmplitude;
                    float curveValue = ropeCurve.Evaluate(t);
                    wave *= (1f - Mathf.Abs(t - 0.5f) * ropeEndWaveReduction); // Reduce wave at ends
                    
                    point += Vector3.up * wave * curveValue;
                }

                ropeRenderer.SetPosition(i, point);
            }
        }

        /// <summary>
        /// Tries to grapple to a specific point.
        /// </summary>
        public bool TryGrappleTo(Vector3 point)
        {
            if (IsOnCooldown || isGrappling) return false;

            float distance = Vector3.Distance(grappleOrigin.position, point);
            if (distance < minGrappleDistance || distance > maxGrappleDistance) return false;

            StartGrapple(point);
            return true;
        }

        /// <summary>
        /// Forces the grapple to end.
        /// </summary>
        public void ForceEndGrapple()
        {
            EndGrapple(false);
        }

        /// <summary>
        /// Gets the velocity to apply after grappling ends.
        /// </summary>
        public Vector3 GetLaunchVelocity()
        {
            return grappleVelocity;
        }

        /// <summary>
        /// Resets the grapple cooldown.
        /// </summary>
        public void ResetCooldown()
        {
            lastGrappleTime = 0f;
        }

        /// <summary>
        /// Gets a predicted grapple point based on current aim.
        /// </summary>
        public bool GetPredictedGrapplePoint(out Vector3 point, out float distance)
        {
            point = Vector3.zero;
            distance = 0f;

            if (playerCamera == null) return false;

            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            
            if (Physics.Raycast(ray, out RaycastHit hit, maxGrappleDistance, grappleMask))
            {
                distance = Vector3.Distance(grappleOrigin.position, hit.point);
                if (distance >= minGrappleDistance)
                {
                    point = hit.point;
                    return true;
                }
            }

            return false;
        }

        private void OnDrawGizmosSelected()
        {
            // Draw max grapple range
            Gizmos.color = new Color(0, 1, 0, 0.2f);
            Gizmos.DrawWireSphere(transform.position, maxGrappleDistance);

            // Draw min grapple range
            Gizmos.color = new Color(1, 0, 0, 0.2f);
            Gizmos.DrawWireSphere(transform.position, minGrappleDistance);

            // Draw current grapple
            if (isGrappling)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(grappleOrigin != null ? grappleOrigin.position : transform.position, grapplePoint);
                Gizmos.DrawWireSphere(grapplePoint, 0.5f);
            }
        }
    }
}
