using UnityEngine;
using System.Collections;

namespace UsefulScripts.FPS
{
    /// <summary>
    /// Titanfall-inspired mantling/climbing system.
    /// Allows the player to climb up ledges and obstacles.
    /// Supports both quick mantles and full climb animations.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class Mantle : MonoBehaviour
    {
        [Header("Detection")]
        [SerializeField] private float ledgeCheckDistance = 0.7f;
        [SerializeField] private float maxMantleHeight = 2.5f;
        [SerializeField] private float minMantleHeight = 0.5f;
        [SerializeField] private float ledgeCheckHeight = 1.5f;
        [SerializeField] private LayerMask mantleMask = ~0;
        [SerializeField] private float forwardCheckOffset = 0.3f;

        [Header("Mantle Settings")]
        [SerializeField] private float mantleSpeed = 8f;
        [SerializeField] private float quickMantleSpeedMultiplier = 1.5f;
        [SerializeField] private float mantleCooldown = 0.3f;
        [SerializeField] private float minSpeedForQuickMantle = 4f;

        [Header("Climb Settings")]
        [SerializeField] private float climbSpeed = 3f;
        [SerializeField] private float climbStamina = 100f;
        [SerializeField] private float climbStaminaDrain = 25f;
        [SerializeField] private float climbStaminaRegen = 30f;
        [SerializeField] private bool enableClimbing = true;
        [SerializeField] private float maxClimbTime = 3f;

        [Header("Animation")]
        [SerializeField] private float mantleUpDuration = 0.3f;
        [SerializeField] private float mantleForwardDuration = 0.2f;
        [SerializeField] private AnimationCurve mantleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private float mantleVerticalOffset = 0.1f;
        [SerializeField] private float mantleForwardOffset = 0.3f;

        [Header("Input")]
        [SerializeField] private bool autoMantle = true;
        [SerializeField] private KeyCode mantleKey = KeyCode.Space;

        // Components
        private CharacterController controller;

        // Mantle state
        private bool isMantling;
        private bool isClimbing;
        private float lastMantleTime;
        private Vector3 mantleStartPos;
        private Vector3 mantleEndPos;
        private Vector3 ledgeNormal;
        private float mantleProgress;
        private float currentStamina;
        private float climbTimer;

        // Detection cache
        private bool canMantle;
        private float detectedLedgeHeight;
        private Vector3 detectedLedgePoint;

        // Events
        public event System.Action OnMantleStart;
        public event System.Action OnMantleEnd;
        public event System.Action OnClimbStart;
        public event System.Action OnClimbEnd;
        public event System.Action<float> OnStaminaChanged;

        // Properties
        public bool IsMantling => isMantling;
        public bool IsClimbing => isClimbing;
        public bool CanMantle => canMantle;
        public float MantleProgress => mantleProgress;
        public float CurrentStamina => currentStamina;
        public float MaxStamina => climbStamina;
        public float StaminaPercent => currentStamina / climbStamina;
        public float DetectedLedgeHeight => detectedLedgeHeight;

        /// <summary>
        /// Sets the transform position directly, bypassing CharacterController collision.
        /// </summary>
        private void SetPositionDirectly(Vector3 position)
        {
            controller.enabled = false;
            transform.position = position;
            controller.enabled = true;
        }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            currentStamina = climbStamina;
        }

        private void Update()
        {
            DetectLedge();
            HandleInput();
            UpdateClimbing();
            RegenerateStamina();
        }

        private void DetectLedge()
        {
            canMantle = false;
            detectedLedgeHeight = 0f;

            // Don't detect while already mantling
            if (isMantling || isClimbing) return;

            // Check for wall in front
            Vector3 checkOrigin = transform.position + Vector3.up * 0.5f;
            if (!Physics.Raycast(checkOrigin, transform.forward, out RaycastHit wallHit, ledgeCheckDistance, mantleMask))
            {
                return;
            }

            ledgeNormal = wallHit.normal;

            // Check for ledge at various heights
            for (float height = minMantleHeight; height <= maxMantleHeight; height += 0.2f)
            {
                Vector3 highCheckOrigin = transform.position + Vector3.up * height + transform.forward * forwardCheckOffset;
                
                // Check if there's empty space above the ledge
                if (!Physics.Raycast(highCheckOrigin, transform.forward, ledgeCheckDistance * 0.5f, mantleMask))
                {
                    // Check if there's a surface to land on
                    if (Physics.Raycast(highCheckOrigin + transform.forward * ledgeCheckDistance * 0.3f, Vector3.down, out RaycastHit ledgeHit, height + 0.5f, mantleMask))
                    {
                        // Verify the surface is relatively flat
                        float surfaceAngle = Vector3.Angle(ledgeHit.normal, Vector3.up);
                        if (surfaceAngle < 45f)
                        {
                            canMantle = true;
                            detectedLedgeHeight = ledgeHit.point.y - transform.position.y;
                            detectedLedgePoint = ledgeHit.point;
                            return;
                        }
                    }
                }
            }
        }

        private void HandleInput()
        {
            if (isMantling) return;

            bool mantleInput = autoMantle ? Input.GetButton("Jump") : Input.GetKeyDown(mantleKey);
            
            // Check for mantle
            if (mantleInput && canMantle && !controller.isGrounded)
            {
                if (Time.time - lastMantleTime >= mantleCooldown)
                {
                    StartMantle();
                }
            }

            // Check for wall climb
            if (enableClimbing && mantleInput && !controller.isGrounded && !canMantle && currentStamina > 0)
            {
                if (Physics.Raycast(transform.position + Vector3.up * 0.5f, transform.forward, ledgeCheckDistance, mantleMask))
                {
                    if (!isClimbing)
                    {
                        StartClimbing();
                    }
                }
            }

            // Stop climbing if not holding input
            if (isClimbing && !mantleInput)
            {
                StopClimbing();
            }
        }

        private void StartMantle()
        {
            if (isMantling) return;

            isMantling = true;
            mantleProgress = 0f;
            mantleStartPos = transform.position;
            
            // Calculate end position (slightly above and forward from the ledge)
            mantleEndPos = detectedLedgePoint + Vector3.up * mantleVerticalOffset - ledgeNormal * mantleForwardOffset;

            // Check if this is a quick mantle (player has momentum)
            float horizontalSpeed = new Vector3(controller.velocity.x, 0, controller.velocity.z).magnitude;
            if (horizontalSpeed >= minSpeedForQuickMantle)
            {
                StartCoroutine(QuickMantleCoroutine());
            }
            else
            {
                StartCoroutine(MantleCoroutine());
            }

            lastMantleTime = Time.time;
            OnMantleStart?.Invoke();
        }

        private IEnumerator MantleCoroutine()
        {
            // Phase 1: Move up
            Vector3 upTarget = new Vector3(mantleStartPos.x, mantleEndPos.y, mantleStartPos.z);
            float upProgress = 0f;

            while (upProgress < 1f)
            {
                upProgress += Time.deltaTime / mantleUpDuration;
                float t = mantleCurve.Evaluate(upProgress);
                
                Vector3 newPos = Vector3.Lerp(mantleStartPos, upTarget, t);
                SetPositionDirectly(newPos);

                mantleProgress = upProgress * 0.6f;
                yield return null;
            }

            // Phase 2: Move forward
            Vector3 currentPos = transform.position;
            float forwardProgress = 0f;

            while (forwardProgress < 1f)
            {
                forwardProgress += Time.deltaTime / mantleForwardDuration;
                float t = mantleCurve.Evaluate(forwardProgress);
                
                Vector3 newPos = Vector3.Lerp(currentPos, mantleEndPos, t);
                SetPositionDirectly(newPos);

                mantleProgress = 0.6f + forwardProgress * 0.4f;
                yield return null;
            }

            EndMantle();
        }

        private IEnumerator QuickMantleCoroutine()
        {
            float totalDuration = (mantleUpDuration + mantleForwardDuration) / quickMantleSpeedMultiplier;
            float progress = 0f;

            Vector3 startPos = mantleStartPos;

            while (progress < 1f)
            {
                progress += Time.deltaTime / totalDuration;
                float t = mantleCurve.Evaluate(progress);

                // Arc trajectory for quick mantle
                Vector3 midPoint = (startPos + mantleEndPos) / 2f + Vector3.up * 0.5f;
                Vector3 newPos;
                
                if (t < 0.5f)
                {
                    newPos = Vector3.Lerp(startPos, midPoint, t * 2f);
                }
                else
                {
                    newPos = Vector3.Lerp(midPoint, mantleEndPos, (t - 0.5f) * 2f);
                }

                SetPositionDirectly(newPos);

                mantleProgress = progress;
                yield return null;
            }

            EndMantle();
        }

        private void EndMantle()
        {
            isMantling = false;
            mantleProgress = 0f;
            OnMantleEnd?.Invoke();
        }

        private void StartClimbing()
        {
            isClimbing = true;
            climbTimer = 0f;
            OnClimbStart?.Invoke();
        }

        private void UpdateClimbing()
        {
            if (!isClimbing) return;

            climbTimer += Time.deltaTime;

            // Check if we can still climb
            bool hasWall = Physics.Raycast(transform.position + Vector3.up * 0.5f, transform.forward, ledgeCheckDistance * 1.2f, mantleMask);
            
            if (!hasWall || currentStamina <= 0 || climbTimer >= maxClimbTime)
            {
                StopClimbing();
                return;
            }

            // Drain stamina
            currentStamina -= climbStaminaDrain * Time.deltaTime;
            currentStamina = Mathf.Max(0, currentStamina);
            OnStaminaChanged?.Invoke(currentStamina);

            // Move up the wall
            Vector3 climbVelocity = Vector3.up * climbSpeed;
            
            // Add slight movement toward wall
            climbVelocity += transform.forward * 0.5f;

            controller.Move(climbVelocity * Time.deltaTime);

            // Check for ledge while climbing
            DetectLedge();
            if (canMantle)
            {
                StartMantle();
                StopClimbing();
            }
        }

        private void StopClimbing()
        {
            if (isClimbing)
            {
                isClimbing = false;
                OnClimbEnd?.Invoke();
            }
        }

        private void RegenerateStamina()
        {
            if (!isClimbing && !isMantling && controller.isGrounded)
            {
                if (currentStamina < climbStamina)
                {
                    currentStamina += climbStaminaRegen * Time.deltaTime;
                    currentStamina = Mathf.Min(climbStamina, currentStamina);
                    OnStaminaChanged?.Invoke(currentStamina);
                }
            }
        }

        /// <summary>
        /// Forces a mantle if possible.
        /// </summary>
        public bool TryMantle()
        {
            if (!canMantle || isMantling) return false;
            StartMantle();
            return true;
        }

        /// <summary>
        /// Cancels the current mantle (if possible).
        /// </summary>
        public void CancelMantle()
        {
            if (isMantling)
            {
                StopAllCoroutines();
                EndMantle();
            }
        }

        /// <summary>
        /// Resets climbing stamina to full.
        /// </summary>
        public void ResetStamina()
        {
            currentStamina = climbStamina;
            OnStaminaChanged?.Invoke(currentStamina);
        }

        /// <summary>
        /// Sets whether auto-mantle is enabled.
        /// </summary>
        public void SetAutoMantle(bool enabled)
        {
            autoMantle = enabled;
        }

        /// <summary>
        /// Gets the detected ledge position.
        /// </summary>
        public Vector3 GetDetectedLedgePosition()
        {
            return detectedLedgePoint;
        }

        private void OnDrawGizmosSelected()
        {
            // Wall check ray
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position + Vector3.up * 0.5f, transform.forward * ledgeCheckDistance);

            // Ledge detection range
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Vector3 minPos = transform.position + Vector3.up * minMantleHeight;
            Vector3 maxPos = transform.position + Vector3.up * maxMantleHeight;
            Gizmos.DrawLine(minPos, maxPos);
            Gizmos.DrawWireCube(minPos + transform.forward * forwardCheckOffset, new Vector3(0.3f, 0.1f, 0.3f));
            Gizmos.DrawWireCube(maxPos + transform.forward * forwardCheckOffset, new Vector3(0.3f, 0.1f, 0.3f));

            // Detected ledge
            if (canMantle)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(detectedLedgePoint, 0.2f);
            }
        }
    }
}
