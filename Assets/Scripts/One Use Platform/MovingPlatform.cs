using UnityEngine;
using System.Collections;

public class MovingPlatform : MonoBehaviour
{
    [Header("Ajustes de Movimiento")]
    [SerializeField] private float moveDistance = 5f; // Distancia a subir
    [SerializeField] private float duration = 2f;     // Tiempo que tarda en llegar

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private bool hasStarted = false;

    void Start()
    {
        startPosition = transform.position;
        targetPosition = startPosition + Vector3.up * moveDistance;
    }

    public void StartMoving()
    {
        if (!hasStarted)
        {
            StartCoroutine(MoveRoutine());
        }
    }

    private IEnumerator MoveRoutine()
    {
        hasStarted = true;
        float elapsed = 0;

        while (elapsed < duration)
        {
            // Movimiento suave (Lerp)
            transform.position = Vector3.Lerp(startPosition, targetPosition, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPosition; // Asegurar posición final exacta
    }
}