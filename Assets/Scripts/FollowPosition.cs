using UnityEngine;

public class FollowPosition : MonoBehaviour
{
    [SerializeField] private Transform follow;
    [SerializeField] private Vector3 offset;
    void Update()
    {
        this.transform.localPosition = follow.localPosition + new Vector3(follow.forward.x * offset.x, offset.y, follow.forward.z * offset.z);
        Debug.Log("local rotation " + transform.localRotation.eulerAngles + " " + this.gameObject.name);
        Debug.Log("global rotation " + transform.rotation.eulerAngles + " " + this.gameObject.name);
    }
}
