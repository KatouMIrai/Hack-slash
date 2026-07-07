namespace WeaponMazeAlchemy.Game
{
    public class PlayerActor : Actor
    {
        public PlayerActor(GridPosition position)
            : base("Player", Faction.Player, position, Direction.Right, 100, 12, 4)
        {
        }
    }
}
