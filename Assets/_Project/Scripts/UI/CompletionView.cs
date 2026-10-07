using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubmarineVoyage.UI
{
    /// <summary>Modal shown once when every submarine is unlocked and fully upgraded.</summary>
    public class CompletionView : MonoBehaviour
    {
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private Button okButton;

        private void Awake()
        {
            okButton.onClick.AddListener(Hide);
        }

        /// <param name="playTime">Real time from new game to completion; null when unknown (older save).</param>
        public void Show(TimeSpan? playTime)
        {
            var time = playTime.HasValue ? $"Time to complete: {DurationFormat.Short(playTime.Value)}\n\n" : "";
            bodyText.text = "Fleet complete!\n\n" +
                            "All 4 submarines are fully upgraded.\n" +
                            time +
                            "Keep sailing, or reset the save in Settings to start over.";
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
