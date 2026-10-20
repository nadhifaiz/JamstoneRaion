using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryItemUI : MonoBehaviour
{
    public Image itemIcon;
    public TextMeshProUGUI countText;
    [Tooltip("Opsional: harga jual, dipakai tab Barang Jadi. Kosongkan kalau prefab tidak butuh.")]
    public TextMeshProUGUI priceText;

    public void Initialize(Sprite icon, int count, int price = -1)
    {
        itemIcon.sprite = icon;
        countText.text = count > 1 ? $"x{count}" : "";

        if (priceText != null)
        {
            priceText.gameObject.SetActive(price >= 0);
            if (price >= 0) priceText.text = price.ToString();
        }
    }
}