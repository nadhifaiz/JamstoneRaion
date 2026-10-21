using UnityEngine;

[CreateAssetMenu(fileName = "NewCraftedItem", menuName = "Game/Crafted Item Data")]
public class CraftedItemSO : ScriptableObject
{
    [Header("Informasi Barang")]
    public string itemName;
    public Sprite itemSprite;

    [TextArea(2, 4)]
    public string description;

    [Header("Resep (Tag Kombinasi)")]
    public MaterialType requiredTag1;
    public MaterialType requiredTag2;

    [Header("Ekonomi")]
    public int basePrice;
}