using UnityEngine;

public class PlayerHealth : MonoBehaviour, Health
{
    public float baseHealth = 100f;
    public float maxHealth = 100f;

    private void Start()
    {
        PlayerUpgradeSystem.instance.playerHealth = this;
        GameManager.Instance.playerHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        GameManager.Instance.playerHealth -= amount / PlayerUpgradeSystem.instance.damageReduction;

        if (GameManager.Instance.playerHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        //Debug.Log("Player has died.");
        GameManager.Instance.playerHealth = maxHealth;
        // Add your death logic here (e.g., game over screen, respawn, etc.)
    }

    public float GetCurrentHealth()
    {
        return GameManager.Instance.playerHealth;
    }

    public float GetMaxHealth()
    {
        return maxHealth;
    }
}