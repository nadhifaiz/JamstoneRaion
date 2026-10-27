#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

// Menu bantu untuk testing save di Editor: Game > Saves
public static class SaveEditorTools
{
    private static string SaveRoot => Path.Combine(Application.persistentDataPath, "saves");

    [MenuItem("Game/Saves/Buka Folder Save")]
    private static void OpenSaveFolder()
    {
        Directory.CreateDirectory(SaveRoot);
        EditorUtility.RevealInFinder(SaveRoot);
    }

    [MenuItem("Game/Saves/Hapus Semua Save...")]
    private static void DeleteAllSaves()
    {
        if (!Directory.Exists(SaveRoot))
        {
            EditorUtility.DisplayDialog("Hapus Semua Save", "Belum ada folder save.", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog("Hapus Semua Save",
                $"Semua slot save di:\n{SaveRoot}\nakan dihapus permanen. Lanjutkan?", "Hapus", "Batal"))
        {
            return;
        }

        Directory.Delete(SaveRoot, true);
        Debug.Log("[SaveEditorTools] Semua save dihapus.");
    }
}
#endif