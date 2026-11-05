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

    [Header("Customer Sprite")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Sprite walkSprite;
    private Sprite buySprite;

    public CraftedItemSO WantedItem { get; private set; }

    private int movementVersion;

    private void Awake()
    {
        if (bubbleUI == null)
            bubbleUI = GetComponentInChildren<CustomerBubbleUI>(true);

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Setup(CraftedItemSO wanted)
    {
        WantedItem = wanted;
    }

    // ---------- Sprite ----------

    public void SetSprites(Sprite walk, Sprite buy)
    {
        walkSprite = walk;
        buySprite = buy;

        SetWalkingSprite();
    }

    public void SetWalkingSprite()
    {
        if (spriteRenderer != null && walkSprite != null)
            spriteRenderer.sprite = walkSprite;
    }

    public void SetBuyingSprite()
    {
        if (spriteRenderer != null && buySprite != null)
            spriteRenderer.sprite = buySprite;
    }

    // ---------- Movement ----------

    public IEnumerator MoveTo(Vector3 target)
    {
        int currentVersion = ++movementVersion;

        // Setiap kali bergerak, gunakan sprite berjalan.
        SetWalkingSprite();

        while ((transform.position - target).sqrMagnitude > 0.0001f)
        {
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