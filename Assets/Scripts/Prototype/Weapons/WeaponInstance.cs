using System;
using UnityEngine;

namespace WeaponMazeAlchemy.Prototype
{
    [Serializable]
    public class WeaponInstance
    {
        private const int MaxLevel = 10;

        [SerializeField] private int rank;
        [SerializeField] private int level;
        [SerializeField] private int experience;
        [SerializeField] private int genericAbilitySlotCount;
        [SerializeField] private int growthRatePercent;
        [SerializeField] private StatBlock levelOneStats;
        [SerializeField] private AbilityDefinition uniqueAbility;

        public WeaponInstance(
            int rank,
            int level,
            int genericAbilitySlotCount,
            int growthRatePercent,
            StatBlock levelOneStats,
            AbilityDefinition uniqueAbility)
        {
            this.rank = Mathf.Clamp(rank, 1, 20);
            this.level = Mathf.Clamp(level, 1, MaxLevel);
            this.genericAbilitySlotCount = Mathf.Clamp(genericAbilitySlotCount, 1, 4);
            this.growthRatePercent = Mathf.Clamp(growthRatePercent, 1, 200);
            this.levelOneStats = levelOneStats?.Clone() ?? StatBlock.Zero;
            this.uniqueAbility = uniqueAbility;
        }

        public int Rank => rank;
        public int Level => level;
        public int Experience => experience;
        public int GenericAbilitySlotCount => genericAbilitySlotCount;
        public int GrowthRatePercent => growthRatePercent;
        public AbilityDefinition UniqueAbility => uniqueAbility;
        public StatBlock CurrentStats => levelOneStats.GrowByPercent(growthRatePercent, level - 1);

        public void AddExperience(int amount)
        {
            if (level >= MaxLevel)
            {
                return;
            }

            experience += Mathf.Max(0, amount);
            while (level < MaxLevel && experience >= ExperienceToNextLevel())
            {
                experience -= ExperienceToNextLevel();
                level++;
            }
        }

        public int ExperienceToNextLevel()
        {
            return level * 10;
        }
    }
}
