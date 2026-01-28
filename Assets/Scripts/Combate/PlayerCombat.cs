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
    [SerializeField] private float meleeDuration = 0.2f;
    [SerializeField] private float meleeOffsetDistance = 1.2f;

    private float throwTimer;
    private float lastPogoTime;
    private Vector2 aimDirection;
    private Rigidbody rb; // Referencia para el Pogo

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (playerController == null) playerController = GetComponent<PlayerController>();
    }

    void Update()
    {
        // Gestionar Inputs 
        HandleInput();

        //  Cooldown lanzamiento de bola
        if (throwTimer > 0) throwTimer -= Time.deltaTime;

        // ataque con bate
        if (Input.GetButtonDown("Fire1"))
        {
            PerformMelee();
        }

        // lanza bola 
        if (Input.GetButtonDown("Fire2") && throwTimer <= 0)
        {
            ThrowBall();
        }
    }

    private void HandleInput()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");

        //logica de anclaje
        bool isHoldingPosition = Input.GetKey(KeyCode.LeftControl);

        if (isHoldingPosition)
        {
            
            // Bloqueamos el movimiento en el controller
            playerController.SetCanMove(false);

            // Usamos las teclas WASD solo para definir aimDirection
            if (x != 0 || y != 0)
            {
                aimDirection = new Vector2(x, y).normalized;

                // Giramos visualmente al personaje si apunta a los lados
                if (x != 0) playerController.ManualFlip(x);
            }
            // Si no tocamos nada manteniendo Shift, mantenemos la última aimDirection
        }
        else
        {
            
            // Desbloqueamos el movimiento
            playerController.SetCanMove(true);

            // Prioridad de dirección
            if (x != 0 || y != 0)
            {
                aimDirection = new Vector2(x, y).normalized;
            }
            else
            {
          
                float facingDir = playerController.IsFacingRight() ? 1 : -1;
                aimDirection = new Vector2(facingDir, 0);
            }
        }
    }

    private void PerformMelee()
    {
        // Calcula posición spawn
        Vector3 spawnPos = transform.position + (Vector3)aimDirection * meleeOffsetDistance;

        GameObject hitbox = Instantiate(meleeHitboxPrefab, spawnPos, Quaternion.identity);
        hitbox.transform.parent = transform;

        // Configurar hitbox
        MeleeHitbox meleeScript = hitbox.GetComponent<MeleeHitbox>();
        // Le pasamos 'this' para que pueda llamar a DoPogo()
        meleeScript.Setup(aimDirection, this);

        Destroy(hitbox, meleeDuration);
    }

    private void ThrowBall()
    {
        throwTimer = throwCooldown;

        
        // Si apuntas hacia abajo , hace el pogo automáticamente
        if (aimDirection.y < -0.1f && Mathf.Abs(aimDirection.x) < 0.1f)
        {
            DoPogo();
        }

        GameObject ball = Instantiate(ballPrefab, firePoint.position, Quaternion.identity);
        BallProjectile ballScript = ball.GetComponent<BallProjectile>();

        ballScript.Initialize(aimDirection * throwForce, this);
    }

    // Método público llamado por el MeleeHitbox o al lanzar bola abajo
    public void DoPogo()
    {
        //  Evita que se ejecute 2 veces seguidas
        if (Time.time - lastPogoTime < 0.1f) return;
        lastPogoTime = Time.time;
        //aseguro que el pogo sea consistente
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, pogoForce, 0f);
    }

    // Ayuda visual en el Editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 direction = Application.isPlaying ? (Vector3)aimDirection : Vector3.right;
        Gizmos.DrawWireSphere(transform.position + direction * meleeOffsetDistance, 0.5f);
    }
}