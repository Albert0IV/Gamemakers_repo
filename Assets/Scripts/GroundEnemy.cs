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
    [SerializeField] private float attackWindup = 0.5f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRadius = 0.8f;
    [SerializeField] private LayerMask playerLayer;

    [Header("IA / Sensores")]
    [SerializeField] private float detectionRange = 6f;
    [SerializeField] private float patrolDistance = 4f;

    private Vector3 startPos;

    // Control de dirección idéntico al PlayerController
    private bool facingRight = true;

    private int currentHealth;

    private Transform playerTransform;
    private Rigidbody rb;
    private PlayerHealth playerHealthScript;

    // Estados
    private bool isChasing = false;
    private bool canAttack = true;
    private bool isAttacking = false;

    private float contactDamageCooldown = 1.0f;
    private float lastContactTime;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        currentHealth = maxHealth;
        startPos = transform.position;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            playerHealthScript = playerObj.GetComponent<PlayerHealth>();
        }

        if (attackPoint == null) attackPoint = transform;
    }

    void FixedUpdate()
    {
        // Frenar si ataca
        if (isAttacking)
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            return;
        }

        if (playerTransform == null) return;

        float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // 1. ATAQUE
        if (distToPlayer <= attackRange && canAttack)
        {
            StartCoroutine(PerformSlashAttack());
            return;
        }

        // 2. DETECCIÓN
        if (distToPlayer < detectionRange)
        {
            isChasing = true;
        }
        else if (distToPlayer > detectionRange * 1.5f)
        {
            isChasing = false;
        }

        // 3. MOVIMIENTO
        if (isChasing)
        {
            ChaseLogic();
        }
        else
        {
            PatrolLogic();
        }

        transform.position = new Vector3(transform.position.x, transform.position.y, 0);
    }

    private IEnumerator PerformSlashAttack()
    {
        isAttacking = true;
        canAttack = false;

        rb.linearVelocity = Vector3.zero;

        // Girar hacia el jugador antes del ataque
        FacePlayer();

        yield return new WaitForSeconds(attackWindup);

        Collider[] hitPlayers = Physics.OverlapSphere(attackPoint.position, attackRadius, playerLayer);
        foreach (Collider player in hitPlayers)
        {
            PlayerHealth health = player.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(attackDamage);
            }
        }

        yield return new WaitForSeconds(attackCooldown);

        isAttacking = false;
        canAttack = true;
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (Time.time - lastContactTime > contactDamageCooldown)
            {
                if (playerHealthScript != null)
                {
                    playerHealthScript.TakeDamage(bodyDamage);
                    lastContactTime = Time.time;
                }
            }
        }
    }

    private void PatrolLogic()
    {
        float rightLimit = startPos.x + patrolDistance;
        float leftLimit = startPos.x - patrolDistance;

        if (facingRight)
        {
            rb.linearVelocity = new Vector3(moveSpeed, rb.linearVelocity.y, 0);
            if (transform.position.x >= rightLimit)
            {
                Flip();
            }
        }
        else
        {
            rb.linearVelocity = new Vector3(-moveSpeed, rb.linearVelocity.y, 0);
            if (transform.position.x <= leftLimit)
            {
                Flip();
            }
        }
    }

    private void ChaseLogic()
    {
        FacePlayer(); // Orienta al enemigo hacia el jugador

        float dir = facingRight ? 1 : -1;
        rb.linearVelocity = new Vector3(dir * chaseSpeed, rb.linearVelocity.y, 0);
    }

    // --- LÓGICA DE FLIP ACTUALIZADA (IGUAL QUE PLAYER CONTROLLER) ---

    private void FacePlayer()
    {
        // Si el jugador está a la izquierda y yo miro a la derecha -> FLIP
        if (playerTransform.position.x < transform.position.x && facingRight)
        {
            Flip();
        }
        // Si el jugador está a la derecha y yo miro a la izquierda -> FLIP
        else if (playerTransform.position.x > transform.position.x && !facingRight)
        {
            Flip();
        }
    }

    private void Flip()
    {
        // Invertimos el booleano
        facingRight = !facingRight;

        // Invertimos la escala en X (Igual que en tu PlayerController)
        Vector3 localScale = transform.localScale;
        localScale.x *= -1f;
        transform.localScale = localScale;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            Destroy(gameObject);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        if (attackPoint != null) { Gizmos.color = Color.red; Gizmos.DrawWireSphere(attackPoint.position, attackRadius); }
    }
}