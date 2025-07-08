using System;
using UnityEngine;

public class Holsterable : MonoBehaviour
{
    [NonSerialized] public Holster holster;

    [SerializeField] private float groundDisapearTime = 2f;
    private float groundTouchedTime = 0f;

    [SerializeField] private bool hasCollider = false;

    [SerializeField] private GameObject topObject;

    private void Update()
    {
        if (groundTouchedTime > 0)
        {
            groundTouchedTime += Time.deltaTime;
            if (groundTouchedTime >= groundDisapearTime)
            {
                if (holster != null)
                {
                    resetGroundTimer();
                    holster.ReholsterObject(topObject);
                }
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
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
        groundTouchedTime = 0;
    }

    public void resetGroundTimer()
    {
        groundTouchedTime = 0;
    }

}
