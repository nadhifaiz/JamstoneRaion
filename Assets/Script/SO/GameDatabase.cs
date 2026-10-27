using System.Collections.Generic;
using UnityEngine;

// Daftar SEMUA material dan barang jadi di game. File save hanya menyimpan ID teks;
// database inilah yang mengubah ID kembali menjadi objek saat load.
// Tiap MaterialSO / CraftedItemSO baru harus didaftarkan di sini.
[CreateAssetMenu(fileName = "GameDatabase", menuName = "Game/Game Database")]
public class GameDatabase : ScriptableObject
{
    [SerializeField] private List<MaterialSO> materials = new List<MaterialSO>();
    [SerializeField] private List<CraftedItemSO> craftedItems = new List<CraftedItemSO>();

    private Dictionary<string, MaterialSO> materialLookup;
    private Dictionary<string, CraftedItemSO> craftedLookup;

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

    private void BuildLookups()
    {
        materialLookup = new Dictionary<string, MaterialSO>();
        craftedLookup = new Dictionary<string, CraftedItemSO>();

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
    }

    void OnValidate()
    {
        materialLookup = null;
        craftedLookup = null;
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

        materialLookup = null;
        craftedLookup = null;
        UnityEditor.AssetDatabase.SaveAssets();
        Debug.Log($"[GameDatabase] Validasi selesai: {changed} ID diperbarui. Material: {materials.Count}, Barang jadi: {craftedItems.Count}.");
    }
#endif
}