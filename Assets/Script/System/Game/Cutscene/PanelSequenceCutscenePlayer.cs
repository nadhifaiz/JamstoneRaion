using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Memutar panel cutscene satu per satu dengan fade-in.
// Setelah semua panel muncul, pemain menekan klik/Space/Enter untuk lanjut.
// Menekan ketika panel masih muncul akan memperlihatkan sisa panel sekaligus.
public class PanelSequenceCutscenePlayer : CutscenePlayerBase
{
    private enum PageState { Revealing, Ready }

    [Header("References")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private CanvasGroup pageGroup;
    [Tooltip("Image untuk menampilkan panel. Atur posisi dan ukuran setiap Image langsung di Canvas. Jumlahnya menentukan batas maksimal panel per halaman.")]
    [SerializeField] private Image[] panelImages;
    [SerializeField] private GameObject continueHint;
    [Tooltip("Dipakai untuk cutscene yang belum punya halaman (mis. DummyCutscenePlayer), supaya alur tetap bisa dites")]
    [SerializeField] private CutscenePlayerBase fallbackPlayer;

    [Header("Waktu (detik)")]
    [SerializeField] private float firstPanelDelay = 0.3f;
    [Tooltip("Jarak dari mulai munculnya satu panel ke mulai panel berikutnya")]
    [SerializeField] private float panelInterval = 1.2f;
    [SerializeField] private float panelFadeDuration = 0.5f;
    [Tooltip("Lama fade untuk menampilkan sisa panel sekaligus saat pemain menekan lebih awal")]
    [SerializeField] private float quickRevealDuration = 0.2f;
    [SerializeField] private float pageFadeDuration = 0.4f;
    [SerializeField] private float rootFadeDuration = 0.4f;
    [Tooltip("Tekanan diabaikan selama jeda ini setelah halaman tampil dan setelah semua panel muncul")]
    [SerializeField] private float clickLockSeconds = 0.8f;

    private CanvasGroup rootGroup;
    private CanvasGroup[] panelGroups;
    private PageState state;
    private float clickLockUntil;
    private int activePanelCount;

    private void Awake()
    {
        rootGroup = panelRoot.GetComponent<CanvasGroup>();
        if (rootGroup == null) rootGroup = panelRoot.AddComponent<CanvasGroup>();

        panelGroups = new CanvasGroup[panelImages.Length];
        for (int i = 0; i < panelImages.Length; i++)
        {
            if (panelImages[i] == null) continue;

            panelGroups[i] = panelImages[i].GetComponent<CanvasGroup>();
            if (panelGroups[i] == null)
                panelGroups[i] = panelImages[i].gameObject.AddComponent<CanvasGroup>();

            panelImages[i].gameObject.SetActive(false);
        }

        panelRoot.SetActive(false);
    }

    public override IEnumerator Play(CutsceneSO cutscene)
    {
        if (cutscene.pages == null || cutscene.pages.Count == 0)
        {
            if (fallbackPlayer != null)
                yield return fallbackPlayer.Play(cutscene);
            else
                Debug.LogWarning($"[PanelSequence] Cutscene '{cutscene.name}' belum punya halaman dan tidak ada fallbackPlayer.", this);
            yield break;
        }

        rootGroup.alpha = 0f;
        pageGroup.alpha = 0f;
        panelRoot.SetActive(true);
        yield return UIFade.Fade(rootGroup, 0f, 1f, rootFadeDuration);

        foreach (CutscenePage page in cutscene.pages)
        {
            if (page == null || page.panels == null || page.panels.Count == 0)
                continue;

            PreparePage(page);
            yield return ShowPage();
            yield return UIFade.Fade(pageGroup, 1f, 0f, pageFadeDuration);
        }

        yield return UIFade.Fade(rootGroup, 1f, 0f, rootFadeDuration);
        panelRoot.SetActive(false);
    }

    private void PreparePage(CutscenePage page)
    {
        activePanelCount = Mathf.Min(page.panels.Count, panelImages.Length);

        if (page.panels.Count > panelImages.Length)
        {
            Debug.LogWarning(
                $"[PanelSequence] Halaman memiliki {page.panels.Count} panel, tetapi hanya tersedia {panelImages.Length} Image slot. Panel selebihnya diabaikan.",
                this);
        }

        for (int i = 0; i < panelImages.Length; i++)
        {
            if (panelImages[i] == null || panelGroups[i] == null) continue;

            bool used = i < activePanelCount && page.panels[i] != null;
            panelImages[i].gameObject.SetActive(used);
            if (!used) continue;

            panelImages[i].sprite = page.panels[i];
            panelGroups[i].alpha = 0f;
        }

        if (continueHint != null)
            continueHint.SetActive(false);
    }

    // Menunggu semua panel selesai muncul dan pemain menekan untuk lanjut.
    private IEnumerator ShowPage()
    {
        // Seluruh halaman fade-in secara bersamaan sebelum panel mulai muncul.
        yield return UIFade.Fade(pageGroup, 0f, 1f, pageFadeDuration);

        state = PageState.Revealing;
        clickLockUntil = Time.unscaledTime + clickLockSeconds;
        Coroutine reveal = StartCoroutine(RevealPanelsRoutine());

        while (true)
        {
            yield return null;

            if (!WasClickPressed() || Time.unscaledTime < clickLockUntil)
                continue;

            if (state == PageState.Revealing)
            {
                StopCoroutine(reveal);
                reveal = StartCoroutine(RevealRemainingRoutine());
                clickLockUntil = Time.unscaledTime + clickLockSeconds;
                continue;
            }

            break;
        }
    }

    private IEnumerator RevealPanelsRoutine()
    {
        yield return WaitUnscaled(firstPanelDelay);

        for (int i = 0; i < activePanelCount; i++)
        {
            if (panelImages[i] == null || panelGroups[i] == null || !panelImages[i].gameObject.activeSelf)
                continue;

            yield return FadePanel(panelGroups[i], panelFadeDuration);

            if (i < activePanelCount - 1)
                yield return WaitUnscaled(Mathf.Max(0f, panelInterval - panelFadeDuration));
        }

        OnAllRevealed();
    }

    // Jika pemain menekan ketika reveal berjalan, semua panel yang tersisa muncul bersamaan.
    private IEnumerator RevealRemainingRoutine()
    {
        float[] startAlpha = new float[activePanelCount];
        for (int i = 0; i < activePanelCount; i++)
            startAlpha[i] = panelGroups[i] != null ? panelGroups[i].alpha : 1f;

        float duration = Mathf.Max(0f, quickRevealDuration);
        if (duration > 0f)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                for (int i = 0; i < activePanelCount; i++)
                {
                    if (panelGroups[i] != null && panelImages[i] != null && panelImages[i].gameObject.activeSelf)
                        panelGroups[i].alpha = Mathf.Lerp(startAlpha[i], 1f, t);
                }

                yield return null;
            }
        }

        for (int i = 0; i < activePanelCount; i++)
        {
            if (panelGroups[i] != null && panelImages[i] != null && panelImages[i].gameObject.activeSelf)
                panelGroups[i].alpha = 1f;
        }

        OnAllRevealed();
    }

    private void OnAllRevealed()
    {
        state = PageState.Ready;
        clickLockUntil = Time.unscaledTime + clickLockSeconds;

        if (continueHint != null)
            continueHint.SetActive(true);
    }

    private static IEnumerator FadePanel(CanvasGroup group, float duration)
    {
        float startAlpha = group.alpha;
        float elapsed = 0f;

        if (duration <= 0f)
        {
            group.alpha = 1f;
            yield break;
        }

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(startAlpha, 1f, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        group.alpha = 1f;
    }

    private static IEnumerator WaitUnscaled(float seconds)
    {
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private static bool WasClickPressed()
    {
        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame) return true;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame)) return true;

        Touchscreen touch = Touchscreen.current;
        return touch != null && touch.primaryTouch.press.wasPressedThisFrame;
    }
}
