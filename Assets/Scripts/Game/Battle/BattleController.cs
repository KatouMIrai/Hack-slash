using System;
using System.Collections.Generic;
using UnityEngine;

namespace WeaponMazeAlchemy.Game
{
    public class BattleController
    {
        private readonly TurnManager turnManager = new TurnManager();
        private readonly List<EnemyActor> enemies = new List<EnemyActor>();

        public BattleController(GridMap map, PlayerActor player, EnemyActor enemy)
        {
            Map = map ?? throw new ArgumentNullException(nameof(map));
            Player = player ?? throw new ArgumentNullException(nameof(player));
            if (enemy != null)
            {
                enemies.Add(enemy);
            }
        }

        public event Action<string> LogEmitted;

        public GridMap Map { get; }
        public PlayerActor Player { get; }
        public IReadOnlyList<EnemyActor> Enemies => enemies;
        public bool IsBattleOver => !Player.IsAlive;

        public bool TryFacePlayer(Direction direction)
        {
            if (!turnManager.IsPlayerTurn || IsBattleOver || Player.Direction == direction)
            {
                return false;
            }

            Player.Face(direction);
            Log($"プレイヤーは{direction.ToDisplayText()}を向いた");
            return true;
        }

        public bool TryMovePlayer(Direction direction)
        {
            if (!turnManager.IsPlayerTurn || IsBattleOver)
            {
                return false;
            }

            Player.Face(direction);
            GridPosition target = Player.Position + direction.ToGridOffset();
            if (Map.IsWall(target))
            {
                Log("壁があるため移動できない。向きだけ変更した");
                return true;
            }

            if (GetLivingEnemyAt(target) != null)
            {
                Log("敵がいるため移動できない。向きだけ変更した");
                return true;
            }

            return turnManager.TryRunPlayerAction(
                () =>
                {
                    Player.MoveTo(target);
                    Log($"プレイヤーは{direction.ToDisplayText()}へ移動した");
                    return true;
                },
                RunEnemyTurn);
        }

        public bool TryPlayerNormalAttack()
        {
            if (!turnManager.IsPlayerTurn || IsBattleOver)
            {
                return false;
            }

            return turnManager.TryRunPlayerAction(
                () =>
                {
                    GridPosition target = Player.Position + Player.Direction.ToGridOffset();
                    EnemyActor enemy = GetLivingEnemyAt(target);
                    if (enemy == null)
                    {
                        Log("プレイヤーの通常攻撃！ しかし前方に敵はいない");
                        return true;
                    }

                    DealDamage(Player, enemy, "通常攻撃");
                    return true;
                },
                RunEnemyTurn);
        }

        private void RunEnemyTurn()
        {
            if (IsBattleOver)
            {
                return;
            }

            foreach (EnemyActor enemy in enemies)
            {
                if (!enemy.IsAlive || !Player.IsAlive)
                {
                    continue;
                }

                if (enemy.ManhattanDistanceTo(Player) == 1)
                {
                    FaceTarget(enemy, Player.Position);
                    DealDamage(enemy, Player, "攻撃");
                    continue;
                }

                TryMoveEnemyTowardPlayer(enemy);
            }

            if (!Player.IsAlive)
            {
                Log("プレイヤーは倒れた");
            }
        }

        private void TryMoveEnemyTowardPlayer(EnemyActor enemy)
        {
            foreach (Direction direction in BuildTowardPlayerCandidates(enemy))
            {
                GridPosition target = enemy.Position + direction.ToGridOffset();
                if (!CanEnemyMoveTo(target))
                {
                    continue;
                }

                enemy.Face(direction);
                enemy.MoveTo(target);
                Log($"{enemy.ActorName}は{direction.ToDisplayText()}へ移動した");
                return;
            }

            Log($"{enemy.ActorName}は動けない");
        }

        private IEnumerable<Direction> BuildTowardPlayerCandidates(EnemyActor enemy)
        {
            int dx = Player.Position.X - enemy.Position.X;
            int dy = Player.Position.Y - enemy.Position.Y;

            Direction primary = Mathf.Abs(dx) >= Mathf.Abs(dy)
                ? dx >= 0 ? Direction.Right : Direction.Left
                : dy >= 0 ? Direction.Up : Direction.Down;
            Direction secondary = primary == Direction.Left || primary == Direction.Right
                ? dy >= 0 ? Direction.Up : Direction.Down
                : dx >= 0 ? Direction.Right : Direction.Left;

            yield return primary;
            if (secondary != primary)
            {
                yield return secondary;
            }

            yield return Direction.Up;
            yield return Direction.Right;
            yield return Direction.Down;
            yield return Direction.Left;
        }

        private bool CanEnemyMoveTo(GridPosition position)
        {
            return Map.IsWalkable(position)
                && Player.Position != position
                && GetLivingEnemyAt(position) == null;
        }

        private EnemyActor GetLivingEnemyAt(GridPosition position)
        {
            foreach (EnemyActor enemy in enemies)
            {
                if (enemy.IsAlive && enemy.Position == position)
                {
                    return enemy;
                }
            }

            return null;
        }

        private static void FaceTarget(Actor actor, GridPosition targetPosition)
        {
            GridPosition delta = targetPosition - actor.Position;
            if (Mathf.Abs(delta.X) >= Mathf.Abs(delta.Y))
            {
                actor.Face(delta.X >= 0 ? Direction.Right : Direction.Left);
            }
            else
            {
                actor.Face(delta.Y >= 0 ? Direction.Up : Direction.Down);
            }
        }

        private void DealDamage(Actor attacker, Actor target, string actionName)
        {
            int damage = DamageCalculator.Calculate(attacker, target);
            target.TakeDamage(damage);
            Log($"{attacker.ActorName}の{actionName}！ {target.ActorName}に{damage}ダメージ");

            if (!target.IsAlive)
            {
                Log($"{target.ActorName}を倒した");
            }
        }

        private void Log(string message)
        {
            LogEmitted?.Invoke(message);
        }
    }
}
