using UnityEngine;

public class TutorialTrigger : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("El número del mensaje en la lista del TutorialManager")]
    [SerializeField] private int messageIndex;

    private bool hasBeenActivated = false;

    private void OnTriggerEnter(Collider other)
    {
        // Comprobar Tag "Player" y que no se haya usado ya
        if (other.CompareTag("Player") && !hasBeenActivated)
        {
            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.ShowTutorialStep(messageIndex);
                hasBeenActivated = true; // Bloqueamos para que no se repita
            }
        }
    }
}