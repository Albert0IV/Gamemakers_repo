using UnityEngine;
using System.Collections;

public class ElevatorController : MonoBehaviour
{
    [Header("Niveles")]
    [SerializeField] private Transform bottomPoint;
    [SerializeField] private Transform topPoint;
    [SerializeField] private float travelSpeed = 4f;

    [Header("Estado Inicial")]
    [SerializeField] private bool startAtTop = true; // Marcado por defecto

    private bool isAtTop;
    private bool isMoving = false;
    public bool IsMoving => isMoving;

    void Start()
    {
        // Configuramos la posición inicial basada en el booleano
        if (startAtTop)
        {
            transform.position = topPoint.position;
            isAtTop = true;
        }
        else
        {
            transform.position = bottomPoint.position;
            isAtTop = false;
        }
    }

    public void CallElevator(bool callerIsTop)
    {
        if (isMoving || isAtTop == callerIsTop) return;
        StartCoroutine(MoveToFloor(callerIsTop));
    }

    public void ToggleFloor()
    {
        if (isMoving) return;
        StartCoroutine(MoveToFloor(!isAtTop));
    }

    private IEnumerator MoveToFloor(bool goToTop)
    {
        isMoving = true;
        Vector3 targetPos = goToTop ? topPoint.position : bottomPoint.position;

        while (Vector3.Distance(transform.position, targetPos) > 0.001f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, travelSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = targetPos;
        isAtTop = goToTop;
        isMoving = false;
    }
}