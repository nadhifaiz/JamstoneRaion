using UnityEngine;
using UnityEngine.InputSystem;
using System;
using TMPro;
using UnityEngine.UI;

public class BatteryManager : MonoBehaviour
{
    [Header("Battery Settings")]
    [Tooltip("True: baterai hanya terkuras saat crane bergerak/menggenggam")]
    [SerializeField] private bool isActionBasedDrain;
    [SerializeField] private float maxBattery = 100f;
    [SerializeField] private float drainRate = 5f;
    [SerializeField] private float currentBattery;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference interactAction;

    [Header("Tampilan Baterai (UI, isi yang dipakai)")]
    [Tooltip("Slider dengan Min 0 dan Max 1 (nilai dinormalisasi).")]
    [SerializeField] private Slider batterySlider;
    [Tooltip("Image bertipe Filled (Fill Amount dipakai sebagai bar).")]
    [SerializeField] private Image batteryFillImage;
    [SerializeField] private TextMeshProUGUI batteryPercentText;
    [Tooltip("{0} akan diganti dengan persen baterai.")]
    [SerializeField] private string percentFormat = "{0}%";

    public event Action OnBatteryEmpty;

    // Read-only untuk UI baterai nanti
    public float CurrentBattery => currentBattery;
    public float MaxBattery => maxBattery;

    private bool isActive = false;

    public void StartBattery()
    {
        currentBattery = maxBattery;
        isActive = true;
        UpdateBatteryUI();
    }

    public void StopBattery()
    {
        isActive = false;
    }

    private void Update()
    {
        if (!isActive) return;

        if (isActionBasedDrain)
        {
            if (moveAction.action.ReadValue<Vector2>() != Vector2.zero ||
                interactAction.action.IsPressed())
            {
                DrainBattery();
            }
        }
        else
        {
            DrainBattery();
        }
    }

    private void DrainBattery()
    {
        currentBattery = Mathf.Max(0f, currentBattery - drainRate * Time.deltaTime);
        UpdateBatteryUI();

        if (currentBattery <= 0f)
        {
            isActive = false;
            OnBatteryEmpty?.Invoke();
        }
    }

    private void UpdateBatteryUI()
    {
        float normalized = maxBattery > 0f ? Mathf.Clamp01(currentBattery / maxBattery) : 0f;

        if (batterySlider != null) batterySlider.value = normalized;
        if (batteryFillImage != null) batteryFillImage.fillAmount = normalized;
        if (batteryPercentText != null)
        {
            batteryPercentText.text = string.Format(percentFormat, Mathf.RoundToInt(normalized * 100f));
        }
    }
}