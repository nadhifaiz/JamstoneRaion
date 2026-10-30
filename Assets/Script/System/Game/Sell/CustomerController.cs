using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class CustomerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3f;

    [Header("Events")]
    public UnityEvent onBought;
    public UnityEvent onLeftEmpty;

    [Header("Bubble UI")]
    [SerializeField] private CustomerBubbleUI bubbleUI;

    // Barang yang diinginkan pelanggan ini (diisi SellManager saat spawn).
    public CraftedItemSO WantedItem { get; private set; }

    private int movementVersion;

    private void Awake()
    {
        // Cadangan kalau lupa di-assign di prefab.
        if (bubbleUI == null)
            bubbleUI = GetComponentInChildren<CustomerBubbleUI>(true);

        // Bubble menyembunyikan dirinya sendiri di Awake-nya.
    }

    public void Setup(CraftedItemSO wanted)
    {
        WantedItem = wanted;
    }

    // ---------- Gerak ----------

    // Dipakai dengan "yield return" kalau pemanggil perlu menunggu sampai tiba.
    public IEnumerator MoveTo(Vector3 target)
    {
        int currentVersion = ++movementVersion;

        while ((transform.position - target).sqrMagnitude > 0.0001f)
        {
            // Ada perintah gerak yang lebih baru: batalkan yang ini.
            if (currentVersion != movementVersion)
                yield break;

            transform.position = Vector3.MoveTowards(
                transform.position,
                target,
                moveSpeed * Time.deltaTime
            );

            yield return null;
        }

        if (currentVersion == movementVersion)
            transform.position = target;
    }

    // Jalan tanpa ditunggu. Coroutine-nya jalan di pelanggan ini sendiri,
    // jadi otomatis berhenti kalau pelanggan di-Destroy.
    public void GoTo(Vector3 target)
    {
        StartCoroutine(MoveTo(target));
    }

    // ---------- Bubble ----------

    public void ShowRequest(Sprite icon, string itemName, int price)
    {
        if (bubbleUI != null)
            bubbleUI.ShowRequest(icon, itemName, price);
    }

    public void NotifyBought()
    {
        if (bubbleUI != null)
            bubbleUI.ShowBought();

        onBought?.Invoke();
    }

    public void NotifyLeftEmpty()
    {
        if (bubbleUI != null)
            bubbleUI.ShowLeftEmpty();

        onLeftEmpty?.Invoke();
    }

    public void HideBubble()
    {
        if (bubbleUI != null)
            bubbleUI.Hide();
    }
}