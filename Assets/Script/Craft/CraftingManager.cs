using System;
using System.Collections.Generic;
using UnityEngine;

public class CraftingManager : MonoBehaviour
{
    [Serializable]
    public struct TagPrice
    {
        public MaterialType tag;
        public int basePrice;
    }

    [Header("Database Resep")]
    public List<CraftedItemSO> allRecipes;

    [Header("Ekonomi: Harga Dasar per Tag")]
    public List<TagPrice> tagPrices = new List<TagPrice>
    {
        new TagPrice { tag = MaterialType.Beban,    basePrice = 10 },
        new TagPrice { tag = MaterialType.Keras,    basePrice = 12 },
        new TagPrice { tag = MaterialType.Tajam,    basePrice = 15 },
        new TagPrice { tag = MaterialType.Lentur,   basePrice = 8  },
        new TagPrice { tag = MaterialType.Pegangan, basePrice = 5  }
    };

    [Header("Ekonomi: Pengali Tier (Multiplier)")]
    public float tier1Multiplier = 1.0f;
    public float tier2Multiplier = 1.2f;
    public float tier3Multiplier = 1.4f;

    private Dictionary<MaterialType, int> tagPriceLookup;

    void OnValidate()
    {
        tagPriceLookup = null;
    }

    // ==========================================
    // RESEP
    // ==========================================

    // Mengecek apakah kombinasi dua material menghasilkan resep valid
    public CraftedItemSO CheckRecipe(MaterialSO material1, MaterialSO material2)
    {
        MaterialType tagA = material1.materialType;
        MaterialType tagB = material2.materialType;

        foreach (CraftedItemSO recipe in allRecipes)
        {
            bool matchForward = (recipe.requiredTag1 == tagA && recipe.requiredTag2 == tagB);
            bool matchBackward = (recipe.requiredTag1 == tagB && recipe.requiredTag2 == tagA);

            if (matchForward || matchBackward)
            {
                return recipe;
            }
        }
        return null;
    }

    // ==========================================
    // HARGA
    // ==========================================

    public int GetTagBasePrice(MaterialType tag)
    {
        if (tagPriceLookup == null) BuildTagPriceLookup();

        if (tagPriceLookup.TryGetValue(tag, out int price)) return price;

        Debug.LogWarning($"[CraftingManager] Tag {tag} belum punya harga di tabel tagPrices. Dianggap 0.");
        return 0;
    }

    public float GetTierMultiplier(int tier)
    {
        switch (tier)
        {
            case 1: return tier1Multiplier;
            case 2: return tier2Multiplier;
            case 3: return tier3Multiplier;
            default:
                Debug.LogWarning($"[CraftingManager] Tier {tier} tidak dikenal, memakai pengali Tier 1.");
                return tier1Multiplier;
        }
    }

    // Harga satu material.
    public float GetMaterialPrice(MaterialSO mat)
    {
        return GetTagBasePrice(mat.materialType) * GetTierMultiplier(mat.tier);
    }

    // Harga jual akhir dari barang jadi.
    public int CalculateFinalPrice(CraftedItemSO resultItem, MaterialSO mat1, MaterialSO mat2)
    {
        float total = resultItem.basePrice + GetMaterialPrice(mat1) + GetMaterialPrice(mat2);
        return Mathf.RoundToInt(total);
    }

    private void BuildTagPriceLookup()
    {
        tagPriceLookup = new Dictionary<MaterialType, int>();
        foreach (TagPrice entry in tagPrices)
        {
            tagPriceLookup[entry.tag] = entry.basePrice;
        }
    }
}