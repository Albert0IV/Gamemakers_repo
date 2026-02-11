using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [Header("Configuración de Vida")]
    [SerializeField] private int maxLives = 3;
    private int currentLives;

    [Header("Tiempos (Sincronizados)")]
    [SerializeField] private float stunTime = 0.6f;
    [SerializeField] private float invulnerabilityDuration = 2f;

    [Header("Ajustes de Knockback (Empuje)")]
    [Tooltip("La fuerza base que se multiplica por los ratios.")]
    [SerializeField] private float knockbackForce = 12f;

    [Tooltip("Ratio vertical. Ahora puedes poner 2, 5, 10... lo que quieras.")]
    [SerializeField] private float upwardForceRatio = 1.5f;

    [Tooltip("Ratio horizontal.")]
    [SerializeField] private float horizontalForceRatio = 1.0f;

    [Header("Referencias")]
    public PlayerController controller;
    public Rigidbody rb;
    public Renderer playerRenderer;

    private bool isInvulnerable;

    void Start()
    {
        currentLives = maxLives;
        if (!controller) controller = GetComponent<PlayerController>();
        if (!rb) rb = GetComponent<Rigidbody>();
        if (!playerRenderer) playerRenderer = GetComponentInChildren<Renderer>();
    }

    public void TakeDamage(int damage, Vector3 sourcePos)
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

        // 2. FÍSICA
        ApplyKnockback(sourcePos);

        // 3. RUTINAS
        StartCoroutine(StunSequence());
        StartCoroutine(InvulnerabilityFlash());
    }

    private void ApplyKnockback(Vector3 sourcePos)
    {
        if (rb == null) return;

        rb.linearVelocity = Vector3.zero;

        // Determinamos si el golpe viene de la izquierda o derecha
        float sideDir = (transform.position.x - sourcePos.x) >= 0 ? 1f : -1f;

        // Creamos el vector final multiplicando la fuerza base por cada ratio independiente
        // Esto te permite que el eje Y sea mucho más potente que el X o viceversa
        Vector3 finalForce = new Vector3(
            sideDir * horizontalForceRatio * knockbackForce,
            upwardForceRatio * knockbackForce,
            0f
        );

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
            TakeDamage(1, collision.contacts[0].point);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Spikes"))
            TakeDamage(1, other.transform.position);
    }

    private void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}