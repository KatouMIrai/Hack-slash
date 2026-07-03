using System;
using UnityEngine;

namespace WeaponMazeAlchemy.Prototype
{
    public abstract class Actor
    {
        protected Actor(string actorName, Faction faction, GridPosition position, Direction direction, StatBlock baseStats)
        {
            ActorName = actorName;
            Faction = faction;
            Position = position;
            Direction = direction;
            BaseStats = baseStats?.Clone() ?? StatBlock.Zero;
            CurrentHp = MaxHp;
            CurrentMp = MaxMp;
        }

        public string ActorName { get; }
        public Faction Faction { get; }
        public GridPosition Position { get; private set; }
        public Direction Direction { get; private set; }
        public StatBlock BaseStats { get; }
        public int CurrentHp { get; private set; }
        public int CurrentMp { get; private set; }
        public bool IsAlive => CurrentHp > 0;
        public int MaxHp => Mathf.Max(1, GetTotalStats().Hp);
        public int MaxMp => Mathf.Max(0, GetTotalStats().Mp);

        public virtual StatBlock GetTotalStats()
        {
            return BaseStats.Clone();
        }

        public void SetPosition(GridPosition position)
        {
            Position = position;
        }

        public void Face(Direction direction)
        {
            Direction = direction;
        }

        public int TakeDamage(int amount)
        {
            int applied = Mathf.Max(0, amount);
            CurrentHp = Mathf.Max(0, CurrentHp - applied);
            return applied;
        }

        public int Heal(int amount)
        {
            int before = CurrentHp;
            CurrentHp = Mathf.Min(MaxHp, CurrentHp + Mathf.Max(0, amount));
            return CurrentHp - before;
        }

        public bool SpendMp(int amount)
        {
            int cost = Mathf.Max(0, amount);
            if (CurrentMp < cost)
            {
                return false;
            }

            CurrentMp -= cost;
            return true;
        }

        public void RestoreVitals()
        {
            CurrentHp = MaxHp;
            CurrentMp = MaxMp;
        }

        public void ClampVitalsToMax()
        {
            CurrentHp = Mathf.Min(CurrentHp, MaxHp);
            CurrentMp = Mathf.Min(CurrentMp, MaxMp);
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

            return Math.Abs(Position.X - other.Position.X) + Math.Abs(Position.Y - other.Position.Y);
        }
    }
}
