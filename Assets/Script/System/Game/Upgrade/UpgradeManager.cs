using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Menyimpan level tiap upgrade, memproses pembelian, dan menjawab "berapa efek upgrade X".
// Taruh di objek yang selalu aktif. Efek dibaca oleh script lain SAAT FASE DIMULAI.
public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    public enum BuyResult { Success, Invalid, MaxLevel, NotEnoughAmber, MissingRequirements }

    [Tooltip("Urutan di sini = urutan kartu di panel Upgrade")]
    [SerializeField] private List<UpgradeSO> upgrades = new List<UpgradeSO>();

    private readonly Dictionary<UpgradeSO, int> levels = new Dictionary<UpgradeSO, int>();

    // Dipicu saat ada pembelian (dipakai SaveManager dan panel UI)
    public event Action OnChanged;

    public IReadOnlyList<UpgradeSO> Upgrades => upgrades;
    public IEnumerable<KeyValuePair<UpgradeSO, int>> AllLevels => levels;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // ==========================================
    // LEVEL & SYARAT
    // ==========================================

    public int GetLevel(UpgradeSO upgrade)
    {
        return upgrade != null && levels.TryGetValue(upgrade, out int level) ? level : 0;
    }

    public bool IsMaxed(UpgradeSO upgrade)
    {
        return GetLevel(upgrade) >= upgrade.MaxLevel;
    }

    // Data level berikutnya, null kalau sudah maksimum
    public UpgradeLevel GetNextLevel(UpgradeSO upgrade)
    {
        return IsMaxed(upgrade) ? null : upgrade.GetLevel(GetLevel(upgrade) + 1);
    }

    public int GetOwnedCount(UpgradeRequirement requirement)
    {
        InventoryManager inv = InventoryManager.Instance;
        if (inv == null) return 0;

        if (requirement.material != null) return inv.GetMaterialCount(requirement.material);

        if (requirement.craftedItem != null)
        {
            return inv.craftedItems.Where(kvp => kvp.Key.item == requirement.craftedItem).Sum(kvp => kvp.Value);
        }

        return 0;
    }

    public bool CanAfford(UpgradeLevel level)
    {
        return CurrencyManager.Instance != null && CurrencyManager.Instance.Amber >= level.amberCost;
    }

    public bool MeetsRequirements(UpgradeLevel level)
    {
        foreach (UpgradeRequirement req in level.requirements)
        {
            if (GetOwnedCount(req) < req.count) return false;
        }
        return true;
    }

    // ==========================================
    // PEMBELIAN
    // ==========================================

    // Semua syarat dicek DULU, baru Amber dan bahan dikonsumsi sekaligus (tidak ada pembelian setengah jalan).
    // Semua perubahan terjadi di frame yang sama, jadi tersimpan sebagai satu penulisan save.
    public BuyResult TryBuy(UpgradeSO upgrade)
    {
        if (upgrade == null || CurrencyManager.Instance == null || InventoryManager.Instance == null) return BuyResult.Invalid;

        UpgradeLevel next = GetNextLevel(upgrade);
        if (next == null) return BuyResult.MaxLevel;
        if (!CanAfford(next)) return BuyResult.NotEnoughAmber;
        if (!MeetsRequirements(next)) return BuyResult.MissingRequirements;

        // Amber dulu: hanya bagian ini yang bisa gagal, sehingga bahan tidak terpotong kalau gagal
        if (next.amberCost > 0 && !CurrencyManager.Instance.TrySpend(next.amberCost)) return BuyResult.NotEnoughAmber;

        foreach (UpgradeRequirement req in next.requirements)
        {
            if (req.material != null) InventoryManager.Instance.ConsumeMaterial(req.material, req.count);
            else if (req.craftedItem != null) ConsumeCraftedItems(req.craftedItem, req.count);
        }

        levels[upgrade] = GetLevel(upgrade) + 1;
        GameLog.Log($"[Upgrade] {upgrade.displayName} naik ke level {levels[upgrade]}");
        OnChanged?.Invoke();
        return BuyResult.Success;
    }

    // Unit termurah dipakai lebih dulu, tidak peduli harga unitnya
    private void ConsumeCraftedItems(CraftedItemSO item, int count)
    {
        InventoryManager inv = InventoryManager.Instance;
        var keys = inv.craftedItems.Keys.Where(k => k.item == item).OrderBy(k => k.price).ToList();

        foreach (CraftedItemKey key in keys)
        {
            if (count <= 0) break;

            int take = Mathf.Min(count, inv.GetCraftedItemCount(key));
            inv.ConsumeCraftedItem(key, take);
            count -= take;
        }
    }

    // ==========================================
    // EFEK (dibaca script lain saat fase dimulai)
    // ==========================================

    public float GetBatteryMultiplier() => 1f + GetEffect(UpgradeType.CraneBattery, 0f);

    public float GetSpeedMultiplier() => 1f + GetEffect(UpgradeType.CraneSpeed, 0f);

    public float GetIntervalMultiplier() => GetEffect(UpgradeType.CustomerInterval, 1f);

    public int GetEtalaseSlotCount(int baseCount) => Mathf.Max(baseCount, Mathf.RoundToInt(GetEffect(UpgradeType.EtalaseSlots, baseCount)));

    public float GetSalvageChance() => Mathf.Clamp01(GetEffect(UpgradeType.SalvageChance, 0f));
    public int GetCustomerCapacity()
    {
        return Mathf.Max(
            1,
            Mathf.RoundToInt(GetEffect(UpgradeType.CustomerCapacity, 1f))
        );
    }

    // Nilai efek di level sekarang; fallback kalau upgrade belum dibeli
    private float GetEffect(UpgradeType type, float fallback)
    {
        foreach (UpgradeSO upgrade in upgrades)
        {
            if (upgrade == null || upgrade.type != type) continue;

            int level = GetLevel(upgrade);
            if (level > 0) return upgrade.GetLevel(level).effectValue;
        }
        return fallback;
    }

    // ==========================================
    // LOAD DARI SAVE
    // ==========================================

    // Mengganti seluruh level secara diam-diam (tanpa OnChanged). Level dijepit ke batas data saat ini,
    // jadi save aman walau jumlah level di data dikurangi.
    public void LoadState(Dictionary<UpgradeSO, int> loaded)
    {
        levels.Clear();
        foreach (var kvp in loaded)
        {
            int level = Mathf.Clamp(kvp.Value, 0, kvp.Key.MaxLevel);
            if (level > 0) levels[kvp.Key] = level;
        }
    }
}