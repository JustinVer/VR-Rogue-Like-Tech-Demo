using UnityEngine;

public class RangedEnemy : BaseEnemy
{
    [Header("Ranged Settings")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float shootForce = 20f;

    protected override void Attack()
    {

        Debug.Log("ranged shoot");
        if (projectilePrefab != null && firePoint != null)
        {
            GameObject proj = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

            if (proj.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.velocity = firePoint.forward * shootForce;
            }
        }
    }
}