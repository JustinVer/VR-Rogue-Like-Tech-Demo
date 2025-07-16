using UnityEngine;

public class UpgradePickup : MonoBehaviour
{
    public enum UpgradeType { Damage, FireRate, Special }

    public UpgradeType upgradeType;
    public float value = 5f;
    [SerializeField] private Vector3 offset = Vector3.up * 1.5f;
    [SerializeField] private string text;

    private void Start()
    {
        showText();
    }

    public void trigger()
    {
        if (PlayerUpgradeSystem.instance)
        {
            PlayerUpgradeSystem.instance.ApplyUpgrade(upgradeType, value);
            Destroy(gameObject);
        }
    }

    public void showText()
    {

        GeneralReferences.Instance.powerUpTextObject.transform.position = this.transform.position + offset;
        GeneralReferences.Instance.powerUpTextBox.text = text;
        GeneralReferences.Instance.powerUpTextObject.transform.rotation = Quaternion.identity;
        GeneralReferences.Instance.powerUpTextObject.gameObject.SetActive(true);
        Debug.Log("Text is shown");
    }

    public void hideText()
    {
        GeneralReferences.Instance.powerUpTextBox.text = "";
        GeneralReferences.Instance.powerUpTextObject.gameObject.SetActive(false);
    }
}
