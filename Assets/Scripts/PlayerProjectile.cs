using UnityEngine;

public class PlayerProjectile : Projectile
{
    public override void OnTriggerEnter(Collider other)
    {
        if (!other.isTrigger)
        {
            if (other.gameObject.TryGetComponent(out Health enemy))
            {
                if (Random.value < PlayerUpgradeSystem.instance.criticalChance)
                {
                    enemy.TakeDamage((damage + PlayerUpgradeSystem.instance.damageFlat) * PlayerUpgradeSystem.instance.damagePercentage * PlayerUpgradeSystem.instance.criticalDamageMultiplier);
                }
                else
                {
                    enemy.TakeDamage((damage + PlayerUpgradeSystem.instance.damageFlat) * PlayerUpgradeSystem.instance.damagePercentage);
                }
            }
            Destroy(gameObject);
        }
    }
}
