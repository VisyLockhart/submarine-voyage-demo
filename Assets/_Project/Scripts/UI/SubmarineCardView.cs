using System;
using SubmarineVoyage.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubmarineVoyage.UI
{
    /// <summary>
    /// Displays one submarine slot (locked or unlocked) and forwards the button click. Holds no game rules.
    /// </summary>
    public class SubmarineCardView : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text stateText;
        [SerializeField] private TMP_Text countdownText;
        [SerializeField] private Button actionButton;
        [SerializeField] private TMP_Text actionButtonLabel;

        /// <summary>Raised with this card so one handler can serve every card.</summary>
        public event Action<SubmarineCardView> ActionClicked;

        private void Awake()
        {
            actionButton.onClick.AddListener(() => ActionClicked?.Invoke(this));
        }

        public void ShowLocked(int slotNumber, int unlockCost, bool isNextToUnlock, bool canAfford)
        {
            nameText.text = $"Slot {slotNumber}";
            stateText.text = isNextToUnlock ? "Locked" : $"Unlock slot {slotNumber - 1} first";
            countdownText.text = $"{unlockCost} G";
            actionButtonLabel.text = "Unlock";
            actionButton.interactable = isNextToUnlock && canAfford;
        }

        public void Refresh(Submarine submarine, DateTime nowUtc)
        {
            nameText.text = $"{submarine.Name}\nCargo Lv{submarine.CargoLevel} / Speed Lv{submarine.SpeedLevel}";

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
                    countdownText.text = DurationFormat.Countdown(submarine.GetRemaining(nowUtc));
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
    }
}
