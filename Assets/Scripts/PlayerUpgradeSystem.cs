using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Jump;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

public class PlayerUpgradeSystem : MonoBehaviour
{
    public float damageFlat = 0f;
    public float damagePercentage = 1f;
    public float fireRateModifier = 1f;
    public float projectileSpeedModifier = 1f;
    public float multiShot = 1f;
    public float projectileSizeModifier = 1f;
    public float movementSpeedModifier = 1f;
    public float jumpHeightModifier = 1f;
    public float criticalChance = 0.01f;
    public float criticalDamageMultiplier = 2f;
    public float healthBonusFlat = 0f;
    public float healthBonusPercentage = 1f;
    public float damageReduction = 0f;
    public float magazineSizePercentage = 1f;

    static public PlayerUpgradeSystem instance;
    public PlayerHealth playerHealth;
    [SerializeField] JumpProvider jumpProvider;
    private float baseJumpHeight;
    [SerializeField] DynamicMoveProvider moveProvider;
    private float baseMoveSpeed;


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
    private void Start()
    {
        baseJumpHeight = jumpProvider.jumpHeight;
        baseMoveSpeed = moveProvider.moveSpeed;
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
            case UpgradePickup.UpgradeType.DamageFlat:
                damageFlat += value;
                break;
            case UpgradePickup.UpgradeType.DamagePercentage:
                damagePercentage += value;
                break;
            case UpgradePickup.UpgradeType.FireRate:
                fireRateModifier += value;
                break;
            case UpgradePickup.UpgradeType.ProjectileSpeed:
                projectileSpeedModifier += value;
                break;
            case UpgradePickup.UpgradeType.MultiShot:
                multiShot += value;
                break;
            case UpgradePickup.UpgradeType.ProjectileSize:
                projectileSizeModifier += value;
                break;
            case UpgradePickup.UpgradeType.MovementSpeed:
                movementSpeedModifier += value;
                moveProvider.moveSpeed = baseMoveSpeed * movementSpeedModifier;
                break;
            case UpgradePickup.UpgradeType.JumpHeight:
                jumpHeightModifier += value;
                jumpProvider.jumpHeight = baseJumpHeight * jumpHeightModifier;
                break;
            case UpgradePickup.UpgradeType.CritChance:
                criticalChance += value;
                break;
            case UpgradePickup.UpgradeType.CritDamageMultiplier:
                criticalDamageMultiplier += value;
                break;
            case UpgradePickup.UpgradeType.HealthBonusFlat:
                healthBonusFlat += value;
                playerHealth.maxHealth = (playerHealth.baseHealth + healthBonusFlat) * healthBonusPercentage;
                break;
            case UpgradePickup.UpgradeType.HealthBonusPercentage:
                healthBonusPercentage += value;
                playerHealth.maxHealth = (playerHealth.baseHealth + healthBonusFlat) * healthBonusPercentage;
                break;
            case UpgradePickup.UpgradeType.DamageReduction:
                damageReduction += value;
                break;
            case UpgradePickup.UpgradeType.MagazineSizeBonus:
                magazineSizePercentage += value;
                break;
        }
    }
}