using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Pemicu cutscene + status "sudah dilihat" (disimpan di save). Taruh di objek yang selalu aktif.
//
// Aturan pemicu (diturunkan dari data, bukan flag manual):
//   Saat masuk Hub, kalau season sekarang punya cutscene pembuka yang BELUM dilihat, putar.
//   -> game baru: pembuka season 1; pergantian season: pembuka season berikutnya;
//      keluar di tengah cutscene: diputar ulang (karena baru ditandai dilihat setelah selesai);
//      lanjut save biasa: tidak diulang.
// Ending: diputar sekali di akhir hari terakhir cerita (lihat PlayEndingIfNeeded).
public class CutsceneManager : MonoBehaviour
{
    public static CutsceneManager Instance { get; private set; }

    [SerializeField] private CutscenePlayerBase player;   // pemutar yang dipakai (dummy sekarang)
    [SerializeField] private EndingScreenUI endingScreen;

    private readonly HashSet<string> seen = new HashSet<string>();

    public bool EndingSeen { get; private set; }
    public bool IsPlaying { get; private set; }
    public IEnumerable<string> SeenCutscenes => seen;

    // Dipicu saat status "sudah dilihat" berubah (dipakai SaveManager untuk live save)
    public event Action OnChanged;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // ==========================================
    // CUTSCENE PEMBUKA SEASON
    // ==========================================

    // Dipanggil GameManager.EnterHub (game dimulai/dimuat). Tidak melakukan apa-apa saat transisi hari,
    // karena transisi memanggil PlayPendingRoutine() sendiri supaya urutannya tertata.
    public void TryPlayPending()
    {
        if (IsPlaying) return;
        if (GameManager.Instance != null && GameManager.Instance.IsTransitioning) return;
        if (GetPendingCutscene() == null) return;

        StartCoroutine(PlayPendingRoutine());
    }

    // Bisa di-yield dari coroutine lain (dipakai transisi pergantian hari)
    public IEnumerator PlayPendingRoutine()
    {
        if (IsPlaying) yield break;

        CutsceneSO pending = GetPendingCutscene();
        if (pending == null) yield break;

        yield return PlayAndMarkSeen(pending);
    }

    private CutsceneSO GetPendingCutscene()
    {
        SeasonManager seasons = SeasonManager.Instance;
        if (seasons == null) return null;

        CutsceneSO intro = seasons.CurrentSeason != null ? seasons.CurrentSeason.introCutscene : null;
        if (intro == null || seen.Contains(intro.Id)) return null;

        return intro;
    }

    private IEnumerator PlayAndMarkSeen(CutsceneSO cutscene)
    {
        if (player == null)
        {
            Debug.LogError("[CutsceneManager] 'player' belum di-assign, cutscene dilewati.", this);
            yield break;
        }

        IsPlaying = true;
        yield return player.Play(cutscene);

        // Ditandai dilihat SETELAH selesai: kalau game ditutup di tengah cutscene, diputar ulang
        if (!string.IsNullOrEmpty(cutscene.Id)) seen.Add(cutscene.Id);
        else Debug.LogError($"[CutsceneManager] Cutscene '{cutscene.name}' belum punya ID, tidak bisa ditandai sudah dilihat.", cutscene);

        IsPlaying = false;
        OnChanged?.Invoke();
    }

    // ==========================================
    // ENDING
    // ==========================================

    // Dipanggil dari transisi pergantian hari, SEBELUM hari dinaikkan. Hanya berlaku di hari terakhir cerita
    // dan hanya sekali; setelahnya game lanjut sebagai endless.
    public IEnumerator PlayEndingIfNeeded(int day)
    {
        SeasonManager seasons = SeasonManager.Instance;
        if (EndingSeen || seasons == null || seasons.Story == null || !seasons.Story.IsStoryEndDay(day)) yield break;

        IsPlaying = true;

        if (seasons.Story.endingCutscene != null && player != null)
        {
            yield return player.Play(seasons.Story.endingCutscene);
        }

        if (endingScreen != null)
        {
            yield return endingScreen.Show(seasons.Story.endingType);
        }
        else
        {
            Debug.LogError("[CutsceneManager] 'endingScreen' belum di-assign, layar ending dilewati.", this);
        }

        EndingSeen = true;
        IsPlaying = false;
        OnChanged?.Invoke(); // live save menulis EndingSeen di akhir frame ini

        if (endingScreen != null && endingScreen.ReturnToMenuRequested)
        {
            yield return endingScreen.GoToMenu();
        }
    }

    // ==========================================
    // LOAD DARI SAVE
    // ==========================================

    // Mengganti status secara diam-diam (tanpa OnChanged), khusus dipakai saat load save
    public void LoadState(IEnumerable<string> seenCutscenes, bool endingSeen)
    {
        seen.Clear();
        foreach (string id in seenCutscenes)
        {
            if (!string.IsNullOrEmpty(id)) seen.Add(id);
        }
        EndingSeen = endingSeen;
    }
}