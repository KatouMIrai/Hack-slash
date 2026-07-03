namespace WeaponMazeAlchemy.Prototype
{
    public class EnemyActor : Actor
    {
        public EnemyActor(string actorName, GridPosition position, StatBlock baseStats)
            : base(actorName, Faction.Enemy, position, Direction.Left, baseStats)
        {
        }
    }
}
