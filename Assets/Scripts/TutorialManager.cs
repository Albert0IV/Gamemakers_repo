using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    [Header("Referencias UI")]
    [SerializeField] private TextMeshProUGUI infoText;

    [Header("Ajustes de Tiempo")]
    [SerializeField] private float typingSpeed = 0.05f;
    [SerializeField] private float displayDuration = 3f;
    [SerializeField] private float fadeDuration = 1f;

    [Header("Base de Datos de Mensajes")]
    [TextArea(3, 5)]
    [SerializeField] private List<string> tutorialMessages = new List<string>();

    private Coroutine tutorialCoroutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Al inicio, nos aseguramos de que el texto esté vacío y transparente
        if (infoText != null)
        {
            infoText.text = "";
            SetTextAlpha(0);

            // OPCIONAL: Si quieres que el objeto esté desactivado del todo:
            // infoText.gameObject.SetActive(false);
        }
    }

    public void ShowTutorialStep(int index)
    {
        if (index >= 0 && index < tutorialMessages.Count)
        {
            // ACTIVAR EL OBJETO: Por si estaba desactivado en el Inspector
            if (!infoText.gameObject.activeSelf)
            {
                infoText.gameObject.SetActive(true);
            }

            if (tutorialCoroutine != null) StopCoroutine(tutorialCoroutine);
            tutorialCoroutine = StartCoroutine(TypeAndFade(tutorialMessages[index]));
        }
    }

    private IEnumerator TypeAndFade(string fullText)
    {
        // 1. Resetear visibilidad
        infoText.text = "";
        SetTextAlpha(1);

        // 2. Efecto Máquina de Escribir
        foreach (char letter in fullText.ToCharArray())
        {
            infoText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        // 3. Esperar tiempo de lectura
        yield return new WaitForSeconds(displayDuration);

        // 4. Fade Out
        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            SetTextAlpha(Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration));
            yield return null;
        }

        infoText.text = "";

        // OPCIONAL: Desactivar el objeto al terminar para ahorrar procesos
        // infoText.gameObject.SetActive(false);
    }

    // Función auxiliar para no repetir código de color
    private void SetTextAlpha(float alpha)
    {
        Color c = infoText.color;
        c.a = alpha;
        infoText.color = c;
    }
}