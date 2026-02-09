using UnityEngine;

public class MeleeHitbox : MonoBehaviour
{
    private Vector2 strikeDirection;
    private PlayerCombat player;
    private int batDamage = 20; // Daño base del bate

    public void Setup(Vector2 dir, PlayerCombat playerRef)
    {
        strikeDirection = dir;
        player = playerRef;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void OnTriggerEnter(Collider other)
    {
        // GOLPEAR BOLA
        if (other.CompareTag("Ball"))
        {
            BallProjectile ball = other.GetComponent<BallProjectile>();
            if (ball != null)
            {
                ball.GetHitByBat(strikeDirection);
                if (strikeDirection.y < -0.1f) player.DoPogo();
            }
        }

        // GOLPEAR ENEMIGO
        if (other.CompareTag("Enemy"))
        {
            GroundEnemy enemy = other.GetComponent<GroundEnemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(batDamage);

                // Empujón simple
                Rigidbody enemyRb = enemy.GetComponent<Rigidbody>();
                if (enemyRb != null)
                {
                    enemyRb.AddForce(strikeDirection * 5f, ForceMode.Impulse);
                }
            }
        }
    }
}