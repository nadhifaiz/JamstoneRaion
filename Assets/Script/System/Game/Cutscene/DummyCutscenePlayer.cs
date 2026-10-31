using System.Collections;
using TMPro;
using UnityEngine;

// Pemutar sementara: menampilkan panel berisi judul + teks (dengan fade), lalu ditutup otomatis atau lewat tombol Skip.
// Panel perlu background layar penuh dengan raycastTarget aktif dan Canvas di atas layar transisi hari.
public class DummyCutscenePlayer : CutscenePlayerBase
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI bodyText;

    [Tooltip("Tutup otomatis setelah N detik. 0 = hanya menunggu tombol Skip.")]
    [SerializeField] private float autoSkipSeconds = 1.5f;
    [Tooltip("Lama fade masuk dan keluar (detik). 0 = tanpa fade.")]
    [SerializeField] private float fadeDuration = 0.4f;

    private CanvasGroup canvasGroup;
    private bool finished;

    void Awake()
    {
        // CanvasGroup ditambahkan otomatis di panel kalau belum ada
        canvasGroup = panelRoot.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = panelRoot.AddComponent<CanvasGroup>();

        panelRoot.SetActive(false);
    }

    public override IEnumerator Play(CutsceneSO cutscene)
    {
        finished = false;
        titleText.text = cutscene.title;
        bodyText.text = cutscene.placeholderText;

        canvasGroup.alpha = 0f;
        panelRoot.SetActive(true);
        yield return UIFade.Fade(canvasGroup, 0f, 1f, fadeDuration);

        float elapsed = 0f;
        while (!finished)
        {
            elapsed += Time.unscaledDeltaTime;
            if (autoSkipSeconds > 0f && elapsed >= autoSkipSeconds) break;
            yield return null;
        }

        yield return UIFade.Fade(canvasGroup, 1f, 0f, fadeDuration);
        panelRoot.SetActive(false);
    }

    // Hubungkan ke tombol Skip di panel
    public void Button_Skip() => finished = true;
}