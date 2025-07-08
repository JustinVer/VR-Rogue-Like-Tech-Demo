using System;
using UnityEngine;

public class Holsterable : MonoBehaviour
{
    [NonSerialized] public Holster holster;

    [SerializeField] private float groundDisapearTime = 2f;
    private float groundTouchedTime = 0f;

    [SerializeField] private bool hasCollider = false;
    public Transform grabPosition;

    protected virtual void Update()
    {
        if (groundTouchedTime > 0)
        {
            groundTouchedTime += Time.deltaTime;
            if (groundTouchedTime >= groundDisapearTime)
            {
                if (holster != null)
                {
                    resetGroundTimer();
                    holster.Reholster(this);
                }
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasCollider)
        {
            CollisionInvoke(collision);
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (hasCollider)
        {
            CollisionExitInvoke(collision);
        }
    }

    public void CollisionInvoke(Collision collision)
    {
        if (collision.gameObject.layer == 3 || collision.gameObject.layer == 0)
        {
            groundTouchedTime = 0;
            groundTouchedTime += Time.deltaTime;
            if (groundTouchedTime >= groundDisapearTime)
            {
                Destroy(this.gameObject);
            }
        }
    }

    public void CollisionExitInvoke(Collision collision)
    {
        resetGroundTimer();
    }

    public void resetGroundTimer()
    {
        groundTouchedTime = 0;
    }

}
