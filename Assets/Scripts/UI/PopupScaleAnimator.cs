using System.Collections;
using UnityEngine;

public class PopupScaleAnimator : MonoBehaviour
{
    [Min(0.01f)] public float duration = 0.18f;
    [Min(0.01f)] public float startScale = 0.82f;
    [Min(1f)] public float overshootScale = 1.04f;

    private RectTransform rectTransform;
    private Coroutine openRoutine;
    private Vector3 baseScale = Vector3.one;
    private bool hasBaseScale;

    private void Awake()
    {
        rectTransform = transform as RectTransform;
        CaptureBaseScale();
    }

    private void OnDisable()
    {
        if (openRoutine != null)
        {
            StopCoroutine(openRoutine);
            openRoutine = null;
        }

        if (rectTransform != null && hasBaseScale)
        {
            rectTransform.localScale = baseScale;
        }
    }

    public void PlayOpen()
    {
        if (rectTransform == null)
        {
            rectTransform = transform as RectTransform;
        }

        if (rectTransform == null)
        {
            return;
        }

        if (!hasBaseScale)
        {
            CaptureBaseScale();
        }

        if (openRoutine != null)
        {
            StopCoroutine(openRoutine);
        }

        openRoutine = StartCoroutine(AnimateOpen());
    }

    private void CaptureBaseScale()
    {
        if (rectTransform == null)
        {
            rectTransform = transform as RectTransform;
        }

        if (rectTransform == null)
        {
            return;
        }

        baseScale = rectTransform.localScale;
        hasBaseScale = true;
    }

    private IEnumerator AnimateOpen()
    {
        float elapsed = 0f;
        Vector3 fromScale = baseScale * startScale;
        Vector3 peakScale = baseScale * overshootScale;

        rectTransform.localScale = fromScale;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            rectTransform.localScale = t < 0.72f
                ? Vector3.LerpUnclamped(fromScale, peakScale, EaseOutCubic(t / 0.72f))
                : Vector3.LerpUnclamped(peakScale, baseScale, EaseOutCubic((t - 0.72f) / 0.28f));
            yield return null;
        }

        rectTransform.localScale = baseScale;
        openRoutine = null;
    }

    private static float EaseOutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        float inverse = 1f - t;
        return 1f - inverse * inverse * inverse;
    }
}
