using UnityEngine;

namespace WeaponMazeAlchemy.Prototype
{
    public enum Direction
    {
        Up,
        Right,
        Down,
        Left
    }

    public static class DirectionExtensions
    {
        public static GridPosition ToGridOffset(this Direction direction)
        {
            return direction switch
            {
                Direction.Up => new GridPosition(0, 1),
                Direction.Right => new GridPosition(1, 0),
                Direction.Down => new GridPosition(0, -1),
                Direction.Left => new GridPosition(-1, 0),
                _ => GridPosition.Zero
            };
        }

        public static Quaternion ToRotation(this Direction direction)
        {
            float angle = direction switch
            {
                Direction.Up => 0f,
                Direction.Right => -90f,
                Direction.Down => 180f,
                Direction.Left => 90f,
                _ => 0f
            };

            return Quaternion.Euler(0f, 0f, angle);
        }
    }
}
