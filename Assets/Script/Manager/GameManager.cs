using UnityEngine;
using UnityEngine.UI; // Untuk mengatur tombol UI (Button)

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
    public GameObject bin;

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
        craneController.enabled = false;

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
                craneController.enabled = true;
                bin.SetActive(true);

                spawner.StartSpawning();
                batteryManager.StartBattery();
                break;

            case GameState.Craft:
                craftUIPanel.SetActive(true);
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
        batteryManager.StopBattery();
        EndCollectPhase();
    }

    private void EndCollectPhase()
    {
        Debug.Log("Fase Collect Selesai.");

        spawner.StopSpawning();
        spawner.ClearAllMaterials();
        bin.SetActive(false);

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
}