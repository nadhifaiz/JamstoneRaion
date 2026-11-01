using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScreenTransition : MonoBehaviour
{
    public static ScreenTransition Instance { get; private set; }

    [SerializeField] private CanvasGroup overlay;
    [SerializeField] private LoadingScreenUI loadingScreen;

    [Header("Loading")]
    [Tooltip("Loading screen tampil minimal selama ini. Bar naik bertahap sepanjang waktu ini, " +
             "jadi tidak melonjak ke 100% walau proses aslinya hampir instan.")]
    [SerializeField] private float minLoadingSeconds = 1.5f;
    [Tooltip("Batas kecepatan bar (satuan per detik): meredam lonjakan kalau progres asli melompat")]
    [SerializeField] private float progressFillSpeed = 2.5f;
    [Tooltip("Bar ditahan di 100% sebentar sebelum layar dibuka, supaya terlihat penuh")]
    [SerializeField] private float holdAtFullSeconds = 0.25f;

    [Header("Pindah Scene")]
    [SerializeField] private float sceneFadeDuration = 0.4f;

    public bool IsBusy { get; private set; }
    public float MinLoadingSeconds => minLoadingSeconds;
    public float ProgressFillSpeed => progressFillSpeed;

    void Awake()
    {
        if (Instance != null)
        {
            // Scene game boleh punya salinan sendiri untuk testing langsung; yang kedua dibuang
            Destroy(transform.root.gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(transform.root.gameObject);

        overlay.alpha = 0f;
        overlay.blocksRaycasts = false;
        loadingScreen.Hide();
    }

    // ==========================================
    // BAHAN DASAR (dipakai PhaseTransition)
    // ==========================================

    public void SetBlocking(bool blocking) => overlay.blocksRaycasts = blocking;

    public void SetBlack(float alpha) => overlay.alpha = alpha;

    public IEnumerator FadeToBlack(float duration)
    {
        overlay.blocksRaycasts = true;
        yield return UIFade.Fade(overlay, overlay.alpha, 1f, duration);
    }

    public IEnumerator FadeFromBlack(float duration)
    {
        yield return UIFade.Fade(overlay, overlay.alpha, 0f, duration);
        overlay.blocksRaycasts = false;
        loadingScreen.Hide();
    }

    public void ShowLoading() => loadingScreen.Show();

    public void SetLoadingProgress(float value) => loadingScreen.SetProgress(value);

    // Progres yang DITAMPILKAN: tidak pernah melampaui progres asli (real), tapi juga naik halus mengikuti
    // waktu minimum. Tanpa ini, proses asli yang cuma sekejap membuat bar melompat langsung ke 100%.
    public float AdvanceProgress(float shown, float real, float elapsed)
    {
        float time01 = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, minLoadingSeconds));
        float eased = time01 * time01 * (3f - 2f * time01); // smoothstep: pelan - cepat - pelan

        float target = Mathf.Min(real, eased);
        return Mathf.MoveTowards(shown, target, progressFillSpeed * Time.unscaledDeltaTime);
    }

    // Tahan sebentar di 100% sebelum layar dibuka
    public IEnumerator HoldAtFull()
    {
        yield return new WaitForSecondsRealtime(holdAtFullSeconds);
    }

    // ==========================================
    // PINDAH SCENE: gelap -> loading (progres asli) -> terang
    // ==========================================

    public static void LoadScene(string sceneName)
    {
        if (Instance == null)
        {
            SceneManager.LoadScene(sceneName);
            return;
        }

        if (!Instance.IsBusy) Instance.StartCoroutine(Instance.LoadSceneRoutine(sceneName));
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        IsBusy = true;
        Time.timeScale = 1f;

        yield return FadeToBlack(sceneFadeDuration);

        loadingScreen.Show();

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false; // tahan di 90% sampai bar dan waktu minimum selesai

        float elapsed = 0f;
        float shown = 0f;

        while (operation.progress < 0.9f || shown < 0.99f || elapsed < minLoadingSeconds)
        {
            elapsed += Time.unscaledDeltaTime;

            float real = Mathf.Clamp01(operation.progress / 0.9f);
            shown = AdvanceProgress(shown, real, elapsed);
            loadingScreen.SetProgress(shown);

            yield return null;
        }

        loadingScreen.SetProgress(1f);
        yield return HoldAtFull();
        operation.allowSceneActivation = true;

        while (!operation.isDone) yield return null;

        // Beri dua frame supaya Awake/Start scene baru selesai sebelum layar dibuka
        yield return null;
        yield return null;

        yield return FadeFromBlack(sceneFadeDuration);
        IsBusy = false;
    }
}