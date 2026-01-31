using UnityEngine;

namespace UsefulScripts.Camera
{
    /// <summary>
    /// Smooth camera follow with multiple modes and features.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        public enum FollowMode
        {
            Instant,
            SmoothDamp,
            Lerp,
            FixedLerp
        }

        [Header("Target")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0, 5, -10);

        [Header("Follow Settings")]
        [SerializeField] private FollowMode followMode = FollowMode.SmoothDamp;
        [SerializeField] private float smoothTime = 0.3f;
        [SerializeField] private float lerpSpeed = 5f;

        [Header("Look At")]
        [SerializeField] private bool lookAtTarget = true;
        [SerializeField] private float lookAtSmoothTime = 0.1f;

        [Header("Bounds")]
        [SerializeField] private bool useBounds = false;
        [SerializeField] private Vector2 minBounds;
        [SerializeField] private Vector2 maxBounds;

        [Header("Dead Zone")]
        [SerializeField] private bool useDeadZone = false;
        [SerializeField] private Vector2 deadZoneSize = new Vector2(2f, 1f);

        [Header("Look Ahead")]
        [SerializeField] private bool useLookAhead = false;
        [SerializeField] private float lookAheadDistance = 3f;
        [SerializeField] private float lookAheadSpeed = 2f;

        // Private
        private Vector3 velocity = Vector3.zero;
        private Vector3 currentLookAhead;
        private Vector3 lastTargetPosition;
        private Quaternion rotationVelocity;

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 targetPosition = CalculateTargetPosition();
            MoveToTarget(targetPosition);

            if (lookAtTarget)
            {
                LookAtTarget();
            }

            lastTargetPosition = target.position;
        }

        private Vector3 CalculateTargetPosition()
        {
            Vector3 desiredPosition = target.position + offset;

            // Look ahead
            if (useLookAhead)
            {
                Vector3 targetVelocity = (target.position - lastTargetPosition) / Time.deltaTime;
                Vector3 lookAheadTarget = targetVelocity.normalized * lookAheadDistance;
                currentLookAhead = Vector3.Lerp(currentLookAhead, lookAheadTarget, Time.deltaTime * lookAheadSpeed);
                desiredPosition += new Vector3(currentLookAhead.x, 0, currentLookAhead.z);
            }

            // Dead zone
            if (useDeadZone)
            {
                Vector3 diff = desiredPosition - transform.position;
                if (Mathf.Abs(diff.x) < deadZoneSize.x)
                {
                    desiredPosition.x = transform.position.x;
                }
                if (Mathf.Abs(diff.y) < deadZoneSize.y)
                {
                    desiredPosition.y = transform.position.y;
                }
            }

            // Bounds
            if (useBounds)
            {
                desiredPosition.x = Mathf.Clamp(desiredPosition.x, minBounds.x, maxBounds.x);
                desiredPosition.y = Mathf.Clamp(desiredPosition.y, minBounds.y, maxBounds.y);
            }

            return desiredPosition;
        }

        private void MoveToTarget(Vector3 targetPosition)
        {
            switch (followMode)
            {
                case FollowMode.Instant:
                    transform.position = targetPosition;
                    break;

                case FollowMode.SmoothDamp:
                    transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
                    break;

                case FollowMode.Lerp:
                    transform.position = Vector3.Lerp(transform.position, targetPosition, lerpSpeed * Time.deltaTime);
                    break;

                case FollowMode.FixedLerp:
                    transform.position = Vector3.Lerp(transform.position, targetPosition, lerpSpeed * Time.fixedDeltaTime);
                    break;
            }
        }

        private void LookAtTarget()
        {
            Vector3 lookDirection = target.position - transform.position;
            if (lookDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, lookAtSmoothTime);
            }
        }

        /// <summary>
        /// Set a new target to follow
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (target != null)
            {
                lastTargetPosition = target.position;
            }
        }

        /// <summary>
        /// Set the camera offset
        /// </summary>
        public void SetOffset(Vector3 newOffset)
        {
            offset = newOffset;
        }

        /// <summary>
        /// Snap camera to target immediately
        /// </summary>
        public void SnapToTarget()
        {
            if (target == null) return;
            transform.position = target.position + offset;
            velocity = Vector3.zero;
        }

        /// <summary>
        /// Set camera bounds
        /// </summary>
        public void SetBounds(Vector2 min, Vector2 max)
        {
            minBounds = min;
            maxBounds = max;
            useBounds = true;
        }

        /// <summary>
        /// Remove camera bounds
        /// </summary>
        public void ClearBounds()
        {
            useBounds = false;
        }

        private void OnDrawGizmosSelected()
        {
            if (useBounds)
            {
                Gizmos.color = Color.yellow;
                Vector3 center = new Vector3((minBounds.x + maxBounds.x) / 2, (minBounds.y + maxBounds.y) / 2, 0);
                Vector3 size = new Vector3(maxBounds.x - minBounds.x, maxBounds.y - minBounds.y, 1);
                Gizmos.DrawWireCube(center, size);
            }

            if (useDeadZone)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(transform.position, new Vector3(deadZoneSize.x * 2, deadZoneSize.y * 2, 1));
            }
        }
    }
}
