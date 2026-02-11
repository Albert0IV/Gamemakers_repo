using UnityEngine;
using System.Collections;

public class GroundEnemy : MonoBehaviour
{
    [Header("Estadísticas Generales")]
    [SerializeField] private int maxHealth = 50;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float chaseSpeed = 5f;

    [Header("Ajustes de Knockback (Recibir Daño)")]
    [Tooltip("Fuerza base que multiplica a los ratios.")]
    [SerializeField] private float knockbackForce = 12f;
    [Tooltip("Multiplicador horizontal. Úsalo alto para que el enemigo sea desplazado lateralmente.")]
    [SerializeField] private float horizontalKnockbackRatio = 3.0f;
    [Tooltip("Multiplicador vertical. Bajo (0.3 - 0.5) para que no vuele demasiado.")]
    [SerializeField] private float upwardKnockbackRatio = 0.4f;

    [Header("Feedback Visual")]
    [SerializeField] private Renderer enemyRenderer;
    [SerializeField] private float flashDuration = 0.2f;
    [SerializeField] private GameObject attackVisual; // Objeto que representa el slash
    [SerializeField] private float visualAttackDuration = 0.15f;

    [Header("Daño por Contacto")]
    [SerializeField] private int bodyDamage = 1;

    [Header("Ataque Activo (Melee)")]
    [SerializeField] private int attackDamage = 2;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackWindup = 0.5f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRadius = 0.8f;
    [SerializeField] private LayerMask playerLayer;

    [Header("IA / Sensores")]
    [SerializeField] private float detectionRange = 6f;
    [SerializeField] private float patrolDistance = 4f;

    private Vector3 startPos;
    private bool facingRight = true;
    private int currentHealth;

    private Transform playerTransform;
    private Rigidbody rb;

    private bool isChasing = false;
    private bool canAttack = true;
    private bool isAttacking = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        currentHealth = maxHealth;
        startPos = transform.position;

        // Buscamos al jugador de forma robusta por su script principal
        PlayerController pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc != null) playerTransform = pc.transform;

        if (attackPoint == null) attackPoint = transform;
        if (enemyRenderer == null) enemyRenderer = GetComponentInChildren<Renderer>();

        // Aseguramos que el visual esté apagado al inicio
        if (attackVisual != null) attackVisual.SetActive(false);
    }

    void FixedUpdate()
    {
        if (isAttacking)
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            return;
        }

        if (playerTransform == null) return;

        float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distToPlayer <= attackRange && canAttack)
        {
            StartCoroutine(PerformSlashAttack());
            return;
        }

        if (distToPlayer < detectionRange) isChasing = true;
        else if (distToPlayer > detectionRange * 1.5f) isChasing = false;

        if (isChasing) ChaseLogic();
        else PatrolLogic();
    }

    // --- SISTEMA DE DAÑO Y KNOCKBACK ---
    public void TakeDamage(int damage, Vector3 attackerPos)
    {
        currentHealth -= damage;

        // Dirección basada SIEMPRE en la posición del jugador respecto al enemigo
        ApplyKnockbackFromPlayer();

        StartCoroutine(FlashRed());

        if (currentHealth <= 0) Die();
    }

    private void ApplyKnockbackFromPlayer()
    {
        if (rb == null || playerTransform == null) return;

        rb.linearVelocity = Vector3.zero;

        // Si el enemigo está a la derecha del jugador (x mayor), se va a la derecha (1)
        float forceDirectionX = (transform.position.x >= playerTransform.position.x) ? 1f : -1f;

        Vector3 forceVector = new Vector3(
            forceDirectionX * horizontalKnockbackRatio * knockbackForce,
            upwardKnockbackRatio * knockbackForce,
            0f
        );

        rb.AddForce(forceVector, ForceMode.VelocityChange);
    }

    private IEnumerator FlashRed()
    {
        if (enemyRenderer)
        {
            enemyRenderer.material.color = Color.red;
            yield return new WaitForSeconds(flashDuration);
            enemyRenderer.material.color = Color.white;
        }
    }

    // --- LÓGICA DE ATAQUE CON INDICADOR VISUAL ---
    private IEnumerator PerformSlashAttack()
    {
        isAttacking = true;
        canAttack = false;
        rb.linearVelocity = Vector3.zero;

        FacePlayer();

        // Tiempo de "carga" o preparación
        yield return new WaitForSeconds(attackWindup);

        // ACTIVAR INDICADOR Y APLICAR DAÑO
        if (attackVisual != null) attackVisual.SetActive(true);

        Collider[] hitPlayers = Physics.OverlapSphere(attackPoint.position, attackRadius, playerLayer);
        foreach (Collider player in hitPlayers)
        {
            PlayerHealth health = player.GetComponent<PlayerHealth>();
            if (health != null) health.TakeDamage(attackDamage, transform.position);
        }

        // El visual dura un breve instante
        yield return new WaitForSeconds(visualAttackDuration);
        if (attackVisual != null) attackVisual.SetActive(false);

        // Esperar el resto del cooldown
        yield return new WaitForSeconds(attackCooldown - visualAttackDuration);

        isAttacking = false;
        canAttack = true;
    }

    // --- LÓGICA DE MOVIMIENTO E IA ---
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerHealth hp = collision.gameObject.GetComponent<PlayerHealth>();
            if (hp != null) hp.TakeDamage(bodyDamage, transform.position);
        }
    }

    private void PatrolLogic()
    {
        float rightLimit = startPos.x + patrolDistance;
        float leftLimit = startPos.x - patrolDistance;

        if (facingRight)
        {
            rb.linearVelocity = new Vector3(moveSpeed, rb.linearVelocity.y, 0);
            if (transform.position.x >= rightLimit) Flip();
        }
        else
        {
            rb.linearVelocity = new Vector3(-moveSpeed, rb.linearVelocity.y, 0);
            if (transform.position.x <= leftLimit) Flip();
        }
    }

    private void ChaseLogic()
    {
        FacePlayer();
        float dir = facingRight ? 1 : -1;
        rb.linearVelocity = new Vector3(dir * chaseSpeed, rb.linearVelocity.y, 0);
    }

    private void FacePlayer()
    {
        if (playerTransform == null) return;
        if (playerTransform.position.x < transform.position.x && facingRight) Flip();
        else if (playerTransform.position.x > transform.position.x && !facingRight) Flip();
    }

    private void Flip()
    {
        facingRight = !facingRight;
        Vector3 localScale = transform.localScale;
        localScale.x *= -1f;
        transform.localScale = localScale;
    }

    private void Die() => Destroy(gameObject);

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        if (attackPoint != null) Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
    }
}