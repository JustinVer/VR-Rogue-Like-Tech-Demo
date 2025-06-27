using UnityEngine;

public class FollowRotation : MonoBehaviour
{
    [SerializeField] private Transform follow;
    [SerializeField] private Vector3 offset;
    [SerializeField] private bool followX = false;
    [SerializeField] private bool followY = false;
    [SerializeField] private bool followZ = false;
    void LateUpdate()
    {
        if (follow == null)
            return;

        // Get target rotation in Euler angles
        Vector3 targetEuler = follow.rotation.eulerAngles;
        Vector3 currentEuler = transform.rotation.eulerAngles;

        // Apply selected axes
        if (!followX) targetEuler.x = currentEuler.x;
        if (!followY) targetEuler.y = currentEuler.y;
        if (!followZ) targetEuler.z = currentEuler.z;

        // Apply offset and set rotation
        Quaternion finalRotation = Quaternion.Euler(targetEuler + offset);
        transform.rotation = finalRotation;
    }
}
