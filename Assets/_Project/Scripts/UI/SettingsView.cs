using System;
using SubmarineVoyage.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubmarineVoyage.UI
{
    /// <summary>
    /// Settings modal: time scale choice and a two-step reset. Raises requests only.
    /// </summary>
    public class SettingsView : MonoBehaviour
    {
        [Tooltip("One button per TimeScaleOptions.All entry, in the same order (1x, 60x, 600x).")]
        [SerializeField] private Button[] timeScaleButtons;
        [SerializeField] private TMP_Text timeScaleInfoText;
        [SerializeField] private Button resetButton;
        [SerializeField] private TMP_Text resetButtonLabel;
        [SerializeField] private Button closeButton;

        public event Action<double> TimeScaleSelected;
        public event Action ResetRequested;

        private bool _confirmingReset;

        private void Awake()
        {
            for (var i = 0; i < timeScaleButtons.Length && i < TimeScaleOptions.All.Count; i++)
            {
                var value = TimeScaleOptions.All[i];
                timeScaleButtons[i].onClick.AddListener(() => TimeScaleSelected?.Invoke(value));
            }
            resetButton.onClick.AddListener(OnResetClicked);
            closeButton.onClick.AddListener(Hide);
        }

        public void Show(double currentTimeScale, TimeSpan longestVoyage)
        {
            SetResetConfirming(false);
            gameObject.SetActive(true);
            Refresh(currentTimeScale, longestVoyage);
        }

        public void Hide()
        {
            SetResetConfirming(false);
            gameObject.SetActive(false);
        }

        /// <param name="longestVoyage">Real duration of the longest route at the current scale, as a hint.</param>
        public void Refresh(double currentTimeScale, TimeSpan longestVoyage)
        {
            // The active option is shown by disabling its button.
            for (var i = 0; i < timeScaleButtons.Length && i < TimeScaleOptions.All.Count; i++)
                timeScaleButtons[i].interactable = TimeScaleOptions.All[i] != currentTimeScale;

            timeScaleInfoText.text = $"Time scale {currentTimeScale}x   (longest route: {DurationFormat.Short(longestVoyage)})\n" +
                                     "Applies to new voyages only.";
        }

        private void OnResetClicked()
        {
            if (!_confirmingReset)
            {
                SetResetConfirming(true);
                return;
            }

            SetResetConfirming(false);
            ResetRequested?.Invoke();
        }

        private void SetResetConfirming(bool confirming)
        {
            _confirmingReset = confirming;
            resetButtonLabel.text = confirming ? "Tap again to delete save" : "Reset save";
        }
    }
}
