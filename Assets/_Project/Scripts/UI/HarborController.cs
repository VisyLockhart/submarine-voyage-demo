using System.Collections.Generic;
using System.Linq;
using SubmarineVoyage.Core;
using SubmarineVoyage.Data;
using TMPro;
using UnityEngine;

namespace SubmarineVoyage.UI
{
    /// <summary>
    /// Scene entry point: creates the core objects and connects them to the views.
    /// </summary>
    public class HarborController : MonoBehaviour
    {
        [SerializeField] private SubmarineCardView card;
        [SerializeField] private TMP_Text walletText;
        [SerializeField] private RouteSelectionView routeSelection;
        [SerializeField] private RouteDefinition[] routes;

        [Tooltip("Game seconds per real second. 60 = 1 real second is 1 game minute.")]
        [SerializeField] private float timeScale = 60f;

        private readonly IClock _clock = new SystemClock();
        private readonly IRandomSource _random = new SystemRandomSource();
        private readonly Wallet _wallet = new Wallet();
        private readonly Submarine _submarine = new Submarine("Submarine 1");
        private IReadOnlyList<Route> _routes;

        private void Awake()
        {
            _routes = routes.Select(r => r.ToRoute()).ToList();
        }

        private void Start()
        {
            routeSelection.Hide();
        }

        private void OnEnable() => card.ActionClicked += OnCardAction;
        private void OnDisable() => card.ActionClicked -= OnCardAction;

        private void Update()
        {
            // Only text updates per frame; the state itself comes from timestamps.
            Refresh();
        }

        private void OnCardAction()
        {
            var now = _clock.UtcNow;
            switch (_submarine.GetState(now))
            {
                case SubmarineState.Idle:
                    routeSelection.Show(_routes, timeScale, Depart);
                    break;
                case SubmarineState.ReadyToCollect:
                    _wallet.Add(_submarine.Collect(now, _random));
                    break;
            }
            Refresh();
        }

        private void Depart(Route route)
        {
            // Re-check: the voyage starts when the route is picked, not when the dialog opened.
            var now = _clock.UtcNow;
            if (_submarine.GetState(now) != SubmarineState.Idle) return;
            _submarine.Depart(route, now, timeScale);
            Refresh();
        }

        private void Refresh()
        {
            card.Refresh(_submarine, _clock.UtcNow);
            walletText.text = $"Gold {_wallet.Gold}   Materials {_wallet.Materials}";
        }
    }
}
