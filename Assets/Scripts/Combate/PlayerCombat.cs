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
    [SerializeField] private float meleeCooldown = 0.5f;
    [SerializeField] private float meleeDuration = 0.2f;
    [SerializeField] private float meleeOffsetDistance = 1.2f;

    private float throwTimer;
    private float meleeTimer;
    private float lastPogoTime;
    private Vector2 aimDirection;
    private Rigidbody rb;
    private BallProjectile currentBall;

    // --- NUEVA FUNCIÓN PARA EL HUD ---
    public bool CanShoot()
    {
        // Puedes disparar si no hay una bola activa Y el cooldown ha terminado
        return currentBall == null && throwTimer <= 0;
    }

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

        // Limpieza de referencia: Si la bola fue destruida, ponemos currentBall a null
        // Esto permite que el HUD sepa instantáneamente que la bola ya no está en juego.
        if (currentBall != null && currentBall.gameObject == null)
        {
            currentBall = null;
        }

        // UNIFICADO: Todo con Click Izquierdo (Fire1)
        if (Input.GetButtonDown("Fire1"))
        {
            HandleUniversalAttack();
        }
    }

    private void HandleUniversalAttack()
    {
        // Determinamos la dirección (siempre hacia donde mira el personaje o abajo si saltas)
        // Aquí puedes añadir lógica para disparar hacia adelante si estás en el suelo
        if (!playerController.CheckGrounded())
        {
            aimDirection = Vector2.down;
        }
        else
        {
            // Disparar horizontalmente basado en hacia donde mira el controller
            aimDirection = playerController.IsFacingRight() ? Vector2.right : Vector2.left;
        }

        if (currentBall != null)
        {
            // Si la bola existe, el click intenta batearla
            if (meleeTimer <= 0)
            {
                PerformMelee();
            }
        }
        else if (throwTimer <= 0)
        {
            // Si no hay bola y no hay cooldown, la lanzamos
            ThrowBall();
        }
    }

    private void PerformMelee()
    {
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

        // Si lanzamos hacia abajo (en el aire), aplicamos pogo
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