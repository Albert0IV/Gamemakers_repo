using UnityEngine;

public class CameraSystem : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform target;

    [Header("Seguimiento")]
    [SerializeField] private float smoothSpeed = 10f; // Aumentado para mayor respuesta
    [SerializeField] private Vector3 offset = new Vector3(0, 2, -10);

    [Header("Anticipación")]
    [SerializeField] private float lookAheadDistance = 4f;
    [SerializeField] private float lookAheadSpeed = 2.5f;

    [Header("Ajustes de Screen Shake")]
    // Asegúrate de que estos valores NO sean 0 en el Inspector
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

    // Métodos públicos
    public void ShakePogo() { TriggerShake(pogoDuration, pogoMagnitude); }
    public void ShakeDamage() { TriggerShake(damageDuration, damageMagnitude); }
    public void ShakeDash() { TriggerShake(dashDuration, dashMagnitude); }

    private void TriggerShake(float duration, float magnitude)
    {
        shakeTimeRemaining = duration;
        currentShakeMagnitude = magnitude;
        Debug.Log("SHAKE DISPARADO: Mag " + magnitude + " Dur " + duration);
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 1. Lógica de Seguimiento Horizontal
        float moveInputX = Input.GetAxisRaw("Horizontal");
        if (moveInputX != 0) lastDirectionX = moveInputX > 0 ? 1 : -1;

        currentLookAheadX = Mathf.Lerp(currentLookAheadX, lastDirectionX * lookAheadDistance, Time.deltaTime * lookAheadSpeed);

        // 2. Lógica de Shake (Cálculo del Offset)
        if (shakeTimeRemaining > 0)
        {
            shakeTimeRemaining -= Time.deltaTime;

            // Usamos InsideUnitCircle para que solo afecte a X e Y (2D) y no a la profundidad Z
            Vector2 randomPoint = Random.insideUnitCircle * currentShakeMagnitude;
            currentShakeOffset = new Vector3(randomPoint.x, randomPoint.y, 0);
        }
        else
        {
            currentShakeOffset = Vector3.zero;
        }

        // 3. Aplicación Final
        // Calculamos a dónde debería ir la cámara
        Vector3 desiredPos = target.position + offset;
        desiredPos.x += currentLookAheadX;

        // Suavizamos el movimiento hacia esa posición
        Vector3 smoothedPos = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);

        // IMPORTANTE: Sumamos el shake DESPUÉS del Lerp para que el suavizado no lo anule
        transform.position = smoothedPos + currentShakeOffset;

        // Bloqueamos la rotación para que no baile
        transform.rotation = Quaternion.identity;
    }
}