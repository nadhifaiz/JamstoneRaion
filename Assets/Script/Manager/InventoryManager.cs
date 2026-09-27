using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("Data Penyimpanan")]
    // 1. Penyimpanan Bahan Baku (Rongsokan)
    public Dictionary<MaterialSO, int> collectedMaterials { get; private set; } = new Dictionary<MaterialSO, int>();

    // 2. Penyimpanan Barang Jadi (Hasil Crafting)
    public Dictionary<CraftedItemSO, int> craftedItems { get; private set; } = new Dictionary<CraftedItemSO, int>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ==========================================
    // BAGIAN 1: LOGIKA BAHAN BAKU (MATERIAL)
    // ==========================================

    public void AddMaterial(MaterialSO material, int amount = 1)
    {
        if (collectedMaterials.ContainsKey(material))
            collectedMaterials[material] += amount;
        else
            collectedMaterials.Add(material, amount);

        Debug.Log($"[Inventory] {material.itemName} (+{amount}). Total: {collectedMaterials[material]}");
    }

    public bool ConsumeMaterial(MaterialSO material, int amount)
    {
        if (collectedMaterials.ContainsKey(material) && collectedMaterials[material] >= amount)
        {
            collectedMaterials[material] -= amount;
            if (collectedMaterials[material] == 0)
                collectedMaterials.Remove(material); // Bersihkan memori jika habis

            return true;
        }
        return false;
    }

    public int GetMaterialCount(MaterialSO material)
    {
        if (collectedMaterials.ContainsKey(material)) return collectedMaterials[material];
        return 0;
    }

    // ==========================================
    // BAGIAN 2: LOGIKA BARANG JADI (CRAFTED ITEM)
    // ==========================================

    public void AddCraftedItem(CraftedItemSO item, int amount = 1)
    {
        if (craftedItems.ContainsKey(item))
            craftedItems[item] += amount;
        else
            craftedItems.Add(item, amount);

        Debug.Log($"[Inventory] Barang Jadi: {item.itemName} (+{amount}). Total: {craftedItems[item]}");
    }

    // Fungsi ini akan dipakai saat fase Selling (barang dijual)
    public bool ConsumeCraftedItem(CraftedItemSO item, int amount)
    {
        if (craftedItems.ContainsKey(item) && craftedItems[item] >= amount)
        {
            craftedItems[item] -= amount;
            if (craftedItems[item] == 0)
                craftedItems.Remove(item);

            return true;
        }
        return false;
    }

    public int GetCraftedItemCount(CraftedItemSO item)
    {
        if (craftedItems.ContainsKey(item)) return craftedItems[item];
        return 0;
    }
}