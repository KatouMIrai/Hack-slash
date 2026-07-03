namespace WeaponMazeAlchemy.Prototype
{
    public class PlayerActor : Actor
    {
        public PlayerActor(GridPosition position, StatBlock baseStats, WeaponInstance equippedWeapon)
            : base("プレイヤー", Faction.Player, position, Direction.Right, baseStats)
        {
            EquippedWeapon = equippedWeapon;
            RestoreVitals();
        }

        public WeaponInstance EquippedWeapon { get; private set; }

        public override StatBlock GetTotalStats()
        {
            return EquippedWeapon != null
                ? BaseStats.Add(EquippedWeapon.CurrentStats)
                : BaseStats.Clone();
        }

        public void Equip(WeaponInstance weapon)
        {
            EquippedWeapon = weapon;
            ClampVitalsToMax();
        }
    }
}
