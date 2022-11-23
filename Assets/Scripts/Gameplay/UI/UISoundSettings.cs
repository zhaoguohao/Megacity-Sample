using Unity.MegaCity.Audio;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Modifies the AudioMixer Groups via AudioMaster
/// Reads the data to set the Sliders in the AudioView
/// </summary>
public class UISoundSettings : UISettingsTab
{
    private Slider m_VolumeSlider;
    private Slider m_SoundFXSlider;

    public override string TabName => "audio";


    protected override void Initialization()
    {
        var root = GameSettingsView.Q<GroupBox>().Q<VisualElement>("sliders");
        m_VolumeSlider = root.Q<Slider>("volume");
        m_SoundFXSlider = root.Q<Slider>("sound-fx");
        m_SoundFXSlider.RegisterValueChangedCallback(OnSoundFXUpdated);
        m_VolumeSlider.RegisterValueChangedCallback(OnVolumeUpdated);
        base.Initialization();
    }

    protected override void SaveCurrentState()
    {
        base.SaveCurrentState();
        UpdateSliderCurrentState(m_VolumeSlider);
        UpdateSliderCurrentState(m_SoundFXSlider);
    }

    private void OnDestroy()
    {
        if (IsSet)
        {
            m_SoundFXSlider.UnregisterValueChangedCallback(OnSoundFXUpdated);
            m_VolumeSlider.UnregisterValueChangedCallback(OnVolumeUpdated);
        }
    }

    private void OnVolumeUpdated(ChangeEvent<float> value)
    {
        AudioMaster.Instance.volume.audioMixer.SetFloat("volume",Mathf.Log(value.newValue) * 20);
    }

    private void OnSoundFXUpdated(ChangeEvent<float> value)
    {
        AudioMaster.Instance.volume.audioMixer.SetFloat("sound-fx",Mathf.Log(value.newValue) * 20);
    }

    public override void Reset()
    {
        base.Reset();
        ResetSliderCurrentState(m_VolumeSlider);
        ResetSliderCurrentState(m_SoundFXSlider);
    }
}
