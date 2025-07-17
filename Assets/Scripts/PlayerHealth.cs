using UnityEngine;

public class PlayerHealth : MonoBehaviour, Health
{
    public float baseHealth = 100f;
    public float maxHealth = 100f;
    private float currentHealth;

    private void Start()
    {
        PlayerUpgradeSystem.instance.playerHealth = this;
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount / PlayerUpgradeSystem.instance.damageReduction;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        //Debug.Log("Player has died.");
        currentHealth = maxHealth;
        // Add your death logic here (e.g., game over screen, respawn, etc.)
    }

    public float GetCurrentHealth()
    {
        return currentHealth;
    }

    public float GetMaxHealth()
    {
        return maxHealth;
    }
}