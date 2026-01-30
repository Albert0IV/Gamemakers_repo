using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private float horizontal;
    private bool isFacingRight = true;
    private float jumpBufferTimer = 0f;

    // Variable para bloquear movimiento al apuntar
    private bool canMove = true;

    [Header("Movimiento")]
    [SerializeField] private float speed = 8f;

    [Header("Salto")]
    [SerializeField] private bool useJumpVelocity = false;
    [SerializeField] private float jumpHeight = 4f;
    [SerializeField] private float jumpVelocity = 12f;
    [SerializeField] private float jumpCutMultiplier = 0.3f;
    [SerializeField] private float jumpBufferTime = 0.2f;

    [Header("Caida & Gravedad")]
    [SerializeField] private float maxFallSpeed = -20f;
    [SerializeField] private float gravityNormal = 3f; // Valor recomendado: 3
    [SerializeField] private float fallMultiplier = 2f; // Valor recomendado: 2

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
    // Distancias ajustadas para evitar huecos y mejorar detección
    private float groundCheckDistance = 0.05f;
    private float wallCheckDistance = 0.05f;

    private void Start()
    {
        if (playerCollider == null) playerCollider = GetComponent<Collider>();
    }

    void Update()
    {
        if (canMove)
        {
            horizontal = Input.GetAxisRaw("Horizontal");
        }
        else
        {
            horizontal = 0f;
        }

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

        if (Input.GetButtonDown("Jump"))
            jumpBufferTimer = jumpBufferTime;

        // Dash (Mantenido en LeftShift según tu código original)
        if (Input.GetKeyDown(KeyCode.LeftShift) && dashCooldownTimer <= 0f && !isDashing && canMove)
            StartCoroutine(Dash());

        if (dashCooldownTimer > 0f)
            dashCooldownTimer -= Time.deltaTime;

        isTouchingWall = CheckWall();
        isWallSliding = isTouchingWall && !isGroundedNow && rb.linearVelocity.y < 0.1f;

        // Salto variable (Jump Cut)
        if (Input.GetButtonUp("Jump") && rb.linearVelocity.y > 0f)
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier, 0f);

        if (canMove) Flip();

        //animator.SetBool("isWalking", Mathf.Abs(horizontal) > 0.1f && isGroundedNow);
        //animator.SetBool("isJumping", !isGroundedNow);
        //animator.SetBool("isDashing", isDashing);
        //animator.SetBool("isWallSliding", isWallSliding);

        wasGroundedPrev = isGroundedNow;
    }

    private void FixedUpdate()
    {
        if (isDashing) return;

        // --- 1. SISTEMA DE GRAVEDAD PERSONALIZADA ---
        // Esto soluciona la sensación de "flotar" sin romper el salto.
        if (!CheckGrounded() && !isWallSliding)
        {
            float targetGravity = gravityNormal;

            // Si caemos, aumentamos la gravedad para dar peso
            if (rb.linearVelocity.y < 0)
            {
                targetGravity *= fallMultiplier;
            }

            // Aplicamos la fuerza extra (Restamos 1 porque Unity ya tiene gravedad base)
            Vector3 extraGravityForce = Physics.gravity * (targetGravity - 1f);
            rb.AddForce(extraGravityForce, ForceMode.Acceleration);
        }
        // --------------------------------------------

        float currentSpeed = speed;

        // Salto
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
                // Fórmula de física corregida para usar la gravedad personalizada
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

            if (performedAction)
            {
                jumpBufferTimer = 0f;
                coyoteTimer = 0f;
            }
            else
            {
                jumpBufferTimer -= Time.fixedDeltaTime;
            }
        }

        // Movimiento Horizontal
        if (!isWallJumping)
        {
            float targetVelocityX = horizontal * currentSpeed;

            // --- 2. SOLUCIÓN WALL SLIDE: FUERZA ADHESIVA ---
            if (isWallSliding)
            {
                // Detectamos si el jugador empuja hacia la pared
                bool pushingIntoWall = (isFacingRight && horizontal > 0) || (!isFacingRight && horizontal < 0);

                if (pushingIntoWall)
                {
                    // Aplicamos una fuerza mínima (0.5f) para mantener el contacto con el BoxCast
                    // pero evitar la fricción excesiva del motor de físicas.
                    float stickyForce = 0.5f;
                    targetVelocityX = isFacingRight ? stickyForce : -stickyForce;
                }
            }
            // -----------------------------------------------

            // Aplicamos la velocidad calculada
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

        // Wall Slide
        if (isWallSliding && !isWallJumping)
        {
            if (rb.linearVelocity.y < -wallSlideSpeed)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, -wallSlideSpeed, 0f);
            }
        }
        else
        {
            // Clamp de velocidad máxima de caída
            float clampedY = Mathf.Clamp(rb.linearVelocity.y, maxFallSpeed, float.MaxValue);

            // Solo aplicamos el clamp si estamos cayendo más rápido que el límite
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

    private bool CheckGrounded()
    {
        Vector3 center = playerCollider.bounds.center;
        Vector3 size = playerCollider.bounds.size;
        Vector3 boxSize = new Vector3(size.x * 0.9f, 0.05f, size.z);
        return Physics.BoxCast(center, boxSize / 2, Vector3.down, Quaternion.identity, (size.y / 2) + groundCheckDistance, groundLayer);
    }

    private bool CheckWall()
    {
        Vector3 center = playerCollider.bounds.center;
        Vector3 size = playerCollider.bounds.size;
        Vector3 direction = isFacingRight ? Vector3.right : Vector3.left;
        Vector3 boxSize = new Vector3(0.05f, size.y * 0.8f, size.z);
        return Physics.BoxCast(center, boxSize / 2, direction, Quaternion.identity, (size.x / 2) + wallCheckDistance, groundLayer);
    }

    private void Flip()
    {
        if (isWallJumping) return;

        // Evitamos girar mientras deslizamos para no perder el contacto con la pared
        if (isWallSliding) return;

        if (isFacingRight && horizontal < 0f || !isFacingRight && horizontal > 0f)
        {
            isFacingRight = !isFacingRight;
            Vector3 localScale = transform.localScale;
            localScale.x *= -1f;
            transform.localScale = localScale;
        }
    }

    private void CheckFlipImmediate(float direction)
    {
        if ((isFacingRight && direction < 0) || (!isFacingRight && direction > 0))
        {
            isFacingRight = !isFacingRight;
            Vector3 localScale = transform.localScale;
            localScale.x *= -1f;
            transform.localScale = localScale;
        }
    }

    private IEnumerator Dash()
    {
        isDashing = true;
        rb.useGravity = false;

        // Ignoramos input vertical (ponemos 0f en la Y)
        Vector3 dashDirection = new Vector3(
            Input.GetAxisRaw("Horizontal"),
            0f,
            0f
        ).normalized;

        // Si no se pulsa nada, usa la dirección en la que mira el jugador
        if (dashDirection == Vector3.zero)
            dashDirection = isFacingRight ? Vector3.right : Vector3.left;

        rb.linearVelocity = dashDirection * dashForce;

        yield return new WaitForSeconds(dashDuration);

        rb.useGravity = true;
        isDashing = false;
        dashCooldownTimer = dashCooldown;

        // Al terminar el dash, reseteamos la velocidad para que no quede inercia flotante
        rb.linearVelocity = new Vector3(0f, 0f, 0f);
    }

    private void StopWallJump()
    {
        isWallJumping = false;
    }

    public void SetCanMove(bool state)
    {
        canMove = state;

        if (!canMove)
        {
            horizontal = 0f;
            animator.SetBool("isWalking", false);
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        }
    }

    public void ManualFlip(float xInput)
    {
        if ((isFacingRight && xInput < 0) || (!isFacingRight && xInput > 0))
        {
            isFacingRight = !isFacingRight;
            Vector3 localScale = transform.localScale;
            localScale.x *= -1f;
            transform.localScale = localScale;
        }
    }

    public bool IsFacingRight() => isFacingRight;

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