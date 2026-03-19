using UnityEngine;

public class MeleeHitbox : MonoBehaviour
{
    private Vector2 strikeDirection;
    private PlayerCombat player;
    [SerializeField] private int batDamage = 20;

    [Header("VFX")]
    [SerializeField] private GameObject hitParticlesPrefab; // ASIGNAR PREFAB CHISPAS

    public void Setup(Vector2 dir, PlayerCombat playerRef)
    {
        strikeDirection = dir;
        player = playerRef;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Buscamos el sistema de cámara en la escena
        CameraSystem cam = FindFirstObjectByType<CameraSystem>();

        if (other.CompareTag("Ball"))
        {
            BallProjectile ball = other.GetComponent<BallProjectile>();
            if (ball != null)
            {
                SpawnHitParticles(other.transform.position);
                ball.GetHitByBat(strikeDirection);

                // Lógica de Shake para la Pelota
                if (cam != null)
                {
                    if (strikeDirection.y < -0.1f)
                        cam.ShakePogo(); // Shake fuerte si es pogo
                    else
                        cam.ShakeDash(); // Shake ligero si es batazo normal
                }

                if (strikeDirection.y < -0.1f) player.DoPogo();
                return;
            }
        }

        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable != null)
        {
            SpawnHitParticles(other.transform.position);
            damageable.TakeDamage(batDamage, player.transform.position);

            // Lógica de Shake para Enemigos/Objetos
            if (cam != null)
            {
                if (strikeDirection.y < -0.1f)
                    cam.ShakePogo();
                else
                    cam.ShakeDash();
            }

            if (strikeDirection.y < -0.1f) player.DoPogo();

            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb != null) rb.AddForce(strikeDirection * 5f, ForceMode.Impulse);
        }
    }

    private void SpawnHitParticles(Vector3 pos)
    {
        if (hitParticlesPrefab != null)
        {
            GameObject fx = Instantiate(hitParticlesPrefab, pos, Quaternion.identity);
            Destroy(fx, 1f);
        }
    }
}