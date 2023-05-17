using System;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities.Solvers;
using TMPro;
using UnityEngine;
using Visometry.VisionLib.SDK.Core;
#if UNITY_WSA
using Visometry.VisionLib.SDK.HoloLens;
#endif

namespace Visometry.VisionLib.SDK.MRTK.Examples
{
    /// <summary>
    /// Provides functionality for the hand menu used in the
    /// HoloLensModelTrackingSetup example scene.
    /// Uses the MRTK hand menu which can be activated by rotating the hand palm to the camera.
    /// </summary>
    /// @ingroup Examples
    [AddComponentMenu("VisionLib/HoloLens/MRTK/Tracking Setup Hand Menu")]
    public class TrackingSetupHandMenu : MonoBehaviour
    {
        public PressableButtonHoloLens2 toggleTrackingButton;
        public PressableButtonHoloLens2 resetTrackingButton;
        public PressableButtonHoloLens2 toggleDebugImageButton;
        public PressableButtonHoloLens2 toggleParametersButton;
        public PressableButtonHoloLens2 saveConfigurationButton;

        public GameObject parametersMenu;

        private ModelTrackerParameters modelTrackerParameters;

        private const string iconResume = "IconDone";
        private const string iconPause = "IconClose";

        private void Awake()
        {
            this.modelTrackerParameters = FindObjectOfType<ModelTrackerParameters>();
            if (this.modelTrackerParameters == null)
            {
                gameObject.AddComponent<ModelTrackerParameters>();
            }

            UpdateButton(
                this.toggleTrackingButton,
                "Pause Tracking",
                TrackingSetupHandMenu.iconPause);
            UpdateButton(this.toggleParametersButton, "Parameters On");
            UpdateButton(this.toggleDebugImageButton, "Show Debug Image");

            SetParametersMenuActive(false);
        }

        private void OnEnable()
        {
            this.toggleTrackingButton.ButtonPressed.AddListener(ToggleTracking);
            this.resetTrackingButton.ButtonPressed.AddListener(ResetTracking);
            this.toggleDebugImageButton.ButtonPressed.AddListener(ToggleDebugImage);
            this.toggleParametersButton.ButtonPressed.AddListener(ToggleParameters);
            this.saveConfigurationButton.ButtonPressed.AddListener(SaveConfiguration);
        }

        private void OnDisable()
        {
            this.saveConfigurationButton.ButtonPressed.RemoveListener(SaveConfiguration);
            this.toggleParametersButton.ButtonPressed.RemoveListener(ToggleParameters);
            this.toggleDebugImageButton.ButtonPressed.RemoveListener(ToggleDebugImage);
            this.resetTrackingButton.ButtonPressed.RemoveListener(ResetTracking);
            this.toggleTrackingButton.ButtonPressed.RemoveListener(ToggleTracking);
        }

        private void ToggleTracking()
        {
            if (TrackingManager.Instance.GetTrackingRunning())
            {
                TrackingManager.Instance.PauseTracking();
                UpdateButton(
                    this.toggleTrackingButton,
                    "Resume Tracking",
                    TrackingSetupHandMenu.iconResume);
            }
            else
            {
                TrackingManager.Instance.ResumeTracking();
                UpdateButton(
                    this.toggleTrackingButton,
                    "Pause Tracking",
                    TrackingSetupHandMenu.iconPause);
            }
        }

        private void ResetTracking()
        {
#if UNITY_WSA
            HoloLensTrackerEvents.ResetTrackingHard();
#endif
        }

        private void ToggleDebugImage()
        {
            if (
#if UNITY_WSA
                HoloLensTrackerEvents.IsDebugImageActive
#else
                false
#endif
                )
            {
                HideDebugImageAndUpdateButton();
            }
            else
            {
                ShowDebugImageAndUpdateButton();
            }
        }

        private void ShowDebugImageAndUpdateButton()
        {
#if UNITY_WSA
            HoloLensTrackerEvents.ShowDebugImage();
#endif
            UpdateButton(this.toggleDebugImageButton, "Hide Debug Image");
        }

        private void HideDebugImageAndUpdateButton()
        {
#if UNITY_WSA
            HoloLensTrackerEvents.HideDebugImage();
#endif
            UpdateButton(this.toggleDebugImageButton, "Show Debug Image");
        }

        private void ToggleParameters()
        {
            if (this.parametersMenu.activeSelf)
            {
                SetParametersMenuActive(false);
                UpdateButton(this.toggleParametersButton, "Parameters On");
                HideDebugImageAndUpdateButton();
                
            }
            else
            {
                SetParametersMenuActive(true);
                UpdateButton(this.toggleParametersButton, "Parameters Off");
                ShowDebugImageAndUpdateButton();
            }
        }

        private void SetParametersMenuActive(bool active)
        {
            this.parametersMenu.SetActive(active);
            this.parametersMenu.GetComponent<RadialView>().enabled = active;
        }

        private void SaveConfiguration()
        {
            this.modelTrackerParameters.SaveCurrentConfiguration();
        }

        private static void UpdateButton(
            PressableButtonHoloLens2 button,
            string label,
            string icon = null)
        {
            var buttonLabel = button.MovingButtonIconText.GetComponentInChildren<TextMeshPro>();
            buttonLabel.text = label;

            if (String.IsNullOrEmpty(icon))
            {
                return;
            }
            var configHelper = button.GetComponent<ButtonConfigHelper>();
            configHelper.SetQuadIconByName(icon);
        }
    }
}
