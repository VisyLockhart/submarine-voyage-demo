using System;
using SubmarineVoyage.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubmarineVoyage.UI
{
    /// <summary>One row in the route selection dialog. Lives on a prefab that is instantiated per route.</summary>
    public class RouteButtonView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;

        /// <summary>Shows the route as this submarine would sail it, with its speed and cargo upgrades.</summary>
        public void Bind(Route route, Submarine submarine, double timeScale, Action<Route> onSelected)
        {
            var gameTime = DurationFormat.Short(route.GameDuration);
            var realTime = DurationFormat.Short(submarine.GetVoyageDuration(route, timeScale));
            var (min, max) = submarine.GetRewardRange(route);
            label.text = $"{route.DisplayName}   {gameTime} (wait {realTime})\n" +
                         $"Gold {min.Gold}-{max.Gold}   Materials {min.Materials}-{max.Materials}";

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onSelected(route));
        }
    }
}
