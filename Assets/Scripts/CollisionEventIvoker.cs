using UnityEngine;
using UnityEngine.Events;

public class CollisionEventIvoker : MonoBehaviour
{
    [System.Serializable]
    public class TriggerEvent : UnityEvent<Collider> { }

    [System.Serializable]
    public class CollisionEvent : UnityEvent<Collision> { }

    [Header("Trigger Events")]
    public bool useTrigger = true;
    public TriggerEvent onTriggerEnter;

    [Header("Collision Events")]
    public bool useCollision = false;
    public CollisionEvent onCollisionEnter;

    void OnTriggerEnter(Collider other)
    {
        if (useTrigger)
        {
            onTriggerEnter.Invoke(other);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("event ivoker collision");
        if (useCollision)
        {
            onCollisionEnter.Invoke(collision);
        }
    }

}
