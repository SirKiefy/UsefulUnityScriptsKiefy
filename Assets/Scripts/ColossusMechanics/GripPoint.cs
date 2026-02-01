using UnityEngine;

namespace UsefulScripts.ColossusMechanics
{
    /// <summary>
    /// Defines a specific point on a surface or creature that can be grabbed.
    /// Attach this to grip-able locations on a colossus or climbable surface.
    /// </summary>
    public class GripPoint : MonoBehaviour
    {
        [Header("Grip Settings")]
        [SerializeField] private float gripRadius = 0.5f;
        [SerializeField] private bool isWeakPoint = false;
        [SerializeField] private float weakPointDamageMultiplier = 2f;
        [SerializeField] private bool requiresSpecificApproach = false;
        [SerializeField] private Vector3 approachDirection = Vector3.up;
        [SerializeField] private float approachAngleTolerance = 45f;

        [Header("Fur/Surface Settings")]
        [SerializeField] private bool hasFur = true;
        [SerializeField] private float furGripBonus = 1.2f;

        [Header("Visual Feedback")]
        [SerializeField] private Color gizmoColor = Color.green;
        [SerializeField] private bool showInGame = false;
        [SerializeField] private GameObject highlightEffect;

        // Parent reference for moving with the colossus
        private Transform parentTransform;
        private Vector3 localPosition;
        private Quaternion localRotation;

        // Properties
        public float GripRadius => gripRadius;
        public bool IsWeakPoint => isWeakPoint;
        public float DamageMultiplier => isWeakPoint ? weakPointDamageMultiplier : 1f;
        public bool HasFur => hasFur;
        public float FurGripBonus => hasFur ? furGripBonus : 1f;

        private void Awake()
        {
            parentTransform = transform.parent;
            if (parentTransform != null)
            {
                localPosition = parentTransform.InverseTransformPoint(transform.position);
                localRotation = Quaternion.Inverse(parentTransform.rotation) * transform.rotation;
            }
        }

        private void Start()
        {
            if (highlightEffect != null)
            {
                highlightEffect.SetActive(showInGame);
            }
        }

        /// <summary>
        /// Checks if a position is within grip range of this point.
        /// </summary>
        public bool IsWithinGripRange(Vector3 position)
        {
            return Vector3.Distance(transform.position, position) <= gripRadius;
        }

        /// <summary>
        /// Checks if the approach angle is valid for gripping.
        /// </summary>
        public bool IsValidApproach(Vector3 approachFrom)
        {
            if (!requiresSpecificApproach) return true;

            Vector3 worldApproachDir = transform.TransformDirection(approachDirection);
            Vector3 playerApproach = (transform.position - approachFrom).normalized;
            float angle = Vector3.Angle(worldApproachDir, playerApproach);

            return angle <= approachAngleTolerance;
        }

        /// <summary>
        /// Gets the world position of this grip point.
        /// </summary>
        public Vector3 GetWorldPosition()
        {
            return transform.position;
        }

        /// <summary>
        /// Gets the grip position adjusted for player attachment.
        /// </summary>
        public Vector3 GetAttachPosition()
        {
            return transform.position;
        }

        /// <summary>
        /// Gets the up direction of this grip point (for player orientation).
        /// </summary>
        public Vector3 GetUpDirection()
        {
            return transform.up;
        }

        /// <summary>
        /// Gets the forward direction of this grip point.
        /// </summary>
        public Vector3 GetForwardDirection()
        {
            return transform.forward;
        }

        /// <summary>
        /// Shows the highlight effect.
        /// </summary>
        public void ShowHighlight()
        {
            if (highlightEffect != null)
            {
                highlightEffect.SetActive(true);
            }
        }

        /// <summary>
        /// Hides the highlight effect.
        /// </summary>
        public void HideHighlight()
        {
            if (highlightEffect != null && !showInGame)
            {
                highlightEffect.SetActive(false);
            }
        }

        private void OnDrawGizmos()
        {
            // Draw grip radius
            Gizmos.color = isWeakPoint ? Color.red : gizmoColor;
            Gizmos.DrawWireSphere(transform.position, gripRadius);

            // Draw approach direction if required
            if (requiresSpecificApproach)
            {
                Gizmos.color = Color.yellow;
                Vector3 worldApproachDir = transform.TransformDirection(approachDirection);
                Gizmos.DrawRay(transform.position, worldApproachDir * gripRadius * 2f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Draw filled sphere when selected
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.3f);
            Gizmos.DrawSphere(transform.position, gripRadius);

            // Draw approach cone
            if (requiresSpecificApproach)
            {
                Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
                Vector3 worldApproachDir = transform.TransformDirection(approachDirection);
                // Draw cone lines
                for (int i = 0; i < 8; i++)
                {
                    float angle = i * 45f * Mathf.Deg2Rad;
                    Vector3 offset = Quaternion.AngleAxis(approachAngleTolerance, worldApproachDir) * 
                                    Quaternion.AngleAxis(angle * Mathf.Rad2Deg, worldApproachDir) * 
                                    Vector3.up * gripRadius;
                    Gizmos.DrawLine(transform.position, transform.position + worldApproachDir * gripRadius * 2f + offset);
                }
            }
        }
    }
}
