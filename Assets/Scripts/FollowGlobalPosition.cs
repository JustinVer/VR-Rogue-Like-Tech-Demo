using UnityEngine;

public class FollowGlobalPosition : MonoBehaviour
{
    [SerializeField] private Transform follow;
    [SerializeField] private Vector3 offset;
    [SerializeField] private float distance;

    void Update()
    {
        Vector3 forward = follow.forward;
        forward.y = 0;
        forward = forward.normalized;
        this.transform.position = offset + follow.position + (forward * distance);
    }
}
