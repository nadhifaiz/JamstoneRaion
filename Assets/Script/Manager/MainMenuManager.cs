
using UnityEngine;

// Mengelola perpindahan panel pada Main Menu.
// Tidak menangani logika save/load.
public class MainMenuManager : MonoBehaviour
{
    [Header("Panel Main Menu")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject savePanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private GameObject creditsPanel;

    private void Start()
    {
        ShowMainMenu();
    }

    public void ShowMainMenu()
    {
        HideAllPanels();
        SetPanelActive(mainMenuPanel, true);
    }

    public void ShowSavePanel()
    {
        HideAllPanels();
        SetPanelActive(savePanel, true);
    }

    public void ShowSettingsPanel()
    {
        HideAllPanels();
        SetPanelActive(settingsPanel, true);
    }

    public void ShowTutorialPanel()
    {
        HideAllPanels();
        SetPanelActive(tutorialPanel, true);
    }

    public void ShowCreditsPanel()
    {
        HideAllPanels();
        SetPanelActive(creditsPanel, true);
    }

    private void HideAllPanels()
    {
        SetPanelActive(mainMenuPanel, false);
        SetPanelActive(savePanel, false);
        SetPanelActive(settingsPanel, false);
        SetPanelActive(tutorialPanel, false);
        SetPanelActive(creditsPanel, false);
    }

    private void SetPanelActive(GameObject panel, bool isActive)
    {
        if (panel != null)
            panel.SetActive(isActive);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
