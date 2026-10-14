using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class MaterialUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [Header("UI References")]
    public Image itemIcon;
    public TextMeshProUGUI countText;

    [HideInInspector] public MaterialSO materialData;
    [HideInInspector] public CraftingUIManager uiManager;

    private int currentDisplayCount;
    private GameObject ghostIcon;
    private CanvasGroup cg;

    void Awake()
    {
        // Dipakai untuk menyembunyikan item tanpa SetActive(false),
        // supaya event drag tetap jalan saat stok tinggal 1.
        cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
    }

    public void Initialize(MaterialSO data, int count, CraftingUIManager manager)
    {
        materialData = data;
        currentDisplayCount = count;
        uiManager = manager;

        itemIcon.sprite = data.itemSprite;
        UpdateCountText();
        ApplyVisibility();
    }

    public void UpdateDisplayCount(int change)
    {
        currentDisplayCount += change;
        UpdateCountText();
        ApplyVisibility();
    }

    private void ApplyVisibility()
    {
        bool visible = currentDisplayCount > 0;
        cg.alpha = visible ? 1f : 0f;
        cg.blocksRaycasts = visible;
    }

    private void UpdateCountText()
    {
        countText.text = currentDisplayCount > 1 ? $"x{currentDisplayCount}" : "";
    }

    // --- KLIK: isi slot kosong pertama ---

    public void OnPointerClick(PointerEventData eventData)
    {
        // Unity tidak mengirim click kalau sebelumnya terjadi drag,
        // tapi tetap dijaga untuk aman.
        if (eventData.dragging) return;
        if (currentDisplayCount <= 0) return;

        CraftingSlot slot = uiManager.GetFirstEmptySlot();
        if (slot == null) return;

        UpdateDisplayCount(-1);
        slot.FillSlot(this);
    }

    // --- DRAG BAYANGAN ---

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentDisplayCount <= 0) return;
        UpdateDisplayCount(-1);

        ghostIcon = new GameObject("GhostIcon");
        ghostIcon.transform.SetParent(GetComponentInParent<Canvas>().transform, false);
        ghostIcon.transform.SetAsLastSibling();

        Image ghostImage = ghostIcon.AddComponent<Image>();
        ghostImage.sprite = materialData.itemSprite;
        ghostImage.raycastTarget = false;

        RectTransform rt = ghostIcon.GetComponent<RectTransform>();
        rt.sizeDelta = GetComponent<RectTransform>().sizeDelta;
        ghostIcon.transform.position = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (ghostIcon != null)
        {
            ghostIcon.transform.position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (ghostIcon != null) Destroy(ghostIcon);

        GameObject dropTarget = eventData.pointerCurrentRaycast.gameObject;

        // InParent: aman kalau raycast kena child slot (ikon/teks)
        CraftingSlot slot = dropTarget != null ? dropTarget.GetComponentInParent<CraftingSlot>() : null;

        if (slot != null && slot.IsEmpty())
        {
            slot.FillSlot(this);
        }
        else
        {
            UpdateDisplayCount(1);
        }
    }
}