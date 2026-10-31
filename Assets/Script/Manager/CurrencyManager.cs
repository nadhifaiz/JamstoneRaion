
using System;
using UnityEngine;
using TMPro;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    [Header("Tampilan Saldo (UI)")]
    [Tooltip("Masukkan semua teks saldo dari tiap panel (Hub, Collect, Craft, Sell, dll).")]
    public TextMeshProUGUI[] amberTexts;

    [Tooltip("{0} akan diganti dengan jumlah Amber.")]
    public string amberTextFormat = "{0} Amber";

    public int Amber { get; private set; }

    // Dipanggil ketika saldo berubah.
    public event Action<int> OnAmberChanged;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        UpdateAmberUI();
    }

    public void Add(int amount)
    {
        if (amount <= 0) return;

        Amber += amount;

        GameLog.Log($"[Currency] +{amount} Amber. Total: {Amber}");

        UpdateAmberUI();
        OnAmberChanged?.Invoke(Amber);
    }

    // Mengatur saldo tanpa memicu event, khusus untuk load save.
    public void SetAmberSilently(int value)
    {
        Amber = Mathf.Max(0, value);
        UpdateAmberUI();
    }

    public bool TrySpend(int amount)
    {
        if (amount <= 0 || Amber < amount) return false;

        Amber -= amount;

        UpdateAmberUI();
        OnAmberChanged?.Invoke(Amber);

        return true;
    }

    private void UpdateAmberUI()
    {
        if (amberTexts == null) return;

        string label = string.Format(amberTextFormat, Amber);

        foreach (TextMeshProUGUI text in amberTexts)
        {
            if (text != null)
                text.text = label;
        }
    }
}
