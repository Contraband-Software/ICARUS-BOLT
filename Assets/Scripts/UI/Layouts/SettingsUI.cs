using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using SharedState;
using UnityEngine.InputSystem;
using System;
using UnityEngine.UI;
using TMPro;

public class SettingsUI : MonoBehaviour
{
    [SerializeField] private InputEvents inputEvents;

    [Serializable]
    public class SettingsUIElements
    {
        public Canvas canvas;
        public Button btn_apply;
        public Button btn_discard;
        public Button btn_reset_defaults;
        public Slider x_sensitivity_slider;
        public TextMeshProUGUI x_sensitivity_slider_value_text;
    }

    [SerializeField] SettingsUIElements elements = new SettingsUIElements();

    public UnityEvent OnOpened;
    public UnityEvent OnClosed;

    private void Start()
    {
        Hide();
    }

    public void Open()
    {
        GameSettings.InitializeWorkingCopy();
        Show();
    }

    public void Close()
    {
        GameSettings.Save();
        Hide();
    }

    public void Show()
    {
        UpdateUI();
        elements.canvas.enabled = true;
    }

    public void Hide()
    {
        elements.canvas.enabled = false;
    }

    public void Button_Apply()
    {
        GameSettings.WriteWorkingCopy();
        UpdateUI();
    }
    public void Button_Discard()
    {
        GameSettings.ClearWorkingCopy();
        GameSettings.InitializeWorkingCopy();
        UpdateUI();
    }
    public void Button_Reset_Defaults()
    {
        GameSettings.ClearWorkingCopy();
        GameSettings.ResetToDefaults();
        GameSettings.InitializeWorkingCopy();
        UpdateUI();
    }

    public void Changed_X_Sensitivity(float v)
    {
        GameSettings.Set_X_Sensitivity(v);
        UpdateUI();
    }

    private void UpdateUI()
    {
        bool settingsDirty = GameSettings.CheckDirty();

        // UI Buttons
        elements.btn_apply.interactable = settingsDirty;
        elements.btn_discard.interactable = settingsDirty;

        // X Sensitivy Slider
        float x_sens_v = GameSettings.DataWorkingCopy.control.x_sensitivity;
        elements.x_sensitivity_slider_value_text.text = x_sens_v.ToString("0.00");
        elements.x_sensitivity_slider.SetValueWithoutNotify(GameSettings.Get_X_Sensitivity_As_Percent());
    }
}
