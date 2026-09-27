using UnityEngine;

[CreateAssetMenu(fileName = "NewCraftedItem", menuName = "Game/Crafted Item Data")]
public class CraftedItemSO : ScriptableObject
{
    [Header("Informasi Barang")]
    public string itemName; // Contoh: "Tombak / Pedang"
    public Sprite itemSprite;

    [Header("Resep (Tag Kombinasi)")]
    public MaterialType requiredTag1;
    public MaterialType requiredTag2;

    [Header("Ekonomi")]
    [Tooltip("Harga dasar (Base Price) dalam Amber")]
    public int basePrice;

    // Nanti properti untuk sistem Tier bisa ditambahkan di sini
}