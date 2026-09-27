using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class BatteryManager : MonoBehaviour
{
    public bool isActionBasedDrain;
    public float maxBattery = 100f;
    public float currentBattery;
    public float drainRate = 5f;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference interactAction;

    public event Action OnBatteryEmpty;

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