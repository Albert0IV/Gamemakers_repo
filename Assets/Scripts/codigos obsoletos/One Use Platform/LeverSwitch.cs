using UnityEngine;

public class LeverSwitch : MonoBehaviour, IDamageable
{
    [Header("Referencias")]
    [SerializeField] private MovingPlatform platformToActivate;

    [Header("Visual")]
    [SerializeField] private Renderer leverRenderer;
    [SerializeField] private Color activatedColor = Color.green;

    private bool isTriggered = false;

    public void TakeDamage(int damage, Vector3 attackerPos)
    {
        if (isTriggered) return;

        isTriggered = true;
        if (leverRenderer != null) leverRenderer.material.color = activatedColor;
        if (platformToActivate != null) platformToActivate.StartMoving();

        Debug.Log("Palanca activada con " + damage + " de daño!");
    }
}