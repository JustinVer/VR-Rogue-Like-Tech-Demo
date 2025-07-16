using System.Collections;
using UnityEngine;

public class RangedEnemy : BaseEnemy
{
    [Header("Ranged Settings")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float shootForce = 20f;
    [SerializeField] private float spawnDelay = 0.5f;

    protected override void Attack()
    {
        if (projectilePrefab != null && firePoint != null)
        {
            StartCoroutine(spawnBulletAfter(spawnDelay));
        }
    }

    private IEnumerator spawnBulletAfter(float wait)
    {
        yield return new WaitForSeconds(wait);
        if (attackInterupded == false)
        {
            GameObject proj = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

            if (proj.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.velocity = firePoint.forward * shootForce;
            }
            if (proj.TryGetComponent<Projectile>(out var projectile))
            {
                projectile.damage = damage * GameManager.Instance.currentDifficulty;
            }
        }
    }
}