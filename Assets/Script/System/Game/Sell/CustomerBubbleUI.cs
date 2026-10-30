using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Bubble chat di atas kepala pelanggan.
// Semua fungsi Show* memastikan bubble aktif, jadi urutan pemanggilan tidak jadi masalah.
public class CustomerBubbleUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject bubbleBackground;
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text messageText;

    [Header("Bubble Varians")]
    [SerializeField] private GameObject bubbleBuy;
    [SerializeField] private TMP_Text bubbleFeedBack;

    private void Awake()
    {
        Hide();
    }

    // price <= 0 berarti harga tidak ditampilkan (misalnya barang tidak ada di etalase).
    public void ShowRequest(Sprite icon, string itemName, int price)
    {
        string message = price > 0
            ? $"Aku mau membeli {itemName} seharga {price}!"
            : $"Aku mau membeli {itemName}!";

        Show("Request", icon, message);
    }

    public void ShowBought()
    {
        Show("Feedback", null, "Terima kasih!");
    }

    public void ShowLeftEmpty()
    {
        Show("Feedback", null, "Yah, barangnya tidak ada...");
    }

    public void Hide()
    {
        if (bubbleBackground != null)
            bubbleBackground.SetActive(false);
    }

    private void Show(string messageType, Sprite icon, string message)
    {
        if (bubbleBackground != null)
            bubbleBackground.SetActive(true);

        if (messageType == "Request")
        {
            if (bubbleBuy != null)
                bubbleBuy.SetActive(true);

            if (bubbleFeedBack != null)
                bubbleFeedBack.gameObject.SetActive(false);

            if (itemIcon != null)
            {
                itemIcon.sprite = icon;
            }

            if (messageText != null)
                messageText.text = message;
        }
        else if (messageType == "Feedback")
        {
            if (bubbleFeedBack != null)
                bubbleFeedBack.gameObject.SetActive(true);

            if (bubbleBuy != null)
                bubbleBuy.SetActive(false);

            if (bubbleFeedBack != null)
                bubbleFeedBack.text = message;
        }
    }
}