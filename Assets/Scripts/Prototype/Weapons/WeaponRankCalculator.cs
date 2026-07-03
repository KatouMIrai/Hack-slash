using UnityEngine;

namespace WeaponMazeAlchemy.Prototype
{
    public static class WeaponRankCalculator
    {
        public static int GetBaseRank(int dungeonDifficulty, int currentFloor)
        {
            int safeDifficulty = Mathf.Max(1, dungeonDifficulty);
            int safeFloor = Mathf.Max(1, currentFloor);
            return Mathf.Clamp(Mathf.CeilToInt(safeDifficulty * safeFloor * 0.25f), 1, 20);
        }

        public static int RollSpawnRank(int dungeonDifficulty, int currentFloor)
        {
            int baseRank = GetBaseRank(dungeonDifficulty, currentFloor);
            int rolledRank = UnityEngine.Random.Range(baseRank - 1, baseRank + 2);
            return Mathf.Clamp(rolledRank, 1, 20);
        }
    }
}
