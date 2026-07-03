using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WeaponMazeAlchemy.Prototype
{
    public class WeaponGenerator
    {
        private readonly WeaponStatRangeTable statRangeTable;
        private readonly List<AbilityDefinition> abilityDefinitions;

        public WeaponGenerator(WeaponStatRangeTable statRangeTable = null, IEnumerable<AbilityDefinition> abilityDefinitions = null)
        {
            this.statRangeTable = statRangeTable != null ? statRangeTable : WeaponStatRangeTable.CreateRuntimeDefault();
            this.abilityDefinitions = abilityDefinitions != null
                ? abilityDefinitions.Where(ability => ability != null).ToList()
                : CreateDefaultAbilities();
        }

        public WeaponInstance GenerateForDungeon(int dungeonDifficulty, int currentFloor)
        {
            int rank = WeaponRankCalculator.RollSpawnRank(dungeonDifficulty, currentFloor);
            return Generate(rank);
        }

        public WeaponInstance Generate(int rank)
        {
            int clampedRank = Mathf.Clamp(rank, 1, 20);
            RankStatRange statRange = statRangeTable.GetRangeForRank(clampedRank);
            StatBlock stats = statRange.RollStats();
            AbilityDefinition ability = RollAbilityForWeaponRank(clampedRank);
            int slots = Mathf.Clamp(Mathf.CeilToInt(clampedRank / 5f), 1, 4);
            int growthRate = UnityEngine.Random.Range(1, 201);

            return new WeaponInstance(clampedRank, 1, slots, growthRate, stats, ability);
        }

        private AbilityDefinition RollAbilityForWeaponRank(int weaponRank)
        {
            if (abilityDefinitions.Count == 0)
            {
                abilityDefinitions.AddRange(CreateDefaultAbilities());
            }

            List<AbilityDefinition> candidates = abilityDefinitions
                .Where(ability => Mathf.Abs(ability.Rank - weaponRank) <= 5)
                .ToList();

            if (candidates.Count == 0)
            {
                candidates = abilityDefinitions;
            }

            return candidates[UnityEngine.Random.Range(0, candidates.Count)];
        }

        private static List<AbilityDefinition> CreateDefaultAbilities()
        {
            return new List<AbilityDefinition>
            {
                AbilityDefinition.CreateRuntime("Forward Strike", 1, AbilityKind.ForwardAttack, ElementType.Physical, 3, 140, 1, 0),
                AbilityDefinition.CreateRuntime("Flame Ring", 3, AbilityKind.AreaAttack, ElementType.Fire, 6, 90, 0, 1),
                AbilityDefinition.CreateRuntime("First Aid", 1, AbilityKind.Heal, ElementType.Heal, 4, 120, 0, 0)
            };
        }
    }
}
