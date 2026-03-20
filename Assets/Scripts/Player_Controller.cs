using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Debug Estado")]
    public bool canMove = true; // para bloquear el control cuando nos morimos con los pinchos 

    [Header("Movimiento")]
    [SerializeField] private float speed = 8f;
    private float horizontal;
    private bool isFacingRight = true;

    [Header("Salto")]
    [SerializeField] private bool useJumpVelocity = false;
    [SerializeField] private float jumpHeight = 4f;
    [SerializeField] private float jumpVelocity = 12f;
    [SerializeField] private float jumpCutMultiplier = 0.3f; // esto frena el salto si soltamos el espacio rapido
    [SerializeField] private float jumpBufferTime = 0.2f; // guarda el salto si pulsas un pelin antes de caer
    private float jumpBufferTimer = 0f;

    [Header("Caida & Gravedad")]
    [SerializeField] private float maxFallSpeed = -20f;
    [SerializeField] private float gravityNormal = 3f;
    [SerializeField] private float fallMultiplier = 2f; // cae mas rapido de lo que sube para que se sienta mejor

    [Header("Coyote Time")]
    [SerializeField] private float coyoteTime = 0.1f; // tiempo extra para saltar aunque ya no pises suelo
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
    private float wallCheckDistance = 0.1f;

    public float GetDashCooldownTimer() => dashCooldownTimer;
    public float GetMaxDashCooldown() => dashCooldown;
    public bool IsDashing() => isDashing;

    private void Start()
    {
        // buscamos los componentes al arrancar por si se nos olvido arrastrarlos
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

        animator.SetBool("Grounded", isGroundedNow);

        // esto es para las particulas cuando caemos al suelo despues de un salto
        if (isGroundedNow && !wasGrounded && rb.linearVelocity.y < -1f)
        {
            if (landingParticles != null) landingParticles.Play();
            animator.SetTrigger("Land");
        }
        wasGrounded = isGroundedNow;

        // el cronometro del coyote time (si tocas suelo se resetea)
        if (isGroundedNow) coyoteTimer = coyoteTime;
        else coyoteTimer -= Time.deltaTime;

        isTouchingWall = CheckWall();
        bool wasWallSliding = isWallSliding;
        isWallSliding = isTouchingWall && !isGroundedNow && rb.linearVelocity.y < 0.1f;
        animator.SetBool("IsWallSliding", isWallSliding);

        // si nos pegamos a la pared, giramos para mirar hacia afuera automaticamente
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

        // si pulsas salto, el buffer se activa para que el juego "recuerde" que quieres saltar
        if (Input.GetButtonDown("Jump")) jumpBufferTimer = jumpBufferTime;

        if (Input.GetKeyDown(KeyCode.LeftShift) && dashCooldownTimer <= 0f && !isDashing)
            StartCoroutine(Dash());

        // esto hace que el salto sea mas alto si dejas pulsado el boton (variable jump height)
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

        // aqui aplicamos la gravedad personalizada segun si estamos cayendo o subiendo
        if (!CheckGrounded() && !isWallSliding)
        {
            float targetGravity = gravityNormal;
            if (rb.linearVelocity.y < 0) targetGravity *= fallMultiplier; // cae mas rapido
            Vector3 extraGravityForce = Physics.gravity * (targetGravity - 1f);
            rb.AddForce(extraGravityForce, ForceMode.Acceleration);
        }

        if (!canMove) return;

        // aqui se decide si el salto es normal, con coyote time o en la pared
        if (jumpBufferTimer > 0f)
        {
            bool performedAction = false;

            // logica para saltar desde una pared
            if (enableWallJump && isTouchingWall && !CheckGrounded())
            {
                animator.SetTrigger("Jump");
                isWallJumping = true;
                Invoke(nameof(StopWallJump), wallJumpDuration);

                float jumpDirection = isFacingRight ? 1f : -1f;
                rb.linearVelocity = new Vector3(wallJumpForce.x * jumpDirection, wallJumpForce.y, 0f);

                CheckFlipImmediate(jumpDirection);
                performedAction = true;
            }
            // salto normal aprovechando el coyote timer
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

        // movimiento horizontal normal (siempre que no estemos saltando desde la pared)
        if (!isWallJumping)
        {
            float targetVelocityX = horizontal * speed;
            if (isWallSliding) targetVelocityX = 0f; // no te mueves hacia los lados si resbalas
            rb.linearVelocity = new Vector3(targetVelocityX, rb.linearVelocity.y, 0f);
        }

        // frenamos la caida si estamos resbalando por una pared
        if (isWallSliding && !isWallJumping)
        {
            if (rb.linearVelocity.y < -wallSlideSpeed)
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, -wallSlideSpeed, 0f);
        }
        else
        {
            // limitamos la velocidad maxima de caida para que no sea infinito
            float clampedY = Mathf.Clamp(rb.linearVelocity.y, maxFallSpeed, float.MaxValue);
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, clampedY, 0f);
        }
    }

    void LateUpdate()
    {
        // esto es clave para que el personaje no se escape hacia el fondo o adelante en el eje z
        transform.position = new Vector3(transform.position.x, transform.position.y, 0f);
    }

    public bool CheckGrounded()
    {
        // usamos una caja invisible (boxcast) debajo de los pies para detectar el suelo
        Vector3 center = playerCollider.bounds.center;
        Vector3 size = playerCollider.bounds.size;
        return Physics.BoxCast(center, new Vector3(size.x * 0.9f, 0.05f, size.z) / 2, Vector3.down, Quaternion.identity, (size.y / 2) + groundCheckDistance, groundLayer);
    }

    private bool CheckWall()
    {
        // lanzamos cajas a los lados para saber si hay una pared cerca
        Vector3 center = playerCollider.bounds.center;
        Vector3 size = playerCollider.bounds.size;
        bool hitRight = Physics.BoxCast(center, new Vector3(0.05f, size.y * 0.8f, size.z) / 2, Vector3.right, Quaternion.identity, (size.x / 2) + wallCheckDistance, groundLayer);
        bool hitLeft = Physics.BoxCast(center, new Vector3(0.05f, size.y * 0.8f, size.z) / 2, Vector3.left, Quaternion.identity, (size.x / 2) + wallCheckDistance, groundLayer);
        return hitRight || hitLeft;
    }

    private void Flip()
    {
        // gira el dibujo del personaje segun hacia donde caminas
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
        localScale.x *= -1f; // esto da la vuelta al sprite
        transform.localScale = localScale;
    }

    private IEnumerator Dash()
    {
        isDashing = true;
        animator.SetBool("IsDashing", true);
        rb.useGravity = false; // quitamos gravedad para que el dash sea totalmente recto

        if (dashParticles != null) dashParticles.Play();

        CameraSystem cam = Camera.main.GetComponent<CameraSystem>();
        if (cam != null) cam.ShakeDash();

        // miramos hacia donde pulsas para hacer el dash, si no pulsas nada, hacia donde miras
        Vector3 dashDirection = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, 0f).normalized;
        if (dashDirection == Vector3.zero) dashDirection = isFacingRight ? Vector3.right : Vector3.left;

        rb.linearVelocity = dashDirection * dashForce;

        yield return new WaitForSeconds(dashDuration);

        if (dashParticles != null) dashParticles.Stop();

        rb.useGravity = true;
        isDashing = false;
        animator.SetBool("IsDashing", false);
        dashCooldownTimer = dashCooldown;
        rb.linearVelocity = Vector3.zero; // frenazo en seco al terminar el impulso
    }

    private void StopWallJump() => isWallJumping = false;
    public bool IsWallSliding() => isWallSliding;
    public void SetCanMove(bool state) => canMove = state;
    public bool IsFacingRight() => isFacingRight;
}