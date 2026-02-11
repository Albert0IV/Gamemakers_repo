using UnityEngine;

public class MeleeHitbox : MonoBehaviour
{
    private Vector2 strikeDirection;
    private PlayerCombat player;
    [SerializeField] private int batDamage = 20; // Daño estándar

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
                enemy.TakeDamage(batDamage, transform.position);
                Rigidbody enemyRb = enemy.GetComponent<Rigidbody>();
                if (enemyRb != null) enemyRb.AddForce(strikeDirection * 5f, ForceMode.Impulse);
            }
        }

        // GOLPEAR MURO (NUEVO: Pasa el daño normal)
        BreakableObject breakable = other.GetComponent<BreakableObject>();
        if (breakable != null)
        {
            breakable.HitObject(batDamage, player.transform);

            // Si golpeamos hacia abajo contra un suelo rompible, hacemos pogo
            if (strikeDirection.y < -0.1f) player.DoPogo();
        }
    }
}