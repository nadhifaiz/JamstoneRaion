using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Tempel di Button (atau Toggle/Slider) untuk memberi suara klik & hover
/// tanpa menyentuh script lain. Tombol yang non-interactable tidak berbunyi.
/// </summary>
[RequireComponent(typeof(Selectable))]
public class UIButtonSound : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    public SoundId clickSound = SoundId.UIClick;
    public SoundId hoverSound = SoundId.None; // None = tanpa suara hover

    Selectable selectable;

    void Awake() => selectable = GetComponent<Selectable>();

    bool CanPlay => selectable != null && selectable.IsInteractable();

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (CanPlay && hoverSound != SoundId.None) AudioManager.Play(hoverSound);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (CanPlay && clickSound != SoundId.None) AudioManager.Play(clickSound);
    }
}