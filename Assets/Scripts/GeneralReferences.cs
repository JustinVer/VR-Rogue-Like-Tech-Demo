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
            textBoxAvailable = new bool[textBoxes.Length];
            for (int i = 0; i < textBoxAvailable.Length; i++)
            {
                textBoxAvailable[i] = true;
            }
        }
    }

    public GameObject[] allPowerUps;
    [SerializeField] private GameObject[] textBoxes;
    private bool[] textBoxAvailable;

    public (GameObject, TextMeshProUGUI) getTextBox()
    {
        GameObject textBoxOpen = null;
        TextMeshProUGUI textBoxText = null;

        for (int i = 0; i < textBoxAvailable.Length; i++)
        {
            if (textBoxAvailable[i])
            {
                textBoxOpen = textBoxes[i];
                textBoxAvailable[i] = false;
                if (textBoxOpen != null)
                {
                    textBoxText = textBoxOpen.GetComponentInChildren<TextMeshProUGUI>();
                    if (textBoxText != null)
                    {
                        break;
                    }
                }
            }
        }

        return (textBoxOpen, textBoxText);
    }

    public void returnTextBox(GameObject textBox)
    {
        for (int i = 0; i < textBoxes.Length; i++)
        {
            if (textBoxes[i] = textBox)
            {
                textBoxAvailable[i] = true;
                textBox.SetActive(false);
            }
        }
    }
}
