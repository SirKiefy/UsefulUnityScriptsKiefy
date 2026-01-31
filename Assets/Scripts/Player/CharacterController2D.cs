using UnityEngine;

namespace UsefulScripts.Player
{
    /// <summary>
    /// Simple 2D character controller for platformers.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class CharacterController2D : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 8f;
        [SerializeField] private float acceleration = 50f;
        [SerializeField] private float deceleration = 50f;
        [SerializeField] private float airControlMultiplier = 0.5f;

        [Header("Jumping")]
        [SerializeField] private float jumpForce = 12f;
        [SerializeField] private float jumpCutMultiplier = 0.5f;
        [SerializeField] private float coyoteTime = 0.15f;
        [SerializeField] private float jumpBufferTime = 0.1f;
        [SerializeField] private int maxAirJumps = 0;

        [Header("Ground Check")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private Vector2 groundCheckSize = new Vector2(0.9f, 0.1f);
        [SerializeField] private LayerMask groundLayer;

        [Header("Gravity")]
        [SerializeField] private float gravityScale = 3f;
        [SerializeField] private float fallGravityMultiplier = 1.5f;
        [SerializeField] private float maxFallSpeed = 20f;

        // Components
        private Rigidbody2D rb;

        // State
        private float horizontalInput;
        private bool jumpInput;
        private bool jumpInputReleased;
        private bool isGrounded;
        private bool wasGrounded;
        private float coyoteTimeCounter;
        private float jumpBufferCounter;
        private int airJumpsRemaining;
        private bool isFacingRight = true;

        // Events
        public event System.Action OnJump;
        public event System.Action OnLand;
        public event System.Action<bool> OnDirectionChanged;

        // Properties
        public bool IsGrounded => isGrounded;
        public bool IsFacingRight => isFacingRight;
        public float HorizontalVelocity => rb.linearVelocity.x;
        public float VerticalVelocity => rb.linearVelocity.y;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = gravityScale;
        }

        private void Update()
        {
            GatherInput();
            UpdateTimers();
            CheckGround();
        }

        private void FixedUpdate()
        {
            Move();
            Jump();
            ApplyGravity();
        }

        private void GatherInput()
        {
            horizontalInput = Input.GetAxisRaw("Horizontal");
            
            if (Input.GetButtonDown("Jump"))
            {
                jumpInput = true;
                jumpBufferCounter = jumpBufferTime;
            }

            if (Input.GetButtonUp("Jump"))
            {
                jumpInputReleased = true;
            }
        }

        private void UpdateTimers()
        {
            if (isGrounded)
            {
                coyoteTimeCounter = coyoteTime;
            }
            else
            {
                coyoteTimeCounter -= Time.deltaTime;
            }

            jumpBufferCounter -= Time.deltaTime;
        }

        private void CheckGround()
        {
            wasGrounded = isGrounded;
            
            if (groundCheck != null)
            {
                isGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);
            }
            else
            {
                isGrounded = Physics2D.OverlapBox(transform.position + Vector3.down * 0.5f, groundCheckSize, 0f, groundLayer);
            }

            // Just landed
            if (isGrounded && !wasGrounded)
            {
                airJumpsRemaining = maxAirJumps;
                OnLand?.Invoke();
            }
        }

        private void Move()
        {
            float targetSpeed = horizontalInput * moveSpeed;
            float accelRate = Mathf.Abs(targetSpeed) > 0.01f ? acceleration : deceleration;
            
            if (!isGrounded)
            {
                accelRate *= airControlMultiplier;
            }

            float speedDiff = targetSpeed - rb.linearVelocity.x;
            float movement = speedDiff * accelRate * Time.fixedDeltaTime;

            rb.linearVelocity = new Vector2(rb.linearVelocity.x + movement, rb.linearVelocity.y);

            // Flip sprite
            if (horizontalInput > 0 && !isFacingRight)
            {
                Flip();
            }
            else if (horizontalInput < 0 && isFacingRight)
            {
                Flip();
            }
        }

        private void Jump()
        {
            // Ground/Coyote jump
            if (jumpBufferCounter > 0 && coyoteTimeCounter > 0)
            {
                PerformJump();
                jumpBufferCounter = 0;
                coyoteTimeCounter = 0;
            }
            // Air jump
            else if (jumpInput && !isGrounded && airJumpsRemaining > 0)
            {
                PerformJump();
                airJumpsRemaining--;
            }

            // Variable jump height
            if (jumpInputReleased && rb.linearVelocity.y > 0)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
            }

            jumpInput = false;
            jumpInputReleased = false;
        }

        private void PerformJump()
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            OnJump?.Invoke();
        }

        private void ApplyGravity()
        {
            if (rb.linearVelocity.y < 0)
            {
                rb.gravityScale = gravityScale * fallGravityMultiplier;
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, -maxFallSpeed));
            }
            else
            {
                rb.gravityScale = gravityScale;
            }
        }

        private void Flip()
        {
            isFacingRight = !isFacingRight;
            transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
            OnDirectionChanged?.Invoke(isFacingRight);
        }

        /// <summary>
        /// Set horizontal movement input (for AI or custom input)
        /// </summary>
        public void SetHorizontalInput(float input)
        {
            horizontalInput = Mathf.Clamp(input, -1f, 1f);
        }

        /// <summary>
        /// Trigger a jump (for AI or custom input)
        /// </summary>
        public void TriggerJump()
        {
            jumpInput = true;
            jumpBufferCounter = jumpBufferTime;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Vector3 checkPos = groundCheck != null ? groundCheck.position : transform.position + Vector3.down * 0.5f;
            Gizmos.DrawWireCube(checkPos, groundCheckSize);
        }
    }
}
