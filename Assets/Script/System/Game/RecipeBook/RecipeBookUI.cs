using System.Linq;
using TMPro;
using UnityEngine;

public class RecipeBookUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Transform content;
    [SerializeField] private GameObject rowPrefab;
    [SerializeField] private TextMeshProUGUI progressText;

    [Header("Resep Terkunci")]
    [SerializeField] private bool showSilhouetteWhenLocked = true;

    public bool IsOpen => panelRoot.activeSelf;

    void OnDisable()
    {
        // Fase Craft berakhir (panel induk dimatikan): tutup pop-up supaya tidak tertinggal terbuka
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    public void Open()
    {
        panelRoot.SetActive(true);
        Refresh();
    }

    public void Close() => panelRoot.SetActive(false);

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    public void Refresh()
    {
        var book = RecipeBookManager.Instance;
        var recipes = book.AllRecipes.Where(r => r != null).ToList();

        // Nomor urut mengikuti urutan di CraftingManager.allRecipes
        var numbered = recipes.Select((recipe, index) => (recipe, number: index + 1)).ToList();

        UIListHelper.Populate(content, rowPrefab, numbered, (go, item) =>
        {
            go.GetComponent<RecipeRowUI>().Bind(item.number, item.recipe, book.IsDiscovered(item.recipe), showSilhouetteWhenLocked);
        });

        if (progressText != null)
        {
            progressText.text = $"{book.DiscoveredCount}/{recipes.Count} resep ditemukan";
        }
    }
}