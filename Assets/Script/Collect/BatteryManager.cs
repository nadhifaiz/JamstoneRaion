using UnityEngine;
using UnityEngine.InputSystem;
using System;

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

    public event Action OnBatteryEmpty;

    // Read-only untuk UI baterai nanti
    public float CurrentBattery => currentBattery;
    public float MaxBattery => maxBattery;

    private bool isActive = false;

    public void StartBattery()
    {
        currentBattery = maxBattery;
        isActive = true;
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
        currentBattery -= drainRate * Time.deltaTime;

        if (currentBattery <= 0)
        {
            currentBattery = 0;
            isActive = false;

            OnBatteryEmpty?.Invoke();
        }
    }
}