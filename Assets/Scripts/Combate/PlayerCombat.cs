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

    // Eliminamos la variable isCooldownActive que daba error y usamos throwTimer directamente

    public float GetThrowTimer() => throwTimer;
    public float GetMaxThrowCooldown() => throwCooldown;
    public bool HasBallInWorld() => currentBall != null;

    public bool CanShoot()
    {
        // Solo dispara si NO hay bola en el mundo, el timer llegó a 0 y NO está en el suelo
        return currentBall == null && throwTimer <= 0 && !playerController.CheckGrounded();
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (playerController == null) playerController = GetComponent<PlayerController>();
        throwTimer = 0; // Empezamos con la bola lista
    }

    void Update()
    {
        // El temporizador siempre intenta bajar a 0
        if (throwTimer > 0)
        {
            throwTimer -= Time.deltaTime;
        }

        if (meleeTimer > 0) meleeTimer -= Time.deltaTime;

        // Si la bola se destruye (por ejemplo, cae al vacío), recuperamos el estado
        if (currentBall != null && currentBall.gameObject == null)
        {
            ReturnBall();
        }

        if (Input.GetButtonDown("Fire1"))
        {
            HandleUniversalAttack();
        }
    }

    private void HandleUniversalAttack()
    {
        aimDirection = Vector2.down;

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
        DoPogo();
        GameObject ballObj = Instantiate(ballPrefab, firePoint.position, Quaternion.identity);
        currentBall = ballObj.GetComponent<BallProjectile>();
        currentBall.Initialize(aimDirection * throwForce, this);

        // Al lanzar, NO activamos el timer todavía. 
        // El HUD detectará que currentBall != null y se pondrá a 0.
        throwTimer = 0;
    }

    public void ReturnBall()
    {
        currentBall = null;
        // AQUÍ es donde empieza el tiempo de espera
        throwTimer = throwCooldown;
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

    public void DoPogo()
    {
        if (Time.time - lastPogoTime < 0.1f) return;
        lastPogoTime = Time.time;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, pogoForce, 0f);
    }
  
}