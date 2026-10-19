using System;
using UnityEngine;

public class EtalaseManager : MonoBehaviour
{
    [Min(1)] public int slotCount = 3;

    public event Action OnChanged;

    private CraftedItemSO[] slots;

    private CraftedItemSO[] Slots
    {
        get
        {
            if (slots == null) slots = new CraftedItemSO[slotCount];
            return slots;
        }
    }

    public int SlotCount => Slots.Length;

    public CraftedItemSO GetSlot(int index)
    {
        if (index < 0 || index >= Slots.Length) return null;
        return Slots[index];
    }

    public bool HasAnyItem()
    {
        foreach (var s in Slots) if (s != null) return true;
        return false;
    }

    public int GetFirstEmptyIndex()
    {
        for (int i = 0; i < Slots.Length; i++) if (Slots[i] == null) return i;
        return -1;
    }

    public bool TryPlace(CraftedItemSO item, int index)
    {
        if (item == null || index < 0 || index >= Slots.Length) return false;
        if (Slots[index] != null) return false;
        if (!InventoryManager.Instance.ConsumeCraftedItem(item, 1)) return false;

        Slots[index] = item;
        OnChanged?.Invoke();
        return true;
    }

    public bool TryPlaceFirstEmpty(CraftedItemSO item)
    {
        int index = GetFirstEmptyIndex();
        if (index < 0) return false;
        return TryPlace(item, index);
    }

    // Tarik item dari etalase kembali ke inventory
    public bool Remove(int index)
    {
        if (index < 0 || index >= Slots.Length || Slots[index] == null) return false;

        InventoryManager.Instance.AddCraftedItem(Slots[index], 1);
        Slots[index] = null;
        OnChanged?.Invoke();
        return true;
    }

    // Dipanggil saat pelanggan membeli. Item terjual (tidak balik ke inventory).
    public bool TrySell(CraftedItemSO wanted, out int price)
    {
        price = 0;
        if (wanted == null) return false;

        for (int i = 0; i < Slots.Length; i++)
        {
            if (Slots[i] == wanted)
            {
                price = wanted.basePrice;
                Slots[i] = null;
                OnChanged?.Invoke();
                return true;
            }
        }
        return false;
    }

    // Dipanggil saat fase Sell berakhir. Aman dipanggil berulang.
    public void ReturnAllToInventory()
    {
        bool changed = false;
        for (int i = 0; i < Slots.Length; i++)
        {
            if (Slots[i] == null) continue;
            InventoryManager.Instance.AddCraftedItem(Slots[i], 1);
            Slots[i] = null;
            changed = true;
        }
        if (changed) OnChanged?.Invoke();
    }
}