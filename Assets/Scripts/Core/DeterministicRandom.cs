using System;
using UnityEngine;

namespace FruitDefense.Core
{
    [Serializable]
    public sealed class DeterministicRandom
    {
        public const uint ZeroSeedState = 0x6D2B79F5u;

        [SerializeField] private uint _state;

        public uint State { get { return _state; } }

        public DeterministicRandom(int seed = 0)
        {
            Reset(seed);
        }

        public void Reset(int seed)
        {
            RestoreState(unchecked((uint)seed));
        }

        public void RestoreState(uint state)
        {
            _state = state == 0u ? ZeroSeedState : state;
        }

        public uint NextUInt()
        {
            var value = _state;
            value ^= value << 13;
            value ^= value >> 17;
            value ^= value << 5;
            _state = value;
            return value;
        }

        public int NextInt(int maxExclusive)
        {
            if (maxExclusive <= 0) throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            return (int)(NextUInt() % (uint)maxExclusive);
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            return minInclusive + NextInt(maxExclusive - minInclusive);
        }

        public double NextUnitDouble()
        {
            return NextUInt() / 4294967296.0;
        }
    }
}
