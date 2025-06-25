using UnityEngine;

public class ToggleColliders : MonoBehaviour
{
    [SerializeField] private Collider[] colliders;

    private void turnOnColliders()
    {
        foreach (Collider collider in colliders)
        {
            collider.enabled = true;
        }
    }

    private void turnOffColliders()
    {
        foreach (Collider collider in colliders)
        {
            collider.enabled = false;
        }
    }
}
