using System;
using SubmarineVoyage.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubmarineVoyage.UI
{
    /// <summary>
    /// Modal upgrade shop for one submarine. Only displays and raises requests;
    /// the controller applies the upgrade through Core and calls <see cref="Refresh"/>.
    /// </summary>
    public class UpgradeShopView : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private UpgradeRowView cargoRow;
        [SerializeField] private UpgradeRowView speedRow;
        [SerializeField] private Button closeButton;

        public event Action<UpgradeType> UpgradeRequested;

        public Submarine Current { get; private set; }

        private void Awake()
        {
            cargoRow.Clicked += () => UpgradeRequested?.Invoke(UpgradeType.Cargo);
            speedRow.Clicked += () => UpgradeRequested?.Invoke(UpgradeType.Speed);
            closeButton.onClick.AddListener(Hide);
        }

        public void Show(Submarine submarine, Wallet wallet)
        {
            Current = submarine;
            gameObject.SetActive(true);
            Refresh(wallet);
        }

        public void Hide()
        {
            Current = null;
            gameObject.SetActive(false);
        }

        public void Refresh(Wallet wallet)
        {
            if (Current == null) return;

            titleText.text = $"Upgrade {Current.Name}";
            ShowRow(cargoRow, UpgradeType.Cargo, "Cargo", "Reward",
                level => $"x{UpgradeRules.CargoMultiplier(level):0.0}", wallet);
            ShowRow(speedRow, UpgradeType.Speed, "Speed", "Voyage time",
                level => $"x{UpgradeRules.SpeedMultiplier(level):0.00}", wallet);
        }

        private void ShowRow(UpgradeRowView row, UpgradeType type, string name, string effectName,
            Func<int, string> effect, Wallet wallet)
        {
            var level = Current.GetLevel(type);
            if (Current.IsMaxLevel(type))
            {
                row.Show($"{name} Lv{level} (MAX)\n{effectName} {effect(level)}", "MAX", false);
                return;
            }

            var gold = UpgradeRules.GoldCost(level);
            var materials = UpgradeRules.MaterialCost(level);
            row.Show($"{name} Lv{level} -> Lv{level + 1}\n{effectName} {effect(level)} -> {effect(level + 1)}",
                $"{gold} G + {materials} Mat",
                wallet.CanAfford(gold, materials));
        }
    }
}
