using System;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.UI;
using TMPro;
using Visometry.VisionLib.SDK.Core;
using Visometry.VisionLib.SDK.Core.Details;

namespace Visometry.VisionLib.SDK.MRTK.Examples
{
    /// <summary>
    /// Adjusts the given float runtime parameter through an MRTK pinch slider.
    /// Please use the provided prefab `VLRuntimeParameterPinchSlider`.
    /// </summary>
    /// @ingroup Examples
    [AddComponentMenu("VisionLib/HoloLens/MRTK/Runtime Parameter Slider")]
    [RequireComponent(typeof(PinchSlider))]
    public class RuntimeParameterSlider : MonoBehaviour
    {
        public RuntimeParameter parameter;

        public float minValue = 0f;
        public float maxValue = 1f;
        public bool wholeNumbers = false;

        [SerializeField]
        private TextMeshPro valueText = null;
        [SerializeField]
        private TextMeshPro nameText = null;

        private PinchSlider slider;
        private PinchSlider Slider
        {
            get
            {
                if (this.slider == null)
                {
                    this.slider = GetComponent<PinchSlider>();
                }
                return this.slider;
            }
        }

        private void Awake()
        {
            this.nameText.text = this.parameter.parameterName;
        }

        private void OnEnable()
        {
            this.Slider.OnValueUpdated.AddListener(OnSliderUpdated);
        }

        private void OnDisable()
        {
            this.Slider.OnValueUpdated.RemoveListener(OnSliderUpdated);
        }

        private void OnSliderUpdated(SliderEventData eventData)
        {
            var mappedValue = MathHelper.Remap(
                this.Slider.SliderValue,
                0f,
                1f,
                this.minValue,
                this.maxValue);

            SetParameterValue(mappedValue);
        }

        private void SetParameterValue(float newValue)
        {
            if (!TrackingManager.Instance.GetTrackerInitialized())
            {
                return;
            }

            this.valueText.text = this.wholeNumbers
                ? Mathf.RoundToInt(newValue).ToString()
                : $"{newValue:F2}";

            this.parameter.SetValue(newValue);
        }

        public void SetValue(float newValue)
        {
            SetParameterValue(newValue);

            this.Slider.SliderValue = MathHelper.Remap(
                newValue,
                this.minValue,
                this.maxValue,
                0f,
                1f);
        }
    }
}
