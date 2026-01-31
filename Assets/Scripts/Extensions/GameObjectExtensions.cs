using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace UsefulScripts.Extensions
{
    /// <summary>
    /// Extension methods for GameObject and Component.
    /// </summary>
    public static class GameObjectExtensions
    {
        // --- Component Extensions ---

        /// <summary>
        /// Get or add a component
        /// </summary>
        public static T GetOrAddComponent<T>(this GameObject go) where T : Component
        {
            T component = go.GetComponent<T>();
            if (component == null)
            {
                component = go.AddComponent<T>();
            }
            return component;
        }

        /// <summary>
        /// Get or add a component
        /// </summary>
        public static T GetOrAddComponent<T>(this Component c) where T : Component => 
            c.gameObject.GetOrAddComponent<T>();

        /// <summary>
        /// Check if has component
        /// </summary>
        public static bool HasComponent<T>(this GameObject go) where T : Component => 
            go.GetComponent<T>() != null;

        /// <summary>
        /// Check if has component
        /// </summary>
        public static bool HasComponent<T>(this Component c) where T : Component => 
            c.GetComponent<T>() != null;

        /// <summary>
        /// Try get component with out parameter
        /// </summary>
        public static bool TryGetComponentInChildren<T>(this GameObject go, out T component) where T : Component
        {
            component = go.GetComponentInChildren<T>();
            return component != null;
        }

        /// <summary>
        /// Get all components in children excluding self
        /// </summary>
        public static T[] GetComponentsInChildrenExcludingSelf<T>(this GameObject go) where T : Component
        {
            List<T> components = new List<T>();
            foreach (Transform child in go.transform)
            {
                components.AddRange(child.GetComponentsInChildren<T>());
            }
            return components.ToArray();
        }

        // --- Layer Extensions ---

        /// <summary>
        /// Set layer recursively
        /// </summary>
        public static void SetLayerRecursively(this GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
            {
                child.gameObject.SetLayerRecursively(layer);
            }
        }

        /// <summary>
        /// Set layer recursively by name
        /// </summary>
        public static void SetLayerRecursively(this GameObject go, string layerName) => 
            go.SetLayerRecursively(LayerMask.NameToLayer(layerName));

        /// <summary>
        /// Check if in layer mask
        /// </summary>
        public static bool IsInLayerMask(this GameObject go, LayerMask mask) => 
            ((1 << go.layer) & mask) != 0;

        // --- Active State ---

        /// <summary>
        /// Toggle active state
        /// </summary>
        public static void ToggleActive(this GameObject go) => 
            go.SetActive(!go.activeSelf);

        /// <summary>
        /// Deactivate and return self (for chaining)
        /// </summary>
        public static GameObject Deactivate(this GameObject go)
        {
            go.SetActive(false);
            return go;
        }

        /// <summary>
        /// Activate and return self (for chaining)
        /// </summary>
        public static GameObject Activate(this GameObject go)
        {
            go.SetActive(true);
            return go;
        }

        // --- Find Extensions ---

        /// <summary>
        /// Find child by name (recursive)
        /// </summary>
        public static Transform FindDeepChild(this Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name)
                    return child;

                var result = child.FindDeepChild(name);
                if (result != null)
                    return result;
            }
            return null;
        }

        /// <summary>
        /// Find child by tag (recursive)
        /// </summary>
        public static Transform FindChildWithTag(this Transform parent, string tag)
        {
            foreach (Transform child in parent)
            {
                if (child.CompareTag(tag))
                    return child;

                var result = child.FindChildWithTag(tag);
                if (result != null)
                    return result;
            }
            return null;
        }

        /// <summary>
        /// Find all children with tag
        /// </summary>
        public static List<Transform> FindChildrenWithTag(this Transform parent, string tag)
        {
            List<Transform> found = new List<Transform>();
            FindChildrenWithTagRecursive(parent, tag, found);
            return found;
        }

        private static void FindChildrenWithTagRecursive(Transform parent, string tag, List<Transform> found)
        {
            foreach (Transform child in parent)
            {
                if (child.CompareTag(tag))
                    found.Add(child);
                FindChildrenWithTagRecursive(child, tag, found);
            }
        }

        // --- Bounds ---

        /// <summary>
        /// Get bounds encapsulating all renderers
        /// </summary>
        public static Bounds GetBounds(this GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return new Bounds(go.transform.position, Vector3.zero);
            }

            Bounds bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1))
            {
                bounds.Encapsulate(renderer.bounds);
            }
            return bounds;
        }

        /// <summary>
        /// Get center of all renderers
        /// </summary>
        public static Vector3 GetCenter(this GameObject go) => go.GetBounds().center;

        // --- Destruction ---

        /// <summary>
        /// Destroy after delay
        /// </summary>
        public static void DestroyDelayed(this GameObject go, float delay) => 
            Object.Destroy(go, delay);

        /// <summary>
        /// Destroy safely (null check)
        /// </summary>
        public static void DestroySafe(this GameObject go)
        {
            if (go != null)
            {
                Object.Destroy(go);
            }
        }
    }
}
