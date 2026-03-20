using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Transform firePoint;
    [SerializeField] private Animator animator; // ASIGNADO EN INSPECTOR

    [Header("Prefabs")]
    [SerializeField] private GameObject ballPrefab;
    [SerializeField] private GameObject meleeHitboxPrefab;

    [Header("Configuración Bola")]
    [SerializeField] private float throwCooldown = 1.0f;
    [SerializeField] private float throwForce = 15f;
    [SerializeField] private float pogoForce = 15f;

    [Header("Configuración Melee")]
    [SerializeField] private float meleeCooldown = 0.5f;
    [SerializeField] private float meleeDuration = 0.2f;
    [SerializeField] private float meleeOffsetDistance = 1.2f;

    private float throwTimer;
    private float meleeTimer;
    private float lastPogoTime;
    private Vector2 aimDirection;
    private Rigidbody rb;
    private BallProjectile currentBall;

    public float GetThrowTimer() => throwTimer;
    public float GetMaxThrowCooldown() => throwCooldown;
    public bool HasBallInWorld() => currentBall != null;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (playerController == null) playerController = GetComponent<PlayerController>();
        if (animator == null) animator = GetComponent<Animator>();
        throwTimer = 0;
    }

    void Update()
    {
        if (throwTimer > 0) throwTimer -= Time.deltaTime;
        if (meleeTimer > 0) meleeTimer -= Time.deltaTime;

        if (currentBall != null && currentBall.gameObject == null) ReturnBall();

        if (Input.GetButtonDown("Fire1")) HandleUniversalAttack();
    }

    private void HandleUniversalAttack()
    {
        aimDirection = Vector2.down; // Siempre hacia abajo como pediste

        if (currentBall != null)
        {
            if (meleeTimer <= 0) PerformMelee();
        }
        else if (throwTimer <= 0 && !playerController.CheckGrounded())
        {
            ThrowBall();
        }
    }

    private void ThrowBall()
    {
        // --- ANIMACIÓN: Lanzar bola abajo ---
        animator.SetTrigger("ThrowDown");

        DoPogo();
        GameObject ballObj = Instantiate(ballPrefab, firePoint.position, Quaternion.identity);
        currentBall = ballObj.GetComponent<BallProjectile>();
        currentBall.Initialize(aimDirection * throwForce, this);
        throwTimer = 0;
    }

    public void ReturnBall()
    {
        currentBall = null;
        throwTimer = throwCooldown;
    }

    private void PerformMelee()
    {
        // --- ANIMACIÓN: Bate abajo ---
        animator.SetTrigger("AttackDown");

        meleeTimer = meleeCooldown;
        Vector3 spawnPos = transform.position + (Vector3)aimDirection * meleeOffsetDistance;
        GameObject hitboxObj = Instantiate(meleeHitboxPrefab, spawnPos, Quaternion.identity);
        hitboxObj.transform.parent = transform;
        MeleeHitbox meleeScript = hitboxObj.GetComponent<MeleeHitbox>();
        meleeScript.Setup(aimDirection, this);
        Destroy(hitboxObj, meleeDuration);
    }

    public void DoPogo()
    {
        if (Time.time - lastPogoTime < 0.1f) return;
        lastPogoTime = Time.time;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, pogoForce, 0f);
    }
}