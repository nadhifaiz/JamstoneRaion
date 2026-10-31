using System.Collections.Generic;
using UnityEngine;

// Season aktif ditentukan dari hari sekarang (GameManager.currentDay), jadi tidak ada state tambahan
// yang perlu disimpan. Taruh di objek yang selalu aktif.
public class SeasonManager : MonoBehaviour
{
    public static SeasonManager Instance { get; private set; }

    [SerializeField] private StoryConfigSO story;

    public StoryConfigSO Story => story;

    private int CurrentDay => GameManager.Instance != null ? GameManager.Instance.currentDay : 1;

    public SeasonSO CurrentSeason => story != null ? story.GetSeason(CurrentDay) : null;

    public int CurrentDayInSeason => story != null ? story.GetDayInSeason(CurrentDay) : 1;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (story == null) Debug.LogError("[SeasonManager] 'story' belum di-assign.", this);
    }

    // Pengali harga untuk barang ini di season sekarang (1 = tidak ada kejadian)
    public float GetPriceMultiplier(CraftedItemSO item)
    {
        SeasonSO season = CurrentSeason;
        if (season == null || item == null) return 1f;

        float multiplier = 1f;
        foreach (SeasonPriceEvent evt in season.priceEvents)
        {
            if (evt.item == item) multiplier *= evt.multiplier;
        }
        return multiplier;
    }

    public int ApplyPrice(CraftedItemSO item, int basePrice)
    {
        return Mathf.RoundToInt(basePrice * GetPriceMultiplier(item));
    }

    public IReadOnlyList<SeasonPriceEvent> GetActiveEvents()
    {
        SeasonSO season = CurrentSeason;
        return season != null ? season.priceEvents : new List<SeasonPriceEvent>();
    }
}