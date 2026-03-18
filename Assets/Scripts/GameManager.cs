using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Configuración de Usuario")]
    public static string PlayerName = "Invitado";
    [SerializeField] private TMP_InputField nameInput;

    [Header("Configuración de Reset (Seguridad)")]
    [Tooltip("Tiempo en segundos que hay que mantener Shift + Z para borrar todo.")]
    [SerializeField] private float timeToReset = 3.0f;
    private float currentResetTimer = 0f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (PlayerPrefs.HasKey("LastPlayerName"))
        {
            PlayerName = PlayerPrefs.GetString("LastPlayerName");
            if (nameInput != null) nameInput.text = PlayerName;
        }
    }

    private void Update()
    {
        HandleResetInput();
    }

    private void HandleResetInput()
    {
        // CAMBIO: Ahora detecta la tecla Z
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.Z))
        {
            currentResetTimer += Time.deltaTime;

            if (currentResetTimer > 0.1f)
            {
                float porcentaje = (currentResetTimer / timeToReset) * 100f;
                Debug.Log($"<color=cyan>RESETEANDO SCOREBOARD: {porcentaje:0}%...</color>");
            }

            if (currentResetTimer >= timeToReset)
            {
                ExecuteFullReset();
                currentResetTimer = 0f;
            }
        }
        else
        {
            currentResetTimer = 0f;
        }
    }

    private void ExecuteFullReset()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        Debug.Log("<color=white><b>DATOS ELIMINADOS:</b> Reiniciando nivel...</color>");

        // Recargamos la escena para que las tablas de puntuación se vean vacías
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void SaveName(string input)
    {
        if (!string.IsNullOrEmpty(input))
        {
            PlayerName = input;
            PlayerPrefs.SetString("LastPlayerName", PlayerName);
            PlayerPrefs.Save();
        }
    }
}