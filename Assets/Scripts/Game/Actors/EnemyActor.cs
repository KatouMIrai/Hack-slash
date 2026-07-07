namespace WeaponMazeAlchemy.Game
{
    public class EnemyActor : Actor
    {
        public EnemyActor(EnemyActorDefinition definition, GridPosition position)
            : base(
                definition != null ? definition.DisplayName : "Enemy",
                Faction.Enemy,
                position,
                Direction.Left,
                definition != null ? definition.MaxHp : 30,
                definition != null ? definition.Attack : 8,
                definition != null ? definition.Defense : 2)
        {
            Definition = definition;
        }

        public EnemyActorDefinition Definition { get; }
        public EnemyMoveType MoveType => Definition != null ? Definition.MoveType : EnemyMoveType.Standard;
    }
}
