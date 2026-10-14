using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class CraneController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    private Vector2 moveInput;
    private Rigidbody2D rb;

    [Header("Grab Settings")]
    public Transform grabPoint;
    public float grabRadius = 0.5f;
    public LayerMask materialLayer;

    [Header("Juice (Efek Fisika)")]
    public float pushForce = 5f;
    public float pushRadius = 1.5f;

    [Header("Input Actions")]
    public InputActionReference moveAction;
    public InputActionReference grabAction;

    private MaterialObject grabbedMaterial = null;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void OnEnable()
    {
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
        rb.linearVelocity = moveInput * moveSpeed;
    }

    private void OnGrabStarted(InputAction.CallbackContext context)
    {
        if (grabbedMaterial == null)
        {
            TryGrab();
        }
    }

    private void OnGrabCanceled(InputAction.CallbackContext context)
    {
        if (grabbedMaterial != null)
        {
            Release();
        }
    }

    private void TryGrab()
    {
        // Ambil semua objek di radius capit
        Collider2D[] hits = Physics2D.OverlapCircleAll(grabPoint.position, grabRadius, materialLayer);

        MaterialObject bestMaterial = null;
        float closestDistance = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            MaterialObject mat = hit.GetComponent<MaterialObject>();
            if (mat == null) continue;

            // 1. Validasi: Cek apakah barang ini terekspos (berada paling atas di koordinat aslinya)
            bool isTopmost = true;
            Collider2D[] checks = Physics2D.OverlapPointAll(mat.transform.position, materialLayer);
            foreach (Collider2D checkHit in checks)
            {
                MaterialObject upperMat = checkHit.GetComponent<MaterialObject>();

                if (upperMat != null && upperMat != mat && upperMat.layerIndex > mat.layerIndex)
                {
                    isTopmost = false;
                    break;
                }
            }

            // 2. Jika dia terekspos (paling atas di tumpukannya), cek jaraknya ke capit
            if (isTopmost)
            {
                float dist = Vector2.Distance(grabPoint.position, mat.transform.position);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    bestMaterial = mat;
                }
            }
        }

        if (bestMaterial != null)
        {
            grabbedMaterial = bestMaterial;
            Vector2 originalPos = grabbedMaterial.transform.position;

            grabbedMaterial.transform.SetParent(grabPoint);
            grabbedMaterial.transform.localPosition = Vector3.zero;

            Rigidbody2D materialRb = grabbedMaterial.GetComponent<Rigidbody2D>();
            materialRb.bodyType = RigidbodyType2D.Kinematic;
            materialRb.linearVelocity = Vector2.zero;

            grabbedMaterial.GetComponent<Collider2D>().enabled = false;

            // --- 3. LOGIKA PROMOTE LAYER ---
            Collider2D[] underHits = Physics2D.OverlapPointAll(originalPos, materialLayer);
            foreach (Collider2D hit in underHits)
            {
                MaterialObject underMat = hit.GetComponent<MaterialObject>();
                if (underMat != null && underMat != grabbedMaterial)
                {
                    underMat.PromoteLayer();
                }
            }

            // --- 4. EFEK DORONG ---
            PushNearbyMaterials();
        }
    }

    private void PushNearbyMaterials()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(grabPoint.position, pushRadius, materialLayer);
        foreach (Collider2D col in colliders)
        {
            if (col.gameObject == grabbedMaterial.gameObject) continue;

            MaterialObject nearbyMat = col.GetComponent<MaterialObject>();

            if (nearbyMat != null && nearbyMat.layerIndex == grabbedMaterial.layerIndex)
            {
                Rigidbody2D nearbyRb = col.GetComponent<Rigidbody2D>();
                if (nearbyRb != null)
                {
                    Vector2 pushDirection = (col.transform.position - grabPoint.position).normalized;
                    nearbyRb.AddForce(pushDirection * pushForce, ForceMode2D.Impulse);
                }
            }
        }
    }

    private void Release()
    {
        // Dilepaskan
        grabbedMaterial.transform.SetParent(null);

        // Mengaktifkan collider
        Rigidbody2D materialRb = grabbedMaterial.GetComponent<Rigidbody2D>();
        materialRb.bodyType = RigidbodyType2D.Dynamic;

        grabbedMaterial.GetComponent<Collider2D>().enabled = true;

        // Memaksa barang dibawahnya turun layer jika ada barang.
        int layer2Mask = 1 << 8;
        Collider2D topHit = Physics2D.OverlapPoint(grabbedMaterial.transform.position, layer2Mask);

        if (topHit != null)
        {
            MaterialObject underneathMat = topHit.GetComponent<MaterialObject>();
            if (underneathMat != null)
            {
                underneathMat.DemoteLayer();
            }
        }

        grabbedMaterial.Initialize(grabbedMaterial.materialData, 2);


        grabbedMaterial = null;
    }

    private void OnDrawGizmosSelected()
    {
        if (grabPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(grabPoint.position, grabRadius);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(grabPoint.position, pushRadius);
        }
    }
}