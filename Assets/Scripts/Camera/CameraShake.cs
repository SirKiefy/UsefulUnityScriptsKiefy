using UnityEngine;
using System.Collections;

namespace UsefulScripts.Camera
{
    /// <summary>
    /// Camera shake system with multiple shake modes.
    /// </summary>
    public class CameraShake : MonoBehaviour
    {
        public enum ShakeMode
        {
            Perlin,
            Random,
            Sine
        }

        [Header("Default Settings")]
        [SerializeField] private float defaultDuration = 0.3f;
        [SerializeField] private float defaultMagnitude = 0.2f;
        [SerializeField] private ShakeMode defaultMode = ShakeMode.Perlin;

        [Header("Trauma Settings")]
        [SerializeField] private bool useTrauma = true;
        [SerializeField] private float traumaDecay = 1f;
        [SerializeField] private float maxShakeAngle = 10f;
        [SerializeField] private float maxShakeOffset = 0.5f;

        // State
        private Vector3 originalPosition;
        private Quaternion originalRotation;
        private float trauma;
        private float perlinSeed;
        private Coroutine shakeCoroutine;
        private bool isShaking;

        // Static instance for easy access
        private static CameraShake _instance;
        public static CameraShake Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<CameraShake>();
                }
                return _instance;
            }
        }

        private void Awake()
        {
            _instance = this;
            originalPosition = transform.localPosition;
            originalRotation = transform.localRotation;
            perlinSeed = Random.Range(0f, 1000f);
        }

        private void Update()
        {
            if (useTrauma && trauma > 0)
            {
                ApplyTraumaShake();
                trauma = Mathf.Max(0, trauma - traumaDecay * Time.deltaTime);
            }
        }

        private void ApplyTraumaShake()
        {
            float shake = trauma * trauma; // Square for more dramatic effect

            // Position shake using Perlin noise
            float offsetX = maxShakeOffset * shake * (Mathf.PerlinNoise(perlinSeed, Time.time * 25f) * 2 - 1);
            float offsetY = maxShakeOffset * shake * (Mathf.PerlinNoise(perlinSeed + 1, Time.time * 25f) * 2 - 1);

            // Rotation shake
            float angle = maxShakeAngle * shake * (Mathf.PerlinNoise(perlinSeed + 2, Time.time * 25f) * 2 - 1);

            transform.localPosition = originalPosition + new Vector3(offsetX, offsetY, 0);
            transform.localRotation = originalRotation * Quaternion.Euler(0, 0, angle);
        }

        /// <summary>
        /// Add trauma to the camera (values 0-1)
        /// </summary>
        public void AddTrauma(float amount)
        {
            trauma = Mathf.Clamp01(trauma + amount);
        }

        /// <summary>
        /// Shake the camera with default settings
        /// </summary>
        public void Shake()
        {
            Shake(defaultDuration, defaultMagnitude, defaultMode);
        }

        /// <summary>
        /// Shake the camera with specified parameters
        /// </summary>
        public void Shake(float duration, float magnitude, ShakeMode mode = ShakeMode.Perlin)
        {
            if (shakeCoroutine != null)
            {
                StopCoroutine(shakeCoroutine);
            }
            shakeCoroutine = StartCoroutine(ShakeRoutine(duration, magnitude, mode));
        }

        /// <summary>
        /// Shake on a specific axis
        /// </summary>
        public void ShakeAxis(float duration, float magnitude, Vector3 axis)
        {
            if (shakeCoroutine != null)
            {
                StopCoroutine(shakeCoroutine);
            }
            shakeCoroutine = StartCoroutine(ShakeAxisRoutine(duration, magnitude, axis.normalized));
        }

        /// <summary>
        /// Stop the current shake
        /// </summary>
        public void StopShake()
        {
            if (shakeCoroutine != null)
            {
                StopCoroutine(shakeCoroutine);
            }
            trauma = 0;
            transform.localPosition = originalPosition;
            transform.localRotation = originalRotation;
            isShaking = false;
        }

        private IEnumerator ShakeRoutine(float duration, float magnitude, ShakeMode mode)
        {
            isShaking = true;
            float elapsed = 0;
            float seed = Random.Range(0f, 1000f);

            while (elapsed < duration)
            {
                float progress = elapsed / duration;
                float dampingFactor = 1f - progress; // Linear decay
                float currentMagnitude = magnitude * dampingFactor;

                Vector3 offset = Vector3.zero;

                switch (mode)
                {
                    case ShakeMode.Perlin:
                        offset.x = (Mathf.PerlinNoise(seed, elapsed * 25f) * 2 - 1) * currentMagnitude;
                        offset.y = (Mathf.PerlinNoise(seed + 1, elapsed * 25f) * 2 - 1) * currentMagnitude;
                        break;

                    case ShakeMode.Random:
                        offset = Random.insideUnitSphere * currentMagnitude;
                        offset.z = 0;
                        break;

                    case ShakeMode.Sine:
                        float frequency = 30f;
                        offset.x = Mathf.Sin(elapsed * frequency) * currentMagnitude;
                        offset.y = Mathf.Cos(elapsed * frequency * 1.1f) * currentMagnitude;
                        break;
                }

                transform.localPosition = originalPosition + offset;

                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.localPosition = originalPosition;
            transform.localRotation = originalRotation;
            isShaking = false;
        }

        private IEnumerator ShakeAxisRoutine(float duration, float magnitude, Vector3 axis)
        {
            isShaking = true;
            float elapsed = 0;

            while (elapsed < duration)
            {
                float progress = elapsed / duration;
                float dampingFactor = 1f - progress;
                float offset = Mathf.Sin(elapsed * 50f) * magnitude * dampingFactor;

                transform.localPosition = originalPosition + axis * offset;

                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.localPosition = originalPosition;
            isShaking = false;
        }

        /// <summary>
        /// Static method to shake the main camera
        /// </summary>
        public static void ShakeCamera(float duration, float magnitude, ShakeMode mode = ShakeMode.Perlin)
        {
            Instance?.Shake(duration, magnitude, mode);
        }

        /// <summary>
        /// Static method to add trauma to main camera
        /// </summary>
        public static void AddCameraTrauma(float amount)
        {
            Instance?.AddTrauma(amount);
        }

        /// <summary>
        /// Set the original position (useful after camera movement)
        /// </summary>
        public void UpdateOriginalPosition()
        {
            if (!isShaking)
            {
                originalPosition = transform.localPosition;
                originalRotation = transform.localRotation;
            }
        }
    }
}
