using UnityEngine;
using UnityEngine.UI;

// Pratinjau isi etalase di scene (di luar panel drag-drop). Hanya menampilkan, tanpa interaksi.
//
// Tiap slot = satu GameObject berisi border + Image ikon sebagai CHILD.
// Drag GameObject slot ke slotObjects sesuai urutan slot etalase.
//   - Slot belum dibuka upgrade  -> GameObject slot dinonaktifkan (border ikut hilang).
//   - Slot dibuka tapi kosong    -> border tetap tampil, ikon dimatikan.
//   - Slot terisi                -> ikon diganti sprite barang.
// Otomatis ikut berubah saat barang ditaruh, ditarik, terjual, atau dikembalikan di akhir fase.
public class EtalaseShowcaseUI : MonoBehaviour
{
    public EtalaseManager etalase;

    [Tooltip("Urutan = urutan slot etalase. Siapkan sebanyak slot maksimum (termasuk hasil upgrade).")]
    public GameObject[] slotObjects;

    [Tooltip("Nama child yang berisi Image ikon. Kalau tidak ketemu, dipakai Image child pertama " +
             "(Image milik objek slot itu sendiri, misalnya border, tidak dihitung).")]
    public string iconChildName = "Icon";

    public bool hideLockedSlots = true;

    private Image[] icons;
    private bool warnedMissingSlots;

    private void Awake()
    {
        CacheIcons();
    }

    private void OnEnable()
    {
        if (icons == null)
            CacheIcons();

        if (etalase != null)
            etalase.OnChanged += Refresh;

        Refresh();
    }

    private void OnDisable()
    {
        if (etalase != null)
            etalase.OnChanged -= Refresh;
    }

    private void CacheIcons()
    {
        int count = slotObjects != null ? slotObjects.Length : 0;
        icons = new Image[count];

        for (int i = 0; i < count; i++)
        {
            if (slotObjects[i] == null)
                continue;

            icons[i] = FindIcon(slotObjects[i]);

            if (icons[i] == null)
            {
                Debug.LogWarning(
                    $"[EtalaseShowcaseUI] slotObjects[{i}] ({slotObjects[i].name}) tidak punya Image di child-nya.",
                    slotObjects[i]);
            }
        }
    }

    private Image FindIcon(GameObject slot)
    {
        // Sertakan child yang nonaktif; abaikan Image milik objek slot sendiri (border).
        Image[] images = slot.GetComponentsInChildren<Image>(true);
        Image firstChildImage = null;

        foreach (Image image in images)
        {
            if (image.gameObject == slot)
                continue;

            if (image.gameObject.name == iconChildName)
                return image;

            if (firstChildImage == null)
                firstChildImage = image;
        }

        return firstChildImage;
    }

    private void Refresh()
    {
        if (etalase == null || slotObjects == null)
            return;

        if (!warnedMissingSlots && etalase.SlotCount > slotObjects.Length)
        {
            warnedMissingSlots = true;
            Debug.LogWarning(
                $"[EtalaseShowcaseUI] Etalase punya {etalase.SlotCount} slot tapi hanya ada " +
                $"{slotObjects.Length} GameObject slot. Tambahkan slot untuk sisanya.", this);
        }

        for (int i = 0; i < slotObjects.Length; i++)
        {
            GameObject slot = slotObjects[i];

            if (slot == null)
                continue;

            bool unlocked = i < etalase.SlotCount;

            if (hideLockedSlots)
                slot.SetActive(unlocked);

            Image icon = i < icons.Length ? icons[i] : null;

            if (icon == null)
                continue;

            CraftedItemKey key = unlocked ? etalase.GetSlot(i) : default;
            bool hasItem = !key.IsEmpty && key.item != null;

            icon.sprite = hasItem ? key.item.itemSprite : null;
            icon.preserveAspect = true;
            icon.enabled = hasItem;
        }
    }
}