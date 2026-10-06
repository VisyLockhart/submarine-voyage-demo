using System;
using SubmarineVoyage.Core;
using UnityEngine;

namespace SubmarineVoyage.Data
{
    /// <summary>
    /// Designer-editable route data stored as an asset. Converted to the engine-free
    /// <see cref="Route"/> so game rules never depend on UnityEngine.
    /// </summary>
    [CreateAssetMenu(fileName = "Route", menuName = "Submarine Voyage/Route")]
    public class RouteDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [Min(1)] [SerializeField] private int gameDurationMinutes = 10;
        [Min(0)] [SerializeField] private int minGold;
        [Min(0)] [SerializeField] private int maxGold;
        [Min(0)] [SerializeField] private int minMaterials;
        [Min(0)] [SerializeField] private int maxMaterials;

        public Route ToRoute() =>
            new Route(id, displayName, TimeSpan.FromMinutes(gameDurationMinutes),
                minGold, maxGold, minMaterials, maxMaterials);
    }
}
