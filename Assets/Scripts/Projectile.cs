using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float damage = 10f;
    public float lifetime = 5f;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter(UnityEngine.Collider other)
    {
        if (other.gameObject.TryGetComponent(out Enemy enemy))
        {
            enemy.TakeDamage((damage + PlayerUpgradeSystem.instance.damageBonus) * PlayerUpgradeSystem.instance.damageModifier);
        }
        Destroy(gameObject);
    }
}
