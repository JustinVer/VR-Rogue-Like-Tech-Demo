using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float damage = 10f;
    public float lifetime = 5f;


    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    public virtual void OnTriggerEnter(Collider other)
    {
        if (!other.isTrigger)
        {
            if (other.gameObject.TryGetComponent(out Health enemy))
            {
                enemy.TakeDamage(damage * GameManager.Instance.currentDifficulty);
            }
            Destroy(gameObject);
        }
    }


}
