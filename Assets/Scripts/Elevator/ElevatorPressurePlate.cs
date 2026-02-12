using UnityEngine;
using System.Collections;

public class ElevatorPressurePlate : MonoBehaviour
{
    [SerializeField] private float activationDelay = 0.5f;
    [SerializeField] private float sinkDistance = 0.1f;
    [SerializeField] private float sinkSpeed = 5f;

    private Vector3 initialLocalPos;
    private Vector3 sunkenLocalPos;
    private bool playerOnTop = false;
    private ElevatorController elevator;

    private Coroutine logicRoutine;
    private Coroutine moveRoutine;

    void Start()
    {
        // Guardamos la posición local (relativa al ascensor)
        initialLocalPos = transform.localPosition;
        sunkenLocalPos = initialLocalPos + Vector3.down * sinkDistance;
        elevator = GetComponentInParent<ElevatorController>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            playerOnTop = true;
            collision.gameObject.transform.SetParent(transform);

            if (logicRoutine != null) StopCoroutine(logicRoutine);
            logicRoutine = StartCoroutine(PressureSequence());
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            playerOnTop = false;
            collision.gameObject.transform.SetParent(null);

            if (logicRoutine != null) StopCoroutine(logicRoutine);

            if (moveRoutine != null) StopCoroutine(moveRoutine);
            moveRoutine = StartCoroutine(AnimatePlate(initialLocalPos));
        }
    }

    private IEnumerator PressureSequence()
    {
        if (moveRoutine != null) StopCoroutine(moveRoutine);
        moveRoutine = StartCoroutine(AnimatePlate(sunkenLocalPos));

        yield return new WaitForSeconds(activationDelay);

        if (playerOnTop && elevator != null && !elevator.IsMoving)
        {
            elevator.ToggleFloor();
        }
    }

    private IEnumerator AnimatePlate(Vector3 targetPos)
    {
        while (Vector3.Distance(transform.localPosition, targetPos) > 0.001f)
        {
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, targetPos, sinkSpeed * Time.deltaTime);
            yield return null;
        }
        transform.localPosition = targetPos;
    }
}