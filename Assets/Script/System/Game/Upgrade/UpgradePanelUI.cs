using System.Linq;
using TMPro;
using UnityEngine;

// Panel Upgrade di Hub. Pasang di objek di dalam hubPanel (aktif selama Hub), BUKAN di panelRoot.
// Panel ikut tertutup otomatis saat Hub dimatikan (masuk fase).
public class UpgradePanelUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Transform cardContent;        // Content milik Scroll View / grid kartu
    [SerializeField] private GameObject cardPrefab;        // prefab dengan UpgradeCardUI
    [SerializeField] private TextMeshProUGUI amberText;    // opsional: saldo Amber

    private bool subscribed;
    private bool needsRefresh;

    public bool IsOpen => panelRoot.activeSelf;

    void OnEnable()
    {
        Subscribe();
    }

    void Start()
    {
        Subscribe(); // OnEnable bisa lebih dulu dari Awake manager; Start pasti sesudahnya
    }

    void OnDisable()
    {
        Unsubscribe();
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    // Refresh digabung di akhir frame: satu pembelian memicu beberapa event (Amber, inventory, upgrade)
    void LateUpdate()
    {
        if (!needsRefresh) return;

        needsRefresh = false;
        Refresh();
    }

    public void Open()
    {
        panelRoot.SetActive(true);
        Refresh();
    }

    public void Close() => panelRoot.SetActive(false);

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    private void Refresh()
    {
        if (!panelRoot.activeSelf) return;

        UpgradeManager manager = UpgradeManager.Instance;
        if (manager == null) return;

        var list = manager.Upgrades.Where(u => u != null).ToList();

        UIListHelper.Populate(cardContent, cardPrefab, list, (go, upgrade) =>
        {
            go.GetComponent<UpgradeCardUI>().Bind(upgrade, manager, OnBuyClicked);
        });

        if (amberText != null && CurrencyManager.Instance != null)
        {
            amberText.text = $"{CurrencyManager.Instance.Amber} Amber";
        }
    }

    private void OnBuyClicked(UpgradeSO upgrade)
    {
        UpgradeManager.Instance.TryBuy(upgrade); // tampilan diperbarui lewat event
    }

    private void MarkRefresh() => needsRefresh = true;
    private void OnAmberChanged(int _) => needsRefresh = true;

    private void Subscribe()
    {
        if (subscribed) return;
        if (UpgradeManager.Instance == null || CurrencyManager.Instance == null || InventoryManager.Instance == null) return;

        subscribed = true;
        UpgradeManager.Instance.OnChanged += MarkRefresh;
        CurrencyManager.Instance.OnAmberChanged += OnAmberChanged;
        InventoryManager.Instance.OnChanged += MarkRefresh;
    }

    private void Unsubscribe()
    {
        if (!subscribed) return;
        subscribed = false;

        if (UpgradeManager.Instance != null) UpgradeManager.Instance.OnChanged -= MarkRefresh;
        if (CurrencyManager.Instance != null) CurrencyManager.Instance.OnAmberChanged -= OnAmberChanged;
        if (InventoryManager.Instance != null) InventoryManager.Instance.OnChanged -= MarkRefresh;
    }
}