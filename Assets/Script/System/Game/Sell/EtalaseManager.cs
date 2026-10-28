using System;
using UnityEngine;

// Data + logika etalase (tanpa UI). Item yang ditaruh di etalase DIPINDAH dari inventory,
// dikembalikan saat ditarik atau saat fase Sell berakhir, dan hilang kalau terjual.
// Tiap slot menyimpan item + harganya (CraftedItemKey); default(CraftedItemKey) = slot kosong.
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

    // Jumlah slot dasar dari inspector; jumlah sebenarnya bisa lebih banyak karena upgrade
    public int BaseSlotCount => slotCount;

    public int SlotCount => Slots.Length;

    // Dipanggil saat fase Sell dimulai. Etalase seharusnya sudah kosong; kalau tidak, isinya dikembalikan dulu.
    public void SetSlotCount(int count)
    {
        count = Mathf.Max(1, count);
        if (slots != null && slots.Length == count) return;

        ReturnAllToInventory();
        slots = new CraftedItemKey[count];
        OnChanged?.Invoke();
    }

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

    // Dipanggil saat pelanggan membeli. Kalau ada beberapa unit item yang sama,
    // yang terjual adalah slot paling kiri. Harga yang dibayar = harga unit itu.
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