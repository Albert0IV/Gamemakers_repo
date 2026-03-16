using UnityEngine;

public class BallProjectile : MonoBehaviour
{
    [Header("Stats Base")]
    [SerializeField] private float speed = 15f;
    [SerializeField] private int damage = 10;

    [Header("Multiplicadores")]
    [SerializeField] private float speedMultiplierPerHit = 1.2f;
    [SerializeField] private int damageMultiplierPerHit = 2;

    [Header("Comportamiento")]
    [SerializeField] private float homingSensitivity = 5f;
    [SerializeField] private Vector3 pogoTargetOffset = new Vector3(0f, -2f, 0f);
    [SerializeField] private float pogoSeekPrecision = 1f;
    [SerializeField] private float maxContactTime = 0.2f; // Tiempo máximo rozando una pared
    [SerializeField] private float idleLifeTime = 1.0f;  // Tiempo para borrar si está quieta

    [Header("Temporizador de Vuelo")]
    [SerializeField] private float maxLifeAfterHit = 5.0f; // Tiempo máximo tras el batazo

    private Rigidbody rb;
    private PlayerCombat player;
    private int bounces = 0;

    private bool isReturning = false;
    private bool isPogoSeeking = false;
    private bool wasPogoHit = false;
    private bool isStopped = false;
    private bool canHitPlayer = false;
    private bool wasLaunched = false;

    private float lifeTimeTimer = 0f;
    private float stopTimer = 0f;
    private float launchTimer = 0f;

    private Collider currentContactCollider;
    private float contactTimer; // Ahora se usa correctamente para evitar el warning

    public void Initialize(Vector3 velocity, PlayerCombat owner)
    {
        rb = GetComponent<Rigidbody>();
        player = owner;
        rb.linearVelocity = velocity;
        wasLaunched = true; // Empieza a contar desde que aparece
    }

    private void Update()
    {
        lifeTimeTimer += Time.deltaTime;

        // 1. Lógica de autodestrucción si la pelota se detiene
        if (isStopped)
        {
            stopTimer += Time.deltaTime;
            if (stopTimer >= idleLifeTime)
            {
                Destroy(gameObject);
            }
        }
        else
        {
            stopTimer = 0f;
        }

        // 2. Lógica de autodestrucción desde el lanzamiento (Batazo)
        if (wasLaunched && !isStopped)
        {
            launchTimer += Time.deltaTime;
            if (launchTimer >= maxLifeAfterHit)
            {
                Destroy(gameObject);
            }
        }

        // Prevención de colisión inmediata con el jugador
        if (lifeTimeTimer > 0.5f && !canHitPlayer && !isReturning && !isPogoSeeking)
        {
            canHitPlayer = true;
        }
    }

    private void FixedUpdate()
    {
        if (isStopped) return;

        if (isPogoSeeking && player != null)
        {
            Vector3 targetPos = player.transform.position + pogoTargetOffset;
            Vector3 directionToTarget = (targetPos - transform.position).normalized;
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, directionToTarget * speed, Time.fixedDeltaTime * homingSensitivity);

            if (Vector2.Distance(transform.position, targetPos) < pogoSeekPrecision)
            {
                isPogoSeeking = false;
                isReturning = true;
            }
        }
        else if (isReturning && player != null)
        {
            Vector3 directionToPlayer = (player.transform.position - transform.position).normalized;
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, directionToPlayer * speed, Time.fixedDeltaTime * homingSensitivity);
        }
        else
        {
            if (rb.linearVelocity.magnitude > 0)
                rb.linearVelocity = rb.linearVelocity.normalized * speed;
        }

        // Mantener el juego en 2D
        transform.position = new Vector3(transform.position.x, transform.position.y, 0);
    }

    private void OnCollisionEnter(Collision collision)
    {
        currentContactCollider = collision.collider;
        contactTimer = 0f;

        if (isStopped && !collision.gameObject.CompareTag("Player")) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            if (canHitPlayer || isReturning) Destroy(gameObject);
            return;
        }

        if (collision.gameObject.TryGetComponent<IDamageable>(out IDamageable damageable))
        {
            damageable.TakeDamage(damage, transform.position);
        }

        BounceLogic(collision.contacts[0].normal);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (isStopped) return;

        // Uso de contactTimer y maxContactTime para evitar los warnings y mejorar el gameplay
        if (collision.collider == currentContactCollider)
        {
            contactTimer += Time.deltaTime;
            if (contactTimer >= maxContactTime)
            {
                StopBall();
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (currentContactCollider != null && collision.collider == currentContactCollider)
        {
            currentContactCollider = null;
            contactTimer = 0f;
        }
    }

    private void StopBall()
    {
        isStopped = true;
        isReturning = false;
        isPogoSeeking = false;
        wasLaunched = false; // Detener el reloj de vuelo al frenar
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    public void GetHitByBat(Vector2 newDirection)
    {
        isStopped = false;
        stopTimer = 0f;

        // Reiniciar el tiempo de vida en cada batazo
        launchTimer = 0f;
        wasLaunched = true;

        speed *= speedMultiplierPerHit;
        damage *= damageMultiplierPerHit;
        bounces = 0;
        canHitPlayer = false;
        lifeTimeTimer = 0f;
        isReturning = false;
        isPogoSeeking = false;
        currentContactCollider = null;
        contactTimer = 0f;

        wasPogoHit = (newDirection.y < -0.1f);
        rb.linearVelocity = newDirection.normalized * speed;
    }

    private void BounceLogic(Vector3 normal)
    {
        bounces++;
        rb.linearVelocity = Vector3.Reflect(rb.linearVelocity, normal);

        if (bounces >= 1)
        {
            if (wasPogoHit)
            {
                isPogoSeeking = true;
                wasPogoHit = false;
            }
            else
            {
                isReturning = true;
            }
        }
    }
}