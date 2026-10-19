using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Satu slot etalase. Pasang di objek slot yang punya Image background (raycastTarget = true).
// itemImage = Image anak untuk menampilkan item. Klik slot terisi = tarik item kembali.
public class EtalaseSlotUI : MonoBehaviour, IPointerClickHandler
{
    public Image itemImage;

    private EtalaseUIManager ui;
    public int Index { get; private set; }

    public void Init(EtalaseUIManager manager, int index)
    {
        ui = manager;
        Index = index;
    }

    public void SetItem(CraftedItemSO item)
    {
        bool hasItem = item != null;
        itemImage.enabled = hasItem;
        itemImage.sprite = hasItem ? item.itemSprite : null;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (ui != null) ui.RemoveFromSlot(Index);
    }
}