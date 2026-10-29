using System;
using System.Collections.Generic;
using UnityEngine;

// Kejadian harga di sebuah season: harga jual barang tertentu dikali pengali, lengkap dengan alasan cerita.
[Serializable]
public class SeasonPriceEvent
{
    public CraftedItemSO item;
    [Tooltip("1.5 = naik 50%, 0.8 = turun 20%")]
    [Min(0.1f)] public float multiplier = 1.5f;
    [TextArea(1, 3)] public string reason;
}

[CreateAssetMenu(fileName = "NewSeason", menuName = "Game/Season Data")]
public class SeasonSO : ScriptableObject
{
    public string displayName;
    [Min(1)] public int days = 7;

    [TextArea(2, 5)] public string storyBlurb;

    [Tooltip("Cutscene yang diputar saat season ini dimulai (kosongkan kalau tidak ada)")]
    public CutsceneSO introCutscene;

    public List<SeasonPriceEvent> priceEvents = new List<SeasonPriceEvent>();
}