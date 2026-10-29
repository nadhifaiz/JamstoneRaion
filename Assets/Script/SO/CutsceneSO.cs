using System;
using UnityEngine;

// Data satu cutscene. Saat ini hanya judul + teks sementara untuk pemutar dummy.
// Nanti field untuk cutscene sungguhan (slide gambar, video, dll.) cukup ditambahkan di sini,
// tanpa mengubah pemicu, penyimpanan, atau alur game.
[CreateAssetMenu(fileName = "NewCutscene", menuName = "Game/Cutscene Data")]
public class CutsceneSO : ScriptableObject
{
    [Header("ID Save")]
    [Tooltip("Dibuat otomatis. Dipakai save untuk menandai cutscene yang sudah dilihat. " +
             "Jangan menduplikasi aset dengan Ctrl+D; kalau terlanjur, klik kanan > Buat ID Baru.")]
    [SerializeField] private string id;
    public string Id => id;

    [Header("Data")]
    public string title;

    [TextArea(3, 8)]
    [Tooltip("Teks sementara yang ditampilkan pemutar dummy")]
    public string placeholderText;

#if UNITY_EDITOR
    void OnValidate()
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