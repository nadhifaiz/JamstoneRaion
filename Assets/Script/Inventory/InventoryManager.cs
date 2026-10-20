using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("Data Penyimpanan")]
    // 1. Penyimpanan Bahan Baku (Rongsokan)
    public Dictionary<MaterialSO, int> collectedMaterials { get; private set; } = new Dictionary<MaterialSO, int>();

    // 2. Penyimpanan Barang Jadi (Hasil Crafting). Kunci = item + harga jual, jadi harga tersimpan per unit.
    public Dictionary<CraftedItemKey, int> craftedItems { get; private set; } = new Dictionary<CraftedItemKey, int>();

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
                collectedMaterials.Remove(material);

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

    public void AddCraftedItem(CraftedItemKey key, int amount = 1)
    {
        if (key.IsEmpty) return;

        if (craftedItems.ContainsKey(key))
            craftedItems[key] += amount;
        else
            craftedItems.Add(key, amount);

        Debug.Log($"[Inventory] Barang Jadi: {key.item.itemName} @{key.price} (+{amount}). Total: {craftedItems[key]}");
    }

    // Dipakai saat barang ditaruh di etalase
    public bool ConsumeCraftedItem(CraftedItemKey key, int amount)
    {
        if (key.IsEmpty) return false;

        if (craftedItems.ContainsKey(key) && craftedItems[key] >= amount)
        {
            craftedItems[key] -= amount;
            if (craftedItems[key] == 0)
                craftedItems.Remove(key);

            return true;
        }
        return false;
    }

    public int GetCraftedItemCount(CraftedItemKey key)
    {
        if (craftedItems.ContainsKey(key)) return craftedItems[key];
        return 0;
    }
}