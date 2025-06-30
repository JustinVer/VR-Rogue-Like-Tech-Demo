using UnityEngine;

public class FollowPosition : MonoBehaviour
{
    [SerializeField] private Transform follow;
    [SerializeField] private Vector3 offset;
    void Update()
    {
        this.transform.localPosition = follow.localPosition + new Vector3(follow.forward.x * offset.x, offset.y, follow.forward.z * offset.z);
    }
}
