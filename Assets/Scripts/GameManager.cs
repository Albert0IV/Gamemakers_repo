using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    // El nombre es estático para que sea el mismo en todas las escenas
    public static string PlayerName = "Invitado";
    [SerializeField] private TMP_InputField nameInput;

    private void Start()
    {
        // Recuperar el último nombre guardado al abrir el juego
        if (PlayerPrefs.HasKey("LastPlayerName"))
        {
            PlayerName = PlayerPrefs.GetString("LastPlayerName");
            if (nameInput != null) nameInput.text = PlayerName;
            Debug.Log("Nombre cargado de memoria: " + PlayerName);
        }
    }

    // Esta es la función que debes conectar al On Value Changed (Dynamic) del InputField
    public void SaveName(string input)
    {
        if (!string.IsNullOrEmpty(input))
        {
            PlayerName = input;
            PlayerPrefs.SetString("LastPlayerName", PlayerName);
            Debug.Log("Nombre guardado en GameManager: " + PlayerName);
        }
    }
}