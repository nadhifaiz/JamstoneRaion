using System.Collections.Generic;
using UnityEngine;

// Daftar SEMUA material, barang jadi, dan upgrade di game. File save hanya menyimpan ID teks;
// database inilah yang mengubah ID kembali menjadi objek saat load.
// Tiap aset baru harus didaftarkan di sini.
[CreateAssetMenu(fileName = "GameDatabase", menuName = "Game/Game Database")]
public class GameDatabase : ScriptableObject
{
    [SerializeField] private List<MaterialSO> materials = new List<MaterialSO>();
    [SerializeField] private List<CraftedItemSO> craftedItems = new List<CraftedItemSO>();
    [SerializeField] private List<UpgradeSO> upgrades = new List<UpgradeSO>();

    private Dictionary<string, MaterialSO> materialLookup;
    private Dictionary<string, CraftedItemSO> craftedLookup;
    private Dictionary<string, UpgradeSO> upgradeLookup;

    public MaterialSO GetMaterial(string id)
    {
        if (materialLookup == null) BuildLookups();
        return id != null && materialLookup.TryGetValue(id, out MaterialSO so) ? so : null;
    }

    public CraftedItemSO GetCraftedItem(string id)
    {
        if (craftedLookup == null) BuildLookups();
        return id != null && craftedLookup.TryGetValue(id, out CraftedItemSO so) ? so : null;
    }

    public UpgradeSO GetUpgrade(string id)
    {
        if (upgradeLookup == null) BuildLookups();
        return id != null && upgradeLookup.TryGetValue(id, out UpgradeSO so) ? so : null;
    }

    private void BuildLookups()
    {
        materialLookup = new Dictionary<string, MaterialSO>();
        craftedLookup = new Dictionary<string, CraftedItemSO>();
        upgradeLookup = new Dictionary<string, UpgradeSO>();

        foreach (MaterialSO m in materials)
        {
            if (m == null) continue;
            if (string.IsNullOrEmpty(m.Id)) { Debug.LogError($"[GameDatabase] Material '{m.name}' belum punya ID. Jalankan 'Validasi ID' dari menu klik kanan database.", m); continue; }
            if (!materialLookup.TryAdd(m.Id, m)) Debug.LogError($"[GameDatabase] ID material duplikat: '{m.name}'. Jalankan 'Validasi ID'.", m);
        }

        foreach (CraftedItemSO c in craftedItems)
        {
            if (c == null) continue;
            if (string.IsNullOrEmpty(c.Id)) { Debug.LogError($"[GameDatabase] Barang '{c.name}' belum punya ID. Jalankan 'Validasi ID' dari menu klik kanan database.", c); continue; }
            if (!craftedLookup.TryAdd(c.Id, c)) Debug.LogError($"[GameDatabase] ID barang duplikat: '{c.name}'. Jalankan 'Validasi ID'.", c);
        }

        foreach (UpgradeSO u in upgrades)
        {
            if (u == null) continue;
            if (string.IsNullOrEmpty(u.Id)) { Debug.LogError($"[GameDatabase] Upgrade '{u.name}' belum punya ID. Jalankan 'Validasi ID' dari menu klik kanan database.", u); continue; }
            if (!upgradeLookup.TryAdd(u.Id, u)) Debug.LogError($"[GameDatabase] ID upgrade duplikat: '{u.name}'. Jalankan 'Validasi ID'.", u);
        }
    }

    void OnValidate()
    {
        materialLookup = null;
        craftedLookup = null;
        upgradeLookup = null;
    }

#if UNITY_EDITOR
    // Isi ID yang kosong dan perbaiki ID duplikat (mis. akibat Ctrl+D pada aset)
    [ContextMenu("Validasi ID (isi kosong & perbaiki duplikat)")]
    private void ValidateIds()
    {
        var seen = new HashSet<string>();
        int changed = 0;

        foreach (MaterialSO m in materials)
        {
            if (m == null) continue;
            if (string.IsNullOrEmpty(m.Id) || !seen.Add(m.Id)) { m.AssignNewId(); seen.Add(m.Id); changed++; }
        }

        foreach (CraftedItemSO c in craftedItems)
        {
            if (c == null) continue;
            if (string.IsNullOrEmpty(c.Id) || !seen.Add(c.Id)) { c.AssignNewId(); seen.Add(c.Id); changed++; }
        }

        foreach (UpgradeSO u in upgrades)
        {
            if (u == null) continue;
            if (string.IsNullOrEmpty(u.Id) || !seen.Add(u.Id)) { u.AssignNewId(); seen.Add(u.Id); changed++; }
        }

        materialLookup = null;
        craftedLookup = null;
        upgradeLookup = null;
        UnityEditor.AssetDatabase.SaveAssets();
        Debug.Log($"[GameDatabase] Validasi selesai: {changed} ID diperbarui. Material: {materials.Count}, Barang jadi: {craftedItems.Count}, Upgrade: {upgrades.Count}.");
    }
#endif
}