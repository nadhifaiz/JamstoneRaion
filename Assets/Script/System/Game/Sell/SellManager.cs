using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Taruh di objek yang SELALU aktif (di luar sellUIPanel).
public class SellManager : MonoBehaviour
{
    [Header("References")]
    public EtalaseManager etalase;
    public EtalaseUIManager etalaseUI;
    public CraftingManager recipeManager;

    [Header("Fase")]
    public float phaseDuration = 60f;
    public bool startTimerOnFirstClose = true;

    [Header("Pelanggan")]
    public CustomerController customerPrefab;
    public Transform spawnPoint;
    public Transform centerPoint;
    public Transform[] waitingPoints;
    public Transform exitPoint;

    [Tooltip("Jeda dasar antar kedatangan pelanggan")]
    public float spawnIntervalMin = 3f;
    public float spawnIntervalMax = 6f;

    [Tooltip("Jeda terendah setelah upgrade interval diterapkan")]
    public float minIntervalFloor = 1f;

    [Tooltip("Lama pelanggan menunggu di tengah dengan bubble permintaan tampil")]
    public float waitAtCenter = 1.5f;

    [Tooltip("Lama bubble hasil (terima kasih / barang tidak ada) tampil sebelum pelanggan pergi")]
    public float resultBubbleDuration = 1.2f;

    [Range(0f, 1f)]
    [Tooltip("Peluang pelanggan meminta barang yang sedang ada di etalase. " +
             "Sisanya meminta barang acak dari semua resep (bisa di luar etalase).")]
    public float chanceRequestStocked = 0.5f;

    [Header("UI")]
    public TextMeshProUGUI timerText;

    private bool isActive;
    private bool isSelling;
    private bool timerEnded;

    private float timeRemaining;
    private float spawnCountdown;
    private float intervalMultiplier = 1f;

    private int maxCustomerCapacity = 1;

    // Urutan antrean: index 0 = yang sedang dilayani.
    private readonly List<CustomerController> customers = new List<CustomerController>();
    private readonly List<CraftedItemSO> stockedBuffer = new List<CraftedItemSO>();

    private Coroutine queueRoutine;

    // ---------- Dipanggil GameManager / UI ----------

    public void StartSellPhase()
    {
        if (!ValidateReferences())
            return;

        isActive = true;

        UpgradeManager upgrades = UpgradeManager.Instance;

        intervalMultiplier = upgrades != null
            ? upgrades.GetIntervalMultiplier()
            : 1f;

        int capacity = upgrades != null
            ? upgrades.GetCustomerCapacity()
            : 1;

        // Jumlah customer dibatasi oleh jumlah titik antrean yang tersedia.
        int waitingCount = waitingPoints != null ? waitingPoints.Length : 0;
        maxCustomerCapacity = Mathf.Max(1, Mathf.Min(capacity, waitingCount + 1));

        etalase.SetSlotCount(
            upgrades != null
                ? upgrades.GetEtalaseSlotCount(etalase.BaseSlotCount)
                : etalase.BaseSlotCount
        );

        ResetQueue();

        timerEnded = false;
        isSelling = !startTimerOnFirstClose;
        timeRemaining = phaseDuration;

        ResetSpawnCountdown();
        UpdateTimerText();

        etalaseUI.Open();
    }

    public void NotifyEtalaseClosed()
    {
        if (isActive && !isSelling)
            isSelling = true;
    }

    public void SkipPhase()
    {
        if (!isActive)
            return;

        isActive = false;
        GameManager.Instance.EndSellPhase();
    }

    public void CleanupPhase()
    {
        isActive = false;
        isSelling = false;

        ResetQueue();

        if (etalase != null)
            etalase.ReturnAllToInventory();
    }

    // ---------- Validasi ----------

    private bool ValidateReferences()
    {
        bool ok = true;

        ok &= Require(etalase, nameof(etalase));
        ok &= Require(etalaseUI, nameof(etalaseUI));
        ok &= Require(recipeManager, nameof(recipeManager));
        ok &= Require(customerPrefab, nameof(customerPrefab));
        ok &= Require(spawnPoint, nameof(spawnPoint));
        ok &= Require(centerPoint, nameof(centerPoint));

        if (waitingPoints != null)
        {
            for (int i = 0; i < waitingPoints.Length; i++)
                ok &= Require(waitingPoints[i], $"waitingPoints[{i}]");
        }

        return ok;
    }

    private bool Require(Object reference, string fieldName)
    {
        if (reference != null)
            return true;

        Debug.LogError($"[SellManager] {fieldName} belum di-assign.", this);
        return false;
    }

    // ---------- Loop fase ----------

    private void Update()
    {
        if (!isActive || !isSelling)
            return;

        if (!timerEnded)
            TickTimer();

        // Setelah waktu habis, tidak ada pelanggan baru. Tunggu antrean selesai.
        if (timerEnded)
        {
            if (customers.Count == 0 && queueRoutine == null)
                FinishPhase();

            return;
        }

        // Jadwal spawn berjalan terus, ada barang di etalase atau tidak.
        spawnCountdown -= Time.deltaTime;

        if (spawnCountdown <= 0f)
        {
            if (customers.Count < maxCustomerCapacity)
                SpawnCustomer();

            ResetSpawnCountdown();
        }
    }

    private void TickTimer()
    {
        timeRemaining -= Time.deltaTime;

        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            timerEnded = true;
        }

        UpdateTimerText();
    }

    private void FinishPhase()
    {
        isActive = false;
        GameManager.Instance.EndSellPhase();
    }

    private void ResetSpawnCountdown()
    {
        float min = Mathf.Max(
            minIntervalFloor,
            spawnIntervalMin * intervalMultiplier
        );

        float max = Mathf.Max(
            min,
            spawnIntervalMax * intervalMultiplier
        );

        spawnCountdown = Random.Range(min, max);
    }

    // ---------- Antrean pelanggan ----------

    private void SpawnCustomer()
    {
        var recipes = recipeManager.allRecipes;

        if (recipes == null || recipes.Count == 0)
        {
            Debug.LogError("[SellManager] Daftar recipe kosong, pelanggan tidak bisa dibuat.", this);
            return;
        }

        CustomerController customer = Instantiate(
            customerPrefab,
            spawnPoint.position,
            Quaternion.identity
        );

        customer.Setup(PickWantedItem(recipes));
        customers.Add(customer);

        // Pelanggan baru langsung menuju titik antreannya.
        // Index 0 tidak perlu: ProcessQueue yang menggerakkannya ke tengah.
        int queueIndex = customers.Count - 1;

        if (queueIndex > 0)
            SendToWaitingPoint(customer, queueIndex);

        // Hanya satu coroutine yang memproses transaksi.
        if (queueRoutine == null)
            queueRoutine = StartCoroutine(ProcessQueue());
    }

    private CraftedItemSO PickWantedItem(IReadOnlyList<CraftedItemSO> recipes)
    {
        // Kadang pilih dari barang yang ada di etalase...
        if (Random.value < chanceRequestStocked)
        {
            stockedBuffer.Clear();

            foreach (CraftedItemSO recipe in recipes)
            {
                if (recipe != null && etalase.TryGetPrice(recipe, out _))
                    stockedBuffer.Add(recipe);
            }

            if (stockedBuffer.Count > 0)
                return stockedBuffer[Random.Range(0, stockedBuffer.Count)];
        }

        // ...selebihnya acak dari semua resep (bisa di luar etalase).
        return recipes[Random.Range(0, recipes.Count)];
    }

    private IEnumerator ProcessQueue()
    {
        while (customers.Count > 0)
        {
            CustomerController customer = customers[0];

            if (customer == null)
            {
                customers.RemoveAt(0);
                RepositionQueue();
                continue;
            }

            yield return ServeCustomer(customer);
            yield return SendAway(customer);

            customers.RemoveAt(0);
            Destroy(customer.gameObject);

            // Pelanggan berikutnya maju setelah pelanggan depan pergi.
            RepositionQueue();
        }

        queueRoutine = null;
    }

    // Maju ke tengah, minta barang, lalu transaksi. Bubble tampil sepanjang proses.
    private IEnumerator ServeCustomer(CustomerController customer)
    {
        CraftedItemSO wanted = customer.WantedItem;

        yield return customer.MoveTo(centerPoint.position);

        // Bubble permintaan SELALU tampil. Harga hanya muncul kalau barangnya ada.
        bool inStock = etalase.TryGetPrice(wanted, out int shelfPrice);

        customer.ShowRequest(
            wanted.itemSprite,
            wanted.itemName,
            inStock ? ApplySeasonPrice(wanted, shelfPrice) : 0
        );

        yield return new WaitForSeconds(waitAtCenter);

        // Transaksi memakai harga aktual dari etalase, dengan penyesuaian musim
        // yang sama seperti harga di bubble.
        if (etalase.TrySell(wanted, out int soldPrice))
        {
            int finalPrice = ApplySeasonPrice(wanted, soldPrice);

            CurrencyManager.Instance.Add(finalPrice);

            GameLog.Log($"[Sell] Pelanggan membeli {wanted.itemName} seharga {finalPrice}");

            customer.NotifyBought();
        }
        else
        {
            GameLog.Log($"[Sell] Pelanggan mencari {wanted.itemName}, tidak ada. Pergi.");

            customer.NotifyLeftEmpty();
        }

        // Beri waktu pemain membaca hasilnya, lalu tutup bubble.
        yield return new WaitForSeconds(resultBubbleDuration);
        customer.HideBubble();
    }

    private IEnumerator SendAway(CustomerController customer)
    {
        Transform exit = exitPoint != null ? exitPoint : spawnPoint;
        yield return customer.MoveTo(exit.position);
    }

    private void RepositionQueue()
    {
        for (int i = 1; i < customers.Count; i++)
        {
            if (customers[i] != null)
                SendToWaitingPoint(customers[i], i);
        }
    }

    private void SendToWaitingPoint(CustomerController customer, int queueIndex)
    {
        int pointIndex = queueIndex - 1;

        if (waitingPoints == null || pointIndex >= waitingPoints.Length)
            return;

        customer.GoTo(waitingPoints[pointIndex].position);
    }

    // Hentikan semua proses antrean dan hapus pelanggan yang tersisa.
    private void ResetQueue()
    {
        StopAllCoroutines();
        queueRoutine = null;

        foreach (CustomerController customer in customers)
        {
            if (customer != null)
                Destroy(customer.gameObject);
        }

        customers.Clear();
    }

    private int ApplySeasonPrice(CraftedItemSO item, int price)
    {
        return SeasonManager.Instance != null
            ? SeasonManager.Instance.ApplyPrice(item, price)
            : price;
    }

    // ---------- UI ----------

    private void UpdateTimerText()
    {
        if (timerText == null)
            return;

        int seconds = Mathf.CeilToInt(timeRemaining);

        timerText.text = $"{seconds / 60:00}:{seconds % 60:00}";
    }
}