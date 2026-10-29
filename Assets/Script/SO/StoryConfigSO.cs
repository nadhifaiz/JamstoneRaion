using System.Collections.Generic;
using UnityEngine;

public enum EndingType
{
    ToBeContinued,   // "Bersambung..."
    TheEnd           // "Tamat"
}

// Susunan cerita: daftar season berurutan + jenis ending. Menambah season baru = menambah aset ke daftar,
// ending pindah otomatis ke akhir season terakhir. Setelah cerita habis, game lanjut endless:
// season berulang dari awal.
[CreateAssetMenu(fileName = "StoryConfig", menuName = "Game/Story Config")]
public class StoryConfigSO : ScriptableObject
{
    [SerializeField] private List<SeasonSO> seasons = new List<SeasonSO>();

    [Header("Ending")]
    public EndingType endingType = EndingType.ToBeContinued;
    [Tooltip("Cutscene ending (opsional). Versi 1: kosongkan, cukup layar Bersambung.")]
    public CutsceneSO endingCutscene;

    public int TotalStoryDays
    {
        get
        {
            int total = 0;
            for (int i = 0; i < seasons.Count; i++) total += GetDays(i);
            return total;
        }
    }

    // Hari 1-based. Setelah hari cerita habis (endless), siklus kembali ke season pertama.
    public SeasonSO GetSeason(int day)
    {
        int index = GetSeasonIndex(day);
        return index >= 0 ? seasons[index] : null;
    }

    public int GetSeasonIndex(int day)
    {
        int total = TotalStoryDays;
        if (total <= 0) return -1;

        int dayInCycle = (Mathf.Max(1, day) - 1) % total; // 0-based
        for (int i = 0; i < seasons.Count; i++)
        {
            int days = GetDays(i);
            if (dayInCycle < days) return i;
            dayInCycle -= days;
        }
        return -1;
    }

    // Urutan hari di dalam season (1-based), mis. 3 untuk "hari ke-3 dari 7"
    public int GetDayInSeason(int day)
    {
        int total = TotalStoryDays;
        if (total <= 0) return 1;

        int dayInCycle = (Mathf.Max(1, day) - 1) % total;
        for (int i = 0; i < seasons.Count; i++)
        {
            int days = GetDays(i);
            if (dayInCycle < days) return dayInCycle + 1;
            dayInCycle -= days;
        }
        return 1;
    }

    // Hari terakhir cerita: saat hari ini selesai, ending diputar (sekali saja)
    public bool IsStoryEndDay(int day)
    {
        int total = TotalStoryDays;
        return total > 0 && day == total;
    }

    private int GetDays(int index)
    {
        return seasons[index] != null ? seasons[index].days : 0;
    }
}