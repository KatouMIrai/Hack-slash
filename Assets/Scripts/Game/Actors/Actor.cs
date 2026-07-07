using UnityEngine;

namespace WeaponMazeAlchemy.Game
{
    public abstract class Actor
    {
        protected Actor(string actorName, Faction faction, GridPosition position, Direction direction, int maxHp, int attack, int defense)
        {
            ActorName = actorName;
            Faction = faction;
            Position = position;
            Direction = direction;
            MaxHp = Mathf.Max(1, maxHp);
            CurrentHp = MaxHp;
            Attack = Mathf.Max(0, attack);
            Defense = Mathf.Max(0, defense);
        }

        public string ActorName { get; }
        public Faction Faction { get; }
        public GridPosition Position { get; private set; }
        public Direction Direction { get; private set; }
        public int CurrentHp { get; private set; }
        public int MaxHp { get; }
        public int Attack { get; }
        public int Defense { get; }
        public bool IsAlive => CurrentHp > 0;

        public void Face(Direction direction)
        {
            Direction = direction;
        }

        public void MoveTo(GridPosition position)
        {
            Position = position;
        }

        public int TakeDamage(int amount)
        {
            int applied = Mathf.Max(0, amount);
            CurrentHp = Mathf.Max(0, CurrentHp - applied);
            return applied;
        }

        public bool IsOpponentOf(Actor other)
        {
            return other != null && Faction != other.Faction;
        }

        public int ManhattanDistanceTo(Actor other)
        {
            if (other == null)
            {
                return int.MaxValue;
            }

            return Mathf.Abs(Position.X - other.Position.X) + Mathf.Abs(Position.Y - other.Position.Y);
        }
    }
}
