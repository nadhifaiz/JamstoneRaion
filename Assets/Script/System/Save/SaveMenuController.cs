using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Mengelola daftar save, preview slot terpilih, load, game baru, dan hapus.
public class SaveMenuController : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string gameSceneName = "TestingScene";

    [Header("Daftar Save")]
    [SerializeField] private Transform listContent;
    [SerializeField] private GameObject rowPrefab;
    [SerializeField] private GameObject emptyLabel;

    [Header("Preview Save")]
    [SerializeField] private GameObject previewPanel;
    [SerializeField] private TextMeshProUGUI previewSlotNameText;
    [SerializeField] private TextMeshProUGUI previewDayText;
    [SerializeField] private TextMeshProUGUI previewAmberText;
    [SerializeField] private TextMeshProUGUI previewLastPlayedText;
    [SerializeField] private GameObject previewCompletedBadge;
    [SerializeField] private TextMeshProUGUI previewStatusText;
    [SerializeField] private Button loadButton;
    [SerializeField] private Button deleteButton;

    [Header("Panel Game Baru")]
    [SerializeField] private GameObject newGamePanel;
    [SerializeField] private TMP_InputField nameInput;

    [Header("Panel Konfirmasi Hapus")]
    [SerializeField] private GameObject deletePanel;
    [SerializeField] private TextMeshProUGUI deleteMessageText;

    private List<SaveSlotMeta> slots = new List<SaveSlotMeta>();
    private SaveSlotMeta selectedSlot;
    private SaveSlotMeta pendingDelete;

    private void Start()
    {
        Time.timeScale = 1f;

        if (newGamePanel != null) newGamePanel.SetActive(false);
        if (deletePanel != null) deletePanel.SetActive(false);

        if (loadButton != null) loadButton.onClick.AddListener(LoadSelectedSlot);
        if (deleteButton != null) deleteButton.onClick.AddListener(AskDeleteSelectedSlot);

        RefreshList();
    }

    private void OnDestroy()
    {
        if (loadButton != null) loadButton.onClick.RemoveListener(LoadSelectedSlot);
        if (deleteButton != null) deleteButton.onClick.RemoveListener(AskDeleteSelectedSlot);
    }

    // Mengambil ulang metadata save dan membangun daftar baris.
    public void RefreshList()
    {
        slots = SaveStorage.GetAllSlotMetas();

        UIListHelper.Populate(listContent, rowPrefab, slots, (go, meta) =>
        {
            SaveSlotRowUI row = go.GetComponent<SaveSlotRowUI>();
            if (row != null)
                row.Bind(meta, SelectSlot);
        });

        if (emptyLabel != null) emptyLabel.SetActive(slots.Count == 0);

        // Pilih slot pertama otomatis jika ada save.
        if (slots.Count > 0)
        {
            SaveSlotMeta slotToSelect = selectedSlot == null
                ? slots[0]
                : slots.Find(slot => slot.id == selectedSlot.id);

            if (slotToSelect == null) slotToSelect = slots[0];
            SelectSlot(slotToSelect);
        }
        else
        {
            selectedSlot = null;
            ShowPreview(false);
        }
    }

    // Dipanggil ketika pengguna mengeklik sebuah baris save.
    public void SelectSlot(SaveSlotMeta meta)
    {
        if (meta == null) return;

        selectedSlot = meta;

        if (previewSlotNameText != null)
            previewSlotNameText.text = meta.slotName;

        if (previewDayText != null)
            previewDayText.text = $"Hari {meta.currentDay}";

        if (previewAmberText != null)
            previewAmberText.text = $"{meta.amber} Amber";

        if (previewLastPlayedText != null)
        {
            previewLastPlayedText.text = meta.LastPlayedLocal.ToString(
                "dd/MM/yyyy HH:mm",
                System.Globalization.CultureInfo.InvariantCulture);
        }

        if (previewCompletedBadge != null)
            previewCompletedBadge.SetActive(meta.isCompleted);

        if (previewStatusText != null)
            previewStatusText.text = meta.isCompleted ? "Tamat" : "Belum tamat";

        ShowPreview(true);
    }

    private void ShowPreview(bool visible)
    {
        if (previewPanel != null) previewPanel.SetActive(visible);
        if (loadButton != null) loadButton.interactable = visible && selectedSlot != null;
        if (deleteButton != null) deleteButton.interactable = visible && selectedSlot != null;
    }

    // Tombol Load pada panel preview.
    public void LoadSelectedSlot()
    {
        if (selectedSlot == null) return;

        SaveSession.SelectSlot(selectedSlot.id);
        LoadGameScene();
    }

    // Tombol Game Baru.
    public void OpenNewGamePanel()
    {
        if (nameInput != null)
        {
            nameInput.text = $"Game {slots.Count + 1}";
            nameInput.ActivateInputField();
        }

        if (newGamePanel != null) newGamePanel.SetActive(true);
    }

    public void CancelNewGame()
    {
        if (newGamePanel != null) newGamePanel.SetActive(false);
    }

    public void ConfirmNewGame()
    {
        string slotName = nameInput != null ? nameInput.text : $"Game {slots.Count + 1}";
        if (string.IsNullOrWhiteSpace(slotName))
            slotName = $"Game {slots.Count + 1}";

        SaveSession.StartNewGame(slotName);
        LoadGameScene();
    }

    private void LoadGameScene()
    {
        if (!Application.CanStreamedLevelBeLoaded(gameSceneName))
        {
            Debug.LogError($"[SaveMenu] Scene '{gameSceneName}' tidak ada di Build Settings (File > Build Profiles/Settings > Scenes In Build).");
            return;
        }

        SceneManager.LoadScene(gameSceneName);
    }

    // Tombol Delete pada panel preview: tampilkan konfirmasi dahulu.
    public void AskDeleteSelectedSlot()
    {
        if (selectedSlot == null) return;

        pendingDelete = selectedSlot;

        if (deleteMessageText != null)
            deleteMessageText.text =
                $"Hapus save \"{pendingDelete.slotName}\"?\nTindakan ini tidak bisa dibatalkan.";

        if (deletePanel != null) deletePanel.SetActive(true);
    }

    public void ConfirmDelete()
    {
        if (pendingDelete != null)
        {
            SaveStorage.DeleteSlot(pendingDelete.id);

            if (SaveSession.CurrentSlotId == pendingDelete.id)
                SaveSession.Clear();
        }

        pendingDelete = null;

        if (deletePanel != null) deletePanel.SetActive(false);

        // RefreshList akan memilih slot pertama yang tersisa atau menyembunyikan preview.
        selectedSlot = null;
        RefreshList();
    }

    public void CancelDelete()
    {
        pendingDelete = null;
        if (deletePanel != null) deletePanel.SetActive(false);
    }
}
