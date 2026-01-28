using UnityEngine;

public class MeleeHitbox : MonoBehaviour
{
    private Vector2 strikeDirection;
    private PlayerCombat player;

    public void Setup(Vector2 dir, PlayerCombat playerRef)
    {
        strikeDirection = dir;
        player = playerRef;

        // Rotar el hitbox visualmente para que coincida con el golpe 
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Si golpeamos la bola
        if (other.CompareTag("Ball"))
        {
            BallProjectile ball = other.GetComponent<BallProjectile>();
            if (ball != null)
            {
                // redirige bola 
                ball.GetHitByBat(strikeDirection);

                // Si golpeamos hacia abajo (Pogo)
                if (strikeDirection.y < -0.1f)
                {
                    player.DoPogo();
                }
            }
        }

        // Si golpeamos Enemigo
        if (other.CompareTag("Enemy"))
        {
            // por ahora nada
            
        }
    }
}