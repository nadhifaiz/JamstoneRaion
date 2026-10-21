using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Mencatat resep yang sudah ditemukan pemain. Data hidup selama sesi permainan dan
// TIDAK di-reset saat ganti hari. Taruh di objek yang selalu aktif (mis. bersama InventoryManager).
// Satu resep = satu CraftedItemSO (kombinasi tag-nya ada di CraftedItemSO itu sendiri).
public class RecipeBookManager : MonoBehaviour
{
    public static RecipeBookManager Instance { get; private set; }

    [SerializeField] private CraftingManager recipeManager; // sumber daftar resep (allRecipes)

    private readonly HashSet<CraftedItemSO> discovered = new HashSet<CraftedItemSO>();

    public IReadOnlyList<CraftedItemSO> AllRecipes => recipeManager.allRecipes;

    public int TotalCount => recipeManager.allRecipes.Count(r => r != null);

    public int DiscoveredCount => recipeManager.allRecipes.Count(r => r != null && discovered.Contains(r));

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (recipeManager == null)
        {
            Debug.LogError("[RecipeBookManager] 'recipeManager' belum di-assign.", this);
        }
    }

    public bool IsDiscovered(CraftedItemSO recipe)
    {
        return recipe != null && discovered.Contains(recipe);
    }

    // Mengembalikan true kalau ini penemuan PERTAMA resep tersebut
    public bool Discover(CraftedItemSO recipe)
    {
        if (recipe == null) return false;

        bool isNew = discovered.Add(recipe);
        if (isNew) GameLog.Log($"[RecipeBook] Resep baru ditemukan: {recipe.itemName}");
        return isNew;
    }
}