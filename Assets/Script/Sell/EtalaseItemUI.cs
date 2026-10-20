using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// Item barang jadi di grid kiri Etalase. Bisa di-drag ke slot atau diklik (masuk slot kosong pertama).
public class EtalaseItemUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [Header("UI References")]
    public Image itemIcon;
    public TextMeshProUGUI countText;
    [Tooltip("Opsional: menampilkan harga jual unit ini")]
    public TextMeshProUGUI priceText;

    private CraftedItemKey itemKey;
    private EtalaseUIManager ui;
    private GameObject ghostIcon;

    public void Initialize(CraftedItemKey key, int count, EtalaseUIManager manager)
    {
        itemKey = key;
        ui = manager;
        itemIcon.sprite = key.item.itemSprite;
        countText.text = count > 1 ? $"x{count}" : "";
        if (priceText != null) priceText.text = key.price.ToString();
    }

    void OnDisable()
    {
        // Jaga-jaga kalau panel ditutup saat drag berjalan
        if (ghostIcon != null) Destroy(ghostIcon);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.dragging) return;
        ui.PlaceInFirstEmpty(itemKey);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        ghostIcon = new GameObject("GhostIcon");
        ghostIcon.transform.SetParent(GetComponentInParent<Canvas>().transform, false);
        ghostIcon.transform.SetAsLastSibling();

        Image ghostImage = ghostIcon.AddComponent<Image>();
        ghostImage.sprite = itemKey.item.itemSprite;
        ghostImage.raycastTarget = false;

        ghostIcon.GetComponent<RectTransform>().sizeDelta = GetComponent<RectTransform>().sizeDelta;
        ghostIcon.transform.position = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (ghostIcon != null) ghostIcon.transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (ghostIcon != null) Destroy(ghostIcon);

        GameObject target = eventData.pointerCurrentRaycast.gameObject;
        EtalaseSlotUI slot = target != null ? target.GetComponentInParent<EtalaseSlotUI>() : null;

        // Stok tidak diubah selama drag; baru berkurang kalau drop berhasil
        if (slot != null) ui.PlaceItem(itemKey, slot.Index);
    }
}