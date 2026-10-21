using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CraftingSlot : MonoBehaviour, IPointerClickHandler
{
    public CraftingUIManager uiManager;

    [Header("UI Visual")]
    public Image slotImage;
    public Sprite emptySprite;
    [SerializeField] private TagBadgeUI tagBadge; // opsional: tag material yang ada di slot

    private MaterialSO currentMaterial = null;
    private MaterialUI originItemUI = null;
    private ItemDetailTrigger detailTrigger;

    void Awake()
    {
        // Fallback kalau referensi lupa diisi di inspector
        if (uiManager == null) uiManager = GetComponentInParent<CraftingUIManager>();
        if (slotImage == null) slotImage = GetComponent<Image>();
        detailTrigger = GetComponent<ItemDetailTrigger>();

        if (uiManager == null) Debug.LogError($"[CraftingSlot] uiManager belum di-assign di {name}", this);
        if (slotImage == null) Debug.LogError($"[CraftingSlot] slotImage belum di-assign di {name}", this);

        if (tagBadge != null) tagBadge.Hide();
    }

    public bool IsEmpty() => currentMaterial == null;

    public void FillSlot(MaterialUI originItem)
    {
        currentMaterial = originItem.materialData;
        originItemUI = originItem;

        slotImage.sprite = currentMaterial.itemSprite;
        slotImage.color = Color.white;

        // Stok tidak ditampilkan: material di slot masih tercatat di inventory sampai RAKIT
        if (detailTrigger != null) detailTrigger.SetMaterial(currentMaterial, false);
        if (tagBadge != null) tagBadge.Show(currentMaterial.materialType);

        uiManager.UpdateSlotData(this, currentMaterial);
    }

    // Double-click = tarik material kembali ke gudang (klik 1x dipakai ItemDetailTrigger)
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.clickCount != 2) return;

        if (!IsEmpty())
        {
            ClearSlotAndReturnItem();
        }
    }

    public void ClearSlotAndReturnItem()
    {
        if (originItemUI != null)
        {
            originItemUI.UpdateDisplayCount(1);
        }

        ClearSlot();
    }

    public void ClearSlot()
    {
        currentMaterial = null;
        originItemUI = null;

        slotImage.sprite = emptySprite;

        if (detailTrigger != null) detailTrigger.Clear();
        if (tagBadge != null) tagBadge.Hide();

        uiManager.UpdateSlotData(this, null);
    }
}