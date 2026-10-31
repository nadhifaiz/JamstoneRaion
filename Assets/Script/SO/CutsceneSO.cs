using System;
using System.Collections.Generic;
using UnityEngine;

// Satu halaman cutscene berisi kumpulan sprite panel yang ditampilkan berurutan.
[Serializable]
public class CutscenePage
{
    [Tooltip("Sprite panel dalam urutan kemunculan. Setiap elemen akan ditampilkan satu per satu.")]
    public List<Sprite> panels = new List<Sprite>();
}

// Data satu cutscene. Pemutar sungguhan (PanelSequenceCutscenePlayer) memakai 'pages';
// pemutar dummy memakai 'title' dan 'placeholderText'.
[CreateAssetMenu(fileName = "NewCutscene", menuName = "Game/Cutscene Data")]
public class CutsceneSO : ScriptableObject
{
    [Header("ID Save")]
    [Tooltip("Dibuat otomatis. Dipakai save untuk menandai cutscene yang sudah dilihat. Jangan menduplikasi ID antar aset.")]
    [SerializeField] private string id;
    public string Id => id;

    [Header("Halaman (pemutar komik)")]
    public List<CutscenePage> pages = new List<CutscenePage>();

    [Header("Pemutar dummy / cadangan")]
    public string title;

    [TextArea(3, 8)]
    [Tooltip("Teks sementara yang ditampilkan pemutar dummy kalau cutscene belum punya halaman")]
    public string placeholderText;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(id)) AssignNewId();
    }

    [ContextMenu("Buat ID Baru")]
    public void AssignNewId()
    {
        id = Guid.NewGuid().ToString("N");
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
