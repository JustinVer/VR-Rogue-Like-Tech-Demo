using UnityEngine;
using UnityEngine.AI;

public abstract class BaseEnemy : MonoBehaviour, Health
{
    [Header("Enemy Settings")]
    public float maxHealth = 100f;
    public float damage = 10f;
    public float attackCooldown = 1f;
    public float attackRange = 2f;
    public float detectionRange = 15f;
    [SerializeField] private Transform detectionPoint;

    protected float currentHealth;
    protected float lastAttackTime;
    protected Transform player;
    protected NavMeshAgent agent;
    [SerializeField] private LayerMask detectionLayerMask;
    [SerializeField] private LayerMask playerLayer;
    protected bool needLineOfSit = true;
    private bool playerDetected = false;

    protected virtual void Start()
    {
        currentHealth = maxHealth;
        player = GameObject.FindGameObjectWithTag("Player").transform.GetChild(0).transform;
        agent = GetComponent<NavMeshAgent>();
    }

    protected virtual void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (playerDetected || distance <= detectionRange)
        {
            playerDetected = true;
            if (distance <= attackRange && (!Physics.Linecast(detectionPoint.position, player.position, detectionLayerMask) || !needLineOfSit))
            {
                agent.isStopped = true;

                // Smoothly rotate toward the player
                Vector3 direction = (player.position - transform.position).normalized;
                direction.y = 0;
                if (direction != Vector3.zero)
                {
                    Quaternion lookRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
                }

                if (Time.time - lastAttackTime >= attackCooldown && Physics.Linecast(detectionPoint.position, (detectionPoint.forward * attackRange) + detectionPoint.position, playerLayer))
                {
                    Attack();
                    lastAttackTime = Time.time;
                }


            }
            else
            {
                agent.isStopped = false;
                agent.SetDestination(player.position);
            }
        }
    }

    public virtual void TakeDamage(float amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        Destroy(gameObject);
    }

    protected abstract void Attack();
}