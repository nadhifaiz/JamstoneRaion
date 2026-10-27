using UnityEngine;
using UnityEngine.UI;
using TMPro;

// View statis item di panel Inventory (material maupun barang jadi). Tidak bisa di-drag.
// Pasang juga ItemDetailTrigger di prefab agar bisa diklik untuk melihat rincian.
public class InventoryItemUI : MonoBehaviour
{
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI countText;
    [Tooltip("Opsional: harga jual, hanya tampil untuk barang jadi")]
    [SerializeField] private TextMeshProUGUI priceText;
    [Tooltip("Opsional: penanda tag, hanya tampil untuk material")]
    [SerializeField] private TagBadgeUI tagBadge;

    private ItemDetailTrigger detailTrigger;

    void Awake()
    {
        detailTrigger = GetComponent<ItemDetailTrigger>();
    }

    public void InitializeMaterial(MaterialSO material, int count)
    {
        itemIcon.sprite = material.itemSprite;
        SetCount(count);

        if (priceText != null) priceText.gameObject.SetActive(false);
        if (tagBadge != null) tagBadge.Show(material.materialType);
        if (detailTrigger != null) detailTrigger.SetMaterial(material, true);
    }

    public void InitializeCrafted(CraftedItemKey key, int count)
    {
        itemIcon.sprite = key.item.itemSprite;
        SetCount(count);

        if (priceText != null)
        {
            priceText.gameObject.SetActive(true);
            priceText.text = key.price.ToString();
        }
        if (tagBadge != null) tagBadge.Hide();
        if (detailTrigger != null) detailTrigger.SetCraftedItem(key, true);
    }

    private void SetCount(int count)
    {
        countText.text = count > 1 ? $"x{count}" : "";
    }
}