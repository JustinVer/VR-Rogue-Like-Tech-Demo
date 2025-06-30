using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
public class GunSliderWithSpring : MonoBehaviour
{
    [Header("Slide Track (Local Positions)")]
    public Transform trackRoot; // Usually the gun transform
    public Vector3 localTrackStart = Vector3.zero;
    public Vector3 localTrackEnd = new Vector3(0f, 0f, -0.1f); // Default slide back 10cm

    [Header("Slide Settings")]
    public float slideThreshold = 0.95f;
    public float returnSpeed = 5f;

    private XRBaseInteractor interactor = null;
    private bool isGrabbed = false;
    private float slideAmount = 0f;

    public bool IsLoaded { get; private set; }

    void Start()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true; // We control the position manually now
        }
    }

    void OnEnable()
    {
        var interactable = GetComponent<XRGrabInteractable>();
        interactable.selectEntered.AddListener(OnGrab);
        interactable.selectExited.AddListener(OnRelease);
    }

    void OnDisable()
    {
        var interactable = GetComponent<XRGrabInteractable>();
        interactable.selectEntered.RemoveListener(OnGrab);
        interactable.selectExited.RemoveListener(OnRelease);
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        interactor = args.interactorObject.transform.GetComponent<XRBaseInteractor>();
        isGrabbed = true;
    }

    void OnRelease(SelectExitEventArgs args)
    {
        interactor = null;
        isGrabbed = false;
    }

    void Update()
    {
        if (trackRoot == null) return;

        Vector3 worldStart = trackRoot.TransformPoint(localTrackStart);
        Vector3 worldEnd = trackRoot.TransformPoint(localTrackEnd);
        Vector3 slideDir = (worldEnd - worldStart).normalized;
        float trackLength = Vector3.Distance(worldStart, worldEnd);

        if (isGrabbed && interactor != null)
        {
            Vector3 handPos = interactor.transform.position;
            Vector3 toHand = handPos - worldStart;

            float proj = Vector3.Dot(toHand, slideDir);
            float clamped = Mathf.Clamp(proj, 0f, trackLength);
            Vector3 targetPos = worldStart + slideDir * clamped;

            transform.position = targetPos;
            transform.rotation = Quaternion.LookRotation(trackRoot.forward, trackRoot.up);

            slideAmount = clamped / trackLength;
        }
        else
        {
            // Return to start
            slideAmount = Mathf.MoveTowards(slideAmount, 0f, returnSpeed * Time.deltaTime);
            Vector3 targetPos = Vector3.Lerp(worldStart, worldEnd, slideAmount);

            transform.position = targetPos;
            transform.rotation = Quaternion.LookRotation(trackRoot.forward, trackRoot.up);
        }

        IsLoaded = slideAmount >= slideThreshold;
    }

    public float GetSlideAmount01()
    {
        return slideAmount;
    }
}
