using UnityEngine;

namespace WeaponMazeAlchemy.Game
{
    [CreateAssetMenu(fileName = "AbilityDefinition", menuName = "Weapon Maze Alchemy/Game/Ability Definition")]
    public class AbilityDefinition : ScriptableObject
    {
        [SerializeField] private string displayName = "Ability";

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    }
}
