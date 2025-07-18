using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;

public class ToggleMovementVingiette : MonoBehaviour
{
    [Header("Vignette Settings")]
    [Tooltip("The Global Volume in your scene with the Vignette override.")]
    public Volume postProcessVolume;

    [Tooltip("How strong the vignette is at its peak.")]
    [Range(0f, 1f)]
    public float maxIntensity = 0.971f;

    [Tooltip("Approximate time in seconds for the vignette to fade in. Lower is faster.")]
    public float fadeInTime = 0.1f; // Changed from Speed to Time

    [Tooltip("Approximate time in seconds for the vignette to fade out. Lower is faster.")]
    public float fadeOutTime = 0.3f; // Changed from Speed to Time

    [Tooltip("How long the vignette stays at full intensity after a snap turn.")]
    public float snapTurnPulseDuration = 0.1f;


    [Header("Locomotion References")]
    [Tooltip("The Input Action for continuous movement (e.g., XRI LeftHand Locomotion/Move).")]
    public InputActionProperty moveAction;

    [Tooltip("The Input Action for continuous turning (e.g., XRI RightHand Locomotion/Turn).")]
    public InputActionProperty turnAction;

    [Tooltip("Reference to the Snap Turn Provider for event-based turning.")]
    public SnapTurnProvider snapTurnProvider;


    private Vignette vignette;
    private float currentTargetIntensity = 0f;
    private Coroutine snapTurnPulseCoroutine;

    // This variable is required for SmoothDamp to work
    private float vignetteVelocity;

    private void Awake()
    {
        // Safely get the Vignette component from the Volume's profile
        if (postProcessVolume != null && postProcessVolume.profile.TryGet(out vignette))
        {
            // Success! Initialize the intensity to 0.
            vignette.intensity.value = 0f;
        }
        else
        {
            Debug.LogError("Vignette or Post Process Volume is not set up correctly!", this);
            this.enabled = false; // Disable script if not set up
        }
    }

    private void OnEnable()
    {
        // Subscribe to the snap turn event if the provider is assigned
        if (snapTurnProvider != null)
        {
            snapTurnProvider.locomotionStarted += OnSnapTurn;
        }
    }

    private void OnDisable()
    {
        // Always unsubscribe to prevent errors
        if (snapTurnProvider != null)
        {
            snapTurnProvider.locomotionStarted -= OnSnapTurn;
        }
        // Also ensure vignette is off when this object is disabled
        if (vignette != null)
        {
            vignette.intensity.value = 0f;
        }
    }

    void Update()
    {
        Vector2 moveInput = moveAction.action?.ReadValue<Vector2>() ?? Vector2.zero;
        Vector2 turnInput = turnAction.action?.ReadValue<Vector2>() ?? Vector2.zero;

        if (moveInput.magnitude > 0.1f || Mathf.Abs(turnInput.x) > 0.1f)
        {
            currentTargetIntensity = maxIntensity;
        }
        else
        {
            if (snapTurnPulseCoroutine == null)
            {
                currentTargetIntensity = 0f;
            }
        }

        // --- NEW FADE LOGIC USING SMOOTH DAMP ---
        // Determine the fade time based on whether we are fading in or out
        float smoothTime = (currentTargetIntensity > vignette.intensity.value) ? fadeInTime : fadeOutTime;

        // Use SmoothDamp for a much cleaner and frame-rate independent animation
        vignette.intensity.value = Mathf.SmoothDamp(
            vignette.intensity.value,
            currentTargetIntensity,
            ref vignetteVelocity,
            smoothTime
        );
    }

    private void OnSnapTurn(LocomotionProvider args)
    {
        // If a pulse is already running, stop it before starting a new one
        if (snapTurnPulseCoroutine != null)
        {
            StopCoroutine(snapTurnPulseCoroutine);
        }
        snapTurnPulseCoroutine = StartCoroutine(PulseVignette());
    }

    private IEnumerator PulseVignette()
    {
        // Immediately set the target intensity to max
        currentTargetIntensity = maxIntensity;

        // Wait for the pulse duration
        yield return new WaitForSeconds(snapTurnPulseDuration);

        // After the pulse, allow the vignette to start fading out
        snapTurnPulseCoroutine = null;
    }

}
