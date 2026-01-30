using UnityEngine;

public class BallProjectile : MonoBehaviour
{
    [Header("Stats Base")]
    [SerializeField] private float speed = 15f;
    [SerializeField] private int damage = 10;
    [SerializeField] private float speedMultiplierPerHit = 1.2f;
    [SerializeField] private int damageMultiplierPerHit = 2;

    [Header("Comportamiento Homing")]
    [SerializeField] private float homingSensitivity = 5f;

    [Header("Comportamiento Pogo")]
    [SerializeField] private Vector3 pogoTargetOffset = new Vector3(0f, -2f, 0f);
    [SerializeField] private float pogoSeekPrecision = 1f;

    [Header("Seguridad Anti-Atasco")]
    [SerializeField] private float maxContactTime = 0.2f;

    private Rigidbody rb;
    private PlayerCombat player;
    private int bounces = 0;

    // ESTADOS
    private bool isReturning = false;
    private bool isPogoSeeking = false;
    private bool wasPogoHit = false;
    private bool isStopped = false;
    private bool canHitPlayer = false;

    private float lifeTimeTimer = 0f;

    private Collider currentContactCollider;
    private float contactTimer;

    public void Initialize(Vector3 velocity, PlayerCombat owner)
    {
        rb = GetComponent<Rigidbody>();
        player = owner;
        rb.linearVelocity = velocity;
    }

    private void Update()
    {
        lifeTimeTimer += Time.deltaTime;

        // Si la bola está parada, seguro que queremos poder recogerla
        if (isStopped) canHitPlayer = true;

        if (lifeTimeTimer > 0.5f && !canHitPlayer && !isReturning && !isPogoSeeking)
        {
            canHitPlayer = true;
        }
    }

    private void FixedUpdate()
    {
        // Si esta detenida, no hacemos nada más
        // Al hacer return aquí, dejamos que el Rigidbody actúe normal (gravedad),

        if (isStopped) return;


        // retorno a debajo del jugador 
        if (isPogoSeeking && player != null)
        {
            Vector3 targetPos = player.transform.position + pogoTargetOffset;
            Vector3 directionToTarget = (targetPos - transform.position).normalized;

            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, directionToTarget * speed, Time.fixedDeltaTime * homingSensitivity);

            if (Vector3.Distance(transform.position, targetPos) < pogoSeekPrecision)
            {
                isPogoSeeking = false;
                isReturning = true;
            }
        }
        // retorno al jugador
        else if (isReturning && player != null)
        {
            Vector3 directionToPlayer = (player.transform.position - transform.position).normalized;
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, directionToPlayer * speed, Time.fixedDeltaTime * homingSensitivity);
        }
        // movimiento constante
        else
        {
            rb.linearVelocity = rb.linearVelocity.normalized * speed;
        }

        // Bloquear Z
        transform.position = new Vector3(transform.position.x, transform.position.y, 0);
    }

    private void OnCollisionEnter(Collision collision)
    {
        currentContactCollider = collision.collider;
        contactTimer = 0f;


        if (isStopped && !collision.gameObject.CompareTag("Player")) return;

        // JUGADOR (Recogida)
        if (collision.gameObject.CompareTag("Player"))
        {
            if (canHitPlayer)
            {
                Destroy(gameObject);
            }
            return;
        }

        // REBOTE NORMAL
        BounceLogic(collision.contacts[0].normal);
    }

    private void OnCollisionStay(Collision collision)
    {
        // Si ya está parada, no necesitamos comprobar nada más
        if (isStopped) return;

        if (collision.collider == currentContactCollider)
        {
            contactTimer += Time.fixedDeltaTime;

            if (contactTimer > maxContactTime)
            {
                StopBall();
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.collider == currentContactCollider)
        {
            currentContactCollider = null;
            contactTimer = 0f;
        }
    }

    // Función para detener la bola
    private void StopBall()
    {
        isStopped = true;
        isReturning = false;
        isPogoSeeking = false;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        canHitPlayer = true;
    }

    public void GetHitByBat(Vector2 newDirection)
    {
        isStopped = false; // volver a permitir movimiento
        // Aumentar stats
        speed *= speedMultiplierPerHit;
        damage *= damageMultiplierPerHit;
        // Resetear estado
        bounces = 0;
        canHitPlayer = false;
        lifeTimeTimer = 0f;
        // Limpiar lógica anterior
        isReturning = false;
        isPogoSeeking = false;
        currentContactCollider = null;
        contactTimer = 0f;
        // Detectar intención Pogo (Golpe hacia abajo)
        if (newDirection.y < -0.1f)
        {
            wasPogoHit = true;
        }
        else
        {
            wasPogoHit = false;
        }
        // Aplicar disparo físico
        rb.linearVelocity = newDirection.normalized * speed;
    }

    private void BounceLogic(Vector3 normal)
    {
        bounces++;
        canHitPlayer = true;  // Permitir recogida

        rb.linearVelocity = Vector3.Reflect(rb.linearVelocity, normal);

        if (bounces >= 1)
        {
            // Decidir comportamiento de vuelta
            if (wasPogoHit)
            {
                isPogoSeeking = true; // Ruta Pogo (ir abajo)
                wasPogoHit = false;
            }
            else
            {
                isReturning = true; // Vuelta directa
            }
        }
    }
}