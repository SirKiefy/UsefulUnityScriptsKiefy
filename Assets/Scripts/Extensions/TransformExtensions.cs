using UnityEngine;

namespace UsefulScripts.Extensions
{
    /// <summary>
    /// Extension methods for Transform component.
    /// </summary>
    public static class TransformExtensions
    {
        // --- Position ---

        /// <summary>
        /// Set X position
        /// </summary>
        public static void SetX(this Transform t, float x) => 
            t.position = new Vector3(x, t.position.y, t.position.z);

        /// <summary>
        /// Set Y position
        /// </summary>
        public static void SetY(this Transform t, float y) => 
            t.position = new Vector3(t.position.x, y, t.position.z);

        /// <summary>
        /// Set Z position
        /// </summary>
        public static void SetZ(this Transform t, float z) => 
            t.position = new Vector3(t.position.x, t.position.y, z);

        /// <summary>
        /// Set local X position
        /// </summary>
        public static void SetLocalX(this Transform t, float x) => 
            t.localPosition = new Vector3(x, t.localPosition.y, t.localPosition.z);

        /// <summary>
        /// Set local Y position
        /// </summary>
        public static void SetLocalY(this Transform t, float y) => 
            t.localPosition = new Vector3(t.localPosition.x, y, t.localPosition.z);

        /// <summary>
        /// Set local Z position
        /// </summary>
        public static void SetLocalZ(this Transform t, float z) => 
            t.localPosition = new Vector3(t.localPosition.x, t.localPosition.y, z);

        // --- Scale ---

        /// <summary>
        /// Set uniform scale
        /// </summary>
        public static void SetScale(this Transform t, float scale) => 
            t.localScale = new Vector3(scale, scale, scale);

        /// <summary>
        /// Set scale X
        /// </summary>
        public static void SetScaleX(this Transform t, float x) => 
            t.localScale = new Vector3(x, t.localScale.y, t.localScale.z);

        /// <summary>
        /// Set scale Y
        /// </summary>
        public static void SetScaleY(this Transform t, float y) => 
            t.localScale = new Vector3(t.localScale.x, y, t.localScale.z);

        /// <summary>
        /// Set scale Z
        /// </summary>
        public static void SetScaleZ(this Transform t, float z) => 
            t.localScale = new Vector3(t.localScale.x, t.localScale.y, z);

        // --- Rotation ---

        /// <summary>
        /// Reset rotation to identity
        /// </summary>
        public static void ResetRotation(this Transform t) => 
            t.rotation = Quaternion.identity;

        /// <summary>
        /// Reset local rotation to identity
        /// </summary>
        public static void ResetLocalRotation(this Transform t) => 
            t.localRotation = Quaternion.identity;

        /// <summary>
        /// Set rotation on Y axis only
        /// </summary>
        public static void SetRotationY(this Transform t, float y) => 
            t.eulerAngles = new Vector3(t.eulerAngles.x, y, t.eulerAngles.z);

        /// <summary>
        /// Look at target on Y axis only (horizontal look)
        /// </summary>
        public static void LookAtY(this Transform t, Vector3 target)
        {
            Vector3 direction = target - t.position;
            direction.y = 0;
            if (direction != Vector3.zero)
            {
                t.rotation = Quaternion.LookRotation(direction);
            }
        }

        /// <summary>
        /// Look at target on Y axis only (horizontal look)
        /// </summary>
        public static void LookAtY(this Transform t, Transform target) => 
            t.LookAtY(target.position);

        // --- Reset ---

        /// <summary>
        /// Reset position to zero
        /// </summary>
        public static void ResetPosition(this Transform t) => 
            t.position = Vector3.zero;

        /// <summary>
        /// Reset local position to zero
        /// </summary>
        public static void ResetLocalPosition(this Transform t) => 
            t.localPosition = Vector3.zero;

        /// <summary>
        /// Reset all local transforms
        /// </summary>
        public static void ResetLocal(this Transform t)
        {
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;
        }

        // --- Children ---

        /// <summary>
        /// Destroy all children
        /// </summary>
        public static void DestroyChildren(this Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(t.GetChild(i).gameObject);
            }
        }

        /// <summary>
        /// Destroy all children immediately (Editor only)
        /// </summary>
        public static void DestroyChildrenImmediate(this Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(t.GetChild(i).gameObject);
            }
        }

        /// <summary>
        /// Set active state of all children
        /// </summary>
        public static void SetChildrenActive(this Transform t, bool active)
        {
            foreach (Transform child in t)
            {
                child.gameObject.SetActive(active);
            }
        }

        /// <summary>
        /// Get all children as array
        /// </summary>
        public static Transform[] GetChildren(this Transform t)
        {
            Transform[] children = new Transform[t.childCount];
            for (int i = 0; i < t.childCount; i++)
            {
                children[i] = t.GetChild(i);
            }
            return children;
        }

        // --- Distance ---

        /// <summary>
        /// Get distance to another transform
        /// </summary>
        public static float DistanceTo(this Transform t, Transform other) => 
            Vector3.Distance(t.position, other.position);

        /// <summary>
        /// Get distance to a position
        /// </summary>
        public static float DistanceTo(this Transform t, Vector3 position) => 
            Vector3.Distance(t.position, position);

        /// <summary>
        /// Get flat distance (ignoring Y) to another transform
        /// </summary>
        public static float FlatDistanceTo(this Transform t, Transform other)
        {
            Vector3 a = t.position;
            Vector3 b = other.position;
            a.y = b.y = 0;
            return Vector3.Distance(a, b);
        }

        // --- Direction ---

        /// <summary>
        /// Get direction to another transform
        /// </summary>
        public static Vector3 DirectionTo(this Transform t, Transform other) => 
            (other.position - t.position).normalized;

        /// <summary>
        /// Get direction to a position
        /// </summary>
        public static Vector3 DirectionTo(this Transform t, Vector3 position) => 
            (position - t.position).normalized;

        /// <summary>
        /// Get flat direction (ignoring Y) to another transform
        /// </summary>
        public static Vector3 FlatDirectionTo(this Transform t, Transform other)
        {
            Vector3 direction = other.position - t.position;
            direction.y = 0;
            return direction.normalized;
        }
    }
}
