using System;
using System.Collections.Generic;
using SubmarineVoyage.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SubmarineVoyage.UI
{
    /// <summary>
    /// Modal dialog listing routes. Put this on the full-screen overlay panel; the overlay's
    /// Image blocks clicks to the cards behind it while the dialog is open.
    /// </summary>
    public class RouteSelectionView : MonoBehaviour
    {
        [SerializeField] private RouteButtonView routeButtonPrefab;
        [SerializeField] private Transform routeButtonContainer;
        [SerializeField] private Button cancelButton;

        private readonly List<RouteButtonView> _buttons = new List<RouteButtonView>();
        private Action<Route> _onSelected;

        private void Awake()
        {
            cancelButton.onClick.AddListener(Hide);
        }

        public void Show(IReadOnlyList<Route> routes, Submarine submarine, double timeScale, Action<Route> onSelected)
        {
            _onSelected = onSelected;

            // Routes do not change at runtime, so the rows are created once and re-bound.
            for (var i = _buttons.Count; i < routes.Count; i++)
                _buttons.Add(Instantiate(routeButtonPrefab, routeButtonContainer));

            for (var i = 0; i < _buttons.Count; i++)
            {
                var visible = i < routes.Count;
                _buttons[i].gameObject.SetActive(visible);
                if (visible) _buttons[i].Bind(routes[i], submarine, timeScale, Select);
            }

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            _onSelected = null;
            gameObject.SetActive(false);
        }

        private void Select(Route route)
        {
            var callback = _onSelected;
            Hide();
            callback?.Invoke(route);
        }
    }
}
