using UnityEngine;

namespace UsefulScripts.FPS
{
    /// <summary>
    /// Titanfall-inspired wall running system.
    /// Allows the player to run along walls when in the air and near a wall.
    /// Includes camera tilt, momentum preservation, and wall jump mechanics.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class WallRunning : MonoBehaviour
    {
        [Header("Wall Detection")]
        [SerializeField] private LayerMask wallMask = ~0;
        [SerializeField] private float wallCheckDistance = 1f;
        [SerializeField] private float minWallHeight = 2f;
        [SerializeField] private float minHeightFromGround = 1f;

        [Header("Wall Running")]
        [SerializeField] private float wallRunSpeed = 12f;
        [SerializeField] private float wallRunDuration = 2f;
        [SerializeField] private float wallRunGravity = -2f;
        [SerializeField] private float wallRunUpwardForce = 2f;
        [SerializeField] private float wallStickForce = 5f;
        [SerializeField] private float minSpeedForWallRun = 4f;

        [Header("Wall Jump")]
        [SerializeField] private float wallJumpUpForce = 8f;
        [SerializeField] private float wallJumpSideForce = 12f;
        [SerializeField] private float wallJumpCooldown = 0.3f;

        [Header("Camera Effects")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float cameraTiltAngle = 15f;
        [SerializeField] private float cameraTiltSpeed = 10f;
        [SerializeField] private float fovIncrease = 10f;
        [SerializeField] private float fovChangeSpeed = 5f;

        // Components
        private CharacterController controller;
        private Camera playerCamera;

        // Wall running state
        private bool isWallRunning;
        private bool isWallLeft;
        private bool isWallRight;
        private float wallRunTimer;
        private Vector3 wallNormal;
        private Vector3 wallForward;
        private float lastWallJumpTime;
        private float defaultFov;
        private float currentTilt;

        // External velocity (set by other movement systems)
        private Vector3 externalVelocity;

        // Events
        public event System.Action OnWallRunStart;
        public event System.Action OnWallRunEnd;
        public event System.Action<bool> OnWallRunSideChanged; // true = left, false = right
        public event System.Action OnWallJump;

        // Properties
        public bool IsWallRunning => isWallRunning;
        public bool IsWallLeft => isWallLeft;
        public bool IsWallRight => isWallRight;
        public Vector3 WallNormal => wallNormal;
        public Vector3 WallForward => wallForward;
        public float WallRunTimeRemaining => Mathf.Max(0, wallRunDuration - wallRunTimer);

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            
            if (cameraTransform != null)
            {
                playerCamera = cameraTransform.GetComponent<Camera>();
                if (playerCamera != null)
                {
                    defaultFov = playerCamera.fieldOfView;
                }
            }
        }

        private void Update()
        {
            CheckForWalls();
            UpdateWallRun();
            UpdateCameraEffects();
            HandleWallJumpInput();
        }

        private void CheckForWalls()
        {
            // Don't check for walls if grounded
            if (controller.isGrounded)
            {
                isWallLeft = false;
                isWallRight = false;
                return;
            }

            // Check if we're high enough from the ground
            if (!IsHighEnoughFromGround())
            {
                isWallLeft = false;
                isWallRight = false;
                return;
            }

            // Check for walls on both sides
            RaycastHit leftHit, rightHit;
            isWallLeft = Physics.Raycast(transform.position, -transform.right, out leftHit, wallCheckDistance, wallMask);
            isWallRight = Physics.Raycast(transform.position, transform.right, out rightHit, wallCheckDistance, wallMask);

            // Verify wall height
            if (isWallLeft)
            {
                isWallLeft = IsValidWall(leftHit.point, leftHit.normal);
            }
            if (isWallRight)
            {
                isWallRight = IsValidWall(rightHit.point, rightHit.normal);
            }

            // Set wall normal based on which wall we're on
            if (isWallLeft)
            {
                wallNormal = leftHit.normal;
            }
            else if (isWallRight)
            {
                wallNormal = rightHit.normal;
            }
        }

        private bool IsHighEnoughFromGround()
        {
            return !Physics.Raycast(transform.position, Vector3.down, minHeightFromGround);
        }

        private bool IsValidWall(Vector3 hitPoint, Vector3 hitNormal)
        {
            // Check if the wall is vertical enough
            float wallAngle = Vector3.Angle(hitNormal, Vector3.up);
            if (wallAngle < 80f || wallAngle > 100f)
            {
                return false;
            }

            // Check wall height - wall must extend high enough
            if (Physics.Raycast(hitPoint + Vector3.up * 0.5f, -hitNormal, wallCheckDistance * 1.5f, wallMask))
            {
                // Check if wall continues up to minimum height
                if (Physics.Raycast(hitPoint + Vector3.up * minWallHeight, -hitNormal, wallCheckDistance * 1.5f, wallMask))
                {
                    return true;
                }
                // Wall doesn't extend high enough
                return false;
            }

            // No wall detected at base height
            return false;
        }

        private void UpdateWallRun()
        {
            bool shouldWallRun = CanWallRun();

            if (shouldWallRun && !isWallRunning)
            {
                StartWallRun();
            }
            else if (!shouldWallRun && isWallRunning)
            {
                StopWallRun();
            }

            if (isWallRunning)
            {
                wallRunTimer += Time.deltaTime;

                // Calculate wall forward direction (direction to run along the wall)
                wallForward = Vector3.Cross(wallNormal, Vector3.up);
                if (Vector3.Dot(wallForward, transform.forward) < 0)
                {
                    wallForward = -wallForward;
                }

                // Apply wall run movement
                Vector3 wallRunVelocity = wallForward * wallRunSpeed;
                
                // Add slight upward force at the start
                float upwardMultiplier = Mathf.Clamp01(1f - (wallRunTimer / wallRunDuration));
                wallRunVelocity.y = wallRunGravity + (wallRunUpwardForce * upwardMultiplier);

                // Add force to stick to wall
                wallRunVelocity -= wallNormal * wallStickForce;

                // Move the player
                controller.Move((wallRunVelocity + externalVelocity) * Time.deltaTime);

                // End wall run if timer expires
                if (wallRunTimer >= wallRunDuration)
                {
                    StopWallRun();
                }
            }
        }

        private bool CanWallRun()
        {
            // Must be in the air
            if (controller.isGrounded) return false;

            // Must have a wall nearby
            if (!isWallLeft && !isWallRight) return false;

            // Must be moving fast enough
            float horizontalSpeed = new Vector3(controller.velocity.x, 0, controller.velocity.z).magnitude;
            if (!isWallRunning && horizontalSpeed < minSpeedForWallRun) return false;

            // Wall jump cooldown
            if (Time.time - lastWallJumpTime < wallJumpCooldown) return false;

            // Must have wall run time remaining
            if (wallRunTimer >= wallRunDuration) return false;

            return true;
        }

        private void StartWallRun()
        {
            isWallRunning = true;
            wallRunTimer = 0f;
            OnWallRunStart?.Invoke();
            OnWallRunSideChanged?.Invoke(isWallLeft);
        }

        private void StopWallRun()
        {
            if (isWallRunning)
            {
                isWallRunning = false;
                OnWallRunEnd?.Invoke();
            }
        }

        private void UpdateCameraEffects()
        {
            // Camera tilt
            float targetTilt = 0f;
            if (isWallRunning)
            {
                targetTilt = isWallLeft ? cameraTiltAngle : -cameraTiltAngle;
            }

            currentTilt = Mathf.Lerp(currentTilt, targetTilt, cameraTiltSpeed * Time.deltaTime);

            if (cameraTransform != null)
            {
                Vector3 currentRotation = cameraTransform.localEulerAngles;
                cameraTransform.localEulerAngles = new Vector3(currentRotation.x, currentRotation.y, currentTilt);
            }

            // FOV change
            if (playerCamera != null)
            {
                float targetFov = isWallRunning ? defaultFov + fovIncrease : defaultFov;
                playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFov, fovChangeSpeed * Time.deltaTime);
            }
        }

        private void HandleWallJumpInput()
        {
            if (isWallRunning && Input.GetButtonDown("Jump"))
            {
                WallJump();
            }
        }

        /// <summary>
        /// Performs a wall jump, pushing the player away from the wall.
        /// </summary>
        public void WallJump()
        {
            if (!isWallRunning) return;

            // Calculate jump direction
            Vector3 jumpDirection = wallNormal * wallJumpSideForce + Vector3.up * wallJumpUpForce;
            
            // Apply the jump as external velocity
            externalVelocity = jumpDirection;
            
            lastWallJumpTime = Time.time;
            StopWallRun();
            
            // Reset wall run timer for the next wall
            wallRunTimer = 0f;
            
            OnWallJump?.Invoke();
        }

        /// <summary>
        /// Resets the wall run timer, allowing for another full wall run.
        /// Call this when the player lands.
        /// </summary>
        public void ResetWallRun()
        {
            wallRunTimer = 0f;
            isWallRunning = false;
        }

        /// <summary>
        /// Sets external velocity to be applied during wall running.
        /// </summary>
        public void SetExternalVelocity(Vector3 velocity)
        {
            externalVelocity = velocity;
        }

        /// <summary>
        /// Gets the velocity applied by wall running.
        /// </summary>
        public Vector3 GetWallRunVelocity()
        {
            if (!isWallRunning) return Vector3.zero;
            
            Vector3 velocity = wallForward * wallRunSpeed;
            float upwardMultiplier = Mathf.Clamp01(1f - (wallRunTimer / wallRunDuration));
            velocity.y = wallRunGravity + (wallRunUpwardForce * upwardMultiplier);
            velocity -= wallNormal * wallStickForce;
            
            return velocity;
        }

        private void OnDrawGizmosSelected()
        {
            // Draw wall check rays
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, -transform.right * wallCheckDistance);
            Gizmos.DrawRay(transform.position, transform.right * wallCheckDistance);

            // Draw ground check ray
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, Vector3.down * minHeightFromGround);

            // Draw wall forward direction when wall running
            if (isWallRunning)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawRay(transform.position, wallForward * 2f);
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(transform.position, wallNormal * 2f);
            }
        }
    }
}
