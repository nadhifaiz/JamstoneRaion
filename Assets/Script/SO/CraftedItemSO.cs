using System;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCraftedItem", menuName = "Game/Crafted Item Data")]
public class CraftedItemSO : ScriptableObject
{
    [Header("ID Save")]
    [Tooltip("Dibuat otomatis. JANGAN diubah setelah dipakai: file save mengacu ke ID ini.")]
    [SerializeField] private string id;
    public string Id => id;

    [Header("Informasi Barang")]
    public string itemName;
    public Sprite itemSprite;

    [TextArea(2, 4)]
    [Tooltip("Teks deskripsi yang tampil di panel rincian")]
    public string description;

    [Header("Resep (Tag Kombinasi)")]
    public MaterialType requiredTag1;
    public MaterialType requiredTag2;

    [Header("Ekonomi")]
    [Tooltip("Harga dasar barang (Amber). Harga jual akhir dihitung saat crafting: " +
             "basePrice + harga bahan 1 + harga bahan 2, dan disimpan per unit di inventory.")]
    public int basePrice;

#if UNITY_EDITOR
    void OnValidate()
    {
        if (string.IsNullOrEmpty(id)) AssignNewId();
    }

    public void AssignNewId()
    {
        id = Guid.NewGuid().ToString("N");
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}