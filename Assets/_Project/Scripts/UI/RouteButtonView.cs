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

        public void Bind(Route route, double timeScale, Action<Route> onSelected)
        {
            var gameTime = DurationFormat.Short(route.GameDuration);
            var realTime = DurationFormat.Short(route.GetRealDuration(timeScale));
            label.text = $"{route.DisplayName}   {gameTime} (wait {realTime})\n" +
                         $"Gold {route.MinGold}-{route.MaxGold}   Materials {route.MinMaterials}-{route.MaxMaterials}";

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onSelected(route));
        }
    }
}
