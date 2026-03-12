using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class StatsPanelController : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The Stats Panel UI GameObject to be shown/hidden.")]
    public GameObject statsPanel;

    [Header("XR Rig References")]
    [Tooltip("The controller transform that the UI will be anchored to (e.g., LeftHand Controller).")]
    public Transform anchorController;

    [Tooltip("The main camera transform, used to make the UI face the player.")]
    public Transform cameraTransform;

    [Header("Input Action")]
    [Tooltip("The input action for holding the button (e.g., XRI LeftHand Interaction/Primary Button).")]
    public InputActionProperty showPanelAction;

    [Header("Positioning Settings")]
    [Tooltip("An additional offset to apply to the panel's position.")]
    public Vector3 positionOffset = new Vector3(0, 0, 0.1f); // Push it slightly forward

    private RectTransform panelRectTransform;

    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI roomText;

    private void Awake()
    {
        // Get the RectTransform for positioning calculations
        if (statsPanel != null)
        {
            panelRectTransform = statsPanel.GetComponent<RectTransform>();
        }

        // Ensure the panel is hidden at the start
        if (statsPanel != null)
        {
            statsPanel.SetActive(false);
        }
    }

    private void OnEnable()
    {
        // Subscribe to the button press and release events
        showPanelAction.action.performed += OnShowPanel;
        showPanelAction.action.canceled += OnHidePanel;
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent errors
        showPanelAction.action.performed -= OnShowPanel;
        showPanelAction.action.canceled -= OnHidePanel;

        // Also ensure the panel is hidden if this script is disabled
        if (statsPanel != null)
        {
            statsPanel.SetActive(false);
        }
    }

    private void OnShowPanel(InputAction.CallbackContext context)
    {
        Debug.Log("ShowPanel");
        if (statsPanel != null)
        {
            statsPanel.SetActive(true);
            // We call LateUpdate once immediately to prevent a 1-frame lag in positioning
            LateUpdate();
        }
    }

    private void OnHidePanel(InputAction.CallbackContext context)
    {
        if (statsPanel != null)
        {
            statsPanel.SetActive(false);
        }
    }

    // Use LateUpdate to position the UI to prevent jitter, as it runs after controller tracking updates
    private void LateUpdate()
    {
        // Only run the logic if the panel is active
        if (statsPanel == null || !statsPanel.activeSelf)
        {
            return;
        }
        statsPanel.transform.position = anchorController.position + new Vector3(0, (panelRectTransform.rect.height / 2f / 100f) + 0.02f, 0);

        // --- Billboarding Logic (Face the Player)
        Vector3 lookAtPosition = new Vector3(cameraTransform.position.x, cameraTransform.position.y, cameraTransform.position.z);
        statsPanel.transform.LookAt(lookAtPosition);
        statsPanel.transform.rotation *= Quaternion.Euler(0, 180, 0);

        //Update stats
        healthText.text = "Health: " + GameManager.Instance.playerHealth;
        roomText.text = "Room: " + ((int)((GameManager.Instance.currentDifficulty - 1) / GameManager.Instance.difficultyIncrease) + 1);
    }
}