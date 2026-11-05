using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

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

    [Header("Daily Progress (Urutan)")]
    public bool hasCollectedToday = false;
    public bool hasCraftedToday = false;
    public bool hasSoldToday = false;

    [Header("Tampilan Hari (UI)")]
    public TextMeshProUGUI[] dayTexts;
    public string dayTextFormat = "Day {0}";
    public UnityEvent<int> onDayChanged;

    public Image mainBackgroundImage;
    public Sprite bgHubCollect;
    public Sprite bgHubCraft;
    public Sprite bgHubSell;

    [Header("Transisi Pergantian Hari")]
    public CanvasGroup transitionCanvasGroup;
    public TextMeshProUGUI transitionText;
    public float screenFadeDuration = 1f;
    public float textFadeDuration = 0.8f;
    public float textHoldDuration = 1.2f;

    [Header("Collect System References")]
    public GameObject collectBackground;
    public MaterialSpawner spawner;
    public CraneController craneController;
    public BatteryManager batteryManager;
    public BinController bin;
    public bool deferCollectStart;

    [Header("Craft System References")]
    public CraftingUIManager craftingUIManager;

    [Header("Sell System References")]
    public SellManager sellManager;

    [Header("Shared UI References")]
    public InventoryPanelUI inventoryPanel;

    [Header("UI Panels")]
    public GameObject hubPanel;
    public GameObject collectUIPanel;
    public GameObject craftUIPanel;
    public GameObject sellUIPanel;

    [Header("Hub Menu Buttons")]
    public Button btnCollect;
    public Button btnCraft;
    public Button btnSell;

    private bool isTransitioning = false;
    public bool IsTransitioning => isTransitioning;

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

        // Pastikan layar transisi mati di awal
        if (transitionCanvasGroup != null)
        {
            transitionCanvasGroup.alpha = 0f;
            transitionCanvasGroup.gameObject.SetActive(false);
        }

        if (SaveManager.Instance != null) SaveManager.Instance.LoadGame();
        UpdateDayUI();
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
        GameLog.Log($"--- Masuk ke Fase: {currentState} | Hari: {currentDay} ---");

        // DayEnd: jangan sembunyikan panel dulu, biar transisi fade menutupi tampilan terakhir
        if (newState == GameState.DayEnd)
        {
            EndDayLogic();
            return;
        }

        HideAllPanels();
        ClearAllPhaseObjects();

        switch (currentState)
        {
            case GameState.Hub: EnterHub(); break;
            case GameState.Collect: EnterCollect(); break;
            case GameState.Craft: EnterCraft(); break;
            case GameState.Sell: EnterSell(); break;
        }

        if (SaveManager.Instance != null) SaveManager.Instance.MarkDirty();
    }

    private void RequestPhase(GameState target, Button focusButton)
    {
        if (PhaseTransition.Instance != null) PhaseTransition.Instance.GoToPhase(target, focusButton.transform);
        else ChangeState(target);
    }

    private void ReturnToHub(Button focusButton)
    {
        if (PhaseTransition.Instance != null) PhaseTransition.Instance.ReturnToHub(focusButton.transform);
        else ChangeState(GameState.Hub);
    }

    private void HideAllPanels()
    {
        hubPanel.SetActive(false);
        collectUIPanel.SetActive(false);
        craftUIPanel.SetActive(false);
        sellUIPanel.SetActive(false);

        if (inventoryPanel != null) inventoryPanel.Close();
        if (ItemDetailPanel.Instance != null) ItemDetailPanel.Instance.Hide(); // ← baris baru
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
        UpdateHubBackground();
        UpdateHubButtons();

        if (hasCollectedToday && hasCraftedToday && hasSoldToday)
        {
            ChangeState(GameState.DayEnd); // musiknya diatur EndDayLogic
            return;
        }

        if (CutsceneManager.Instance != null) CutsceneManager.Instance.TryPlayPending();
    }

    private void EnterCollect()
    {
        collectUIPanel.SetActive(true);
        if (!deferCollectStart) StartCollectGameplay();
    }

    public void StartCollectGameplay() => InitializeCollectPhaseObjects();

    private void EnterCraft()
    {
        craftUIPanel.SetActive(true);
        InitializeCraftPhaseObjects();
    }

    private void EnterSell()
    {
        sellUIPanel.SetActive(true);
        InitializeSellPhaseObjects();
    }

    // ==========================================
    // UPDATE UI HARI & BACKGROUND HUB
    // ==========================================

    private void UpdateDayUI()
    {
        string label = string.Format(dayTextFormat, currentDay);

        if (dayTexts != null)
        {
            foreach (var t in dayTexts)
            {
                if (t != null) t.text = label;
            }
        }

        onDayChanged?.Invoke(currentDay);
    }

    private void UpdateHubBackground()
    {
        Sprite newBg;

        if (!hasCollectedToday) newBg = bgHubCollect;
        else if (!hasCraftedToday) newBg = bgHubCraft;
        else newBg = bgHubSell;

        if (mainBackgroundImage != null && newBg != null)
        {
            mainBackgroundImage.sprite = newBg;
        }
    }

    private void UpdateHubButtons()
    {
        btnCollect.interactable = !hasCollectedToday;
        btnCraft.interactable = hasCollectedToday && !hasCraftedToday;
        btnSell.interactable = hasCraftedToday && !hasSoldToday;
    }

    // Dipanggil dari tombol UI
    public void Button_StartCollect() => RequestPhase(GameState.Collect, btnCollect);
    public void Button_StartCraft() => RequestPhase(GameState.Craft, btnCraft);
    public void Button_StartSell() => RequestPhase(GameState.Sell, btnSell);
    public void Button_GoToMainMenu() => ScreenTransition.LoadScene("MainMenu");

    // ==========================================
    // MENGAKHIRI FASE
    // ==========================================

    public void SkipCollectPhase()
    {
        GameLog.Log("Fase Collect di-skip oleh pemain!");
        EndCollectPhase();
    }

    private void EndCollectPhase()
    {
        GameLog.Log("Fase Collect Selesai.");
        hasCollectedToday = true;
        ReturnToHub(btnCollect);
    }

    public void EndCraftPhase()
    {
        GameLog.Log("Fase Craft Selesai.");
        hasCraftedToday = true;
        ReturnToHub(btnCraft);
    }

    public void EndSellPhase()
    {
        GameLog.Log("Fase Sell Selesai.");
        hasSoldToday = true;
        ReturnToHub(btnSell);
    }

    // ==========================================
    // PERGANTIAN HARI (DENGAN TRANSISI)
    // ==========================================

    private void EndDayLogic()
    {
        GameLog.Log($"Hari ke-{currentDay} berakhir.");
        if (isTransitioning) return;
        AudioManager.Music(MusicId.None);
        StartCoroutine(DayTransitionRoutine());
    }

    private IEnumerator DayTransitionRoutine()
    {
        isTransitioning = true;

        // Siapkan panel (blokir klik selama transisi)
        transitionCanvasGroup.gameObject.SetActive(true);
        transitionCanvasGroup.blocksRaycasts = true;
        transitionCanvasGroup.alpha = 0f;
        SetTransitionTextAlpha(0f);
        transitionText.text = $"Day {currentDay} Ended";

        // 1. Layar jadi gelap (fade in hitam)
        yield return FadeScreen(0f, 1f, screenFadeDuration);

        // 2. Teks "Day X Ended" fade in -> tahan -> fade out
        yield return FadeText(0f, 1f, textFadeDuration);
        yield return new WaitForSecondsRealtime(textHoldDuration);
        yield return FadeText(1f, 0f, textFadeDuration);
        if (CutsceneManager.Instance != null)
            yield return CutsceneManager.Instance.PlayEndingIfNeeded(currentDay);

        // 3. Di balik layar hitam: naikkan hari, reset progres, kembali ke Hub
        currentDay++;
        hasCollectedToday = false;
        hasCraftedToday = false;
        hasSoldToday = false;

        UpdateDayUI();
        ChangeState(GameState.Hub);
        if (CutsceneManager.Instance != null)
            yield return CutsceneManager.Instance.PlayPendingRoutine();

        AudioManager.Music(MusicId.InGame);

        // 4. Teks "Day X+1 Started..." fade in -> tahan -> fade out
        transitionText.text = $"Day {currentDay} Started...";
        yield return FadeText(0f, 1f, textFadeDuration);
        yield return new WaitForSecondsRealtime(textHoldDuration);
        yield return FadeText(1f, 0f, textFadeDuration);

        // 5. Layar jadi terang lagi (fade out hitam)
        yield return FadeScreen(1f, 0f, screenFadeDuration);

        transitionCanvasGroup.blocksRaycasts = false;
        transitionCanvasGroup.gameObject.SetActive(false);
        isTransitioning = false;
    }

    private IEnumerator FadeScreen(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            transitionCanvasGroup.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        transitionCanvasGroup.alpha = to;
    }

    private IEnumerator FadeText(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            SetTransitionTextAlpha(Mathf.Lerp(from, to, t / duration));
            yield return null;
        }
        SetTransitionTextAlpha(to);
    }

    private void SetTransitionTextAlpha(float a)
    {
        transitionText.alpha = a;
    }

    // ==========================================
    // HELPER FASE: COLLECT
    // ==========================================

    private void InitializeCollectPhaseObjects()
    {
        craneController.gameObject.SetActive(true);
        collectBackground.SetActive(true);
        bin.gameObject.SetActive(true);

        spawner.StartSpawning();
        batteryManager.StartBattery();
    }

    private void ClearCollectPhaseObjects()
    {
        spawner.StopSpawning();
        spawner.ClearAllMaterials();

        collectBackground.SetActive(false);
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
        else
        {
            Debug.LogError("[GameManager] 'sellManager' belum di-assign di inspector, logika Sell tidak jalan.", this);
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