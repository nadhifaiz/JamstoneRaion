using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Satu baris slot save yang dapat diklik untuk menampilkan preview.
public class SaveSlotRowUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI slotNameText;
    [SerializeField] private TextMeshProUGUI dayText;
    [SerializeField] private TextMeshProUGUI amberText;
    [SerializeField] private TextMeshProUGUI lastPlayedText;
    [SerializeField] private GameObject completedBadge;
    [SerializeField] private Button selectButton;

    public void Bind(SaveSlotMeta meta, Action<SaveSlotMeta> onSelect)
    {
        if (meta == null) return;

        if (slotNameText != null) slotNameText.text = meta.slotName;
        if (dayText != null) dayText.text = $"Hari {meta.currentDay}";
        if (amberText != null) amberText.text = $"{meta.amber} Amber";

        if (lastPlayedText != null)
        {
            lastPlayedText.text = meta.LastPlayedLocal.ToString(
                "dd/MM/yyyy HH:mm",
                System.Globalization.CultureInfo.InvariantCulture);
        }

        if (completedBadge != null)
            completedBadge.SetActive(meta.isCompleted);

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() => onSelect?.Invoke(meta));
        }
    }
}
