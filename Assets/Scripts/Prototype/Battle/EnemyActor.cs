namespace WeaponMazeAlchemy.Prototype
{
    public class EnemyActor : Actor
    {
        private int slowTurnCount;

        public EnemyActor(string actorName, GridPosition position, StatBlock baseStats, EnemyDefinition definition)
            : base(actorName, Faction.Enemy, position, Direction.Left, baseStats)
        {
            Definition = definition;
        }

        public EnemyDefinition Definition { get; }
        public EnemyKind Kind => Definition.Kind;
        public EnemyAiKind AiKind => Definition.AiKind;

        public bool ShouldSkipActionThisTurn()
        {
            if (AiKind != EnemyAiKind.Slow)
            {
                return false;
            }

            slowTurnCount++;
            return slowTurnCount % 2 == 1;
        }
    }
}
