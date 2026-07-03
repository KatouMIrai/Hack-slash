using System.Collections.Generic;
using System.Linq;

namespace WeaponMazeAlchemy.Prototype
{
    public class GridMap
    {
        private readonly List<Actor> actors = new List<Actor>();
        private readonly HashSet<GridPosition> walls = new HashSet<GridPosition>();

        public GridMap(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public int Width { get; }
        public int Height { get; }
        public IReadOnlyList<Actor> Actors => actors;
        public IReadOnlyCollection<GridPosition> WallPositions => walls;

        public bool IsInBounds(GridPosition position)
        {
            return position.X >= 0 && position.X < Width && position.Y >= 0 && position.Y < Height;
        }

        public Actor GetActorAt(GridPosition position)
        {
            return actors.FirstOrDefault(actor => actor.IsAlive && actor.Position == position);
        }

        public bool IsWall(GridPosition position)
        {
            return !IsInBounds(position) || walls.Contains(position);
        }

        public bool IsWalkable(GridPosition position)
        {
            return IsInBounds(position) && !walls.Contains(position);
        }

        public void SetWall(GridPosition position, bool isWall)
        {
            if (!IsInBounds(position))
            {
                return;
            }

            if (isWall)
            {
                walls.Add(position);
            }
            else
            {
                walls.Remove(position);
            }
        }

        public bool CanMoveTo(GridPosition position)
        {
            return IsWalkable(position) && GetActorAt(position) == null;
        }

        public bool AddActor(Actor actor)
        {
            if (actor == null || !CanMoveTo(actor.Position))
            {
                return false;
            }

            actors.Add(actor);
            return true;
        }

        public bool TryMoveActor(Actor actor, Direction direction)
        {
            if (actor == null || !actor.IsAlive)
            {
                return false;
            }

            actor.Face(direction);
            GridPosition target = actor.Position + direction.ToGridOffset();
            if (!CanMoveTo(target))
            {
                return false;
            }

            actor.SetPosition(target);
            return true;
        }

        public void RemoveDeadActors()
        {
            actors.RemoveAll(actor => !actor.IsAlive);
        }

        public IEnumerable<Actor> GetOpponentsInArea(Actor source, GridPosition center, int radius)
        {
            int safeRadius = System.Math.Max(0, radius);
            foreach (Actor actor in actors)
            {
                if (actor == null || !actor.IsAlive || !actor.IsOpponentOf(source))
                {
                    continue;
                }

                int dx = System.Math.Abs(actor.Position.X - center.X);
                int dy = System.Math.Abs(actor.Position.Y - center.Y);
                if (dx <= safeRadius && dy <= safeRadius)
                {
                    yield return actor;
                }
            }
        }
    }
}
