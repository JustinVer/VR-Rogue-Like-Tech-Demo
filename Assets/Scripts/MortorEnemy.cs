using System.Collections;
using UnityEngine;

public class MortorEnemy : BaseEnemy
{
    [Header("Ranged Settings")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float fireTimeMultiplyer = 12.5f;
    public float fireTimeBase = 0.8f;
    [SerializeField] private float spawnDelay = 1.2f;

    protected override void Attack()
    {
        if (projectilePrefab != null && firePoint != null)
        {
            StartCoroutine(spawnBulletAfter(spawnDelay));
        }
    }

    public static Vector3 CalculateVelocityForTime(Vector3 startPoint, Vector3 targetPoint, float timeMultiplier, float timeBase)
    {
        Vector3 distance = targetPoint - startPoint;
        Vector3 distanceXZ = new Vector3(distance.x, 0, distance.z);

        float timeToTarget = (distanceXZ.magnitude / timeMultiplier) + timeBase;
        float vxz = distanceXZ.magnitude / timeToTarget;
        float vy = (distance.y + 0.5f * Mathf.Abs(Physics.gravity.y) * timeToTarget * timeToTarget) / timeToTarget;

        Vector3 result = distanceXZ.normalized * vxz;
        result.y = vy;

        return result;
    }

    private IEnumerator spawnBulletAfter(float wait)
    {
        yield return new WaitForSeconds(wait);
        GameObject proj = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

        if (proj.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.velocity = CalculateVelocityForTime(firePoint.position, player.transform.position, fireTimeMultiplyer, fireTimeBase);
        }
        if (proj.TryGetComponent<Projectile>(out var projectile))
        {
            projectile.damage = damage * GameManager.Instance.currentDifficulty;
        }
    }
}
