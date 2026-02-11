using UnityEngine;
using System.Collections;

public class BreakableObject : MonoBehaviour
{
    public enum WeakSide { Left, Right, Top, Bottom }

    [Header("Configuración")]
    [SerializeField] private int hitPoints = 3; // Cuantos golpes aguanta
    [SerializeField] private WeakSide weakSide = WeakSide.Left;

    [Header("Visuales")]
    [SerializeField] private GameObject breakEffect;
    [SerializeField] private GameObject blockEffect;
    [SerializeField] private Transform visualModel; // Arrastra aquí el hijo que tiene el Sprite/Mesh si quieres que vibre

    private bool isShaking = false;
    private Vector3 originalPos;

    private void Start()
    {
        if (visualModel == null) visualModel = transform; // Si no asignas nada, usa el propio transform
        originalPos = visualModel.localPosition;
    }

    public void HitObject(int damage, Transform attackerTransform)
    {
        bool success = false;

        float xDiff = attackerTransform.position.x - transform.position.x;
        float yDiff = attackerTransform.position.y - transform.position.y;

        switch (weakSide)
        {
            case WeakSide.Left:
                if (xDiff < 0) success = true;
                break;
            case WeakSide.Right:
                if (xDiff > 0) success = true;
                break;
            case WeakSide.Top:
                if (yDiff > 0) success = true;
                break;
            case WeakSide.Bottom:
                if (yDiff < 0) success = true;
                break;
        }

        if (success)
        {
            TakeDamage(damage);
        }
        else
        {
            if (blockEffect != null)
                Instantiate(blockEffect, transform.position, Quaternion.identity);
        }
    }

    private void TakeDamage(int damage)
    {
        hitPoints -= damage;

        if (hitPoints > 0)
        {
            // FEEDBACK: Si no se rompe, tiembla un poco
            StartCoroutine(ShakeModel());
        }
        else
        {
            Break();
        }
    }

    private void Break()
    {
        if (breakEffect != null)
        {
            Instantiate(breakEffect, transform.position, Quaternion.identity);
        }
        Destroy(gameObject);
    }

    // Corutina simple para que el objeto vibre al ser golpeado
    private IEnumerator ShakeModel()
    {
        if (isShaking) yield break;
        isShaking = true;

        float elapsed = 0f;
        float duration = 0.15f;
        float magnitude = 0.1f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            visualModel.localPosition = originalPos + new Vector3(x, y, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        visualModel.localPosition = originalPos;
        isShaking = false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Vector3 direction = Vector3.zero;

        switch (weakSide)
        {
            case WeakSide.Left: direction = Vector3.left; break;
            case WeakSide.Right: direction = Vector3.right; break;
            case WeakSide.Top: direction = Vector3.up; break;
            case WeakSide.Bottom: direction = Vector3.down; break;
        }

        Gizmos.DrawRay(transform.position, direction * 1.5f);
        Gizmos.DrawSphere(transform.position + (direction * 1.5f), 0.2f);
    }
}