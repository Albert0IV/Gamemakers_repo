using UnityEngine;

public class CheckpointTrigger : MonoBehaviour
{
    [Tooltip("Asigna un número: 1 para el primero, 2 para el segundo, etc.")]
    public int checkpointIndex;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth health = other.GetComponent<PlayerHealth>();
            if (health != null)
            {
                // Le enviamos la posición Y el índice
                health.SetCheckpoint(transform.position, checkpointIndex);
            }
        }
    }
}