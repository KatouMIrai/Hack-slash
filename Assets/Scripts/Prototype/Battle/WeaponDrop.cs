namespace WeaponMazeAlchemy.Prototype
{
    public class WeaponDrop
    {
        public WeaponDrop(GridPosition position, WeaponInstance weapon)
        {
            Position = position;
            Weapon = weapon;
        }

        public GridPosition Position { get; }
        public WeaponInstance Weapon { get; }
    }
}
