using System;
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
        [Tooltip("One card per fleet slot, in slot order.")]
        [SerializeField] private SubmarineCardView[] cards;
        [SerializeField] private TMP_Text walletText;
        [SerializeField] private RouteSelectionView routeSelection;
        [SerializeField] private RewardView rewardView;
        [SerializeField] private UpgradeShopView upgradeShop;
        [SerializeField] private RouteDefinition[] routes;

        [Tooltip("Game seconds per real second. 60 = 1 real second is 1 game minute.")]
        [SerializeField] private float timeScale = 60f;

        [Tooltip("Gold at game start. Raise it temporarily to test unlocks and upgrades.")]
        [Min(0)] [SerializeField] private int startingGold;

        [Min(0)] [SerializeField] private int startingMaterials;

        private readonly IClock _clock = new SystemClock();
        private readonly IRandomSource _random = new SystemRandomSource();
        private Wallet _wallet;
        private readonly Fleet _fleet = new Fleet();
        private IReadOnlyList<Route> _routes;

        private void Awake()
        {
            _wallet = new Wallet(startingGold, startingMaterials);
            _routes = routes.Select(r => r.ToRoute()).ToList();
            if (cards.Length != _fleet.SlotCount)
                Debug.LogError($"HarborController needs {_fleet.SlotCount} cards but has {cards.Length}.", this);
        }

        private void Start()
        {
            routeSelection.Hide();
            rewardView.Hide();
            upgradeShop.Hide();
        }

        private void OnEnable()
        {
            foreach (var card in cards)
            {
                card.ActionClicked += OnCardAction;
                card.UpgradeClicked += OnCardUpgrade;
            }
            upgradeShop.UpgradeRequested += OnUpgradeRequested;
        }

        private void OnDisable()
        {
            foreach (var card in cards)
            {
                card.ActionClicked -= OnCardAction;
                card.UpgradeClicked -= OnCardUpgrade;
            }
            upgradeShop.UpgradeRequested -= OnUpgradeRequested;
        }

        private void Update()
        {
            // Only text updates per frame; the state itself comes from timestamps.
            Refresh();
        }

        private void OnCardAction(SubmarineCardView card)
        {
            var slot = Array.IndexOf(cards, card);
            if (!_fleet.IsUnlocked(slot))
            {
                _fleet.TryUnlockNext(_wallet);
                Refresh();
                return;
            }

            var submarine = _fleet.Submarines[slot];
            var now = _clock.UtcNow;
            switch (submarine.GetState(now))
            {
                case SubmarineState.Idle:
                    routeSelection.Show(_routes, timeScale, route => Depart(submarine, route));
                    break;
                case SubmarineState.ReadyToCollect:
                    // Read the route name first: collecting clears the current route.
                    var routeName = submarine.CurrentRoute.DisplayName;
                    var reward = submarine.Collect(now, _random);
                    _wallet.Add(reward);
                    rewardView.Show(submarine.Name, routeName, reward);
                    break;
            }
            Refresh();
        }

        private void OnCardUpgrade(SubmarineCardView card)
        {
            var slot = Array.IndexOf(cards, card);
            if (!_fleet.IsUnlocked(slot)) return;
            upgradeShop.Show(_fleet.Submarines[slot], _wallet);
        }

        private void OnUpgradeRequested(UpgradeType type)
        {
            var submarine = upgradeShop.Current;
            if (submarine == null) return;
            submarine.TryUpgrade(type, _wallet);
            upgradeShop.Refresh(_wallet);
            Refresh();
        }

        private void Depart(Submarine submarine, Route route)
        {
            // Re-check: the voyage starts when the route is picked, not when the dialog opened.
            var now = _clock.UtcNow;
            if (submarine.GetState(now) != SubmarineState.Idle) return;
            submarine.Depart(route, now, timeScale);
            Refresh();
        }

        private void Refresh()
        {
            var now = _clock.UtcNow;
            for (var slot = 0; slot < cards.Length; slot++)
            {
                if (_fleet.IsUnlocked(slot))
                {
                    cards[slot].Refresh(_fleet.Submarines[slot], now);
                }
                else
                {
                    var cost = _fleet.GetUnlockCost(slot);
                    cards[slot].ShowLocked(slot + 1, cost, _fleet.IsNextToUnlock(slot), _wallet.CanAfford(cost));
                }
            }
            walletText.text = $"Gold {_wallet.Gold}   Materials {_wallet.Materials}";
        }
    }
}
