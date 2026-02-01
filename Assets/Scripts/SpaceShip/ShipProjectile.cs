using UnityEngine;

namespace UsefulScripts.SpaceShip
{
    /// <summary>
    /// Projectile for spaceship weapons.
    /// Handles ballistic and missile behavior.
    /// </summary>
    public class ShipProjectile : MonoBehaviour
    {
        [Header("Projectile Settings")]
        [SerializeField] private ProjectileType projectileType = ProjectileType.Ballistic;
        [SerializeField] private float damage = 25f;
        [SerializeField] private DamageType damageType = DamageType.Kinetic;
        [SerializeField] private float lifetime = 5f;
        [SerializeField] private float speed = 1500f;

        [Header("Missile Settings")]
        [SerializeField] private bool isHoming = false;
        [SerializeField] private float turnRate = 180f;
        [SerializeField] private float armingTime = 0.5f;
        [SerializeField] private float acquisitionRange = 500f;
        [SerializeField] private float trackingAngle = 60f;

        [Header("Explosion")]
        [SerializeField] private bool explodeOnImpact = false;
        [SerializeField] private float explosionRadius = 10f;
        [SerializeField] private float explosionDamage = 50f;
        [SerializeField] private GameObject explosionPrefab;

        [Header("Effects")]
        [SerializeField] private TrailRenderer trail;
        [SerializeField] private ParticleSystem thrusterParticles;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip impactSound;

        // State
        private Transform target;
        private Rigidbody rb;
        private float spawnTime;
        private bool isArmed;
        private bool hasHit;
        private Transform owner;

        // Properties
        public Transform Target => target;
        public float Damage => damage;
        public DamageType DamageTypeValue => damageType;
        public Transform Owner => owner;
        public bool IsArmed => isArmed;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            spawnTime = Time.time;

            if (rb != null)
            {
                rb.useGravity = false;
            }
        }

        private void Start()
        {
            // Initial velocity if not set externally
            if (rb != null && rb.linearVelocity.magnitude < 1f)
            {
                rb.linearVelocity = transform.forward * speed;
            }

            // Start thruster effects for missiles
            if (projectileType == ProjectileType.Missile && thrusterParticles != null)
            {
                thrusterParticles.Play();
            }

            // Destroy after lifetime
            Destroy(gameObject, lifetime);
        }

        private void Update()
        {
            // Check arming
            if (!isArmed && Time.time - spawnTime >= armingTime)
            {
                isArmed = true;
            }

            // Homing behavior
            if (isHoming && target != null && isArmed)
            {
                TrackTarget();
            }
        }

        private void TrackTarget()
        {
            Vector3 dirToTarget = (target.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, dirToTarget);

            // Only track if within tracking angle
            if (angle <= trackingAngle)
            {
                Quaternion targetRotation = Quaternion.LookRotation(dirToTarget);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnRate * Time.deltaTime);

                if (rb != null)
                {
                    rb.linearVelocity = transform.forward * speed;
                }
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            HandleImpact(collision.transform, collision.contacts[0].point);
        }

        private void OnTriggerEnter(Collider other)
        {
            HandleImpact(other.transform, other.ClosestPoint(transform.position));
        }

        private void HandleImpact(Transform hitTransform, Vector3 hitPoint)
        {
            if (hasHit) return;
            if (hitTransform == owner) return;
            if (!isArmed && projectileType == ProjectileType.Missile) return;

            hasHit = true;

            if (explodeOnImpact)
            {
                Explode(hitPoint);
            }
            else
            {
                ApplyDamage(hitTransform, hitPoint);
            }

            PlayImpactEffects(hitPoint);
            Destroy();
        }

        private void ApplyDamage(Transform target, Vector3 hitPoint)
        {
            // Try to damage ship health system
            ShipHealthSystem healthSystem = target.GetComponent<ShipHealthSystem>();
            if (healthSystem != null)
            {
                healthSystem.TakeDamage(damage, damageType, hitPoint, owner);
                return;
            }

            // Try combat manager
            ShipCombatManager combatManager = target.GetComponent<ShipCombatManager>();
            if (combatManager != null)
            {
                combatManager.ProcessDamage(damage, damageType, hitPoint, owner);
                return;
            }

            // Try generic health system
            var genericHealth = target.GetComponent<Player.HealthSystem>();
            if (genericHealth != null)
            {
                genericHealth.TakeDamage(damage);
            }
        }

        private void Explode(Vector3 explosionPoint)
        {
            // Spawn explosion effect
            if (explosionPrefab != null)
            {
                GameObject explosion = Instantiate(explosionPrefab, explosionPoint, Quaternion.identity);
                Destroy(explosion, 3f);
            }

            // Deal area damage
            Collider[] hits = Physics.OverlapSphere(explosionPoint, explosionRadius);
            foreach (var hit in hits)
            {
                if (hit.transform == owner) continue;

                float distance = Vector3.Distance(explosionPoint, hit.transform.position);
                float falloff = 1f - (distance / explosionRadius);
                float actualDamage = explosionDamage * Mathf.Max(0f, falloff);

                ApplyDamage(hit.transform, hit.ClosestPoint(explosionPoint));

                // Apply explosion force
                Rigidbody hitRb = hit.GetComponent<Rigidbody>();
                if (hitRb != null)
                {
                    hitRb.AddExplosionForce(explosionDamage * 10f, explosionPoint, explosionRadius, 0.5f);
                }
            }
        }

        private void PlayImpactEffects(Vector3 point)
        {
            if (audioSource != null && impactSound != null)
            {
                AudioSource.PlayClipAtPoint(impactSound, point);
            }
        }

        /// <summary>
        /// Set projectile damage
        /// </summary>
        public void SetDamage(float damage)
        {
            this.damage = damage;
        }

        /// <summary>
        /// Set projectile target for homing
        /// </summary>
        public void SetTarget(Transform target)
        {
            this.target = target;
        }

        /// <summary>
        /// Set projectile owner (won't damage owner)
        /// </summary>
        public void SetOwner(Transform owner)
        {
            this.owner = owner;
        }

        /// <summary>
        /// Set damage type
        /// </summary>
        public void SetDamageType(DamageType type)
        {
            this.damageType = type;
        }

        /// <summary>
        /// Initialize projectile with full parameters
        /// </summary>
        public void Initialize(float damage, DamageType damageType, Transform owner, Transform target = null)
        {
            this.damage = damage;
            this.damageType = damageType;
            this.owner = owner;
            this.target = target;
        }

        /// <summary>
        /// Destroy this projectile
        /// </summary>
        public void Destroy()
        {
            if (trail != null)
            {
                trail.transform.SetParent(null);
                Destroy(trail.gameObject, trail.time);
            }

            Destroy(gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            // Draw explosion radius
            if (explodeOnImpact)
            {
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
                Gizmos.DrawWireSphere(transform.position, explosionRadius);
            }

            // Draw tracking cone
            if (isHoming)
            {
                Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
                Vector3 leftDir = Quaternion.Euler(0, -trackingAngle, 0) * transform.forward;
                Vector3 rightDir = Quaternion.Euler(0, trackingAngle, 0) * transform.forward;
                Gizmos.DrawRay(transform.position, leftDir * acquisitionRange);
                Gizmos.DrawRay(transform.position, rightDir * acquisitionRange);
            }

            // Draw target line
            if (target != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, target.position);
            }
        }
    }

    /// <summary>
    /// Type of projectile
    /// </summary>
    public enum ProjectileType
    {
        /// <summary>
        /// Simple ballistic projectile
        /// </summary>
        Ballistic,

        /// <summary>
        /// Guided missile
        /// </summary>
        Missile,

        /// <summary>
        /// Energy weapon (laser, plasma)
        /// </summary>
        Energy,

        /// <summary>
        /// Torpedo (slow, heavy damage)
        /// </summary>
        Torpedo
    }
}
