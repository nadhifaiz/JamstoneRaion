using System.Collections;
using UnityEngine;

// Fade alpha CanvasGroup. Memakai waktu tanpa skala (unscaled) supaya tetap jalan walau Time.timeScale = 0.
public static class UIFade
{
    public static IEnumerator Fade(CanvasGroup group, float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            group.alpha = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        group.alpha = to;
    }
}