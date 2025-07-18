using System.Collections;
using TMPro;
using UnityEngine;

public class DamagePopUp : MonoBehaviour
{
    private TextMeshProUGUI textMesh;
    private Color originalColor;
    private float originalFontSize;

    // Static creator to make it easy to call
    public static void Create(Vector3 position, int damageAmount, bool isCritical)
    {
        // Get a popup from the pool
        GameObject popupGO = GeneralReferences.Instance.GetPopupFromPool();
        if (popupGO == null) return; // Pool might be empty or not set up

        popupGO.transform.position = position;
        DamagePopUp popup = popupGO.GetComponent<DamagePopUp>();
        popup.Setup(damageAmount, isCritical);
    }

    private void Awake()
    {
        textMesh = GetComponent<TextMeshProUGUI>();
        // Store original values to reset them when reused from the pool
        originalColor = textMesh.color;
        originalFontSize = textMesh.fontSize;
    }

    public void Setup(int damageAmount, bool isCritical = false)
    {
        // Reset state before setting new values
        textMesh.color = originalColor;
        textMesh.fontSize = originalFontSize;
        textMesh.alpha = 1f;

        // Set text and apply critical hit effects if applicable
        textMesh.SetText(NumberFormatter.FormatNumber(damageAmount));
        if (isCritical)
        {
            textMesh.fontSize *= 1.5f;
            textMesh.color = Color.red;
        }

        // Start the animation coroutine
        StartCoroutine(AnimatePopup());
    }

    private IEnumerator AnimatePopup()
    {
        float moveYSpeed = 4f;
        float moveXSpeed = 1f; // Add a little horizontal drift
        float disappearSpeed = 3f;
        float lifetime = 0.6f;

        // Random horizontal direction
        float randomX = Random.Range(-1f, 1f);
        Vector3 moveVector = new Vector3(randomX, 1) * moveYSpeed;

        // --- Animation Loop ---
        float timer = lifetime;
        while (timer > 0)
        {
            transform.position += moveVector * Time.deltaTime;
            moveVector -= moveVector * 8f * Time.deltaTime; // Slow down movement
            timer -= Time.deltaTime;
            yield return null;
        }

        // --- Fade Out Loop ---
        Color textColor = textMesh.color;
        while (textColor.a > 0)
        {
            textColor.a -= disappearSpeed * Time.deltaTime;
            textMesh.color = textColor;
            yield return null;
        }

        // Return to the pool instead of destroying
        GeneralReferences.Instance.ReturnPopupToPool(gameObject);
    }

    // Billboard effect to always face the camera
    private void LateUpdate()
    {
        if (Camera.main != null)
        {
            transform.LookAt(Camera.main.transform);
            transform.Rotate(0, 180, 0);
        }
    }
}

// Helper class for formatting numbers (e.g., 12345 -> "12.3K")
// You can put this in its own file "NumberFormatter.cs"
public static class NumberFormatter
{
    public static string FormatNumber(int num)
    {
        if (num >= 1000000)
            return (num / 1000000f).ToString("0.#") + "M";
        if (num >= 1000)
            return (num / 1000f).ToString("0.#") + "K";

        return num.ToString();
    }
}