using UnityEngine;

namespace UsefulScripts.Extensions
{
    /// <summary>
    /// Extension methods for Vector2, Vector3, and related types.
    /// </summary>
    public static class VectorExtensions
    {
        // --- Vector3 Extensions ---

        /// <summary>
        /// Returns a copy with modified x value
        /// </summary>
        public static Vector3 WithX(this Vector3 v, float x) => new Vector3(x, v.y, v.z);

        /// <summary>
        /// Returns a copy with modified y value
        /// </summary>
        public static Vector3 WithY(this Vector3 v, float y) => new Vector3(v.x, y, v.z);

        /// <summary>
        /// Returns a copy with modified z value
        /// </summary>
        public static Vector3 WithZ(this Vector3 v, float z) => new Vector3(v.x, v.y, z);

        /// <summary>
        /// Returns the vector with absolute values
        /// </summary>
        public static Vector3 Abs(this Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

        /// <summary>
        /// Flattens the vector on the Y axis (useful for horizontal distance)
        /// </summary>
        public static Vector3 Flat(this Vector3 v) => new Vector3(v.x, 0, v.z);

        /// <summary>
        /// Returns the direction to another point
        /// </summary>
        public static Vector3 DirectionTo(this Vector3 from, Vector3 to) => (to - from).normalized;

        /// <summary>
        /// Returns a random offset within a range
        /// </summary>
        public static Vector3 RandomOffset(this Vector3 v, float range) => 
            v + new Vector3(Random.Range(-range, range), Random.Range(-range, range), Random.Range(-range, range));

        /// <summary>
        /// Clamp all components between min and max
        /// </summary>
        public static Vector3 Clamp(this Vector3 v, float min, float max) =>
            new Vector3(Mathf.Clamp(v.x, min, max), Mathf.Clamp(v.y, min, max), Mathf.Clamp(v.z, min, max));

        /// <summary>
        /// Convert to Vector2 (dropping z)
        /// </summary>
        public static Vector2 ToVector2(this Vector3 v) => new Vector2(v.x, v.y);

        /// <summary>
        /// Convert to Vector2 XZ (dropping y)
        /// </summary>
        public static Vector2 ToVector2XZ(this Vector3 v) => new Vector2(v.x, v.z);

        // --- Vector2 Extensions ---

        /// <summary>
        /// Returns a copy with modified x value
        /// </summary>
        public static Vector2 WithX(this Vector2 v, float x) => new Vector2(x, v.y);

        /// <summary>
        /// Returns a copy with modified y value
        /// </summary>
        public static Vector2 WithY(this Vector2 v, float y) => new Vector2(v.x, y);

        /// <summary>
        /// Returns the vector with absolute values
        /// </summary>
        public static Vector2 Abs(this Vector2 v) => new Vector2(Mathf.Abs(v.x), Mathf.Abs(v.y));

        /// <summary>
        /// Returns the direction to another point
        /// </summary>
        public static Vector2 DirectionTo(this Vector2 from, Vector2 to) => (to - from).normalized;

        /// <summary>
        /// Rotate the vector by degrees
        /// </summary>
        public static Vector2 Rotate(this Vector2 v, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }

        /// <summary>
        /// Returns the angle in degrees
        /// </summary>
        public static float ToAngle(this Vector2 v) => Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;

        /// <summary>
        /// Convert to Vector3 (z = 0)
        /// </summary>
        public static Vector3 ToVector3(this Vector2 v) => new Vector3(v.x, v.y, 0);

        /// <summary>
        /// Convert to Vector3 XZ plane (y = 0)
        /// </summary>
        public static Vector3 ToVector3XZ(this Vector2 v) => new Vector3(v.x, 0, v.y);

        /// <summary>
        /// Clamp all components between min and max
        /// </summary>
        public static Vector2 Clamp(this Vector2 v, float min, float max) =>
            new Vector2(Mathf.Clamp(v.x, min, max), Mathf.Clamp(v.y, min, max));

        // --- Vector2Int Extensions ---

        /// <summary>
        /// Convert to Vector2
        /// </summary>
        public static Vector2 ToVector2(this Vector2Int v) => new Vector2(v.x, v.y);

        /// <summary>
        /// Get all adjacent positions (4-directional)
        /// </summary>
        public static Vector2Int[] GetAdjacent(this Vector2Int v) => new Vector2Int[]
        {
            v + Vector2Int.up,
            v + Vector2Int.down,
            v + Vector2Int.left,
            v + Vector2Int.right
        };

        /// <summary>
        /// Get all surrounding positions (8-directional)
        /// </summary>
        public static Vector2Int[] GetSurrounding(this Vector2Int v) => new Vector2Int[]
        {
            v + Vector2Int.up,
            v + Vector2Int.down,
            v + Vector2Int.left,
            v + Vector2Int.right,
            v + new Vector2Int(1, 1),
            v + new Vector2Int(-1, 1),
            v + new Vector2Int(1, -1),
            v + new Vector2Int(-1, -1)
        };
    }
}
