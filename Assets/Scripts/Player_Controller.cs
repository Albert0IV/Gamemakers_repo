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

    [Header("Componentes")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Animator animator;
    [SerializeField] private Collider playerCollider;
    [SerializeField] private LayerMask groundLayer;

    private float groundCheckDistance = 0.05f;
    private float wallCheckDistance = 0.05f;

    // --- NUEVAS FUNCIONES PARA EL HUD ---
    public float GetDashCooldownTimer() => dashCooldownTimer;
    public float GetMaxDashCooldown() => dashCooldown;
    public bool IsDashing() => isDashing;

    private void Start()
    {
        if (playerCollider == null) playerCollider = GetComponent<Collider>();
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (transform.localScale.x < 0) isFacingRight = false;
    }

    void Update()
    {
        if (dashCooldownTimer > 0f) dashCooldownTimer -= Time.deltaTime;

        bool isGroundedNow = CheckGrounded();
        if (isGroundedNow) coyoteTimer = coyoteTime;
        else coyoteTimer -= Time.deltaTime;

        isTouchingWall = CheckWall();
        isWallSliding = isTouchingWall && !isGroundedNow && rb.linearVelocity.y < 0.1f;

        if (!canMove)
        {
            horizontal = 0f;
            return;
        }

        horizontal = Input.GetAxisRaw("Horizontal");

        if (Input.GetButtonDown("Jump")) jumpBufferTimer = jumpBufferTime;

        if (Input.GetKeyDown(KeyCode.LeftShift) && dashCooldownTimer <= 0f && !isDashing)
            StartCoroutine(Dash());

        if (Input.GetButtonUp("Jump") && rb.linearVelocity.y > 0f)
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier, 0f);

        Flip();
    }

    private void FixedUpdate()
    {
        if (isDashing) return;

        if (!CheckGrounded() && !isWallSliding)
        {
            float targetGravity = gravityNormal;
            if (rb.linearVelocity.y < 0) targetGravity *= fallMultiplier;
            Vector3 extraGravityForce = Physics.gravity * (targetGravity - 1f);
            rb.AddForce(extraGravityForce, ForceMode.Acceleration);
        }

        if (!canMove) return;

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
                performedAction = true;
            }
            else if (coyoteTimer > 0f)
            {
                float jumpingPower = useJumpVelocity ? jumpVelocity : Mathf.Sqrt(2f * Mathf.Abs(Physics.gravity.y * gravityNormal) * jumpHeight);
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpingPower, 0f);
                performedAction = true;
            }

            if (performedAction) jumpBufferTimer = 0f;
            else jumpBufferTimer -= Time.fixedDeltaTime;
        }

        if (!isWallJumping)
        {
            float targetVelocityX = horizontal * speed;
            if (isWallSliding)
            {
                if ((isFacingRight && horizontal > 0) || (!isFacingRight && horizontal < 0)) targetVelocityX = isFacingRight ? 0.5f : -0.5f;
            }
            rb.linearVelocity = new Vector3(targetVelocityX, rb.linearVelocity.y, 0f);
        }

        if (isWallSliding && !isWallJumping)
        {
            if (rb.linearVelocity.y < -wallSlideSpeed) rb.linearVelocity = new Vector3(rb.linearVelocity.x, -wallSlideSpeed, 0f);
        }
        else
        {
            float clampedY = Mathf.Clamp(rb.linearVelocity.y, maxFallSpeed, float.MaxValue);
            if (rb.linearVelocity.y < clampedY) rb.linearVelocity = new Vector3(rb.linearVelocity.x, clampedY, 0f);
        }
    }

    void LateUpdate()
    {
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
        Vector3 direction = isFacingRight ? Vector3.right : Vector3.left;
        return Physics.BoxCast(center, new Vector3(0.05f, size.y * 0.8f, size.z) / 2, direction, Quaternion.identity, (size.x / 2) + wallCheckDistance, groundLayer);
    }

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

    public void SetCanMove(bool state) => canMove = state;
    public bool IsFacingRight()
    {
        return isFacingRight;
    }
}