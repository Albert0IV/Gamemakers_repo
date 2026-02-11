using UnityEngine;
using System.Collections;

public class GroundEnemy : MonoBehaviour
{
    [Header("Estadísticas Generales")]
    [SerializeField] private int maxHealth = 50;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float chaseSpeed = 5f;

    [Header("Daño por Contacto (Cuerpo)")]
    [SerializeField] private int bodyDamage = 1;

    [Header("Ataque Activo (Melee)")]
    [SerializeField] private int attackDamage = 2;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackWindup = 0.5f; // Tiempo antes del golpe
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

    // Estados
    private bool isChasing = false;
    private bool canAttack = true;
    private bool isAttacking = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        currentHealth = maxHealth;
        startPos = transform.position;

        // Buscamos al jugador por etiqueta
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }

        if (attackPoint == null) attackPoint = transform;
    }

    void FixedUpdate()
    {
        // 1. Si estamos atacando, nos quedamos quietos
        if (isAttacking)
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            return;
        }

        if (playerTransform == null) return;

        float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // 2. LÓGICA DE ATAQUE
        if (distToPlayer <= attackRange && canAttack)
        {
            StartCoroutine(PerformSlashAttack());
            return; // Salimos para no movernos mientras iniciamos el ataque
        }

        // 3. LÓGICA DE DETECCIÓN
        if (distToPlayer < detectionRange)
        {
            isChasing = true;
        }
        else if (distToPlayer > detectionRange * 1.5f) // Hysteresis (margen para dejar de perseguir)
        {
            isChasing = false;
        }

        // 4. MOVIMIENTO
        if (isChasing)
        {
            ChaseLogic();
        }
        else
        {
            PatrolLogic();
        }
    }

    private IEnumerator PerformSlashAttack()
    {
        isAttacking = true;
        canAttack = false;

        // Frenar en seco para atacar
        rb.linearVelocity = Vector3.zero;

        // Mirar al jugador antes de golpear
        FacePlayer();

        // Esperar tiempo de preparación (animación de levantar arma)
        yield return new WaitForSeconds(attackWindup);

        // DETECTAR GOLPE
        // Creamos un círculo invisible en el attackPoint
        Collider[] hitPlayers = Physics.OverlapSphere(attackPoint.position, attackRadius, playerLayer);

        foreach (Collider player in hitPlayers)
        {
            PlayerHealth health = player.GetComponent<PlayerHealth>();
            if (health != null)
            {
                // ¡IMPORTANTE! Pasamos 'transform.position' para que el jugador sepa desde dónde le pegaron
                // y pueda calcular el empuje (Knockback) correctamente.
                health.TakeDamage(attackDamage, transform.position);
            }
        }

        // Esperar cooldown
        yield return new WaitForSeconds(attackCooldown);

        isAttacking = false;
        canAttack = true;
    }

    // Usamos OnCollisionEnter en lugar de Stay para un golpe seco y rebote limpio
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerHealth hp = collision.gameObject.GetComponent<PlayerHealth>();
            if (hp != null)
            {
                // Golpe por contacto físico
                hp.TakeDamage(bodyDamage, transform.position);
            }
        }
    }

    private void PatrolLogic()
    {
        float rightLimit = startPos.x + patrolDistance;
        float leftLimit = startPos.x - patrolDistance;

        // Moverse
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
        FacePlayer(); // Girar hacia el jugador

        // Moverse hacia donde mira
        float dir = facingRight ? 1 : -1;
        rb.linearVelocity = new Vector3(dir * chaseSpeed, rb.linearVelocity.y, 0);
    }

    private void FacePlayer()
    {
        // Solo girar si el jugador está claramente al otro lado
        if (playerTransform.position.x < transform.position.x && facingRight)
        {
            Flip();
        }
        else if (playerTransform.position.x > transform.position.x && !facingRight)
        {
            Flip();
        }
    }

    private void Flip()
    {
        facingRight = !facingRight;
        Vector3 localScale = transform.localScale;
        localScale.x *= -1f;
        transform.localScale = localScale;
    }

    // Esta función permite que el jugador mate al enemigo (opcional)
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        // Animación de herido opcional aquí
        if (currentHealth <= 0)
        {
            Destroy(gameObject);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Dibujos de ayuda en el editor
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange); // Rango visión

        Gizmos.color = Color.red;
        if (attackPoint != null)
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius); // Área de daño del ataque

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(new Vector3(startPos.x - patrolDistance, startPos.y, startPos.z), new Vector3(startPos.x + patrolDistance, startPos.y, startPos.z)); // Patrulla
    }
}