using UnityEngine;

namespace UsefulScripts.ColossusMechanics
{
    /// <summary>
    /// Handles visual and audio feedback when a colossus shakes.
    /// Attach to the player to receive shake feedback when gripping a shaking colossus.
    /// </summary>
    public class ColossusShakeEffect : MonoBehaviour
    {
        [Header("Camera Shake")]
        [SerializeField] private Camera targetCamera;
        [SerializeField] private float cameraShakeIntensity = 0.3f;
        [SerializeField] private float cameraShakeSpeed = 20f;
        [SerializeField] private AnimationCurve shakeIntensityCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

        [Header("Controller Rumble")]
        [SerializeField] private bool enableRumble = true;
        [SerializeField] private float lowFrequencyMotor = 0.5f;
        [SerializeField] private float highFrequencyMotor = 0.3f;

        [Header("Screen Effects")]
        [SerializeField] private bool enableScreenEffects = true;
        [SerializeField] private float vignetteIntensity = 0.3f;
        [SerializeField] private float chromaticAberration = 0.5f;
        [SerializeField] private Color lowStaminaTint = new Color(1f, 0.3f, 0.3f, 0.2f);

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip shakeStartSound;
        [SerializeField] private AudioClip shakeLoopSound;
        [SerializeField] private AudioClip gripStrainSound;
        [SerializeField] private AudioClip staminaLowSound;
        [SerializeField] private float shakeVolume = 0.7f;

        [Header("References")]
        [SerializeField] private GripSystem gripSystem;

        // Camera shake state
        private Vector3 originalCameraLocalPosition;
        private bool isShaking;
        private float shakeTime;
        private float currentShakeIntensity;

        // Effects state
        private bool isPlayingShakeLoop;

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (gripSystem == null)
            {
                gripSystem = GetComponent<GripSystem>();
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }

            if (targetCamera != null)
            {
                originalCameraLocalPosition = targetCamera.transform.localPosition;
            }
        }

        private void OnEnable()
        {
            if (gripSystem != null)
            {
                gripSystem.OnShakeStart += HandleShakeStart;
                gripSystem.OnShakeEnd += HandleShakeEnd;
                gripSystem.OnStaminaChanged += HandleStaminaChanged;
                gripSystem.OnGripEnd += HandleGripEnd;
            }
        }

        private void OnDisable()
        {
            if (gripSystem != null)
            {
                gripSystem.OnShakeStart -= HandleShakeStart;
                gripSystem.OnShakeEnd -= HandleShakeEnd;
                gripSystem.OnStaminaChanged -= HandleStaminaChanged;
                gripSystem.OnGripEnd -= HandleGripEnd;
            }

            StopAllEffects();
        }

        private void Update()
        {
            UpdateCameraShake();
            UpdateStaminaEffects();
        }

        private void HandleShakeStart()
        {
            isShaking = true;
            shakeTime = 0f;

            // Play shake start sound
            if (audioSource != null && shakeStartSound != null)
            {
                audioSource.PlayOneShot(shakeStartSound, shakeVolume);
            }

            // Start shake loop
            if (audioSource != null && shakeLoopSound != null && !isPlayingShakeLoop)
            {
                audioSource.clip = shakeLoopSound;
                audioSource.loop = true;
                audioSource.volume = shakeVolume;
                audioSource.Play();
                isPlayingShakeLoop = true;
            }

            // Start controller rumble
            StartRumble();
        }

        private void HandleShakeEnd()
        {
            isShaking = false;

            // Stop shake loop
            if (isPlayingShakeLoop)
            {
                audioSource.Stop();
                audioSource.loop = false;
                isPlayingShakeLoop = false;
            }

            // Stop controller rumble
            StopRumble();

            // Smoothly return camera to original position
            if (targetCamera != null)
            {
                targetCamera.transform.localPosition = originalCameraLocalPosition;
            }
        }

        private void HandleStaminaChanged(float current, float max)
        {
            float staminaPercent = current / max;

            // Play low stamina warning sound
            if (staminaPercent <= 0.2f && gripSystem.IsGripping)
            {
                if (audioSource != null && staminaLowSound != null && !audioSource.isPlaying)
                {
                    audioSource.PlayOneShot(staminaLowSound, 0.5f);
                }
            }

            // Update rumble intensity based on stamina
            if (enableRumble && gripSystem.IsGripping)
            {
                float rumbleIntensity = 1f - staminaPercent;
                UpdateRumble(rumbleIntensity);
            }
        }

        private void HandleGripEnd()
        {
            StopAllEffects();
        }

        private void UpdateCameraShake()
        {
            if (targetCamera == null) return;

            if (isShaking && gripSystem != null && gripSystem.IsGripping)
            {
                shakeTime += Time.deltaTime;
                
                // Get shake intensity from colossus
                float colossusIntensity = 1f;
                if (gripSystem.CurrentColossus != null)
                {
                    colossusIntensity = gripSystem.CurrentColossus.ShakeIntensity;
                }

                // Calculate shake
                float intensity = cameraShakeIntensity * colossusIntensity;
                
                // Add extra shake when stamina is low
                if (gripSystem.IsLowStamina)
                {
                    intensity *= 1.5f;
                }

                // Use Perlin noise for smooth camera shake
                float xShake = (Mathf.PerlinNoise(shakeTime * cameraShakeSpeed, 0f) - 0.5f) * 2f;
                float yShake = (Mathf.PerlinNoise(0f, shakeTime * cameraShakeSpeed) - 0.5f) * 2f;

                Vector3 shakeOffset = new Vector3(xShake, yShake, 0f) * intensity;
                targetCamera.transform.localPosition = originalCameraLocalPosition + shakeOffset;

                currentShakeIntensity = intensity;
            }
            else
            {
                // Smoothly return to original position
                targetCamera.transform.localPosition = Vector3.Lerp(
                    targetCamera.transform.localPosition,
                    originalCameraLocalPosition,
                    Time.deltaTime * 10f
                );
                currentShakeIntensity = 0f;
            }
        }

        private void UpdateStaminaEffects()
        {
            if (gripSystem == null || !gripSystem.IsGripping) return;

            // Play strain sound periodically when stamina is low
            if (gripSystem.IsLowStamina && audioSource != null && gripStrainSound != null)
            {
                if (!audioSource.isPlaying && Random.value < Time.deltaTime * 2f)
                {
                    audioSource.PlayOneShot(gripStrainSound, 0.3f);
                }
            }
        }

        private void StartRumble()
        {
            if (!enableRumble) return;

            // Note: Unity's built-in input system doesn't directly support rumble.
            // This is a placeholder for integration with Input System package or third-party solutions.
            // For actual implementation, you would use:
            // - Gamepad.current.SetMotorSpeeds(lowFrequencyMotor, highFrequencyMotor);
        }

        private void UpdateRumble(float intensity)
        {
            if (!enableRumble) return;

            // Placeholder for rumble intensity update
            // Gamepad.current?.SetMotorSpeeds(lowFrequencyMotor * intensity, highFrequencyMotor * intensity);
        }

        private void StopRumble()
        {
            if (!enableRumble) return;

            // Placeholder for stopping rumble
            // Gamepad.current?.SetMotorSpeeds(0f, 0f);
        }

        private void StopAllEffects()
        {
            isShaking = false;
            isPlayingShakeLoop = false;

            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
                audioSource.loop = false;
            }

            if (targetCamera != null)
            {
                targetCamera.transform.localPosition = originalCameraLocalPosition;
            }

            StopRumble();
        }

        /// <summary>
        /// Gets the current camera shake intensity.
        /// </summary>
        public float GetCurrentShakeIntensity()
        {
            return currentShakeIntensity;
        }

        /// <summary>
        /// Manually triggers a camera shake effect.
        /// </summary>
        public void TriggerShake(float intensity, float duration)
        {
            StartCoroutine(ManualShakeCoroutine(intensity, duration));
        }

        private System.Collections.IEnumerator ManualShakeCoroutine(float intensity, float duration)
        {
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float currentIntensity = intensity * shakeIntensityCurve.Evaluate(t);

                if (targetCamera != null)
                {
                    float xShake = (Mathf.PerlinNoise(elapsed * cameraShakeSpeed, 0f) - 0.5f) * 2f;
                    float yShake = (Mathf.PerlinNoise(0f, elapsed * cameraShakeSpeed) - 0.5f) * 2f;
                    Vector3 shakeOffset = new Vector3(xShake, yShake, 0f) * currentIntensity;
                    targetCamera.transform.localPosition = originalCameraLocalPosition + shakeOffset;
                }

                yield return null;
            }

            if (targetCamera != null)
            {
                targetCamera.transform.localPosition = originalCameraLocalPosition;
            }
        }

        /// <summary>
        /// Sets the target camera for shake effects.
        /// </summary>
        public void SetTargetCamera(Camera camera)
        {
            targetCamera = camera;
            if (targetCamera != null)
            {
                originalCameraLocalPosition = targetCamera.transform.localPosition;
            }
        }

        /// <summary>
        /// Sets the grip system reference.
        /// </summary>
        public void SetGripSystem(GripSystem system)
        {
            // Unsubscribe from old system
            if (gripSystem != null)
            {
                gripSystem.OnShakeStart -= HandleShakeStart;
                gripSystem.OnShakeEnd -= HandleShakeEnd;
                gripSystem.OnStaminaChanged -= HandleStaminaChanged;
                gripSystem.OnGripEnd -= HandleGripEnd;
            }

            gripSystem = system;

            // Subscribe to new system
            if (gripSystem != null)
            {
                gripSystem.OnShakeStart += HandleShakeStart;
                gripSystem.OnShakeEnd += HandleShakeEnd;
                gripSystem.OnStaminaChanged += HandleStaminaChanged;
                gripSystem.OnGripEnd += HandleGripEnd;
            }
        }
    }
}
