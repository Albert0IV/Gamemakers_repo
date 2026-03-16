using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class ScoreSystem : MonoBehaviour
{
    [SerializeField] private GameObject newRecordLabel;
    [SerializeField] private TextMeshProUGUI highscoreTableText;

    [System.Serializable]
    public class ScoreEntry { public string name; public float time; }

    public void ProcessScore(float finalTime)
    {
        string levelKey = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + "_Best";
        List<ScoreEntry> scores = new List<ScoreEntry>();

        // 1. Cargar Top 5 actual
        for (int i = 0; i < 5; i++)
        {
            if (PlayerPrefs.HasKey(levelKey + "_" + i + "_Time"))
            {
                scores.Add(new ScoreEntry
                {
                    name = PlayerPrefs.GetString(levelKey + "_" + i + "_Name"),
                    time = PlayerPrefs.GetFloat(levelKey + "_" + i + "_Time")
                });
            }
        }

        // 2. ¿Es récord absoluto? (Primer puesto)
        bool isNewBest = (scores.Count == 0 || finalTime < scores[0].time);
        if (newRecordLabel != null) newRecordLabel.SetActive(isNewBest);

        // 3. Añadir el nombre que tiene el GameManager ACTUAL
        Debug.Log("Procesando record para: " + GameManager.PlayerName);
        scores.Add(new ScoreEntry { name = GameManager.PlayerName, time = finalTime });

        // 4. Ordenar (menor tiempo primero)
        scores.Sort((a, b) => a.time.CompareTo(b.time));

        // 5. Guardar y mostrar Top 5
        int count = Mathf.Min(scores.Count, 5);
        highscoreTableText.text = "<color=#FFFF00>TOP 5 RECORDS</color>\n\n";

        for (int i = 0; i < count; i++)
        {
            PlayerPrefs.SetString(levelKey + "_" + i + "_Name", scores[i].name);
            PlayerPrefs.SetFloat(levelKey + "_" + i + "_Time", scores[i].time);

            highscoreTableText.text += $"{i + 1}. {scores[i].name.ToUpper()} - {FormatTime(scores[i].time)}\n";
        }
        PlayerPrefs.Save();
    }

    string FormatTime(float t) => string.Format("{0:00}:{1:00}:{2:00}", Mathf.FloorToInt(t / 60), Mathf.FloorToInt(t % 60), Mathf.FloorToInt((t * 100) % 100));
}