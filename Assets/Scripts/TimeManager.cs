using UnityEngine;
using TMPro;

public class TimeManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI recordText;

    private float currentTime;
    private float bestTime;
    private bool isRunning = true;
    private bool isCountdown = false;
    private string levelKey;

    void Start()
    {
        levelKey = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        // Buscamos el mejor tiempo (el índice 0 de la tabla)
        bestTime = PlayerPrefs.GetFloat(levelKey + "_Best_0_Time", 9999f);

        if (bestTime >= 9999f)
        {
            // Caso: No hay récord
            recordText.text = "RECORD: --:--";
            currentTime = 0;
            isCountdown = false;
        }
        else
        {
            // Caso: Sí hay récord, activamos contrarreloj
            recordText.text = "RECORD: " + FormatTime(bestTime);
            currentTime = bestTime; // Empezamos desde el tiempo del récord
            isCountdown = true;
        }
    }

    void Update()
    {
        if (isRunning)
        {
            if (isCountdown)
            {
                // Restamos tiempo para la contrarreloj
                currentTime -= Time.deltaTime;
            }
            else
            {
                // Sumamos tiempo si no hay récord
                currentTime += Time.deltaTime;
            }

            timerText.text = FormatTime(currentTime);

            // Cambiar color a rojo si estamos en tiempo negativo
            if (currentTime < 0)
            {
                timerText.color = Color.red;
            }
        }
    }

    public float StopTimer()
    {
        isRunning = false;

        // Si estábamos en contrarreloj, el tiempo real transcurrido es:
        // RecordOriginal - TiempoRestante
        if (isCountdown)
        {
            return bestTime - currentTime;
        }
        else
        {
            return currentTime;
        }
    }

    public string FormatTime(float time)
    {
        // Detectar si el tiempo es negativo para añadir el signo
        string sign = (time < 0) ? "-" : "";
        float absoluteTime = Mathf.Abs(time);

        int minutes = Mathf.FloorToInt(absoluteTime / 60);
        int seconds = Mathf.FloorToInt(absoluteTime % 60);
        int fraction = Mathf.FloorToInt((absoluteTime * 100) % 100);

        return string.Format("{0}{1:00}:{2:00}:{3:00}", sign, minutes, seconds, fraction);
    }
}