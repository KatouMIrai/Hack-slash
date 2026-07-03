using System;

namespace WeaponMazeAlchemy.Prototype
{
    public class TurnManager
    {
        public bool IsPlayerTurn { get; private set; } = true;

        public bool TryRunPlayerAction(Func<bool> action, Action enemyTurn)
        {
            if (!IsPlayerTurn || action == null)
            {
                return false;
            }

            bool consumedTurn = action.Invoke();
            if (!consumedTurn)
            {
                return false;
            }

            IsPlayerTurn = false;
            enemyTurn?.Invoke();
            IsPlayerTurn = true;
            return true;
        }
    }
}
