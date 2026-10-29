using System.Text;
using TMPro;
using UnityEngine;

// Info season + "Kabar Pasar" (kejadian harga). Pasang di panel Hub dan panel Sell;
// diperbarui otomatis tiap panelnya diaktifkan.
public class SeasonInfoUI : MonoBehaviour
{
    private const string ColorUp = "#6FCF6F";
    private const string ColorDown = "#E05A5A";

    [SerializeField] private TextMeshProUGUI seasonNameText;
    [SerializeField] private TextMeshProUGUI dayInSeasonText;
    [SerializeField] private TextMeshProUGUI blurbText;
    [SerializeField] private TextMeshProUGUI eventsText;

    void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        SeasonManager manager = SeasonManager.Instance;
        if (manager == null) return;

        SeasonSO season = manager.CurrentSeason;
        if (season == null) return;

        if (seasonNameText != null) seasonNameText.text = season.displayName;
        if (dayInSeasonText != null) dayInSeasonText.text = $"Hari {manager.CurrentDayInSeason}/{season.days}";
        if (blurbText != null) blurbText.text = season.storyBlurb;
        if (eventsText != null) eventsText.text = BuildEventsText(season);
    }

    private static string BuildEventsText(SeasonSO season)
    {
        if (season.priceEvents.Count == 0) return "Tidak ada kabar khusus.";

        var sb = new StringBuilder();
        foreach (SeasonPriceEvent evt in season.priceEvents)
        {
            if (evt.item == null) continue;

            bool up = evt.multiplier >= 1f;
            int percent = Mathf.RoundToInt(Mathf.Abs(evt.multiplier - 1f) * 100f);

            if (sb.Length > 0) sb.Append('\n');
            sb.Append($"{evt.item.itemName} <color={(up ? ColorUp : ColorDown)}>{(up ? "naik" : "turun")} {percent}%</color>");
            if (!string.IsNullOrEmpty(evt.reason)) sb.Append($" - {evt.reason}");
        }
        return sb.ToString();
    }
}