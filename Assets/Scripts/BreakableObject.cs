using UnityEngine;
using System.Collections;

public class BreakableObject : MonoBehaviour, IDamageable
{
    public enum WeakSide { Left, Right, Top, Bottom }

    [Header("Configuración")]
    [SerializeField] private int hitPoints = 3;
    [SerializeField] private WeakSide weakSide = WeakSide.Left;

    [Header("Visuales")]
    [SerializeField] private GameObject breakEffect;
    [SerializeField] private GameObject blockEffect;
    [SerializeField] private Transform visualModel;

    private bool isShaking = false;
    private Vector3 originalPos;

    private void Start()
    {
        if (visualModel == null) visualModel = transform;
        originalPos = visualModel.localPosition;
    }

    // Método de la Interfaz
    public void TakeDamage(int damage, Vector3 attackerPos)
    {
        bool success = false;
        float xDiff = attackerPos.x - transform.position.x;
        float yDiff = attackerPos.y - transform.position.y;

        switch (weakSide)
        {
            case WeakSide.Left: if (xDiff < 0) success = true; break;
            case WeakSide.Right: if (xDiff > 0) success = true; break;
            case WeakSide.Top: if (yDiff > 0) success = true; break;
            case WeakSide.Bottom: if (yDiff < 0) success = true; break;
        }

        if (success) ProcessHit(damage);
        else if (blockEffect != null) Instantiate(blockEffect, transform.position, Quaternion.identity);
    }

    private void ProcessHit(int damage)
    {
        hitPoints -= damage;
        if (hitPoints > 0) StartCoroutine(ShakeModel());
        else Break();
    }

    private void Break()
    {
        if (breakEffect != null) Instantiate(breakEffect, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

    private IEnumerator ShakeModel()
    {
        if (isShaking) yield break;
        isShaking = true;
        float elapsed = 0f;
        float duration = 0.15f;
        float magnitude = 0.1f;
        while (elapsed < duration)
        {
            visualModel.localPosition = originalPos + new Vector3(Random.Range(-1f, 1f) * magnitude, Random.Range(-1f, 1f) * magnitude, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }
        visualModel.localPosition = originalPos;
        isShaking = false;
    }
}