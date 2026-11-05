using System.Collections;
using UnityEngine;

public class PhaseTransition : MonoBehaviour
{
    public static PhaseTransition Instance { get; private set; }

    [Header("Zoom")]
    [Tooltip("Parent yang berisi background Hub + hubPanel. Inilah yang di-zoom; harus membentang penuh layar.")]
    [SerializeField] private RectTransform zoomRoot;
    [SerializeField] private float zoomScale = 3f;

    [Header("Durasi (detik)")]
    [SerializeField] private float zoomInDuration = 0.55f;
    [Tooltip("Fade-in panel tujuan")]
    [SerializeField] private float revealDuration = 0.4f;
    [Tooltip("Fase -> gelap saat kembali ke Hub")]
    [SerializeField] private float fadeToHubDuration = 0.25f;
    [SerializeField] private float zoomOutDuration = 0.45f;

    [Header("Loading Collect")]
    [Tooltip("Pengaman: loading dipaksa selesai setelah sekian detik walau pengisian area belum tuntas")]
    [SerializeField] private float loadingTimeout = 10f;

    public bool IsBusy { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private bool CanAnimate => ScreenTransition.Instance != null && zoomRoot != null;

    // ==========================================
    // HUB -> FASE
    // ==========================================

    public void GoToPhase(GameState target, Transform focus)
    {
        if (IsBusy) return;

        if (!CanAnimate)
        {
            // deferCollectStart = false di jalur ini, jadi ChangeState sudah memulai gameplay Collect sendiri
            GameManager.Instance.ChangeState(target);
            return;
        }

        AudioManager.Music(MusicId.None); // musik fase lama berhenti saat zoom ke tombol fase baru
        StartCoroutine(GoToPhaseRoutine(target, focus));
    }

    private IEnumerator GoToPhaseRoutine(GameState target, Transform focus)
    {
        IsBusy = true;

        ScreenTransition screen = ScreenTransition.Instance;
        GameManager gm = GameManager.Instance;

        // Posisi awal dan titik fokus diambil SEBELUM di-zoom
        Vector3 restPosition = zoomRoot.localPosition;
        Vector3 focusWorld = UIZoom.GetWorldCenter((RectTransform)focus);

        // 1. Zoom ke tombol sambil menggelap
        yield return ZoomInRoutine(screen, restPosition, focusWorld);

        // 2. Di balik layar gelap: ganti fase. Collect menunda gameplay-nya sampai loading selesai.
        bool isCollect = target == GameState.Collect;
        gm.deferCollectStart = isCollect;
        gm.ChangeState(target);
        gm.deferCollectStart = false;
        UIZoom.Reset(zoomRoot, restPosition);

        // 3. Collect: loading screen (area diisi penuh)
        if (isCollect) yield return RunCollectLoading(screen, gm);

        if (target == GameState.Collect) AudioManager.Music(MusicId.CollectPhase); // musik fase Collect baru dimulai setelah loading selesai
        else if (target == GameState.Craft) AudioManager.Music(MusicId.CraftingPhase); // musik fase Crafting baru dimulai setelah loading selesai
        else if (target == GameState.Sell) AudioManager.Music(MusicId.SellPhase); // musik fase Sell baru dimulai setelah loading selesai
        else AudioManager.Music(MusicId.InGame); // musik fase InGame baru dimulai setelah loading selesai

        // 4. Panel tujuan fade-in
        yield return screen.FadeFromBlack(revealDuration);

        // 5. Gameplay Collect baru mulai setelah pemain bisa melihat areanya
        if (isCollect) gm.StartCollectGameplay();

        IsBusy = false;
    }

    private IEnumerator ZoomInRoutine(ScreenTransition screen, Vector3 restPosition, Vector3 focusWorld)
    {
        screen.SetBlocking(true);

        float elapsed = 0f;
        while (elapsed < zoomInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / zoomInDuration);

            float eased = t * t; // makin cepat mendekati tombol
            UIZoom.Apply(zoomRoot, restPosition, Mathf.Lerp(1f, zoomScale, eased), focusWorld);
            screen.SetBlack(Mathf.InverseLerp(0.45f, 1f, t)); // menggelap di paruh akhir zoom

            yield return null;
        }

        screen.SetBlack(1f);
    }

    // Mengisi area Collect sampai penuh; progres bar mengikuti progres asli, dengan waktu tampil minimum
    private IEnumerator RunCollectLoading(ScreenTransition screen, GameManager gm)
    {
        screen.ShowLoading();

        float real = 0f;
        float shown = 0f;
        float elapsed = 0f;

        if (gm.spawner != null) StartCoroutine(gm.spawner.PrewarmRoutine(progress => real = progress));
        else real = 1f;

        while ((shown < 1f || elapsed < screen.MinLoadingSeconds) && elapsed < loadingTimeout)
        {
            elapsed += Time.unscaledDeltaTime;
            shown = screen.AdvanceProgress(shown, real, elapsed);
            screen.SetLoadingProgress(shown);
            yield return null;
        }

        screen.SetLoadingProgress(1f);
        yield return screen.HoldAtFull();
    }

    // ==========================================
    // FASE -> HUB (tidak ada loading: tidak ada yang dimuat)
    // ==========================================

    public void ReturnToHub(Transform focus)
    {
        if (IsBusy) return;

        if (!CanAnimate)
        {
            GameManager.Instance.ChangeState(GameState.Hub);
            return;
        }

        AudioManager.Music(MusicId.None); // musik fase lama berhenti saat zoom ke tombol fase baru
        StartCoroutine(ReturnToHubRoutine(focus));
    }

    private IEnumerator ReturnToHubRoutine(Transform focus)
    {
        IsBusy = true;

        ScreenTransition screen = ScreenTransition.Instance;
        GameManager gm = GameManager.Instance;

        // 1. Layar fase menggelap dengan cepat
        yield return screen.FadeToBlack(fadeToHubDuration);

        // 2. Di balik layar gelap: kembali ke Hub (bisa memicu pergantian hari kalau semua fase selesai)
        gm.ChangeState(GameState.Hub);

        // 3. Hub muncul dalam keadaan zoom di tombol fase tadi, lalu zoom-out sambil fade-in
        Vector3 restPosition = zoomRoot.localPosition;
        Vector3 focusWorld = UIZoom.GetWorldCenter((RectTransform)focus);

        float elapsed = 0f;
        while (elapsed < zoomOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / zoomOutDuration);

            float eased = 1f - (1f - t) * (1f - t); // melambat di akhir
            UIZoom.Apply(zoomRoot, restPosition, Mathf.Lerp(zoomScale, 1f, eased), focusWorld);
            screen.SetBlack(1f - Mathf.InverseLerp(0f, 0.6f, t)); // terang di 60% pertama

            yield return null;
        }

        AudioManager.Music(MusicId.InGame); // musik fase InGame baru dimulai setelah zoom-out selesai
        UIZoom.Reset(zoomRoot, restPosition);
        screen.SetBlack(0f);
        screen.SetBlocking(false);

        IsBusy = false;
    }
}