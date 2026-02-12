using UnityEngine;
using System.Collections;

public class ElevatorLever : MonoBehaviour, IDamageable
{
    [SerializeField] private ElevatorController elevator;
    [SerializeField] private bool isTopLever; // Marcar en el inspector si esta palanca está arriba
    [SerializeField] private Renderer leverRenderer;
    [SerializeField] private Color feedbackColor = Color.cyan;

    private Color originalColor;

    void Start()
    {
        if (leverRenderer != null) originalColor = leverRenderer.material.color;
    }

    public void TakeDamage(int damage, Vector3 attackerPos)
    {
        if (elevator != null)
        {
            elevator.CallElevator(isTopLever);
            StartCoroutine(FlashFeedback());
        }
    }

    private IEnumerator FlashFeedback()
    {
        if (leverRenderer == null) yield break;
        leverRenderer.material.color = feedbackColor;
        yield return new WaitForSeconds(0.5f);
        leverRenderer.material.color = originalColor;
    }
}