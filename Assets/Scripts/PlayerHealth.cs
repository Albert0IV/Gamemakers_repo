using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.SceneManagement; // IMPORTANTE: Necesario para reiniciar la escena

public class PlayerHealth : MonoBehaviour
{
    [Header("vidas")]
    [SerializeField] private int maxLives = 3;
    private int currentLives;

    [Header("ui de vidas")]
    [SerializeField] private GameObject lifeIconPrefab;
    [SerializeField] private Transform lifePanel;
    private List<GameObject> lifeIcons = new List<GameObject>();

    void Start()
    {
        currentLives = maxLives;
        UpdateLivesUI();
    }

    public void TakeDamage(int damageAmount)
    {
        currentLives -= damageAmount;
        currentLives = Mathf.Max(currentLives, 0);
        UpdateLivesUI();

        if (currentLives <= 0)
        {
            // En lugar de reiniciar las variables, recargamos la escena completa
            RestartScene();
        }
    }

    private void UpdateLivesUI()
    {
        foreach (Transform child in lifePanel)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < currentLives; i++)
        {
            Instantiate(lifeIconPrefab, lifePanel);
        }
    }

    // Nueva función para reiniciar la escena
    private void RestartScene()
    {
        // Obtiene la escena activa actual y la vuelve a cargar
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}