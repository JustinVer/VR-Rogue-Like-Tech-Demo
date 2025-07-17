using System;
using UnityEngine;

public class UpgradePickup : MonoBehaviour
{
    [Serializable]
    public enum UpgradeType
    {
        DamageFlat,
        DamagePercentage,
        FireRate,
        MovementSpeed,
        JumpHeight,
        ProjectileSize,
        ProjectileSpeed,
        MultiShot,
        CritChance,
        CritDamageMultiplier,
        HealthBonusFlat,
        HealthBonusPercentage,
        DamageReduction,
        MagazineSizeBonus
    }

    public UpgradeType upgradeType;
    public float value = 5f;
    [SerializeField] private Vector3 offset = Vector3.up;
    [SerializeField] private string text;
    public GameObject parent;

    [SerializeField] private float rotationSpeed = 20f;
    [SerializeField] private float bobHeight = 0.1f;
    [SerializeField] private float bobSpeed = 1f;

    private Vector3 startPosition;

    private void Start()
    {
        startPosition = transform.position;
    }

    private void Update()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        transform.position = startPosition + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
    }

    public void trigger()
    {
        if (PlayerUpgradeSystem.instance)
        {
            PlayerUpgradeSystem.instance.ApplyUpgrade(upgradeType, value);
            hideText();
            try
            {
                Chest chest = parent.GetComponent<Chest>();
                chest.destroyPickups();
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
        }
    }

    public void showText()
    {

        GeneralReferences.Instance.powerUpTextObject.transform.position = this.transform.position + offset;
        GeneralReferences.Instance.powerUpTextBox.text = text;
        GeneralReferences.Instance.powerUpTextObject.transform.rotation = this.transform.rotation * Quaternion.Euler(0, 180, 0);
        GeneralReferences.Instance.powerUpTextObject.gameObject.SetActive(true);
        Debug.Log("Text is shown");
    }

    public void hideText()
    {
        GeneralReferences.Instance.powerUpTextBox.text = "";
        GeneralReferences.Instance.powerUpTextObject.gameObject.SetActive(false);
    }
}
