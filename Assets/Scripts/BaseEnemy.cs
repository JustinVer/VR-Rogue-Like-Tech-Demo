using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public abstract class BaseEnemy : MonoBehaviour, Health
{
    [Header("Enemy Settings")]
    public float maxHealth = 15;
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
    [SerializeField] protected bool needLineOfSit = true;
    private bool playerDetected = false;
    [SerializeField] private Animator animator;
    [SerializeField] private float deathAnimationTime;
    [SerializeField] private Rigidbody rb;
    protected bool attackInterupded = false;
    protected bool dead = false;

    protected virtual void Start()
    {
        currentHealth = maxHealth;
        player = GameObject.FindGameObjectWithTag("Player").transform.GetChild(0).transform;
        agent = GetComponent<NavMeshAgent>();

        // Disable agent auto rotation — we handle it ourselves
        agent.updateRotation = false;
    }

    protected virtual void Update()
    {
        if (player == null || dead) return;

        float distance = Vector3.Distance(detectionPoint.position, player.position);

        if (playerDetected || distance <= detectionRange)
        {
            playerDetected = true;

            // Check line of sight if needed
            bool hasLineOfSight = !Physics.Linecast(detectionPoint.position, player.position + Vector3.up, detectionLayerMask);
            if (distance <= attackRange && (hasLineOfSight || !needLineOfSit))
            {
                agent.isStopped = true;
                rb.velocity = Vector3.zero;
                animator.SetBool("Running", false);

                // Rotate toward the player
                Vector3 lookDirection = (player.position - detectionPoint.position).normalized;
                lookDirection.y = 0;

                if (lookDirection != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
                }

                float angleToPlayer = Vector3.Angle(transform.forward, lookDirection);

                // Only attack if roughly facing the player
                AnimatorStateInfo animInfo = animator.GetCurrentAnimatorStateInfo(0);
                if (Time.time - lastAttackTime >= attackCooldown && angleToPlayer < 10f && (animInfo.IsName("Idle") || animInfo.IsName("Walk") || animInfo.IsName("Run")))
                {
                    animator.SetTrigger("Attacking");
                    attackInterupded = false;
                    Attack();
                    lastAttackTime = Time.time;
                }
            }
            else
            {
                agent.isStopped = false;
                agent.SetDestination(player.position);
                animator.SetBool("Running", true);

                // Rotate toward movement direction (optional)
                if (agent.velocity.sqrMagnitude > 0.1f)
                {
                    Vector3 moveDir = agent.velocity.normalized;
                    moveDir.y = 0;
                    Quaternion moveRot = Quaternion.LookRotation(moveDir);
                    transform.rotation = Quaternion.Slerp(transform.rotation, moveRot, Time.deltaTime * 5f);
                }
            }
        }
    }

    public virtual void TakeDamage(float amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            if (!dead)
            {
                StartCoroutine(Die());
            }
        }
        else
        {
            attackInterupded = true;
            if (!animator.GetCurrentAnimatorStateInfo(0).IsName("GetHit"))
            {
                animator.SetTrigger("Hit");
            }
        }
    }

    protected virtual IEnumerator WaitForAnimationToFinishBool(string animationName, string boolName, bool value)
    {
        yield return null;
        yield return new WaitUntil(() => !animator.GetCurrentAnimatorStateInfo(0).IsName(animationName));
        animator.SetBool(boolName, value);
    }

    protected virtual IEnumerator Die()
    {
        dead = true;
        animator.SetTrigger("Dead");
        yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).IsName("Die"));
        rb.velocity = Vector3.zero;
        yield return new WaitForSeconds(deathAnimationTime);
        Destroy(gameObject);
    }

    protected abstract void Attack();
}
