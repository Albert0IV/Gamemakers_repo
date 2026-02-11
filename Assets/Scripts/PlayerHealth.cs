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
    [Tooltip("Tiempo que espera después del golpe de pinchos antes de teletransportarlo.")]
    [SerializeField] private float timeBeforeTeleport = 0.5f;
    [Tooltip("Tiempo atrás para buscar la posición segura (ej. 10 segundos).")]
    [SerializeField] private float safePositionDelay = 10f;

    [Header("Ajustes de Knockback")]
    [SerializeField] private float knockbackForce = 12f;
    [SerializeField] private float upwardForceRatio = 1.5f;
    [SerializeField] private float horizontalForceRatio = 1.0f;

    [Header("Referencias")]
    public PlayerController controller;
    public Rigidbody rb;
    public Renderer playerRenderer;

    private bool isInvulnerable;

    // Estructura para guardar posiciones con tiempo
    private struct PositionStamp
    {
        public Vector3 position;
        public float time;
    }
    private List<PositionStamp> positionHistory = new List<PositionStamp>();
    private Vector3 lastSafePosition;

    void Start()
    {
        currentLives = maxLives;
        if (!controller) controller = GetComponent<PlayerController>();
        if (!rb) rb = GetComponent<Rigidbody>();
        if (!playerRenderer) playerRenderer = GetComponentInChildren<Renderer>();

        lastSafePosition = transform.position; // Posición inicial por defecto
    }

    void Update()
    {
        // Solo guardamos posición si el controlador dice que estamos en el suelo
        // (Asumimos que tu PlayerController tiene una forma de saber si toca suelo, 
        // pero usaremos una lógica interna aquí por seguridad)
        if (CheckGroundedInternal())
        {
            positionHistory.Add(new PositionStamp { position = transform.position, time = Time.time });
        }

        // Limpiamos el historial para quedarnos solo con lo necesario
        // Buscamos la posición que ocurrió hace 'safePositionDelay' segundos
        UpdateSafePosition();
    }

    private void UpdateSafePosition()
    {
        float targetTime = Time.time - safePositionDelay;

        // Buscamos en el historial el punto más cercano a hace 10 segundos
        while (positionHistory.Count > 0 && positionHistory[0].time < targetTime)
        {
            lastSafePosition = positionHistory[0].position;
            positionHistory.RemoveAt(0);
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

        // 1. STUN RADICAL
        if (controller != null) controller.enabled = false;

        // 2. FÍSICA (Knockback inicial)
        ApplyKnockback(sourcePos);

        // 3. SI SON PINCHOS -> TELETRANSPORTE
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
        // Esperamos el tiempo configurable antes de teletransportar
        yield return new WaitForSeconds(timeBeforeTeleport);

        // Teletransporte
        rb.linearVelocity = Vector3.zero;
        transform.position = lastSafePosition;

        // Un pequeño margen extra antes de devolver el control para que la cámara se ajuste
        yield return new WaitForSeconds(0.1f);

        if (controller != null) controller.enabled = true;
    }

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

    // --- DETECCIÓN DE DAÑO ---

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Spikes"))
            TakeDamage(1, collision.contacts[0].point, true); // Enviamos 'true' porque son pinchos
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Spikes"))
            TakeDamage(1, other.transform.position, true); // Enviamos 'true' porque son pinchos
    }

    private bool CheckGroundedInternal()
    {
        // Usamos un Raycast simple para saber si el player está tocando el suelo
        return Physics.Raycast(transform.position, Vector3.down, 1.1f);
    }

    private void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}