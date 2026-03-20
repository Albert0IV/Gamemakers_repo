using UnityEngine;

public class CameraSystem : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform target;
    private PlayerController playerController;

    [Header("Seguimiento")]
    [SerializeField] private float smoothSpeed = 10f;
    [SerializeField] private Vector3 offset = new Vector3(0, 2, -10);

    [Header("Anticipación")]
    [SerializeField] private float lookAheadDistance = 4f;
    [SerializeField] private float lookAheadSpeed = 2.5f;
    [SerializeField] private float wallSlideLookAheadMultiplier = 1.5f; // Cuánto más lejos mira en la pared

    [Header("Ajustes de Screen Shake")]
    public float pogoMagnitude = 0.3f;
    public float pogoDuration = 0.1f;
    public float damageMagnitude = 0.7f;
    public float damageDuration = 0.4f;
    public float dashMagnitude = 0.15f;
    public float dashDuration = 0.1f;

    private float shakeTimeRemaining;
    private float currentShakeMagnitude;
    private Vector3 currentShakeOffset;

    private float currentLookAheadX;
    private float lastDirectionX = 1;

    void Start()
    {
        // Intentamos obtener el componente automáticamente si el target es el jugador
        if (target != null) playerController = target.GetComponent<PlayerController>();
    }

    // Métodos públicos para disparar el Shake desde otros scripts
    public void ShakePogo() { TriggerShake(pogoDuration, pogoMagnitude); }
    public void ShakeDamage() { TriggerShake(damageDuration, damageMagnitude); }
    public void ShakeDash() { TriggerShake(dashDuration, dashMagnitude); }

    private void TriggerShake(float duration, float magnitude)
    {
        shakeTimeRemaining = duration;
        currentShakeMagnitude = magnitude;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 1. DETERMINAR DIRECCIÓN Y DISTANCIA DE ENFOQUE (Look Ahead)
        float moveInputX = Input.GetAxisRaw("Horizontal");
        float finalLookAheadDistance = lookAheadDistance;

        // EXCEPCIÓN DE WALLSLIDE:
        if (playerController != null && playerController.IsWallSliding())
        {
            // Forzamos la dirección hacia donde mira el personaje (hacia afuera de la pared)
            lastDirectionX = playerController.IsFacingRight() ? 1 : -1;

            // Aplicamos el multiplicador para ver más lejos en situaciones de pared
            finalLookAheadDistance *= wallSlideLookAheadMultiplier;
        }
        else if (moveInputX != 0)
        {
            // Comportamiento normal por input
            lastDirectionX = moveInputX > 0 ? 1 : -1;
        }

        // Suavizamos el desplazamiento horizontal (Interpolar hacia el objetivo)
        float targetLookAhead = lastDirectionX * finalLookAheadDistance;
        currentLookAheadX = Mathf.Lerp(currentLookAheadX, targetLookAhead, Time.deltaTime * lookAheadSpeed);

        // 2. Lógica de Screen Shake
        if (shakeTimeRemaining > 0)
        {
            shakeTimeRemaining -= Time.deltaTime;
            Vector2 randomPoint = Random.insideUnitCircle * currentShakeMagnitude;
            currentShakeOffset = new Vector3(randomPoint.x, randomPoint.y, 0);
        }
        else
        {
            currentShakeOffset = Vector3.zero;
        }

        // 3. CÁLCULO DE POSICIÓN FINAL
        Vector3 desiredPos = target.position + offset;
        desiredPos.x += currentLookAheadX;

        // Suavizamos el movimiento de la cámara hacia la posición deseada
        Vector3 smoothedPos = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);

        // Aplicamos la posición + el efecto de sacudida
        transform.position = smoothedPos + currentShakeOffset;

        // Bloqueamos rotación
        transform.rotation = Quaternion.identity;
    }
}