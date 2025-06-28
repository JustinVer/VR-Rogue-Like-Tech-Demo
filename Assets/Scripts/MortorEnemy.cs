using UnityEngine;

public class MortorEnemy : BaseEnemy
{
    [Header("Ranged Settings")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float fireTime = 2f;

    protected override void Attack()
    {

        Debug.Log("ranged shoot");
        if (projectilePrefab != null && firePoint != null)
        {
            GameObject proj = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

            if (proj.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.velocity = CalculateVelocityForTime(firePoint.position, player.transform.position, fireTime);
            }
        }
    }

    public static Vector3 CalculateVelocityForTime(Vector3 startPoint, Vector3 targetPoint, float timeToTarget)
    {
        Vector3 distance = targetPoint - startPoint;
        Vector3 distanceXZ = new Vector3(distance.x, 0, distance.z);

        float vxz = distanceXZ.magnitude / timeToTarget;
        float vy = (distance.y + 0.5f * Mathf.Abs(Physics.gravity.y) * timeToTarget * timeToTarget) / timeToTarget;

        Vector3 result = distanceXZ.normalized * vxz;
        result.y = vy;

        return result;
    }
}
