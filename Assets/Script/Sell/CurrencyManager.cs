using System;
using UnityEngine;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    public int Amber { get; private set; }

    // Untuk UI saldo nanti: CurrencyManager.Instance.OnAmberChanged += ...
    public event Action<int> OnAmberChanged;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void Add(int amount)
    {
        if (amount <= 0) return;
        Amber += amount;
        Debug.Log($"[Currency] +{amount} Amber. Total: {Amber}");
        OnAmberChanged?.Invoke(Amber);
    }

    public bool TrySpend(int amount)
    {
        if (amount <= 0 || Amber < amount) return false;
        Amber -= amount;
        OnAmberChanged?.Invoke(Amber);
        return true;
    }
}