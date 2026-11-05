using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TutorialUI : MonoBehaviour
{
    [Header("Slide Data")]
    [SerializeField] private TutorialSlide[] slides;

    [Header("UI Display")]
    [SerializeField] private Image tutorialDisplayImage;
    [SerializeField] private TMP_Text tutorialDescriptionText;

    [Header("Navigation Buttons")]
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;

    private int currentSlideIndex = 0;

    private void OnEnable()
    {
        if (prevButton != null)
        {
            prevButton.onClick.RemoveAllListeners();
            prevButton.onClick.AddListener(OnPrevClicked);
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextClicked);
        }

        // Reset slide ke index 0
        currentSlideIndex = 0;
        UpdateSlideUI();
    }

    private void OnDisable()
    {
        // Bersihkan listener saat panel ditutup
        if (prevButton != null) prevButton.onClick.RemoveAllListeners();
        if (nextButton != null) nextButton.onClick.RemoveAllListeners();
    }

    public void OnNextClicked()
    {
        if (currentSlideIndex < slides.Length - 1)
        {
            currentSlideIndex++;
            UpdateSlideUI();
        }
    }

    public void OnPrevClicked()
    {
        if (currentSlideIndex > 0)
        {
            currentSlideIndex--;
            UpdateSlideUI();
        }
    }

    private void UpdateSlideUI()
    {
        if (slides == null || slides.Length == 0) return;

        // 1. Update Konten Slide
        TutorialSlide current = slides[currentSlideIndex];
        if (tutorialDisplayImage != null && current.slideImage != null)
        {
            tutorialDisplayImage.sprite = current.slideImage;
        }

        if (tutorialDescriptionText != null)
        {
            tutorialDescriptionText.text = current.descriptionText;
        }

        // 2. Cek Kondisi Posisi Slide
        bool isFirstSlide = currentSlideIndex == 0;
        bool isLastSlide = currentSlideIndex == slides.Length - 1;

        // 3. Atur Visibilitas Tombol Navigasi
        if (prevButton != null)
        {
            prevButton.gameObject.SetActive(!isFirstSlide);
        }

        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(!isLastSlide);
        }
    }
}

[System.Serializable]
public class TutorialSlide
{
    [Tooltip("Gambar/ilustrasi tutorial untuk slide ini")]
    public Sprite slideImage;

    [TextArea(2, 5)]
    [Tooltip("Teks deskripsi tutorial (pengganti 'lorem ipsum dolor')")]
    public string descriptionText;
}