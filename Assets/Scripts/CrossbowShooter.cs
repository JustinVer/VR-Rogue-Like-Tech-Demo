using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class CrossbowShooter : MonoBehaviour
{
    public GameObject projectilePrefab;
    public Transform muzzlePoint;
    public float shootForce = 20f;
    public InputActionReference fireAction;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
    private bool isHeld = false;
    private float timeSinceShot = 0f;
    public float fireRateDelay = 0.2f;
    private Magazine currentMag;

    [SerializeField] private Collider[] gunCollider;
    [SerializeField] private XRGrabInteractable sliderInteractable;
    [SerializeField] private Collider magCollider;
    [SerializeField] public Magazine.AmmoType ammoType { private set; get; }

    private bool hasShot = false;
    [SerializeField] private Animator animator;

    void Awake()
    {
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
    }

    void OnEnable()
    {
        fireAction?.action.Enable();
        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnRelease);
    }

    void OnDisable()
    {
        fireAction?.action.Disable();
        grabInteractable.selectEntered.RemoveListener(OnGrab);
        grabInteractable.selectExited.RemoveListener(OnRelease);
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        isHeld = true;
        foreach (Collider c in gunCollider)
        {
            c.enabled = false;
        }
        sliderInteractable.enabled = true;
        magCollider.enabled = true;
        if (currentMag != null)
        {
            currentMag.turnOnColliders();
            currentMag.turnOnInteraction();
        }

    }

    void OnRelease(SelectExitEventArgs args)
    {
        isHeld = false;
        foreach (Collider c in gunCollider)
        {
            c.enabled = true;
        }
        sliderInteractable.enabled = false;
        magCollider.enabled = false;
        if (currentMag != null)
        {
            currentMag.turnOffColliders();
            currentMag.turnOffInteraction();
        }
    }

    void Update()
    {
        timeSinceShot += Time.deltaTime;
    }

    public void Shoot()
    {
        if ((fireRateDelay / PlayerUpgradeSystem.instance.fireRateModifier) <= timeSinceShot && hasShot)
        {
            GameObject proj = Instantiate(projectilePrefab, muzzlePoint.position, muzzlePoint.rotation);
            if (proj.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.velocity = muzzlePoint.forward * shootForce;
            }
            timeSinceShot = 0f;
            if (currentMag != null && currentMag.removeBullet())
            {
                hasShot = true;
            }
            else
            {
                hasShot = false;
            }
            animator.SetTrigger("Fire");
        }
    }

    public void addMag(XRSocketInteractor interactor)
    {
        try
        {
            currentMag = interactor.firstInteractableSelected.transform.gameObject.GetComponent<Magazine>();
            currentMag.turnOffColliders();
        }
        catch (Exception)
        {
        }
    }

    public void magRemoved()
    {
        currentMag.turnOnColliders();
        currentMag = null;
    }

    public void gunEquiped()
    {
        Magazinespawner.instance.isHoldingGun++;
    }

    public void gunDroped()
    {
        Magazinespawner.instance.isHoldingGun--;
    }

    public void sliderPulled()
    {
        if (currentMag != null && currentMag.removeBullet())
        {
            hasShot = true;
        }
        else
        {
            hasShot = false;
        }
    }
}
