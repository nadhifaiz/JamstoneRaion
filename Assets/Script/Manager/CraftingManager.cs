using System.Collections.Generic;
using UnityEngine;

public class CraftingManager : MonoBehaviour
{
    [Header("Database Resep")]
    [Tooltip("Masukkan semua CraftedItemSO yang kamu buat ke sini")]
    public List<CraftedItemSO> allRecipes;

    // Fungsi ini dipanggil saat pemain menekan tombol "Craft"
    // material1 dan material2 didapat dari input slot UI Crafting
    public CraftedItemSO CheckRecipe(MaterialSO material1, MaterialSO material2)
    {
        MaterialType tagA = material1.materialType;
        MaterialType tagB = material2.materialType;

        foreach (CraftedItemSO recipe in allRecipes)
        {
            // Cek kombinasi maju (Tag 1 + Tag 2)
            bool matchForward = (recipe.requiredTag1 == tagA && recipe.requiredTag2 == tagB);

            // Cek kombinasi mundur (Tag 2 + Tag 1)
            bool matchBackward = (recipe.requiredTag1 == tagB && recipe.requiredTag2 == tagA);

            if (matchForward || matchBackward)
            {
                Debug.Log($"Resep ditemukan! Hasil: {recipe.itemName}");
                return recipe; // Kembalikan barang jadinya
            }
        }

        Debug.Log("Resep tidak valid / menghasilkan sampah.");
        return null; // Tidak ada resep yang cocok
    }
}