using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float damage = 10f;
    public float lifetime = 5f;


    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.isTrigger)
        {
            if (other.gameObject.TryGetComponent(out Health enemy))
            {
                enemy.TakeDamage((damage + PlayerUpgradeSystem.instance.damageBonus) * PlayerUpgradeSystem.instance.damageModifier);
            }
            Destroy(gameObject);
        }
    }


}
