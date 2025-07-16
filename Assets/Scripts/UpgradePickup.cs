using UnityEngine;

public class UpgradePickup : MonoBehaviour
{
    public enum UpgradeType { Damage, FireRate, Special }

    public UpgradeType upgradeType;
    public float value = 5f;
    [SerializeField] private Vector3 offset = Vector3.up;
    [SerializeField] private string text;
    public GameObject parent;

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
