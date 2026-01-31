using UnityEngine;

namespace UsefulScripts.Core
{
    /// <summary>
    /// Generic Singleton pattern for MonoBehaviours.
    /// Ensures only one instance exists and persists across scenes.
    /// </summary>
    /// <typeparam name="T">The type of the singleton class</typeparam>
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static readonly object _lock = new object();
        private static bool _applicationIsQuitting = false;

        /// <summary>
        /// Gets the singleton instance. Creates one if it doesn't exist.
        /// </summary>
        public static T Instance
        {
            get
            {
                if (_applicationIsQuitting)
                {
                    Debug.LogWarning($"[Singleton] Instance '{typeof(T)}' already destroyed on application quit. Won't create again.");
                    return null;
                }

                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = FindFirstObjectByType<T>();

                        if (_instance == null)
                        {
                            GameObject singletonObject = new GameObject();
                            _instance = singletonObject.AddComponent<T>();
                            singletonObject.name = $"[Singleton] {typeof(T)}";
                        }
                    }

                    return _instance;
                }
            }
        }

        /// <summary>
        /// Whether to persist across scene loads
        /// </summary>
        [SerializeField] protected bool persistAcrossScenes = true;

        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
                if (persistAcrossScenes)
                {
                    DontDestroyOnLoad(gameObject);
                }
                OnSingletonAwake();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Called when the singleton is first initialized
        /// </summary>
        protected virtual void OnSingletonAwake() { }

        protected virtual void OnApplicationQuit()
        {
            _applicationIsQuitting = true;
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
