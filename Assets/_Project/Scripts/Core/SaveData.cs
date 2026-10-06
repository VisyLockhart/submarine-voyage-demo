using System;
using System.Collections.Generic;

namespace SubmarineVoyage.Core
{
    /// <summary>
    /// Serializable snapshot of the game. Public fields and no DateTime/nullable types so it
    /// works with Unity's JsonUtility; times are stored as UTC ticks (0 = none).
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public int gold;
        public int materials;

        /// <summary>0 in saves made before settings existed; restored as the default.</summary>
        public double timeScale;
        public List<SubmarineSaveData> submarines = new List<SubmarineSaveData>();
    }

    [Serializable]
    public class SubmarineSaveData
    {
        public int cargoLevel = UpgradeRules.MinLevel;
        public int speedLevel = UpgradeRules.MinLevel;
        public string routeId = "";
        public long departedAtTicks;
        public long returnAtTicks;
    }
}
