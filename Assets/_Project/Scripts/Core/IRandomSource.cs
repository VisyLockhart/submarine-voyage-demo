using System;

namespace SubmarineVoyage.Core
{
    public interface IRandomSource
    {
        /// <summary>Returns an integer in [minInclusive, maxInclusive].</summary>
        int Range(int minInclusive, int maxInclusive);
    }

    public sealed class SystemRandomSource : IRandomSource
    {
        private readonly Random _random;

        public SystemRandomSource() : this(new Random()) { }

        public SystemRandomSource(Random random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public int Range(int minInclusive, int maxInclusive) => _random.Next(minInclusive, maxInclusive + 1);
    }
}
