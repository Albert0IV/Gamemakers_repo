using UnityEngine;
using System.Collections;

// interfaz idamageable para que el enemigo reciba daño de forma estandar
public class GroundEnemy : MonoBehaviour, IDamageable
{
    [Header("Estadísticas Generales")]
    [SerializeField] private int maxHealth = 50;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float chaseSpeed = 5f;

    [Header("Ajustes de Knockback (Recibir Daño)")]
    [SerializeField] private float knockbackForce = 12f;
    [SerializeField] private float horizontalKnockbackRatio = 3.0f;
    [SerializeField] private float upwardKnockbackRatio = 0.4f;

    [Header("Feedback Visual")]
    [SerializeField] private Renderer enemyRenderer;
    [SerializeField] private float flashDuration = 0.2f;
    [SerializeField] private GameObject attackVisual;
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

    // inicializa componentes y busca al jugador
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        currentHealth = maxHealth;
        startPos = transform.position;

        PlayerController pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc != null) playerTransform = pc.transform;

        if (attackPoint == null) attackPoint = transform;
        if (enemyRenderer == null) enemyRenderer = GetComponentInChildren<Renderer>();
        if (attackVisual != null) attackVisual.SetActive(false);
    }
    // gestiona estados de movimiento y ataque en cada frame fisico
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
    // metodo de la interfaz para restar vida y aplicar efectos al ser golpeado
    public void TakeDamage(int damage, Vector3 attackerPos)
    {
        currentHealth -= damage;
        ApplyKnockbackFromPlayer();
        StartCoroutine(FlashRed());
        if (currentHealth <= 0) Die();
    }
    // aplica fuerza fisica para empujar al enemigo lejos del jugador
    private void ApplyKnockbackFromPlayer()
    {
        if (rb == null || playerTransform == null) return;
        rb.linearVelocity = Vector3.zero;
        float forceDirectionX = (transform.position.x >= playerTransform.position.x) ? 1f : -1f;
        Vector3 forceVector = new Vector3(forceDirectionX * horizontalKnockbackRatio * knockbackForce, upwardKnockbackRatio * knockbackForce, 0f);
        rb.AddForce(forceVector, ForceMode.VelocityChange);
    }
    // cambia el color del material temporalmente como feedback de daño
    private IEnumerator FlashRed()
    {
        if (enemyRenderer)
        {
            enemyRenderer.material.color = Color.red;
            yield return new WaitForSeconds(flashDuration);
            enemyRenderer.material.color = Color.white;
        }
    }
    // secuencia de ataque melee: aviso, activacion de daño en area y enfriamiento
    private IEnumerator PerformSlashAttack()
    {
        isAttacking = true;
        canAttack = false;
        rb.linearVelocity = Vector3.zero;
        FacePlayer();
        yield return new WaitForSeconds(attackWindup);
        if (attackVisual != null) attackVisual.SetActive(true);

        Collider[] hitPlayers = Physics.OverlapSphere(attackPoint.position, attackRadius, playerLayer);
        foreach (Collider player in hitPlayers)
        {
            PlayerHealth health = player.GetComponent<PlayerHealth>();
            if (health != null) health.TakeDamage(attackDamage, transform.position);
        }

        yield return new WaitForSeconds(visualAttackDuration);
        if (attackVisual != null) attackVisual.SetActive(false);
        yield return new WaitForSeconds(attackCooldown - visualAttackDuration);
        isAttacking = false;
        canAttack = true;
    }
    // detecta colision fisica para dañar al jugador por contacto
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerHealth hp = collision.gameObject.GetComponent<PlayerHealth>();
            if (hp != null) hp.TakeDamage(bodyDamage, transform.position);
        }
    }
    // mueve al enemigo de izquierda a derecha en un area fija
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
    // mueve al enemigo directamente hacia el jugador
    private void ChaseLogic()
    {
        FacePlayer();
        float dir = facingRight ? 1 : -1;
        rb.linearVelocity = new Vector3(dir * chaseSpeed, rb.linearVelocity.y, 0);
    }
    // orienta al enemigo segun la posicion del jugador
    private void FacePlayer()
    {
        if (playerTransform == null) return;
        if (playerTransform.position.x < transform.position.x && facingRight) Flip();
        else if (playerTransform.position.x > transform.position.x && !facingRight) Flip();
    }
    // invierte la escala horizontal para girar visualmente al enemigo
    private void Flip()
    {
        facingRight = !facingRight;
        Vector3 localScale = transform.localScale;
        localScale.x *= -1f;
        transform.localScale = localScale;
    }
    // elimina el objeto de la escena al morir
    private void Die() => Destroy(gameObject);
    // dibuja esferas en el editor para visualizar rangos de vision y ataque
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        if (attackPoint != null) Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
    }
}