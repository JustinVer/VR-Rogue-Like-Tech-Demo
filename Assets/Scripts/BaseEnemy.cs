using System.Collections;
using Unity.VisualScripting;
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

    protected virtual void Start()
    {
        currentHealth = maxHealth;
        player = GameObject.FindGameObjectWithTag("Player").transform.GetChild(0).transform;
        agent = GetComponent<NavMeshAgent>();
    }

    protected virtual void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(detectionPoint.position, player.position);

        if (playerDetected || distance <= detectionRange)
        {
            playerDetected = true;

            if (distance <= attackRange && (!Physics.Linecast(detectionPoint.position, player.position + new Vector3(0, 1, 0), detectionLayerMask) || !needLineOfSit))
            {
                agent.isStopped = true;
                rb.velocity = Vector3.zero;
                animator.SetBool("Running", false);
                // Smoothly rotate toward the player
                Vector3 direction = (player.position - detectionPoint.position).normalized;
                direction.y = 0;
                if (direction != Vector3.zero)
                {
                    Quaternion lookRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
                    AnimatorStateInfo animInfo = animator.GetCurrentAnimatorStateInfo(0);
                    if (Time.time - lastAttackTime >= attackCooldown && ((transform.rotation.y - lookRotation.y) < 0.1 && (transform.rotation.y - lookRotation.y) > -0.1) && (animInfo.IsName("Idle") || animInfo.IsName("Walk") || animInfo.IsName("Run")))
                    {
                        animator.SetTrigger("Attacking");
                        attackInterupded = false;
                        Attack();
                        lastAttackTime = Time.time;
                    }
                }
            }
            else
            {
                agent.isStopped = false;
                animator.SetBool("Running", true);
                agent.SetDestination(player.position);
            }
        }
    }

    public virtual void TakeDamage(float amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            animator.ResetTrigger("Hit");
            StartCoroutine(Die());
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
        yield return new WaitForNextFrameUnit();
        yield return new WaitUntil(() => !animator.GetCurrentAnimatorStateInfo(0).IsName(animationName));
        animator.SetBool(boolName, value);
    }

    protected virtual IEnumerator Die()
    {
        animator.ResetTrigger("Hit");
        animator.SetTrigger("Dead");
        yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).IsName("Die"));
        yield return new WaitForSeconds(deathAnimationTime);
        Destroy(gameObject);
    }

    protected abstract void Attack();
}