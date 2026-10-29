using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// Layar ending sementara: judul ("Bersambung..." atau "Tamat") + pilihan Lanjut Main / Kembali ke Menu.
// Panel perlu background layar penuh dengan raycastTarget aktif dan Canvas di atas layar transisi hari.
public class EndingScreenUI : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private string toBeContinuedTitle = "Bersambung...";
    [SerializeField] private string theEndTitle = "Tamat";
    [SerializeField] private string menuSceneName = "MainMenu";

    private bool choiceMade;

    public bool ReturnToMenuRequested { get; private set; }

    void Awake()
    {
        panelRoot.SetActive(false);
    }

    // Selesai setelah pemain memilih salah satu tombol
    public IEnumerator Show(EndingType type)
    {
        choiceMade = false;
        ReturnToMenuRequested = false;

        titleText.text = type == EndingType.ToBeContinued ? toBeContinuedTitle : theEndTitle;
        panelRoot.SetActive(true);

        yield return new WaitUntil(() => choiceMade);

        panelRoot.SetActive(false);
    }

    // Hubungkan ke tombol "Lanjut Main"
    public void Button_Continue()
    {
        ReturnToMenuRequested = false;
        choiceMade = true;
    }

    // Hubungkan ke tombol "Kembali ke Menu"
    public void Button_BackToMenu()
    {
        ReturnToMenuRequested = true;
        choiceMade = true;
    }

    public IEnumerator GoToMenu()
    {
        yield return null; // beri satu frame agar live save (LateUpdate) selesai menulis
        Time.timeScale = 1f;
        SceneManager.LoadScene(menuSceneName);
    }
}