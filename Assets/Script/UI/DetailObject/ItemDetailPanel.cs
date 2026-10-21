using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Satu panel rincian untuk seluruh game, diletakkan "docked" (mis. di sisi layar) di Canvas paling depan,
// tanpa backdrop supaya drag/double-click pada item lain tetap berfungsi.
// Pasang script di objek yang selalu aktif; panelRoot = isi panel yang di-enable/disable.
public class ItemDetailPanel : MonoBehaviour
{
    public static ItemDetailPanel Instance { get; private set; }

    [Header("Data")]
    [SerializeField] private CraftingManager recipeManager; // untuk menghitung nilai bahan
    [SerializeField] private TagVisualsSO tagVisuals;

    public enum PanelMode { Docked, FollowItem }

    [Header("Posisi")]
    [Tooltip("Docked = posisi tetap sesuai desain di scene. FollowItem = muncul di samping item yang diklik.")]
    [SerializeField] private PanelMode mode = PanelMode.FollowItem;
    [Tooltip("Jarak panel dari item (satuan unit Canvas)")]
    [SerializeField] private float followMargin = 12f;

    [Header("Perilaku")]
    [Tooltip("Tekan di luar panel = tutup. Tidak memblokir input: drag dan double-click pada item lain tetap jalan.")]
    [SerializeField] private bool closeOnOutsidePress = true;

    [Header("UI")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI typeText;
    [SerializeField] private TextMeshProUGUI infoText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Image[] tagIcons;             // 2 Image: material pakai 1, barang jadi pakai 2

    private ItemDetailTrigger owner;
    private RectTransform panelRect;
    private RectTransform parentRect;
    private Camera panelCamera;

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

        panelRect = panelRoot.GetComponent<RectTransform>();
        parentRect = panelRect.parent as RectTransform;
        panelCamera = GetCameraFor(GetComponentInParent<Canvas>(true));

        // Mode FollowItem memposisikan titik tengah panel, jadi pivot dipaksa ke tengah
        if (mode == PanelMode.FollowItem) panelRect.pivot = new Vector2(0.5f, 0.5f);

        panelRoot.SetActive(false);
    }

    void Update()
    {
        if (!closeOnOutsidePress || owner == null) return;

        // Dicek saat tombol DITEKAN (bukan dilepas). Klik item membuka panel saat dilepas,
        // jadi tekanan yang sama tidak langsung menutup panel yang baru dibuka.
        if (!TryGetPressPosition(out Vector2 screenPosition)) return;

        if (!RectTransformUtility.RectangleContainsScreenPoint(panelRect, screenPosition, panelCamera))
        {
            Hide();
        }
    }

    private static bool TryGetPressPosition(out Vector2 position)
    {
        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            position = mouse.position.ReadValue();
            return true;
        }

        Touchscreen touch = Touchscreen.current;
        if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
        {
            position = touch.primaryTouch.position.ReadValue();
            return true;
        }

        position = default;
        return false;
    }

    public void Show(ItemDetailTrigger trigger)
    {
        ItemDetailData data = ItemDetailBuilder.Build(trigger, recipeManager, tagVisuals);
        if (data == null) return;

        owner = trigger;
        panelRoot.SetActive(true);

        iconImage.sprite = data.icon;
        titleText.text = data.title;
        typeText.text = data.typeLabel;
        infoText.text = string.Join("\n", data.infoLines);

        descriptionText.gameObject.SetActive(!string.IsNullOrEmpty(data.description));
        descriptionText.text = data.description;

        for (int i = 0; i < tagIcons.Length; i++)
        {
            bool hasTag = i < data.tags.Count;
            tagIcons[i].gameObject.SetActive(hasTag);
            if (hasTag) tagIcons[i].sprite = tagVisuals.GetIcon(data.tags[i]);
        }

        if (mode == PanelMode.FollowItem)
        {
            // Ukuran panel harus dihitung dulu (teks baru) sebelum posisinya ditentukan
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);
            PositionNextTo(trigger.transform as RectTransform);
        }
    }

    // Dihubungkan juga ke tombol tutup (X) di panel
    public void Hide()
    {
        owner = null;
        panelRoot.SetActive(false);
    }

    public void HideIfOwner(ItemDetailTrigger trigger)
    {
        if (owner == trigger) Hide();
    }

    // ==========================================
    // POSISI MODE FOLLOW ITEM
    // ==========================================

    // Prioritas: di kanan item, kalau tidak muat pindah ke kiri; lalu dijepit agar tetap di dalam layar.
    // Semua dihitung di ruang lokal parent panel, jadi berlaku untuk Overlay maupun Camera canvas.
    // Syarat: parent panel (mis. layer/Canvas) membentang penuh menutupi layar.
    private void PositionNextTo(RectTransform target)
    {
        if (target == null || parentRect == null) return;

        // Batas item dalam ruang lokal parent panel
        Camera targetCamera = GetCameraFor(target.GetComponentInParent<Canvas>());
        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);

        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);

        foreach (Vector3 corner in corners)
        {
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(targetCamera, corner);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPoint, panelCamera, out Vector2 local);
            min = Vector2.Min(min, local);
            max = Vector2.Max(max, local);
        }

        Vector2 size = panelRect.rect.size;
        Vector2 half = size * 0.5f;
        Rect bounds = parentRect.rect;

        float x = max.x + followMargin + half.x;                       // kanan item
        if (x + half.x > bounds.xMax) x = min.x - followMargin - half.x; // tidak muat: kiri item
        float y = max.y - half.y;                                       // sejajar sisi atas item

        x = Mathf.Clamp(x, bounds.xMin + half.x, bounds.xMax - half.x);
        y = Mathf.Clamp(y, bounds.yMin + half.y, bounds.yMax - half.y);

        panelRect.localPosition = new Vector3(x, y, panelRect.localPosition.z);
    }

    // Overlay tidak butuh kamera; Screen Space Camera / World Space memakai kamera canvas-nya
    private static Camera GetCameraFor(Canvas canvas)
    {
        if (canvas == null) return null;

        canvas = canvas.rootCanvas;
        return canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
    }
}