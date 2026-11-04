using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tempel di Slider (Min 0, Max 1) pada menu pengaturan. Pilih channel di Inspector.
/// </summary>
[RequireComponent(typeof(Slider))]
public class VolumeSlider : MonoBehaviour
{
    [SerializeField] AudioChannel channel = AudioChannel.Master;

    Slider slider;

    void Awake() => slider = GetComponent<Slider>();

    void OnEnable()
    {
        if (AudioManager.Instance != null)
            slider.SetValueWithoutNotify(AudioManager.Instance.GetVolume(channel));
        slider.onValueChanged.AddListener(OnChanged);
    }

    void OnDisable() => slider.onValueChanged.RemoveListener(OnChanged);

    void OnChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetVolume(channel, value);
    }
}