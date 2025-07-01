using UnityEngine;

public class ExplodeOnHit : MonoBehaviour
{
    [SerializeField] private float explosionRadius = 2f;
    [SerializeField] private float explosionDamage = 5f;
    private void OnTriggerEnter(Collider other)
    {
        if (!other.isTrigger)
        {
            RaycastHit[] hits = Physics.SphereCastAll(this.transform.position, explosionRadius, this.transform.forward, explosionRadius);
            foreach (RaycastHit hit in hits)
            {
                if (hit.transform.gameObject.TryGetComponent(out Health enemy))
                {
                    enemy.TakeDamage(explosionDamage);
                }
            }
        }
    }
}
