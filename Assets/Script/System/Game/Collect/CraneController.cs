using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class CraneController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;

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
        // Crane diaktifkan tiap fase Collect dimulai, jadi upgrade dibaca di sini
        speedMultiplier = UpgradeManager.Instance != null ? UpgradeManager.Instance.GetSpeedMultiplier() : 1f;

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

    void Update()
    {
        moveInput = moveAction.action.ReadValue<Vector2>();
    }

    void FixedUpdate()
    {
        rb.linearVelocity = moveInput * (moveSpeed * speedMultiplier);
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
    }

    // Material terdekat di radius capit yang terekspos (paling atas di tumpukannya)
    private MaterialObject FindBestMaterial()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(grabPoint.position, grabRadius, materialLayer);

        MaterialObject best = null;
        float closestDistance = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            if (!hit.TryGetComponent(out MaterialObject mat)) continue;
            if (!IsTopmost(mat)) continue;

            float dist = Vector2.Distance(grabPoint.position, mat.transform.position);
            if (dist < closestDistance)
            {
                closestDistance = dist;
                best = mat;
            }
        }

        return best;
    }

    // Terekspos = tidak ada material di layer lebih tinggi pada koordinat yang sama
    private bool IsTopmost(MaterialObject mat)
    {
        Collider2D[] checks = Physics2D.OverlapPointAll(mat.transform.position, materialLayer);
        foreach (Collider2D checkHit in checks)
        {
            if (checkHit.TryGetComponent(out MaterialObject upper) && upper != mat && upper.LayerIndex > mat.LayerIndex)
            {
                return false;
            }
        }
        return true;
    }

    // Material yang tadinya tertimpa naik satu layer. Dipanggil SETELAH collider yang dipegang dimatikan.
    private void PromoteMaterialsUnder(Vector2 position)
    {
        Collider2D[] underHits = Physics2D.OverlapPointAll(position, materialLayer);
        foreach (Collider2D hit in underHits)
        {
            if (hit.TryGetComponent(out MaterialObject underMat) && underMat != grabbedMaterial)
            {
                underMat.PromoteLayer();
            }
        }
    }

    private void PushNearbyMaterials()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(grabPoint.position, pushRadius, materialLayer);
        foreach (Collider2D col in colliders)
        {
            if (col.gameObject == grabbedMaterial.gameObject) continue;

            if (col.TryGetComponent(out MaterialObject nearbyMat) &&
                nearbyMat.LayerIndex == grabbedMaterial.LayerIndex &&
                col.TryGetComponent(out Rigidbody2D nearbyRb))
            {
                Vector2 pushDirection = (col.transform.position - grabPoint.position).normalized;
                nearbyRb.AddForce(pushDirection * pushForce, ForceMode2D.Impulse);
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

        // Material yang dilepas selalu menempati layer teratas, jadi material di layer teratas
        // pada titik yang sama dipaksa turun dulu.
        // Catatan: collider sudah aktif lagi di sini, jadi hasilnya bisa mengenai material itu
        // sendiri kalau layernya memang sudah teratas (perilaku asli dipertahankan).
        Collider2D topHit = Physics2D.OverlapPoint(released.transform.position, MaterialLayers.TopMask);
        if (topHit != null && topHit.TryGetComponent(out MaterialObject underneath))
        {
            underneath.DemoteLayer();
        }

        released.Initialize(released.MaterialData, MaterialLayers.MaxIndex);
        grabbedMaterial = null;
    }

    private void OnDrawGizmosSelected()
    {
        if (grabPoint == null) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(grabPoint.position, grabRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(grabPoint.position, pushRadius);
    }
}