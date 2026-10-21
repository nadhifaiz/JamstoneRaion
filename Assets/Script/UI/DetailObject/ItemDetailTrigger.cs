using UnityEngine;
using UnityEngine.EventSystems;

// Tempel di prefab item mana pun (material atau barang jadi). Klik 1x membuka panel rincian.
// Sengaja HANYA mengimplementasi klik, bukan drag, supaya drag pada item tetap diteruskan
// ke ScrollRect induknya dan ke script drag milik item itu sendiri.
// Aksi cepat memakai klik 2x di script masing-masing (MaterialUI, EtalaseItemUI, slot).
public class ItemDetailTrigger : MonoBehaviour, IPointerClickHandler
{
    public enum SourceKind { None, Material, CraftedItem }

    public SourceKind Kind { get; private set; }
    public MaterialSO Material { get; private set; }
    public CraftedItemKey CraftedItem { get; private set; }

    // true = tampilkan baris "Dimiliki" (jumlah di inventory). Matikan untuk item yang sedang di slot meja/etalase.
    public bool ShowStock { get; private set; }

    public void SetMaterial(MaterialSO material, bool showStock)
    {
        Kind = material != null ? SourceKind.Material : SourceKind.None;
        Material = material;
        CraftedItem = default;
        ShowStock = showStock;
    }

    public void SetCraftedItem(CraftedItemKey key, bool showStock)
    {
        Kind = key.IsEmpty ? SourceKind.None : SourceKind.CraftedItem;
        CraftedItem = key;
        Material = null;
        ShowStock = showStock;
    }

    public void Clear()
    {
        Kind = SourceKind.None;
        Material = null;
        CraftedItem = default;
        HidePanelIfOwner();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (Kind == SourceKind.None) return;
        if (eventData.dragging || eventData.clickCount != 1) return;

        if (ItemDetailPanel.Instance != null) ItemDetailPanel.Instance.Show(this);
    }

    // Item hilang/grid dibangun ulang: kalau panel sedang menampilkan item ini, tutup
    void OnDisable()
    {
        HidePanelIfOwner();
    }

    private void HidePanelIfOwner()
    {
        if (ItemDetailPanel.Instance != null) ItemDetailPanel.Instance.HideIfOwner(this);
    }
}