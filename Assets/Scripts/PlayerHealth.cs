using UnityEngine;
using System.Collections;
using System.Collections.Generic;
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
    [Tooltip("Tiempo que espera después del golpe antes de teletransportarlo.")]
    [SerializeField] private float timeBeforeTeleport = 0.5f;
    [Tooltip("Ajuste de altura para que el jugador no aparezca dentro del suelo.")]
    [SerializeField] private float respawnOffsetY = 1.0f; // <--- NUEVO: Modificable en Inspector

    [Header("Ajustes de Knockback")]
    [SerializeField] private float knockbackForce = 12f;
    [SerializeField] private float upwardForceRatio = 1.5f;
    [SerializeField] private float horizontalForceRatio = 1.0f;

    [Header("Referencias")]
    public PlayerController controller;
    public Rigidbody rb;
    public Renderer playerRenderer;

    private bool isInvulnerable;

    // Lógica de Checkpoints Unidireccionales
    private Vector3 lastSafePosition;
    private int currentCheckpointIndex = -1;

    void Start()
    {
        currentLives = maxLives;
        if (!controller) controller = GetComponent<PlayerController>();
        if (!rb) rb = GetComponent<Rigidbody>();
        if (!playerRenderer) playerRenderer = GetComponentInChildren<Renderer>();

        // Posición inicial por defecto
        lastSafePosition = transform.position;
    }

    // --- SISTEMA DE CHECKPOINTS ---

    public void SetCheckpoint(Vector3 newPos, int index)
    {
        if (index >= currentCheckpointIndex)
        {
            lastSafePosition = newPos;
            currentCheckpointIndex = index;
            Debug.Log($"<color=green>Checkpoint {index} activado.</color>");
        }
    }

    // --- SISTEMA DE DAÑO ---

    public void TakeDamage(int damage, Vector3 sourcePos, bool isFromSpikes = false)
    {
        if (isInvulnerable) return;

        currentLives -= damage;

        if (currentLives <= 0)
        {
            RestartLevel();
            return;
        }

        if (controller != null) controller.enabled = false;

        ApplyKnockback(sourcePos);

        if (isFromSpikes)
        {
            StartCoroutine(TeleportSequence());
        }
        else
        {
            StartCoroutine(StunSequence());
        }

        StartCoroutine(InvulnerabilityFlash());
    }

    private IEnumerator TeleportSequence()
    {
        yield return new WaitForSeconds(timeBeforeTeleport);

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // APLICAMOS EL OFFSET AQUÍ
        // Calculamos la posición final sumando el offset en el eje Y
        Vector3 spawnPos = new Vector3(lastSafePosition.x, lastSafePosition.y + respawnOffsetY, lastSafePosition.z);
        transform.position = spawnPos;

        yield return new WaitForSeconds(0.1f);

        if (controller != null) controller.enabled = true;
    }

    // ... (El resto del código: StunSequence, ApplyKnockback, InvulnerabilityFlash, OnCollisionEnter, etc. se mantienen igual)

    private IEnumerator StunSequence()
    {
        yield return new WaitForSeconds(stunTime);
        if (controller != null) controller.enabled = true;
    }

    private void ApplyKnockback(Vector3 sourcePos)
    {
        if (rb == null) return;
        rb.linearVelocity = Vector3.zero;
        float sideDir = (transform.position.x - sourcePos.x) >= 0 ? 1f : -1f;
        Vector3 finalForce = new Vector3(sideDir * horizontalForceRatio * knockbackForce, upwardForceRatio * knockbackForce, 0f);
        rb.AddForce(finalForce, ForceMode.VelocityChange);
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
        if (collision.gameObject.CompareTag("Spikes"))
            TakeDamage(1, collision.contacts[0].point, true);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Spikes"))
            TakeDamage(1, other.transform.position, true);
    }

    private void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}