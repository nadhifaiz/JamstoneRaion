using System.Linq;
using UnityEngine;

public class EtalaseUIManager : MonoBehaviour
{
    [Header("References")]
    public EtalaseManager etalase;
    public SellManager sellManager;
    public GameObject panelRoot;
    public Transform gridContent;
    public GameObject gridItemPrefab;
    public EtalaseSlotUI[] slotUIs;

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

    private void RefreshSlots()
    {
        int count = Mathf.Min(slotUIs.Length, etalase.SlotCount);
        for (int i = 0; i < count; i++) slotUIs[i].SetItem(etalase.GetSlot(i));
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

    // ==========================================
    // Fungsi Eksternal
    // ==========================================

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

    public void PutAll()
    {
        var snapshot = InventoryManager.Instance.craftedItems
            .Where(kvp => kvp.Value > 0)
            .OrderBy(kvp => kvp.Key.item.itemName)
            .ThenByDescending(kvp => kvp.Key.price)
            .ToList();

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