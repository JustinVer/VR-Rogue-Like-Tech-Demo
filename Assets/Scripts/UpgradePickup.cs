using UnityEngine;

public class UpgradePickup : MonoBehaviour
{
    public enum UpgradeType { Damage, FireRate, Special }

    public UpgradeType upgradeType;
    public float value = 5f;

    public void trigger()
    {
        if (PlayerUpgradeSystem.instance)
        {
            PlayerUpgradeSystem.instance.ApplyUpgrade(upgradeType, value);
            Destroy(gameObject);
        }
    }
}
