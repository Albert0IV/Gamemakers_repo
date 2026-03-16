using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelController : MonoBehaviour
{
    [Header("Menús UI")]
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject victoryMenu;

    [Header("Referencias de Escena")]
    [Tooltip("Arrastra aquí el objeto que servirá como meta (opcional)")]
    [SerializeField] private GameObject goalObject;

    private bool gameEnded = false;

    void Start()
    {
        Time.timeScale = 1f;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && !gameEnded)
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        bool isPaused = !pauseMenu.activeSelf;
        pauseMenu.SetActive(isPaused);
        Time.timeScale = isPaused ? 0f : 1f;

        Cursor.visible = isPaused;
        Cursor.lockState = isPaused ? CursorLockMode.None : CursorLockMode.Locked;
    }

    // Esta es la función que activará el trigger
    public void WinLevel()
    {
        if (gameEnded) return;
        gameEnded = true;

        victoryMenu.SetActive(true);
        Time.timeScale = 0f;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // 1. Detener tiempo y procesar récords
        TimeManager tm = FindFirstObjectByType<TimeManager>();
        if (tm != null)
        {
            float finalTime = tm.StopTimer();
            GetComponent<ScoreSystem>().ProcessScore(finalTime);
        }

        // 2. Desbloquear siguiente nivel
        string current = SceneManager.GetActiveScene().name;
        if (current == "Nivel_1") PlayerPrefs.SetInt("Nivel_2_Unlocked", 1);
        if (current == "Nivel_2") PlayerPrefs.SetInt("Nivel_3_Unlocked", 1);
        PlayerPrefs.Save();
    }

    public void Restart() => SceneManager.LoadScene(SceneManager.GetActiveScene().name);

    public void NextLevel()
    {
        string currentName = SceneManager.GetActiveScene().name;
        int nextIdx = int.Parse(currentName.Replace("Nivel_", "")) + 1;

        if (nextIdx <= 3) SceneManager.LoadScene("Nivel_" + nextIdx);
        else SceneManager.LoadScene("MainMenu");
    }

    public void MainMenu() => SceneManager.LoadScene("MainMenu");
}