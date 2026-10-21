using System;
using UnityEngine;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    // Aman dipanggil dari mana saja, bahkan kalau PauseManager belum ada di scene
    public static bool IsPaused => Instance != null && Instance.isPaused;

    [Header("UI")]
    [Tooltip("Panel pause fullscreen. Harus di Canvas dengan sorting order tertinggi, punya Image dengan Raycast Target aktif.")]
    [SerializeField] private GameObject pausePanel;

    public event Action OnPaused;
    public event Action OnResumed;

    private bool isPaused;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (pausePanel != null) pausePanel.SetActive(false);
    }

    void OnDestroy()
    {
        if (Instance != this) return;

        // Jaga-jaga supaya game berikutnya tidak membeku (restart scene, kembali ke menu, dll)
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Instance = null;
    }

    // Dihubungkan ke tombol Pause di inspector
    public void Pause()
    {
        if (isPaused) return;

        // Jangan menyela transisi pergantian hari
        if (GameManager.Instance != null && GameManager.Instance.IsTransitioning) return;

        isPaused = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        if (pausePanel != null) pausePanel.SetActive(true);

        OnPaused?.Invoke();
    }

    // Dihubungkan ke tombol Resume di inspector
    public void Resume()
    {
        if (!isPaused) return;

        isPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        if (pausePanel != null) pausePanel.SetActive(false);

        OnResumed?.Invoke();
    }

    public void TogglePause()
    {
        if (isPaused) Resume();
        else Pause();
    }
}