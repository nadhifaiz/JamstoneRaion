using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public enum AudioChannel { Master, Music, SFX }

/// <summary>
/// Pusat audio. Taruh di scene PERTAMA game (menu/boot). Bertahan antar scene.
/// Panggil dari script lain lewat helper static (aman walau AudioManager belum ada di scene):
///     AudioManager.Play(SoundId.HammerHit);
///     AudioManager.Music(MusicId.Selling);
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Referensi")]
    [SerializeField] SoundLibrary library;
    [SerializeField] AudioMixer mixer;
    [SerializeField] AudioMixerGroup musicGroup;
    [SerializeField] AudioMixerGroup sfxGroup;

    [Header("Pengaturan")]
    [SerializeField] int sfxPoolSize = 10;
    [SerializeField] float defaultMusicFade = 1f;

    // Nama parameter yang harus di-EXPOSE di Audio Mixer
    // (klik kanan Volume di Inspector grup -> Expose, lalu rename di tab Exposed Parameters).
    const string MasterParam = "MasterVolume";
    const string MusicParam = "MusicVolume";
    const string SfxParam = "SFXVolume";

    // Key PlayerPrefs diberi prefix unik supaya tidak bentrok dengan data lain.
    const string KeyMaster = "audio_settings_master";
    const string KeyMusic = "audio_settings_music";
    const string KeySfx = "audio_settings_sfx";

    AudioSource[] musicSources;
    AudioSource[] sfxSources;
    int activeMusic;
    int nextSfx;
    MusicId currentMusic = MusicId.None;
    Coroutine musicRoutine;

    // ---------- Lifecycle ----------

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // objek ini harus berada di root hierarchy

        musicSources = new AudioSource[2];
        for (int i = 0; i < 2; i++)
        {
            musicSources[i] = CreateSource("Music" + i, musicGroup);
            musicSources[i].loop = true;
        }

        sfxSources = new AudioSource[Mathf.Max(1, sfxPoolSize)];
        for (int i = 0; i < sfxSources.Length; i++)
            sfxSources[i] = CreateSource("SFX" + i, sfxGroup);
    }

    // Mixer.SetFloat kadang tidak bekerja di Awake, jadi diterapkan di Start.
    void Start() => ApplySavedVolumes();

    void OnApplicationPause(bool paused) { if (paused) PlayerPrefs.Save(); }
    void OnApplicationQuit() => PlayerPrefs.Save();

    AudioSource CreateSource(string objName, AudioMixerGroup group)
    {
        var go = new GameObject(objName);
        go.transform.SetParent(transform);
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.outputAudioMixerGroup = group;
        return src;
    }

    // ---------- Helper static (aman dipanggil kapan saja) ----------

    public static void Play(SoundId id)
    {
        if (Instance != null) Instance.PlaySFX(id);
    }

    public static void Music(MusicId id, float fadeTime = -1f)
    {
        if (Instance != null) Instance.PlayMusic(id, fadeTime);
    }

    // ---------- SFX ----------

    public void PlaySFX(SoundId id)
    {
        if (id == SoundId.None || library == null) return;
        if (!library.TryGetSfx(id, out var e) || e.clips == null || e.clips.Length == 0) return;

        var clip = e.clips[Random.Range(0, e.clips.Length)];
        if (clip == null) return;

        // Round-robin: kalau semua source sibuk, yang paling lama dipakai diambil alih.
        var src = sfxSources[nextSfx];
        nextSfx = (nextSfx + 1) % sfxSources.Length;

        src.pitch = 1f + Random.Range(-e.pitchVariation, e.pitchVariation);
        src.volume = e.volume;
        src.clip = clip;
        src.Play();
    }

    // ---------- Musik ----------

    public void PlayMusic(MusicId id, float fadeTime = -1f)
    {
        if (library == null || id == currentMusic) return;
        currentMusic = id;

        if (musicRoutine != null) StopCoroutine(musicRoutine);
        musicRoutine = StartCoroutine(CrossfadeMusic(id, fadeTime < 0f ? defaultMusicFade : fadeTime));
    }

    public void StopMusic(float fadeTime = -1f) => PlayMusic(MusicId.None, fadeTime);

    IEnumerator CrossfadeMusic(MusicId id, float time)
    {
        var from = musicSources[activeMusic];
        activeMusic = 1 - activeMusic;
        var to = musicSources[activeMusic];

        float targetVol = 0f;
        if (library.TryGetMusic(id, out var entry) && entry.clip != null)
        {
            to.clip = entry.clip;
            to.volume = 0f;
            to.Play();
            targetVol = entry.volume;
        }
        else
        {
            to.Stop();
        }

        float fromStart = from.volume;
        for (float t = 0f; t < time; t += Time.unscaledDeltaTime) // unscaled: tetap jalan saat game di-pause
        {
            float k = t / time;
            from.volume = Mathf.Lerp(fromStart, 0f, k);
            to.volume = Mathf.Lerp(0f, targetVol, k);
            yield return null;
        }

        from.volume = 0f;
        from.Stop();
        to.volume = targetVol;
        musicRoutine = null;
    }

    // ---------- Volume (0..1) ----------

    public float GetVolume(AudioChannel ch) => PlayerPrefs.GetFloat(KeyFor(ch), 1f);

    public void SetVolume(AudioChannel ch, float value01)
    {
        value01 = Mathf.Clamp01(value01);
        PlayerPrefs.SetFloat(KeyFor(ch), value01); // disimpan saat pause/quit
        ApplyToMixer(ch, value01);
    }

    void ApplySavedVolumes()
    {
        foreach (AudioChannel ch in System.Enum.GetValues(typeof(AudioChannel)))
            ApplyToMixer(ch, GetVolume(ch));
    }

    void ApplyToMixer(AudioChannel ch, float value01)
    {
        if (mixer == null) return;
        // Slider linear -> desibel. 0.0001 supaya tidak log10(0).
        float dB = Mathf.Log10(Mathf.Max(value01, 0.0001f)) * 20f;
        mixer.SetFloat(ParamFor(ch), dB);
    }

    static string KeyFor(AudioChannel ch) => ch switch
    {
        AudioChannel.Music => KeyMusic,
        AudioChannel.SFX => KeySfx,
        _ => KeyMaster,
    };

    static string ParamFor(AudioChannel ch) => ch switch
    {
        AudioChannel.Music => MusicParam,
        AudioChannel.SFX => SfxParam,
        _ => MasterParam,
    };
}