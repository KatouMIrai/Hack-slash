using System;
using UnityEngine;

namespace WeaponMazeAlchemy.Prototype
{
    public class EnemyDefinition
    {
        private EnemyDefinition(
            EnemyKind kind,
            string displayName,
            int baseHp,
            int baseAttack,
            int baseDefense,
            EnemyAiKind aiKind,
            Color viewColor)
        {
            Kind = kind;
            DisplayName = displayName;
            BaseHp = baseHp;
            BaseAttack = baseAttack;
            BaseDefense = baseDefense;
            AiKind = aiKind;
            ViewColor = viewColor;
        }

        public EnemyKind Kind { get; }
        public string DisplayName { get; }
        public int BaseHp { get; }
        public int BaseAttack { get; }
        public int BaseDefense { get; }
        public EnemyAiKind AiKind { get; }
        public Color ViewColor { get; }
        public string AiDisplayName
        {
            get
            {
                switch (AiKind)
                {
                    case EnemyAiKind.Standard:
                        return "標準";
                    case EnemyAiKind.Aggressive:
                        return "攻撃的";
                    case EnemyAiKind.Slow:
                        return "鈍重";
                    default:
                        return AiKind.ToString();
                }
            }
        }

        public static EnemyDefinition FromTemplateTile(char tile)
        {
            switch (tile)
            {
                case 'E':
                    return Slime;
                case 'B':
                    return Bat;
                case 'G':
                    return Golem;
                default:
                    throw new ArgumentException($"Unsupported enemy tile '{tile}'.", nameof(tile));
            }
        }

        public static bool IsEnemyTile(char tile)
        {
            return tile == 'E' || tile == 'B' || tile == 'G';
        }

        public static EnemyDefinition Slime { get; } = new EnemyDefinition(
            EnemyKind.Slime,
            "Slime",
            55,
            10,
            3,
            EnemyAiKind.Standard,
            new Color(0.25f, 0.85f, 0.35f));

        public static EnemyDefinition Bat { get; } = new EnemyDefinition(
            EnemyKind.Bat,
            "Bat",
            38,
            15,
            1,
            EnemyAiKind.Aggressive,
            new Color(0.68f, 0.35f, 0.95f));

        public static EnemyDefinition Golem { get; } = new EnemyDefinition(
            EnemyKind.Golem,
            "Golem",
            85,
            7,
            8,
            EnemyAiKind.Slow,
            new Color(0.55f, 0.58f, 0.6f));
    }
}
