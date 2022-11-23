using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UIElements;

/// <summary>
/// Access to GameObjects in the Scene and graphics settings that allows change the quality of the game.
/// Uses the Graphic Settings View to modify by Toggle and DropdownField that should be Modify in the UI,
/// These controls modify:
///  - Postprocessing
///  - Quality Settings
///  - Texture Detail
///  - Shadow Quality
///  - Level of Detail
///  - Motion Blur
///  - Reflections
///  - Fog
///  - VSync
///  - ScreenMode
/// </summary>
public class UIGraphicsSettings : UISettingsTab
{
    [SerializeField]
    private Volume m_PostProcessing;
    [SerializeField]
    private GameObject FogVolume;
    [SerializeField]
    private GameObject ReflectionVolume;

    private Toggle m_PostprocesingValue;
    private Toggle m_VolumetricFogValue;
    private Toggle m_ReflectionsValue;
    private Toggle m_MotionBlurValue;
    private Toggle m_VerticalSyncValue;

    private DropdownField m_QualityValue;
    private DropdownField m_ScreenmodeValue;
    private DropdownField m_TextureDetailsValue;
    private DropdownField m_ShadowQualityValue;
    private DropdownField m_LevelOfDetailValue;

    private MotionBlur m_MotionBlur;
    private VisualElement m_MainRoot;

    public override string TabName => "graphics";
    private bool m_CanSetAsCustom = true;

    protected override void Initialization()
    {
        m_MainRoot = GetComponent<UIDocument>().rootVisualElement.parent;
        var root = GameSettingsView.Q<GroupBox>().Q<VisualElement>("advance");
        m_PostprocesingValue = root.Q<GroupBox>().Q<Toggle>("postprocessing");
        m_VolumetricFogValue = root.Q<GroupBox>().Q<Toggle>("volumetric-fog");
        m_ReflectionsValue = root.Q<GroupBox>().Q<Toggle>("reflections");
        m_MotionBlurValue = root.Q<GroupBox>().Q<Toggle>("motion-blur");
        m_VerticalSyncValue = root.Q<GroupBox>().Q<Toggle>("vertical-sync");

        m_QualityValue = root.Q<DropdownField>("quality-settings");
        m_ScreenmodeValue = root.Q<DropdownField>("screen-mode");
        m_TextureDetailsValue = root.Q<DropdownField>("texture-details");
        m_ShadowQualityValue = root.Q<DropdownField>("shadow-quality");
        m_LevelOfDetailValue = root.Q<DropdownField>("level-of-detail");

        m_PostprocesingValue.RegisterValueChangedCallback(OnPostprocessingChanged);
        m_VolumetricFogValue.RegisterValueChangedCallback(OnVolumetricFogChanged);
        m_ReflectionsValue.RegisterValueChangedCallback(OnReflectionsChanged);
        m_MotionBlurValue.RegisterValueChangedCallback(OnMotionBlurChanged);
        m_VerticalSyncValue.RegisterValueChangedCallback(OnVsyncChanged);

        m_QualityValue.RegisterValueChangedCallback(OnGraphicsQualityChanged);
        m_ScreenmodeValue.RegisterValueChangedCallback(OnScreenmodeChanged);
        m_TextureDetailsValue.RegisterValueChangedCallback(OnTextureDetailsChanged);
        m_ShadowQualityValue.RegisterValueChangedCallback(OnShadowQualityChanged);
        m_LevelOfDetailValue.RegisterValueChangedCallback(LevelOfDetailChanged);
        m_PostProcessing.profile.TryGet(out m_MotionBlur);

        switch (QualitySettings.GetQualityLevel())
        {
            case 0:
                m_QualityValue.value = m_QualityValue.choices[2];
                OnHighButtonOnclicked();
                break;
            case 1:
                m_QualityValue.value = m_QualityValue.choices[1];
                OnMediumButtonOnclicked();
                break;
            case 2:
                m_QualityValue.value = m_QualityValue.choices[0];
                OnLowButtonOnclicked();
                break;
        }

        if (Screen.fullScreenMode == FullScreenMode.Windowed && !Screen.fullScreen)
        {
            m_ScreenmodeValue.value = FullScreenMode.Windowed.ToString();
        }

        base.Initialization();
    }

    protected override void SaveCurrentState()
    {
        UpdateCurrentToggleState(m_PostprocesingValue);
        UpdateCurrentToggleState(m_VolumetricFogValue);
        UpdateCurrentToggleState(m_ReflectionsValue);
        UpdateCurrentToggleState(m_MotionBlurValue);
        UpdateCurrentToggleState(m_VerticalSyncValue);

        UpdateCurrentDropdownFieldState(m_ScreenmodeValue);
        UpdateCurrentDropdownFieldState(m_TextureDetailsValue);
        UpdateCurrentDropdownFieldState(m_ShadowQualityValue);
        UpdateCurrentDropdownFieldState(m_LevelOfDetailValue);

        base.SaveCurrentState();
    }

    public override void Reset()
    {
        base.Reset();
        ResetCurrentToggleState(m_PostprocesingValue);
        ResetCurrentToggleState(m_VolumetricFogValue);
        ResetCurrentToggleState(m_ReflectionsValue);
        ResetCurrentToggleState(m_MotionBlurValue);
        ResetCurrentToggleState(m_VerticalSyncValue);

        ResetCurrentDropdownFieldState(m_ScreenmodeValue);
        ResetCurrentDropdownFieldState(m_TextureDetailsValue);
        ResetCurrentDropdownFieldState(m_ShadowQualityValue);
        ResetCurrentDropdownFieldState(m_LevelOfDetailValue);
    }

    private void OnHighButtonOnclicked()
    {
        m_CanSetAsCustom = false;
        QualitySettings.SetQualityLevel(0);
        m_VerticalSyncValue.value = true;
        m_MotionBlurValue.value = true;
        m_VolumetricFogValue.value = true;
        m_PostprocesingValue.value = true;
        m_ReflectionsValue.value = true;

        var detail = "High";
        m_ShadowQualityValue.value = detail;
        m_TextureDetailsValue.value = detail;
        m_LevelOfDetailValue.value = detail;
    }

    private void OnMediumButtonOnclicked()
    {
        m_CanSetAsCustom = false;
        QualitySettings.SetQualityLevel(1);
        m_VerticalSyncValue.value = false;
        m_MotionBlurValue.value = false;
        m_VolumetricFogValue.value = true;
        m_PostprocesingValue.value = true;
        m_ReflectionsValue.value = true;

        var detail = "Medium";
        m_ShadowQualityValue.value = detail;
        m_TextureDetailsValue.value = detail;
        m_LevelOfDetailValue.value = detail;
    }

    private void OnLowButtonOnclicked()
    {
        m_CanSetAsCustom = false;
        QualitySettings.SetQualityLevel(2);
        m_VerticalSyncValue.value = false;
        m_MotionBlurValue.value = false;
        m_VolumetricFogValue.value = false;
        m_PostprocesingValue.value = false;
        m_ReflectionsValue.value = false;

        var detail = "Low";
        m_ShadowQualityValue.value = detail;
        m_TextureDetailsValue.value = detail;
        m_LevelOfDetailValue.value = detail;
    }

    private void OnDestroy()
    {
        if (IsSet)
        {
            m_PostprocesingValue.UnregisterValueChangedCallback(OnPostprocessingChanged);
            m_VolumetricFogValue.UnregisterValueChangedCallback(OnVolumetricFogChanged);
            m_ReflectionsValue.UnregisterValueChangedCallback(OnReflectionsChanged);
            m_MotionBlurValue.UnregisterValueChangedCallback(OnMotionBlurChanged);
            m_VerticalSyncValue.UnregisterValueChangedCallback(OnVsyncChanged);

            m_ScreenmodeValue.UnregisterValueChangedCallback(OnScreenmodeChanged);
            m_TextureDetailsValue.UnregisterValueChangedCallback(OnTextureDetailsChanged);
            m_ShadowQualityValue.UnregisterValueChangedCallback(OnShadowQualityChanged);
            m_LevelOfDetailValue.UnregisterValueChangedCallback(LevelOfDetailChanged);
        }
    }

    private void Update()
    {
        m_CanSetAsCustom = true;
        if (IsVisible && m_MainRoot != null)
            AddStylesToPopupComboboxList();
    }

    private void AddStylesToPopupComboboxList()
    {
        m_MainRoot.Query<ScrollView>().ForEach((c) =>
        {
            c.style.backgroundColor = new StyleColor(new Color(0.0f, 0.0f, 0.0f, 0.97f));
            c.style.color = new StyleColor(Color.white);
            c.parent.style.borderBottomColor = new StyleColor(new Color(0.02352941f, 0.6862745f, 1));
            c.parent.style.borderLeftColor = new StyleColor(new Color(0.02352941f, 0.6862745f, 1));
            c.parent.style.borderRightColor = new StyleColor(new Color(0.02352941f, 0.6862745f, 1));
            c.parent.style.borderTopColor = new StyleColor(new Color(0.02352941f, 0.6862745f, 1));

            c.parent.style.borderLeftWidth = new StyleFloat(1);
            c.parent.style.borderRightWidth = new StyleFloat(1);
            c.parent.style.borderTopWidth = new StyleFloat(1);
            c.parent.style.borderBottomWidth = new StyleFloat(1);
        });
    }

    private void OnVsyncChanged(ChangeEvent<bool> value)
    {
        QualitySettings.vSyncCount = value.newValue ? 1 : 0;
        SetCustom();
    }

    private void OnMotionBlurChanged(ChangeEvent<bool> value)
    {
        m_MotionBlur.active = value.newValue;
        SetCustom();
    }

    private void OnVolumetricFogChanged(ChangeEvent<bool> value)
    {
        RenderSettings.fog = value.newValue;
        FogVolume.SetActive(value.newValue);
        SetCustom();
    }

    private void OnPostprocessingChanged(ChangeEvent<bool> value)
    {
        m_PostProcessing.enabled = value.newValue;
        SetCustom();
    }

    private void OnReflectionsChanged(ChangeEvent<bool> value)
    {
        ReflectionVolume.SetActive(value.newValue);
        QualitySettings.realtimeReflectionProbes = value.newValue;
        SetCustom();
    }

    private void OnShadowQualityChanged(ChangeEvent<string> value)
    {
        SetShadowQuality(value.newValue.ToLower());
        SetCustom();
    }

    private void OnTextureDetailsChanged(ChangeEvent<string> value)
    {
        SetTextureDetail(value.newValue.ToLower());
        SetCustom();
    }

    private void LevelOfDetailChanged(ChangeEvent<string> value)
    {
        SetLOD(value.newValue.ToLower());
        SetCustom();
    }


    private void OnGraphicsQualityChanged(ChangeEvent<string> value)
    {
        switch (value.newValue)
        {
            case "High":
                OnHighButtonOnclicked();
                break;
            case "Medium":
                OnMediumButtonOnclicked();
                break;
            case "Low":
                OnLowButtonOnclicked();
                break;
        }
    }

    private void OnScreenmodeChanged(ChangeEvent<string> value)
    {
        SetFullscreen(value.newValue.ToLower());
        SetCustom();
    }

    private void SetShadowQuality(string value)
    {
        switch (value)
        {
            case "low":
                QualitySettings.shadows = ShadowQuality.Disable;
                break;
            case "medium":
                QualitySettings.shadows = ShadowQuality.HardOnly;
                break;
            case "high":
                QualitySettings.shadows = ShadowQuality.All;
                break;
        }
    }

    private void SetTextureDetail(string value)
    {
        switch (value)
        {
            case "low":
                QualitySettings.globalTextureMipmapLimit = 2;
                break;
            case "medium":
                QualitySettings.globalTextureMipmapLimit = 1;
                break;
            case "high":
                QualitySettings.globalTextureMipmapLimit = 0;
                break;
        }
    }

    private void SetLOD(string value)
    {
        switch (value)
        {
            case "low":
                QualitySettings.maximumLODLevel = 2;
                break;
            case "medium":
                QualitySettings.maximumLODLevel = 1;
                break;
            case "high":
                QualitySettings.maximumLODLevel = 0;
                break;
        }
    }

    private void SetFullscreen(string value)
    {
        if (value == FullScreenMode.Windowed.ToString().ToLower())
        {
            Screen.fullScreen = false;
            Screen.fullScreenMode = FullScreenMode.Windowed;
        }
        else
        {
            Screen.fullScreen = true;
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
        }
    }

    private void SetCustom()
    {
        if (m_CanSetAsCustom)
            m_QualityValue.value = m_QualityValue.choices[3];
    }
}
