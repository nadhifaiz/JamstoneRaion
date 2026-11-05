using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadingScreenUI : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [Tooltip("Image dengan Image Type = Filled, Fill Method = Horizontal")]
    [SerializeField] private Image progressFill;
    [SerializeField] private TextMeshProUGUI tipText;
    [SerializeField] private TextMeshProUGUI percentText;   // opsional

    [TextArea(2, 4)]
    [SerializeField]
    private string[] tips =
    {
        "Resep yang salah membuat kedua bahan hangus. Buku Resep akan membantumu!",
        "Klik dua kali pada material untuk langsung memasangnya di meja rakit.",
        "Saat kamu keluar ditengah fase berjalan, maka fase itu akan dianggap selesai. Jadi pastikan kamu sudah siap sebelum memulai fase.",
        "Harga barang bisa naik di season tertentu. Cek Kabar Pasar di Hub.",
        "Barang yang belum laku dikembalikan ke inventory saat fase Sell berakhir.",
        "Upgrade baterai membuat waktu Collect lebih lama."
    };

    private int lastTipIndex = -1;

    public void Show()
    {
        panelRoot.SetActive(true);
        SetProgress(0f);
        tipText.text = PickTip();
    }

    public void Hide()
    {
        panelRoot.SetActive(false);
    }

    public void SetProgress(float value)
    {
        value = Mathf.Clamp01(value);
        progressFill.fillAmount = value;
        if (percentText != null) percentText.text = $"{Mathf.RoundToInt(value * 100f)}%";
    }

    private string PickTip()
    {
        if (tips == null || tips.Length == 0) return "";
        if (tips.Length == 1) return tips[0];

        int index;
        do
        {
            index = Random.Range(0, tips.Length);
        }
        while (index == lastTipIndex); // jangan tampilkan tips yang sama dua kali berturut-turut

        lastTipIndex = index;
        return tips[index];
    }
}