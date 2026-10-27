using System;

// Ringkasan ringan satu slot save, disimpan di meta.json supaya menu utama bisa menampilkan
// daftar save tanpa memuat data lengkap. JsonUtility tidak bisa menyimpan DateTime, jadi memakai ticks UTC.
[Serializable]
public class SaveSlotMeta
{
    public string id;
    public string slotName;
    public int currentDay = 1;
    public int amber;
    public bool isCompleted;     // tamat: hari terakhir sudah selesai
    public long createdUtcTicks;
    public long lastPlayedUtcTicks;

    public DateTime LastPlayedLocal => new DateTime(lastPlayedUtcTicks, DateTimeKind.Utc).ToLocalTime();
    public DateTime CreatedLocal => new DateTime(createdUtcTicks, DateTimeKind.Utc).ToLocalTime();
}