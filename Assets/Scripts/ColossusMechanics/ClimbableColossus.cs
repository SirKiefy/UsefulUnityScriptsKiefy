using UnityEngine;

namespace UsefulScripts.ColossusMechanics
{
    /// <summary>
    /// Attach to entities (like a colossus) that can be climbed.
    /// Manages grip points and shake events that test the player's grip.
    /// </summary>
    public class ClimbableColossus : MonoBehaviour
    {
        [Header("Colossus Settings")]
        [SerializeField] private string colossusName = "Colossus";
        [SerializeField] private float health = 1000f;
        [SerializeField] private float currentHealth;
        [SerializeField] private bool isAlive = true;

        [Header("Shake Settings")]
        [SerializeField] private bool canShake = true;
        [SerializeField] private float shakeInterval = 5f;
        [SerializeField] private float shakeIntervalVariance = 2f;
        [SerializeField] private float shakeDuration = 1.5f;
        [SerializeField] private float shakeIntensity = 1f;
        [SerializeField] private float staminaDrainMultiplierDuringShake = 3f;

        [Header("Movement")]
        [SerializeField] private bool isMoving = false;
        [SerializeField] private float movementSpeed = 5f;
        [SerializeField] private Transform[] waypoints;
        [SerializeField] private bool loopWaypoints = true;

        [Header("Detection")]
        [SerializeField] private float playerDetectionRadius = 20f;
        [SerializeField] private LayerMask playerLayer = ~0;
        [SerializeField] private float aggroShakeMultiplier = 1.5f;

        // Shake state
        private bool isShaking;
        private float shakeTimer;
        private float nextShakeTime;
        private Vector3 shakeOffset;
        private float originalShakeIntensity;
        private bool isCustomShake;

        // Movement state
        private int currentWaypointIndex;
        private Vector3 originalPosition;

        // Cached components
        private GripPoint[] gripPoints;
        private Rigidbody rb;

        // Events
        public event System.Action OnShakeStart;
        public event System.Action OnShakeEnd;
        public event System.Action<float> OnDamageTaken;
        public event System.Action OnDeath;
        public event System.Action<Vector3> OnPositionChanged;

        // Properties
        public bool IsShaking => isShaking;
        public float ShakeIntensity => isShaking ? shakeIntensity : 0f;
        public float CurrentShakeDrainMultiplier => isShaking ? staminaDrainMultiplierDuringShake : 1f;
        public bool IsAlive => isAlive;
        public float Health => currentHealth;
        public float MaxHealth => health;
        public float HealthPercent => currentHealth / health;
        public string Name => colossusName;
        public GripPoint[] GripPoints => gripPoints;
        public bool IsMoving => isMoving;
        public Vector3 Velocity => rb != null ? rb.linearVelocity : Vector3.zero;

        private void Awake()
        {
            currentHealth = health;
            gripPoints = GetComponentsInChildren<GripPoint>();
            rb = GetComponent<Rigidbody>();
            originalPosition = transform.position;

            ScheduleNextShake();
        }

        private void Update()
        {
            if (!isAlive) return;

            UpdateShake();
            UpdateMovement();
            CheckForPlayer();
        }

        private void ScheduleNextShake()
        {
            float variance = Random.Range(-shakeIntervalVariance, shakeIntervalVariance);
            nextShakeTime = Time.time + shakeInterval + variance;
        }

        private void UpdateShake()
        {
            if (!canShake) return;

            // Check if it's time to shake
            if (!isShaking && Time.time >= nextShakeTime)
            {
                StartShake();
            }

            // Update shake
            if (isShaking)
            {
                shakeTimer -= Time.deltaTime;

                // Generate shake offset using Perlin noise for smoother shaking
                float shakeX = (Mathf.PerlinNoise(Time.time * 25f, 0f) - 0.5f) * 2f * shakeIntensity;
                float shakeY = (Mathf.PerlinNoise(0f, Time.time * 25f) - 0.5f) * 2f * shakeIntensity;
                float shakeZ = (Mathf.PerlinNoise(Time.time * 25f, Time.time * 25f) - 0.5f) * 2f * shakeIntensity;
                shakeOffset = new Vector3(shakeX, shakeY, shakeZ);

                if (shakeTimer <= 0)
                {
                    EndShake();
                }
            }
        }

        private void UpdateMovement()
        {
            if (!isMoving || waypoints == null || waypoints.Length == 0) return;

            Transform targetWaypoint = waypoints[currentWaypointIndex];
            if (targetWaypoint == null) return;

            Vector3 direction = (targetWaypoint.position - transform.position).normalized;
            Vector3 newPosition = transform.position + direction * movementSpeed * Time.deltaTime;

            // Check if reached waypoint
            if (Vector3.Distance(transform.position, targetWaypoint.position) < 0.5f)
            {
                currentWaypointIndex++;
                if (currentWaypointIndex >= waypoints.Length)
                {
                    if (loopWaypoints)
                    {
                        currentWaypointIndex = 0;
                    }
                    else
                    {
                        isMoving = false;
                        return;
                    }
                }
            }

            Vector3 positionDelta = newPosition - transform.position;
            transform.position = newPosition;
            OnPositionChanged?.Invoke(positionDelta);
        }

        private void CheckForPlayer()
        {
            // Detect nearby player and potentially increase shake frequency
            Collider[] players = Physics.OverlapSphere(transform.position, playerDetectionRadius, playerLayer);
            
            if (players.Length > 0 && canShake && !isShaking)
            {
                // Check if any player is gripping
                foreach (Collider player in players)
                {
                    GripSystem gripSystem = player.GetComponent<GripSystem>();
                    if (gripSystem != null && gripSystem.IsGripping)
                    {
                        // If a player is gripping, reduce time to next shake
                        nextShakeTime = Mathf.Min(nextShakeTime, Time.time + shakeInterval / aggroShakeMultiplier);
                    }
                }
            }
        }

        /// <summary>
        /// Starts a shake event.
        /// </summary>
        public void StartShake()
        {
            if (isShaking || !isAlive) return;

            isShaking = true;
            shakeTimer = shakeDuration;
            OnShakeStart?.Invoke();
        }

        /// <summary>
        /// Starts a shake with custom duration and intensity.
        /// </summary>
        public void StartShake(float duration, float intensity)
        {
            if (isShaking || !isAlive) return;

            isShaking = true;
            isCustomShake = true;
            shakeTimer = duration;
            originalShakeIntensity = shakeIntensity;
            shakeIntensity = intensity;
            OnShakeStart?.Invoke();
        }

        /// <summary>
        /// Ends the current shake event.
        /// </summary>
        public void EndShake()
        {
            if (!isShaking) return;

            isShaking = false;
            shakeOffset = Vector3.zero;
            
            // Restore original intensity if this was a custom shake
            if (isCustomShake)
            {
                shakeIntensity = originalShakeIntensity;
                isCustomShake = false;
            }
            
            ScheduleNextShake();
            OnShakeEnd?.Invoke();
        }

        /// <summary>
        /// Gets the current shake offset for attached objects.
        /// </summary>
        public Vector3 GetShakeOffset()
        {
            return shakeOffset;
        }

        /// <summary>
        /// Takes damage from an attack.
        /// </summary>
        public void TakeDamage(float damage, GripPoint hitPoint = null)
        {
            if (!isAlive) return;

            float actualDamage = damage;
            if (hitPoint != null)
            {
                actualDamage *= hitPoint.DamageMultiplier;
            }

            currentHealth -= actualDamage;
            OnDamageTaken?.Invoke(actualDamage);

            if (currentHealth <= 0)
            {
                currentHealth = 0;
                Die();
            }
        }

        /// <summary>
        /// Kills the colossus.
        /// </summary>
        public void Die()
        {
            if (!isAlive) return;

            isAlive = false;
            isShaking = false;
            isMoving = false;
            OnDeath?.Invoke();
        }

        /// <summary>
        /// Finds the nearest grip point to a world position.
        /// </summary>
        public GripPoint FindNearestGripPoint(Vector3 position)
        {
            GripPoint nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (GripPoint point in gripPoints)
            {
                float distance = Vector3.Distance(position, point.GetWorldPosition());
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = point;
                }
            }

            return nearest;
        }

        /// <summary>
        /// Finds the nearest grip point within a maximum range.
        /// </summary>
        public GripPoint FindNearestGripPointInRange(Vector3 position, float maxRange)
        {
            GripPoint nearest = null;
            float nearestDistance = maxRange;

            foreach (GripPoint point in gripPoints)
            {
                float distance = Vector3.Distance(position, point.GetWorldPosition());
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = point;
                }
            }

            return nearest;
        }

        /// <summary>
        /// Sets the movement state.
        /// </summary>
        public void SetMoving(bool moving)
        {
            isMoving = moving;
        }

        /// <summary>
        /// Sets waypoints for movement.
        /// </summary>
        public void SetWaypoints(Transform[] newWaypoints)
        {
            waypoints = newWaypoints;
            currentWaypointIndex = 0;
        }

        private void OnDrawGizmosSelected()
        {
            // Draw detection radius
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, playerDetectionRadius);

            // Draw waypoint path
            if (waypoints != null && waypoints.Length > 0)
            {
                Gizmos.color = Color.cyan;
                for (int i = 0; i < waypoints.Length - 1; i++)
                {
                    if (waypoints[i] != null && waypoints[i + 1] != null)
                    {
                        Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
                    }
                }
                if (loopWaypoints && waypoints.Length > 1 && waypoints[0] != null && waypoints[waypoints.Length - 1] != null)
                {
                    Gizmos.DrawLine(waypoints[waypoints.Length - 1].position, waypoints[0].position);
                }
            }
        }
    }
}
