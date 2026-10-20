using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// Satu slot etalase. Pasang di objek slot yang punya Image background (raycastTarget = true).
// itemImage = Image anak untuk menampilkan item. Klik slot terisi = tarik item kembali.
public class EtalaseSlotUI : MonoBehaviour, IPointerClickHandler
{
    public Image itemImage;
    [Tooltip("Opsional: menampilkan harga jual item di slot")]
    public TextMeshProUGUI priceText;

    private EtalaseUIManager ui;
    public int Index { get; private set; }

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
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (ui != null) ui.RemoveFromSlot(Index);
    }
}