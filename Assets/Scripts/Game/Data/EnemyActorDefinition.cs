using System.Collections.Generic;
using UnityEngine;

namespace WeaponMazeAlchemy.Game
{
    [CreateAssetMenu(fileName = "EnemyActorDefinition", menuName = "Weapon Maze Alchemy/Game/Enemy Actor Definition")]
    public class EnemyActorDefinition : ScriptableObject
    {
        [SerializeField] private string displayName = "Slime";
        [SerializeField] private EnemyMoveType moveType = EnemyMoveType.Standard;
        [SerializeField] private int maxHp = 30;
        [SerializeField] private int attack = 8;
        [SerializeField] private int defense = 2;
        [SerializeField] private List<AbilityDefinition> abilities = new List<AbilityDefinition>();

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public EnemyMoveType MoveType => moveType;
        public int MaxHp => Mathf.Max(1, maxHp);
        public int Attack => Mathf.Max(0, attack);
        public int Defense => Mathf.Max(0, defense);
        public IReadOnlyList<AbilityDefinition> Abilities => abilities;

        public void InitializeRuntime(string enemyName, EnemyMoveType enemyMoveType, int enemyMaxHp, int enemyAttack, int enemyDefense)
        {
            displayName = enemyName;
            moveType = enemyMoveType;
            maxHp = enemyMaxHp;
            attack = enemyAttack;
            defense = enemyDefense;
            abilities.Clear();
        }
    }
}
