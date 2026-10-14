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

    [Header("Game Data")]
    public GameState currentState;
    public int currentDay = 1;
    public int maxDays = 30;

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
    public CraftingUIManager craftingUIManager; // TAMBAHAN: Referensi ke UI Crafting

    [Header("UI Panels")]
    public GameObject hubPanel;
    public GameObject collectUIPanel;
    public GameObject craftUIPanel;
    public GameObject sellUIPanel;

    [Header("Hub Menu Buttons")]
    public Button btnCollect;
    public Button btnCraft;
    public Button btnSell;

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

    public void ChangeState(GameState newState)
    {
        currentState = newState;
        Debug.Log($"--- Masuk ke Fase: {currentState} | Hari: {currentDay} ---");

        hubPanel.SetActive(false);
        collectUIPanel.SetActive(false);
        craftUIPanel.SetActive(false);
        sellUIPanel.SetActive(false);

        ClearCollectPhaseObjects();
        ClearCraftPhaseObjects();
        ClearSellPhaseObjects();

        switch (currentState)
        {
            case GameState.Hub:
                hubPanel.SetActive(true);
                UpdateHubButtons();

                if (hasCollectedToday && hasCraftedToday && hasSoldToday)
                {
                    ChangeState(GameState.DayEnd);
                }
                break;

            case GameState.Collect:
                collectUIPanel.SetActive(true);
                InitializeCollectPhaseObjects();
                break;

            case GameState.Craft:
                craftUIPanel.SetActive(true);
                InitializeCraftPhaseObjects(); // TAMBAHAN: Panggil inisialisasi fase Craft
                break;

            case GameState.Sell:
                sellUIPanel.SetActive(true);
                break;

            case GameState.DayEnd:
                EndDayLogic();
                break;
        }
    }

    // --- LOGIKA HUB MENU ---
    private void UpdateHubButtons()
    {
        btnCollect.interactable = !hasCollectedToday;
        btnCraft.interactable = hasCollectedToday && !hasCraftedToday;
        btnSell.interactable = hasCraftedToday && !hasSoldToday;
    }

    // -- LOGIKA TOMBOL UI ---
    public void Button_StartCollect() => ChangeState(GameState.Collect);
    public void Button_StartCraft() => ChangeState(GameState.Craft);
    public void Button_StartSell() => ChangeState(GameState.Sell);


    // --- LOGIKA MENGAKHIRI FASE ---
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

    // --- LOGIKA PERGANTIAN HARI ---
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
            Debug.Log("GAME TAMAT! 30 Hari Selesai.");
        }
    }

    // --- Fungsi Helper Fase ---
    private void ClearCollectPhaseObjects()
    {
        spawner.StopSpawning();
        spawner.ClearAllMaterials();

        bin.gameObject.SetActive(false);
        craneController.gameObject.SetActive(false);
        batteryManager.StopBattery();
    }

    private void InitializeCollectPhaseObjects()
    {
        craneController.gameObject.SetActive(true);
        bin.gameObject.SetActive(true);

        spawner.StartSpawning();
        batteryManager.StartBattery();
    }

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

    private void ClearSellPhaseObjects()
    {
        // Tambahkan logika pembersihan objek terkait fase sell jika diperlukan
    }
}