using UnityEngine;

public class DPSZone : MonoBehaviour
{
    public float DPS = 5f;
    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.TryGetComponent(out Health enemy))
        {
            enemy.TakeDamage((DPS / Time.deltaTime + PlayerUpgradeSystem.instance.damageFlat) * PlayerUpgradeSystem.instance.damagePercentage);
        }
    }
}
