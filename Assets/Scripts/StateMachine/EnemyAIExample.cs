using UnityEngine;

namespace UsefulScripts.StateMachine
{
    /// <summary>
    /// Example enemy AI using the state machine system.
    /// </summary>
    public class EnemyAI : MonoBehaviour
    {
        [Header("Settings")]
        public float idleTime = 2f;
        public float patrolSpeed = 2f;
        public float chaseSpeed = 4f;
        public float attackRange = 1.5f;
        public float detectionRange = 10f;
        public float attackCooldown = 1f;

        [Header("References")]
        public Transform[] patrolPoints;
        public Transform player;

        // Components
        private StateMachine<EnemyAI> stateMachine;
        private Rigidbody rb;

        // State
        public int currentPatrolIndex;
        public float lastAttackTime;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            
            // Initialize state machine
            stateMachine = new StateMachine<EnemyAI>(this);
            
            // Add states
            stateMachine.AddStates(
                new IdleState(),
                new PatrolState(),
                new ChaseState(),
                new AttackState()
            );

            // Set initial state
            stateMachine.SetInitialState<IdleState>();

            // Subscribe to state changes (optional)
            stateMachine.OnStateChanged += (from, to) =>
            {
                Debug.Log($"State changed from {from?.GetType().Name} to {to?.GetType().Name}");
            };
        }

        private void Update()
        {
            stateMachine.Update(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            stateMachine.FixedUpdate(Time.fixedDeltaTime);
        }

        public float DistanceToPlayer()
        {
            if (player == null) return float.MaxValue;
            return Vector3.Distance(transform.position, player.position);
        }

        public void MoveTowards(Vector3 target, float speed)
        {
            Vector3 direction = (target - transform.position).normalized;
            if (rb != null)
            {
                rb.linearVelocity = new Vector3(direction.x * speed, rb.linearVelocity.y, direction.z * speed);
            }
            else
            {
                transform.position += direction * speed * Time.deltaTime;
            }

            // Face movement direction
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
            }
        }

        public void Attack()
        {
            Debug.Log("Enemy attacks!");
            lastAttackTime = Time.time;
            // Add attack logic here
        }

        // Gizmos for visualization
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }

    // --- Enemy States ---

    public class IdleState : State<EnemyAI>
    {
        private float timer;

        public override void Enter()
        {
            timer = 0;
        }

        public override void Update(float deltaTime)
        {
            timer += deltaTime;

            // Check for player
            if (context.DistanceToPlayer() < context.detectionRange)
            {
                context.GetComponent<EnemyAI>().GetStateMachine()?.ChangeState<ChaseState>();
                return;
            }

            // Transition to patrol
            if (timer >= context.idleTime)
            {
                context.GetComponent<EnemyAI>().GetStateMachine()?.ChangeState<PatrolState>();
            }
        }
    }

    public class PatrolState : State<EnemyAI>
    {
        public override void Update(float deltaTime)
        {
            // Check for player
            if (context.DistanceToPlayer() < context.detectionRange)
            {
                context.GetComponent<EnemyAI>().GetStateMachine()?.ChangeState<ChaseState>();
                return;
            }

            // Patrol logic
            if (context.patrolPoints == null || context.patrolPoints.Length == 0)
            {
                context.GetComponent<EnemyAI>().GetStateMachine()?.ChangeState<IdleState>();
                return;
            }

            Transform target = context.patrolPoints[context.currentPatrolIndex];
            context.MoveTowards(target.position, context.patrolSpeed);

            // Reached patrol point
            if (Vector3.Distance(context.transform.position, target.position) < 0.5f)
            {
                context.currentPatrolIndex = (context.currentPatrolIndex + 1) % context.patrolPoints.Length;
                context.GetComponent<EnemyAI>().GetStateMachine()?.ChangeState<IdleState>();
            }
        }
    }

    public class ChaseState : State<EnemyAI>
    {
        public override void Update(float deltaTime)
        {
            float distance = context.DistanceToPlayer();

            // Lost player
            if (distance > context.detectionRange * 1.5f)
            {
                context.GetComponent<EnemyAI>().GetStateMachine()?.ChangeState<IdleState>();
                return;
            }

            // In attack range
            if (distance < context.attackRange)
            {
                context.GetComponent<EnemyAI>().GetStateMachine()?.ChangeState<AttackState>();
                return;
            }

            // Chase player
            if (context.player != null)
            {
                context.MoveTowards(context.player.position, context.chaseSpeed);
            }
        }
    }

    public class AttackState : State<EnemyAI>
    {
        public override void Enter()
        {
            // Stop movement
        }

        public override void Update(float deltaTime)
        {
            float distance = context.DistanceToPlayer();

            // Out of range
            if (distance > context.attackRange * 1.2f)
            {
                context.GetComponent<EnemyAI>().GetStateMachine()?.ChangeState<ChaseState>();
                return;
            }

            // Attack if cooldown is done
            if (Time.time - context.lastAttackTime >= context.attackCooldown)
            {
                context.Attack();
            }
        }
    }

    // Extension for getting state machine from EnemyAI
    public static class EnemyAIExtensions
    {
        private static System.Reflection.FieldInfo stateMachineField;

        public static StateMachine<EnemyAI> GetStateMachine(this EnemyAI enemy)
        {
            if (stateMachineField == null)
            {
                stateMachineField = typeof(EnemyAI).GetField("stateMachine", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            }
            return stateMachineField?.GetValue(enemy) as StateMachine<EnemyAI>;
        }
    }
}
