using System;
using UnityEngine;

public class EtalaseManager : MonoBehaviour
{
    [Min(1)] public int slotCount = 3;

    public event Action OnChanged;

    private CraftedItemKey[] slots;

    private CraftedItemKey[] Slots
    {
        get
        {
            if (slots == null) slots = new CraftedItemKey[slotCount];
            return slots;
        }
    }

    public int SlotCount => Slots.Length;

    public CraftedItemKey GetSlot(int index)
    {
        if (index < 0 || index >= Slots.Length) return default;
        return Slots[index];
    }

    public bool HasAnyItem()
    {
        foreach (var s in Slots) if (!s.IsEmpty) return true;
        return false;
    }

    public int GetFirstEmptyIndex()
    {
        for (int i = 0; i < Slots.Length; i++) if (Slots[i].IsEmpty) return i;
        return -1;
    }

    public bool TryPlace(CraftedItemKey key, int index)
    {
        if (key.IsEmpty || index < 0 || index >= Slots.Length) return false;
        if (!Slots[index].IsEmpty) return false;
        if (!InventoryManager.Instance.ConsumeCraftedItem(key, 1)) return false;

        Slots[index] = key;
        OnChanged?.Invoke();
        return true;
    }

    public bool TryPlaceFirstEmpty(CraftedItemKey key)
    {
        int index = GetFirstEmptyIndex();
        if (index < 0) return false;
        return TryPlace(key, index);
    }

    // Tarik item dari etalase kembali ke inventory
    public bool Remove(int index)
    {
        if (index < 0 || index >= Slots.Length || Slots[index].IsEmpty) return false;

        InventoryManager.Instance.AddCraftedItem(Slots[index], 1);
        Slots[index] = default;
        OnChanged?.Invoke();
        return true;
    }

    public bool TrySell(CraftedItemSO wanted, out int price)
    {
        price = 0;
        if (wanted == null) return false;

        for (int i = 0; i < Slots.Length; i++)
        {
            if (!Slots[i].IsEmpty && Slots[i].item == wanted)
            {
                price = Slots[i].price;
                Slots[i] = default;
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
            if (Slots[i].IsEmpty) continue;
            InventoryManager.Instance.AddCraftedItem(Slots[i], 1);
            Slots[i] = default;
            changed = true;
        }
        if (changed) OnChanged?.Invoke();
    }
}