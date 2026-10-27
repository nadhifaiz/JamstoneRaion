using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Baca/tulis file save di PC. Satu slot = satu folder: <persistentDataPath>/saves/<slotId>/
//   save.json       data lengkap
//   save.json.bak   cadangan save sebelumnya
//   meta.json       ringkasan untuk daftar di menu utama
// Penulisan save aman: ditulis ke file sementara lalu menggantikan file lama (File.Replace).
// Kalau save.json rusak, load otomatis memakai cadangannya.
public static class SaveStorage
{
    private const string SaveFileName = "save.json";
    private const string MetaFileName = "meta.json";

    private static string RootDirectory => Path.Combine(Application.persistentDataPath, "saves");

    public static string GetSlotDirectory(string slotId)
    {
        foreach (char invalid in Path.GetInvalidFileNameChars())
        {
            slotId = slotId.Replace(invalid, '_');
        }
        return Path.Combine(RootDirectory, slotId);
    }

    private static string GetSavePath(string slotId) => Path.Combine(GetSlotDirectory(slotId), SaveFileName);
    private static string GetMetaPath(string slotId) => Path.Combine(GetSlotDirectory(slotId), MetaFileName);

    // ==========================================
    // SLOT
    // ==========================================

    public static string CreateSlotId()
    {
        string id;
        do
        {
            id = Guid.NewGuid().ToString("N").Substring(0, 12);
        }
        while (Directory.Exists(GetSlotDirectory(id)));

        return id;
    }

    public static bool DeleteSlot(string slotId)
    {
        try
        {
            string dir = GetSlotDirectory(slotId);
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveStorage] Gagal menghapus slot '{slotId}': {e.Message}");
            return false;
        }
    }

    // Daftar semua slot, yang terakhir dimainkan di urutan pertama
    public static List<SaveSlotMeta> GetAllSlotMetas()
    {
        var result = new List<SaveSlotMeta>();
        if (!Directory.Exists(RootDirectory)) return result;

        foreach (string dir in Directory.GetDirectories(RootDirectory))
        {
            string slotId = Path.GetFileName(dir);

            if (TryReadMeta(slotId, out SaveSlotMeta meta))
            {
                result.Add(meta);
            }
            else if (TryRead(slotId, out SaveData data))
            {
                // meta hilang/rusak tapi save masih ada: bangun ulang dari save.json
                result.Add(BuildMetaFromSave(slotId, data));
            }
        }

        result.Sort((a, b) => b.lastPlayedUtcTicks.CompareTo(a.lastPlayedUtcTicks));
        return result;
    }

    private static SaveSlotMeta BuildMetaFromSave(string slotId, SaveData data)
    {
        string savePath = GetSavePath(slotId);
        long lastWrite = File.Exists(savePath) ? File.GetLastWriteTimeUtc(savePath).Ticks : DateTime.UtcNow.Ticks;

        return new SaveSlotMeta
        {
            id = slotId,
            slotName = "(tanpa nama)",
            currentDay = data.currentDay,
            amber = data.amber,
            isCompleted = false, // tidak bisa diketahui tanpa maxDays
            createdUtcTicks = lastWrite,
            lastPlayedUtcTicks = lastWrite
        };
    }

    // ==========================================
    // META
    // ==========================================

    public static bool WriteMeta(SaveSlotMeta meta)
    {
        try
        {
            Directory.CreateDirectory(GetSlotDirectory(meta.id));
            File.WriteAllText(GetMetaPath(meta.id), JsonUtility.ToJson(meta, true));
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveStorage] Gagal menulis meta slot '{meta.id}': {e.Message}");
            return false;
        }
    }

    public static bool TryReadMeta(string slotId, out SaveSlotMeta meta)
    {
        meta = null;
        string path = GetMetaPath(slotId);
        if (!File.Exists(path)) return false;

        try
        {
            meta = JsonUtility.FromJson<SaveSlotMeta>(File.ReadAllText(path));
            if (meta == null) return false;

            meta.id = slotId; // nama folder adalah sumber kebenaran
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveStorage] Gagal membaca meta slot '{slotId}': {e.Message}");
            meta = null;
            return false;
        }
    }

    // ==========================================
    // DATA SAVE
    // ==========================================

    public static bool Write(string slotId, SaveData data)
    {
        try
        {
            Directory.CreateDirectory(GetSlotDirectory(slotId));

            string path = GetSavePath(slotId);
            string tempPath = path + ".tmp";
            string backupPath = path + ".bak";

            File.WriteAllText(tempPath, JsonUtility.ToJson(data, true));

            if (File.Exists(path)) File.Replace(tempPath, path, backupPath);
            else File.Move(tempPath, path);

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveStorage] Gagal menyimpan slot '{slotId}': {e.Message}");
            return false;
        }
    }

    public static bool TryRead(string slotId, out SaveData data)
    {
        string path = GetSavePath(slotId);

        if (TryReadFile(path, out data)) return true;

        if (File.Exists(path))
        {
            Debug.LogWarning($"[SaveStorage] save.json slot '{slotId}' rusak, mencoba cadangan (.bak).");
        }

        if (TryReadFile(path + ".bak", out data)) return true;

        data = null;
        return false;
    }

    private static bool TryReadFile(string path, out SaveData data)
    {
        data = null;
        if (!File.Exists(path)) return false;

        try
        {
            data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
            return data != null;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveStorage] Gagal membaca '{path}': {e.Message}");
            data = null;
            return false;
        }
    }
}