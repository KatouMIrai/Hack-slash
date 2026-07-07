using System.Collections.Generic;

namespace WeaponMazeAlchemy.Game
{
    public class GridMap
    {
        private readonly HashSet<GridPosition> walls = new HashSet<GridPosition>();

        public GridMap(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public int Width { get; }
        public int Height { get; }
        public IReadOnlyCollection<GridPosition> WallPositions => walls;

        public bool IsInBounds(GridPosition position)
        {
            return position.X >= 0 && position.X < Width && position.Y >= 0 && position.Y < Height;
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
    }
}
