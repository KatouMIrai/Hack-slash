using System;
using UnityEngine;

namespace WeaponMazeAlchemy.Prototype
{
    [Serializable]
    public class StatBlock
    {
        [SerializeField] private int hp;
        [SerializeField] private int mp;
        [SerializeField] private int attack;
        [SerializeField] private int defense;
        [SerializeField] private int criticalRate;
        [SerializeField] private int criticalDamage;
        [SerializeField] private int elementalDamageBuff;
        [SerializeField] private int evasionRate;

        public StatBlock()
        {
        }

        public StatBlock(
            int hp,
            int mp,
            int attack,
            int defense,
            int criticalRate,
            int criticalDamage,
            int elementalDamageBuff,
            int evasionRate)
        {
            this.hp = hp;
            this.mp = mp;
            this.attack = attack;
            this.defense = defense;
            this.criticalRate = criticalRate;
            this.criticalDamage = criticalDamage;
            this.elementalDamageBuff = elementalDamageBuff;
            this.evasionRate = evasionRate;
        }

        public int Hp => hp;
        public int Mp => mp;
        public int Attack => attack;
        public int Defense => defense;
        public int CriticalRate => criticalRate;
        public int CriticalDamage => criticalDamage;
        public int ElementalDamageBuff => elementalDamageBuff;
        public int EvasionRate => evasionRate;

        public static StatBlock Zero => new StatBlock();

        public StatBlock Add(StatBlock other)
        {
            if (other == null)
            {
                return Clone();
            }

            return new StatBlock(
                hp + other.hp,
                mp + other.mp,
                attack + other.attack,
                defense + other.defense,
                criticalRate + other.criticalRate,
                criticalDamage + other.criticalDamage,
                elementalDamageBuff + other.elementalDamageBuff,
                evasionRate + other.evasionRate);
        }

        public StatBlock GrowByPercent(int growthRatePercent, int levelUps)
        {
            if (levelUps <= 0)
            {
                return Clone();
            }

            float growth = Mathf.Max(0, growthRatePercent) / 100f;
            return new StatBlock(
                hp + Mathf.CeilToInt(hp * growth * levelUps),
                mp + Mathf.CeilToInt(mp * growth * levelUps),
                attack + Mathf.CeilToInt(attack * growth * levelUps),
                defense + Mathf.CeilToInt(defense * growth * levelUps),
                criticalRate + Mathf.CeilToInt(criticalRate * growth * levelUps),
                criticalDamage + Mathf.CeilToInt(criticalDamage * growth * levelUps),
                elementalDamageBuff + Mathf.CeilToInt(elementalDamageBuff * growth * levelUps),
                evasionRate + Mathf.CeilToInt(evasionRate * growth * levelUps));
        }

        public StatBlock Clone()
        {
            return new StatBlock(hp, mp, attack, defense, criticalRate, criticalDamage, elementalDamageBuff, evasionRate);
        }
    }
}
