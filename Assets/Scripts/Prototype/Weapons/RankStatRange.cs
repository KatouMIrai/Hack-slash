using System;
using UnityEngine;

namespace WeaponMazeAlchemy.Prototype
{
    [Serializable]
    public class RankStatRange
    {
        [SerializeField, Range(1, 20)] private int minRank = 1;
        [SerializeField, Range(1, 20)] private int maxRank = 5;
        [SerializeField] private IntRange hp = new IntRange(20, 100);
        [SerializeField] private IntRange mp = new IntRange(10, 50);
        [SerializeField] private IntRange attack = new IntRange(5, 50);
        [SerializeField] private IntRange defense = new IntRange(5, 50);
        [SerializeField] private IntRange criticalRate = new IntRange(1, 5);
        [SerializeField] private IntRange criticalDamage = new IntRange(1, 10);
        [SerializeField] private IntRange elementalDamageBuff = new IntRange(1, 10);
        [SerializeField] private IntRange evasionRate = new IntRange(0, 3);

        public RankStatRange()
        {
        }

        public RankStatRange(
            int minRank,
            int maxRank,
            IntRange hp,
            IntRange mp,
            IntRange attack,
            IntRange defense,
            IntRange criticalRate,
            IntRange criticalDamage,
            IntRange elementalDamageBuff,
            IntRange evasionRate)
        {
            this.minRank = minRank;
            this.maxRank = maxRank;
            this.hp = hp;
            this.mp = mp;
            this.attack = attack;
            this.defense = defense;
            this.criticalRate = criticalRate;
            this.criticalDamage = criticalDamage;
            this.elementalDamageBuff = elementalDamageBuff;
            this.evasionRate = evasionRate;
        }

        public bool Contains(int rank)
        {
            return rank >= minRank && rank <= maxRank;
        }

        public StatBlock RollStats()
        {
            return new StatBlock(
                hp.Roll(),
                mp.Roll(),
                attack.Roll(),
                defense.Roll(),
                criticalRate.Roll(),
                criticalDamage.Roll(),
                elementalDamageBuff.Roll(),
                evasionRate.Roll());
        }
    }
}
