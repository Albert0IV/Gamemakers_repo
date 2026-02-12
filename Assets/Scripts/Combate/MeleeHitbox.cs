using UnityEngine;

public class MeleeHitbox : MonoBehaviour
{
    private Vector2 strikeDirection;
    private PlayerCombat player;
    [SerializeField] private int batDamage = 20; // Daño estándar del bate

    public void Setup(Vector2 dir, PlayerCombat playerRef)
    {
        strikeDirection = dir;
        player = playerRef;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void OnTriggerEnter(Collider other)
    {
        // GOLPEAR BOLA (Caso especial por su lógica de dirección y pogo)
        if (other.CompareTag("Ball"))
        {
            BallProjectile ball = other.GetComponent<BallProjectile>();
            if (ball != null)
            {
                ball.GetHitByBat(strikeDirection);
                if (strikeDirection.y < -0.1f) player.DoPogo();
                return;
            }
        }

        // SISTEMA UNIVERSAL: Enemigos, Palancas, Objetos Rompibles
        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable != null)
        {
            // Aplicamos el daño del bate
            damageable.TakeDamage(batDamage, player.transform.position);

            // Si golpeamos hacia abajo contra algo dañable, hacemos pogo
            

            // Si tiene físicas, aplicamos un empuje extra
            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb != null) rb.AddForce(strikeDirection * 5f, ForceMode.Impulse);
        }
    }
}