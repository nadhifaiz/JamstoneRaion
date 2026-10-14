using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CraftingSlot : MonoBehaviour, IPointerClickHandler
{
    public CraftingUIManager uiManager;

    [Header("UI Visual")]
    public Image slotImage;
    public Sprite emptySprite;

    private MaterialSO currentMaterial = null;
    private MaterialUI originItemUI = null;

    void Awake()
    {
        // Fallback kalau referensi lupa diisi di inspector
        if (uiManager == null) uiManager = GetComponentInParent<CraftingUIManager>();
        if (slotImage == null) slotImage = GetComponent<Image>();

        if (uiManager == null) Debug.LogError($"[CraftingSlot] uiManager belum di-assign di {name}", this);
        if (slotImage == null) Debug.LogError($"[CraftingSlot] slotImage belum di-assign di {name}", this);
    }

    public bool IsEmpty() => currentMaterial == null;

    public void FillSlot(MaterialUI originItem)
    {
        currentMaterial = originItem.materialData;
        originItemUI = originItem;

        slotImage.sprite = currentMaterial.itemSprite;
        slotImage.color = Color.white;

        uiManager.UpdateSlotData(this, currentMaterial);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
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
        // Opsional: Transparan jika tidak ada gambar emptySprite
        // slotImage.color = new Color(1, 1, 1, 0);

        uiManager.UpdateSlotData(this, null);
    }
}