using System;
using System.Collections.Generic;
using UnityEngine;

// PENTING: kalau menambah ID baru, taruh di PALING BAWAH enum
// supaya nilai angka ID lama tidak bergeser dan data di Inspector tidak rusak.
public enum SoundId
{
    None = 0,

    // UI
    UIClick,
    UIHover,
    UIOpenPanel,
    UIClosePanel,
    UIError,

    // Crafting
    AnvilAppear,
    HammerHit,
    CraftComplete,
    CraftFail,

    // Jual
    CustomerWalk,
    BubbleShow,
    SellSuccess,
    SellFail,
}

public enum MusicId
{
    None = 0,
    MainMenu,
    InGame,
    CollectPhase,
    CraftingPhase,
    SellPhase,
}

[CreateAssetMenu(menuName = "Audio/Sound Library", fileName = "SoundLibrary")]
public class SoundLibrary : ScriptableObject
{
    [Serializable]
    public class SfxEntry
    {
        public SoundId id;
        [Tooltip("Isi lebih dari satu clip untuk variasi; dipilih acak tiap diputar.")]
        public AudioClip[] clips;
        [Range(0f, 1f)] public float volume = 1f;
        [Tooltip("Variasi pitch acak (+/-). 0.05-0.1 cocok untuk suara berulang seperti palu.")]
        [Range(0f, 0.5f)] public float pitchVariation = 0f;
    }

    [Serializable]
    public class MusicEntry
    {
        public MusicId id;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
    }

    public SfxEntry[] sfx;
    public MusicEntry[] music;

    Dictionary<SoundId, SfxEntry> sfxMap;
    Dictionary<MusicId, MusicEntry> musicMap;

    void OnEnable() => sfxMap = null; // paksa bangun ulang setelah edit/reload

    void BuildMaps()
    {
        sfxMap = new Dictionary<SoundId, SfxEntry>();
        if (sfx != null)
            foreach (var e in sfx)
                if (e != null && e.id != SoundId.None) sfxMap[e.id] = e;

        musicMap = new Dictionary<MusicId, MusicEntry>();
        if (music != null)
            foreach (var e in music)
                if (e != null && e.id != MusicId.None) musicMap[e.id] = e;
    }

    public bool TryGetSfx(SoundId id, out SfxEntry entry)
    {
        if (sfxMap == null) BuildMaps();
        return sfxMap.TryGetValue(id, out entry);
    }

    public bool TryGetMusic(MusicId id, out MusicEntry entry)
    {
        if (musicMap == null) BuildMaps();
        return musicMap.TryGetValue(id, out entry);
    }
}