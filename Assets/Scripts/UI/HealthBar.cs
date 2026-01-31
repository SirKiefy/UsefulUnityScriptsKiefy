using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UsefulScripts.UI
{
    /// <summary>
    /// Universal health bar component that works with the HealthSystem.
    /// </summary>
    public class HealthBar : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Player.HealthSystem healthSystem;
        [SerializeField] private Image healthFillImage;
        [SerializeField] private Image shieldFillImage;
        [SerializeField] private Image damageFlashImage;
        [SerializeField] private TextMeshProUGUI healthText;

        [Header("Settings")]
        [SerializeField] private bool smoothFill = true;
        [SerializeField] private float smoothSpeed = 5f;
        [SerializeField] private bool showNumbers = true;
        [SerializeField] private string numberFormat = "{0}/{1}";

        [Header("Colors")]
        [SerializeField] private Gradient healthGradient;
        [SerializeField] private Color shieldColor = new Color(0.3f, 0.7f, 1f);
        [SerializeField] private Color damageFlashColor = new Color(1f, 0f, 0f, 0.5f);

        [Header("Effects")]
        [SerializeField] private float damageFlashDuration = 0.1f;
        [SerializeField] private bool shakeOnDamage = true;
        [SerializeField] private float shakeMagnitude = 5f;

        private float targetFill;
        private float currentFill;
        private RectTransform rectTransform;
        private Vector3 originalPosition;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                originalPosition = rectTransform.localPosition;
            }

            SetupGradient();
        }

        private void OnEnable()
        {
            if (healthSystem != null)
            {
                healthSystem.OnHealthChanged += UpdateHealthBar;
                healthSystem.OnShieldChanged += UpdateShieldBar;
                healthSystem.OnDamageTaken += OnDamage;

                // Initialize
                UpdateHealthBar(healthSystem.CurrentHealth, healthSystem.MaxHealth);
                UpdateShieldBar(healthSystem.CurrentShield, healthSystem.MaxShield);
            }
        }

        private void OnDisable()
        {
            if (healthSystem != null)
            {
                healthSystem.OnHealthChanged -= UpdateHealthBar;
                healthSystem.OnShieldChanged -= UpdateShieldBar;
                healthSystem.OnDamageTaken -= OnDamage;
            }
        }

        private void Update()
        {
            if (smoothFill && healthFillImage != null)
            {
                currentFill = Mathf.Lerp(currentFill, targetFill, smoothSpeed * Time.deltaTime);
                healthFillImage.fillAmount = currentFill;
                UpdateHealthColor();
            }
        }

        private void SetupGradient()
        {
            if (healthGradient.colorKeys.Length == 0)
            {
                healthGradient = new Gradient();
                healthGradient.SetKeys(
                    new GradientColorKey[] 
                    {
                        new GradientColorKey(Color.red, 0f),
                        new GradientColorKey(Color.yellow, 0.5f),
                        new GradientColorKey(Color.green, 1f)
                    },
                    new GradientAlphaKey[] 
                    {
                        new GradientAlphaKey(1f, 0f),
                        new GradientAlphaKey(1f, 1f)
                    }
                );
            }
        }

        private void UpdateHealthBar(float current, float max)
        {
            targetFill = max > 0 ? current / max : 0;

            if (!smoothFill && healthFillImage != null)
            {
                healthFillImage.fillAmount = targetFill;
                currentFill = targetFill;
            }

            UpdateHealthColor();
            UpdateText(current, max);
        }

        private void UpdateShieldBar(float current, float max)
        {
            if (shieldFillImage != null)
            {
                shieldFillImage.fillAmount = max > 0 ? current / max : 0;
                shieldFillImage.color = shieldColor;
                shieldFillImage.gameObject.SetActive(max > 0);
            }
        }

        private void UpdateHealthColor()
        {
            if (healthFillImage != null && healthGradient != null)
            {
                healthFillImage.color = healthGradient.Evaluate(smoothFill ? currentFill : targetFill);
            }
        }

        private void UpdateText(float current, float max)
        {
            if (healthText != null && showNumbers)
            {
                healthText.text = string.Format(numberFormat, Mathf.CeilToInt(current), Mathf.CeilToInt(max));
            }
        }

        private void OnDamage(float damage)
        {
            if (damageFlashImage != null)
            {
                StartCoroutine(DamageFlash());
            }

            if (shakeOnDamage)
            {
                StartCoroutine(Shake());
            }
        }

        private System.Collections.IEnumerator DamageFlash()
        {
            damageFlashImage.color = damageFlashColor;
            yield return new WaitForSeconds(damageFlashDuration);
            damageFlashImage.color = Color.clear;
        }

        private System.Collections.IEnumerator Shake()
        {
            float elapsed = 0;
            float duration = 0.2f;

            while (elapsed < duration)
            {
                float x = Random.Range(-1f, 1f) * shakeMagnitude;
                float y = Random.Range(-1f, 1f) * shakeMagnitude;
                rectTransform.localPosition = originalPosition + new Vector3(x, y, 0);
                elapsed += Time.deltaTime;
                yield return null;
            }

            rectTransform.localPosition = originalPosition;
        }

        /// <summary>
        /// Set the health system to track
        /// </summary>
        public void SetHealthSystem(Player.HealthSystem system)
        {
            if (healthSystem != null)
            {
                healthSystem.OnHealthChanged -= UpdateHealthBar;
                healthSystem.OnShieldChanged -= UpdateShieldBar;
                healthSystem.OnDamageTaken -= OnDamage;
            }

            healthSystem = system;

            if (healthSystem != null)
            {
                healthSystem.OnHealthChanged += UpdateHealthBar;
                healthSystem.OnShieldChanged += UpdateShieldBar;
                healthSystem.OnDamageTaken += OnDamage;

                UpdateHealthBar(healthSystem.CurrentHealth, healthSystem.MaxHealth);
                UpdateShieldBar(healthSystem.CurrentShield, healthSystem.MaxShield);
            }
        }
    }
}
