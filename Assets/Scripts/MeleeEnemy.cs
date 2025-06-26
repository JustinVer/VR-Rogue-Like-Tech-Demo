public class MeleeEnemy : BaseEnemy
{
    protected override void Attack()
    {
        // Basic melee damage logic
        if (player != null)
        {
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }
        }
    }
}