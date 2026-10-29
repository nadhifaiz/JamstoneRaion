using System.Collections;
using TMPro;
using UnityEngine;

// Taruh di objek yang SELALU aktif (di luar sellUIPanel), mis. "SellSystem".
public class SellManager : MonoBehaviour
{
    [Header("References")]
    public EtalaseManager etalase;
    public EtalaseUIManager etalaseUI;
    public CraftingManager recipeManager;      // sumber daftar item yang bisa diinginkan pelanggan (allRecipes)

    [Header("Fase")]
    public float phaseDuration = 60f;
    [Tooltip("True: timer & pelanggan baru mulai setelah panel Etalase pertama kali ditutup")]
    public bool startTimerOnFirstClose = true;

    [Header("Pelanggan")]
    public CustomerController customerPrefab;
    public Transform spawnPoint;
    public Transform centerPoint;
    public Transform exitPoint;                // kosong = keluar lewat spawnPoint
    [Tooltip("Jeda setelah pelanggan sebelumnya pergi (nanti bisa di-upgrade)")]
    public float spawnIntervalMin = 3f;
    public float spawnIntervalMax = 6f;
    [Tooltip("Jeda terendah setelah upgrade interval diterapkan")]
    public float minIntervalFloor = 1f;
    public float waitAtCenter = 1.5f;

    [Header("UI")]
    public TextMeshProUGUI timerText;

    private bool isActive;
    private bool isSelling;
    private bool timerEnded;
    private float timeRemaining;
    private float spawnCountdown;
    private float intervalMultiplier = 1f;
    private CustomerController activeCustomer;

    // ---------- Dipanggil GameManager / UI ----------

    public void StartSellPhase()
    {
        isActive = true; // di-set dulu supaya tombol Skip tetap berfungsi kalau setup belum lengkap
        if (!ValidateReferences()) return;

        // Upgrade dibaca saat fase dimulai
        UpgradeManager upgrades = UpgradeManager.Instance;
        intervalMultiplier = upgrades != null ? upgrades.GetIntervalMultiplier() : 1f;
        etalase.SetSlotCount(upgrades != null ? upgrades.GetEtalaseSlotCount(etalase.BaseSlotCount) : etalase.BaseSlotCount);

        timerEnded = false;
        isSelling = !startTimerOnFirstClose;
        timeRemaining = phaseDuration;
        ResetSpawnCountdown();
        UpdateTimerText();

        etalaseUI.Open(); // pemain diminta mengisi etalase di awal fase
    }

    // Memberi tahu persis referensi mana yang belum diisi di inspector
    private bool ValidateReferences()
    {
        bool ok = true;

        if (etalase == null) { Debug.LogError("[SellManager] 'etalase' belum di-assign.", this); ok = false; }
        if (etalaseUI == null) { Debug.LogError("[SellManager] 'etalaseUI' belum di-assign.", this); ok = false; }
        if (recipeManager == null) { Debug.LogError("[SellManager] 'recipeManager' belum di-assign.", this); ok = false; }
        if (customerPrefab == null) { Debug.LogError("[SellManager] 'customerPrefab' belum di-assign.", this); ok = false; }
        if (spawnPoint == null) { Debug.LogError("[SellManager] 'spawnPoint' belum di-assign.", this); ok = false; }
        if (centerPoint == null) { Debug.LogError("[SellManager] 'centerPoint' belum di-assign.", this); ok = false; }

        return ok;
    }

    public void NotifyEtalaseClosed()
    {
        if (isActive && !isSelling) isSelling = true;
    }

    // Tombol Skip/Selesai: berhenti langsung (pelanggan yang sedang ada ikut dibersihkan)
    public void SkipPhase()
    {
        if (!isActive) return;
        isActive = false;
        GameManager.Instance.EndSellPhase();
    }

    // Dipanggil GameManager.ClearSellPhaseObjects() tiap ganti state. Aman dipanggil berulang.
    public void CleanupPhase()
    {
        isActive = false;
        isSelling = false;
        StopAllCoroutines();

        if (activeCustomer != null) Destroy(activeCustomer.gameObject);
        activeCustomer = null;

        if (etalase != null) etalase.ReturnAllToInventory();
    }

    // ---------- Loop fase ----------

    void Update()
    {
        if (!isActive || !isSelling) return;

        if (!timerEnded)
        {
            timeRemaining -= Time.deltaTime;
            if (timeRemaining <= 0f)
            {
                timeRemaining = 0f;
                timerEnded = true; // tidak ada pelanggan baru; tunggu yang sedang ada selesai
            }
            UpdateTimerText();
        }

        if (activeCustomer != null) return;

        if (timerEnded)
        {
            FinishPhase();
            return;
        }

        if (etalase.HasAnyItem())
        {
            spawnCountdown -= Time.deltaTime;
            if (spawnCountdown <= 0f)
            {
                SpawnCustomer();
                ResetSpawnCountdown();
            }
        }
    }

    private void FinishPhase()
    {
        isActive = false;
        GameManager.Instance.EndSellPhase();
    }

    private void ResetSpawnCountdown()
    {
        float min = Mathf.Max(minIntervalFloor, spawnIntervalMin * intervalMultiplier);
        float max = Mathf.Max(min, spawnIntervalMax * intervalMultiplier);
        spawnCountdown = Random.Range(min, max);
    }

    private void SpawnCustomer()
    {
        var recipes = recipeManager != null ? recipeManager.allRecipes : null;
        if (customerPrefab == null || spawnPoint == null || centerPoint == null || recipes == null || recipes.Count == 0)
        {
            Debug.LogError("[SellManager] customerPrefab/spawnPoint/centerPoint/recipeManager belum lengkap.");
            return;
        }

        CraftedItemSO wanted = recipes[Random.Range(0, recipes.Count)];
        activeCustomer = Instantiate(customerPrefab, spawnPoint.position, Quaternion.identity);
        StartCoroutine(CustomerRoutine(activeCustomer, wanted));
    }

    private IEnumerator CustomerRoutine(CustomerController customer, CraftedItemSO wanted)
    {
        yield return customer.MoveTo(centerPoint.position);
        yield return new WaitForSeconds(waitAtCenter);

        // Dicek saat sampai (pemain masih bisa menarik/menaruh item selama pelanggan jalan)
        if (etalase.TrySell(wanted, out int price))
        {
            // Kejadian harga season: harga unit dikali pengali saat DIJUAL (stok lama ikut naik)
            if (SeasonManager.Instance != null) price = SeasonManager.Instance.ApplyPrice(wanted, price);

            CurrencyManager.Instance.Add(price);
            GameLog.Log($"[Sell] Pelanggan membeli {wanted.itemName} seharga {price}");
            customer.NotifyBought();
        }
        else
        {
            GameLog.Log($"[Sell] Pelanggan mencari {wanted.itemName}, tidak ada. Pergi.");
            customer.NotifyLeftEmpty();
        }

        yield return new WaitForSeconds(0.5f);
        yield return customer.MoveTo((exitPoint != null ? exitPoint : spawnPoint).position);

        Destroy(customer.gameObject);
        activeCustomer = null;
    }

    private void UpdateTimerText()
    {
        if (timerText == null) return;
        int s = Mathf.CeilToInt(timeRemaining);
        timerText.text = $"{s / 60:00}:{s % 60:00}";
    }
}