using System;
using TMPro;
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
    GameObject powerUpTextObject = null;
    TextMeshProUGUI powerUpTextBox = null;


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
            Destroy(this.gameObject);
        }
    }

    public void showText()
    {
        (powerUpTextObject, powerUpTextBox) = GeneralReferences.Instance.getTextBox();
        powerUpTextObject.gameObject.SetActive(true);
        powerUpTextObject.transform.position = parent.transform.position + offset;
        powerUpTextBox.text = text;
        Debug.Log("Rotation = " + parent.transform.rotation + " " + Quaternion.Euler(0, 180, 0) + " " + parent.transform.rotation * Quaternion.Euler(0, 180, 0));
        powerUpTextObject.transform.rotation = parent.transform.rotation * Quaternion.Euler(0, 180, 0);
    }

    public void hideText()
    {
        powerUpTextBox.text = "";
        GeneralReferences.Instance.returnTextBox(powerUpTextObject);
    }
}
