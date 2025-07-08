using UnityEngine;

public class SendCollisionToChild : MonoBehaviour
{
    public System.Action<Collision> onCollisionEnter;

    private void OnCollisionEnter(Collision collision)
    {
        onCollisionEnter?.Invoke(collision);
    }
}
