using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
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

    [Header("Partículas (VFX)")]
    [SerializeField] private ParticleSystem dashParticles;
    [SerializeField] private ParticleSystem landingParticles;
    private bool wasGrounded;

    [Header("Componentes")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Animator animator;
    [SerializeField] private Collider playerCollider;
    [SerializeField] private LayerMask groundLayer;

    private float groundCheckDistance = 0.05f;
    private float wallCheckDistance = 0.1f; // Un poco más de margen para el flip

    // Getters para otros scripts (Combat/HUD)
    public float GetDashCooldownTimer() => dashCooldownTimer;
    public float GetMaxDashCooldown() => dashCooldown;
    public bool IsDashing() => isDashing;

    private void Start()
    {
        if (playerCollider == null) playerCollider = GetComponent<Collider>();
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (animator == null) animator = GetComponent<Animator>();
        if (transform.localScale.x < 0) isFacingRight = false;
        if (dashParticles != null) dashParticles.Stop();
    }

    void Update()
    {
        if (dashCooldownTimer > 0f) dashCooldownTimer -= Time.deltaTime;

        bool isGroundedNow = CheckGrounded();

        // --- Lógica de Aterrizaje y Coyote Time ---
        animator.SetBool("Grounded", isGroundedNow);
        if (isGroundedNow && !wasGrounded && rb.linearVelocity.y < -1f)
        {
            if (landingParticles != null) landingParticles.Play();
            animator.SetTrigger("Land");
        }
        wasGrounded = isGroundedNow;

        if (isGroundedNow) coyoteTimer = coyoteTime;
        else coyoteTimer -= Time.deltaTime;

        // --- Lógica de Pared con Auto-Flip ---
        isTouchingWall = CheckWall();
        bool wasWallSliding = isWallSliding;
        isWallSliding = isTouchingWall && !isGroundedNow && rb.linearVelocity.y < 0.1f;
        animator.SetBool("IsWallSliding", isWallSliding);

        // Si empieza a deslizar, giramos para mirar hacia afuera
        if (isWallSliding && !wasWallSliding)
        {
            PerformFlip();
        }

        if (!canMove)
        {
            horizontal = 0f;
            UpdateAnimations(isGroundedNow);
            return;
        }

        horizontal = Input.GetAxisRaw("Horizontal");

        // --- Jump Buffer & Dash Input ---
        if (Input.GetButtonDown("Jump")) jumpBufferTimer = jumpBufferTime;

        if (Input.GetKeyDown(KeyCode.LeftShift) && dashCooldownTimer <= 0f && !isDashing)
            StartCoroutine(Dash());

        // Salto corto (Variable Jump Height)
        if (Input.GetButtonUp("Jump") && rb.linearVelocity.y > 0f)
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier, 0f);

        Flip();
        UpdateAnimations(isGroundedNow);
    }

    private void UpdateAnimations(bool isGrounded)
    {
        animator.SetFloat("Speed", Mathf.Abs(horizontal));
        animator.SetFloat("VerticalVelocity", rb.linearVelocity.y);
    }

    private void FixedUpdate()
    {
        if (isDashing) return;

        // Gravedad personalizada
        if (!CheckGrounded() && !isWallSliding)
        {
            float targetGravity = gravityNormal;
            if (rb.linearVelocity.y < 0) targetGravity *= fallMultiplier;
            Vector3 extraGravityForce = Physics.gravity * (targetGravity - 1f);
            rb.AddForce(extraGravityForce, ForceMode.Acceleration);
        }

        if (!canMove) return;

        // --- Procesar Salto (Buffer + Coyote/Wall) ---
        if (jumpBufferTimer > 0f)
        {
            bool performedAction = false;

            // Wall Jump
            if (enableWallJump && isTouchingWall && !CheckGrounded())
            {
                animator.SetTrigger("Jump");
                isWallJumping = true;
                Invoke(nameof(StopWallJump), wallJumpDuration);

                // Salta en dirección opuesta a la que mira (porque mira hacia afuera de la pared)
                float jumpDirection = isFacingRight ? 1f : -1f;
                rb.linearVelocity = new Vector3(wallJumpForce.x * jumpDirection, wallJumpForce.y, 0f);

                // Al saltar de la pared, nos aseguramos de mirar hacia donde saltamos
                CheckFlipImmediate(jumpDirection);
                performedAction = true;
            }
            // Salto Normal / Coyote
            else if (coyoteTimer > 0f)
            {
                animator.SetTrigger("Jump");
                float jumpingPower = useJumpVelocity ? jumpVelocity : Mathf.Sqrt(2f * Mathf.Abs(Physics.gravity.y * gravityNormal) * jumpHeight);
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpingPower, 0f);
                performedAction = true;
            }

            if (performedAction) jumpBufferTimer = 0f;
            else jumpBufferTimer -= Time.fixedDeltaTime;
        }

        // --- Movimiento Horizontal ---
        if (!isWallJumping)
        {
            float targetVelocityX = horizontal * speed;

            // Fricción en pared
            if (isWallSliding)
            {
                // Si intentamos movernos hacia la pared, reducimos velocidad X a casi 0
                targetVelocityX = 0f;
            }
            rb.linearVelocity = new Vector3(targetVelocityX, rb.linearVelocity.y, 0f);
        }

        // --- Velocidad de caída (Wall Slide vs Normal) ---
        if (isWallSliding && !isWallJumping)
        {
            if (rb.linearVelocity.y < -wallSlideSpeed)
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, -wallSlideSpeed, 0f);
        }
        else
        {
            float clampedY = Mathf.Clamp(rb.linearVelocity.y, maxFallSpeed, float.MaxValue);
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, clampedY, 0f);
        }
    }

    void LateUpdate()
    {
        // Forzar eje Z a 0 para evitar que el personaje se desvíe en 3D
        transform.position = new Vector3(transform.position.x, transform.position.y, 0f);
    }

    public bool CheckGrounded()
    {
        Vector3 center = playerCollider.bounds.center;
        Vector3 size = playerCollider.bounds.size;
        return Physics.BoxCast(center, new Vector3(size.x * 0.9f, 0.05f, size.z) / 2, Vector3.down, Quaternion.identity, (size.y / 2) + groundCheckDistance, groundLayer);
    }

    private bool CheckWall()
    {
        Vector3 center = playerCollider.bounds.center;
        Vector3 size = playerCollider.bounds.size;

        // Raycast a ambos lados para que el Flip no rompa la detección
        bool hitRight = Physics.BoxCast(center, new Vector3(0.05f, size.y * 0.8f, size.z) / 2, Vector3.right, Quaternion.identity, (size.x / 2) + wallCheckDistance, groundLayer);
        bool hitLeft = Physics.BoxCast(center, new Vector3(0.05f, size.y * 0.8f, size.z) / 2, Vector3.left, Quaternion.identity, (size.x / 2) + wallCheckDistance, groundLayer);

        return hitRight || hitLeft;
    }

    private void Flip()
    {
        // No girar por input si estamos en la pared (el auto-flip lo maneja) o haciendo walljump
        if (isWallJumping || isWallSliding) return;

        if (isFacingRight && horizontal < 0f || !isFacingRight && horizontal > 0f)
            PerformFlip();
    }

    private void CheckFlipImmediate(float direction)
    {
        if ((direction < 0 && isFacingRight) || (direction > 0 && !isFacingRight))
            PerformFlip();
    }

    private void PerformFlip()
    {
        isFacingRight = !isFacingRight;
        Vector3 localScale = transform.localScale;
        localScale.x *= -1f;
        transform.localScale = localScale;
    }

    private IEnumerator Dash()
    {
        isDashing = true;
        animator.SetBool("IsDashing", true);
        rb.useGravity = false;

        if (dashParticles != null) dashParticles.Play();

        CameraSystem cam = Camera.main.GetComponent<CameraSystem>();
        if (cam != null) cam.ShakeDash();

        // Dirección del dash (si no hay input, dash hacia donde mira)
        Vector3 dashDirection = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, 0f).normalized;
        if (dashDirection == Vector3.zero) dashDirection = isFacingRight ? Vector3.right : Vector3.left;

        rb.linearVelocity = dashDirection * dashForce;

        yield return new WaitForSeconds(dashDuration);

        if (dashParticles != null) dashParticles.Stop();

        rb.useGravity = true;
        isDashing = false;
        animator.SetBool("IsDashing", false);
        dashCooldownTimer = dashCooldown;
        rb.linearVelocity = Vector3.zero; // Frenazo al terminar el dash
    }

    private void StopWallJump() => isWallJumping = false;
    public bool IsWallSliding() => isWallSliding;
    public void SetCanMove(bool state) => canMove = state;
    public bool IsFacingRight() => isFacingRight;
}