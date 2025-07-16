using TMPro;
using UnityEngine;

public class GeneralReferences : MonoBehaviour
{
    public static GeneralReferences Instance { get; private set; }

    private void Awake()
    {
        // A simple singleton pattern to make the instance easily accessible.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public GameObject[] allPowerUps;
    public GameObject powerUpTextObject;
    public TextMeshProUGUI powerUpTextBox;
}
