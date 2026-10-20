using UnityEngine;
using UnityEngine.UI;

public enum GameState
{
    Hub,
    Collect,
    Craft,
    Sell,
    DayEnd
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // ==========================================
    // DATA & REFERENSI (INSPECTOR)
    // ==========================================

    [Header("Game Data")]
    public GameState currentState;
    public int currentDay = 1;
    public int maxDays = 7;

    [Header("Daily Progress (Urutan)")]
    public bool hasCollectedToday = false;
    public bool hasCraftedToday = false;
    public bool hasSoldToday = false;

    [Header("Collect System References")]
    public MaterialSpawner spawner;
    public CraneController craneController;
    public BatteryManager batteryManager;
    public BinController bin;

    [Header("Craft System References")]
    public CraftingUIManager craftingUIManager;

    [Header("Sell System References")]
    public SellManager sellManager;

    [Header("Shared UI References")]
    public InventoryPanelUI inventoryPanel; // panel inventory ditutup otomatis tiap ganti fase

    [Header("UI Panels")]
    public GameObject hubPanel;
    public GameObject collectUIPanel;
    public GameObject craftUIPanel;
    public GameObject sellUIPanel;

    [Header("Hub Menu Buttons")]
    public Button btnCollect;
    public Button btnCraft;
    public Button btnSell;

    // ==========================================
    // SIKLUS HIDUP UNITY
    // ==========================================

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
        if (batteryManager != null)
        {
            batteryManager.OnBatteryEmpty += EndCollectPhase;
        }

        ChangeState(GameState.Hub);
    }

    void OnDestroy()
    {
        if (batteryManager != null)
        {
            batteryManager.OnBatteryEmpty -= EndCollectPhase;
        }
    }

    // ==========================================
    // STATE MACHINE
    // ==========================================

    public void ChangeState(GameState newState)
    {
        currentState = newState;
        Debug.Log($"--- Masuk ke Fase: {currentState} | Hari: {currentDay} ---");

        HideAllPanels();
        ClearAllPhaseObjects();

        switch (currentState)
        {
            case GameState.Hub: EnterHub(); break;
            case GameState.Collect: EnterCollect(); break;
            case GameState.Craft: EnterCraft(); break;
            case GameState.Sell: EnterSell(); break;
            case GameState.DayEnd: EndDayLogic(); break;
        }
    }

    private void HideAllPanels()
    {
        hubPanel.SetActive(false);
        collectUIPanel.SetActive(false);
        craftUIPanel.SetActive(false);
        sellUIPanel.SetActive(false);

        if (inventoryPanel != null) inventoryPanel.Close();
    }

    private void ClearAllPhaseObjects()
    {
        ClearCollectPhaseObjects();
        ClearCraftPhaseObjects();
        ClearSellPhaseObjects();
    }

    private void EnterHub()
    {
        hubPanel.SetActive(true);
        UpdateHubButtons();

        if (hasCollectedToday && hasCraftedToday && hasSoldToday)
        {
            ChangeState(GameState.DayEnd);
        }
    }

    private void EnterCollect()
    {
        collectUIPanel.SetActive(true);
        InitializeCollectPhaseObjects();
    }

    private void EnterCraft()
    {
        craftUIPanel.SetActive(true);
        InitializeCraftPhaseObjects();
    }

    private void EnterSell()
    {
        sellUIPanel.SetActive(true); // harus aktif dulu sebelum fase dimulai
        InitializeSellPhaseObjects();
    }

    // ==========================================
    // HUB MENU
    // ==========================================

    private void UpdateHubButtons()
    {
        btnCollect.interactable = !hasCollectedToday;
        btnCraft.interactable = hasCollectedToday && !hasCraftedToday;
        btnSell.interactable = hasCraftedToday && !hasSoldToday;
    }

    // Dipanggil dari tombol UI (nama method jangan diubah, terhubung di inspector)
    public void Button_StartCollect() => ChangeState(GameState.Collect);
    public void Button_StartCraft() => ChangeState(GameState.Craft);
    public void Button_StartSell() => ChangeState(GameState.Sell);

    // ==========================================
    // MENGAKHIRI FASE
    // ==========================================

    public void SkipCollectPhase()
    {
        Debug.Log("Fase Collect di-skip oleh pemain!");
        EndCollectPhase();
    }

    private void EndCollectPhase()
    {
        Debug.Log("Fase Collect Selesai.");
        hasCollectedToday = true;
        ChangeState(GameState.Hub);
    }

    public void EndCraftPhase()
    {
        Debug.Log("Fase Craft Selesai.");
        hasCraftedToday = true;
        ChangeState(GameState.Hub);
    }

    public void EndSellPhase()
    {
        Debug.Log("Fase Sell Selesai.");
        hasSoldToday = true;
        ChangeState(GameState.Hub);
    }

    // ==========================================
    // PERGANTIAN HARI
    // ==========================================

    private void EndDayLogic()
    {
        Debug.Log($"Hari ke-{currentDay} berakhir.");

        if (currentDay < maxDays)
        {
            currentDay++;
            hasCollectedToday = false;
            hasCraftedToday = false;
            hasSoldToday = false;

            ChangeState(GameState.Hub);
        }
        else
        {
            Debug.Log("GAME TAMAT! 7 Hari Selesai.");
        }
    }

    // ==========================================
    // HELPER FASE: COLLECT
    // ==========================================

    private void InitializeCollectPhaseObjects()
    {
        craneController.gameObject.SetActive(true);
        bin.gameObject.SetActive(true);

        spawner.StartSpawning();
        batteryManager.StartBattery();
    }

    private void ClearCollectPhaseObjects()
    {
        spawner.StopSpawning();
        spawner.ClearAllMaterials();

        bin.gameObject.SetActive(false);
        craneController.gameObject.SetActive(false);
        batteryManager.StopBattery();
    }

    // ==========================================
    // HELPER FASE: CRAFT
    // ==========================================

    private void InitializeCraftPhaseObjects()
    {
        if (craftingUIManager != null)
        {
            craftingUIManager.SetupCraftingUI();
        }
    }

    private void ClearCraftPhaseObjects()
    {
        if (craftingUIManager != null)
        {
            craftingUIManager.ForceClearMeja();
        }
    }

    // ==========================================
    // HELPER FASE: SELL
    // ==========================================

    private void InitializeSellPhaseObjects()
    {
        if (sellManager != null)
        {
            sellManager.StartSellPhase();
        }
    }

    private void ClearSellPhaseObjects()
    {
        // Mengembalikan item etalase ke inventory & menghapus pelanggan. Aman dipanggil berulang.
        if (sellManager != null)
        {
            sellManager.CleanupPhase();
        }
    }
}