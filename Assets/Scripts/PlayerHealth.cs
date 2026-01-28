using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
public class PlayerHealth : MonoBehaviour
{
    [Header("vidas")] //titulo para agrupar variables de vida
    [SerializeField] private int maxLives = 3; //cantidad maxima de vidas
    private int currentLives; //vidas actuales

    [Header("ui de vidas")] //titulo para agrupar elementos visuales
    [SerializeField] private GameObject lifeIconPrefab; //prefab del icono de vida
    [SerializeField] private Transform lifePanel; //panel donde se colocan los iconos
    private List<GameObject> lifeIcons = new List<GameObject>(); //lista de iconos en pantalla

    void Start()
    {
        currentLives = maxLives; //inicia con vidas completas
        UpdateLivesUI(); //actualiza la interfaz de vidas
    }

    public void TakeDamage(int damageAmount) //metodo para recibir daño
    {
        currentLives -= damageAmount; //resta vidas
        currentLives = Mathf.Max(currentLives, 0); //evita que sea menor a cero
        UpdateLivesUI(); //actualiza la interfaz

        if (currentLives <= 0) //si se queda sin vidas
        {
            currentLives = maxLives; //reinicia vidas
            UpdateLivesUI(); //actualiza interfaz
        }
    }

    private void UpdateLivesUI() //actualiza los iconos visuales
    {
        foreach (Transform child in lifePanel) //elimina iconos anteriores
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < currentLives; i++) //crea nuevos iconos segun vidas
        {
            Instantiate(lifeIconPrefab, lifePanel);
        }
    }

}