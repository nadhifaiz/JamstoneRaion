using System;
using UnityEngine;

// JANGAN ubah urutan/nilai enum ini: Unity menyimpannya sebagai angka di tiap aset
// (Beban=0, Keras=1, ...). Harga dasar per tag diatur di CraftingManager.tagPrices,
// ikon dan nama tampilan tag diatur di TagVisualsSO.
public enum MaterialType
{
    Beban,
    Keras,
    Tajam,
    Lentur,
    Pegangan
}

[CreateAssetMenu(fileName = "NewMaterial", menuName = "Game/Material Data")]
public class MaterialSO : ScriptableObject
{
    [Header("ID Save")]
    [Tooltip("Dibuat otomatis. JANGAN diubah setelah dipakai: file save mengacu ke ID ini.")]
    [SerializeField] private string id;
    public string Id => id;

    [Header("Data")]
    public string itemName;
    public Sprite itemSprite;
    public MaterialType materialType;
    public int tier = 1;

    [TextArea(2, 4)]
    [Tooltip("Teks deskripsi yang tampil di panel rincian")]
    public string description;

#if UNITY_EDITOR
    void OnValidate()
    {
        if (string.IsNullOrEmpty(id)) AssignNewId();
    }

    public void AssignNewId()
    {
        id = Guid.NewGuid().ToString("N");
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}