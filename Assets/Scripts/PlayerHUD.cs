using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private PlayerController controller;
    [SerializeField] private PlayerCombat combat;

    [Header("UI Dash (La imagen con Fill)")]
    [SerializeField] private Image dashFilledImage;

    [Header("UI Pelota (La imagen con Fill)")]
    [SerializeField] private Image ballFilledImage;

    private void Start()
    {
        if (controller == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                controller = player.GetComponent<PlayerController>();
                combat = player.GetComponent<PlayerCombat>();
            }
        }
    }

    private void Update()
    {
        if (controller != null) UpdateDashUI();
        if (combat != null) UpdateBallUI();
    }

    private void UpdateDashUI()
    {
        if (dashFilledImage == null) return;

        float cd = controller.GetDashCooldownTimer();
        float max = controller.GetMaxDashCooldown();

        // El fillAmount sube de 0 a 1. 
        // Mientras carga, solo se ve una parte del color "Listo".
        // El resto deja ver la imagen de fondo (el color de cooldown).
        dashFilledImage.fillAmount = (cd <= 0) ? 1f : 1f - (cd / max);
    }

    private void UpdateBallUI()
    {
        if (ballFilledImage == null || combat == null) return;

        // Si puede disparar, fill al 100% (tapa el fondo).
        // Si no, fill al 0% (se ve solo el fondo).
        ballFilledImage.fillAmount = combat.CanShoot() ? 1f : 0f;
    }
}