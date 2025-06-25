using UnityEngine;

public class PlayerUpgradeSystem : MonoBehaviour
{
    public float damageBonus = 0f;
    public float damageModifier = 1f;
    public float fireRateModifier = 1f;

    static public PlayerUpgradeSystem instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
    public void ApplyUpgrade(UpgradePickup.UpgradeType type, float value)
    {
        switch (type)
        {
            case UpgradePickup.UpgradeType.Damage:
                damageBonus += value;
                break;
            case UpgradePickup.UpgradeType.FireRate:
                fireRateModifier += value;
                break;
            case UpgradePickup.UpgradeType.Special:
                // Unlock new ability here
                break;
        }

        Debug.Log($"Upgrade applied: {type} +{value}");
    }
}
