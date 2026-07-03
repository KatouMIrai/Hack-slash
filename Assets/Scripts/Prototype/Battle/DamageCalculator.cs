using UnityEngine;

namespace WeaponMazeAlchemy.Prototype
{
    public static class DamageCalculator
    {
        public static DamageResult Calculate(Actor attacker, Actor defender, int powerPercent, ElementType elementType)
        {
            StatBlock attackerStats = attacker.GetTotalStats();
            StatBlock defenderStats = defender.GetTotalStats();

            if (RollPercent(defenderStats.EvasionRate))
            {
                return new DamageResult(0, false, true);
            }

            bool isCritical = RollPercent(attackerStats.CriticalRate);
            float attackMultiplier = Mathf.Max(1, powerPercent) / 100f;
            float elementalMultiplier = elementType == ElementType.Physical
                ? 1f
                : 1f + attackerStats.ElementalDamageBuff / 100f;
            float criticalMultiplier = isCritical
                ? Mathf.Max(100, attackerStats.CriticalDamage) / 100f
                : 1f;
            float defenseReduction = attacker.Faction == Faction.Player
                ? defenderStats.Defense / 2f
                : defenderStats.Defense;
            float variance = UnityEngine.Random.Range(0.9f, 1.11f);
            float rawDamage = (attackerStats.Attack * elementalMultiplier * criticalMultiplier * attackMultiplier - defenseReduction) * variance;
            int damage = Mathf.Max(1, Mathf.RoundToInt(rawDamage));

            return new DamageResult(damage, isCritical, false);
        }

        private static bool RollPercent(int percent)
        {
            return UnityEngine.Random.Range(0, 100) < Mathf.Clamp(percent, 0, 100);
        }
    }
}
