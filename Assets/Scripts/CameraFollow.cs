using UnityEngine;

public class CameraSystem : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform target;

    [Header("Seguimiento")]
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private Vector3 offset = new Vector3(0, 2, -10);

    [Header("Anticipación Horizontal")]
    [SerializeField] private float lookAheadDistance = 4f;
    [SerializeField] private float lookAheadSpeed = 2.5f;

    [Header("Mirar Arriba/Abajo (W/S)")]
    [SerializeField] private float lookUpDistance = 6f;
    [SerializeField] private float lookDownDistance = 4f;
    [SerializeField] private float idleWaitBeforeLook = 1f; // El segundo de quietud que pides
    [SerializeField] private float verticalWaitTime = 2f;    // Los 2 segundos de mantener W/S
    [SerializeField] private float verticalSmoothSpeed = 3f;

    private float currentLookAheadX;
    private float currentLookVerticalY;
    private float verticalTimer;
    private float idleTimer; // Contador para la quietud
    private float lastDirectionX = 1;

    void LateUpdate()
    {
        if (target == null) return;

        // 1. OBTENER INPUT
        float moveInputX = Input.GetAxisRaw("Horizontal");
        float moveInputY = Input.GetAxisRaw("Vertical");

        // 2. LÓGICA HORIZONTAL
        if (moveInputX != 0)
        {
            lastDirectionX = moveInputX > 0 ? 1 : -1;
            idleTimer = 0; // Si se mueve horizontalmente, reseteamos la quietud
        }

        float targetLookAheadX = lastDirectionX * lookAheadDistance;
        currentLookAheadX = Mathf.Lerp(currentLookAheadX, targetLookAheadX, Time.deltaTime * lookAheadSpeed);

        // 3. LÓGICA DE QUIETUD Y VERTICAL
        // Primero: ¿El jugador está quieto horizontalmente?
        if (moveInputX == 0)
        {
            idleTimer += Time.deltaTime;
        }
        else
        {
            idleTimer = 0;
            verticalTimer = 0;
        }

        float targetVerticalY = 0;

        // Solo si ha estado quieto más del tiempo de reposo (1s), permitimos contar el tiempo de mirada (2s)
        if (idleTimer >= idleWaitBeforeLook)
        {
            if (moveInputY != 0)
            {
                verticalTimer += Time.deltaTime;
            }
            else
            {
                verticalTimer = 0;
            }

            if (verticalTimer >= verticalWaitTime)
            {
                targetVerticalY = (moveInputY > 0) ? lookUpDistance : -lookDownDistance;
            }
        }
        else
        {
            verticalTimer = 0; // Si no ha pasado el segundo de reposo, el contador de W/S no sube
        }

        currentLookVerticalY = Mathf.Lerp(currentLookVerticalY, targetVerticalY, Time.deltaTime * verticalSmoothSpeed);

        // 4. POSICIÓN FINAL
        Vector3 desiredPosition = target.position + offset;
        desiredPosition.x += currentLookAheadX;
        desiredPosition.y += currentLookVerticalY;

        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.rotation = Quaternion.identity;
    }
}