using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public enum InventoryTab
{
    Materials,
    CraftedItems
}

public class InventoryPanelUI : MonoBehaviour
{
    [Header("References")]
    public GameObject panelRoot;
    public Transform gridContent;
    public GameObject itemPrefab;
    public GameObject emptyLabel;

    [Header("Tabs")]
    public Button tabMaterialsButton;
    public Button tabCraftedButton;
    public Color selectedTabColor = Color.white;
    public Color normalTabColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    public InventoryTab defaultTab = InventoryTab.Materials;

    private InventoryTab currentTab;

    public bool IsOpen => panelRoot.activeSelf;

    void Awake()
    {
        currentTab = defaultTab;
        tabMaterialsButton.onClick.AddListener(() => ShowTab(InventoryTab.Materials));
        tabCraftedButton.onClick.AddListener(() => ShowTab(InventoryTab.CraftedItems));
        Close();
    }

    public void Open()
    {
        panelRoot.SetActive(true);
        ShowTab(currentTab);
    }

    public void Close() => panelRoot.SetActive(false);

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    public void ShowTab(InventoryTab tab)
    {
        currentTab = tab;
        SetTabVisual(tabMaterialsButton, tab == InventoryTab.Materials);
        SetTabVisual(tabCraftedButton, tab == InventoryTab.CraftedItems);
        Refresh();
    }

    public void Refresh()
    {
        var inv = InventoryManager.Instance;
        int entryCount = 0;

        switch (currentTab)
        {
            case InventoryTab.Materials:
                var mats = inv.collectedMaterials
                    .Where(kvp => kvp.Value > 0)
                    .OrderBy(kvp => kvp.Key.tier)
                    .ThenBy(kvp => kvp.Key.itemName)
                    .ToList();

                UIListHelper.Populate(gridContent, itemPrefab, mats, (go, kvp) =>
                {
                    go.GetComponent<InventoryItemUI>().InitializeMaterial(kvp.Key, kvp.Value);
                });
                entryCount = mats.Count;
                break;

            case InventoryTab.CraftedItems:
                // Item sama dengan harga berbeda tampil sebagai tumpukan terpisah
                var crafted = inv.craftedItems
                    .Where(kvp => kvp.Value > 0)
                    .OrderBy(kvp => kvp.Key.item.itemName)
                    .ThenByDescending(kvp => kvp.Key.price)
                    .ToList();

                UIListHelper.Populate(gridContent, itemPrefab, crafted, (go, kvp) =>
                {
                    go.GetComponent<InventoryItemUI>().InitializeCrafted(kvp.Key, kvp.Value);
                });
                entryCount = crafted.Count;
                break;
        }

        if (emptyLabel != null) emptyLabel.SetActive(entryCount == 0);
    }

    private void SetTabVisual(Button tab, bool selected)
    {
        if (tab.targetGraphic != null)
            tab.targetGraphic.color = selected ? selectedTabColor : normalTabColor;
    }
}