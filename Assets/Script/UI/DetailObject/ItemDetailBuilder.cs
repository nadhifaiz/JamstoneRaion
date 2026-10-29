using UnityEngine;

// Mengubah sumber data sebuah ItemDetailTrigger (MaterialSO / CraftedItemKey) menjadi ItemDetailData.
public static class ItemDetailBuilder
{
    public static ItemDetailData Build(ItemDetailTrigger trigger, CraftingManager recipeManager, TagVisualsSO tagVisuals)
    {
        switch (trigger.Kind)
        {
            case ItemDetailTrigger.SourceKind.Material:
                return BuildMaterial(trigger.Material, trigger.ShowStock, recipeManager, tagVisuals);

            case ItemDetailTrigger.SourceKind.CraftedItem:
                return BuildCraftedItem(trigger.CraftedItem, trigger.ShowStock, tagVisuals);

            default:
                return null;
        }
    }

    private static ItemDetailData BuildMaterial(MaterialSO mat, bool showStock, CraftingManager recipeManager, TagVisualsSO tagVisuals)
    {
        var data = new ItemDetailData
        {
            icon = mat.itemSprite,
            title = mat.itemName,
            typeLabel = "Bahan Baku",
            description = mat.description
        };

        data.tags.Add(mat.materialType);
        data.infoLines.Add($"Tag: {tagVisuals.GetName(mat.materialType)}");
        data.infoLines.Add($"Tier: {mat.tier}");

        if (recipeManager != null)
        {
            int value = Mathf.RoundToInt(recipeManager.GetMaterialPrice(mat));
            data.infoLines.Add($"Nilai Bahan: {value} Amber");
        }

        if (showStock && InventoryManager.Instance != null)
        {
            data.infoLines.Add($"Dimiliki: {InventoryManager.Instance.GetMaterialCount(mat)}");
        }

        return data;
    }

    private static ItemDetailData BuildCraftedItem(CraftedItemKey key, bool showStock, TagVisualsSO tagVisuals)
    {
        CraftedItemSO item = key.item;

        var data = new ItemDetailData
        {
            icon = item.itemSprite,
            title = item.itemName,
            typeLabel = "Barang Jadi",
            description = item.description
        };

        data.tags.Add(item.requiredTag1);
        data.tags.Add(item.requiredTag2);

        data.infoLines.Add($"Harga Jual: {key.price} Amber");

        // Kalau season sekarang punya kejadian harga untuk barang ini, tampilkan harga pasarnya
        if (SeasonManager.Instance != null)
        {
            float multiplier = SeasonManager.Instance.GetPriceMultiplier(item);
            if (!Mathf.Approximately(multiplier, 1f))
            {
                int marketPrice = Mathf.RoundToInt(key.price * multiplier);
                int percent = Mathf.RoundToInt(Mathf.Abs(multiplier - 1f) * 100f);
                data.infoLines.Add($"Harga Pasar Sekarang: {marketPrice} Amber ({(multiplier > 1f ? "naik" : "turun")} {percent}%)");
            }
        }
        data.infoLines.Add($"Dibuat dari: {tagVisuals.GetName(item.requiredTag1)} + {tagVisuals.GetName(item.requiredTag2)}");

        if (showStock && InventoryManager.Instance != null)
        {
            data.infoLines.Add($"Dimiliki: {InventoryManager.Instance.GetCraftedItemCount(key)}");
        }

        return data;
    }
}