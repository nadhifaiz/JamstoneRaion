using System;
using System.Collections.Generic;
using UnityEngine;

// Satu tempat untuk ikon + nama tampilan tiap tag. Dipakai buku resep, badge tag di material,
// dan panel rincian, jadi ikon tag cukup diatur di sini. Buat lewat Create > Game > Tag Visuals.
[CreateAssetMenu(fileName = "TagVisuals", menuName = "Game/Tag Visuals")]
public class TagVisualsSO : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        public MaterialType tag;
        public string displayName;
        public Sprite icon;
    }

    [SerializeField] private List<Entry> entries = new List<Entry>();
    [Tooltip("Ikon '?' untuk resep yang belum ditemukan")]
    [SerializeField] private Sprite unknownIcon;

    public Sprite UnknownIcon => unknownIcon;

    public string GetName(MaterialType tag)
    {
        if (TryGet(tag, out Entry entry) && !string.IsNullOrEmpty(entry.displayName)) return entry.displayName;
        return tag.ToString();
    }

    public Sprite GetIcon(MaterialType tag)
    {
        return TryGet(tag, out Entry entry) ? entry.icon : null;
    }

    private bool TryGet(MaterialType tag, out Entry result)
    {
        foreach (Entry entry in entries)
        {
            if (entry.tag == tag)
            {
                result = entry;
                return true;
            }
        }
        result = default;
        return false;
    }
}