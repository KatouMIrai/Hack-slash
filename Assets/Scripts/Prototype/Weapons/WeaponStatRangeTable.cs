using UnityEngine;

namespace WeaponMazeAlchemy.Prototype
{
    [CreateAssetMenu(menuName = "Weapon Maze Prototype/Weapon Stat Range Table")]
    public class WeaponStatRangeTable : ScriptableObject
    {
        [SerializeField] private RankStatRange[] rankRanges =
        {
            new RankStatRange(1, 5, new IntRange(20, 100), new IntRange(10, 50), new IntRange(5, 50), new IntRange(5, 50), new IntRange(1, 5), new IntRange(1, 10), new IntRange(1, 10), new IntRange(0, 3)),
            new RankStatRange(6, 10, new IntRange(50, 200), new IntRange(25, 100), new IntRange(40, 130), new IntRange(40, 130), new IntRange(1, 15), new IntRange(1, 30), new IntRange(1, 20), new IntRange(0, 5)),
            new RankStatRange(11, 15, new IntRange(50, 600), new IntRange(25, 300), new IntRange(40, 300), new IntRange(40, 300), new IntRange(1, 20), new IntRange(1, 60), new IntRange(1, 30), new IntRange(0, 7)),
            new RankStatRange(16, 20, new IntRange(1, 2000), new IntRange(1, 1300), new IntRange(10, 1000), new IntRange(10, 1000), new IntRange(1, 25), new IntRange(1, 70), new IntRange(1, 40), new IntRange(0, 20))
        };

        public RankStatRange GetRangeForRank(int rank)
        {
            int clampedRank = Mathf.Clamp(rank, 1, 20);
            foreach (RankStatRange range in rankRanges)
            {
                if (range != null && range.Contains(clampedRank))
                {
                    return range;
                }
            }

            return rankRanges != null && rankRanges.Length > 0 ? rankRanges[0] : new RankStatRange();
        }

        public static WeaponStatRangeTable CreateRuntimeDefault()
        {
            return CreateInstance<WeaponStatRangeTable>();
        }
    }
}
