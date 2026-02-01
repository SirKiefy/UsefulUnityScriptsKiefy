using System;
using System.Collections.Generic;
using UnityEngine;

namespace UsefulScripts.Player
{
    #region Enums

    /// <summary>
    /// Defines player movement states.
    /// </summary>
    public enum MovementState
    {
        Idle,
        Walking,
        Running,
        Sprinting,
        Crouching,
        Crawling,
        Jumping,
        Falling,
        Landing,
        Swimming,
        Climbing,
        Flying,
        Mounted,
        Dodging,
        Dashing,
        Sliding,
        WallRunning,
        Stunned,
        Dead
    }

    /// <summary>
    /// Defines player action states (can be combined with movement).
    /// </summary>
    [Flags]
    public enum ActionState
    {
        None = 0,
        Attacking = 1,
        Blocking = 2,
        Casting = 4,
        Interacting = 8,
        Aiming = 16,
        Reloading = 32,
        UsingItem = 64,
        Channeling = 128
    }

    /// <summary>
    /// Defines the type of ground the player is on.
    /// </summary>
    public enum GroundType
    {
        Normal,
        Ice,
        Mud,
        Sand,
        Water,
        Lava,
        Grass,
        Stone,
        Metal,
        Wood
    }

    #endregion

    /// <summary>
    /// Advanced configuration for the player character controller.
    /// </summary>
    [CreateAssetMenu(fileName = "NewPlayerControllerConfig", menuName = "UsefulScripts/Player/Controller Config")]
    public class PlayerControllerConfig : ScriptableObject
    {
        [Header("Walking/Running")]
        public float walkSpeed = 4f;
        public float runSpeed = 7f;
        public float sprintSpeed = 12f;
        public float crouchSpeed = 2f;
        public float crawlSpeed = 1f;
        public float acceleration = 50f;
        public float deceleration = 50f;
        public float airControlMultiplier = 0.3f;

        [Header("Jumping")]
        public float jumpForce = 10f;
        public float jumpCutMultiplier = 0.5f;
        public int maxAirJumps = 1;
        public float coyoteTime = 0.15f;
        public float jumpBufferTime = 0.15f;
        public float jumpCooldown = 0.1f;

        [Header("Gravity")]
        public float gravityScale = 3f;
        public float fallGravityMultiplier = 1.5f;
        public float maxFallSpeed = 25f;
        public float fastFallMultiplier = 2f;

        [Header("Dodge/Dash")]
        public float dodgeSpeed = 15f;
        public float dodgeDuration = 0.3f;
        public float dodgeCooldown = 0.5f;
        public float dashSpeed = 25f;
        public float dashDuration = 0.15f;
        public float dashCooldown = 1f;
        public bool dodgeHasIframes = true;
        public float iframeDuration = 0.2f;

        [Header("Slide")]
        public float slideSpeed = 14f;
        public float slideDuration = 0.8f;
        public float slideCooldown = 0.3f;
        public float slideFriction = 0.95f;

        [Header("Climbing")]
        public float climbSpeed = 3f;
        public float climbStamina = 10f;
        public float ledgeGrabDistance = 0.5f;

        [Header("Swimming")]
        public float swimSpeed = 4f;
        public float swimSprintSpeed = 6f;
        public float diveSpeed = 5f;
        public float surfaceSpeed = 3f;
        public float waterDrag = 0.9f;
        public float oxygenDuration = 60f;

        [Header("Wall Movement")]
        public float wallRunSpeed = 10f;
        public float wallRunDuration = 1.5f;
        public float wallJumpForce = 12f;
        public float wallSlideSpeed = 2f;
        public float wallCheckDistance = 0.5f;

        [Header("Flying/Gliding")]
        public float flySpeed = 8f;
        public float glideSpeed = 6f;
        public float glideGravity = 0.5f;
        public float flyAscendSpeed = 5f;
        public float flyDescendSpeed = 7f;

        [Header("Stamina Costs")]
        public float sprintStaminaCost = 10f;
        public float jumpStaminaCost = 5f;
        public float dodgeStaminaCost = 15f;
        public float dashStaminaCost = 25f;
        public float climbStaminaCost = 8f;
        public float swimStaminaCost = 5f;

        [Header("Ground Detection")]
        public float groundCheckDistance = 0.1f;
        public float groundCheckRadius = 0.3f;
        public LayerMask groundLayer;
        public float slopeLimit = 45f;

        [Header("Step Handling")]
        public float stepHeight = 0.3f;
        public float stepSmooth = 0.1f;

        [Header("Camera")]
        public float lookSensitivityX = 2f;
        public float lookSensitivityY = 2f;
        public float minPitch = -80f;
        public float maxPitch = 80f;
        public bool invertY = false;

        [Header("Collision")]
        public float standingHeight = 2f;
        public float crouchingHeight = 1f;
        public float crawlingHeight = 0.5f;
        public float bodyRadius = 0.3f;
    }

    /// <summary>
    /// Detailed advanced player character controller with modular movement systems.
    /// Supports walking, running, sprinting, crouching, jumping, dodging, dashing,
    /// climbing, swimming, flying, wall running, and mounted movement.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class AdvancedPlayerController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private PlayerControllerConfig config;

        [Header("References")]
        [SerializeField] private CharacterSheet characterSheet;
        [SerializeField] private PlayerStatController statController;
        [SerializeField] private MountSystem mountSystem;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private CapsuleCollider bodyCollider;

        [Header("Ground Check")]
        [SerializeField] private Transform groundCheckPoint;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private LayerMask climbableLayer;
        [SerializeField] private LayerMask waterLayer;

        // Components
        private Rigidbody rb;

        // Movement State
        private MovementState currentState = MovementState.Idle;
        private MovementState previousState = MovementState.Idle;
        private ActionState currentAction = ActionState.None;

        // Input
        private Vector2 moveInput;
        private Vector2 lookInput;
        private bool jumpPressed;
        private bool jumpHeld;
        private bool sprintHeld;
        private bool crouchPressed;
        private bool crouchHeld;
        private bool dodgePressed;
        private bool dashPressed;

        // Ground Detection
        private bool isGrounded;
        private bool wasGrounded;
        private RaycastHit groundHit;
        private GroundType currentGroundType = GroundType.Normal;
        private float groundAngle;

        // Jumping
        private float coyoteTimeCounter;
        private float jumpBufferCounter;
        private float jumpCooldownTimer;
        private int airJumpsRemaining;
        private bool isJumping;

        // Dodge/Dash
        private float dodgeCooldownTimer;
        private float dashCooldownTimer;
        private float dodgeTimer;
        private float dashTimer;
        private Vector3 dodgeDirection;
        private Vector3 dashDirection;
        private bool isDodging;
        private bool isDashing;
        private bool hasIframes;

        // Slide
        private float slideCooldownTimer;
        private float slideTimer;
        private bool isSliding;
        private Vector3 slideDirection;

        // Wall Movement
        private bool isTouchingWall;
        private bool isWallRunning;
        private float wallRunTimer;
        private Vector3 wallNormal;
        private float wallSlideSpeed;

        // Climbing
        private bool isClimbing;
        private bool canClimb;
        private Vector3 climbSurfaceNormal;

        // Swimming
        private bool isInWater;
        private bool isUnderwater;
        private float currentOxygen;
        private float maxOxygen;

        // Flying
        private bool canFly;
        private bool isFlying;
        private bool isGliding;

        // Camera
        private float pitch;
        private float yaw;

        // Movement Modifiers
        private float speedMultiplier = 1f;
        private float jumpMultiplier = 1f;
        private float gravityMultiplier = 1f;
        private Dictionary<string, float> activeSpeedModifiers = new Dictionary<string, float>();

        // State timers
        private float stateTimer;
        private float landingRecoveryTimer;
        private float stunTimer;

        #region Events

        public event Action<MovementState, MovementState> OnStateChanged;
        public event Action OnJump;
        public event Action OnLand;
        public event Action OnDodge;
        public event Action OnDash;
        public event Action OnSlideStart;
        public event Action OnSlideEnd;
        public event Action<bool> OnGroundedChanged;
        public event Action OnStartClimbing;
        public event Action OnStopClimbing;
        public event Action OnEnterWater;
        public event Action OnExitWater;
        public event Action OnStartFlying;
        public event Action OnStopFlying;
        public event Action<float> OnOxygenChanged;

        #endregion

        #region Properties

        public MovementState CurrentState => currentState;
        public ActionState CurrentAction => currentAction;
        public bool IsGrounded => isGrounded;
        public bool IsMoving => moveInput.magnitude > 0.1f;
        public bool IsSprinting => sprintHeld && isGrounded && currentState == MovementState.Running;
        public bool IsCrouching => currentState == MovementState.Crouching || currentState == MovementState.Crawling;
        public bool IsJumping => isJumping;
        public bool IsDodging => isDodging;
        public bool IsDashing => isDashing;
        public bool IsSliding => isSliding;
        public bool IsClimbing => isClimbing;
        public bool IsSwimming => currentState == MovementState.Swimming;
        public bool IsFlying => isFlying;
        public bool IsGliding => isGliding;
        public bool IsWallRunning => isWallRunning;
        public bool IsMounted => mountSystem != null && mountSystem.IsMounted;
        public bool HasIframes => hasIframes;
        public GroundType CurrentGroundType => currentGroundType;
        public float CurrentSpeed => rb != null ? new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z).magnitude : 0f;
        public Vector3 Velocity => rb != null ? rb.linearVelocity : Vector3.zero;
        public float GroundAngle => groundAngle;
        public float Oxygen => currentOxygen;
        public float OxygenPercent => maxOxygen > 0 ? currentOxygen / maxOxygen : 1f;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();

            if (bodyCollider == null)
            {
                bodyCollider = GetComponent<CapsuleCollider>();
            }

            if (characterSheet == null)
            {
                characterSheet = GetComponent<CharacterSheet>();
            }

            if (statController == null)
            {
                statController = GetComponent<PlayerStatController>();
            }

            if (mountSystem == null)
            {
                mountSystem = GetComponent<MountSystem>();
            }

            InitializeController();
        }

        private void Start()
        {
            maxOxygen = config != null ? config.oxygenDuration : 60f;
            currentOxygen = maxOxygen;
        }

        private void Update()
        {
            GatherInput();
            UpdateTimers();
            CheckGround();
            CheckWater();
            CheckWalls();
            UpdateState();
            UpdateCamera();
        }

        private void FixedUpdate()
        {
            ApplyMovement();
            ApplyGravity();
        }

        #endregion

        #region Initialization

        private void InitializeController()
        {
            if (rb != null)
            {
                rb.freezeRotation = true;
                rb.useGravity = false;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            }

            if (config == null)
            {
                Debug.LogWarning("AdvancedPlayerController: No config assigned, using defaults");
            }

            if (groundLayer == 0 && config != null)
            {
                groundLayer = config.groundLayer;
            }
        }

        #endregion

        #region Input

        private void GatherInput()
        {
            // Movement input
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            moveInput = new Vector2(horizontal, vertical).normalized;

            // Look input
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");
            lookInput = new Vector2(mouseX, mouseY);

            // Action inputs
            if (Input.GetButtonDown("Jump"))
            {
                jumpPressed = true;
                jumpBufferCounter = config != null ? config.jumpBufferTime : 0.15f;
            }
            jumpHeld = Input.GetButton("Jump");

            sprintHeld = Input.GetButton("Sprint") || Input.GetKey(KeyCode.LeftShift);

            if (Input.GetButtonDown("Crouch") || Input.GetKeyDown(KeyCode.LeftControl))
            {
                crouchPressed = true;
            }
            crouchHeld = Input.GetButton("Crouch") || Input.GetKey(KeyCode.LeftControl);

            // Dodge/Dash (using key codes as fallback)
            if (Input.GetKeyDown(KeyCode.LeftAlt))
            {
                dodgePressed = true;
            }

            if (Input.GetKeyDown(KeyCode.Q))
            {
                dashPressed = true;
            }
        }

        /// <summary>
        /// Sets movement input externally (for AI or custom input).
        /// </summary>
        public void SetMovementInput(Vector2 input)
        {
            moveInput = Vector2.ClampMagnitude(input, 1f);
        }

        /// <summary>
        /// Sets look input externally.
        /// </summary>
        public void SetLookInput(Vector2 input)
        {
            lookInput = input;
        }

        /// <summary>
        /// Triggers a jump.
        /// </summary>
        public void TriggerJump()
        {
            jumpPressed = true;
            jumpBufferCounter = config != null ? config.jumpBufferTime : 0.15f;
        }

        /// <summary>
        /// Triggers a dodge.
        /// </summary>
        public void TriggerDodge()
        {
            dodgePressed = true;
        }

        /// <summary>
        /// Triggers a dash.
        /// </summary>
        public void TriggerDash()
        {
            dashPressed = true;
        }

        #endregion

        #region Ground Detection

        private void CheckGround()
        {
            wasGrounded = isGrounded;

            Vector3 checkPos = groundCheckPoint != null ? groundCheckPoint.position : transform.position;
            float checkDist = config != null ? config.groundCheckDistance : 0.1f;
            float checkRadius = config != null ? config.groundCheckRadius : 0.3f;

            isGrounded = Physics.SphereCast(
                checkPos + Vector3.up * checkRadius,
                checkRadius,
                Vector3.down,
                out groundHit,
                checkDist + checkRadius,
                groundLayer
            );

            if (isGrounded)
            {
                groundAngle = Vector3.Angle(groundHit.normal, Vector3.up);
                DetermineGroundType();

                if (!wasGrounded)
                {
                    HandleLanding();
                }

                coyoteTimeCounter = config != null ? config.coyoteTime : 0.15f;
                airJumpsRemaining = config != null ? config.maxAirJumps : 1;
            }
            else
            {
                coyoteTimeCounter -= Time.deltaTime;

                if (wasGrounded && rb.linearVelocity.y <= 0 && !isJumping)
                {
                    // Just left ground without jumping
                }
            }

            if (wasGrounded != isGrounded)
            {
                OnGroundedChanged?.Invoke(isGrounded);
            }
        }

        private void DetermineGroundType()
        {
            if (groundHit.collider == null)
            {
                currentGroundType = GroundType.Normal;
                return;
            }

            // Check for tagged surfaces or physic materials
            string tag = groundHit.collider.tag;

            currentGroundType = tag switch
            {
                "Ice" => GroundType.Ice,
                "Mud" => GroundType.Mud,
                "Sand" => GroundType.Sand,
                "Water" => GroundType.Water,
                "Lava" => GroundType.Lava,
                "Grass" => GroundType.Grass,
                "Stone" => GroundType.Stone,
                "Metal" => GroundType.Metal,
                "Wood" => GroundType.Wood,
                _ => GroundType.Normal
            };
        }

        private void HandleLanding()
        {
            isJumping = false;
            OnLand?.Invoke();

            // Calculate landing impact
            float fallSpeed = Mathf.Abs(previousState == MovementState.Falling ? rb.linearVelocity.y : 0);

            // Apply landing recovery based on fall speed
            if (fallSpeed > 15f)
            {
                landingRecoveryTimer = 0.3f;
            }
            else if (fallSpeed > 10f)
            {
                landingRecoveryTimer = 0.1f;
            }
        }

        #endregion

        #region Water Detection

        private void CheckWater()
        {
            bool wasInWater = isInWater;

            // Check if in water volume
            Collider[] waterColliders = Physics.OverlapSphere(transform.position, 0.5f, waterLayer);
            isInWater = waterColliders.Length > 0;

            if (isInWater)
            {
                // Check if head is underwater
                Vector3 headPos = transform.position + Vector3.up * (config != null ? config.standingHeight - 0.2f : 1.8f);
                isUnderwater = Physics.CheckSphere(headPos, 0.2f, waterLayer);

                // Oxygen management
                if (isUnderwater)
                {
                    currentOxygen -= Time.deltaTime;
                    currentOxygen = Mathf.Max(0, currentOxygen);
                    OnOxygenChanged?.Invoke(OxygenPercent);

                    if (currentOxygen <= 0 && statController != null)
                    {
                        statController.TakeDamage(10f * Time.deltaTime, ElementType.Water);
                    }
                }
                else
                {
                    currentOxygen = Mathf.Min(maxOxygen, currentOxygen + Time.deltaTime * 5f);
                    OnOxygenChanged?.Invoke(OxygenPercent);
                }
            }
            else if (wasInWater)
            {
                // Restore oxygen when out of water
                currentOxygen = maxOxygen;
                OnOxygenChanged?.Invoke(OxygenPercent);
            }

            if (isInWater && !wasInWater)
            {
                OnEnterWater?.Invoke();
            }
            else if (!isInWater && wasInWater)
            {
                OnExitWater?.Invoke();
            }
        }

        #endregion

        #region Wall Detection

        private void CheckWalls()
        {
            float checkDist = config != null ? config.wallCheckDistance : 0.5f;

            // Check for walls on sides
            Vector3 right = transform.right;
            Vector3 left = -transform.right;

            bool rightWall = Physics.Raycast(transform.position + Vector3.up, right, out RaycastHit rightHit, checkDist, climbableLayer);
            bool leftWall = Physics.Raycast(transform.position + Vector3.up, left, out RaycastHit leftHit, checkDist, climbableLayer);

            isTouchingWall = rightWall || leftWall;

            if (isTouchingWall)
            {
                wallNormal = rightWall ? rightHit.normal : leftHit.normal;
                canClimb = (climbableLayer.value & (1 << (rightWall ? rightHit : leftHit).collider.gameObject.layer)) != 0;
            }
            else
            {
                canClimb = false;
            }
        }

        #endregion

        #region State Management

        private void UpdateState()
        {
            previousState = currentState;

            // Check for state transitions
            if (stunTimer > 0)
            {
                currentState = MovementState.Stunned;
            }
            else if (statController != null && statController.IsDead)
            {
                currentState = MovementState.Dead;
            }
            else if (IsMounted)
            {
                currentState = MovementState.Mounted;
            }
            else if (isDashing)
            {
                currentState = MovementState.Dashing;
            }
            else if (isDodging)
            {
                currentState = MovementState.Dodging;
            }
            else if (isSliding)
            {
                currentState = MovementState.Sliding;
            }
            else if (isWallRunning)
            {
                currentState = MovementState.WallRunning;
            }
            else if (isClimbing)
            {
                currentState = MovementState.Climbing;
            }
            else if (isFlying)
            {
                currentState = MovementState.Flying;
            }
            else if (isInWater && !isGrounded)
            {
                currentState = MovementState.Swimming;
            }
            else if (!isGrounded)
            {
                if (rb.linearVelocity.y > 0.1f && isJumping)
                {
                    currentState = MovementState.Jumping;
                }
                else
                {
                    currentState = MovementState.Falling;
                }
            }
            else if (landingRecoveryTimer > 0)
            {
                currentState = MovementState.Landing;
            }
            else if (crouchHeld)
            {
                currentState = moveInput.magnitude > 0.1f ? MovementState.Crouching : MovementState.Crouching;
            }
            else if (moveInput.magnitude > 0.1f)
            {
                if (sprintHeld && CanSprint())
                {
                    currentState = MovementState.Sprinting;
                }
                else if (sprintHeld || moveInput.magnitude > 0.5f)
                {
                    currentState = MovementState.Running;
                }
                else
                {
                    currentState = MovementState.Walking;
                }
            }
            else
            {
                currentState = MovementState.Idle;
            }

            // Handle state changes
            if (currentState != previousState)
            {
                OnStateChanged?.Invoke(previousState, currentState);
            }

            // Update collider height for crouching
            UpdateColliderHeight();

            // Handle dodge/dash input
            HandleDodgeDash();

            // Handle sliding
            HandleSlide();

            // Handle climbing
            HandleClimbing();

            // Handle flying
            HandleFlying();

            // Handle wall running
            HandleWallRunning();
        }

        private void UpdateColliderHeight()
        {
            if (bodyCollider == null) return;

            float targetHeight = currentState switch
            {
                MovementState.Crouching => config != null ? config.crouchingHeight : 1f,
                MovementState.Crawling => config != null ? config.crawlingHeight : 0.5f,
                MovementState.Sliding => config != null ? config.crouchingHeight : 1f,
                _ => config != null ? config.standingHeight : 2f
            };

            // Smoothly transition height
            bodyCollider.height = Mathf.Lerp(bodyCollider.height, targetHeight, Time.deltaTime * 10f);
            bodyCollider.center = new Vector3(0, bodyCollider.height / 2f, 0);
        }

        private bool CanSprint()
        {
            if (statController == null) return true;

            float sprintCost = (config != null ? config.sprintStaminaCost : 10f) * Time.deltaTime;
            return statController.HasResource(ResourceType.Stamina, sprintCost);
        }

        #endregion

        #region Movement

        private void ApplyMovement()
        {
            if (currentState == MovementState.Stunned || currentState == MovementState.Dead)
            {
                rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
                return;
            }

            Vector3 targetVelocity = CalculateTargetVelocity();

            // Get current horizontal velocity
            Vector3 currentHorizontal = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);

            // Calculate acceleration
            float accel = moveInput.magnitude > 0.1f ?
                (config != null ? config.acceleration : 50f) :
                (config != null ? config.deceleration : 50f);

            if (!isGrounded)
            {
                accel *= config != null ? config.airControlMultiplier : 0.3f;
            }

            // Apply ground type modifiers
            accel *= GetGroundTypeModifier();

            // Lerp towards target velocity
            Vector3 newHorizontal = Vector3.Lerp(currentHorizontal, targetVelocity, accel * Time.fixedDeltaTime);

            // Apply velocity
            rb.linearVelocity = new Vector3(newHorizontal.x, rb.linearVelocity.y, newHorizontal.z);

            // Handle jumping
            HandleJump();

            // Consume stamina for sprinting
            if (currentState == MovementState.Sprinting && statController != null)
            {
                float cost = (config != null ? config.sprintStaminaCost : 10f) * Time.fixedDeltaTime;
                statController.ConsumeStamina(cost);
            }
        }

        private Vector3 CalculateTargetVelocity()
        {
            float speed = GetCurrentMaxSpeed();

            // Get movement direction based on camera
            Vector3 forward = cameraTransform != null ? cameraTransform.forward : transform.forward;
            Vector3 right = cameraTransform != null ? cameraTransform.right : transform.right;

            forward.y = 0;
            right.y = 0;
            forward.Normalize();
            right.Normalize();

            Vector3 moveDir = (forward * moveInput.y + right * moveInput.x).normalized;

            // Special movement handling
            if (isDodging)
            {
                return dodgeDirection * (config != null ? config.dodgeSpeed : 15f);
            }

            if (isDashing)
            {
                return dashDirection * (config != null ? config.dashSpeed : 25f);
            }

            if (isSliding)
            {
                return slideDirection * (config != null ? config.slideSpeed : 14f) * slideCooldownTimer;
            }

            // Apply speed multipliers
            speed *= speedMultiplier * CalculateTotalSpeedModifier();

            return moveDir * speed;
        }

        private float GetCurrentMaxSpeed()
        {
            if (config == null) return 7f;

            return currentState switch
            {
                MovementState.Walking => config.walkSpeed,
                MovementState.Running => config.runSpeed,
                MovementState.Sprinting => config.sprintSpeed,
                MovementState.Crouching => config.crouchSpeed,
                MovementState.Crawling => config.crawlSpeed,
                MovementState.Swimming => sprintHeld ? config.swimSprintSpeed : config.swimSpeed,
                MovementState.Flying => config.flySpeed,
                MovementState.Climbing => config.climbSpeed,
                MovementState.WallRunning => config.wallRunSpeed,
                MovementState.Mounted => mountSystem != null ? mountSystem.GetMountSpeed(sprintHeld) : config.runSpeed,
                _ => config.walkSpeed
            };
        }

        private float GetGroundTypeModifier()
        {
            return currentGroundType switch
            {
                GroundType.Ice => 0.3f,
                GroundType.Mud => 0.6f,
                GroundType.Sand => 0.8f,
                GroundType.Water => 0.7f,
                _ => 1f
            };
        }

        private float CalculateTotalSpeedModifier()
        {
            float total = 1f;
            foreach (var mod in activeSpeedModifiers.Values)
            {
                total *= mod;
            }

            // Apply character sheet speed bonus
            if (characterSheet != null)
            {
                float speedStat = characterSheet.GetSecondaryStat(SecondaryStat.MovementSpeed);
                total *= speedStat / 100f;
            }

            return total;
        }

        #endregion

        #region Jumping

        private void HandleJump()
        {
            if (jumpCooldownTimer > 0) return;

            bool canJump = false;

            // Ground jump
            if (jumpBufferCounter > 0 && coyoteTimeCounter > 0)
            {
                canJump = true;
            }
            // Air jump
            else if (jumpPressed && !isGrounded && airJumpsRemaining > 0)
            {
                canJump = true;
                airJumpsRemaining--;
            }

            if (canJump)
            {
                // Check stamina
                if (statController != null)
                {
                    float cost = config != null ? config.jumpStaminaCost : 5f;
                    if (!statController.ConsumeStamina(cost))
                    {
                        canJump = false;
                    }
                }

                if (canJump)
                {
                    PerformJump();
                }
            }

            // Variable jump height
            if (!jumpHeld && rb.linearVelocity.y > 0 && isJumping)
            {
                float cutMult = config != null ? config.jumpCutMultiplier : 0.5f;
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y * cutMult, rb.linearVelocity.z);
            }

            jumpPressed = false;
        }

        private void PerformJump()
        {
            float jumpForce = config != null ? config.jumpForce : 10f;
            jumpForce *= jumpMultiplier;

            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);

            isJumping = true;
            jumpBufferCounter = 0;
            coyoteTimeCounter = 0;
            jumpCooldownTimer = config != null ? config.jumpCooldown : 0.1f;

            OnJump?.Invoke();
        }

        #endregion

        #region Dodge and Dash

        private void HandleDodgeDash()
        {
            // Handle dodge
            if (dodgePressed && dodgeCooldownTimer <= 0 && !isDodging && !isDashing)
            {
                if (statController == null || statController.ConsumeStamina(config != null ? config.dodgeStaminaCost : 15f))
                {
                    StartDodge();
                }
            }

            // Handle dash
            if (dashPressed && dashCooldownTimer <= 0 && !isDodging && !isDashing)
            {
                if (statController == null || statController.ConsumeStamina(config != null ? config.dashStaminaCost : 25f))
                {
                    StartDash();
                }
            }

            // Update dodge
            if (isDodging)
            {
                dodgeTimer -= Time.deltaTime;
                if (dodgeTimer <= 0)
                {
                    EndDodge();
                }
            }

            // Update dash
            if (isDashing)
            {
                dashTimer -= Time.deltaTime;
                if (dashTimer <= 0)
                {
                    EndDash();
                }
            }

            // Update iframes
            if (hasIframes && config != null)
            {
                float iframeDur = config.iframeDuration;
                if (dodgeTimer < (config.dodgeDuration - iframeDur))
                {
                    hasIframes = false;
                }
            }

            dodgePressed = false;
            dashPressed = false;
        }

        private void StartDodge()
        {
            isDodging = true;
            dodgeTimer = config != null ? config.dodgeDuration : 0.3f;
            dodgeCooldownTimer = config != null ? config.dodgeCooldown : 0.5f;

            // Determine dodge direction
            if (moveInput.magnitude > 0.1f)
            {
                Vector3 forward = cameraTransform != null ? cameraTransform.forward : transform.forward;
                Vector3 right = cameraTransform != null ? cameraTransform.right : transform.right;
                forward.y = 0;
                right.y = 0;
                dodgeDirection = (forward.normalized * moveInput.y + right.normalized * moveInput.x).normalized;
            }
            else
            {
                dodgeDirection = -transform.forward;
            }

            // Enable iframes
            if (config != null && config.dodgeHasIframes)
            {
                hasIframes = true;
                if (statController != null)
                {
                    statController.StartInvincibility(config.iframeDuration);
                }
            }

            OnDodge?.Invoke();
        }

        private void EndDodge()
        {
            isDodging = false;
            hasIframes = false;
        }

        private void StartDash()
        {
            isDashing = true;
            dashTimer = config != null ? config.dashDuration : 0.15f;
            dashCooldownTimer = config != null ? config.dashCooldown : 1f;

            // Dash in facing/move direction
            if (moveInput.magnitude > 0.1f)
            {
                Vector3 forward = cameraTransform != null ? cameraTransform.forward : transform.forward;
                Vector3 right = cameraTransform != null ? cameraTransform.right : transform.right;
                forward.y = 0;
                right.y = 0;
                dashDirection = (forward.normalized * moveInput.y + right.normalized * moveInput.x).normalized;
            }
            else
            {
                dashDirection = transform.forward;
            }

            OnDash?.Invoke();
        }

        private void EndDash()
        {
            isDashing = false;
        }

        #endregion

        #region Sliding

        private void HandleSlide()
        {
            // Start slide when crouching while running/sprinting
            if (crouchPressed && (currentState == MovementState.Running || currentState == MovementState.Sprinting)
                && slideCooldownTimer <= 0 && !isSliding)
            {
                StartSlide();
            }

            if (isSliding)
            {
                slideTimer -= Time.deltaTime;

                // Apply friction
                float friction = config != null ? config.slideFriction : 0.95f;
                slideDirection *= Mathf.Pow(friction, Time.deltaTime * 60f);

                if (slideTimer <= 0 || !crouchHeld)
                {
                    EndSlide();
                }
            }

            crouchPressed = false;
        }

        private void StartSlide()
        {
            isSliding = true;
            slideTimer = config != null ? config.slideDuration : 0.8f;
            slideCooldownTimer = config != null ? config.slideCooldown : 0.3f;

            // Slide in current movement direction
            Vector3 forward = cameraTransform != null ? cameraTransform.forward : transform.forward;
            Vector3 right = cameraTransform != null ? cameraTransform.right : transform.right;
            forward.y = 0;
            right.y = 0;
            slideDirection = (forward.normalized * moveInput.y + right.normalized * moveInput.x).normalized;

            if (slideDirection.magnitude < 0.1f)
            {
                slideDirection = transform.forward;
            }

            OnSlideStart?.Invoke();
        }

        private void EndSlide()
        {
            isSliding = false;
            OnSlideEnd?.Invoke();
        }

        #endregion

        #region Climbing

        private void HandleClimbing()
        {
            // Start climbing when touching climbable wall and pressing towards it
            if (canClimb && isTouchingWall && !isClimbing && !isGrounded)
            {
                float dot = Vector3.Dot(transform.forward, -wallNormal);
                if (dot > 0.5f && moveInput.y > 0)
                {
                    StartClimbing();
                }
            }

            if (isClimbing)
            {
                // Consume stamina
                if (statController != null)
                {
                    float cost = (config != null ? config.climbStaminaCost : 8f) * Time.deltaTime;
                    if (!statController.ConsumeStamina(cost))
                    {
                        StopClimbing();
                        return;
                    }
                }

                // Stop if not touching wall or pressing away
                if (!isTouchingWall || moveInput.y < -0.5f)
                {
                    StopClimbing();
                    return;
                }

                // Climbing movement
                float climbSpeed = config != null ? config.climbSpeed : 3f;
                rb.linearVelocity = new Vector3(0, moveInput.y * climbSpeed, 0);

                // Jump off wall
                if (jumpPressed)
                {
                    StopClimbing();
                    Vector3 jumpDir = (wallNormal + Vector3.up).normalized;
                    float jumpForce = config != null ? config.wallJumpForce : 12f;
                    rb.linearVelocity = jumpDir * jumpForce;
                    OnJump?.Invoke();
                }
            }
        }

        private void StartClimbing()
        {
            isClimbing = true;
            climbSurfaceNormal = wallNormal;
            rb.useGravity = false;
            OnStartClimbing?.Invoke();
        }

        private void StopClimbing()
        {
            isClimbing = false;
            rb.useGravity = true;
            OnStopClimbing?.Invoke();
        }

        #endregion

        #region Wall Running

        private void HandleWallRunning()
        {
            // Start wall running when in air, touching wall, and moving
            if (!isWallRunning && isTouchingWall && !isGrounded && !isClimbing &&
                moveInput.magnitude > 0.1f && rb.linearVelocity.y > -5f)
            {
                StartWallRun();
            }

            if (isWallRunning)
            {
                wallRunTimer -= Time.deltaTime;

                if (!isTouchingWall || wallRunTimer <= 0 || isGrounded)
                {
                    StopWallRun();
                    return;
                }

                // Wall run movement
                float wallRunSpeed = config != null ? config.wallRunSpeed : 10f;
                Vector3 wallForward = Vector3.Cross(wallNormal, Vector3.up);

                // Determine direction based on input
                float dot = Vector3.Dot(transform.forward, wallForward);
                if (dot < 0) wallForward = -wallForward;

                rb.linearVelocity = new Vector3(
                    wallForward.x * wallRunSpeed,
                    config != null ? -config.wallSlideSpeed : -2f,
                    wallForward.z * wallRunSpeed
                );

                // Wall jump
                if (jumpPressed)
                {
                    StopWallRun();
                    Vector3 jumpDir = (wallNormal + Vector3.up).normalized;
                    float jumpForce = config != null ? config.wallJumpForce : 12f;
                    rb.linearVelocity = jumpDir * jumpForce;
                    OnJump?.Invoke();
                }
            }
        }

        private void StartWallRun()
        {
            isWallRunning = true;
            wallRunTimer = config != null ? config.wallRunDuration : 1.5f;
        }

        private void StopWallRun()
        {
            isWallRunning = false;
        }

        #endregion

        #region Flying

        private void HandleFlying()
        {
            // Check if player can fly (from mount or ability)
            canFly = (mountSystem != null && mountSystem.IsMounted && mountSystem.CanMountFly());

            if (!canFly && isFlying)
            {
                StopFlying();
                return;
            }

            // Toggle flying with jump while mounted on flying creature
            if (canFly && jumpPressed && !isFlying && !isGrounded)
            {
                StartFlying();
            }
            else if (isFlying && jumpPressed && isGrounded)
            {
                StopFlying();
            }

            if (isFlying)
            {
                // Flying movement
                float flySpeed = config != null ? config.flySpeed : 8f;
                float ascendSpeed = config != null ? config.flyAscendSpeed : 5f;
                float descendSpeed = config != null ? config.flyDescendSpeed : 7f;

                Vector3 moveDir = CalculateTargetVelocity().normalized;
                Vector3 velocity = moveDir * flySpeed;

                // Ascend/descend
                if (jumpHeld)
                {
                    velocity.y = ascendSpeed;
                }
                else if (crouchHeld)
                {
                    velocity.y = -descendSpeed;
                }
                else
                {
                    velocity.y = 0;
                }

                rb.linearVelocity = velocity;
            }
        }

        private void StartFlying()
        {
            isFlying = true;
            rb.useGravity = false;
            OnStartFlying?.Invoke();
        }

        private void StopFlying()
        {
            isFlying = false;
            rb.useGravity = true;
            OnStopFlying?.Invoke();
        }

        /// <summary>
        /// Enables gliding mode.
        /// </summary>
        public void StartGliding()
        {
            if (!isGrounded && !isFlying)
            {
                isGliding = true;
            }
        }

        /// <summary>
        /// Disables gliding mode.
        /// </summary>
        public void StopGliding()
        {
            isGliding = false;
        }

        #endregion

        #region Gravity

        private void ApplyGravity()
        {
            if (isGrounded || isFlying || isClimbing || currentState == MovementState.Swimming)
            {
                return;
            }

            float gravityScale = config != null ? config.gravityScale : 3f;
            float fallMult = config != null ? config.fallGravityMultiplier : 1.5f;
            float maxFall = config != null ? config.maxFallSpeed : 25f;

            // Apply stronger gravity when falling
            float gravity = gravityScale;
            if (rb.linearVelocity.y < 0)
            {
                gravity *= fallMult;
            }

            // Gliding reduces gravity
            if (isGliding)
            {
                gravity = config != null ? config.glideGravity : 0.5f;
            }

            // Fast fall
            if (crouchHeld && rb.linearVelocity.y < 0)
            {
                gravity *= config != null ? config.fastFallMultiplier : 2f;
            }

            gravity *= gravityMultiplier;

            // Apply gravity
            rb.linearVelocity += Vector3.down * gravity * Time.fixedDeltaTime * 10f;

            // Clamp fall speed
            if (rb.linearVelocity.y < -maxFall)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, -maxFall, rb.linearVelocity.z);
            }
        }

        #endregion

        #region Camera

        private void UpdateCamera()
        {
            if (config == null) return;

            float sensX = config.lookSensitivityX;
            float sensY = config.lookSensitivityY;

            yaw += lookInput.x * sensX;
            pitch -= lookInput.y * sensY * (config.invertY ? -1f : 1f);

            pitch = Mathf.Clamp(pitch, config.minPitch, config.maxPitch);

            // Apply rotation to player body (horizontal)
            transform.rotation = Quaternion.Euler(0, yaw, 0);

            // Apply rotation to camera (vertical)
            if (cameraTransform != null)
            {
                cameraTransform.localRotation = Quaternion.Euler(pitch, 0, 0);
            }
        }

        #endregion

        #region Timers

        private void UpdateTimers()
        {
            if (jumpBufferCounter > 0) jumpBufferCounter -= Time.deltaTime;
            if (jumpCooldownTimer > 0) jumpCooldownTimer -= Time.deltaTime;
            if (dodgeCooldownTimer > 0) dodgeCooldownTimer -= Time.deltaTime;
            if (dashCooldownTimer > 0) dashCooldownTimer -= Time.deltaTime;
            if (slideCooldownTimer > 0) slideCooldownTimer -= Time.deltaTime;
            if (landingRecoveryTimer > 0) landingRecoveryTimer -= Time.deltaTime;
            if (stunTimer > 0) stunTimer -= Time.deltaTime;
            stateTimer += Time.deltaTime;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds a speed modifier.
        /// </summary>
        public void AddSpeedModifier(string id, float multiplier)
        {
            activeSpeedModifiers[id] = multiplier;
        }

        /// <summary>
        /// Removes a speed modifier.
        /// </summary>
        public void RemoveSpeedModifier(string id)
        {
            activeSpeedModifiers.Remove(id);
        }

        /// <summary>
        /// Sets the overall speed multiplier.
        /// </summary>
        public void SetSpeedMultiplier(float multiplier)
        {
            speedMultiplier = Mathf.Max(0f, multiplier);
        }

        /// <summary>
        /// Sets the jump multiplier.
        /// </summary>
        public void SetJumpMultiplier(float multiplier)
        {
            jumpMultiplier = Mathf.Max(0f, multiplier);
        }

        /// <summary>
        /// Sets the gravity multiplier.
        /// </summary>
        public void SetGravityMultiplier(float multiplier)
        {
            gravityMultiplier = multiplier;
        }

        /// <summary>
        /// Applies stun for a duration.
        /// </summary>
        public void ApplyStun(float duration)
        {
            stunTimer = duration;
        }

        /// <summary>
        /// Teleports the player to a position.
        /// </summary>
        public void Teleport(Vector3 position)
        {
            transform.position = position;
            rb.linearVelocity = Vector3.zero;
        }

        /// <summary>
        /// Applies an impulse force.
        /// </summary>
        public void ApplyImpulse(Vector3 force)
        {
            rb.AddForce(force, ForceMode.Impulse);
        }

        /// <summary>
        /// Applies knockback from a source.
        /// </summary>
        public void ApplyKnockback(Vector3 source, float force)
        {
            Vector3 direction = (transform.position - source).normalized;
            direction.y = 0.3f;
            direction.Normalize();
            rb.AddForce(direction * force, ForceMode.Impulse);
        }

        /// <summary>
        /// Gets a status summary.
        /// </summary>
        public string GetStatusSummary()
        {
            return $"State: {currentState} | Speed: {CurrentSpeed:F1} | Grounded: {isGrounded}";
        }

        #endregion
    }
}
