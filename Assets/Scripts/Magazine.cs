using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class Magazine : MonoBehaviour
{
    public int maxShots { private set; get; }
    private int shots = 15;
    [SerializeField] private Collider magCollider;
    [SerializeField] private Collider interactionCollider;
    [SerializeField] private float groundDisapearTime = 2f;
    private float groundTouchedTime = 0f;
    [SerializeField] private InteractionLayerMask defaultLayerMask;
    [SerializeField] private InteractionLayerMask magTypeLayerMask;
    [SerializeField] private XRGrabInteractable interactable;
    [SerializeField] private Rigidbody rb;
    public Magazinespawner magazinespawner { set; private get; }

    public enum AmmoType { Pistol, Rifle, Sniper, Shotgun }

    private void Start()
    {
        shots = Mathf.RoundToInt(shots * PlayerUpgradeSystem.instance.magazineSizePercentage);
    }

    private void Update()
    {
        if (groundTouchedTime > 0)
        {
            groundTouchedTime += Time.deltaTime;
            if (groundTouchedTime >= groundDisapearTime)
            {
                Destroy(this.gameObject);
            }
        }
    }
    public bool removeBullet()
    {
        if (shots > 0)
        {
            shots--;
            return true;
        }
        return false;
    }

    public void reload()
    {
        shots = maxShots;
    }


    public void turnOffColliders()
    {
        magCollider.enabled = false;
    }

    public void turnOnColliders()
    {
        magCollider.enabled = true;
    }

    public void turnOffInteraction()
    {
        interactionCollider.enabled = false;
    }

    public void turnOnInteraction()
    {
        interactionCollider.enabled = true;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == 3 || collision.gameObject.layer == 0)
        {
            groundTouchedTime += Time.deltaTime;
            if (groundTouchedTime >= groundDisapearTime)
            {
                Destroy(this.gameObject);
            }
        }
    }



    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.layer == 3 || collision.gameObject.layer == 0)
        {
            groundTouchedTime = 0f;
        }
    }

    public void droped()
    {
        if (magazinespawner != null)
        {
            magazinespawner.magGrabed();
            magazinespawner = null;
        }
        if (interactable != null && interactable.interactorsHovering.Count == 0)
        {
            interactable.interactionLayers = defaultLayerMask;
        }
    }

    public void grabed()
    {
        if (interactable != null)
        {
            interactable.interactionLayers = magTypeLayerMask;
        }
    }
}
