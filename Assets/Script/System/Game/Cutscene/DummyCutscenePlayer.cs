using System.Collections;
using TMPro;
using UnityEngine;

// Nanti Di script ini bisa menetapkan logika cutscene
public class DummyCutscenePlayer : CutscenePlayerBase
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI bodyText;

    [Tooltip("Tutup otomatis setelah N detik. 0 = hanya menunggu tombol Skip.")]
    [SerializeField] private float autoSkipSeconds = 1.5f;

    private bool finished;

    void Awake()
    {
        panelRoot.SetActive(false);
    }

    public override IEnumerator Play(CutsceneSO cutscene)
    {
        finished = false;
        titleText.text = cutscene.title;
        bodyText.text = cutscene.placeholderText;
        panelRoot.SetActive(true);

        float elapsed = 0f;
        while (!finished)
        {
            elapsed += Time.unscaledDeltaTime;
            if (autoSkipSeconds > 0f && elapsed >= autoSkipSeconds) break;
            yield return null;
        }

        panelRoot.SetActive(false);
    }

    // Hubungkan ke tombol Skip di panel
    public void Button_Skip() => finished = true;
}