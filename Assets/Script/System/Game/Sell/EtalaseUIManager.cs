using System.Linq;
using UnityEngine;

// Letakkan di dalam sellUIPanel. panelRoot = panel "Etalase" yang di-toggle.
public class EtalaseUIManager : MonoBehaviour
{
    [Header("References")]
    public EtalaseManager etalase;
    public SellManager sellManager;
    public GameObject panelRoot;
    public Transform gridContent;
    public GameObject gridItemPrefab;          // prefab dengan EtalaseItemUI
    public EtalaseSlotUI[] slotUIs;            // urutan array = urutan slot (tanpa ID manual)

    public bool IsOpen => panelRoot.activeSelf;

    void OnEnable()
    {
        for (int i = 0; i < slotUIs.Length; i++) slotUIs[i].Init(this, i);
        if (etalase != null) etalase.OnChanged += RefreshSlots;
    }

    void OnDisable()
    {
        if (etalase != null) etalase.OnChanged -= RefreshSlots;
    }

    public void Open()
    {
        panelRoot.SetActive(true);
        RefreshGrid();
        RefreshSlots();
    }

    // Tombol "Mulai Jualan" / tutup etalase
    public void Close()
    {
        panelRoot.SetActive(false);
        if (sellManager != null) sellManager.NotifyEtalaseClosed();
    }

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    // Hanya slot yang di-refresh saat etalase berubah (mis. pelanggan beli),
    // grid TIDAK dibangun ulang supaya drag yang sedang berjalan tidak rusak.
    private void RefreshSlots()
    {
        if (etalase.SlotCount > slotUIs.Length)
        {
            Debug.LogWarning($"[EtalaseUIManager] Etalase punya {etalase.SlotCount} slot tapi UI hanya menyiapkan {slotUIs.Length}. Tambahkan slot UI.");
        }

        // Slot di atas jumlah yang sudah dibuka (upgrade) tampil terkunci
        for (int i = 0; i < slotUIs.Length; i++)
        {
            bool unlocked = i < etalase.SlotCount;
            slotUIs[i].SetLocked(!unlocked);
            if (unlocked) slotUIs[i].SetItem(etalase.GetSlot(i));
        }
    }

    private void RefreshGrid()
    {
        var items = InventoryManager.Instance.craftedItems
            .Where(kvp => kvp.Value > 0)
            .OrderBy(kvp => kvp.Key.item.itemName)
            .ThenByDescending(kvp => kvp.Key.price)
            .ToList();

        UIListHelper.Populate(gridContent, gridItemPrefab, items, (go, kvp) =>
        {
            go.GetComponent<EtalaseItemUI>().Initialize(kvp.Key, kvp.Value, this);
        });
    }

    // --- Dipanggil EtalaseItemUI / EtalaseSlotUI / tombol ---

    public void PlaceItem(CraftedItemKey key, int slotIndex)
    {
        if (etalase.TryPlace(key, slotIndex)) RefreshGrid();
    }

    public void PlaceInFirstEmpty(CraftedItemKey key)
    {
        if (etalase.TryPlaceFirstEmpty(key)) RefreshGrid();
    }

    public void RemoveFromSlot(int slotIndex)
    {
        if (etalase.Remove(slotIndex)) RefreshGrid();
    }

    // Tombol "put all item": isi slot kosong dengan stok yang ada (harga tertinggi lebih dulu)
    public void PutAll()
    {
        var snapshot = InventoryManager.Instance.craftedItems
            .Where(kvp => kvp.Value > 0)
            .OrderBy(kvp => kvp.Key.item.itemName)
            .ThenByDescending(kvp => kvp.Key.price)
            .ToList(); // snapshot, karena dictionary berubah saat item ditaruh

        foreach (var kvp in snapshot)
        {
            for (int n = 0; n < kvp.Value; n++)
            {
                if (!etalase.TryPlaceFirstEmpty(kvp.Key))
                {
                    RefreshGrid();
                    return;
                }
            }
        }
        RefreshGrid();
    }
}