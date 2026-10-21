using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(SpriteRenderer))]
public class MaterialObject : MonoBehaviour
{
    [Header("Warna per Layer Tumpukan (0 = bawah, 2 = atas)")]
    [SerializeField]
    private Color[] layerTints =
    {
        new Color(0.4f, 0.4f, 0.4f),
        new Color(0.7f, 0.7f, 0.7f),
        Color.white
    };

    public MaterialSO MaterialData { get; private set; }
    public int LayerIndex { get; private set; }

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D body;
    private Collider2D bodyCollider;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        body = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
    }

    // Inisialisasi data material dan menempatkannya di layer tumpukan tertentu.
    public void Initialize(MaterialSO data, int assignedLayer)
    {
        MaterialData = data;
        LayerIndex = assignedLayer;

        spriteRenderer.sprite = data.itemSprite;
        spriteRenderer.sortingOrder = LayerIndex;
        spriteRenderer.color = layerTints[Mathf.Clamp(LayerIndex, 0, layerTints.Length - 1)];

        gameObject.layer = MaterialLayers.ToUnityLayer(LayerIndex);
    }

    // --- Dipegang / dilepas capit ---

    public void AttachTo(Transform anchor)
    {
        transform.SetParent(anchor);
        transform.localPosition = Vector3.zero;

        body.bodyType = RigidbodyType2D.Kinematic;
        body.linearVelocity = Vector2.zero;
        bodyCollider.enabled = false;
    }

    public void Detach()
    {
        transform.SetParent(null);

        body.bodyType = RigidbodyType2D.Dynamic;
        bodyCollider.enabled = true;
    }

    // --- Naik/turun layer tumpukan ---

    public void PromoteLayer()
    {
        if (LayerIndex >= MaterialLayers.MaxIndex) return;

        Initialize(MaterialData, LayerIndex + 1);
    }

    public void DemoteLayer()
    {
        if (transform.parent != null) return;

        if (LayerIndex == 0)
        {
            Destroy(gameObject);
        }
        else
        {
            Initialize(MaterialData, LayerIndex - 1);
            CrushUnderneath();
        }
    }

    // Memaksa material di bawahnya ikut turun layer setelah material ini diturunkan
    private void CrushUnderneath()
    {
        Collider2D[] hits = Physics2D.OverlapPointAll(transform.position);
        foreach (Collider2D hit in hits)
        {
            if (hit.TryGetComponent(out MaterialObject underMat) && underMat != this && underMat.LayerIndex == LayerIndex)
            {
                underMat.DemoteLayer();
            }
        }
    }
}