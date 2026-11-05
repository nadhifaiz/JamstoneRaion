
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class CraneController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Movement Boundaries")]
    [SerializeField] private Collider2D topBoundary;
    [SerializeField] private Collider2D bottomBoundary;
    [SerializeField] private Collider2D leftBoundary;
    [SerializeField] private Collider2D rightBoundary;

    [Header("Grab Settings")]
    [SerializeField] private Transform grabPoint;
    [SerializeField] private float grabRadius = 0.5f;
    [SerializeField] private LayerMask materialLayer;

    [Header("Juice (Efek Fisika)")]
    [SerializeField] private float pushForce = 5f;
    [SerializeField] private float pushRadius = 1.5f;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference grabAction;

    [Header("UI Floating Text Settings")]
    [SerializeField] private GameObject floatingTextPrefab;
    [SerializeField] private Color floatingTextColor = Color.yellow;
    [SerializeField] private Transform canvasParent;

    [Header("Crane Sprites")]
    [SerializeField] private Sprite craneOpenSprite;
    [SerializeField] private Sprite craneClosedSprite;
    [SerializeField] private SpriteRenderer craneSpriteRenderer;

    private Rigidbody2D rb;
    private float speedMultiplier = 1f;
    private Vector2 moveInput;
    private MaterialObject grabbedMaterial;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void OnEnable()
    {
        speedMultiplier = UpgradeManager.Instance != null
            ? UpgradeManager.Instance.GetSpeedMultiplier()
            : 1f;

        moveAction.action.Enable();
        grabAction.action.Enable();

        grabAction.action.started += OnGrabStarted;
        grabAction.action.canceled += OnGrabCanceled;
    }

    void OnDisable()
    {
        moveAction.action.Disable();
        grabAction.action.Disable();

        grabAction.action.started -= OnGrabStarted;
        grabAction.action.canceled -= OnGrabCanceled;
    }

    void Start()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnMaterialChanged += ShowFloatingText;
    }

    void OnDestroy()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnMaterialChanged -= ShowFloatingText;
    }

    void Update()
    {
        moveInput = moveAction.action.ReadValue<Vector2>();
    }

    void FixedUpdate()
    {
        Vector2 velocity = moveInput * (moveSpeed * speedMultiplier);

        MoveWithinBoundaries(velocity);
    }

    // ==========================================
    // MOVEMENT BOUNDARIES
    // ==========================================

    private void MoveWithinBoundaries(Vector2 velocity)
    {
        // Jika boundary belum lengkap, movement tetap berjalan
        if (topBoundary == null ||
            bottomBoundary == null ||
            leftBoundary == null ||
            rightBoundary == null)
        {
            rb.linearVelocity = velocity;
            return;
        }

        // Batas bagian dalam dari masing-masing collider
        float minX = leftBoundary.bounds.max.x;
        float maxX = rightBoundary.bounds.min.x;
        float minY = bottomBoundary.bounds.max.y;
        float maxY = topBoundary.bounds.min.y;

        Vector2 currentPosition = rb.position;

        // Prediksi posisi crane pada physics step berikutnya
        Vector2 targetPosition = currentPosition +
                                 velocity * Time.fixedDeltaTime;

        // Batasi posisi pusat crane
        Vector2 clampedPosition = new Vector2(
            Mathf.Clamp(targetPosition.x, minX, maxX),
            Mathf.Clamp(targetPosition.y, minY, maxY)
        );

        bool blockedX = !Mathf.Approximately(
            targetPosition.x, clampedPosition.x
        );

        bool blockedY = !Mathf.Approximately(
            targetPosition.y, clampedPosition.y
        );

        // Hentikan velocity hanya pada sumbu yang terhalang
        rb.linearVelocity = new Vector2(
            blockedX ? 0f : velocity.x,
            blockedY ? 0f : velocity.y
        );

        // Koreksi posisi jika melewati batas
        if (blockedX || blockedY)
        {
            rb.position = new Vector2(
                blockedX ? clampedPosition.x : currentPosition.x,
                blockedY ? clampedPosition.y : currentPosition.y
            );
        }
    }

    private void OnGrabStarted(InputAction.CallbackContext context)
    {
        if (PauseManager.IsPaused) return;
        if (grabbedMaterial == null) TryGrab();
    }

    private void OnGrabCanceled(InputAction.CallbackContext context)
    {
        if (grabbedMaterial != null) Release();
    }

    // ==========================================
    // MENGAMBIL
    // ==========================================

    private void TryGrab()
    {
        MaterialObject target = FindBestMaterial();
        if (target == null) return;

        grabbedMaterial = target;
        Vector2 originalPos = target.transform.position;

        target.AttachTo(grabPoint);
        PromoteMaterialsUnder(originalPos);
        PushNearbyMaterials();

        // TAMBAHAN: Ubah sprite capit menjadi tertutup saat berhasil mencapit
        if (craneSpriteRenderer != null && craneClosedSprite != null)
        {
            craneSpriteRenderer.sprite = craneClosedSprite;
        }
    }

    // Material terdekat di radius capit yang terekspos
    // (paling atas di tumpukannya)
    private MaterialObject FindBestMaterial()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            grabPoint.position,
            grabRadius,
            materialLayer
        );

        MaterialObject best = null;
        float closestDistance = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            if (!hit.TryGetComponent(out MaterialObject mat)) continue;
            if (!IsTopmost(mat)) continue;

            float dist = Vector2.Distance(
                grabPoint.position,
                mat.transform.position
            );

            if (dist < closestDistance)
            {
                closestDistance = dist;
                best = mat;
            }
        }

        return best;
    }

    // Terekspos = tidak ada material di layer lebih tinggi
    // pada koordinat yang sama
    private bool IsTopmost(MaterialObject mat)
    {
        Collider2D[] checks = Physics2D.OverlapPointAll(
            mat.transform.position,
            materialLayer
        );

        foreach (Collider2D checkHit in checks)
        {
            if (checkHit.TryGetComponent(out MaterialObject upper) &&
                upper != mat &&
                upper.LayerIndex > mat.LayerIndex)
            {
                return false;
            }
        }

        return true;
    }

    // Material yang tadinya tertimpa naik satu layer.
    // Dipanggil SETELAH collider yang dipegang dimatikan.
    private void PromoteMaterialsUnder(Vector2 position)
    {
        Collider2D[] underHits = Physics2D.OverlapPointAll(
            position,
            materialLayer
        );

        foreach (Collider2D hit in underHits)
        {
            if (hit.TryGetComponent(out MaterialObject underMat) &&
                underMat != grabbedMaterial)
            {
                underMat.PromoteLayer();
            }
        }
    }

    private void PushNearbyMaterials()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(
            grabPoint.position,
            pushRadius,
            materialLayer
        );

        foreach (Collider2D col in colliders)
        {
            if (col.gameObject == grabbedMaterial.gameObject) continue;

            if (col.TryGetComponent(out MaterialObject nearbyMat) &&
                nearbyMat.LayerIndex == grabbedMaterial.LayerIndex &&
                col.TryGetComponent(out Rigidbody2D nearbyRb))
            {
                Vector2 pushDirection = (
                    col.transform.position - grabPoint.position
                ).normalized;

                nearbyRb.AddForce(
                    pushDirection * pushForce,
                    ForceMode2D.Impulse
                );
            }
        }
    }

    // ==========================================
    // MELEPAS
    // ==========================================

    private void Release()
    {
        MaterialObject released = grabbedMaterial;
        released.Detach();

        Collider2D topHit = Physics2D.OverlapPoint(
            released.transform.position,
            MaterialLayers.TopMask
        );

        if (topHit != null &&
            topHit.TryGetComponent(out MaterialObject underneath))
        {
            underneath.DemoteLayer();
        }

        released.Initialize(
            released.MaterialData,
            MaterialLayers.MaxIndex
        );

        grabbedMaterial = null;

        // TAMBAHAN: Kembalikan sprite capit menjadi terbuka setelah melepas barang
        if (craneSpriteRenderer != null && craneOpenSprite != null)
        {
            craneSpriteRenderer.sprite = craneOpenSprite;
        }
    }

    // ==========================================
    // GIZMOS
    // ==========================================

    private void OnDrawGizmosSelected()
    {
        if (grabPoint == null) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(grabPoint.position, grabRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(grabPoint.position, pushRadius);
    }

    // ==========================================
    // FEEDBACK
    // ==========================================
    public void ShowFloatingText(string message)
    {
        if (floatingTextPrefab == null || canvasParent == null)
            return;

        GameObject floatingTextObj = Instantiate(
            floatingTextPrefab,
            canvasParent.position,
            Quaternion.identity,
            canvasParent
        );

        Vector3 randomOffset = new Vector3(Random.Range(-30f, 30f), Random.Range(-30f, 30f), 0f);
        floatingTextObj.transform.position += randomOffset;

        FloatingText floatingText = floatingTextObj.GetComponent<FloatingText>();
        if (floatingText != null)
        {
            floatingText.SetText(message);
            floatingText.SetColor(floatingTextColor);
        }
    }
}
