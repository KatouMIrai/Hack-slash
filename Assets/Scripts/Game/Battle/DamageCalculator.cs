using UnityEngine;

namespace WeaponMazeAlchemy.Game
{
    public static class DamageCalculator
    {
        public static int Calculate(Actor attacker, Actor defender)
        {
            if (attacker == null || defender == null)
            {
                return 0;
            }

            return Mathf.Max(1, attacker.Attack - defender.Defense);
        }
    }
}
