using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Transform firePoint;

    [Header("Prefabs")]
    [SerializeField] private GameObject ballPrefab;
    [SerializeField] private GameObject meleeHitboxPrefab;

    [Header("Configuración Bola")]
    [SerializeField] private float throwCooldown = 1.0f;
    [SerializeField] private float throwForce = 15f;
    [SerializeField] private float pogoForce = 15f;

    [Header("Configuración Melee")]
    [SerializeField] private float meleeCooldown = 0.5f; // Cooldown base de 0.5s
    [SerializeField] private float meleeDuration = 0.2f;
    [SerializeField] private float meleeOffsetDistance = 1.2f;

    private float throwTimer;
    private float meleeTimer; // Temporizador para el bate
    private float lastPogoTime;
    private Vector2 aimDirection;
    private Rigidbody rb;
    private BallProjectile currentBall;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (playerController == null) playerController = GetComponent<PlayerController>();
    }

    void Update()
    {
        // Gestión de temporizadores
        if (throwTimer > 0) throwTimer -= Time.deltaTime;
        if (meleeTimer > 0) meleeTimer -= Time.deltaTime;

        // UNIFICADO: Todo con Click Izquierdo (Fire1)
        if (Input.GetButtonDown("Fire1"))
        {
            HandleUniversalAttack();
        }
    }

    private void HandleUniversalAttack()
    {
        // CASO 1: ESTAMOS EN EL AIRE
        if (!playerController.CheckGrounded())
        {
            aimDirection = Vector2.down; // Forzamos dirección hacia abajo

            if (currentBall != null)
            {
                // Si la bola existe, intentamos golpearla (respetando su propio cooldown)
                if (meleeTimer <= 0)
                {
                    PerformMelee();
                }
            }
            else if (throwTimer <= 0)
            {
                // Si no hay bola, la lanzamos hacia abajo
                ThrowBall();
            }
        }
        // CASO 2: ESTAMOS EN EL SUELO
        else
        {
            // Solo lanzamos si no hay una bola activa y el cooldown terminó
            if (currentBall == null && throwTimer <= 0)
            {
                float facingDir = playerController.IsFacingRight() ? 1 : -1;
                aimDirection = new Vector2(facingDir, 0);

                ThrowBall();
            }
        }
    }

    private void PerformMelee()
    {
        // Activamos el cooldown del bate
        meleeTimer = meleeCooldown;

        Vector3 spawnPos = transform.position + (Vector3)aimDirection * meleeOffsetDistance;
        GameObject hitboxObj = Instantiate(meleeHitboxPrefab, spawnPos, Quaternion.identity);
        hitboxObj.transform.parent = transform;

        MeleeHitbox meleeScript = hitboxObj.GetComponent<MeleeHitbox>();
        meleeScript.Setup(aimDirection, this);

        Destroy(hitboxObj, meleeDuration);
    }

    private void ThrowBall()
    {
        throwTimer = throwCooldown;

        // Si lanzamos hacia abajo, aplicamos pogo
        if (aimDirection.y < -0.1f)
        {
            DoPogo();
        }

        GameObject ballObj = Instantiate(ballPrefab, firePoint.position, Quaternion.identity);
        currentBall = ballObj.GetComponent<BallProjectile>();
        currentBall.Initialize(aimDirection * throwForce, this);
    }

    public void DoPogo()
    {
        if (Time.time - lastPogoTime < 0.1f) return;
        lastPogoTime = Time.time;

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, pogoForce, 0f);
    }
}