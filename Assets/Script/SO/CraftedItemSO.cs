using UnityEngine;

[CreateAssetMenu(fileName = "NewCraftedItem", menuName = "Game/Crafted Item Data")]
public class CraftedItemSO : ScriptableObject
{
    [Header("Informasi Barang")]
    public string itemName;
    public Sprite itemSprite;

    [Header("Resep (Tag Kombinasi)")]
    public MaterialType requiredTag1;
    public MaterialType requiredTag2;

    [Header("Ekonomi")]
    public int basePrice;
}