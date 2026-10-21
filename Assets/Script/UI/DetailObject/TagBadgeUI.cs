using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Badge kecil penanda tag di prefab material (grid crafting, inventory, slot meja).
// Referensi TagVisualsSO diisi di prefab (asset, bukan objek scene).
public class TagBadgeUI : MonoBehaviour
{
    [SerializeField] private TagVisualsSO tagVisuals;
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI label; // opsional

    public void Show(MaterialType tag)
    {
        gameObject.SetActive(true);

        Sprite sprite = tagVisuals.GetIcon(tag);
        icon.sprite = sprite;
        icon.enabled = sprite != null;

        if (label != null) label.text = tagVisuals.GetName(tag);
    }

    public void Hide() => gameObject.SetActive(false);
}