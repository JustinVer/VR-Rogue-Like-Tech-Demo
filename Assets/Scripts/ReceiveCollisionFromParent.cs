using UnityEngine;

public class ReceiveCollisionFromParent : MonoBehaviour
{
    [SerializeField] private SendCollisionToChild mySpecificChild;

    private void Start()
    {
        mySpecificChild.onCollisionEnter += HandleMySpecificChildCollision;
    }

    private void HandleMySpecificChildCollision(Collision col)
    {
        Debug.Log("Only this child collider collided!");
    }
}
