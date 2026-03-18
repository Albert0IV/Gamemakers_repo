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
        if (other.CompareTag("Ball"))
        {
            BallProjectile ball = other.GetComponent<BallProjectile>();
            if (ball != null)
            {
                SpawnHitParticles(other.transform.position); // VFX
                ball.GetHitByBat(strikeDirection);
                if (strikeDirection.y < -0.1f) player.DoPogo();
                return;
            }
        }

        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable != null)
        {
            SpawnHitParticles(other.transform.position); // VFX
            damageable.TakeDamage(batDamage, player.transform.position);

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