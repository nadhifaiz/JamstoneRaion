using System;

[Serializable]
public struct CraftedItemKey : IEquatable<CraftedItemKey>
{
    public CraftedItemSO item;
    public int price;

    public CraftedItemKey(CraftedItemSO item, int price)
    {
        this.item = item;
        this.price = price;
    }

    public bool IsEmpty => item == null;

    public bool Equals(CraftedItemKey other) => item == other.item && price == other.price;
    public override bool Equals(object obj) => obj is CraftedItemKey other && Equals(other);
    public override int GetHashCode() => unchecked(((item != null ? item.GetHashCode() : 0) * 397) ^ price);
}