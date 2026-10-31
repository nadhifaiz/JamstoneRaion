
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemDetailPanel : MonoBehaviour
{
    public static ItemDetailPanel Instance { get; private set; }

    [Header("Data")]
    [SerializeField] private CraftingManager recipeManager;
    [SerializeField] private TagVisualsSO tagVisuals;

    [Header("Panel References")]
    [SerializeField] private GameObject overlayRoot;
    [SerializeField] private Button backdropButton;
    [SerializeField] private RectTransform contentRoot;

    [Header("Position")]
    [SerializeField] private float followMargin = 12f;

    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI typeText;
    [SerializeField] private TextMeshProUGUI infoText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Image[] tagIcons;

    private ItemDetailTrigger owner;

    private RectTransform parentRect;
    private Camera panelCamera;

    private void Awake()
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

        parentRect = contentRoot.parent as RectTransform;

        panelCamera = GetCameraFor(
            GetComponentInParent<Canvas>(true)
        );

        contentRoot.pivot = new Vector2(0.5f, 0.5f);

        // Klik backdrop akan menutup panel.
        backdropButton.onClick.AddListener(Hide);

        overlayRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (backdropButton != null)
            backdropButton.onClick.RemoveListener(Hide);

        if (Instance == this)
            Instance = null;
    }

    public void Show(ItemDetailTrigger trigger)
    {
        if (trigger == null) return;

        ItemDetailData data = ItemDetailBuilder.Build(
            trigger,
            recipeManager,
            tagVisuals
        );

        if (data == null) return;

        owner = trigger;
        overlayRoot.SetActive(true);

        // Informasi utama
        iconImage.sprite = data.icon;
        titleText.text = data.title;
        typeText.text = data.typeLabel;
        infoText.text = string.Join("\n", data.infoLines);

        // Deskripsi
        descriptionText.gameObject.SetActive(
            !string.IsNullOrEmpty(data.description)
        );

        descriptionText.text = data.description;

        // Tag
        for (int i = 0; i < tagIcons.Length; i++)
        {
            bool hasTag = i < data.tags.Count;

            tagIcons[i].gameObject.SetActive(hasTag);

            if (hasTag)
            {
                tagIcons[i].sprite =
                    tagVisuals.GetIcon(data.tags[i]);
            }
        }

        // Hitung ukuran panel setelah konten diperbarui.
        Canvas.ForceUpdateCanvases();

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);

        // Posisikan panel mengikuti item.
        PositionNextTo(trigger.transform as RectTransform);
    }

    public void Hide()
    {
        owner = null;
        overlayRoot.SetActive(false);
    }

    public void HideIfOwner(ItemDetailTrigger trigger)
    {
        if (owner == trigger)
            Hide();
    }

    // ==========================================
    // FOLLOW ITEM POSITION
    // ==========================================

    private void PositionNextTo(RectTransform target)
    {
        if (target == null || parentRect == null) return;

        Camera targetCamera = GetCameraFor(
            target.GetComponentInParent<Canvas>()
        );

        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);

        Vector2 min = new Vector2(
            float.MaxValue,
            float.MaxValue
        );

        Vector2 max = new Vector2(
            float.MinValue,
            float.MinValue
        );

        foreach (Vector3 corner in corners)
        {
            Vector2 screenPoint =
                RectTransformUtility.WorldToScreenPoint(
                    targetCamera,
                    corner
                );

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                screenPoint,
                panelCamera,
                out Vector2 local
            );

            min = Vector2.Min(min, local);
            max = Vector2.Max(max, local);
        }

        Vector2 size = contentRoot.rect.size;
        Vector2 half = size * 0.5f;

        Rect bounds = parentRect.rect;

        // Prioritas posisi kanan.
        float x = max.x + followMargin + half.x;

        // Jika tidak cukup, pindah ke kiri.
        if (x + half.x > bounds.xMax)
        {
            x = min.x - followMargin - half.x;
        }

        // Sejajarkan bagian atas panel dengan item.
        float y = max.y - half.y;

        // Pastikan panel tidak keluar layar.
        x = Mathf.Clamp(
            x,
            bounds.xMin + half.x,
            bounds.xMax - half.x
        );

        y = Mathf.Clamp(
            y,
            bounds.yMin + half.y,
            bounds.yMax - half.y
        );

        contentRoot.localPosition = new Vector3(
            x,
            y,
            contentRoot.localPosition.z
        );
    }

    private static Camera GetCameraFor(Canvas canvas)
    {
        if (canvas == null) return null;

        canvas = canvas.rootCanvas;

        return canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : canvas.worldCamera;
    }
}
