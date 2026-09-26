using EditorAttributes;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BumpEventController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform gloveContainer;
    [SerializeField] private Image glovePrefab;
    [SerializeField] private CanvasGroup flash;

    [Header("Gloves")]
    [SerializeField] private int gloveCount = 6;

    [SerializeField] private float gloveDuration = 0.28f;

    [Header("Impact")]
    [SerializeField] private float impactDelay = 0.18f;
    [SerializeField] private float shakeDuration = 0.22f;
    [SerializeField] private float shakeStrength = 18f;

    [Header("Audio")]
    [SerializeField] private AudioClip hitSound;

    private Coroutine bumpRoutine;

    private void OnEnable()
    {
        Debug.Log("[Bump] BumpEventController ENABLED.");

        if (WebhookListener.Instance != null)
        {
            WebhookListener.Instance.OnBumpReceived +=
                OnBumpReceived;

            Debug.Log(
                "[Bump] Successfully subscribed to WebhookListener."
            );
        }
        else
        {
            Debug.LogError(
                "[Bump] WebhookListener.Instance is NULL!"
            );
        }
    }

    private void OnDisable()
    {
        if (WebhookListener.Instance != null)
        {
            WebhookListener.Instance.OnBumpReceived -=
                OnBumpReceived;
        }
    }

    private void OnBumpReceived()
    {
        Debug.Log(
            "[Bump] BUMP RECEIVED! Starting glove effect."
        );

        if (bumpRoutine != null)
        {
            Debug.Log(
                "[Bump] Stopping previous bump effect."
            );

            StopCoroutine(bumpRoutine);
        }

        bumpRoutine =
            StartCoroutine(BumpRoutine());
    }

    private IEnumerator BumpRoutine()
    {
        Debug.Log(
            $"[Bump] Spawning {gloveCount} gloves."
        );

        if (gloveContainer == null)
        {
            Debug.LogError(
                "[Bump] Glove Container is NULL!"
            );

            yield break;
        }

        if (glovePrefab == null)
        {
            Debug.LogError(
                "[Bump] Glove Prefab is NULL!"
            );

            yield break;
        }

        for (int i = 0; i < gloveCount; i++)
        {
            Debug.Log(
                $"[Bump] Spawn glove {i + 1}/{gloveCount}"
            );

            SpawnGlove(i);
        }

        Debug.Log(
            "[Bump] All gloves spawned."
        );

        yield return new WaitForSeconds(
            impactDelay
        );

        Debug.Log(
            "[Bump] IMPACT!"
        );

        PlayHitSound();

        yield return FlashRoutine();

        yield return ShakeRoutine();

        Debug.Log(
            "[Bump] Effect finished."
        );

        bumpRoutine = null;
    }

    [Button("Trigger Bump Effect")]
    public void SpawnGlove(int index)
    {
        if (gloveContainer == null)
        {
            Debug.LogError(
                "[Bump] Cannot spawn glove: Glove Container is NULL."
            );

            return;
        }

        if (glovePrefab == null)
        {
            Debug.LogError(
                "[Bump] Cannot spawn glove: Glove Prefab is NULL."
            );

            return;
        }

        Image glove = Instantiate(
            glovePrefab,
            gloveContainer
        );

        Debug.Log(
            $"[Bump] Glove {index + 1} instantiated: {glove.name}"
        );

        RectTransform rect = glove.rectTransform;

        float angle =
            (360f / gloveCount) * index +
            Random.Range(-25f, 25f);

        float radians =
            angle * Mathf.Deg2Rad;

        Vector2 direction =
            new Vector2(
                Mathf.Cos(radians),
                Mathf.Sin(radians)
            );

        float distanceFromCenter = 1100f;

        rect.anchoredPosition =
            direction * distanceFromCenter;

        rect.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                Random.Range(-45f, 45f)
            );

        StartCoroutine(
            AnimateGlove(
                glove,
                rect,
                direction,
                index
            )
        );
    }

    private IEnumerator AnimateGlove(
        Image glove,
        RectTransform rect,
        Vector2 startDirection,
        int index)
    {
        Vector2 startPosition =
            rect.anchoredPosition;

        Vector2 targetPosition =
            Vector2.zero +
            Random.insideUnitCircle * 100f;

        Quaternion startRotation =
            rect.localRotation;

        Quaternion targetRotation =
            Quaternion.Euler(
                0f,
                0f,
                Random.Range(-20f, 20f)
            );

        float elapsed = 0f;

        while (elapsed < gloveDuration)
        {
            if (glove == null)
                yield break;

            elapsed += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / gloveDuration
                );

            // Smooth entrance.
            float smoothT =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );

            rect.anchoredPosition =
                Vector2.Lerp(
                    startPosition,
                    targetPosition,
                    smoothT
                );

            rect.localRotation =
                Quaternion.Slerp(
                    startRotation,
                    targetRotation,
                    smoothT
                );

            // Punch scale near impact.
            float scale =
                Mathf.Lerp(
                    1.4f,
                    0.85f,
                    smoothT
                );

            rect.localScale =
                Vector3.one * scale;

            yield return null;
        }

        if (glove != null)
        {
            Destroy(glove.gameObject);
        }
    }

    private IEnumerator FlashRoutine()
    {
        if (flash == null)
            yield break;

        flash.alpha = 0f;

        float duration = 0.12f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / duration
                );

            flash.alpha =
                Mathf.Sin(t * Mathf.PI);

            yield return null;
        }

        flash.alpha = 0f;
    }

    private IEnumerator ShakeRoutine()
    {
        if (Camera.main == null)
            yield break;

        Transform cameraTransform =
            Camera.main.transform;

        Vector3 originalPosition =
            cameraTransform.localPosition;

        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;

            float strength =
                1f -
                Mathf.Clamp01(
                    elapsed / shakeDuration
                );

            Vector2 randomOffset =
                Random.insideUnitCircle *
                shakeStrength *
                strength;

            cameraTransform.localPosition =
                originalPosition +
                new Vector3(
                    randomOffset.x,
                    randomOffset.y,
                    0f
                );

            yield return null;
        }

        cameraTransform.localPosition =
            originalPosition;
    }

    private void PlayHitSound()
    {
        if (hitSound == null)
            return;

        AudioSource.PlayClipAtPoint(
            hitSound,
            Vector3.zero
        );
    }
}