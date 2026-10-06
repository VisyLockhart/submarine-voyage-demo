using SubmarineVoyage.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubmarineVoyage.UI
{
    /// <summary>Modal shown after collecting, listing what the voyage brought back.</summary>
    public class RewardView : MonoBehaviour
    {
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private Button okButton;

        private void Awake()
        {
            okButton.onClick.AddListener(Hide);
        }

        public void Show(string submarineName, string routeName, Reward reward)
        {
            bodyText.text = $"{submarineName} returned from {routeName}\n\n" +
                            $"Gold +{reward.Gold}\n" +
                            $"Materials +{reward.Materials}";
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
