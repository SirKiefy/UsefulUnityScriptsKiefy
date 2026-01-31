using UnityEngine;
using System.Collections.Generic;

namespace UsefulScripts.Pooling
{
    /// <summary>
    /// Generic object pooling system for efficient object reuse.
    /// </summary>
    /// <typeparam name="T">The type of component to pool</typeparam>
    public class ObjectPool<T> where T : Component
    {
        private readonly T prefab;
        private readonly Transform parent;
        private readonly Queue<T> pool;
        private readonly List<T> activeObjects;
        private readonly int maxSize;
        private readonly bool expandable;

        public int PooledCount => pool.Count;
        public int ActiveCount => activeObjects.Count;
        public int TotalCount => PooledCount + ActiveCount;

        /// <summary>
        /// Create a new object pool
        /// </summary>
        /// <param name="prefab">Prefab to instantiate</param>
        /// <param name="initialSize">Initial pool size</param>
        /// <param name="maxSize">Maximum pool size (0 = unlimited)</param>
        /// <param name="expandable">Can the pool grow beyond initial size?</param>
        /// <param name="parent">Parent transform for pooled objects</param>
        public ObjectPool(T prefab, int initialSize = 10, int maxSize = 0, bool expandable = true, Transform parent = null)
        {
            this.prefab = prefab;
            this.parent = parent;
            this.maxSize = maxSize;
            this.expandable = expandable;
            pool = new Queue<T>(initialSize);
            activeObjects = new List<T>(initialSize);

            Prewarm(initialSize);
        }

        /// <summary>
        /// Pre-create objects in the pool
        /// </summary>
        public void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (maxSize > 0 && TotalCount >= maxSize) break;
                
                T obj = CreateNewObject();
                obj.gameObject.SetActive(false);
                pool.Enqueue(obj);
            }
        }

        /// <summary>
        /// Get an object from the pool
        /// </summary>
        public T Get()
        {
            T obj;

            if (pool.Count > 0)
            {
                obj = pool.Dequeue();
            }
            else if (expandable && (maxSize == 0 || TotalCount < maxSize))
            {
                obj = CreateNewObject();
            }
            else
            {
                Debug.LogWarning($"Object pool for {typeof(T).Name} is exhausted!");
                return null;
            }

            obj.gameObject.SetActive(true);
            activeObjects.Add(obj);
            
            if (obj is IPoolable poolable)
            {
                poolable.OnSpawned();
            }

            return obj;
        }

        /// <summary>
        /// Get an object from the pool at a specific position
        /// </summary>
        public T Get(Vector3 position)
        {
            T obj = Get();
            if (obj != null)
            {
                obj.transform.position = position;
            }
            return obj;
        }

        /// <summary>
        /// Get an object from the pool at a specific position and rotation
        /// </summary>
        public T Get(Vector3 position, Quaternion rotation)
        {
            T obj = Get();
            if (obj != null)
            {
                obj.transform.SetPositionAndRotation(position, rotation);
            }
            return obj;
        }

        /// <summary>
        /// Return an object to the pool
        /// </summary>
        public void Return(T obj)
        {
            if (obj == null) return;

            if (obj is IPoolable poolable)
            {
                poolable.OnDespawned();
            }

            obj.gameObject.SetActive(false);
            
            if (parent != null)
            {
                obj.transform.SetParent(parent);
            }

            activeObjects.Remove(obj);
            pool.Enqueue(obj);
        }

        /// <summary>
        /// Return all active objects to the pool
        /// </summary>
        public void ReturnAll()
        {
            for (int i = activeObjects.Count - 1; i >= 0; i--)
            {
                Return(activeObjects[i]);
            }
        }

        /// <summary>
        /// Clear the entire pool
        /// </summary>
        public void Clear()
        {
            foreach (T obj in pool)
            {
                if (obj != null)
                {
                    Object.Destroy(obj.gameObject);
                }
            }
            pool.Clear();

            foreach (T obj in activeObjects)
            {
                if (obj != null)
                {
                    Object.Destroy(obj.gameObject);
                }
            }
            activeObjects.Clear();
        }

        private T CreateNewObject()
        {
            T obj = Object.Instantiate(prefab, parent);
            obj.name = $"{prefab.name}_Pooled_{TotalCount}";
            return obj;
        }
    }

    /// <summary>
    /// Interface for poolable objects
    /// </summary>
    public interface IPoolable
    {
        void OnSpawned();
        void OnDespawned();
    }
}
