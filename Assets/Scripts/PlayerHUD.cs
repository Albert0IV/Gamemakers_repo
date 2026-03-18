using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private PlayerController controller;
    [SerializeField] private PlayerCombat combat;

    [Header("UI Dash")]
    [SerializeField] private Image dashFilledImage;
    [SerializeField] private RectTransform dashBackgroundImage;
    [SerializeField] private float dashPopMultiplier = 1.3f; // Cuánto crece respecto a su base
    [SerializeField] private Color chargingColorDash = new Color(1f, 1f, 1f, 0.4f);
    [SerializeField] private Color readyColorDash = Color.cyan;

    [Header("UI Pelota")]
    [SerializeField] private Image ballFilledImage;
    [SerializeField] private RectTransform ballBackgroundImage;
    [SerializeField] private float ballPopMultiplier = 1.4f; // Cuánto crece respecto a su base
    [SerializeField] private Color chargingColorBall = new Color(1f, 1f, 1f, 0.4f);
    [SerializeField] private Color readyColorBall = Color.white;

    [Header("Ajustes Globales")]
    [SerializeField] private float animationSpeed = 8f;

    // Escalas originales del Inspector
    private Vector3 dashMainBaseScale, dashBgBaseScale;
    private Vector3 ballMainBaseScale, ballBgBaseScale;

    private bool dashWasReady;
    private bool ballWasReady;
    private bool isInitialized = false;

    private void Awake()
    {
        // Guardamos las escalas que pusiste en el Inspector para que sean el "punto de retorno"
        if (dashFilledImage != null) dashMainBaseScale = dashFilledImage.rectTransform.localScale;
        if (dashBackgroundImage != null) dashBgBaseScale = dashBackgroundImage.localScale;

        if (ballFilledImage != null) ballMainBaseScale = ballFilledImage.rectTransform.localScale;
        if (ballBackgroundImage != null) ballBgBaseScale = ballBackgroundImage.localScale;
    }

    private void Start()
    {
        if (controller == null || combat == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                if (controller == null) controller = player.GetComponent<PlayerController>();
                if (combat == null) combat = player.GetComponent<PlayerCombat>();
            }
        }

        // Sincronizamos el estado inicial para que no haya diferencia y no salte la animación
        if (controller != null) dashWasReady = (controller.GetDashCooldownTimer() <= 0);
        if (combat != null) ballWasReady = (!combat.HasBallInWorld() && combat.GetThrowTimer() <= 0);

        isInitialized = true;
    }

    private void Update()
    {
        if (controller != null) UpdateDashUI();
        if (combat != null) UpdateBallUI();

        // Suavizado de escalas de vuelta a su base original
        HandleScaleLerp(dashFilledImage?.rectTransform, dashMainBaseScale);
        HandleScaleLerp(dashBackgroundImage, dashBgBaseScale);
        HandleScaleLerp(ballFilledImage?.rectTransform, ballMainBaseScale);
        HandleScaleLerp(ballBackgroundImage, ballBgBaseScale);
    }

    private void UpdateDashUI()
    {
        if (dashFilledImage == null) return;

        float cd = controller.GetDashCooldownTimer();
        bool isReady = cd <= 0;

        dashFilledImage.fillAmount = isReady ? 1f : 1f - (cd / controller.GetMaxDashCooldown());
        dashFilledImage.color = isReady ? readyColorDash : chargingColorDash;

        if (isReady && !dashWasReady && isInitialized)
        {
            ApplyPop(dashFilledImage.rectTransform, dashBackgroundImage, dashMainBaseScale, dashBgBaseScale, dashPopMultiplier);
        }
        dashWasReady = isReady;
    }

    private void UpdateBallUI()
    {
        if (ballFilledImage == null || combat == null) return;

        float tTimer = combat.GetThrowTimer();
        bool hasBallInWorld = combat.HasBallInWorld();
        bool isReady = !hasBallInWorld && tTimer <= 0;

        if (hasBallInWorld)
        {
            ballFilledImage.fillAmount = 0f;
            ballFilledImage.color = chargingColorBall;
        }
        else if (tTimer > 0)
        {
            ballFilledImage.fillAmount = 1f - (tTimer / combat.GetMaxThrowCooldown());
            ballFilledImage.color = chargingColorBall;
        }
        else
        {
            ballFilledImage.fillAmount = 1f;
            ballFilledImage.color = readyColorBall;
        }

        if (isReady && !ballWasReady && isInitialized)
        {
            ApplyPop(ballFilledImage.rectTransform, ballBackgroundImage, ballMainBaseScale, ballBgBaseScale, ballPopMultiplier);
        }
        ballWasReady = isReady;
    }

    private void ApplyPop(RectTransform main, RectTransform bg, Vector3 mainBase, Vector3 bgBase, float multiplier)
    {
        if (main != null) main.localScale = mainBase * multiplier;
        if (bg != null) bg.localScale = bgBase * multiplier;
    }

    private void HandleScaleLerp(RectTransform rect, Vector3 targetBase)
    {
        if (rect != null && rect.localScale != targetBase)
        {
            rect.localScale = Vector3.Lerp(rect.localScale, targetBase, Time.deltaTime * animationSpeed);
        }
    }
}