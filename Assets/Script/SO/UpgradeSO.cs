using System;
using System.Collections.Generic;
using UnityEngine;

public enum UpgradeType
{
    CraneBattery,       // kapasitas baterai crane
    CraneSpeed,         // kecepatan gerak crane
    CustomerInterval,   // jeda antar pelanggan
    EtalaseSlots,       // jumlah slot etalase
    SalvageChance       // peluang satu bahan kembali saat craft gagal
}

// Satu syarat bahan/item untuk sebuah level. Isi SALAH SATU: material atau barang jadi.
[Serializable]
public class UpgradeRequirement
{
    public MaterialSO material;
    public CraftedItemSO craftedItem;
    [Min(1)] public int count = 1;

    public string DisplayName => material != null ? material.itemName : (craftedItem != null ? craftedItem.itemName : "?");
    public Sprite Icon => material != null ? material.itemSprite : (craftedItem != null ? craftedItem.itemSprite : null);
}

[Serializable]
public class UpgradeLevel
{
    [Min(0)] public int amberCost;
    public List<UpgradeRequirement> requirements = new List<UpgradeRequirement>();

    [Tooltip("Arti tergantung tipe upgrade:\n" +
             "CraneBattery: tambahan kapasitas (0.2 = +20%)\n" +
             "CraneSpeed: tambahan kecepatan (0.1 = +10%)\n" +
             "CustomerInterval: pengali jeda (0.85 = 15% lebih cepat)\n" +
             "EtalaseSlots: jumlah slot TOTAL (4, 5, 6)\n" +
             "SalvageChance: peluang 0-1 (0.15 = 15%)")]
    public float effectValue;

    [Tooltip("Teks efek di kartu UI, mis. \"+20% kapasitas baterai\"")]
    public string effectLabel;
}

[CreateAssetMenu(fileName = "NewUpgrade", menuName = "Game/Upgrade Data")]
public class UpgradeSO : ScriptableObject
{
    [Header("ID Save")]
    [Tooltip("Dibuat otomatis. JANGAN diubah setelah dipakai: file save mengacu ke ID ini.")]
    [SerializeField] private string id;
    public string Id => id;

    [Header("Informasi")]
    public string displayName;
    public Sprite icon;
    [TextArea(2, 4)] public string description;
    public UpgradeType type;

    [Header("Level (urut dari level 1)")]
    public List<UpgradeLevel> levels = new List<UpgradeLevel>();

    public int MaxLevel => levels.Count;

    // level: 1-based. Mengembalikan null kalau di luar jangkauan.
    public UpgradeLevel GetLevel(int level)
    {
        return level >= 1 && level <= levels.Count ? levels[level - 1] : null;
    }

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