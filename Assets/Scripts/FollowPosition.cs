using UnityEngine;

public class FollowPosition : MonoBehaviour
{
    [SerializeField] private Transform follow;
    [SerializeField] private Vector3 offset;
    void Update()
    {
        this.transform.position = follow.position + offset;
    }
}
