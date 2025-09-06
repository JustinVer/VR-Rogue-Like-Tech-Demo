using System.Collections.Generic; // Required for Queue
using TMPro;
using UnityEngine;

public class GeneralReferences : MonoBehaviour
{
    public static GeneralReferences Instance { get; private set; }

    [Header("Power-ups")]
    public GameObject[] allPowerUps;

    [Header("UI Text Box Pool (Legacy)")]
    [SerializeField] private GameObject[] textBoxes;
    private bool[] textBoxAvailable;

    [Header("Damage Popup Pool")]
    [SerializeField] private GameObject damagePopupPrefab; // Assign your DamagePopup prefab here!
    [SerializeField] private int popupPoolSize = 20;
    [SerializeField] private Transform canvasTransform;
    private Queue<GameObject> damagePopupPool;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            InitializeTextBoxes();
            InitializePopupPool();
        }
    }

    #region Damage Popup Pooling
    private void InitializePopupPool()
    {
        damagePopupPool = new Queue<GameObject>();
        for (int i = 0; i < popupPoolSize; i++)
        {
            GameObject popup = Instantiate(damagePopupPrefab, canvasTransform);
            popup.SetActive(false);
            damagePopupPool.Enqueue(popup);
        }
    }

    public GameObject GetPopupFromPool()
    {
        if (damagePopupPool.Count > 0)
        {
            GameObject popup = damagePopupPool.Dequeue();
            popup.SetActive(true);
            return popup;
        }

        // Optional: If the pool is empty, create a new one on the fly.
        // This prevents errors but can cause performance spikes if the pool size is too small.
        Debug.LogWarning("Damage Popup Pool exhausted. Instantiating a new one.");
        GameObject newPopup = Instantiate(damagePopupPrefab, canvasTransform);
        return newPopup;
    }

    public void ReturnPopupToPool(GameObject popup)
    {
        popup.SetActive(false);
        popup.transform.SetParent(canvasTransform);
        damagePopupPool.Enqueue(popup);
    }
    #endregion


    #region Legacy Text Box System
    private void InitializeTextBoxes()
    {
        if (textBoxes == null) return;
        textBoxAvailable = new bool[textBoxes.Length];
        for (int i = 0; i < textBoxAvailable.Length; i++)
        {
            textBoxAvailable[i] = true;
        }
    }

    public (GameObject, TextMeshProUGUI) getTextBox()
    {
        GameObject textBoxOpen = null;
        TextMeshProUGUI textBoxText = null;

        for (int i = 0; i < textBoxAvailable.Length; i++)
        {
            if (textBoxAvailable[i])
            {
                Debug.Log("Text box at " + i + " is available");
                textBoxOpen = textBoxes[i];
                Debug.Log("Text box at " + i + " is " + textBoxes[i].name + " " + textBoxOpen.name);
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
        Debug.Log("text box name " + textBox.gameObject.name);
        textBox.SetActive(false);
        for (int i = 0; i < textBoxes.Length; i++)
        {
            if (textBoxes[i] == textBox)
            {
                Debug.Log("Set textbox to be available again " + textBoxAvailable[i] + " " + textBoxes[i].activeSelf + " " + textBoxes[i].name);
                textBoxAvailable[i] = true;
                textBoxes[i].SetActive(false);
                Debug.Log("Set textbox to be available 2 again " + textBoxAvailable[i] + " " + textBoxes[i].activeSelf + " " + textBoxes[i].name);
                break;
            }
        }
    }
    #endregion

    private void Update()
    {
        //Debug.Log(textBoxAvailable[0] + " " + textBoxes[0].name);
    }
}