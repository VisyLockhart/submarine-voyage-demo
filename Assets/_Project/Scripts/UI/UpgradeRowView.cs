using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubmarineVoyage.UI
{
    /// <summary>One upgrade line in the shop: description on the left, price button on the right.</summary>
    public class UpgradeRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text infoText;
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text buttonLabel;

        public event Action Clicked;

        private void Awake()
        {
            button.onClick.AddListener(() => Clicked?.Invoke());
        }

        public void Show(string info, string buttonText, bool interactable)
        {
            infoText.text = info;
            buttonLabel.text = buttonText;
            button.interactable = interactable;
        }
    }
}
