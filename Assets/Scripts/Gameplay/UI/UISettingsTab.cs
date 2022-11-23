using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Contains the shared and global properties and Methods for the UI tabs Views in GameSettings.
/// Manages how to show and hide the states should be controlled.
/// </summary>
public abstract class UISettingsTab : MonoBehaviour
{
    protected bool IsSet = false;
    private string ViewName => "game-settings";
    public VisualElement GameSettingsView { get; set; }
    protected bool IsVisible { get; private set; }

    public abstract string TabName { get; }
    protected Dictionary<Slider, float> m_CurrentSliderData = new Dictionary<Slider, float>();
    protected Dictionary<Toggle, bool> m_CurrentToggleData = new Dictionary<Toggle, bool>();
    protected Dictionary<DropdownField, string> m_CurrentDropdownFieldData = new Dictionary<DropdownField, string>();

    public void Show()
    {
        if (!IsSet)
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            if (root.Q<VisualElement>(ViewName).style.display == DisplayStyle.Flex)
                Initialization();
        }

        IsVisible = true;
        SaveCurrentState();
    }

    public void Hide()
    {
        IsVisible = false;
    }

    public void Apply()
    {
        SaveCurrentState();
    }

    protected virtual void SaveCurrentState()
    {
    }

    protected virtual void Initialization()
    {
        IsSet = true;
    }

    public virtual void Reset()
    {
    }

    protected void UpdateSliderCurrentState(Slider slider)
    {
        if (m_CurrentSliderData.ContainsKey(slider))
        {
            m_CurrentSliderData[slider] = slider.value;
        }
        else
        {
            m_CurrentSliderData.Add(slider, slider.value);
        }
    }

    protected void ResetSliderCurrentState(Slider slider)
    {
        if (m_CurrentSliderData.ContainsKey(slider))
        {
            slider.value = m_CurrentSliderData[slider];
        }
    }

    protected void UpdateCurrentToggleState(Toggle toggle)
    {
        if (m_CurrentToggleData.ContainsKey(toggle))
        {
            m_CurrentToggleData[toggle] = toggle.value;
        }
        else
        {
            m_CurrentToggleData.Add(toggle, toggle.value);
        }
    }

    protected void ResetCurrentToggleState(Toggle toggle)
    {
        if (m_CurrentToggleData.ContainsKey(toggle))
        {
            toggle.value = m_CurrentToggleData[toggle];
        }
    }

    protected void UpdateCurrentDropdownFieldState(DropdownField dropdownField)
    {
        if (m_CurrentDropdownFieldData.ContainsKey(dropdownField))
        {
            m_CurrentDropdownFieldData[dropdownField] = dropdownField.value;
        }
        else
        {
            m_CurrentDropdownFieldData.Add(dropdownField, dropdownField.value);
        }
    }

    protected void ResetCurrentDropdownFieldState(DropdownField dropdownField)
    {
        if (m_CurrentDropdownFieldData.ContainsKey(dropdownField))
        {
            dropdownField.value = m_CurrentDropdownFieldData[dropdownField];
        }
    }
}
