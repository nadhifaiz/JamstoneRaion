using UnityEngine;

public class SceneMusic : MonoBehaviour
{
    [SerializeField] MusicId music = MusicId.MainMenu;
    [SerializeField] float fadeTime = -1f;

    void Start() => AudioManager.Music(music, fadeTime);
}