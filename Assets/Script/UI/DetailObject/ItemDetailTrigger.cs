
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class ItemDetailTrigger : MonoBehaviour, IPointerClickHandler
{
    public enum SourceKind
    {
        None,
        Material,
        CraftedItem
    }

    public enum ClickMode
    {
        Standalone,
        External
    }

    [Header("Click Settings")]
    [SerializeField] private ClickMode clickMode = ClickMode.Standalone;
    [SerializeField, Min(0.05f)] private float doubleClickInterval = 0.35f;

    public SourceKind Kind { get; private set; }
    public MaterialSO Material { get; private set; }
    public CraftedItemKey CraftedItem { get; private set; }
    public bool ShowStock { get; private set; }

    private Coroutine pendingClick;

    public void SetMaterial(MaterialSO material, bool showStock)
    {
        Kind = material != null ? SourceKind.Material : SourceKind.None;
        Material = material;
        CraftedItem = default;
        ShowStock = showStock;
    }

    public void SetCraftedItem(CraftedItemKey key, bool showStock)
    {
        Kind = key.IsEmpty ? SourceKind.None : SourceKind.CraftedItem;
        CraftedItem = key;
        Material = null;
        ShowStock = showStock;
    }

    public void Clear()
    {
        CancelPendingClick();

        Kind = SourceKind.None;
        Material = null;
        CraftedItem = default;

        HidePanelIfOwner();
    }

    // Digunakan otomatis pada mode Standalone.
    public void OnPointerClick(PointerEventData eventData)
    {
        if (clickMode != ClickMode.Standalone) return;

        HandlePointerClick(eventData);
    }

    // Digunakan script eksternal untuk mengelola klik.
    public void HandlePointerClick(
        PointerEventData eventData,
        Action onDoubleClick = null)
    {
        if (Kind == SourceKind.None) return;
        if (eventData.dragging) return;

        // Double click
        if (eventData.clickCount >= 2)
        {
            CancelPendingClick();

            onDoubleClick?.Invoke();
            return;
        }

        // Single click
        if (eventData.clickCount != 1) return;

        CancelPendingClick();
        pendingClick = StartCoroutine(WaitForSingleClick());
    }

    private IEnumerator WaitForSingleClick()
    {
        yield return new WaitForSecondsRealtime(doubleClickInterval);

        pendingClick = null;

        if (Kind != SourceKind.None &&
            ItemDetailPanel.Instance != null)
        {
            ItemDetailPanel.Instance.Show(this);
        }
    }

    private void CancelPendingClick()
    {
        if (pendingClick == null) return;

        StopCoroutine(pendingClick);
        pendingClick = null;
    }

    private void OnDisable()
    {
        CancelPendingClick();
        HidePanelIfOwner();
    }

    private void HidePanelIfOwner()
    {
        if (ItemDetailPanel.Instance != null)
            ItemDetailPanel.Instance.HideIfOwner(this);
    }
}
