using System;
using SubmarineVoyage.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubmarineVoyage.UI
{
    /// <summary>
    /// Displays one submarine and forwards the button click. Holds no game rules.
    /// </summary>
    public class SubmarineCardView : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text stateText;
        [SerializeField] private TMP_Text countdownText;
        [SerializeField] private Button actionButton;
        [SerializeField] private TMP_Text actionButtonLabel;

        public event Action ActionClicked;

        private void Awake()
        {
            actionButton.onClick.AddListener(() => ActionClicked?.Invoke());
        }

        public void Refresh(Submarine submarine, DateTime nowUtc)
        {
            nameText.text = submarine.Name;

            switch (submarine.GetState(nowUtc))
            {
                case SubmarineState.Idle:
                    stateText.text = "Idle";
                    countdownText.text = "--:--";
                    actionButtonLabel.text = "Depart";
                    actionButton.interactable = true;
                    break;
                case SubmarineState.Voyaging:
                    stateText.text = $"Voyaging ({submarine.CurrentRoute.DisplayName})";
                    countdownText.text = FormatRemaining(submarine.GetRemaining(nowUtc));
                    actionButtonLabel.text = "At sea";
                    actionButton.interactable = false;
                    break;
                case SubmarineState.ReadyToCollect:
                    stateText.text = "Returned";
                    countdownText.text = "00:00";
                    actionButtonLabel.text = "Collect";
                    actionButton.interactable = true;
                    break;
            }
        }

        private static string FormatRemaining(TimeSpan remaining)
        {
            // Round up so the display never shows 00:00 while still at sea.
            var seconds = (int)Math.Ceiling(remaining.TotalSeconds);
            return $"{seconds / 60:00}:{seconds % 60:00}";
        }
    }
}
