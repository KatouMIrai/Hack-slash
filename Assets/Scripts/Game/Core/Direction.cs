using UnityEngine;
using UnityEngine.InputSystem;

namespace WeaponMazeAlchemy.Game
{
    public enum Direction
    {
        Up,
        Down,
        Left,
        Right
    }

    public static class DirectionExtensions
    {
        public static GridPosition ToGridOffset(this Direction direction)
        {
            switch (direction)
            {
                case Direction.Up:
                    return new GridPosition(0, 1);
                case Direction.Down:
                    return new GridPosition(0, -1);
                case Direction.Left:
                    return new GridPosition(-1, 0);
                case Direction.Right:
                    return new GridPosition(1, 0);
                default:
                    return GridPosition.Zero;
            }
        }

        public static Direction Opposite(this Direction direction)
        {
            switch (direction)
            {
                case Direction.Up:
                    return Direction.Down;
                case Direction.Down:
                    return Direction.Up;
                case Direction.Left:
                    return Direction.Right;
                case Direction.Right:
                    return Direction.Left;
                default:
                    return direction;
            }
        }

        public static string ToDisplayText(this Direction direction)
        {
            switch (direction)
            {
                case Direction.Up:
                    return "上";
                case Direction.Down:
                    return "下";
                case Direction.Left:
                    return "左";
                case Direction.Right:
                    return "右";
                default:
                    return direction.ToString();
            }
        }

        public static Quaternion ToRotation(this Direction direction)
        {
            float angle;
            switch (direction)
            {
                case Direction.Up:
                    angle = 0f;
                    break;
                case Direction.Right:
                    angle = -90f;
                    break;
                case Direction.Down:
                    angle = 180f;
                    break;
                case Direction.Left:
                    angle = 90f;
                    break;
                default:
                    angle = 0f;
                    break;
            }

            return Quaternion.Euler(0f, 0f, angle);
        }

        public static bool TryFromInput(Keyboard keyboard, out Direction direction)
        {
            if (keyboard != null)
            {
                if (keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame)
                {
                    direction = Direction.Up;
                    return true;
                }

                if (keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame)
                {
                    direction = Direction.Down;
                    return true;
                }

                if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame)
                {
                    direction = Direction.Left;
                    return true;
                }

                if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame)
                {
                    direction = Direction.Right;
                    return true;
                }
            }

            direction = Direction.Down;
            return false;
        }
    }
}
