using UnityEngine;

public class ObjectHealth : MonoBehaviour, Health
{
    [SerializeField] private float currentHealth = 10f;
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        if (currentHealth < 0)
        {
            Destroy(gameObject);
        }
    }
}
