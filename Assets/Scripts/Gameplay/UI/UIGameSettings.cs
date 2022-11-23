using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Allows game settings to navigate through game setting views.
/// Shows and hides view according to the game settings menu.
/// </summary>
public class UIGameSettings : MonoBehaviour
{
    [SerializeField]
    private UISettingsTab [] m_TabViews;
    private int m_CurrentView = 0;

    private Button m_GraphicsButton;
    private Button m_AudioButton;
    private Button m_CloseButton;
    private Button m_ApplyButton;
    private Button m_ControlsButton;
    private VisualElement m_GameSettings;
    private VisualElement m_TriggerSettings;
    private List<VisualElement> m_ViewSettingsList = new List<VisualElement>();
    private List<Button> m_TabButtons = new List<Button>();
    private const string SELECTED_CLASS = "button-menu-active";

    private void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        m_GameSettings = root.Q<VisualElement>("game-settings");

        foreach (var view in m_TabViews)
        {
            view.GameSettingsView = root.Q<VisualElement>(view.TabName);
            m_ViewSettingsList.Add(view.GameSettingsView);
        }

        m_GraphicsButton = root.Q<Button>("graphics-button");
        m_AudioButton = root.Q<Button>("audio-button");
        m_ControlsButton = root.Q<Button>("controls-button");

        m_CloseButton = root.Q<Button>("close-button");
        m_ApplyButton = root.Q<Button>("apply-button");

        m_TabButtons.Add(m_GraphicsButton);
        m_TabButtons.Add(m_AudioButton);
        m_TabButtons.Add(m_ControlsButton);

        m_GraphicsButton.clicked += () =>{ m_CurrentView = 0; };
        m_GraphicsButton.clicked += ShowSettings;
        m_AudioButton.clicked += () =>{ m_CurrentView = 1; };
        m_AudioButton.clicked += ShowSettings;
        m_ControlsButton.clicked += () =>{ m_CurrentView = 2; };
        m_ControlsButton.clicked += ShowSettings;

        m_CloseButton.clicked += Hide;
        m_CloseButton.clicked += Reset;
        m_ApplyButton.clicked += Hide;
        m_ApplyButton.clicked += Apply;

        Hide();
    }

    private void Update()
    {
        if (m_GameSettings.style.display == DisplayStyle.Flex)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                Hide();
        }
    }

    public void Show(VisualElement caller = null)
    {
        if (caller != null)
        {
            caller.style.display = DisplayStyle.None;
            m_TriggerSettings = caller;
        }

        m_GameSettings.style.display = DisplayStyle.Flex;
        ShowSettings();
    }

    private void Hide()
    {
        if (m_TriggerSettings != null)
        {
            m_TriggerSettings.style.display = DisplayStyle.Flex;
            m_TriggerSettings = null;
        }

        m_GameSettings.style.display = DisplayStyle.None;
        m_CurrentView = 0;
    }

    private void ShowSettings()
    {
        m_ViewSettingsList.ForEach((element => element.style.display = DisplayStyle.None));
        m_ViewSettingsList[m_CurrentView].style.display = DisplayStyle.Flex;
        foreach (var tab in m_TabViews) { tab.Hide(); }

        m_TabViews[m_CurrentView].Show();
        m_TabButtons.ForEach(b=> b.RemoveFromClassList(SELECTED_CLASS));
        m_TabButtons[m_CurrentView].AddToClassList(SELECTED_CLASS);
    }

    private void Reset()
    {
        m_TabViews[m_CurrentView].Reset();
    }

    private void Apply()
    {
        m_TabViews[m_CurrentView].Apply();
    }
}
