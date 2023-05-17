using System;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.UI;
using TMPro;
using Visometry.VisionLib.SDK.Core;
using Visometry.VisionLib.SDK.Core.Details;

namespace Visometry.VisionLib.SDK.MRTK.Examples
{
    /// <summary>
    /// Adjusts the given two-stated runtime parameter through an MRTK button.
    /// Please use the provided prefab `VLRuntimeParameterSwitch`.
    /// </summary>
    /// @ingroup Examples
    [AddComponentMenu("VisionLib/HoloLens/MRTK/Runtime Parameter Switch")]
    [RequireComponent(typeof(PressableButtonHoloLens2))]
    public class RuntimeParameterSwitch : MonoBehaviour
    {
        public RuntimeParameter parameter;

        [Tooltip(
            "Check this to use the switch for a two-state string runtime parameter. " +
            "Otherwise, the parameter type is interpreted as a boolean.")]
        public bool parameterTypeIsString = false;
        [OnlyShowIf("parameterTypeIsString", true)]
        public string valueSwitchOn;
        [OnlyShowIf("parameterTypeIsString", true)]
        public string valueSwitchOff;

        [SerializeField]
        private TextMeshPro valueText = null;
        [SerializeField]
        private TextMeshPro nameText = null;

        private Interactable toggleSwitch;
        private Interactable ToggleSwitch
        {
            get
            {
                if (this.toggleSwitch == null)
                {
                    this.toggleSwitch = GetComponent<Interactable>();
                }
                return this.toggleSwitch;
            }
        }

        private void Awake()
        {
            this.nameText.text = this.parameter.parameterName;

            SetSwitchOn(this.ToggleSwitch.IsToggled);
        }

        private void OnEnable()
        {
            this.ToggleSwitch.OnClick.AddListener(ToggleParameterValue);
        }

        private void OnDisable()
        {
            this.ToggleSwitch.OnClick.RemoveListener(ToggleParameterValue);
        }

        private void ToggleParameterValue()
        {
            SetSwitchOn(this.ToggleSwitch.IsToggled);
        }

        private void SetSwitchOn(bool isOn)
        {
            if (!TrackingManager.Instance.GetTrackerInitialized())
            {
                return;
            }

            if (this.parameterTypeIsString)
            {
                SetParameterValue(isOn ? this.valueSwitchOn : this.valueSwitchOff);
            }
            else
            {
                SetParameterValue(isOn);
            }
        }

        private void SetParameterValue(string newValue)
        {
            this.valueText.text = newValue;
            this.parameter.SetValue(newValue);
        }

        private void SetParameterValue(bool newValue)
        {
            this.valueText.text = newValue.ToString();
            this.parameter.SetValue(newValue);
        }

        public void SetValue(string newValue)
        {
            SetParameterValue(newValue);
            this.ToggleSwitch.IsToggled = newValue == this.valueSwitchOn;
        }

        public void SetValue(bool newValue)
        {
            SetParameterValue(newValue);
            this.ToggleSwitch.IsToggled = newValue;
        }
    }
}
