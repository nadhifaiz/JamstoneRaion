using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CraftingUIManager : MonoBehaviour
{
    [Header("References")]
    public CraftingManager recipeManager;

    [Header("Inventory UI")]
    public Transform inventoryGridContent;
    [Tooltip("Prefab MaterialUI yang akan ditampilkan di grid inventory")]
    public GameObject inventoryItemPrefab;

    [Header("Meja Rakit (Blind Crafting)")]
    public CraftingSlot slot1;
    public CraftingSlot slot2;
    public Button craftButton;
    public GameObject blockRaycastPanel;

    [Header("UI Overview (Pop-up Result)")]
    public GameObject overviewPanel;
    public Image overviewItemImage;
    public TextMeshProUGUI overviewTitleText;
    public TextMeshProUGUI overviewDetailText;
    public TextMeshProUGUI overviewPriceText;
    public Sprite failSprite;

    [Header("Animasi Crafting")]
    [Tooltip("Opsional. Kalau kosong, pakai jeda 1,2 detik seperti sebelumnya.")]
    public CraftingAnimationController craftAnimator;

    private MaterialSO mat1;
    private MaterialSO mat2;

    // ==========================================
    // FUNGSI UTAMA
    // ==========================================

    public void ExecuteCraft()
    {
        if (mat1 == null || mat2 == null) return;
        if (!HasEnoughStock(mat1, mat2)) return;

        StartCoroutine(CraftSequenceRoutine(mat1, mat2));
    }

    // Material dioper sebagai parameter supaya tidak berubah selama animasi berjalan
    private IEnumerator CraftSequenceRoutine(MaterialSO usedMat1, MaterialSO usedMat2)
    {
        SetInputBlocked(true);

        // Fase 1: overlay + anvil + palu memalu
        if (craftAnimator != null) yield return craftAnimator.PlayHammerPhase();
        else yield return new WaitForSeconds(1.2f);

        // Cek ulang stok tepat sebelum konsumsi (jangan percaya pengecekan sebelum animasi)
        if (!HasEnoughStock(usedMat1, usedMat2))
        {
            if (craftAnimator != null) craftAnimator.HideImmediate();
            SetInputBlocked(false);
            yield break;
        }

        var inv = InventoryManager.Instance;
        inv.ConsumeMaterial(usedMat1, 1);
        inv.ConsumeMaterial(usedMat2, 1);

        CraftedItemSO resultItem = recipeManager.CheckRecipe(usedMat1, usedMat2);
        int finalPrice = 0;
        bool isNewDiscovery = false;

        if (resultItem != null)
        {
            // Harga melekat pada unit item (CraftedItemKey), bukan pada ScriptableObject
            finalPrice = recipeManager.CalculateFinalPrice(resultItem, usedMat1, usedMat2);
            inv.AddCraftedItem(new CraftedItemKey(resultItem, finalPrice), 1);

            // Craft sukses = resep tercatat permanen di buku resep
            isNewDiscovery = RecipeBookManager.Instance != null && RecipeBookManager.Instance.Discover(resultItem);
        }

        // Craft gagal: upgrade "Bahan Selamat" memberi peluang satu bahan kembali (di frame yang sama
        // dengan konsumsi bahan, jadi tersimpan bersamaan)
        MaterialSO salvaged = resultItem == null ? TrySalvageMaterial(usedMat1, usedMat2) : null;

        // Fase 2: flash + item muncul, lalu overlay hilang
        if (craftAnimator != null)
        {
            bool success = resultItem != null;
            yield return craftAnimator.PlayReveal(
                success ? resultItem.itemSprite : failSprite,
                success ? resultItem.itemName : "Gagal",
                success);
        }

        ShowOverview(resultItem, finalPrice, isNewDiscovery, salvaged);

        ResetMejaRakit();
        RefreshInventoryUI();
        SetInputBlocked(false);
    }

    private MaterialSO TrySalvageMaterial(MaterialSO a, MaterialSO b)
    {
        UpgradeManager upgrades = UpgradeManager.Instance;
        if (upgrades == null) return null;
        if (Random.value >= upgrades.GetSalvageChance()) return null;

        MaterialSO saved = Random.value < 0.5f ? a : b;
        InventoryManager.Instance.AddMaterial(saved, 1);
        return saved;
    }

    private bool HasEnoughStock(MaterialSO a, MaterialSO b)
    {
        var inv = InventoryManager.Instance;

        // Dua slot berisi material yang sama butuh minimal 2 di stok
        if (a == b) return inv.GetMaterialCount(a) >= 2;
        return inv.GetMaterialCount(a) >= 1 && inv.GetMaterialCount(b) >= 1;
    }

    private void SetInputBlocked(bool blocked)
    {
        if (blockRaycastPanel != null) blockRaycastPanel.SetActive(blocked);
        craftButton.interactable = !blocked && mat1 != null && mat2 != null;
    }

    public CraftingSlot GetFirstEmptySlot()
    {
        if (slot1.IsEmpty()) return slot1;
        if (slot2.IsEmpty()) return slot2;
        return null;
    }

    public void UpdateSlotData(CraftingSlot slot, MaterialSO material)
    {
        if (slot == slot1) mat1 = material;
        else if (slot == slot2) mat2 = material;

        craftButton.interactable = (mat1 != null && mat2 != null);
    }

    // ==========================================
    // UI & INTERAKSI
    // ==========================================

    // Method yang bertanggung jawab untuk memperbarui UI daftar material.
    private void RefreshInventoryUI()
    {
        var materials = InventoryManager.Instance.collectedMaterials
            .Where(kvp => kvp.Value > 0)
            .OrderBy(kvp => kvp.Key.itemName);

        UIListHelper.Populate(inventoryGridContent, inventoryItemPrefab, materials, (go, kvp) =>
        {
            go.GetComponent<MaterialUI>().Initialize(kvp.Key, kvp.Value, this);
        });
    }

    private void ShowOverview(CraftedItemSO resultItem, int finalPrice, bool isNewDiscovery, MaterialSO salvaged)
    {
        overviewPanel.SetActive(true);

        if (resultItem != null)
        {
            overviewItemImage.sprite = resultItem.itemSprite;
            overviewTitleText.text = "PEMBUATAN BERHASIL!";
            overviewDetailText.text = $"Kamu berhasil merakit {resultItem.itemName}!";
            if (isNewDiscovery) overviewDetailText.text += "\nResep baru tercatat di buku resep!";
            overviewPriceText.enabled = true; // dinyalakan lagi, karena cabang gagal mematikannya
            overviewPriceText.text = $"Harga Jual: {finalPrice} Amber";
        }
        else
        {
            overviewItemImage.sprite = failSprite;
            overviewTitleText.text = "GAGAL MERAKIT!";
            overviewDetailText.text = "Bahan baku hangus menjadi debu...";
            if (salvaged != null) overviewDetailText.text += $"\n{salvaged.itemName} berhasil diselamatkan!";
            overviewPriceText.enabled = false;
        }
    }

    public void CloseOverview()
    {
        overviewPanel.SetActive(false);
    }

    private void ResetMejaRakit()
    {
        slot1.ClearSlot();
        slot2.ClearSlot();
        mat1 = null;
        mat2 = null;
        craftButton.interactable = false;
    }

    public void FinishCraftingPhase()
    {
        GameManager.Instance.EndCraftPhase();
    }

    public void SetupCraftingUI()
    {
        if (overviewPanel != null) overviewPanel.SetActive(false);
        if (blockRaycastPanel != null) blockRaycastPanel.SetActive(false);

        RefreshInventoryUI();
        ResetMejaRakit();
    }

    public void ForceClearMeja()
    {
        if (!slot1.IsEmpty()) slot1.ClearSlotAndReturnItem();
        if (!slot2.IsEmpty()) slot2.ClearSlotAndReturnItem();
    }
}