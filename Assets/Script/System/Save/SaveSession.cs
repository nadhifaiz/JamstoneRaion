using System;

// Menyimpan slot yang sedang dimainkan, lintas scene (menu utama -> scene game).
// Menu utama memanggil StartNewGame() atau SelectSlot(), lalu memuat scene game.
// Kalau scene game dijalankan langsung dari Editor tanpa menu, HasSlot = false
// dan SaveManager memakai slot debug.
public static class SaveSession
{
    public static string CurrentSlotId { get; private set; }

    public static bool HasSlot => !string.IsNullOrEmpty(CurrentSlotId);

    // Memilih slot yang sudah ada (tombol Continue/Load)
    public static void SelectSlot(string slotId)
    {
        CurrentSlotId = slotId;
    }

    // Membuat slot baru (tombol New Game) dan langsung memilihnya. Mengembalikan ID slot baru.
    // save.json baru ditulis saat game berjalan dan ada perubahan pertama.
    public static string StartNewGame(string slotName)
    {
        if (string.IsNullOrWhiteSpace(slotName)) slotName = "Game Baru";

        long now = DateTime.UtcNow.Ticks;
        var meta = new SaveSlotMeta
        {
            id = SaveStorage.CreateSlotId(),
            slotName = slotName.Trim(),
            createdUtcTicks = now,
            lastPlayedUtcTicks = now
        };

        SaveStorage.WriteMeta(meta);
        CurrentSlotId = meta.id;
        return meta.id;
    }

    public static void Clear()
    {
        CurrentSlotId = null;
    }
}