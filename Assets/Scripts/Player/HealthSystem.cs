using UnityEngine;

namespace UsefulScripts.Player
{
    /// <summary>
    /// Modular player health and damage system.
    /// </summary>
    public class HealthSystem : MonoBehaviour
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth;
        [SerializeField] private bool regenerateHealth = false;
        [SerializeField] private float regenRate = 5f;
        [SerializeField] private float regenDelay = 3f;

        [Header("Invincibility")]
        [SerializeField] private bool enableInvincibility = true;
        [SerializeField] private float invincibilityDuration = 1f;

        [Header("Shield")]
        [SerializeField] private float maxShield = 0f;
        [SerializeField] private float currentShield;
        [SerializeField] private float shieldRegenRate = 10f;
        [SerializeField] private float shieldRegenDelay = 5f;

        // State
        private bool isInvincible;
        private float invincibilityTimer;
        private float regenTimer;
        private float shieldRegenTimer;
        private bool isDead;

        // Events
        public event System.Action<float, float> OnHealthChanged; // current, max
        public event System.Action<float, float> OnShieldChanged; // current, max
        public event System.Action<float> OnDamageTaken;
        public event System.Action<float> OnHealed;
        public event System.Action OnDeath;
        public event System.Action OnRevive;
        public event System.Action OnInvincibilityStart;
        public event System.Action OnInvincibilityEnd;

        // Properties
        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public float MaxShield => maxShield;
        public float CurrentShield => currentShield;
        public float HealthPercent => currentHealth / maxHealth;
        public float ShieldPercent => maxShield > 0 ? currentShield / maxShield : 0f;
        public bool IsDead => isDead;
        public bool IsInvincible => isInvincible;
        public bool HasShield => currentShield > 0;

        private void Awake()
        {
            currentHealth = maxHealth;
            currentShield = maxShield;
        }

        private void Update()
        {
            UpdateInvincibility();
            UpdateRegeneration();
            UpdateShieldRegeneration();
        }

        private void UpdateInvincibility()
        {
            if (isInvincible)
            {
                invincibilityTimer -= Time.deltaTime;
                if (invincibilityTimer <= 0)
                {
                    isInvincible = false;
                    OnInvincibilityEnd?.Invoke();
                }
            }
        }

        private void UpdateRegeneration()
        {
            if (!regenerateHealth || isDead) return;

            if (currentHealth < maxHealth)
            {
                regenTimer -= Time.deltaTime;
                if (regenTimer <= 0)
                {
                    Heal(regenRate * Time.deltaTime);
                }
            }
        }

        private void UpdateShieldRegeneration()
        {
            if (maxShield <= 0 || isDead) return;

            if (currentShield < maxShield)
            {
                shieldRegenTimer -= Time.deltaTime;
                if (shieldRegenTimer <= 0)
                {
                    currentShield = Mathf.Min(currentShield + shieldRegenRate * Time.deltaTime, maxShield);
                    OnShieldChanged?.Invoke(currentShield, maxShield);
                }
            }
        }

        /// <summary>
        /// Take damage
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (isDead || isInvincible || damage <= 0) return;

            float remainingDamage = damage;

            // Shield absorbs damage first
            if (currentShield > 0)
            {
                if (currentShield >= remainingDamage)
                {
                    currentShield -= remainingDamage;
                    remainingDamage = 0;
                }
                else
                {
                    remainingDamage -= currentShield;
                    currentShield = 0;
                }
                shieldRegenTimer = shieldRegenDelay;
                OnShieldChanged?.Invoke(currentShield, maxShield);
            }

            // Apply remaining damage to health
            if (remainingDamage > 0)
            {
                currentHealth -= remainingDamage;
                regenTimer = regenDelay;
                OnDamageTaken?.Invoke(remainingDamage);
                OnHealthChanged?.Invoke(currentHealth, maxHealth);

                if (currentHealth <= 0)
                {
                    currentHealth = 0;
                    Die();
                }
                else if (enableInvincibility)
                {
                    StartInvincibility();
                }
            }
        }

        /// <summary>
        /// Take damage from a specific source
        /// </summary>
        public void TakeDamage(float damage, GameObject source)
        {
            TakeDamage(damage);
        }

        /// <summary>
        /// Heal the entity
        /// </summary>
        public void Heal(float amount)
        {
            if (isDead || amount <= 0) return;

            float actualHeal = Mathf.Min(amount, maxHealth - currentHealth);
            currentHealth += actualHeal;
            
            if (actualHeal > 0)
            {
                OnHealed?.Invoke(actualHeal);
                OnHealthChanged?.Invoke(currentHealth, maxHealth);
            }
        }

        /// <summary>
        /// Restore health to full
        /// </summary>
        public void FullHeal()
        {
            Heal(maxHealth - currentHealth);
        }

        /// <summary>
        /// Add shield
        /// </summary>
        public void AddShield(float amount)
        {
            if (isDead || amount <= 0) return;

            currentShield = Mathf.Min(currentShield + amount, maxShield);
            OnShieldChanged?.Invoke(currentShield, maxShield);
        }

        /// <summary>
        /// Start invincibility period
        /// </summary>
        public void StartInvincibility()
        {
            StartInvincibility(invincibilityDuration);
        }

        /// <summary>
        /// Start invincibility for a specific duration
        /// </summary>
        public void StartInvincibility(float duration)
        {
            isInvincible = true;
            invincibilityTimer = duration;
            OnInvincibilityStart?.Invoke();
        }

        /// <summary>
        /// End invincibility immediately
        /// </summary>
        public void EndInvincibility()
        {
            if (isInvincible)
            {
                isInvincible = false;
                invincibilityTimer = 0;
                OnInvincibilityEnd?.Invoke();
            }
        }

        /// <summary>
        /// Set max health (and optionally heal to new max)
        /// </summary>
        public void SetMaxHealth(float newMax, bool healToMax = false)
        {
            maxHealth = Mathf.Max(1, newMax);
            currentHealth = Mathf.Min(currentHealth, maxHealth);
            
            if (healToMax)
            {
                currentHealth = maxHealth;
            }
            
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        /// <summary>
        /// Kill the entity
        /// </summary>
        public void Die()
        {
            if (isDead) return;

            isDead = true;
            currentHealth = 0;
            OnDeath?.Invoke();
        }

        /// <summary>
        /// Revive the entity with specified health
        /// </summary>
        public void Revive(float healthPercent = 1f)
        {
            if (!isDead) return;

            isDead = false;
            currentHealth = maxHealth * Mathf.Clamp01(healthPercent);
            currentShield = maxShield;
            OnRevive?.Invoke();
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnShieldChanged?.Invoke(currentShield, maxShield);
        }

        /// <summary>
        /// Instantly kill (bypasses invincibility)
        /// </summary>
        public void InstantKill()
        {
            isInvincible = false;
            TakeDamage(currentHealth + currentShield + 1);
        }
    }
}
