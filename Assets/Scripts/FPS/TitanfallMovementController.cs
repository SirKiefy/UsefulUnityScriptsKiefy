using UnityEngine;

namespace UsefulScripts.FPS
{
    /// <summary>
    /// Complete Titanfall-inspired movement controller.
    /// Integrates all advanced movement mechanics: wall running, sliding, grappling, and mantling.
    /// Use this as a complete replacement for basic FPS controllers.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class TitanfallMovementController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 7f;
        [SerializeField] private float sprintSpeed = 12f;
        [SerializeField] private float airSpeed = 5f;
        [SerializeField] private float acceleration = 15f;
        [SerializeField] private float airAcceleration = 8f;
        [SerializeField] private float friction = 6f;

        [Header("Jumping")]
        [SerializeField] private float jumpForce = 10f;
        [SerializeField] private float doubleJumpForce = 8f;
        [SerializeField] private int maxAirJumps = 1;
        [SerializeField] private float gravity = -25f;
        [SerializeField] private float coyoteTime = 0.15f;
        [SerializeField] private float jumpBufferTime = 0.1f;

        [Header("Mouse Look")]
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private float maxLookAngle = 89f;
        [SerializeField] private bool invertY = false;
        [SerializeField] private Transform cameraTransform;

        [Header("Advanced Movement")]
        [SerializeField] private bool enableWallRunning = true;
        [SerializeField] private bool enableSliding = true;
        [SerializeField] private bool enableGrapple = true;
        [SerializeField] private bool enableMantle = true;

        [Header("Wall Running Settings")]
        [SerializeField] private LayerMask wallMask = ~0;
        [SerializeField] private float wallCheckDistance = 1f;
        [SerializeField] private float wallRunSpeed = 14f;
        [SerializeField] private float wallRunDuration = 2f;
        [SerializeField] private float wallRunGravity = -3f;
        [SerializeField] private float wallJumpUpForce = 10f;
        [SerializeField] private float wallJumpSideForce = 10f;
        [SerializeField] private float cameraTiltAngle = 12f;

        [Header("Slide Settings")]
        [SerializeField] private float slideSpeed = 16f;
        [SerializeField] private float slideDuration = 1.2f;
        [SerializeField] private float slideHeight = 0.6f;
        [SerializeField] private float standingHeight = 2f;
        [SerializeField] private KeyCode slideKey = KeyCode.LeftControl;

        [Header("Grapple Settings")]
        [SerializeField] private float maxGrappleDistance = 40f;
        [SerializeField] private float grappleSpeed = 25f;
        [SerializeField] private float grappleCooldown = 4f;
        [SerializeField] private KeyCode grappleKey = KeyCode.E;
        [SerializeField] private LineRenderer grappleRope;

        [Header("Mantle Settings")]
        [SerializeField] private float maxMantleHeight = 2.5f;
        [SerializeField] private float mantleSpeed = 10f;
        [SerializeField] private bool autoMantle = true;
        [SerializeField] private float mantleVerticalOffset = 0.1f;
        [SerializeField] private float mantleForwardOffset = 0.3f;

        [Header("Ground Check")]
        [SerializeField] private LayerMask groundMask;
        [SerializeField] private float groundCheckDistance = 0.2f;

        // Components
        private CharacterController controller;
        private Camera playerCamera;

        // State
        private Vector3 velocity;
        private Vector3 moveDirection;
        private float cameraPitch;
        private bool isGrounded;
        private bool wasGrounded;
        private int airJumpsRemaining;
        private float lastGroundedTime;
        private float lastJumpInputTime;
        private float currentHeight;

        // Wall Running State
        private bool isWallRunning;
        private bool isWallLeft;
        private bool isWallRight;
        private float wallRunTimer;
        private Vector3 wallNormal;
        private Vector3 wallForward;
        private float lastWallJumpTime;
        private float currentCameraTilt;

        // Slide State
        private bool isSliding;
        private float slideTimer;
        private float currentSlideSpeed;
        private Vector3 slideDirection;
        private float lastSlideTime;

        // Grapple State
        private bool isGrappling;
        private Vector3 grapplePoint;
        private float lastGrappleTime;
        private Vector3 grappleVelocity;

        // Mantle State
        private bool isMantling;
        private Vector3 mantleTarget;
        private Vector3 mantleStart;
        private float mantleProgress;

        // Input
        private Vector2 moveInput;
        private Vector2 lookInput;
        private bool jumpInputDown;
        private bool jumpInputHeld;
        private bool sprintInput;

        // Events
        public event System.Action OnJump;
        public event System.Action OnDoubleJump;
        public event System.Action OnLand;
        public event System.Action OnWallRunStart;
        public event System.Action OnWallRunEnd;
        public event System.Action OnWallJump;
        public event System.Action OnSlideStart;
        public event System.Action OnSlideEnd;
        public event System.Action<Vector3> OnGrappleStart;
        public event System.Action OnGrappleEnd;
        public event System.Action OnMantleStart;
        public event System.Action OnMantleEnd;

        // Properties
        public bool IsGrounded => isGrounded;
        public bool IsWallRunning => isWallRunning;
        public bool IsSliding => isSliding;
        public bool IsGrappling => isGrappling;
        public bool IsMantling => isMantling;
        public bool IsSprinting => sprintInput && !isSliding && !isWallRunning;
        public float CurrentSpeed => new Vector3(velocity.x, 0, velocity.z).magnitude;
        public Vector3 Velocity => velocity;
        public float GrappleCooldownRemaining => Mathf.Max(0, grappleCooldown - (Time.time - lastGrappleTime));
        public bool CanGrapple => !isGrappling && GrappleCooldownRemaining <= 0;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            currentHeight = standingHeight;
            airJumpsRemaining = maxAirJumps;

            if (cameraTransform != null)
            {
                playerCamera = cameraTransform.GetComponent<Camera>();
            }
            else if (Camera.main != null)
            {
                playerCamera = Camera.main;
                cameraTransform = playerCamera.transform;
            }
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (grappleRope != null)
            {
                grappleRope.enabled = false;
            }
        }

        private void Update()
        {
            GatherInput();
            
            // Don't process movement if mantling
            if (isMantling)
            {
                UpdateMantle();
                return;
            }

            UpdateGroundCheck();
            
            // Priority-based movement updates
            if (isGrappling)
            {
                UpdateGrapple();
            }
            else if (isWallRunning)
            {
                UpdateWallRun();
            }
            else if (isSliding)
            {
                UpdateSlide();
            }
            else
            {
                UpdateMovement();
            }

            // These always run
            CheckForWallRun();
            CheckForSlide();
            CheckForGrapple();
            CheckForMantle();
            UpdateCameraEffects();
            UpdateLook();
            ApplyGravity();
            ApplyMovement();
        }

        private void GatherInput()
        {
            moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            lookInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
            jumpInputDown = Input.GetButtonDown("Jump");
            jumpInputHeld = Input.GetButton("Jump");
            sprintInput = Input.GetKey(KeyCode.LeftShift);

            if (jumpInputDown)
            {
                lastJumpInputTime = Time.time;
            }
        }

        private void UpdateGroundCheck()
        {
            wasGrounded = isGrounded;
            
            isGrounded = controller.isGrounded || 
                         Physics.SphereCast(transform.position + Vector3.up * 0.1f, controller.radius * 0.9f, 
                                           Vector3.down, out _, groundCheckDistance, groundMask);

            if (isGrounded)
            {
                lastGroundedTime = Time.time;
                
                if (!wasGrounded)
                {
                    // Just landed
                    airJumpsRemaining = maxAirJumps;
                    ResetWallRun();
                    OnLand?.Invoke();

                    // Check for buffered jump
                    if (Time.time - lastJumpInputTime <= jumpBufferTime)
                    {
                        Jump();
                    }
                }
            }
        }

        private void UpdateMovement()
        {
            float targetSpeed = sprintInput ? sprintSpeed : walkSpeed;
            float currentAcceleration = isGrounded ? acceleration : airAcceleration;
            float maxSpeed = isGrounded ? targetSpeed : airSpeed;

            // Calculate desired direction
            Vector3 inputDir = new Vector3(moveInput.x, 0, moveInput.y).normalized;
            Vector3 worldDir = transform.TransformDirection(inputDir);

            if (inputDir.magnitude > 0.1f)
            {
                // Accelerate toward desired direction
                Vector3 targetVelocity = worldDir * targetSpeed;
                velocity.x = Mathf.MoveTowards(velocity.x, targetVelocity.x, currentAcceleration * Time.deltaTime);
                velocity.z = Mathf.MoveTowards(velocity.z, targetVelocity.z, currentAcceleration * Time.deltaTime);
            }
            else if (isGrounded)
            {
                // Apply friction when not moving
                velocity.x = Mathf.MoveTowards(velocity.x, 0, friction * Time.deltaTime);
                velocity.z = Mathf.MoveTowards(velocity.z, 0, friction * Time.deltaTime);
            }

            // Handle jumping
            if (jumpInputDown)
            {
                TryJump();
            }
        }

        private void TryJump()
        {
            // Coyote time jump
            if (Time.time - lastGroundedTime <= coyoteTime)
            {
                Jump();
                return;
            }

            // Double jump
            if (!isGrounded && airJumpsRemaining > 0)
            {
                DoubleJump();
            }
        }

        private void Jump()
        {
            velocity.y = jumpForce;
            lastGroundedTime = 0f; // Prevent coyote time after jumping
            OnJump?.Invoke();
        }

        private void DoubleJump()
        {
            velocity.y = doubleJumpForce;
            airJumpsRemaining--;
            OnDoubleJump?.Invoke();
        }

        private void UpdateLook()
        {
            float mouseX = lookInput.x * mouseSensitivity;
            float mouseY = lookInput.y * mouseSensitivity * (invertY ? 1 : -1);

            transform.Rotate(Vector3.up * mouseX);

            cameraPitch += mouseY;
            cameraPitch = Mathf.Clamp(cameraPitch, -maxLookAngle, maxLookAngle);

            if (cameraTransform != null)
            {
                cameraTransform.localEulerAngles = new Vector3(cameraPitch, 0, currentCameraTilt);
            }
        }

        private void ApplyGravity()
        {
            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2f;
            }
            else if (!isWallRunning && !isGrappling && !isMantling)
            {
                velocity.y += gravity * Time.deltaTime;
            }
        }

        private void ApplyMovement()
        {
            if (!isMantling)
            {
                controller.Move(velocity * Time.deltaTime);
            }
        }

        #region Wall Running

        private void CheckForWallRun()
        {
            if (!enableWallRunning) return;
            if (isGrounded || isSliding || isGrappling || isMantling) return;

            // Check for walls
            RaycastHit leftHit, rightHit;
            isWallLeft = Physics.Raycast(transform.position, -transform.right, out leftHit, wallCheckDistance, wallMask);
            isWallRight = Physics.Raycast(transform.position, transform.right, out rightHit, wallCheckDistance, wallMask);

            // Verify wall validity and height
            if (isWallLeft)
            {
                wallNormal = leftHit.normal;
            }
            else if (isWallRight)
            {
                wallNormal = rightHit.normal;
            }

            // Start wall running if conditions met
            if ((isWallLeft || isWallRight) && !isWallRunning)
            {
                float horizontalSpeed = new Vector3(velocity.x, 0, velocity.z).magnitude;
                if (horizontalSpeed > 3f && velocity.y < 5f && Time.time - lastWallJumpTime > 0.2f)
                {
                    StartWallRun();
                }
            }

            // Stop wall running if no walls
            if (isWallRunning && !isWallLeft && !isWallRight)
            {
                StopWallRun();
            }
        }

        private void StartWallRun()
        {
            isWallRunning = true;
            wallRunTimer = 0f;
            airJumpsRemaining = maxAirJumps; // Restore air jumps
            OnWallRunStart?.Invoke();
        }

        private void UpdateWallRun()
        {
            wallRunTimer += Time.deltaTime;

            // Calculate wall forward direction
            wallForward = Vector3.Cross(wallNormal, Vector3.up);
            if (Vector3.Dot(wallForward, transform.forward) < 0)
            {
                wallForward = -wallForward;
            }

            // Apply wall run movement
            velocity = wallForward * wallRunSpeed;
            velocity.y = wallRunGravity * (wallRunTimer / wallRunDuration);

            // Handle wall jump
            if (jumpInputDown)
            {
                WallJump();
                return;
            }

            // End wall run if timer expires
            if (wallRunTimer >= wallRunDuration)
            {
                StopWallRun();
            }
        }

        private void WallJump()
        {
            velocity = wallNormal * wallJumpSideForce + Vector3.up * wallJumpUpForce;
            lastWallJumpTime = Time.time;
            StopWallRun();
            OnWallJump?.Invoke();
        }

        private void StopWallRun()
        {
            if (isWallRunning)
            {
                isWallRunning = false;
                OnWallRunEnd?.Invoke();
            }
        }

        private void ResetWallRun()
        {
            wallRunTimer = 0f;
        }

        #endregion

        #region Sliding

        private void CheckForSlide()
        {
            if (!enableSliding) return;
            if (!isGrounded || isWallRunning || isGrappling || isMantling) return;

            if (Input.GetKeyDown(slideKey) && !isSliding)
            {
                float horizontalSpeed = new Vector3(velocity.x, 0, velocity.z).magnitude;
                if (horizontalSpeed > 4f && Time.time - lastSlideTime > 0.5f)
                {
                    StartSlide();
                }
            }

            if (!Input.GetKey(slideKey) && isSliding)
            {
                StopSlide();
            }
        }

        private void StartSlide()
        {
            isSliding = true;
            slideTimer = 0f;

            Vector3 horizontalVelocity = new Vector3(velocity.x, 0, velocity.z);
            if (horizontalVelocity.magnitude > 0.1f)
            {
                slideDirection = horizontalVelocity.normalized;
            }
            else
            {
                slideDirection = transform.forward;
            }

            currentSlideSpeed = Mathf.Max(horizontalVelocity.magnitude, slideSpeed);
            lastSlideTime = Time.time;

            OnSlideStart?.Invoke();
        }

        private void UpdateSlide()
        {
            slideTimer += Time.deltaTime;

            // Steer slightly
            float steer = moveInput.x * 1.5f;
            slideDirection = Quaternion.Euler(0, steer, 0) * slideDirection;

            // Decelerate
            currentSlideSpeed -= 5f * Time.deltaTime;
            currentSlideSpeed = Mathf.Max(0, currentSlideSpeed);

            velocity = slideDirection * currentSlideSpeed;
            velocity.y = -2f;

            // Update height
            currentHeight = Mathf.Lerp(currentHeight, slideHeight, 12f * Time.deltaTime);
            controller.height = currentHeight;
            controller.center = new Vector3(0, currentHeight / 2f, 0);

            // Handle slide jump
            if (jumpInputDown)
            {
                velocity.y = jumpForce;
                velocity += slideDirection * currentSlideSpeed * 0.5f;
                StopSlide();
                OnJump?.Invoke();
                return;
            }

            // End slide if too slow or time expired
            if (currentSlideSpeed < 3f || slideTimer >= slideDuration)
            {
                StopSlide();
            }
        }

        private void StopSlide()
        {
            if (isSliding)
            {
                isSliding = false;

                // Check if can stand
                if (!Physics.Raycast(transform.position, Vector3.up, standingHeight - currentHeight + 0.1f))
                {
                    currentHeight = standingHeight;
                    controller.height = currentHeight;
                    controller.center = new Vector3(0, currentHeight / 2f, 0);
                }

                OnSlideEnd?.Invoke();
            }
        }

        #endregion

        #region Grapple

        private void CheckForGrapple()
        {
            if (!enableGrapple) return;

            if (Input.GetKeyDown(grappleKey) && CanGrapple)
            {
                TryStartGrapple();
            }

            if (Input.GetKeyUp(grappleKey) && isGrappling)
            {
                StopGrapple(true);
            }
        }

        private void TryStartGrapple()
        {
            if (playerCamera == null) return;

            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            if (Physics.Raycast(ray, out RaycastHit hit, maxGrappleDistance, groundMask))
            {
                float distance = Vector3.Distance(transform.position, hit.point);
                if (distance >= 3f)
                {
                    StartGrapple(hit.point);
                }
            }
        }

        private void StartGrapple(Vector3 point)
        {
            isGrappling = true;
            grapplePoint = point;
            lastGrappleTime = Time.time;

            // Stop other movement states
            StopWallRun();
            StopSlide();

            if (grappleRope != null)
            {
                grappleRope.enabled = true;
                grappleRope.positionCount = 2;
            }

            OnGrappleStart?.Invoke(point);
        }

        private void UpdateGrapple()
        {
            Vector3 toPoint = grapplePoint - transform.position;
            float distance = toPoint.magnitude;

            if (distance <= 2f)
            {
                StopGrapple(true);
                return;
            }

            // Pull toward grapple point
            velocity = toPoint.normalized * grappleSpeed;
            velocity.y = Mathf.Max(velocity.y, -5f); // Limit downward velocity

            // Update rope visual
            if (grappleRope != null)
            {
                grappleRope.SetPosition(0, cameraTransform != null ? cameraTransform.position + cameraTransform.forward * 0.5f : transform.position);
                grappleRope.SetPosition(1, grapplePoint);
            }
        }

        private void StopGrapple(bool preserveMomentum)
        {
            if (isGrappling)
            {
                isGrappling = false;

                if (preserveMomentum)
                {
                    grappleVelocity = velocity * 0.7f;
                    velocity = grappleVelocity;
                }

                if (grappleRope != null)
                {
                    grappleRope.enabled = false;
                }

                lastGrappleTime = Time.time;
                OnGrappleEnd?.Invoke();
            }
        }

        #endregion

        #region Mantle

        private void CheckForMantle()
        {
            if (!enableMantle) return;
            if (isGrounded || isWallRunning || isGrappling || isSliding || isMantling) return;
            if (!autoMantle && !jumpInputHeld) return;

            // Check for ledge
            Vector3 checkOrigin = transform.position + Vector3.up * 0.5f;
            if (!Physics.Raycast(checkOrigin, transform.forward, out RaycastHit wallHit, 0.7f, groundMask))
            {
                return;
            }

            // Find ledge
            for (float height = 0.5f; height <= maxMantleHeight; height += 0.3f)
            {
                Vector3 highCheck = transform.position + Vector3.up * height + transform.forward * 0.4f;
                if (!Physics.Raycast(highCheck, transform.forward, 0.4f, groundMask))
                {
                    if (Physics.Raycast(highCheck + transform.forward * 0.3f, Vector3.down, out RaycastHit ledgeHit, height, groundMask))
                    {
                        if (Vector3.Angle(ledgeHit.normal, Vector3.up) < 45f)
                        {
                            StartMantle(ledgeHit.point + Vector3.up * mantleVerticalOffset - wallHit.normal * mantleForwardOffset);
                            return;
                        }
                    }
                }
            }
        }

        private void StartMantle(Vector3 target)
        {
            isMantling = true;
            mantleStart = transform.position;
            mantleTarget = target;
            mantleProgress = 0f;
            velocity = Vector3.zero;
            OnMantleStart?.Invoke();
        }

        private void UpdateMantle()
        {
            mantleProgress += Time.deltaTime * mantleSpeed;

            if (mantleProgress >= 1f)
            {
                SetPositionDirectly(mantleTarget);
                
                isMantling = false;
                OnMantleEnd?.Invoke();
                return;
            }

            // Arc movement
            float t = mantleProgress;
            Vector3 midPoint = (mantleStart + mantleTarget) / 2f + Vector3.up * 0.5f;
            
            Vector3 newPos;
            if (t < 0.5f)
            {
                newPos = Vector3.Lerp(mantleStart, midPoint, t * 2f);
            }
            else
            {
                newPos = Vector3.Lerp(midPoint, mantleTarget, (t - 0.5f) * 2f);
            }

            SetPositionDirectly(newPos);
        }

        #endregion

        #region Camera Effects

        private void UpdateCameraEffects()
        {
            // Camera tilt for wall running
            float targetTilt = 0f;
            if (isWallRunning)
            {
                targetTilt = isWallLeft ? cameraTiltAngle : -cameraTiltAngle;
            }

            currentCameraTilt = Mathf.Lerp(currentCameraTilt, targetTilt, 10f * Time.deltaTime);

            // Slide height adjustment
            if (!isSliding && currentHeight != standingHeight)
            {
                if (!Physics.Raycast(transform.position, Vector3.up, standingHeight - currentHeight + 0.1f))
                {
                    currentHeight = Mathf.Lerp(currentHeight, standingHeight, 12f * Time.deltaTime);
                    controller.height = currentHeight;
                    controller.center = new Vector3(0, currentHeight / 2f, 0);
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the transform position directly, bypassing CharacterController collision.
        /// </summary>
        private void SetPositionDirectly(Vector3 position)
        {
            controller.enabled = false;
            transform.position = position;
            controller.enabled = true;
        }

        /// <summary>
        /// Sets movement input directly (for AI or custom input).
        /// </summary>
        public void SetMoveInput(Vector2 input)
        {
            moveInput = Vector2.ClampMagnitude(input, 1f);
        }

        /// <summary>
        /// Sets look input directly (for AI or custom input).
        /// </summary>
        public void SetLookInput(Vector2 input)
        {
            lookInput = input;
        }

        /// <summary>
        /// Toggles cursor lock state.
        /// </summary>
        public void SetCursorLock(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        /// <summary>
        /// Forces a jump if possible.
        /// </summary>
        public void ForceJump()
        {
            TryJump();
        }

        /// <summary>
        /// Applies external velocity (e.g., from explosions, jump pads).
        /// </summary>
        public void AddVelocity(Vector3 additionalVelocity)
        {
            velocity += additionalVelocity;
        }

        /// <summary>
        /// Sets velocity directly.
        /// </summary>
        public void SetVelocity(Vector3 newVelocity)
        {
            velocity = newVelocity;
        }

        /// <summary>
        /// Resets grapple cooldown.
        /// </summary>
        public void ResetGrappleCooldown()
        {
            lastGrappleTime = 0f;
        }

        #endregion

        private void OnDrawGizmosSelected()
        {
            // Ground check
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, Vector3.down * groundCheckDistance);

            // Wall checks
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, -transform.right * wallCheckDistance);
            Gizmos.DrawRay(transform.position, transform.right * wallCheckDistance);

            // Grapple range
            if (enableGrapple)
            {
                Gizmos.color = new Color(0, 1, 0, 0.1f);
                Gizmos.DrawWireSphere(transform.position, maxGrappleDistance);
            }

            // Current state visualization
            if (isWallRunning)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(transform.position, wallForward * 2f);
                Gizmos.color = Color.magenta;
                Gizmos.DrawRay(transform.position, wallNormal * 2f);
            }

            if (isGrappling)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, grapplePoint);
                Gizmos.DrawWireSphere(grapplePoint, 0.5f);
            }
        }
    }
}