using System;
using UnityEngine;

namespace WeaponMazeAlchemy.Prototype
{
    [Serializable]
    public struct IntRange
    {
        [SerializeField] private int min;
        [SerializeField] private int max;

        public IntRange(int min, int max)
        {
            this.min = min;
            this.max = max;
        }

        public int Min => min;
        public int Max => max;

        public int Roll()
        {
            int low = Mathf.Min(min, max);
            int high = Mathf.Max(min, max);
            return UnityEngine.Random.Range(low, high + 1);
        }
    }
}
