using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // Hacemos esta variable visible en el inspector para que veas si funciona el Stun
    [Header("Debug Estado")]
    public bool canMove = true;

    [Header("Movimiento")]
    [SerializeField] private float speed = 8f;
    private float horizontal;
    private bool isFacingRight = true;

    [Header("Salto")]
    [SerializeField] private bool useJumpVelocity = false;
    [SerializeField] private float jumpHeight = 4f;
    [SerializeField] private float jumpVelocity = 12f;
    [SerializeField] private float jumpCutMultiplier = 0.3f;
    [SerializeField] private float jumpBufferTime = 0.2f;
    private float jumpBufferTimer = 0f;

    [Header("Caida & Gravedad")]
    [SerializeField] private float maxFallSpeed = -20f;
    [SerializeField] private float gravityNormal = 3f;
    [SerializeField] private float fallMultiplier = 2f;

    [Header("Coyote Time")]
    [SerializeField] private float coyoteTime = 0.1f;
    private float coyoteTimer;

    [Header("Double Jump")]
    [SerializeField] private bool enableDoubleJump = true;
    [SerializeField] private float doubleJumpVelocity = 12f;
    private bool canDoubleJump;

    [Header("Dash")]
    [SerializeField] private float dashForce = 20f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 0.5f;
    private bool isDashing = false;
    private float dashCooldownTimer = 0f;

    [Header("Wall Grab y Jump")]
    [SerializeField] private float wallSlideSpeed = 2f;
    [SerializeField] private bool enableWallJump = true;
    [SerializeField] private Vector2 wallJumpForce = new Vector2(12f, 14f);
    private bool isTouchingWall;
    private bool isWallSliding;
    private bool isWallJumping = false;
    private float wallJumpDuration = 0.2f;

    [Header("Componentes")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Animator animator;
    [SerializeField] private Collider playerCollider;
    [SerializeField] private LayerMask groundLayer;

    [Header("Resistencia en el aire")]
    [SerializeField] private bool enableAirResistance = true;
    [SerializeField] private float airAccelerationSmoothing = 0.08f;

    private bool wasGroundedPrev = true;
    private float groundCheckDistance = 0.05f;
    private float wallCheckDistance = 0.05f;

    private void Start()
    {
        if (playerCollider == null) playerCollider = GetComponent<Collider>();
        if (rb == null) rb = GetComponent<Rigidbody>();

        if (transform.localScale.x < 0) isFacingRight = false;
    }

    void Update()
    {
        // 1. GESTIÓN DE COOLDOWNS (Siempre corre, incluso aturdido)
        if (dashCooldownTimer > 0f) dashCooldownTimer -= Time.deltaTime;

        bool isGroundedNow = CheckGrounded();

        // Coyote Time
        if (isGroundedNow)
        {
            coyoteTimer = coyoteTime;
            canDoubleJump = true;
        }
        else
        {
            coyoteTimer -= Time.deltaTime;
        }

        isTouchingWall = CheckWall();
        isWallSliding = isTouchingWall && !isGroundedNow && rb.linearVelocity.y < 0.1f;

        // ================================================================
        // 2. BLOQUEO TOTAL DE INPUTS (STUN)
        // Si canMove es false, NO leemos ni movimiento, ni salto, ni dash.
        // ================================================================
        if (!canMove)
        {
            horizontal = 0f;
            // IMPORTANTE: Al hacer return aquí, impedimos que se procese el Salto o Dash abajo.
            // Esto asegura que el jugador no pueda cancelar el aturdimiento saltando.
            return;
        }

        // --- INPUTS DE MOVIMIENTO (Solo si canMove es true) ---

        horizontal = Input.GetAxisRaw("Horizontal");

        // Input Salto (Buffer)
        if (Input.GetButtonDown("Jump"))
            jumpBufferTimer = jumpBufferTime;

        // Input Dash
        if (Input.GetKeyDown(KeyCode.LeftShift) && dashCooldownTimer <= 0f && !isDashing)
            StartCoroutine(Dash());

        // Salto variable (Jump Cut)
        if (Input.GetButtonUp("Jump") && rb.linearVelocity.y > 0f)
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier, 0f);

        Flip();

        wasGroundedPrev = isGroundedNow;
    }

    private void FixedUpdate()
    {
        if (isDashing) return;

        // Gravedad Personalizada (Siempre activa para que caigas bien al recibir daño en el aire)
        if (!CheckGrounded() && !isWallSliding)
        {
            float targetGravity = gravityNormal;
            if (rb.linearVelocity.y < 0) targetGravity *= fallMultiplier;
            Vector3 extraGravityForce = Physics.gravity * (targetGravity - 1f);
            rb.AddForce(extraGravityForce, ForceMode.Acceleration);
        }

        // BLOQUEO DE FÍSICAS POR STUN
        // Si no nos podemos mover, dejamos que la fuerza del golpe (Knockback) nos mueva.
        // No ejecutamos el código de movimiento normal.
        if (!canMove) return;

        // --- LÓGICA DE MOVIMIENTO NORMAL ---

        float currentSpeed = speed;

        // Procesar Salto
        if (jumpBufferTimer > 0f)
        {
            bool performedAction = false;

            if (enableWallJump && isTouchingWall && !CheckGrounded())
            {
                isWallJumping = true;
                Invoke(nameof(StopWallJump), wallJumpDuration);
                float jumpDirection = isFacingRight ? -1f : 1f;
                rb.linearVelocity = new Vector3(wallJumpForce.x * jumpDirection, wallJumpForce.y, 0f);
                CheckFlipImmediate(jumpDirection);
                jumpBufferTimer = 0f;
                canDoubleJump = true;
                performedAction = true;
            }
            else if (coyoteTimer > 0f)
            {
                float jumpingPower = useJumpVelocity
                    ? jumpVelocity
                    : Mathf.Sqrt(2f * Mathf.Abs(Physics.gravity.y * gravityNormal) * jumpHeight);
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpingPower, 0f);
                performedAction = true;
            }
            else if (enableDoubleJump && canDoubleJump)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, doubleJumpVelocity, 0f);
                canDoubleJump = false;
                performedAction = true;
            }

            if (performedAction) jumpBufferTimer = 0f; // Reset buffer
            else jumpBufferTimer -= Time.fixedDeltaTime;
        }

        // Movimiento Horizontal
        if (!isWallJumping)
        {
            float targetVelocityX = horizontal * currentSpeed;

            if (isWallSliding)
            {
                bool pushingIntoWall = (isFacingRight && horizontal > 0) || (!isFacingRight && horizontal < 0);
                if (pushingIntoWall) targetVelocityX = isFacingRight ? 0.5f : -0.5f;
            }

            if (enableAirResistance && !CheckGrounded())
            {
                float smoothedVelocityX = Mathf.Lerp(rb.linearVelocity.x, targetVelocityX, airAccelerationSmoothing);
                rb.linearVelocity = new Vector3(smoothedVelocityX, rb.linearVelocity.y, 0f);
            }
            else
            {
                rb.linearVelocity = new Vector3(targetVelocityX, rb.linearVelocity.y, 0f);
            }
        }

        // Limitadores de velocidad (Wall Slide & Max Fall)
        if (isWallSliding && !isWallJumping)
        {
            if (rb.linearVelocity.y < -wallSlideSpeed)
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, -wallSlideSpeed, 0f);
        }
        else
        {
            float clampedY = Mathf.Clamp(rb.linearVelocity.y, maxFallSpeed, float.MaxValue);
            if (rb.linearVelocity.y < clampedY)
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, clampedY, 0f);
        }
    }

    void LateUpdate()
    {
        Vector3 pos = transform.position;
        pos.z = 0f;
        transform.position = pos;
    }

    // --- UTILITIES & CHECKS ---

    private bool CheckGrounded()
    {
        Vector3 center = playerCollider.bounds.center;
        Vector3 size = playerCollider.bounds.size;
        return Physics.BoxCast(center, new Vector3(size.x * 0.9f, 0.05f, size.z) / 2, Vector3.down, Quaternion.identity, (size.y / 2) + groundCheckDistance, groundLayer);
    }

    private bool CheckWall()
    {
        Vector3 center = playerCollider.bounds.center;
        Vector3 size = playerCollider.bounds.size;
        Vector3 direction = isFacingRight ? Vector3.right : Vector3.left;
        return Physics.BoxCast(center, new Vector3(0.05f, size.y * 0.8f, size.z) / 2, direction, Quaternion.identity, (size.x / 2) + wallCheckDistance, groundLayer);
    }

    // --- FLIP SYSTEM (Negative Scale) ---

    private void Flip()
    {
        if (isWallJumping || isWallSliding) return;
        if (isFacingRight && horizontal < 0f || !isFacingRight && horizontal > 0f) PerformFlip();
    }

    private void CheckFlipImmediate(float direction)
    {
        if ((isFacingRight && direction < 0) || (!isFacingRight && direction > 0)) PerformFlip();
    }

    private void PerformFlip()
    {
        isFacingRight = !isFacingRight;
        Vector3 localScale = transform.localScale;
        localScale.x *= -1f;
        transform.localScale = localScale;
    }

    public void ManualFlip(float xInput)
    {
        if ((isFacingRight && xInput < 0) || (!isFacingRight && xInput > 0)) PerformFlip();
    }

    public bool IsFacingRight() => isFacingRight;

    // --- DASH ---

    private IEnumerator Dash()
    {
        isDashing = true;
        rb.useGravity = false;
        Vector3 dashDirection = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, 0f).normalized;
        if (dashDirection == Vector3.zero) dashDirection = isFacingRight ? Vector3.right : Vector3.left;
        rb.linearVelocity = dashDirection * dashForce;
        yield return new WaitForSeconds(dashDuration);
        rb.useGravity = true;
        isDashing = false;
        dashCooldownTimer = dashCooldown;
        rb.linearVelocity = Vector3.zero;
    }

    private void StopWallJump() => isWallJumping = false;

    // --- PUBLIC METHODS FOR HEALTH SCRIPT ---

    public void SetCanMove(bool state)
    {
        canMove = state;

        if (!canMove)
        {
            // Solo reseteamos animaciones y variables lógicas.
            // NO tocamos rb.linearVelocity aquí para no frenar el Knockback.
            horizontal = 0f;
            jumpBufferTimer = 0f;
            if (animator != null) animator.SetBool("isWalking", false);
        }
    }

    private void OnDrawGizmos()
    {
        if (playerCollider == null) return;
        Gizmos.color = Color.red;
        Vector3 center = playerCollider.bounds.center;
        Vector3 size = playerCollider.bounds.size;
        Gizmos.DrawWireCube(center + Vector3.down * ((size.y / 2) + groundCheckDistance), new Vector3(size.x * 0.9f, 0.05f, size.z));
        Vector3 direction = isFacingRight ? Vector3.right : Vector3.left;
        Gizmos.DrawWireCube(center + direction * ((size.x / 2) + wallCheckDistance), new Vector3(0.05f, size.y * 0.8f, size.z));
    }
}