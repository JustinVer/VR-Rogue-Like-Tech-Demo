using UnityEngine;
using UnityEngine.Events;

public class SpecificColliderTrigger : MonoBehaviour
{
    [Header("Set this to the specific collider you want to respond to")]
    public Collider targetCollider;

    [Header("Event to call when the specific collider collides")]
    public UnityEvent<Collision> onSpecificCollision;
    [Header("Event to call when the specific collider exits")]
    public UnityEvent<Collision> onSpecificExit;

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("collision Entered");
        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.thisCollider == targetCollider)
            {
                Debug.Log("Specific collider found");
                onSpecificCollision?.Invoke(collision);
                break; // Exit early — we found the correct collider
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        Debug.Log("collision exited");
        foreach (ContactPoint contact in collision.contacts)
        {
            Debug.Log(contact.thisCollider + " " + contact.otherCollider + " " + targetCollider);
            if (contact.thisCollider == targetCollider)
            {
                Debug.Log("Specific exit found");
                onSpecificExit?.Invoke(collision);
                break; // Exit early — we found the correct collider
            }
        }
    }
}
