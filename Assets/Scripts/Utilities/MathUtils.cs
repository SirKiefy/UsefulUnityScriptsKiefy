using UnityEngine;

namespace UsefulScripts.Utilities
{
    /// <summary>
    /// Common math utilities and helpers.
    /// </summary>
    public static class MathUtils
    {
        /// <summary>
        /// Remap a value from one range to another
        /// </summary>
        public static float Remap(float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            return (value - fromMin) / (fromMax - fromMin) * (toMax - toMin) + toMin;
        }

        /// <summary>
        /// Smooth damp angle (like Mathf.SmoothDampAngle but returns velocity)
        /// </summary>
        public static float SmoothDampAngle(float current, float target, ref float velocity, float smoothTime)
        {
            return Mathf.SmoothDampAngle(current, target, ref velocity, smoothTime);
        }

        /// <summary>
        /// Get point on bezier curve
        /// </summary>
        public static Vector3 Bezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            float u = 1 - t;
            return u * u * p0 + 2 * u * t * p1 + t * t * p2;
        }

        /// <summary>
        /// Get point on cubic bezier curve
        /// </summary>
        public static Vector3 BezierCubic(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            return uuu * p0 + 3 * uu * t * p1 + 3 * u * tt * p2 + ttt * p3;
        }

        /// <summary>
        /// Spring interpolation
        /// </summary>
        public static float Spring(float current, float target, ref float velocity, float stiffness, float damping, float deltaTime)
        {
            float displacement = current - target;
            float springForce = -stiffness * displacement;
            float dampingForce = -damping * velocity;
            float acceleration = springForce + dampingForce;
            velocity += acceleration * deltaTime;
            return current + velocity * deltaTime;
        }

        /// <summary>
        /// Spring interpolation for Vector3
        /// </summary>
        public static Vector3 Spring(Vector3 current, Vector3 target, ref Vector3 velocity, float stiffness, float damping, float deltaTime)
        {
            Vector3 displacement = current - target;
            Vector3 springForce = -stiffness * displacement;
            Vector3 dampingForce = -damping * velocity;
            Vector3 acceleration = springForce + dampingForce;
            velocity += acceleration * deltaTime;
            return current + velocity * deltaTime;
        }

        /// <summary>
        /// Check if point is inside polygon (2D)
        /// </summary>
        public static bool PointInPolygon(Vector2 point, Vector2[] polygon)
        {
            bool inside = false;
            int j = polygon.Length - 1;

            for (int i = 0; i < polygon.Length; j = i++)
            {
                if (((polygon[i].y > point.y) != (polygon[j].y > point.y)) &&
                    (point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x))
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        /// <summary>
        /// Get random point in circle
        /// </summary>
        public static Vector2 RandomPointInCircle(float radius)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float r = Mathf.Sqrt(Random.Range(0f, 1f)) * radius;
            return new Vector2(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r);
        }

        /// <summary>
        /// Get random point on circle edge
        /// </summary>
        public static Vector2 RandomPointOnCircle(float radius)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            return new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
        }

        /// <summary>
        /// Get random point in sphere
        /// </summary>
        public static Vector3 RandomPointInSphere(float radius)
        {
            return Random.insideUnitSphere * radius;
        }

        /// <summary>
        /// Get random point on sphere surface
        /// </summary>
        public static Vector3 RandomPointOnSphere(float radius)
        {
            return Random.onUnitSphere * radius;
        }

        /// <summary>
        /// Wrap angle between -180 and 180
        /// </summary>
        public static float WrapAngle(float angle)
        {
            angle %= 360;
            if (angle > 180) angle -= 360;
            return angle;
        }

        /// <summary>
        /// Get shortest angle difference
        /// </summary>
        public static float AngleDifference(float from, float to)
        {
            float diff = (to - from + 180) % 360 - 180;
            return diff < -180 ? diff + 360 : diff;
        }

        /// <summary>
        /// Smooth step
        /// </summary>
        public static float SmoothStep(float from, float to, float t)
        {
            t = Mathf.Clamp01(t);
            t = t * t * (3f - 2f * t);
            return from + (to - from) * t;
        }

        /// <summary>
        /// Smoother step (Ken Perlin's improved smoothstep)
        /// </summary>
        public static float SmootherStep(float from, float to, float t)
        {
            t = Mathf.Clamp01(t);
            t = t * t * t * (t * (6f * t - 15f) + 10f);
            return from + (to - from) * t;
        }

        /// <summary>
        /// Approach a target value by a fixed step
        /// </summary>
        public static float Approach(float current, float target, float step)
        {
            if (current < target)
                return Mathf.Min(current + step, target);
            else
                return Mathf.Max(current - step, target);
        }

        /// <summary>
        /// Check if two line segments intersect (2D)
        /// </summary>
        public static bool LineSegmentsIntersect(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, out Vector2 intersection)
        {
            intersection = Vector2.zero;

            float d = (p2.x - p1.x) * (p4.y - p3.y) - (p2.y - p1.y) * (p4.x - p3.x);
            if (Mathf.Approximately(d, 0)) return false;

            float u = ((p3.x - p1.x) * (p4.y - p3.y) - (p3.y - p1.y) * (p4.x - p3.x)) / d;
            float v = ((p3.x - p1.x) * (p2.y - p1.y) - (p3.y - p1.y) * (p2.x - p1.x)) / d;

            if (u < 0 || u > 1 || v < 0 || v > 1) return false;

            intersection = p1 + u * (p2 - p1);
            return true;
        }

        /// <summary>
        /// Calculate spiral position
        /// </summary>
        public static Vector2 Spiral(float angle, float a, float b)
        {
            float r = a + b * angle;
            return new Vector2(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r);
        }
    }
}
