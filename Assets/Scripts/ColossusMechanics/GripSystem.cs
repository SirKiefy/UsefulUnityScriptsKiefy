using UnityEngine;

namespace UsefulScripts.ColossusMechanics
{
    /// <summary>
    /// Shadow of the Colossus-inspired grip and climbing system.
    /// Allows the player to grab onto surfaces and creatures, climb while managing stamina,
    /// and attack weak points while gripping.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class GripSystem : MonoBehaviour
    {
        [Header("Grip Detection")]
        [SerializeField] private float gripReachDistance = 1.5f;
        [SerializeField] private float gripCheckRadius = 0.3f;
        [SerializeField] private LayerMask gripMask = ~0;
        [SerializeField] private Transform gripCheckOrigin;

        [Header("Stamina")]
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float currentStamina;
        [SerializeField] private float staminaDrainWhileGripping = 5f;
        [SerializeField] private float staminaDrainWhileClimbing = 10f;
        [SerializeField] private float staminaDrainOnShake = 20f;
        [SerializeField] private float staminaRegenRate = 15f;
        [SerializeField] private float staminaRegenDelay = 1f;
        [SerializeField] private float lowStaminaThreshold = 20f;

        [Header("Grip Strength")]
        [SerializeField] private bool enableGripStrength = true;
        [SerializeField] private float maxGripStrength = 100f;
        [SerializeField] private float gripStrengthRecovery = 50f;
        [SerializeField] private float shakeGripDrain = 30f;
        [SerializeField] private AnimationCurve gripFalloffCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

        [Header("Climbing")]
        [SerializeField] private float climbSpeed = 3f;
        [SerializeField] private float climbAcceleration = 10f;
        [SerializeField] private float transitionToGripPointSpeed = 5f;
        [SerializeField] private float maxGripPointDistance = 2f;

        [Header("Jump Off")]
        [SerializeField] private float jumpOffForce = 8f;
        [SerializeField] private float jumpOffUpwardForce = 5f;
        [SerializeField] private float wallJumpCooldown = 0.3f;

        [Header("Attack While Gripping")]
        [SerializeField] private bool canAttackWhileGripping = true;
        [SerializeField] private float attackDamage = 50f;
        [SerializeField] private float attackCooldown = 1f;
        [SerializeField] private float attackStaminaCost = 10f;
        [SerializeField] private float chargeAttackMultiplier = 3f;
        [SerializeField] private float maxChargeTime = 2f;

        [Header("Input")]
        [SerializeField] private KeyCode gripKey = KeyCode.Mouse1;
        [SerializeField] private KeyCode attackKey = KeyCode.Mouse0;
        [SerializeField] private KeyCode jumpKey = KeyCode.Space;

        [Header("Visual Feedback")]
        [SerializeField] private bool enableCameraEffects = true;
        [SerializeField] private float lowStaminaCameraShake = 0.1f;

        // Components
        private CharacterController controller;
        private Camera playerCamera;

        // Grip state
        private bool isGripping;
        private bool isClimbing;
        private GripPoint currentGripPoint;
        private ClimbableColossus currentColossus;
        private Vector3 gripNormal;
        private Vector3 lastGripPosition;
        private float currentGripStrength;

        // Movement state
        private Vector3 climbVelocity;
        private Vector3 externalVelocity;
        private float lastJumpOffTime;

        // Stamina state
        private float staminaRegenTimer;
        private bool isStaminaDepleted;

        // Attack state
        private bool isAttacking;
        private bool isChargingAttack;
        private float attackChargeTime;
        private float lastAttackTime;

        // Events
        public event System.Action OnGripStart;
        public event System.Action OnGripEnd;
        public event System.Action OnGripFailed;
        public event System.Action<float, float> OnStaminaChanged;
        public event System.Action OnStaminaDepleted;
        public event System.Action OnStaminaRecovered;
        public event System.Action<float> OnAttack;
        public event System.Action OnChargeStart;
        public event System.Action<float> OnChargeComplete;
        public event System.Action OnJumpOff;
        public event System.Action OnShakeStart;
        public event System.Action OnShakeEnd;

        // Properties
        public bool IsGripping => isGripping;
        public bool IsClimbing => isClimbing;
        public float CurrentStamina => currentStamina;
        public float MaxStamina => maxStamina;
        public float StaminaPercent => currentStamina / maxStamina;
        public bool IsLowStamina => currentStamina <= lowStaminaThreshold;
        public bool IsStaminaDepleted => isStaminaDepleted;
        public GripPoint CurrentGripPoint => currentGripPoint;
        public ClimbableColossus CurrentColossus => currentColossus;
        public float GripStrength => currentGripStrength;
        public float GripStrengthPercent => currentGripStrength / maxGripStrength;
        public bool IsChargingAttack => isChargingAttack;
        public float ChargePercent => Mathf.Clamp01(attackChargeTime / maxChargeTime);

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            playerCamera = Camera.main;
            
            currentStamina = maxStamina;
            currentGripStrength = maxGripStrength;

            if (gripCheckOrigin == null)
            {
                gripCheckOrigin = transform;
            }
        }

        private void Update()
        {
            HandleGripInput();
            HandleClimbInput();
            HandleAttackInput();
            
            UpdateGrip();
            UpdateStamina();
            UpdateGripStrength();
        }

        private void HandleGripInput()
        {
            // Start grip
            if (Input.GetKeyDown(gripKey))
            {
                if (!isGripping && !isStaminaDepleted)
                {
                    TryStartGrip();
                }
            }

            // Release grip
            if (Input.GetKeyUp(gripKey))
            {
                if (isGripping)
                {
                    EndGrip(false);
                }
            }

            // Jump off
            if (Input.GetKeyDown(jumpKey) && isGripping)
            {
                JumpOff();
            }
        }

        private void HandleClimbInput()
        {
            if (!isGripping) return;

            float vertical = Input.GetAxisRaw("Vertical");
            float horizontal = Input.GetAxisRaw("Horizontal");

            isClimbing = Mathf.Abs(vertical) > 0.1f || Mathf.Abs(horizontal) > 0.1f;

            if (isClimbing && !isStaminaDepleted)
            {
                // Calculate climb direction relative to grip surface
                Vector3 up = -Physics.gravity.normalized;
                Vector3 right = Vector3.Cross(up, gripNormal).normalized;
                Vector3 climbUp = Vector3.Cross(gripNormal, right).normalized;

                Vector3 targetVelocity = (right * horizontal + climbUp * vertical) * climbSpeed;
                climbVelocity = Vector3.MoveTowards(climbVelocity, targetVelocity, climbAcceleration * Time.deltaTime);
            }
            else
            {
                climbVelocity = Vector3.MoveTowards(climbVelocity, Vector3.zero, climbAcceleration * 2f * Time.deltaTime);
            }
        }

        private void HandleAttackInput()
        {
            if (!isGripping || !canAttackWhileGripping) return;

            // Start charge attack
            if (Input.GetKeyDown(attackKey))
            {
                if (Time.time - lastAttackTime >= attackCooldown && currentStamina >= attackStaminaCost)
                {
                    isChargingAttack = true;
                    attackChargeTime = 0f;
                    OnChargeStart?.Invoke();
                }
            }

            // Continue charging
            if (Input.GetKey(attackKey) && isChargingAttack)
            {
                attackChargeTime += Time.deltaTime;
                attackChargeTime = Mathf.Min(attackChargeTime, maxChargeTime);
            }

            // Release attack
            if (Input.GetKeyUp(attackKey) && isChargingAttack)
            {
                PerformAttack();
            }
        }

        private void TryStartGrip()
        {
            if (Time.time - lastJumpOffTime < wallJumpCooldown) return;

            // Check for nearby grip point
            GripPoint nearestPoint = FindNearestGripPoint();
            
            if (nearestPoint != null)
            {
                StartGripOnPoint(nearestPoint);
                return;
            }

            // Check for any climbable surface
            Vector3 checkDir = playerCamera != null ? playerCamera.transform.forward : transform.forward;
            if (Physics.SphereCast(gripCheckOrigin.position, gripCheckRadius, checkDir, out RaycastHit hit, gripReachDistance, gripMask))
            {
                ClimbableColossus colossus = hit.collider.GetComponentInParent<ClimbableColossus>();
                if (colossus != null)
                {
                    // Find nearest grip point on this colossus
                    nearestPoint = colossus.FindNearestGripPointInRange(hit.point, maxGripPointDistance);
                    if (nearestPoint != null)
                    {
                        StartGripOnPoint(nearestPoint);
                        return;
                    }
                }

                // Grip surface directly if no specific grip point found
                StartGripOnSurface(hit.point, hit.normal, hit.collider);
                return;
            }

            OnGripFailed?.Invoke();
        }

        private GripPoint FindNearestGripPoint()
        {
            Collider[] nearbyColliders = Physics.OverlapSphere(gripCheckOrigin.position, gripReachDistance, gripMask);
            
            GripPoint nearest = null;
            float nearestDistance = gripReachDistance;

            foreach (Collider col in nearbyColliders)
            {
                GripPoint point = col.GetComponent<GripPoint>();
                if (point == null)
                {
                    point = col.GetComponentInParent<GripPoint>();
                }

                if (point != null && point.IsWithinGripRange(gripCheckOrigin.position))
                {
                    float distance = Vector3.Distance(gripCheckOrigin.position, point.GetWorldPosition());
                    if (distance < nearestDistance && point.IsValidApproach(gripCheckOrigin.position))
                    {
                        nearestDistance = distance;
                        nearest = point;
                    }
                }
            }

            return nearest;
        }

        private void StartGripOnPoint(GripPoint point)
        {
            isGripping = true;
            currentGripPoint = point;
            currentGripStrength = maxGripStrength;
            
            // Get the parent colossus if any
            currentColossus = point.GetComponentInParent<ClimbableColossus>();
            if (currentColossus != null)
            {
                currentColossus.OnShakeStart += HandleShakeStart;
                currentColossus.OnShakeEnd += HandleShakeEnd;
                currentColossus.OnPositionChanged += HandleColossusMove;
            }

            gripNormal = -point.GetForwardDirection();
            lastGripPosition = point.GetWorldPosition();

            point.ShowHighlight();
            OnGripStart?.Invoke();
        }

        private void StartGripOnSurface(Vector3 position, Vector3 normal, Collider surface)
        {
            isGripping = true;
            currentGripPoint = null;
            currentGripStrength = maxGripStrength;
            
            // Get the parent colossus if any
            currentColossus = surface.GetComponentInParent<ClimbableColossus>();
            if (currentColossus != null)
            {
                currentColossus.OnShakeStart += HandleShakeStart;
                currentColossus.OnShakeEnd += HandleShakeEnd;
                currentColossus.OnPositionChanged += HandleColossusMove;
            }

            gripNormal = normal;
            lastGripPosition = position;

            OnGripStart?.Invoke();
        }

        private void EndGrip(bool forced)
        {
            if (!isGripping) return;

            isGripping = false;
            isClimbing = false;
            isChargingAttack = false;
            climbVelocity = Vector3.zero;

            if (currentGripPoint != null)
            {
                currentGripPoint.HideHighlight();
            }

            if (currentColossus != null)
            {
                currentColossus.OnShakeStart -= HandleShakeStart;
                currentColossus.OnShakeEnd -= HandleShakeEnd;
                currentColossus.OnPositionChanged -= HandleColossusMove;
            }

            currentGripPoint = null;
            currentColossus = null;

            OnGripEnd?.Invoke();
        }

        private void UpdateGrip()
        {
            if (!isGripping) return;

            // Move with the colossus
            Vector3 targetPosition = transform.position;
            
            if (currentGripPoint != null)
            {
                // Stay attached to grip point
                Vector3 pointPosition = currentGripPoint.GetWorldPosition();
                Vector3 offset = pointPosition - lastGripPosition;
                targetPosition += offset;
                lastGripPosition = pointPosition;

                // Apply shake offset if colossus is shaking
                if (currentColossus != null && currentColossus.IsShaking)
                {
                    targetPosition += currentColossus.GetShakeOffset();
                }
            }

            // Apply climb movement
            targetPosition += climbVelocity * Time.deltaTime;

            // Apply external velocity (from being moved by colossus)
            targetPosition += externalVelocity * Time.deltaTime;
            externalVelocity = Vector3.Lerp(externalVelocity, Vector3.zero, Time.deltaTime * 5f);

            // Move the player
            controller.enabled = false;
            transform.position = targetPosition;
            controller.enabled = true;

            // Check if still near a valid grip surface
            if (!IsNearGripSurface())
            {
                EndGrip(true);
            }

            // Try to transition to nearest grip point while climbing
            if (isClimbing && currentGripPoint == null)
            {
                GripPoint nearestPoint = FindNearestGripPoint();
                if (nearestPoint != null)
                {
                    TransitionToGripPoint(nearestPoint);
                }
            }
        }

        private bool IsNearGripSurface()
        {
            return Physics.SphereCast(transform.position, gripCheckRadius, -gripNormal, out _, gripReachDistance * 0.5f, gripMask);
        }

        private void TransitionToGripPoint(GripPoint newPoint)
        {
            if (currentGripPoint != null)
            {
                currentGripPoint.HideHighlight();
            }

            currentGripPoint = newPoint;
            currentGripPoint.ShowHighlight();
            gripNormal = -newPoint.GetForwardDirection();
            lastGripPosition = newPoint.GetWorldPosition();
        }

        private void UpdateStamina()
        {
            float previousStamina = currentStamina;

            if (isGripping)
            {
                // Drain stamina while gripping
                float drainRate = isClimbing ? staminaDrainWhileClimbing : staminaDrainWhileGripping;
                
                // Apply fur bonus
                if (currentGripPoint != null && currentGripPoint.HasFur)
                {
                    drainRate /= currentGripPoint.FurGripBonus;
                }

                // Apply shake multiplier
                if (currentColossus != null && currentColossus.IsShaking)
                {
                    drainRate *= currentColossus.CurrentShakeDrainMultiplier;
                }

                currentStamina -= drainRate * Time.deltaTime;
                staminaRegenTimer = staminaRegenDelay;

                // Check for depletion
                if (currentStamina <= 0)
                {
                    currentStamina = 0;
                    if (!isStaminaDepleted)
                    {
                        isStaminaDepleted = true;
                        OnStaminaDepleted?.Invoke();
                        EndGrip(true);
                    }
                }
            }
            else
            {
                // Regenerate stamina
                staminaRegenTimer -= Time.deltaTime;
                if (staminaRegenTimer <= 0 && currentStamina < maxStamina)
                {
                    currentStamina += staminaRegenRate * Time.deltaTime;
                    currentStamina = Mathf.Min(currentStamina, maxStamina);

                    // Check for recovery
                    if (isStaminaDepleted && currentStamina >= lowStaminaThreshold)
                    {
                        isStaminaDepleted = false;
                        OnStaminaRecovered?.Invoke();
                    }
                }
            }

            if (Mathf.Abs(currentStamina - previousStamina) > 0.01f)
            {
                OnStaminaChanged?.Invoke(currentStamina, maxStamina);
            }
        }

        private void UpdateGripStrength()
        {
            if (!enableGripStrength) return;

            if (isGripping && currentColossus != null && currentColossus.IsShaking)
            {
                // Drain grip strength during shake
                currentGripStrength -= shakeGripDrain * Time.deltaTime;
                
                if (currentGripStrength <= 0)
                {
                    currentGripStrength = 0;
                    EndGrip(true);
                }
            }
            else
            {
                // Recover grip strength
                currentGripStrength += gripStrengthRecovery * Time.deltaTime;
                currentGripStrength = Mathf.Min(currentGripStrength, maxGripStrength);
            }
        }

        private void HandleShakeStart()
        {
            OnShakeStart?.Invoke();

            // Apply immediate stamina drain
            currentStamina -= staminaDrainOnShake;
            if (currentStamina < 0) currentStamina = 0;
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        }

        private void HandleShakeEnd()
        {
            OnShakeEnd?.Invoke();
        }

        private void HandleColossusMove(Vector3 delta)
        {
            if (isGripping)
            {
                externalVelocity += delta / Time.deltaTime * 0.5f;
            }
        }

        private void JumpOff()
        {
            if (!isGripping) return;

            // Calculate jump direction (away from surface)
            Vector3 jumpDir = gripNormal + Vector3.up * 0.5f;
            jumpDir.Normalize();

            // Add player input to jump direction
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            Vector3 inputDir = transform.right * horizontal + transform.forward * vertical;
            jumpDir += inputDir * 0.5f;
            jumpDir.Normalize();

            // Calculate jump velocity
            Vector3 jumpVelocity = jumpDir * jumpOffForce + Vector3.up * jumpOffUpwardForce;

            // End grip first
            EndGrip(false);

            // Apply jump force
            lastJumpOffTime = Time.time;
            externalVelocity = jumpVelocity;

            OnJumpOff?.Invoke();
        }

        private void PerformAttack()
        {
            if (!isGripping || currentStamina < attackStaminaCost)
            {
                isChargingAttack = false;
                return;
            }

            // Calculate damage based on charge time
            float chargeMultiplier = 1f + (ChargePercent * (chargeAttackMultiplier - 1f));
            float damage = attackDamage * chargeMultiplier;

            // Consume stamina
            currentStamina -= attackStaminaCost;
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);

            // Apply damage to colossus
            if (currentColossus != null)
            {
                currentColossus.TakeDamage(damage, currentGripPoint);
            }

            // Fire events
            OnChargeComplete?.Invoke(ChargePercent);
            OnAttack?.Invoke(damage);

            isChargingAttack = false;
            attackChargeTime = 0f;
            lastAttackTime = Time.time;
        }

        /// <summary>
        /// Gets the external velocity from jumping off or being thrown.
        /// </summary>
        public Vector3 GetExternalVelocity()
        {
            Vector3 velocity = externalVelocity;
            externalVelocity = Vector3.zero;
            return velocity;
        }

        /// <summary>
        /// Forces the player to release their grip.
        /// </summary>
        public void ForceReleaseGrip()
        {
            EndGrip(true);
        }

        /// <summary>
        /// Adds stamina (e.g., from a power-up).
        /// </summary>
        public void AddStamina(float amount)
        {
            currentStamina = Mathf.Min(currentStamina + amount, maxStamina);
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        }

        /// <summary>
        /// Sets stamina to maximum.
        /// </summary>
        public void RefillStamina()
        {
            currentStamina = maxStamina;
            isStaminaDepleted = false;
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        }

        /// <summary>
        /// Checks if the player can currently grip.
        /// </summary>
        public bool CanGrip()
        {
            return !isStaminaDepleted && Time.time - lastJumpOffTime >= wallJumpCooldown;
        }

        private void OnDrawGizmosSelected()
        {
            if (gripCheckOrigin == null) gripCheckOrigin = transform;

            // Draw grip check sphere
            Gizmos.color = isGripping ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(gripCheckOrigin.position, gripReachDistance);

            // Draw grip direction
            if (playerCamera != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(gripCheckOrigin.position, playerCamera.transform.forward * gripReachDistance);
            }

            // Draw current grip point
            if (isGripping && currentGripPoint != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, currentGripPoint.GetWorldPosition());
                Gizmos.DrawWireSphere(currentGripPoint.GetWorldPosition(), 0.2f);
            }

            // Draw grip normal
            if (isGripping)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawRay(transform.position, gripNormal * 2f);
            }
        }
    }
}
