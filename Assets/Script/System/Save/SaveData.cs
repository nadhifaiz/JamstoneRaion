using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public const int CurrentVersion = 1;

    public int version = CurrentVersion;

    // Progres hari
    public int currentDay = 1;
    public bool hasCollectedToday;
    public bool hasCraftedToday;
    public bool hasSoldToday;

    // Menandai kalau fase sudah dilakukan
    public bool collectInProgress;
    public bool craftInProgress;
    public bool sellInProgress;

    // Ekonomi & inventory
    public int amber;
    public List<MaterialEntry> materials = new List<MaterialEntry>();
    public List<CraftedEntry> craftedItems = new List<CraftedEntry>();

    // Buku resep
    public List<string> discoveredRecipes = new List<string>();

    // Upgrade yang sudah dibeli (hanya level di atas 0)
    public List<UpgradeEntry> upgrades = new List<UpgradeEntry>();

    // Cerita: cutscene yang sudah dilihat dan status ending (setelah ending, game berjalan endless)
    public List<string> seenCutscenes = new List<string>();
    public bool endingSeen;
}

[Serializable]
public class UpgradeEntry
{
    public string id;
    public int level;
}

[Serializable]
public class MaterialEntry
{
    public string id;
    public int count;
}

[Serializable]
public class CraftedEntry
{
    public string id;
    public int price;   // harga jual per unit
    public int count;
}