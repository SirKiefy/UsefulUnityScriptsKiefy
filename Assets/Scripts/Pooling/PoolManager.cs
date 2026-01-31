using UnityEngine;
using System.Collections.Generic;

namespace UsefulScripts.Pooling
{
    /// <summary>
    /// Centralized pool manager for managing multiple object pools.
    /// </summary>
    public class PoolManager : Core.Singleton<PoolManager>
    {
        [System.Serializable]
        public class PoolSettings
        {
            public string poolName;
            public GameObject prefab;
            public int initialSize = 10;
            public int maxSize = 0;
            public bool expandable = true;
        }

        [SerializeField] private List<PoolSettings> pools = new List<PoolSettings>();

        private Dictionary<string, ObjectPool<Transform>> poolDictionary = new Dictionary<string, ObjectPool<Transform>>();

        protected override void OnSingletonAwake()
        {
            InitializePools();
        }

        private void InitializePools()
        {
            foreach (var settings in pools)
            {
                if (settings.prefab == null)
                {
                    Debug.LogWarning($"Pool '{settings.poolName}' has no prefab assigned!");
                    continue;
                }

                CreatePool(settings);
            }
        }

        /// <summary>
        /// Create a pool at runtime
        /// </summary>
        public void CreatePool(PoolSettings settings)
        {
            if (poolDictionary.ContainsKey(settings.poolName))
            {
                Debug.LogWarning($"Pool '{settings.poolName}' already exists!");
                return;
            }

            Transform poolParent = new GameObject($"Pool_{settings.poolName}").transform;
            poolParent.SetParent(transform);

            var pool = new ObjectPool<Transform>(
                settings.prefab.transform,
                settings.initialSize,
                settings.maxSize,
                settings.expandable,
                poolParent
            );

            poolDictionary[settings.poolName] = pool;
        }

        /// <summary>
        /// Create a pool from a prefab
        /// </summary>
        public void CreatePool(string poolName, GameObject prefab, int initialSize = 10, int maxSize = 0, bool expandable = true)
        {
            CreatePool(new PoolSettings
            {
                poolName = poolName,
                prefab = prefab,
                initialSize = initialSize,
                maxSize = maxSize,
                expandable = expandable
            });
        }

        /// <summary>
        /// Get an object from a named pool
        /// </summary>
        public GameObject Get(string poolName)
        {
            if (!poolDictionary.TryGetValue(poolName, out var pool))
            {
                Debug.LogError($"Pool '{poolName}' not found!");
                return null;
            }

            Transform obj = pool.Get();
            return obj != null ? obj.gameObject : null;
        }

        /// <summary>
        /// Get an object from a named pool at a position
        /// </summary>
        public GameObject Get(string poolName, Vector3 position)
        {
            if (!poolDictionary.TryGetValue(poolName, out var pool))
            {
                Debug.LogError($"Pool '{poolName}' not found!");
                return null;
            }

            Transform obj = pool.Get(position);
            return obj != null ? obj.gameObject : null;
        }

        /// <summary>
        /// Get an object from a named pool at a position and rotation
        /// </summary>
        public GameObject Get(string poolName, Vector3 position, Quaternion rotation)
        {
            if (!poolDictionary.TryGetValue(poolName, out var pool))
            {
                Debug.LogError($"Pool '{poolName}' not found!");
                return null;
            }

            Transform obj = pool.Get(position, rotation);
            return obj != null ? obj.gameObject : null;
        }

        /// <summary>
        /// Return an object to its pool
        /// </summary>
        public void Return(string poolName, GameObject obj)
        {
            if (!poolDictionary.TryGetValue(poolName, out var pool))
            {
                Debug.LogError($"Pool '{poolName}' not found!");
                return;
            }

            pool.Return(obj.transform);
        }

        /// <summary>
        /// Return all objects in a pool
        /// </summary>
        public void ReturnAll(string poolName)
        {
            if (!poolDictionary.TryGetValue(poolName, out var pool))
            {
                Debug.LogError($"Pool '{poolName}' not found!");
                return;
            }

            pool.ReturnAll();
        }

        /// <summary>
        /// Return all objects in all pools
        /// </summary>
        public void ReturnAllPools()
        {
            foreach (var pool in poolDictionary.Values)
            {
                pool.ReturnAll();
            }
        }

        /// <summary>
        /// Clear a specific pool
        /// </summary>
        public void ClearPool(string poolName)
        {
            if (!poolDictionary.TryGetValue(poolName, out var pool))
            {
                Debug.LogError($"Pool '{poolName}' not found!");
                return;
            }

            pool.Clear();
            poolDictionary.Remove(poolName);
        }

        /// <summary>
        /// Check if a pool exists
        /// </summary>
        public bool HasPool(string poolName)
        {
            return poolDictionary.ContainsKey(poolName);
        }

        /// <summary>
        /// Get pool statistics
        /// </summary>
        public (int pooled, int active, int total) GetPoolStats(string poolName)
        {
            if (!poolDictionary.TryGetValue(poolName, out var pool))
            {
                return (0, 0, 0);
            }

            return (pool.PooledCount, pool.ActiveCount, pool.TotalCount);
        }
    }
}
