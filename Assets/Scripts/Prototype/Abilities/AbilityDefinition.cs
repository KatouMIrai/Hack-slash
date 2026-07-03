using UnityEngine;

namespace WeaponMazeAlchemy.Prototype
{
    [CreateAssetMenu(menuName = "Weapon Maze Prototype/Ability Definition")]
    public class AbilityDefinition : ScriptableObject
    {
        [SerializeField] private string displayName = "Ability";
        [SerializeField, Range(1, 20)] private int rank = 1;
        [SerializeField] private AbilityKind kind;
        [SerializeField] private ElementType elementType = ElementType.Physical;
        [SerializeField, Min(0)] private int mpCost = 0;
        [SerializeField, Min(1)] private int powerPercent = 100;
        [SerializeField, Min(0)] private int range = 1;
        [SerializeField, Min(0)] private int radius = 0;

        public string DisplayName => displayName;
        public int Rank => rank;
        public AbilityKind Kind => kind;
        public ElementType ElementType => elementType;
        public int MpCost => mpCost;
        public int PowerPercent => powerPercent;
        public int Range => range;
        public int Radius => radius;

        public static AbilityDefinition CreateRuntime(
            string displayName,
            int rank,
            AbilityKind kind,
            ElementType elementType,
            int mpCost,
            int powerPercent,
            int range,
            int radius)
        {
            AbilityDefinition definition = CreateInstance<AbilityDefinition>();
            definition.displayName = displayName;
            definition.rank = Mathf.Clamp(rank, 1, 20);
            definition.kind = kind;
            definition.elementType = elementType;
            definition.mpCost = Mathf.Max(0, mpCost);
            definition.powerPercent = Mathf.Max(1, powerPercent);
            definition.range = Mathf.Max(0, range);
            definition.radius = Mathf.Max(0, radius);
            return definition;
        }
    }
}
