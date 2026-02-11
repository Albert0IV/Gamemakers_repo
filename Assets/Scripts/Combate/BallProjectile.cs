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
        if (isStopped) canHitPlayer = true;
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
            if (Vector3.Distance(transform.position, targetPos) < pogoSeekPrecision)
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
            rb.linearVelocity = rb.linearVelocity.normalized * speed;
        }
        transform.position = new Vector3(transform.position.x, transform.position.y, 0);
    }

    private void OnCollisionEnter(Collision collision)
    {
        currentContactCollider = collision.collider;
        contactTimer = 0f;

        if (isStopped && !collision.gameObject.CompareTag("Player")) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            if (canHitPlayer) Destroy(gameObject);
            return;
        }

        // IMPACTO CON ENEMIGO
        if (collision.gameObject.CompareTag("Enemy"))
        {
            GroundEnemy enemy = collision.gameObject.GetComponent<GroundEnemy>();
            if (enemy != null) enemy.TakeDamage(damage);
        }

        // IMPACTO CON OBJETO ROMPIBLE (Usa el daño actual de la bola)
        BreakableObject breakable = collision.gameObject.GetComponent<BreakableObject>();
        if (breakable != null)
        {
            breakable.HitObject(damage, transform);
        }

        BounceLogic(collision.contacts[0].normal);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (isStopped) return;
        if (collision.collider == currentContactCollider)
        {
            contactTimer += Time.fixedDeltaTime;
            if (contactTimer > maxContactTime) StopBall();
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
        isStopped = false;
        speed *= speedMultiplierPerHit;
        damage *= damageMultiplierPerHit; // El daño aumenta
        bounces = 0;
        canHitPlayer = false;
        lifeTimeTimer = 0f;
        isReturning = false;
        isPogoSeeking = false;
        currentContactCollider = null;
        contactTimer = 0f;

        if (newDirection.y < -0.1f) wasPogoHit = true;
        else wasPogoHit = false;

        rb.linearVelocity = newDirection.normalized * speed;
    }

    private void BounceLogic(Vector3 normal)
    {
        bounces++;
        canHitPlayer = true;
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