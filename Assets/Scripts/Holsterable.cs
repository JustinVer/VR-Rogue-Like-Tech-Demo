using System;
using UnityEngine;

public class Holsterable : MonoBehaviour
{
    [NonSerialized] public Holster holster;

    [SerializeField] private float groundDisapearTime = 2f;
    private float groundTouchedTime = 0f;

    [SerializeField] private bool hasCollider = false;

    private void Update()
    {
        if (groundTouchedTime > 0)
        {
            groundTouchedTime += Time.deltaTime;
            if (groundTouchedTime >= groundDisapearTime)
            {
                if (holster != null)
                {
                    holster.Reholster(this);
                }
            }
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (hasCollider && (collision.gameObject.layer == 3 || collision.gameObject.layer == 0))
        {
            groundTouchedTime += Time.deltaTime;
            if (groundTouchedTime >= groundDisapearTime)
            {
                Destroy(this.gameObject);
            }
        }
    }
    public void CollisionInvoke(Collision collision)
    {
        Debug.Log("Holster collision " + collision.gameObject.name);
        if (collision.gameObject.layer == 3 || collision.gameObject.layer == 0)
        {
            groundTouchedTime += Time.deltaTime;
            if (groundTouchedTime >= groundDisapearTime)
            {
                Destroy(this.gameObject);
            }
        }
    }
}
