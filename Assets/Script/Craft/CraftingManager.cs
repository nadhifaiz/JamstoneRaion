using System.Collections.Generic;
using UnityEngine;

public class CraftingManager : MonoBehaviour
{
    [Header("Database Resep")]
    [Tooltip("Masukkan semua CraftedItemSO yang kamu buat ke sini")]
    public List<CraftedItemSO> allRecipes;

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
                Debug.Log($"Resep ditemukan! Hasil: {recipe.itemName}");
                return recipe;
            }
        }

        Debug.Log("Resep tidak valid / menghasilkan sampah.");
        return null;
    }
}