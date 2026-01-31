using UnityEngine;
using System;
using System.Collections.Generic;

namespace UsefulScripts.Extensions
{
    /// <summary>
    /// General utility extension methods for common types.
    /// </summary>
    public static class GeneralExtensions
    {
        // --- Float Extensions ---

        /// <summary>
        /// Remap value from one range to another
        /// </summary>
        public static float Remap(this float value, float fromMin, float fromMax, float toMin, float toMax) =>
            (value - fromMin) / (fromMax - fromMin) * (toMax - toMin) + toMin;

        /// <summary>
        /// Check if approximately equal
        /// </summary>
        public static bool Approximately(this float a, float b, float tolerance = 0.0001f) =>
            Mathf.Abs(a - b) < tolerance;

        /// <summary>
        /// Round to decimal places
        /// </summary>
        public static float RoundTo(this float value, int decimals) =>
            (float)Math.Round(value, decimals);

        /// <summary>
        /// Clamp between 0 and 1
        /// </summary>
        public static float Clamp01(this float value) => Mathf.Clamp01(value);

        /// <summary>
        /// Get sign (-1, 0, or 1)
        /// </summary>
        public static int Sign(this float value) =>
            value > 0 ? 1 : (value < 0 ? -1 : 0);

        // --- Int Extensions ---

        /// <summary>
        /// Check if even
        /// </summary>
        public static bool IsEven(this int value) => value % 2 == 0;

        /// <summary>
        /// Check if odd
        /// </summary>
        public static bool IsOdd(this int value) => value % 2 != 0;

        /// <summary>
        /// Loop value within range (inclusive)
        /// </summary>
        public static int Loop(this int value, int min, int max)
        {
            int range = max - min + 1;
            return ((value - min) % range + range) % range + min;
        }

        /// <summary>
        /// Check if within range
        /// </summary>
        public static bool InRange(this int value, int min, int max) =>
            value >= min && value <= max;

        // --- String Extensions ---

        /// <summary>
        /// Check if null or empty
        /// </summary>
        public static bool IsNullOrEmpty(this string str) =>
            string.IsNullOrEmpty(str);

        /// <summary>
        /// Check if null or whitespace
        /// </summary>
        public static bool IsNullOrWhitespace(this string str) =>
            string.IsNullOrWhiteSpace(str);

        /// <summary>
        /// Truncate string to max length
        /// </summary>
        public static string Truncate(this string str, int maxLength, string suffix = "...")
        {
            if (string.IsNullOrEmpty(str) || str.Length <= maxLength)
                return str;
            return str.Substring(0, maxLength - suffix.Length) + suffix;
        }

        /// <summary>
        /// Capitalize first letter
        /// </summary>
        public static string Capitalize(this string str)
        {
            if (string.IsNullOrEmpty(str)) return str;
            return char.ToUpper(str[0]) + str.Substring(1);
        }

        // --- Color Extensions ---

        /// <summary>
        /// Return color with modified alpha
        /// </summary>
        public static Color WithAlpha(this Color color, float alpha) =>
            new Color(color.r, color.g, color.b, alpha);

        /// <summary>
        /// Return inverted color
        /// </summary>
        public static Color Invert(this Color color) =>
            new Color(1f - color.r, 1f - color.g, 1f - color.b, color.a);

        /// <summary>
        /// Convert to hex string
        /// </summary>
        public static string ToHex(this Color color) =>
            ColorUtility.ToHtmlStringRGBA(color);

        // --- List Extensions ---

        /// <summary>
        /// Get random element
        /// </summary>
        public static T GetRandom<T>(this IList<T> list)
        {
            if (list == null || list.Count == 0) return default;
            return list[UnityEngine.Random.Range(0, list.Count)];
        }

        /// <summary>
        /// Shuffle list in place
        /// </summary>
        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = UnityEngine.Random.Range(0, n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        /// <summary>
        /// Check if index is valid
        /// </summary>
        public static bool IsValidIndex<T>(this IList<T> list, int index) =>
            index >= 0 && index < list.Count;

        /// <summary>
        /// Get element or default if index invalid
        /// </summary>
        public static T GetOrDefault<T>(this IList<T> list, int index, T defaultValue = default)
        {
            if (list.IsValidIndex(index))
                return list[index];
            return defaultValue;
        }

        /// <summary>
        /// Remove and return last element
        /// </summary>
        public static T Pop<T>(this IList<T> list)
        {
            if (list.Count == 0) return default;
            T item = list[list.Count - 1];
            list.RemoveAt(list.Count - 1);
            return item;
        }

        // --- Array Extensions ---

        /// <summary>
        /// Get random element from array
        /// </summary>
        public static T GetRandom<T>(this T[] array)
        {
            if (array == null || array.Length == 0) return default;
            return array[UnityEngine.Random.Range(0, array.Length)];
        }

        /// <summary>
        /// Check if index is valid
        /// </summary>
        public static bool IsValidIndex<T>(this T[] array, int index) =>
            index >= 0 && index < array.Length;

        // --- Quaternion Extensions ---

        /// <summary>
        /// Get forward direction
        /// </summary>
        public static Vector3 Forward(this Quaternion rotation) =>
            rotation * Vector3.forward;

        /// <summary>
        /// Get right direction
        /// </summary>
        public static Vector3 Right(this Quaternion rotation) =>
            rotation * Vector3.right;

        /// <summary>
        /// Get up direction
        /// </summary>
        public static Vector3 Up(this Quaternion rotation) =>
            rotation * Vector3.up;
    }
}
