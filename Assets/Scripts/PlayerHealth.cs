using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [Header("Configuración de Vida")]
    [SerializeField] private int maxLives = 3;
    private int currentLives;

    [Header("Tiempos de Daño")]
    [SerializeField] private float stunTime = 0.6f;
    [SerializeField] private float invulnerabilityDuration = 2f;

    [Header("Configuración Teletransporte (Spikes)")]
    [SerializeField] private float timeBeforeTeleport = 0.5f;
    [SerializeField] private float respawnOffsetY = 1.0f;

    [Header("Referencias")]
    public PlayerController controller;
    public Rigidbody rb;
    public Renderer playerRenderer;

    private bool isInvulnerable;
    private Vector3 lastSafePosition;
    private int currentCheckpointIndex = -1;

    void Start()
    {
        currentLives = maxLives;
        lastSafePosition = transform.position;
    }

    public void SetCheckpoint(Vector3 newPos, int index)
    {
        if (index >= currentCheckpointIndex)
        {
            lastSafePosition = newPos;
            currentCheckpointIndex = index;
        }
    }

    public void TakeDamage(int damage, Vector3 sourcePos, bool isFromSpikes = false)
    {
        if (isInvulnerable) return;

        currentLives -= damage;

        // Sacudida de Daño
        CameraSystem cam = Camera.main.GetComponent<CameraSystem>();
        if (cam != null) cam.ShakeDamage();

        if (currentLives <= 0) { RestartLevel(); return; }

        if (controller != null) controller.enabled = false;

        ApplyKnockback(sourcePos);

        if (isFromSpikes) StartCoroutine(TeleportSequence());
        else StartCoroutine(StunSequence());

        StartCoroutine(InvulnerabilityFlash());
    }

    private IEnumerator TeleportSequence()
    {
        yield return new WaitForSeconds(timeBeforeTeleport);

        // Arreglo Respawn: Forzar estado y posición
        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;
        transform.position = lastSafePosition + (Vector3.up * respawnOffsetY);

        Physics.SyncTransforms(); // Vital para que Unity registre el cambio antes del siguiente frame

        yield return new WaitForFixedUpdate();

        rb.isKinematic = false;
        if (controller != null) controller.enabled = true;
    }

    private void ApplyKnockback(Vector3 sourcePos)
    {
        if (rb == null) return;
        rb.linearVelocity = Vector3.zero;
        float sideDir = (transform.position.x - sourcePos.x) >= 0 ? 1f : -1f;
        rb.AddForce(new Vector3(sideDir * 12f, 18f, 0f), ForceMode.VelocityChange);
    }

    private IEnumerator StunSequence()
    {
        yield return new WaitForSeconds(stunTime);
        if (controller != null) controller.enabled = true;
    }

    private IEnumerator InvulnerabilityFlash()
    {
        isInvulnerable = true;
        float timer = 0;
        while (timer < invulnerabilityDuration)
        {
            if (playerRenderer) playerRenderer.enabled = !playerRenderer.enabled;
            yield return new WaitForSeconds(0.1f);
            timer += 0.1f;
        }
        if (playerRenderer) playerRenderer.enabled = true;
        isInvulnerable = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Spikes")) TakeDamage(1, collision.contacts[0].point, true);
    }

    private void RestartLevel() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
}