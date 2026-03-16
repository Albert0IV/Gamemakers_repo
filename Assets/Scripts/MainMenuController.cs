using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    public GameObject nivelButtonPrefab;
    public Transform buttonContainer;

    private string[] levelNames = { "Tutorial", "Intermedio", "Dificil" };

    void Start()
    {
        GenerateLevelButtons();
    }

    void GenerateLevelButtons()
    {
        for (int i = 0; i < levelNames.Length; i++)
        {
            GameObject buttonObj = Instantiate(nivelButtonPrefab, buttonContainer);
            Button btn = buttonObj.GetComponent<Button>();
            buttonObj.GetComponentInChildren<TextMeshProUGUI>().text = levelNames[i];

            int levelNumber = i + 1;
            bool isLocked = (levelNumber == 3 && PlayerPrefs.GetInt("Nivel_3_Unlocked", 0) == 0);

            if (isLocked)
            {
                btn.interactable = false;
                buttonObj.GetComponentInChildren<TextMeshProUGUI>().text += " <color=red>(LOCKED)</color>";
            }
            else
            {
                btn.onClick.AddListener(() => SceneManager.LoadScene("Nivel_" + levelNumber));
            }
        }
    }

    public void QuitGame() => Application.Quit();
}