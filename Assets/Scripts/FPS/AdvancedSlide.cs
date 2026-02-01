using UnityEngine;

namespace UsefulScripts.FPS
{
    /// <summary>
    /// Titanfall-inspired sliding system with momentum preservation.
    /// Allows the player to slide along the ground while maintaining speed.
    /// Works best on slopes and preserves momentum from sprinting.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class AdvancedSlide : MonoBehaviour
    {
        [Header("Slide Settings")]
        [SerializeField] private float slideSpeed = 15f;
        [SerializeField] private float slideDuration = 1.5f;
        [SerializeField] private float slideDeceleration = 5f;
        [SerializeField] private float slideCooldown = 0.5f;
        [SerializeField] private float minSpeedToSlide = 5f;
        [SerializeField] private float slideBoostOnSlope = 1.5f;

        [Header("Slide Height")]
        [SerializeField] private float standingHeight = 2f;
        [SerializeField] private float slideHeight = 0.6f;
        [SerializeField] private float heightTransitionSpeed = 12f;
        [SerializeField] private Vector3 slideCameraOffset = new Vector3(0f, -0.4f, 0f);

        [Header("Slide Control")]
        [SerializeField] private float slideSteerSpeed = 1f;
        [SerializeField] private bool canJumpWhileSliding = true;
        [SerializeField] private float slideJumpBoost = 1.2f;

        [Header("Slope Detection")]
        [SerializeField] private float slopeCheckDistance = 1f;
        [SerializeField] private float maxSlopeAngle = 50f;

        [Header("Camera Effects")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float fovIncrease = 5f;
        [SerializeField] private float fovChangeSpeed = 8f;

        [Header("Input")]
        [SerializeField] private KeyCode slideKey = KeyCode.LeftControl;

        // Components
        private CharacterController controller;
        private Camera playerCamera;

        // Slide state
        private bool isSliding;
        private float slideTimer;
        private float currentSlideSpeed;
        private Vector3 slideDirection;
        private float lastSlideTime;
        private float currentHeight;
        private float targetHeight;
        private Vector3 originalCameraPosition;
        private float defaultFov;

        // Events
        public event System.Action OnSlideStart;
        public event System.Action OnSlideEnd;
        public event System.Action OnSlideJump;

        // Properties
        public bool IsSliding => isSliding;
        public float CurrentSlideSpeed => currentSlideSpeed;
        public float SlideTimeRemaining => Mathf.Max(0, slideDuration - slideTimer);
        public Vector3 SlideDirection => slideDirection;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            currentHeight = standingHeight;
            targetHeight = standingHeight;

            if (cameraTransform != null)
            {
                originalCameraPosition = cameraTransform.localPosition;
                playerCamera = cameraTransform.GetComponent<Camera>();
                if (playerCamera != null)
                {
                    defaultFov = playerCamera.fieldOfView;
                }
            }
        }

        private void Update()
        {
            HandleSlideInput();
            UpdateSlide();
            UpdateHeight();
            UpdateCameraEffects();
        }

        private void HandleSlideInput()
        {
            bool slideInputDown = Input.GetKeyDown(slideKey);
            bool slideInputHeld = Input.GetKey(slideKey);

            // Start slide
            if (slideInputDown && CanStartSlide())
            {
                StartSlide();
            }

            // End slide if key released (optional - can change to toggle)
            if (!slideInputHeld && isSliding)
            {
                EndSlide();
            }

            // Handle jump while sliding
            if (isSliding && canJumpWhileSliding && Input.GetButtonDown("Jump"))
            {
                SlideJump();
            }
        }

        private bool CanStartSlide()
        {
            // Must be grounded
            if (!controller.isGrounded) return false;

            // Must be off cooldown
            if (Time.time - lastSlideTime < slideCooldown) return false;

            // Must be moving fast enough
            float horizontalSpeed = new Vector3(controller.velocity.x, 0, controller.velocity.z).magnitude;
            if (horizontalSpeed < minSpeedToSlide) return false;

            // Can't stand up check (already crouched)
            // This allows chaining slides
            
            return true;
        }

        private void StartSlide()
        {
            if (isSliding) return;

            isSliding = true;
            slideTimer = 0f;

            // Set slide direction based on current movement or facing direction
            Vector3 velocity = controller.velocity;
            velocity.y = 0;
            
            if (velocity.magnitude > 0.1f)
            {
                slideDirection = velocity.normalized;
            }
            else
            {
                slideDirection = transform.forward;
            }

            // Calculate initial slide speed (preserve momentum)
            float horizontalSpeed = new Vector3(controller.velocity.x, 0, controller.velocity.z).magnitude;
            currentSlideSpeed = Mathf.Max(horizontalSpeed, slideSpeed);

            // Apply slope boost if sliding downhill
            float slopeAngle = GetSlopeAngle();
            if (slopeAngle > 5f && IsGoingDownhill())
            {
                currentSlideSpeed *= 1f + (slopeAngle / maxSlopeAngle) * (slideBoostOnSlope - 1f);
            }

            targetHeight = slideHeight;
            lastSlideTime = Time.time;

            OnSlideStart?.Invoke();
        }

        private void EndSlide()
        {
            if (!isSliding) return;

            // Check if we can stand up
            if (!CanStandUp())
            {
                return; // Stay crouched if there's no room
            }

            isSliding = false;
            targetHeight = standingHeight;
            lastSlideTime = Time.time;

            OnSlideEnd?.Invoke();
        }

        private bool CanStandUp()
        {
            float checkDistance = standingHeight - currentHeight;
            return !Physics.Raycast(transform.position, Vector3.up, checkDistance + 0.1f);
        }

        private void UpdateSlide()
        {
            if (!isSliding) return;

            slideTimer += Time.deltaTime;

            // Apply steering
            float horizontalInput = Input.GetAxisRaw("Horizontal");
            if (Mathf.Abs(horizontalInput) > 0.1f)
            {
                slideDirection = Quaternion.Euler(0, horizontalInput * slideSteerSpeed, 0) * slideDirection;
                slideDirection.Normalize();
            }

            // Calculate speed with slope influence
            float slopeAngle = GetSlopeAngle();
            float slopeInfluence = 0f;

            if (slopeAngle > 2f)
            {
                if (IsGoingDownhill())
                {
                    // Accelerate downhill
                    slopeInfluence = slopeAngle / maxSlopeAngle * slideBoostOnSlope * 10f;
                    currentSlideSpeed += slopeInfluence * Time.deltaTime;
                }
                else
                {
                    // Decelerate uphill
                    slopeInfluence = slopeAngle / maxSlopeAngle * slideDeceleration * 2f;
                    currentSlideSpeed -= slopeInfluence * Time.deltaTime;
                }
            }
            else
            {
                // Flat ground deceleration
                currentSlideSpeed -= slideDeceleration * Time.deltaTime;
            }

            currentSlideSpeed = Mathf.Max(0, currentSlideSpeed);

            // End slide if too slow or time expired
            if (currentSlideSpeed <= 2f || slideTimer >= slideDuration)
            {
                EndSlide();
                return;
            }

            // Apply slide movement
            Vector3 slideVelocity = slideDirection * currentSlideSpeed;
            slideVelocity.y = -2f; // Small downward force to stay grounded

            controller.Move(slideVelocity * Time.deltaTime);
        }

        private float GetSlopeAngle()
        {
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, slopeCheckDistance))
            {
                return Vector3.Angle(hit.normal, Vector3.up);
            }
            return 0f;
        }

        private bool IsGoingDownhill()
        {
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, slopeCheckDistance))
            {
                Vector3 slopeDirection = Vector3.ProjectOnPlane(Vector3.down, hit.normal).normalized;
                return Vector3.Dot(slideDirection, slopeDirection) > 0;
            }
            return false;
        }

        private void UpdateHeight()
        {
            // Smoothly transition between standing and crouching
            currentHeight = Mathf.Lerp(currentHeight, targetHeight, heightTransitionSpeed * Time.deltaTime);
            
            controller.height = currentHeight;
            controller.center = new Vector3(0, currentHeight / 2f, 0);

            // Update camera position
            if (cameraTransform != null)
            {
                float slideProgress = isSliding ? 1f : 0f;
                Vector3 targetCameraPos = originalCameraPosition + slideCameraOffset * slideProgress;
                
                // Adjust for height difference
                float heightDiff = standingHeight - currentHeight;
                targetCameraPos.y = originalCameraPosition.y - heightDiff * 0.5f;
                
                cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, targetCameraPos, heightTransitionSpeed * Time.deltaTime);
            }
        }

        private void UpdateCameraEffects()
        {
            if (playerCamera == null) return;

            float targetFov = isSliding ? defaultFov + fovIncrease : defaultFov;
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFov, fovChangeSpeed * Time.deltaTime);
        }

        private void SlideJump()
        {
            if (!isSliding) return;

            // Preserve slide momentum in the jump
            Vector3 jumpVelocity = slideDirection * currentSlideSpeed * slideJumpBoost;
            
            EndSlide();
            OnSlideJump?.Invoke();
        }

        /// <summary>
        /// Force starts a slide (for use with other movement systems).
        /// </summary>
        public void ForceStartSlide()
        {
            if (controller.isGrounded)
            {
                StartSlide();
            }
        }

        /// <summary>
        /// Force ends a slide.
        /// </summary>
        public void ForceEndSlide()
        {
            EndSlide();
        }

        /// <summary>
        /// Gets the velocity applied by sliding.
        /// </summary>
        public Vector3 GetSlideVelocity()
        {
            if (!isSliding) return Vector3.zero;
            return slideDirection * currentSlideSpeed;
        }

        /// <summary>
        /// Gets the jump boost velocity when slide jumping.
        /// </summary>
        public Vector3 GetSlideJumpBoost()
        {
            if (!isSliding) return Vector3.zero;
            return slideDirection * currentSlideSpeed * slideJumpBoost;
        }

        /// <summary>
        /// Sets the slide direction (for use with other systems).
        /// </summary>
        public void SetSlideDirection(Vector3 direction)
        {
            slideDirection = direction.normalized;
        }

        private void OnDrawGizmosSelected()
        {
            if (isSliding)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(transform.position, slideDirection * 2f);
            }

            // Draw slope check ray
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, Vector3.down * slopeCheckDistance);
        }
    }
}
