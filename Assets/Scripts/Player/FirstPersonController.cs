using UnityEngine;

namespace UsefulScripts.Player
{
    /// <summary>
    /// First-person character controller for 3D games.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 5f;
        [SerializeField] private float sprintSpeed = 8f;
        [SerializeField] private float crouchSpeed = 2.5f;
        [SerializeField] private float acceleration = 10f;

        [Header("Jumping")]
        [SerializeField] private float jumpHeight = 1.5f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private int maxAirJumps = 0;

        [Header("Mouse Look")]
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private float maxLookAngle = 90f;
        [SerializeField] private bool invertY = false;
        [SerializeField] private Transform cameraTransform;

        [Header("Crouching")]
        [SerializeField] private float standingHeight = 2f;
        [SerializeField] private float crouchHeight = 1f;
        [SerializeField] private float crouchTransitionSpeed = 8f;

        [Header("Head Bob")]
        [SerializeField] private bool enableHeadBob = true;
        [SerializeField] private float bobFrequency = 2f;
        [SerializeField] private float bobAmplitude = 0.05f;

        [Header("Ground Check")]
        [SerializeField] private LayerMask groundMask;
        [SerializeField] private float groundCheckDistance = 0.2f;

        // Components
        private CharacterController controller;

        // State
        private Vector3 velocity;
        private Vector3 moveDirection;
        private float cameraPitch;
        private bool isGrounded;
        private bool isSprinting;
        private bool isCrouching;
        private float currentHeight;
        private int airJumpsRemaining;
        private float bobTimer;
        private Vector3 originalCameraPosition;

        // Input
        private Vector2 moveInput;
        private Vector2 lookInput;
        private bool jumpInput;
        private bool sprintInput;
        private bool crouchInput;

        // Events
        public event System.Action OnJump;
        public event System.Action OnLand;
        public event System.Action<bool> OnSprintChanged;
        public event System.Action<bool> OnCrouchChanged;

        // Properties
        public bool IsGrounded => isGrounded;
        public bool IsSprinting => isSprinting;
        public bool IsCrouching => isCrouching;
        public bool IsMoving => moveInput.sqrMagnitude > 0.01f;
        public float CurrentSpeed => new Vector3(velocity.x, 0, velocity.z).magnitude;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            currentHeight = standingHeight;

            if (cameraTransform != null)
            {
                originalCameraPosition = cameraTransform.localPosition;
            }
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            GatherInput();
            UpdateGroundCheck();
            UpdateMovement();
            UpdateLook();
            UpdateCrouch();
            UpdateHeadBob();
            ApplyGravity();
            ApplyMovement();
        }

        private void GatherInput()
        {
            moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            lookInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
            jumpInput = Input.GetButtonDown("Jump");
            sprintInput = Input.GetKey(KeyCode.LeftShift);
            crouchInput = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C);
        }

        private void UpdateGroundCheck()
        {
            bool wasGrounded = isGrounded;
            isGrounded = controller.isGrounded || 
                         Physics.SphereCast(transform.position, controller.radius, Vector3.down, 
                                           out _, groundCheckDistance, groundMask);

            if (isGrounded && !wasGrounded)
            {
                airJumpsRemaining = maxAirJumps;
                OnLand?.Invoke();
            }
        }

        private void UpdateMovement()
        {
            // Handle sprint
            bool wasSprinting = isSprinting;
            isSprinting = sprintInput && !isCrouching && moveInput.y > 0;
            if (isSprinting != wasSprinting)
            {
                OnSprintChanged?.Invoke(isSprinting);
            }

            // Calculate speed
            float targetSpeed = isCrouching ? crouchSpeed : (isSprinting ? sprintSpeed : walkSpeed);

            // Calculate move direction
            Vector3 inputDirection = new Vector3(moveInput.x, 0, moveInput.y).normalized;
            moveDirection = transform.TransformDirection(inputDirection) * targetSpeed;
        }

        private void UpdateLook()
        {
            float mouseX = lookInput.x * mouseSensitivity;
            float mouseY = lookInput.y * mouseSensitivity * (invertY ? 1 : -1);

            // Rotate player horizontally
            transform.Rotate(Vector3.up * mouseX);

            // Rotate camera vertically
            cameraPitch += mouseY;
            cameraPitch = Mathf.Clamp(cameraPitch, -maxLookAngle, maxLookAngle);

            if (cameraTransform != null)
            {
                cameraTransform.localEulerAngles = new Vector3(cameraPitch, 0, 0);
            }
        }

        private void UpdateCrouch()
        {
            bool wasCrouching = isCrouching;
            
            if (crouchInput && isGrounded)
            {
                isCrouching = true;
                currentHeight = Mathf.Lerp(currentHeight, crouchHeight, crouchTransitionSpeed * Time.deltaTime);
            }
            else if (!crouchInput)
            {
                // Check if we can stand up
                if (!Physics.Raycast(transform.position, Vector3.up, standingHeight - currentHeight + 0.1f))
                {
                    isCrouching = false;
                    currentHeight = Mathf.Lerp(currentHeight, standingHeight, crouchTransitionSpeed * Time.deltaTime);
                }
            }

            controller.height = currentHeight;
            controller.center = new Vector3(0, currentHeight / 2f, 0);

            if (isCrouching != wasCrouching)
            {
                OnCrouchChanged?.Invoke(isCrouching);
            }
        }

        private void UpdateHeadBob()
        {
            if (!enableHeadBob || cameraTransform == null) return;

            if (isGrounded && IsMoving && !isCrouching)
            {
                float speed = isSprinting ? bobFrequency * 1.5f : bobFrequency;
                bobTimer += Time.deltaTime * speed;
                float bobOffset = Mathf.Sin(bobTimer * Mathf.PI * 2) * bobAmplitude;
                cameraTransform.localPosition = originalCameraPosition + new Vector3(0, bobOffset, 0);
            }
            else
            {
                bobTimer = 0;
                cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, 
                                                              originalCameraPosition, 
                                                              Time.deltaTime * 10f);
            }
        }

        private void ApplyGravity()
        {
            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2f; // Small downward force to keep grounded
            }

            // Jump
            if (jumpInput)
            {
                if (isGrounded)
                {
                    velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                    OnJump?.Invoke();
                }
                else if (airJumpsRemaining > 0)
                {
                    velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                    airJumpsRemaining--;
                    OnJump?.Invoke();
                }
            }

            velocity.y += gravity * Time.deltaTime;
        }

        private void ApplyMovement()
        {
            Vector3 move = new Vector3(moveDirection.x, velocity.y, moveDirection.z);
            controller.Move(move * Time.deltaTime);
        }

        /// <summary>
        /// Set movement input (for AI or custom input)
        /// </summary>
        public void SetMoveInput(Vector2 input)
        {
            moveInput = Vector2.ClampMagnitude(input, 1f);
        }

        /// <summary>
        /// Set look input (for AI or custom input)
        /// </summary>
        public void SetLookInput(Vector2 input)
        {
            lookInput = input;
        }

        /// <summary>
        /// Toggle cursor lock
        /// </summary>
        public void SetCursorLock(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
