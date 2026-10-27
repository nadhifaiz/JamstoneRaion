using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Satu baris di Scroll View buku resep. Pasang di root prefab baris.
// Tata letak: judul "1. Kapak" / "1. ???", lalu [ikon tag] + [ikon tag] = [ikon hasil].
// Tanda "+" dan "=" cukup berupa teks/gambar statis di prefab.
public class RecipeRowUI : MonoBehaviour
{
    [SerializeField] private TagVisualsSO tagVisuals;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Image tagIconA;
    [SerializeField] private Image tagIconB;
    [SerializeField] private Image resultIcon;

    public void Bind(int number, CraftedItemSO recipe, bool unlocked, bool showSilhouetteWhenLocked)
    {
        if (unlocked)
        {
            titleText.text = $"{number}. {recipe.itemName}";
            SetIcon(tagIconA, tagVisuals.GetIcon(recipe.requiredTag1), Color.white);
            SetIcon(tagIconB, tagVisuals.GetIcon(recipe.requiredTag2), Color.white);
            SetIcon(resultIcon, recipe.itemSprite, Color.white);
        }
        else
        {
            titleText.text = $"{number}. ???";
            SetIcon(tagIconA, tagVisuals.UnknownIcon, Color.white);
            SetIcon(tagIconB, tagVisuals.UnknownIcon, Color.white);

            // Siluet = sprite asli diwarnai hitam; kalau dimatikan, hasilnya diganti ikon "?"
            if (showSilhouetteWhenLocked) SetIcon(resultIcon, recipe.itemSprite, Color.black);
            else SetIcon(resultIcon, tagVisuals.UnknownIcon, Color.white);
        }
    }

    private static void SetIcon(Image image, Sprite sprite, Color color)
    {
        image.sprite = sprite;
        image.color = color;
        image.enabled = sprite != null;
    }
}