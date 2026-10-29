using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Animasi crafting anvil + palu (uGUI, top down overlay).
/// Dipanggil dari CraftingUIManager:
///   yield return PlayHammerPhase();            // overlay, anvil, palu memalu 5x
///   ... konsumsi bahan, cek resep ...
///   yield return PlayReveal(sprite, nama, ok); // flash + item pop + overlay hilang
/// Semua timing pakai unscaledDeltaTime, jadi aman kalau Time.timeScale = 0.
/// </summary>
public class CraftingAnimationController : MonoBehaviour
{
    [Header("Root")]
    [Tooltip("Panel fullscreen. Isinya Image hitam (alpha ~0.68) + semua objek animasi. Harus ada CanvasGroup.")]
    public GameObject overlayRoot;
    public CanvasGroup overlayGroup;
    [Tooltip("Parent anvil + efek. Inilah yang digoyang saat screen shake.")]
    public RectTransform workArea;

    [Header("Anvil")]
    public CanvasGroup anvilGroup;
    public RectTransform anvil;
    [Tooltip("Image besi panas di atas anvil")]
    public Image heatImage;
    [Tooltip("Image ring: Image Type = Filled, Radial 360, Origin Top")]
    public Image progressRing;
    public Color heatCold = new Color(0.70f, 0.27f, 0.12f);
    public Color heatHot = new Color(1.00f, 0.45f, 0.12f);

    [Header("Palu (pivot sprite = kepala palu, gagang menghadap kanan)")]
    public CanvasGroup hammerGroup;
    public RectTransform hammer;
    [Tooltip("Image duplikat palu, warna hitam alpha ~0.3")]
    public RectTransform hammerShadow;
    public Vector2 hammerEnterOffset = new Vector2(600f, 400f);

    [Header("Efek")]
    public RectTransform sparkContainer;
    [Tooltip("Image kotak kecil oranye (misal 8x8). Akan di-instantiate & dipool.")]
    public Image sparkPrefab;
    public Image flashImage;

    [Header("Hasil")]
    public Image resultImage;
    public TextMeshProUGUI resultNameText;
    [Tooltip("Opsional: kalau diisi, item terbang ke sini setelah muncul")]
    public RectTransform inventoryTarget;

    [Header("Timing")]
    public float[] strikePeriods = { 0.9f, 0.85f, 0.8f, 0.7f, 0.62f };
    public float firstStrikeDelay = 0.75f;
    [Tooltip("1 = persis demo (~4,6 dtk). Naikkan untuk mempercepat (2 = ~2,3 dtk)")]
    public float speed = 1f;
    public float holdTime = 0.9f;
    [Tooltip("Pengali ukuran efek kalau Canvas Scaler kamu beda dengan 420px tinggi")]
    public float unitScale = 1f;

    [Header("Audio (opsional)")]
    public AudioSource sfx;
    public AudioClip hitClip;
    public AudioClip successClip;
    public AudioClip failClip;

    // --- konstanta gerak palu (radian) ---
    const float AngBase = 0.6f;
    const float AngWind = 0.5f;
    const float ImpactAt = 0.72f;

    class Spark { public Image img; public Vector2 vel; public float life, max; }

    readonly List<Spark> active = new List<Spark>();
    readonly Stack<Image> pool = new Stack<Image>();
    float[] starts;
    float shake, squash;
    int hits;

    static float EaseOut(float t) { t = Mathf.Clamp01(t); return 1f - Mathf.Pow(1f - t, 3f); }
    static float EaseIn(float t) { t = Mathf.Clamp01(t); return t * t; }

    // ==========================================
    // FASE 1: OVERLAY + ANVIL + PALU MEMALU
    // ==========================================
    public IEnumerator PlayHammerPhase()
    {
        Prepare();
        int n = strikePeriods.Length;
        float total = starts[n - 1] + strikePeriods[n - 1] + 0.4f;
        float t = 0f;
        int hit = -1;

        while (t < total)
        {
            t += Time.unscaledDeltaTime * speed;

            // overlay & anvil fade in + scale 0.9 -> 1.0
            float intro = Mathf.Clamp01(t / 0.35f);
            overlayGroup.alpha = intro;
            anvilGroup.alpha = EaseOut(intro);
            anvilGroup.transform.localScale = Vector3.one * (0.9f + 0.1f * EaseOut(intro));

            // deteksi impact tiap ketukan
            for (int i = 0; i < n; i++)
            {
                float impactTime = starts[i] + strikePeriods[i] * ImpactAt;
                if (t >= impactTime && hit < i)
                {
                    hit = i;
                    hits = i + 1;
                    bool last = i == n - 1;
                    Burst(last);
                    shake = last ? 9f : 4f;
                    squash = 1f;
                    Sfx(hitClip, 1f + i * 0.12f); // pitch naik tiap ketukan
                }
            }

            UpdateHammer(t, n);

            // progress ring
            float pr = 0f;
            for (int i = 0; i < n; i++)
                pr += Mathf.Clamp01((t - starts[i]) / (strikePeriods[i] * ImpactAt));
            progressRing.fillAmount = pr / n;

            yield return null;
        }
        progressRing.fillAmount = 1f;
    }

    void UpdateHammer(float t, int n)
    {
        float h = 1f;
        Vector2 off = Vector2.zero;
        float alpha = 1f;

        if (t < starts[0])
        {
            // palu meluncur masuk dari pojok kanan atas
            const float enterDelay = 0.35f, enterTime = 0.4f;
            float e = EaseOut((t - enterDelay) / enterTime);
            off = new Vector2((1f - e) * hammerEnterOffset.x, (1f - e) * hammerEnterOffset.y);
            alpha = Mathf.Clamp01((t - enterDelay) / 0.2f);
        }
        else
        {
            int i = 0;
            while (i < n - 1 && t >= starts[i + 1]) i++;
            float p = Mathf.Clamp01((t - starts[i]) / strikePeriods[i]);

            if (p < 0.55f) h = EaseOut(p / 0.55f);                       // windup: naik
            else if (p < ImpactAt) h = 1f - EaseIn((p - 0.55f) / 0.17f); // strike: turun cepat
            else { float k = 1f - (p - ImpactAt) / (1f - ImpactAt); h = 0.12f * k * k; } // pantulan kecil
        }

        float angDeg = (AngBase + AngWind * h) * Mathf.Rad2Deg;
        float scale = 1f + h * 0.35f; // makin tinggi makin "dekat ke kamera"
        Quaternion rot = Quaternion.Euler(0f, 0f, angDeg);

        hammerGroup.alpha = alpha;
        hammer.anchoredPosition = off;
        hammer.localRotation = rot;
        hammer.localScale = Vector3.one * scale;

        // bayangan menjauh saat palu naik, menyatu saat menghantam
        hammerShadow.anchoredPosition = off + new Vector2(h * 34f, -h * 34f) * unitScale;
        hammerShadow.localRotation = rot;
        hammerShadow.localScale = Vector3.one * scale;
    }

    // ==========================================
    // FASE 2: FLASH + ITEM MUNCUL + OVERLAY HILANG
    // ==========================================
    public IEnumerator PlayReveal(Sprite sprite, string label, bool success)
    {
        // palu & anvil fade out
        float t = 0f;
        while (t < 0.3f)
        {
            t += Time.unscaledDeltaTime;
            float a = 1f - Mathf.Clamp01(t / 0.3f);
            hammerGroup.alpha = a;
            anvilGroup.alpha = a;
            yield return null;
        }
        hammerGroup.alpha = 0f;
        anvilGroup.alpha = 0f;

        // siapkan item hasil
        resultImage.sprite = sprite;
        resultImage.color = success ? Color.white : new Color(0.6f, 0.6f, 0.6f, 1f);
        resultImage.rectTransform.localScale = Vector3.zero;
        resultImage.rectTransform.anchoredPosition = Vector2.zero;
        resultImage.gameObject.SetActive(true);
        resultNameText.text = label;
        resultNameText.alpha = 0f;
        resultNameText.gameObject.SetActive(true);

        if (success)
        {
            StartCoroutine(FlashRoutine());
            Burst(true);
            Sfx(successClip, 1f);
        }
        else
        {
            shake = 5f;
            Sfx(failClip, 1f);
        }

        // pop: 0 -> 1.2 -> 1.0
        float failMul = success ? 1f : 0.8f;
        t = 0f;
        while (t < 0.4f)
        {
            t += Time.unscaledDeltaTime;
            float s = t < 0.25f ? 1.2f * EaseOut(t / 0.25f) : 1.2f - 0.2f * ((t - 0.25f) / 0.15f);
            resultImage.rectTransform.localScale = Vector3.one * (s * failMul);
            resultNameText.alpha = Mathf.Clamp01(t / 0.3f);
            yield return null;
        }
        resultImage.rectTransform.localScale = Vector3.one * failMul;

        // tahan sebentar
        t = 0f;
        while (t < holdTime) { t += Time.unscaledDeltaTime; yield return null; }

        // terbang ke inventory (opsional)
        if (success && inventoryTarget != null)
        {
            RectTransform rt = resultImage.rectTransform;
            Vector3 from = rt.localPosition;
            Vector3 to = rt.parent.InverseTransformPoint(inventoryTarget.position);
            t = 0f;
            while (t < 0.5f)
            {
                t += Time.unscaledDeltaTime;
                float e = EaseIn(t / 0.5f);
                rt.localPosition = Vector3.Lerp(from, to, e);
                rt.localScale = Vector3.one * Mathf.Lerp(1f, 0.35f, e);
                resultNameText.alpha = 1f - e;
                yield return null;
            }
        }

        // overlay fade out
        t = 0f;
        while (t < 0.4f)
        {
            t += Time.unscaledDeltaTime;
            overlayGroup.alpha = 1f - Mathf.Clamp01(t / 0.4f);
            yield return null;
        }
        HideImmediate();
    }

    IEnumerator FlashRoutine()
    {
        float t = 0f;
        while (t < 0.2f)
        {
            t += Time.unscaledDeltaTime;
            flashImage.color = new Color(1f, 1f, 1f, (1f - t / 0.2f) * 0.9f);
            yield return null;
        }
        flashImage.color = Color.clear;
    }

    // ==========================================
    // UTILITAS
    // ==========================================
    void Prepare()
    {
        overlayRoot.SetActive(true);
        overlayGroup.alpha = 0f;
        anvilGroup.alpha = 0f;
        hammerGroup.alpha = 0f;
        workArea.anchoredPosition = Vector2.zero;
        anvil.localScale = Vector3.one;
        progressRing.fillAmount = 0f;
        heatImage.color = heatCold;
        flashImage.color = Color.clear;
        resultImage.gameObject.SetActive(false);
        resultNameText.gameObject.SetActive(false);

        starts = new float[strikePeriods.Length];
        float a = firstStrikeDelay;
        for (int i = 0; i < strikePeriods.Length; i++) { starts[i] = a; a += strikePeriods[i]; }

        hits = 0; shake = 0f; squash = 0f;
        ClearSparks();
    }

    /// <summary>Tutup animasi langsung (dipakai kalau craft dibatalkan di tengah jalan).</summary>
    public void HideImmediate()
    {
        StopAllCoroutines();
        ClearSparks();
        shake = 0f; squash = 0f;
        if (workArea != null) workArea.anchoredPosition = Vector2.zero;
        if (overlayRoot != null) overlayRoot.SetActive(false);
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;

        // screen shake & squash meluruh cepat
        shake *= Mathf.Pow(0.001f, dt);
        squash *= Mathf.Pow(0.0005f, dt);
        workArea.anchoredPosition = Random.insideUnitCircle * shake * unitScale;
        anvil.localScale = new Vector3(1f, 1f - 0.07f * squash, 1f);

        // besi makin panas tiap ketukan, berpendar sesaat saat kena
        Color heat = Color.Lerp(heatCold, heatHot, strikePeriods.Length > 0 ? (float)hits / strikePeriods.Length : 0f);
        heatImage.color = Color.Lerp(heat, new Color(1f, 0.85f, 0.5f), squash * 0.5f);

        UpdateSparks(dt);
    }

    // --- percikan api ---
    void Burst(bool big)
    {
        int n = big ? 40 : 16;
        for (int i = 0; i < n; i++)
        {
            float ang = Random.value * Mathf.PI * 2f;
            float spd = (100f + Random.value * (big ? 380f : 240f)) * unitScale;
            Image img = pool.Count > 0 ? pool.Pop() : Instantiate(sparkPrefab, sparkContainer);
            img.gameObject.SetActive(true);
            img.rectTransform.anchoredPosition = Vector2.zero;
            active.Add(new Spark
            {
                img = img,
                vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * spd,
                life = 0f,
                max = 0.35f + Random.value * 0.35f
            });
        }
    }

    void UpdateSparks(float dt)
    {
        for (int i = active.Count - 1; i >= 0; i--)
        {
            Spark s = active[i];
            s.life += dt;
            float k = 1f - s.life / s.max;
            if (k <= 0f)
            {
                s.img.gameObject.SetActive(false);
                pool.Push(s.img);
                active.RemoveAt(i);
                continue;
            }
            s.img.rectTransform.anchoredPosition += s.vel * dt;
            s.vel *= Mathf.Pow(0.96f, dt * 60f);
            s.img.color = new Color(1f, 0.55f + 0.4f * k, 0.16f, k);
        }
    }

    void ClearSparks()
    {
        foreach (Spark s in active)
        {
            s.img.gameObject.SetActive(false);
            pool.Push(s.img);
        }
        active.Clear();
    }

    void Sfx(AudioClip clip, float pitch)
    {
        if (sfx == null || clip == null) return;
        sfx.pitch = pitch;
        sfx.PlayOneShot(clip);
    }
}