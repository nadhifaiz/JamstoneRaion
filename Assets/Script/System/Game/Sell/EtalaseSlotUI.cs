using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// Satu slot etalase. Pasang di objek slot yang punya Image background (raycastTarget = true).
// itemImage = Image anak untuk menampilkan item. Double-click slot terisi = tarik item kembali.
// Pasang ItemDetailTrigger di objek yang sama untuk rincian (klik 1x).
public class EtalaseSlotUI : MonoBehaviour, IPointerClickHandler
{
    public Image itemImage;
    [Tooltip("Opsional: menampilkan harga jual item di slot")]
    public TextMeshProUGUI priceText;

    private EtalaseUIManager ui;
    private ItemDetailTrigger detailTrigger;
    public int Index { get; private set; }

    void Awake()
    {
        detailTrigger = GetComponent<ItemDetailTrigger>();
    }

    public void Init(EtalaseUIManager manager, int index)
    {
        ui = manager;
        Index = index;
    }

    public void SetItem(CraftedItemKey key)
    {
        bool hasItem = !key.IsEmpty;
        itemImage.enabled = hasItem;
        itemImage.sprite = hasItem ? key.item.itemSprite : null;

        if (priceText != null)
        {
            priceText.gameObject.SetActive(hasItem);
            if (hasItem) priceText.text = key.price.ToString();
        }

        // Stok tidak ditampilkan: unit di etalase sudah keluar dari inventory
        if (detailTrigger != null)
        {
            if (hasItem) detailTrigger.SetCraftedItem(key, false);
            else detailTrigger.Clear();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.clickCount != 2) return;
        if (ui != null) ui.RemoveFromSlot(Index);
    }
}