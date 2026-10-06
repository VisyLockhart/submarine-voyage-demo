using System;
using System.Collections.Generic;
using System.Linq;
using SubmarineVoyage.Core;
using SubmarineVoyage.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        [SerializeField] private SettingsView settings;
        [SerializeField] private Button settingsButton;
        [SerializeField] private RouteDefinition[] routes;

        [Tooltip("Gold for a new game (no save file). Raise it temporarily to test unlocks and upgrades.")]
        [Min(0)] [SerializeField] private int startingGold;

        [Min(0)] [SerializeField] private int startingMaterials;

        private readonly IClock _clock = new SystemClock();
        private readonly IRandomSource _random = new SystemRandomSource();

        // Created lazily: Unity forbids Application.persistentDataPath in field initializers.
        private JsonSaveStore _saveStoreInstance;
        private JsonSaveStore SaveStore => _saveStoreInstance ??= new JsonSaveStore();

        private GameState _state;
        private IReadOnlyList<Route> _routes;

        private void Awake()
        {
            _routes = routes.Select(r => r.ToRoute()).ToList();
            LoadOrStartNew();
            if (cards.Length != _state.Fleet.SlotCount)
                Debug.LogError($"HarborController needs {_state.Fleet.SlotCount} cards but has {cards.Length}.", this);
        }

        private void Start()
        {
            HideDialogs();
        }

        private void OnEnable()
        {
            foreach (var card in cards)
            {
                card.ActionClicked += OnCardAction;
                card.UpgradeClicked += OnCardUpgrade;
            }
            upgradeShop.UpgradeRequested += OnUpgradeRequested;
            settings.TimeScaleSelected += OnTimeScaleSelected;
            settings.ResetRequested += ResetGame;
            settingsButton.onClick.AddListener(OpenSettings);
        }

        private void OnDisable()
        {
            foreach (var card in cards)
            {
                card.ActionClicked -= OnCardAction;
                card.UpgradeClicked -= OnCardUpgrade;
            }
            upgradeShop.UpgradeRequested -= OnUpgradeRequested;
            settings.TimeScaleSelected -= OnTimeScaleSelected;
            settings.ResetRequested -= ResetGame;
            settingsButton.onClick.RemoveListener(OpenSettings);
        }

        private void Update()
        {
            // Only text updates per frame; the state itself comes from timestamps.
            Refresh();
        }

        // Mobile/WebGL may kill the app without OnApplicationQuit, so also save when paused.
        private void OnApplicationPause(bool paused)
        {
            if (paused && _state != null) Save();
        }

        private void OnApplicationQuit()
        {
            if (_state != null) Save();
        }

        private void LoadOrStartNew()
        {
            var data = SaveStore.Load();
            _state = data != null
                ? SaveMapper.Restore(data, _routes)
                : GameState.NewGame(startingGold, startingMaterials);
        }

        private void Save() => SaveStore.Save(SaveMapper.Capture(_state));

        /// <summary>Deletes the save and starts a new game.</summary>
        public void ResetGame()
        {
            SaveStore.Delete();
            LoadOrStartNew();
            HideDialogs();
            Refresh();
        }

        [ContextMenu("Reset Save")]
        private void ResetFromInspector()
        {
            if (Application.isPlaying) ResetGame();
            else SaveStore.Delete();
            Debug.Log($"Save reset: {SaveStore.FilePath}");
        }

        [ContextMenu("Log Save Path")]
        private void LogSavePath() => Debug.Log(SaveStore.FilePath);

        private void HideDialogs()
        {
            routeSelection.Hide();
            rewardView.Hide();
            upgradeShop.Hide();
            settings.Hide();
        }

        private void OnCardAction(SubmarineCardView card)
        {
            var slot = Array.IndexOf(cards, card);
            var fleet = _state.Fleet;
            if (!fleet.IsUnlocked(slot))
            {
                if (fleet.TryUnlockNext(_state.Wallet)) Save();
                Refresh();
                return;
            }

            var submarine = fleet.Submarines[slot];
            var now = _clock.UtcNow;
            switch (submarine.GetState(now))
            {
                case SubmarineState.Idle:
                    routeSelection.Show(_routes, _state.TimeScale, route => Depart(submarine, route));
                    break;
                case SubmarineState.ReadyToCollect:
                    // Read the route name first: collecting clears the current route.
                    var routeName = submarine.CurrentRoute.DisplayName;
                    var reward = submarine.Collect(now, _random);
                    _state.Wallet.Add(reward);
                    rewardView.Show(submarine.Name, routeName, reward);
                    Save();
                    break;
            }
            Refresh();
        }

        private void OnCardUpgrade(SubmarineCardView card)
        {
            var slot = Array.IndexOf(cards, card);
            if (!_state.Fleet.IsUnlocked(slot)) return;
            upgradeShop.Show(_state.Fleet.Submarines[slot], _state.Wallet);
        }

        private void OnUpgradeRequested(UpgradeType type)
        {
            var submarine = upgradeShop.Current;
            if (submarine == null) return;
            if (submarine.TryUpgrade(type, _state.Wallet)) Save();
            upgradeShop.Refresh(_state.Wallet);
            Refresh();
        }

        private void OpenSettings() => settings.Show(_state.TimeScale, LongestVoyage());

        private void OnTimeScaleSelected(double timeScale)
        {
            _state.SetTimeScale(timeScale);
            Save();
            settings.Refresh(_state.TimeScale, LongestVoyage());
        }

        private TimeSpan LongestVoyage() =>
            _routes.Count == 0 ? TimeSpan.Zero : _routes.Max(r => r.GetRealDuration(_state.TimeScale));

        private void Depart(Submarine submarine, Route route)
        {
            // Re-check: the voyage starts when the route is picked, not when the dialog opened.
            var now = _clock.UtcNow;
            if (submarine.GetState(now) != SubmarineState.Idle) return;
            submarine.Depart(route, now, _state.TimeScale);
            Save();
            Refresh();
        }

        private void Refresh()
        {
            var now = _clock.UtcNow;
            var fleet = _state.Fleet;
            for (var slot = 0; slot < cards.Length; slot++)
            {
                if (fleet.IsUnlocked(slot))
                {
                    cards[slot].Refresh(fleet.Submarines[slot], now);
                }
                else
                {
                    var cost = fleet.GetUnlockCost(slot);
                    cards[slot].ShowLocked(slot + 1, cost, fleet.IsNextToUnlock(slot), _state.Wallet.CanAfford(cost));
                }
            }
            walletText.text = $"Gold {_state.Wallet.Gold}   Materials {_state.Wallet.Materials}";
        }
    }
}
