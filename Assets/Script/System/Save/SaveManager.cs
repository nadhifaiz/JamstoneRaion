using System.Collections.Generic;
using UnityEngine;

// Live save: setiap perubahan data penting menandai save sebagai "kotor", lalu ditulis di akhir frame itu.
// Beberapa perubahan di frame yang sama (mis. craft: bahan berkurang + barang bertambah) digabung
// jadi satu penulisan, sehingga save tidak pernah berisi "setengah craft".
//
// Slot yang dipakai berasal dari SaveSession (dipilih di menu utama). Kalau scene game dijalankan
// langsung dari Editor tanpa menu, memakai slot 'debug'.
//
// Taruh di objek yang SELALU aktif. Dipanggil dari GameManager:
//   Start()          -> SaveManager.Instance.LoadGame()   (sebelum UpdateDayUI/ChangeState(Hub))
//   akhir ChangeState -> SaveManager.Instance.MarkDirty()
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private GameDatabase database;
    [Tooltip("Isi etalase dihitung sebagai bagian inventory saat menyimpan")]
    [SerializeField] private EtalaseManager etalase;

    [Header("Slot")]
    [Tooltip("Dipakai hanya kalau tidak ada slot terpilih dari menu utama (mis. Play langsung dari Editor)")]
    [SerializeField] private string debugSlotId = "debug";

    private string slotId;
    private SaveSlotMeta currentMeta;

    private bool dirty;
    private bool isLoading;
    private bool isReady;       // true setelah LoadGame selesai
    private bool subscribed;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        Subscribe(); // jaga-jaga kalau LoadGame belum dipanggil
    }

    void OnDestroy()
    {
        Unsubscribe();
    }

    void LateUpdate()
    {
        if (!dirty) return;

        dirty = false;
        SaveNow();
    }

    // ==========================================
    // LOAD
    // ==========================================

    // Mengembalikan true kalau ada save yang dimuat, false kalau ini game baru
    public bool LoadGame()
    {
        if (database == null)
        {
            Debug.LogError("[SaveManager] 'database' belum di-assign. Save/load dimatikan.", this);
            return false;
        }

        Subscribe();

        slotId = SaveSession.HasSlot ? SaveSession.CurrentSlotId : debugSlotId;

        if (!SaveStorage.TryReadMeta(slotId, out currentMeta))
        {
            long now = System.DateTime.UtcNow.Ticks;
            currentMeta = new SaveSlotMeta
            {
                id = slotId,
                slotName = SaveSession.HasSlot ? "Game Baru" : "Debug",
                createdUtcTicks = now,
                lastPlayedUtcTicks = now
            };
        }

        bool loaded = SaveStorage.TryRead(slotId, out SaveData data);
        if (loaded) ApplyLoadedData(data);

        isReady = true;
        return loaded;
    }

    private void ApplyLoadedData(SaveData data)
    {
        isLoading = true;

        GameManager gm = GameManager.Instance;
        if (gm != null)
        {
            gm.currentDay = Mathf.Max(1, data.currentDay);
            gm.hasCraftedToday = data.hasCraftedToday;
            gm.hasSoldToday = data.hasSoldToday;

            // Game tertutup di tengah Collect = fase itu dianggap selesai. Bahan yang sudah masuk
            // tetap milik pemain, tapi Collect hari itu tidak bisa diulang (mencegah farming lewat keluar-masuk).
            gm.hasCollectedToday = data.hasCollectedToday || data.collectInProgress;
        }

        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.SetAmberSilently(data.amber);
        }

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.LoadState(ReadMaterials(data), ReadCraftedItems(data));
        }

        if (RecipeBookManager.Instance != null)
        {
            RecipeBookManager.Instance.LoadDiscovered(ReadRecipes(data));
        }

        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.LoadState(ReadUpgrades(data));
        }

        isLoading = false;
        GameLog.Log($"[Save] Slot '{slotId}' dimuat. Hari {data.currentDay}, Amber {data.amber}.");
    }

    private Dictionary<MaterialSO, int> ReadMaterials(SaveData data)
    {
        var result = new Dictionary<MaterialSO, int>();
        foreach (MaterialEntry entry in data.materials)
        {
            MaterialSO so = database.GetMaterial(entry.id);
            if (so == null) { Debug.LogWarning($"[Save] Material dengan ID '{entry.id}' tidak ada di GameDatabase, dilewati."); continue; }
            if (entry.count > 0) result[so] = entry.count;
        }
        return result;
    }

    private Dictionary<CraftedItemKey, int> ReadCraftedItems(SaveData data)
    {
        var result = new Dictionary<CraftedItemKey, int>();
        foreach (CraftedEntry entry in data.craftedItems)
        {
            CraftedItemSO so = database.GetCraftedItem(entry.id);
            if (so == null) { Debug.LogWarning($"[Save] Barang dengan ID '{entry.id}' tidak ada di GameDatabase, dilewati."); continue; }
            if (entry.count > 0) result[new CraftedItemKey(so, entry.price)] = entry.count;
        }
        return result;
    }

    private Dictionary<UpgradeSO, int> ReadUpgrades(SaveData data)
    {
        var result = new Dictionary<UpgradeSO, int>();
        foreach (UpgradeEntry entry in data.upgrades)
        {
            UpgradeSO so = database.GetUpgrade(entry.id);
            if (so == null) { Debug.LogWarning($"[Save] Upgrade dengan ID '{entry.id}' tidak ada di GameDatabase, dilewati."); continue; }
            if (entry.level > 0) result[so] = entry.level;
        }
        return result;
    }

    private List<CraftedItemSO> ReadRecipes(SaveData data)
    {
        var result = new List<CraftedItemSO>();
        foreach (string id in data.discoveredRecipes)
        {
            CraftedItemSO so = database.GetCraftedItem(id);
            if (so == null) { Debug.LogWarning($"[Save] Resep dengan ID '{id}' tidak ada di GameDatabase, dilewati."); continue; }
            result.Add(so);
        }
        return result;
    }

    // ==========================================
    // SAVE
    // ==========================================

    // Dipanggil tiap ada perubahan data. Penulisan terjadi di akhir frame (LateUpdate).
    public void MarkDirty()
    {
        if (!isReady || isLoading) return;
        dirty = true;
    }

    private void SaveNow()
    {
        SaveData data = BuildSaveData();
        SaveStorage.Write(slotId, data);

        UpdateMeta(data);
        SaveStorage.WriteMeta(currentMeta);
    }

    private void UpdateMeta(SaveData data)
    {
        GameManager gm = GameManager.Instance;

        currentMeta.currentDay = data.currentDay;
        currentMeta.amber = data.amber;
        currentMeta.lastPlayedUtcTicks = System.DateTime.UtcNow.Ticks;

        // Tamat = hari terakhir tercapai dan ketiga fase hari itu selesai (dihitung, bukan disimpan,
        // sehingga save versi lite (7 hari) bisa lanjut kalau maxDays dinaikkan nanti)
        currentMeta.isCompleted = gm != null
            && data.currentDay >= gm.maxDays
            && data.hasCollectedToday && data.hasCraftedToday && data.hasSoldToday;
    }

    private SaveData BuildSaveData()
    {
        var data = new SaveData();

        GameManager gm = GameManager.Instance;
        if (gm != null)
        {
            data.currentDay = gm.currentDay;
            data.hasCollectedToday = gm.hasCollectedToday;
            data.hasCraftedToday = gm.hasCraftedToday;
            data.hasSoldToday = gm.hasSoldToday;
            data.collectInProgress = gm.currentState == GameState.Collect;
        }

        if (CurrencyManager.Instance != null)
        {
            data.amber = CurrencyManager.Instance.Amber;
        }

        InventoryManager inv = InventoryManager.Instance;
        if (inv != null)
        {
            foreach (var kvp in inv.collectedMaterials)
            {
                if (kvp.Key == null || kvp.Value <= 0) continue;
                if (database.GetMaterial(kvp.Key.Id) != kvp.Key)
                {
                    Debug.LogWarning($"[Save] Material '{kvp.Key.name}' belum terdaftar (atau ID bentrok) di GameDatabase.", kvp.Key);
                }
                data.materials.Add(new MaterialEntry { id = kvp.Key.Id, count = kvp.Value });
            }

            // Barang di etalase sudah keluar dari inventory; dihitung kembali supaya tidak hilang
            var crafted = new Dictionary<CraftedItemKey, int>(inv.craftedItems);
            if (etalase != null)
            {
                for (int i = 0; i < etalase.SlotCount; i++)
                {
                    CraftedItemKey key = etalase.GetSlot(i);
                    if (key.IsEmpty) continue;

                    crafted.TryGetValue(key, out int existing);
                    crafted[key] = existing + 1;
                }
            }

            foreach (var kvp in crafted)
            {
                if (kvp.Key.IsEmpty || kvp.Value <= 0) continue;
                if (database.GetCraftedItem(kvp.Key.item.Id) != kvp.Key.item)
                {
                    Debug.LogWarning($"[Save] Barang '{kvp.Key.item.name}' belum terdaftar (atau ID bentrok) di GameDatabase.", kvp.Key.item);
                }
                data.craftedItems.Add(new CraftedEntry { id = kvp.Key.item.Id, price = kvp.Key.price, count = kvp.Value });
            }
        }

        if (RecipeBookManager.Instance != null)
        {
            foreach (CraftedItemSO recipe in RecipeBookManager.Instance.DiscoveredRecipes)
            {
                data.discoveredRecipes.Add(recipe.Id);
            }
        }

        if (UpgradeManager.Instance != null)
        {
            foreach (var kvp in UpgradeManager.Instance.AllLevels)
            {
                if (kvp.Key == null || kvp.Value <= 0) continue;
                if (database.GetUpgrade(kvp.Key.Id) != kvp.Key)
                {
                    Debug.LogWarning($"[Save] Upgrade '{kvp.Key.name}' belum terdaftar (atau ID bentrok) di GameDatabase.", kvp.Key);
                }
                data.upgrades.Add(new UpgradeEntry { id = kvp.Key.Id, level = kvp.Value });
            }
        }

        return data;
    }

    // ==========================================
    // BERLANGGANAN KE PERUBAHAN DATA
    // ==========================================

    private void Subscribe()
    {
        if (subscribed) return;
        subscribed = true;

        if (InventoryManager.Instance != null) InventoryManager.Instance.OnChanged += MarkDirty;
        if (CurrencyManager.Instance != null) CurrencyManager.Instance.OnAmberChanged += OnAmberChanged;
        if (RecipeBookManager.Instance != null) RecipeBookManager.Instance.OnChanged += MarkDirty;
        if (UpgradeManager.Instance != null) UpgradeManager.Instance.OnChanged += MarkDirty;
        if (etalase != null) etalase.OnChanged += MarkDirty;
    }

    private void Unsubscribe()
    {
        if (!subscribed) return;
        subscribed = false;

        if (InventoryManager.Instance != null) InventoryManager.Instance.OnChanged -= MarkDirty;
        if (CurrencyManager.Instance != null) CurrencyManager.Instance.OnAmberChanged -= OnAmberChanged;
        if (RecipeBookManager.Instance != null) RecipeBookManager.Instance.OnChanged -= MarkDirty;
        if (UpgradeManager.Instance != null) UpgradeManager.Instance.OnChanged -= MarkDirty;
        if (etalase != null) etalase.OnChanged -= MarkDirty;
    }

    private void OnAmberChanged(int _) => MarkDirty();
}