using UnityEngine;

public class VictoryTrigger : MonoBehaviour
{
    [SerializeField] private LevelController levelController;

    private void Start()
    {
        // Si no asignaste el controlador manualmente, intenta buscarlo
        if (levelController == null)
            levelController = FindFirstObjectByType<LevelController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Verificamos que sea el jugador quien entra
        if (other.CompareTag("Player"))
        {
            levelController.WinLevel();
        }
    }
}