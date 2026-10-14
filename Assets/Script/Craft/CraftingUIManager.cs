using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CraftingUIManager : MonoBehaviour
{
    [Header("References")]
    public CraftingManager recipeManager;
    public Transform inventoryGridContent;
    public GameObject inventoryItemPrefab;

    [Header("Meja Rakit")]
    public CraftingSlot slot1;
    public CraftingSlot slot2;
    public Image resultPreviewImage;

    [Header("Buttons")]
    public Button craftButton;

    private MaterialSO mat1;
    private MaterialSO mat2;

    // Membaca data InventoryManager dan memunculkan visual kotak di Gudang Kiri
    private void RefreshInventoryUI()
    {
        foreach (Transform child in inventoryGridContent)
        {
            Destroy(child.gameObject);
        }

        foreach (var kvp in InventoryManager.Instance.collectedMaterials)
        {
            MaterialSO material = kvp.Key;
            int count = kvp.Value;

            if (count > 0)
            {
                GameObject newObj = Instantiate(inventoryItemPrefab, inventoryGridContent);
                MaterialUI uiItem = newObj.GetComponent<MaterialUI>();
                uiItem.Initialize(material, count, this);
            }
        }
    }

    // Dipanggil MaterialUI saat diklik: slot kosong pertama (null kalau penuh)
    public CraftingSlot GetFirstEmptySlot()
    {
        if (slot1.IsEmpty()) return slot1;
        if (slot2.IsEmpty()) return slot2;
        return null;
    }

    // Dipanggil oleh CraftingSlot saat diisi atau dikosongkan
    public void UpdateSlotData(CraftingSlot slot, MaterialSO material)
    {
        if (slot == slot1) mat1 = material;
        else if (slot == slot2) mat2 = material;
        else Debug.LogError($"[CraftingUIManager] Slot {slot.name} tidak cocok dengan slot1/slot2. Cek referensi di inspector.", slot);

        CheckLiveRecipe();
    }

    // Mengecek resep secara real-time untuk memunculkan Preview
    private void CheckLiveRecipe()
    {
        if (mat1 != null && mat2 != null)
        {
            craftButton.interactable = true;
            CraftedItemSO result = recipeManager.CheckRecipe(mat1, mat2);

            if (result != null)
            {
                resultPreviewImage.sprite = result.itemSprite;
                resultPreviewImage.color = Color.white;
            }
            else
            {
                resultPreviewImage.sprite = null;
                resultPreviewImage.color = Color.black;
            }
        }
        else
        {
            craftButton.interactable = false;
            resultPreviewImage.sprite = null;
            resultPreviewImage.color = new Color(1, 1, 1, 0);
        }
    }

    public void ExecuteCraft()
    {
        if (mat1 == null || mat2 == null) return;

        var inv = InventoryManager.Instance;

        // Validasi stok SEBELUM mengonsumsi apa pun (tidak perlu rollback)
        if (mat1 == mat2)
        {
            if (inv.GetMaterialCount(mat1) < 2) return;
        }
        else
        {
            if (inv.GetMaterialCount(mat1) < 1 || inv.GetMaterialCount(mat2) < 1) return;
        }

        // Resep dicek dulu sebelum mat1/mat2 dipakai
        CraftedItemSO resultItem = recipeManager.CheckRecipe(mat1, mat2);

        inv.ConsumeMaterial(mat1, 1);
        inv.ConsumeMaterial(mat2, 1);

        if (resultItem != null)
        {
            inv.AddCraftedItem(resultItem, 1);
            Debug.Log($"BERHASIL RAKIT: {resultItem.itemName}");
        }
        else
        {
            Debug.Log("RESEP SALAH! Bahan jadi debu.");
        }

        ResetMejaRakit();
        RefreshInventoryUI();
    }

    private void ResetMejaRakit()
    {
        slot1.ClearSlot();
        slot2.ClearSlot();
    }

    public void FinishCraftingPhase()
    {
        GameManager.Instance.EndCraftPhase();
    }

    public void SetupCraftingUI()
    {
        RefreshInventoryUI();
        ResetMejaRakit();
    }

    public void ForceClearMeja()
    {
        if (!slot1.IsEmpty()) slot1.ClearSlotAndReturnItem();
        if (!slot2.IsEmpty()) slot2.ClearSlotAndReturnItem();
    }
}