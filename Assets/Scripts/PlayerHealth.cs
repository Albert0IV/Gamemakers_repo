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

    [Header("Ajustes de Knockback")]
    [SerializeField] private float knockbackForce = 12f;
    [SerializeField] private float upwardForceRatio = 1.5f;
    [SerializeField] private float horizontalForceRatio = 1.0f;

    [Header("Referencias")]
    public PlayerController controller;
    public Rigidbody rb;
    public Renderer playerRenderer;

    private bool isInvulnerable;

    // --- LÓGICA DE POSICIÓN SEGURA ---
    private Vector3 lastSafePosition;
    private int currentCheckpointIndex = -1;
    private Vector3 initialPosition; // Posición de seguridad absoluta al iniciar la escena

    void Start()
    {
        currentLives = maxLives;
        if (!controller) controller = GetComponent<PlayerController>();
        if (!rb) rb = GetComponent<Rigidbody>();
        if (!playerRenderer) playerRenderer = GetComponentInChildren<Renderer>();

        // Guardamos la posición inicial real del objeto en la jerarquía
        initialPosition = transform.position;

        // Al empezar, la posición segura es la inicial por si no hay checkpoints
        lastSafePosition = initialPosition;
    }

    // --- SISTEMA DE CHECKPOINTS ---
    public void SetCheckpoint(Vector3 newPos, int index)
    {
        // Solo actualizamos si el nuevo checkpoint es el mismo o uno posterior
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

        if (currentLives <= 0)
        {
            RestartLevel();
            return;
        }

        // Desactivamos el script de movimiento para que no interfiera con el knockback/teleport
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

        // Limpiar inercias
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Teletransporte a la posición segura (Checkpoint o posición inicial)
        transform.position = lastSafePosition + (Vector3.up * respawnOffsetY);

        yield return new WaitForSeconds(0.1f);

        if (controller != null) controller.enabled = true;
    }

    // ... (Mantén tus métodos ApplyKnockback, StunSequence e InvulnerabilityFlash igual)

    private void ApplyKnockback(Vector3 sourcePos)
    {
        if (rb == null) return;
        rb.linearVelocity = Vector3.zero;
        float sideDir = (transform.position.x - sourcePos.x) >= 0 ? 1f : -1f;
        Vector3 finalForce = new Vector3(sideDir * horizontalForceRatio * knockbackForce, upwardForceRatio * knockbackForce, 0f);
        rb.AddForce(finalForce, ForceMode.VelocityChange);
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
        // Al reiniciar la escena, todo vuelve a como estaba en el editor
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}