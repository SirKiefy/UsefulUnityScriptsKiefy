using UnityEngine;
using System.Collections.Generic;

namespace UsefulScripts.SpaceShip
{
    /// <summary>
    /// Targeting system for spaceships.
    /// Handles target acquisition, tracking, lead indicators, and target switching.
    /// Inspired by Elite Dangerous, Star Citizen, and 4X strategy games.
    /// </summary>
    public class ShipTargetingSystem : MonoBehaviour
    {
        [Header("Targeting Configuration")]
        [SerializeField] private float maxTargetingRange = 10000f;
        [SerializeField] private float scanAngle = 90f;
        [SerializeField] private LayerMask targetableMask;
        [SerializeField] private float targetUpdateInterval = 0.5f;

        [Header("Lock-On Settings")]
        [SerializeField] private bool enableLockOn = true;
        [SerializeField] private float lockOnTime = 2f;
        [SerializeField] private float lockOnAngle = 15f;
        [SerializeField] private float lockBreakAngle = 45f;
        [SerializeField] private float lockBreakDistance = 15000f;

        [Header("Lead Indicator")]
        [SerializeField] private bool showLeadIndicator = true;
        [SerializeField] private float projectileSpeed = 1500f;
        [SerializeField] private float maxLeadTime = 3f;

        [Header("Subsystem Targeting")]
        [SerializeField] private bool enableSubsystemTargeting = true;
        [SerializeField] private KeyCode cycleSubsystemKey = KeyCode.Y;

        [Header("Hostile Detection")]
        [SerializeField] private string hostileTag = "Enemy";
        [SerializeField] private bool autoTargetHostiles = false;
        [SerializeField] private float threatAssessmentInterval = 1f;

        [Header("IFF (Identification Friend/Foe)")]
        [SerializeField] private string friendlyTag = "Friendly";
        [SerializeField] private string neutralTag = "Neutral";
        [SerializeField] private bool targetFriendlies = false;
        [SerializeField] private bool targetNeutrals = true;

        // State
        private Transform currentTarget;
        private ShipSubsystem targetedSubsystem;
        private List<Transform> availableTargets = new List<Transform>();
        private List<Transform> hostileTargets = new List<Transform>();
        private int currentTargetIndex = -1;
        private int currentSubsystemIndex = -1;
        private float lockProgress;
        private bool isLocked;
        private float lastScanTime;
        private float lastThreatAssessmentTime;
        private Vector3 leadPosition;
        private float targetDistance;
        private Vector3 targetRelativeVelocity;

        // Cached references
        private Rigidbody shipRigidbody;
        private Transform primaryHostile;

        // Events
        public event System.Action<Transform> OnTargetAcquired;
        public event System.Action OnTargetLost;
        public event System.Action<Transform> OnTargetChanged;
        public event System.Action<float> OnLockProgress;
        public event System.Action OnLockAcquired;
        public event System.Action OnLockLost;
        public event System.Action<ShipSubsystem> OnSubsystemTargeted;
        public event System.Action<List<Transform>> OnTargetListUpdated;
        public event System.Action<Transform> OnHostileDetected;
        public event System.Action<TargetInfo> OnTargetInfoUpdated;

        // Properties
        public Transform CurrentTarget => currentTarget;
        public ShipSubsystem TargetedSubsystem => targetedSubsystem;
        public List<Transform> AvailableTargets => availableTargets;
        public List<Transform> HostileTargets => hostileTargets;
        public bool HasTarget => currentTarget != null;
        public bool IsLocked => isLocked;
        public float LockProgress => lockProgress;
        public Vector3 LeadPosition => leadPosition;
        public float TargetDistance => targetDistance;
        public Vector3 TargetRelativeVelocity => targetRelativeVelocity;
        public IFFStatus CurrentTargetIFF => GetIFFStatus(currentTarget);

        private void Awake()
        {
            shipRigidbody = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            UpdateTargetScan();
            UpdateLockOn();
            UpdateLeadIndicator();
            UpdateTargetInfo();
            UpdateThreatAssessment();
            HandleInput();
        }

        #region Target Scanning

        private void UpdateTargetScan()
        {
            if (Time.time - lastScanTime < targetUpdateInterval) return;
            lastScanTime = Time.time;

            ScanForTargets();
        }

        /// <summary>
        /// Scan for available targets in range
        /// </summary>
        public void ScanForTargets()
        {
            availableTargets.Clear();

            Collider[] hits = Physics.OverlapSphere(transform.position, maxTargetingRange, targetableMask);

            foreach (var hit in hits)
            {
                if (hit.transform == transform) continue;

                // Check angle
                Vector3 dirToTarget = (hit.transform.position - transform.position).normalized;
                float angle = Vector3.Angle(transform.forward, dirToTarget);

                if (angle <= scanAngle)
                {
                    // Check IFF
                    IFFStatus iff = GetIFFStatus(hit.transform);
                    if (ShouldTarget(iff))
                    {
                        availableTargets.Add(hit.transform);
                    }
                }
            }

            // Sort by distance
            availableTargets.Sort((a, b) =>
            {
                float distA = Vector3.Distance(transform.position, a.position);
                float distB = Vector3.Distance(transform.position, b.position);
                return distA.CompareTo(distB);
            });

            OnTargetListUpdated?.Invoke(availableTargets);

            // Validate current target
            if (currentTarget != null && !availableTargets.Contains(currentTarget))
            {
                // Target out of range or angle
                if (Vector3.Distance(transform.position, currentTarget.position) > lockBreakDistance)
                {
                    ClearTarget();
                }
            }
        }

        private bool ShouldTarget(IFFStatus iff)
        {
            switch (iff)
            {
                case IFFStatus.Hostile:
                    return true;
                case IFFStatus.Friendly:
                    return targetFriendlies;
                case IFFStatus.Neutral:
                    return targetNeutrals;
                default:
                    return true;
            }
        }

        #endregion

        #region Target Selection

        /// <summary>
        /// Set current target
        /// </summary>
        public void SetTarget(Transform target)
        {
            if (target == currentTarget) return;

            Transform oldTarget = currentTarget;
            currentTarget = target;
            currentTargetIndex = availableTargets.IndexOf(target);
            currentSubsystemIndex = -1;
            targetedSubsystem = null;

            // Reset lock
            lockProgress = 0f;
            isLocked = false;

            if (currentTarget != null)
            {
                OnTargetAcquired?.Invoke(currentTarget);
            }
            else
            {
                OnTargetLost?.Invoke();
            }

            OnTargetChanged?.Invoke(currentTarget);
        }

        /// <summary>
        /// Clear current target
        /// </summary>
        public void ClearTarget()
        {
            SetTarget(null);
        }

        /// <summary>
        /// Select next target in list
        /// </summary>
        public void NextTarget()
        {
            if (availableTargets.Count == 0) return;

            currentTargetIndex = (currentTargetIndex + 1) % availableTargets.Count;
            SetTarget(availableTargets[currentTargetIndex]);
        }

        /// <summary>
        /// Select previous target in list
        /// </summary>
        public void PreviousTarget()
        {
            if (availableTargets.Count == 0) return;

            currentTargetIndex--;
            if (currentTargetIndex < 0) currentTargetIndex = availableTargets.Count - 1;
            SetTarget(availableTargets[currentTargetIndex]);
        }

        /// <summary>
        /// Target nearest hostile
        /// </summary>
        public void TargetNearestHostile()
        {
            Transform nearest = null;
            float nearestDist = float.MaxValue;

            foreach (var target in availableTargets)
            {
                if (GetIFFStatus(target) == IFFStatus.Hostile)
                {
                    float dist = Vector3.Distance(transform.position, target.position);
                    if (dist < nearestDist)
                    {
                        nearestDist = dist;
                        nearest = target;
                    }
                }
            }

            if (nearest != null)
            {
                SetTarget(nearest);
            }
        }

        /// <summary>
        /// Target object in crosshairs
        /// </summary>
        public void TargetInCrosshairs()
        {
            Ray ray = new Ray(transform.position, transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, maxTargetingRange, targetableMask))
            {
                if (hit.transform != transform)
                {
                    SetTarget(hit.transform);
                }
            }
        }

        /// <summary>
        /// Target highest threat
        /// </summary>
        public void TargetHighestThreat()
        {
            if (hostileTargets.Count > 0)
            {
                SetTarget(hostileTargets[0]);
            }
        }

        #endregion

        #region Lock-On

        private void UpdateLockOn()
        {
            if (!enableLockOn || currentTarget == null)
            {
                if (isLocked)
                {
                    isLocked = false;
                    OnLockLost?.Invoke();
                }
                lockProgress = 0f;
                return;
            }

            // Check if target is in lock cone
            Vector3 dirToTarget = (currentTarget.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, dirToTarget);

            if (angle <= lockOnAngle)
            {
                // Progress lock
                if (!isLocked)
                {
                    lockProgress += Time.deltaTime / lockOnTime;
                    OnLockProgress?.Invoke(lockProgress);

                    if (lockProgress >= 1f)
                    {
                        isLocked = true;
                        lockProgress = 1f;
                        OnLockAcquired?.Invoke();
                    }
                }
            }
            else if (angle > lockBreakAngle)
            {
                // Break lock
                if (isLocked)
                {
                    isLocked = false;
                    OnLockLost?.Invoke();
                }
                lockProgress = Mathf.Max(0f, lockProgress - Time.deltaTime / (lockOnTime * 0.5f));
                OnLockProgress?.Invoke(lockProgress);
            }
        }

        /// <summary>
        /// Force lock on current target
        /// </summary>
        public void ForceLock()
        {
            if (currentTarget != null)
            {
                lockProgress = 1f;
                isLocked = true;
                OnLockAcquired?.Invoke();
            }
        }

        /// <summary>
        /// Break current lock
        /// </summary>
        public void BreakLock()
        {
            if (isLocked)
            {
                isLocked = false;
                lockProgress = 0f;
                OnLockLost?.Invoke();
            }
        }

        #endregion

        #region Lead Indicator

        private void UpdateLeadIndicator()
        {
            if (!showLeadIndicator || currentTarget == null)
            {
                leadPosition = Vector3.zero;
                return;
            }

            // Calculate lead position
            leadPosition = CalculateLeadPosition(currentTarget, projectileSpeed);
        }

        /// <summary>
        /// Calculate lead position for a target
        /// </summary>
        public Vector3 CalculateLeadPosition(Transform target, float projectileSpeed)
        {
            if (target == null) return Vector3.zero;

            Vector3 targetPos = target.position;
            Vector3 targetVel = Vector3.zero;

            Rigidbody targetRb = target.GetComponent<Rigidbody>();
            if (targetRb != null)
            {
                targetVel = targetRb.linearVelocity;

                // Subtract our velocity for relative velocity
                if (shipRigidbody != null)
                {
                    targetVel -= shipRigidbody.linearVelocity;
                }
            }

            float distance = Vector3.Distance(transform.position, targetPos);
            float timeToTarget = Mathf.Min(distance / projectileSpeed, maxLeadTime);

            return targetPos + targetVel * timeToTarget;
        }

        /// <summary>
        /// Check if lead indicator is on target
        /// </summary>
        public bool IsLeadOnTarget(float tolerance = 5f)
        {
            if (currentTarget == null) return false;

            Vector3 dirToLead = (leadPosition - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, dirToLead);

            return angle <= tolerance;
        }

        #endregion

        #region Subsystem Targeting

        /// <summary>
        /// Cycle through target's subsystems
        /// </summary>
        public void CycleSubsystem()
        {
            if (!enableSubsystemTargeting || currentTarget == null) return;

            ShipSubsystems subsystems = currentTarget.GetComponent<ShipSubsystems>();
            if (subsystems == null) return;

            var systemList = subsystems.GetAllSubsystems();
            if (systemList.Count == 0) return;

            currentSubsystemIndex = (currentSubsystemIndex + 1) % systemList.Count;
            targetedSubsystem = systemList[currentSubsystemIndex];
            OnSubsystemTargeted?.Invoke(targetedSubsystem);
        }

        /// <summary>
        /// Target specific subsystem type
        /// </summary>
        public void TargetSubsystem(SubsystemType type)
        {
            if (!enableSubsystemTargeting || currentTarget == null) return;

            ShipSubsystems subsystems = currentTarget.GetComponent<ShipSubsystems>();
            if (subsystems == null) return;

            var system = subsystems.GetSubsystem(type);
            if (system != null)
            {
                targetedSubsystem = system;
                OnSubsystemTargeted?.Invoke(targetedSubsystem);
            }
        }

        /// <summary>
        /// Clear subsystem target
        /// </summary>
        public void ClearSubsystemTarget()
        {
            targetedSubsystem = null;
            currentSubsystemIndex = -1;
            OnSubsystemTargeted?.Invoke(null);
        }

        #endregion

        #region Target Info

        private void UpdateTargetInfo()
        {
            if (currentTarget == null)
            {
                targetDistance = 0f;
                targetRelativeVelocity = Vector3.zero;
                return;
            }

            targetDistance = Vector3.Distance(transform.position, currentTarget.position);

            Rigidbody targetRb = currentTarget.GetComponent<Rigidbody>();
            if (targetRb != null && shipRigidbody != null)
            {
                targetRelativeVelocity = targetRb.linearVelocity - shipRigidbody.linearVelocity;
            }
            else if (targetRb != null)
            {
                targetRelativeVelocity = targetRb.linearVelocity;
            }
            else
            {
                targetRelativeVelocity = Vector3.zero;
            }

            // Broadcast target info
            TargetInfo info = new TargetInfo
            {
                target = currentTarget,
                distance = targetDistance,
                relativeVelocity = targetRelativeVelocity,
                closingSpeed = -Vector3.Dot(targetRelativeVelocity, (currentTarget.position - transform.position).normalized),
                leadPosition = leadPosition,
                isLocked = isLocked,
                lockProgress = lockProgress,
                iffStatus = GetIFFStatus(currentTarget),
                targetedSubsystem = targetedSubsystem
            };

            OnTargetInfoUpdated?.Invoke(info);
        }

        /// <summary>
        /// Get full target information
        /// </summary>
        public TargetInfo GetTargetInfo()
        {
            if (currentTarget == null) return new TargetInfo();

            return new TargetInfo
            {
                target = currentTarget,
                distance = targetDistance,
                relativeVelocity = targetRelativeVelocity,
                closingSpeed = -Vector3.Dot(targetRelativeVelocity, (currentTarget.position - transform.position).normalized),
                leadPosition = leadPosition,
                isLocked = isLocked,
                lockProgress = lockProgress,
                iffStatus = GetIFFStatus(currentTarget),
                targetedSubsystem = targetedSubsystem
            };
        }

        #endregion

        #region Threat Assessment

        private void UpdateThreatAssessment()
        {
            if (Time.time - lastThreatAssessmentTime < threatAssessmentInterval) return;
            lastThreatAssessmentTime = Time.time;

            AssessThreats();
        }

        private void AssessThreats()
        {
            hostileTargets.Clear();

            foreach (var target in availableTargets)
            {
                if (GetIFFStatus(target) == IFFStatus.Hostile)
                {
                    hostileTargets.Add(target);
                }
            }

            // Sort by threat level (distance and angle combined)
            hostileTargets.Sort((a, b) =>
            {
                float threatA = CalculateThreatLevel(a);
                float threatB = CalculateThreatLevel(b);
                return threatB.CompareTo(threatA); // Higher threat first
            });

            // Check for new highest threat
            if (hostileTargets.Count > 0 && hostileTargets[0] != primaryHostile)
            {
                primaryHostile = hostileTargets[0];
                OnHostileDetected?.Invoke(primaryHostile);
            }

            // Auto-target if enabled and no current target
            if (autoTargetHostiles && currentTarget == null && hostileTargets.Count > 0)
            {
                SetTarget(hostileTargets[0]);
            }
        }

        private float CalculateThreatLevel(Transform target)
        {
            float distance = Vector3.Distance(transform.position, target.position);
            float angle = Vector3.Angle(transform.forward, (target.position - transform.position).normalized);

            // Higher threat for closer targets pointing at us
            float distanceFactor = 1f - (distance / maxTargetingRange);
            float angleFactor = 1f - (angle / 180f);

            // Check if target is facing us
            float targetAngleToUs = Vector3.Angle(target.forward, (transform.position - target.position).normalized);
            float facingFactor = 1f - (targetAngleToUs / 90f);
            facingFactor = Mathf.Clamp01(facingFactor);

            return (distanceFactor * 0.3f) + (angleFactor * 0.3f) + (facingFactor * 0.4f);
        }

        #endregion

        #region IFF

        /// <summary>
        /// Get IFF status for a target
        /// </summary>
        public IFFStatus GetIFFStatus(Transform target)
        {
            if (target == null) return IFFStatus.Unknown;

            if (target.CompareTag(hostileTag))
            {
                return IFFStatus.Hostile;
            }
            else if (target.CompareTag(friendlyTag))
            {
                return IFFStatus.Friendly;
            }
            else if (target.CompareTag(neutralTag))
            {
                return IFFStatus.Neutral;
            }

            return IFFStatus.Unknown;
        }

        /// <summary>
        /// Set IFF tags
        /// </summary>
        public void SetIFFTags(string hostile, string friendly, string neutral)
        {
            hostileTag = hostile;
            friendlyTag = friendly;
            neutralTag = neutral;
        }

        #endregion

        #region Input

        private void HandleInput()
        {
            if (Input.GetKeyDown(cycleSubsystemKey))
            {
                CycleSubsystem();
            }
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Set targeting range
        /// </summary>
        public void SetTargetingRange(float range)
        {
            maxTargetingRange = Mathf.Max(100f, range);
        }

        /// <summary>
        /// Set lock-on parameters
        /// </summary>
        public void SetLockOnParameters(float time, float angle, float breakAngle)
        {
            lockOnTime = Mathf.Max(0.1f, time);
            lockOnAngle = Mathf.Clamp(angle, 1f, 90f);
            lockBreakAngle = Mathf.Clamp(breakAngle, lockOnAngle, 180f);
        }

        /// <summary>
        /// Set projectile speed for lead calculation
        /// </summary>
        public void SetProjectileSpeed(float speed)
        {
            projectileSpeed = Mathf.Max(1f, speed);
        }

        /// <summary>
        /// Enable or disable auto-targeting of hostiles
        /// </summary>
        public void SetAutoTargetHostiles(bool enabled)
        {
            autoTargetHostiles = enabled;
        }

        #endregion

        private void OnDrawGizmosSelected()
        {
            // Draw targeting range
            Gizmos.color = new Color(0f, 1f, 0f, 0.1f);
            Gizmos.DrawWireSphere(transform.position, maxTargetingRange);

            // Draw scan cone
            Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
            Vector3 leftDir = Quaternion.Euler(0, -scanAngle, 0) * transform.forward;
            Vector3 rightDir = Quaternion.Euler(0, scanAngle, 0) * transform.forward;
            Gizmos.DrawRay(transform.position, leftDir * maxTargetingRange * 0.5f);
            Gizmos.DrawRay(transform.position, rightDir * maxTargetingRange * 0.5f);

            // Draw current target
            if (currentTarget != null)
            {
                Gizmos.color = isLocked ? Color.red : Color.yellow;
                Gizmos.DrawLine(transform.position, currentTarget.position);
                Gizmos.DrawWireSphere(currentTarget.position, 10f);

                // Draw lead indicator
                if (showLeadIndicator && leadPosition != Vector3.zero)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawWireSphere(leadPosition, 5f);
                    Gizmos.DrawLine(currentTarget.position, leadPosition);
                }
            }

            // Draw lock cone
            if (enableLockOn)
            {
                Gizmos.color = new Color(1f, 0f, 0f, 0.1f);
                Vector3 lockLeft = Quaternion.Euler(0, -lockOnAngle, 0) * transform.forward;
                Vector3 lockRight = Quaternion.Euler(0, lockOnAngle, 0) * transform.forward;
                Gizmos.DrawRay(transform.position, lockLeft * 200f);
                Gizmos.DrawRay(transform.position, lockRight * 200f);
            }
        }
    }

    /// <summary>
    /// IFF status for targets
    /// </summary>
    public enum IFFStatus
    {
        Unknown,
        Hostile,
        Neutral,
        Friendly
    }

    /// <summary>
    /// Contains comprehensive target information
    /// </summary>
    [System.Serializable]
    public struct TargetInfo
    {
        public Transform target;
        public float distance;
        public Vector3 relativeVelocity;
        public float closingSpeed;
        public Vector3 leadPosition;
        public bool isLocked;
        public float lockProgress;
        public IFFStatus iffStatus;
        public ShipSubsystem targetedSubsystem;
    }
}
