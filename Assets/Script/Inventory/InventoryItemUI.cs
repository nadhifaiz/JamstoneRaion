using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryItemUI : MonoBehaviour
{
    public Image itemIcon;
    public TextMeshProUGUI countText;

    public void Initialize(Sprite icon, int count)
    {
        itemIcon.sprite = icon;
        countText.text = count > 1 ? $"x{count}" : "";
    }
}