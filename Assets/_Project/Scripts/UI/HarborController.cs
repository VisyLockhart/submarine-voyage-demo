using System;
using SubmarineVoyage.Core;
using TMPro;
using UnityEngine;

namespace SubmarineVoyage.UI
{
    /// <summary>
    /// Scene entry point: creates the core objects and connects them to the views.
    /// Day-1 scope: one submarine, one route.
    /// </summary>
    public class HarborController : MonoBehaviour
    {
        [SerializeField] private SubmarineCardView card;
        [SerializeField] private TMP_Text walletText;

        [Tooltip("Game seconds per real second. 60 = 1 real second is 1 game minute.")]
        [SerializeField] private float timeScale = 60f;

        private readonly IClock _clock = new SystemClock();
        private readonly IRandomSource _random = new SystemRandomSource();
        private readonly Wallet _wallet = new Wallet();
        private readonly Submarine _submarine = new Submarine("Submarine 1");
        private readonly Route _nearSea = new Route("near", "Near Sea", TimeSpan.FromMinutes(10), 50, 80, 0, 1);

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
                    _submarine.Depart(_nearSea, now, timeScale);
                    break;
                case SubmarineState.ReadyToCollect:
                    _wallet.Add(_submarine.Collect(now, _random));
                    break;
            }
            Refresh();
        }

        private void Refresh()
        {
            card.Refresh(_submarine, _clock.UtcNow);
            walletText.text = $"Gold {_wallet.Gold}   Materials {_wallet.Materials}";
        }
    }
}
