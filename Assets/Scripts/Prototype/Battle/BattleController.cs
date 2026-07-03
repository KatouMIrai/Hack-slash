using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WeaponMazeAlchemy.Prototype
{
    public class BattleController
    {
        private readonly TurnManager turnManager = new TurnManager();
        private readonly AbilityExecutor abilityExecutor;
        private readonly WeaponGenerator weaponGenerator;
        private readonly List<EnemyActor> enemies = new List<EnemyActor>();
        private readonly List<WeaponDrop> weaponDrops = new List<WeaponDrop>();

        public BattleController(GridMap map, PlayerActor player, IEnumerable<EnemyActor> enemies)
        {
            Map = map;
            Player = player;
            weaponGenerator = new WeaponGenerator();
            abilityExecutor = new AbilityExecutor(Log);

            Map.AddActor(Player);
            foreach (EnemyActor enemy in enemies)
            {
                if (Map.AddActor(enemy))
                {
                    this.enemies.Add(enemy);
                }
            }
        }

        public event Action<string> LogEmitted;

        public GridMap Map { get; }
        public PlayerActor Player { get; }
        public IReadOnlyList<EnemyActor> Enemies => enemies;
        public IReadOnlyList<WeaponDrop> WeaponDrops => weaponDrops;
        public bool IsBattleOver => !Player.IsAlive || enemies.All(enemy => !enemy.IsAlive);

        public static BattleController CreateDefault()
        {
            WeaponGenerator weaponGenerator = new WeaponGenerator();
            WeaponInstance weapon = weaponGenerator.GenerateForDungeon(1, 5);
            StatBlock playerBaseStats = new StatBlock(100, 30, 10, 5, 5, 150, 0, 0);
            PlayerActor player = new PlayerActor(new GridPosition(1, 1), playerBaseStats, weapon);

            List<EnemyActor> enemies = new List<EnemyActor>
            {
                new EnemyActor("Slime A", new GridPosition(5, 1), new StatBlock(45, 0, 9, 2, 0, 100, 0, 0)),
                new EnemyActor("Slime B", new GridPosition(5, 4), new StatBlock(55, 0, 11, 3, 0, 100, 0, 0))
            };

            BattleController battle = new BattleController(new GridMap(8, 6), player, enemies);
            battle.Log($"Weapon generated: Rank {weapon.Rank}, Lv {weapon.Level}, Ability {weapon.UniqueAbility.DisplayName}");
            battle.Log($"Stats with weapon: HP {player.MaxHp}, MP {player.MaxMp}, ATK {player.GetTotalStats().Attack}, DEF {player.GetTotalStats().Defense}");
            return battle;
        }

        public bool TryMovePlayer(Direction direction)
        {
            return turnManager.TryRunPlayerAction(
                () =>
                {
                    Player.Face(direction);
                    bool moved = Map.TryMoveActor(Player, direction);
                    Log(moved ? $"Player moves {direction}." : $"Player cannot move {direction}.");
                    if (moved)
                    {
                        TryPickupWeaponAtPlayerPosition();
                    }

                    return moved;
                },
                RunEnemyTurn);
        }

        public bool TryFacePlayer(Direction direction)
        {
            if (IsBattleOver || Player.Direction == direction)
            {
                return false;
            }

            Player.Face(direction);
            Log($"Player turns {direction}.");
            return true;
        }

        public bool TryPlayerNormalAttack()
        {
            return turnManager.TryRunPlayerAction(
                () =>
                {
                    Actor target = Map.GetActorAt(Player.Position + Player.Direction.ToGridOffset());
                    if (target == null || !target.IsOpponentOf(Player))
                    {
                        Log("Player attacks, but there is no enemy ahead.");
                        return true;
                    }

                    DealDamage(Player, target, 100, ElementType.Physical, "normal attack");
                    CleanupAfterPlayerAction();
                    return true;
                },
                RunEnemyTurn);
        }

        public bool TryUseUniqueAbility()
        {
            return turnManager.TryRunPlayerAction(
                () =>
                {
                    AbilityDefinition ability = Player.EquippedWeapon?.UniqueAbility;
                    bool executed = abilityExecutor.Execute(Player, ability, Map);
                    CleanupAfterPlayerAction();
                    return executed;
                },
                RunEnemyTurn);
        }

        private void RunEnemyTurn()
        {
            if (IsBattleOver)
            {
                AnnounceBattleState();
                return;
            }

            foreach (EnemyActor enemy in enemies.Where(enemy => enemy.IsAlive).ToList())
            {
                if (!Player.IsAlive)
                {
                    break;
                }

                if (enemy.ManhattanDistanceTo(Player) == 1)
                {
                    FaceTarget(enemy, Player.Position);
                    DealDamage(enemy, Player, 100, ElementType.Physical, "claw");
                    continue;
                }

                TryMoveEnemyTowardPlayer(enemy);
            }

            AnnounceBattleState();
        }

        private void TryMoveEnemyTowardPlayer(EnemyActor enemy)
        {
            Direction primary = Math.Abs(Player.Position.X - enemy.Position.X) >= Math.Abs(Player.Position.Y - enemy.Position.Y)
                ? Player.Position.X >= enemy.Position.X ? Direction.Right : Direction.Left
                : Player.Position.Y >= enemy.Position.Y ? Direction.Up : Direction.Down;
            Direction secondary = primary == Direction.Left || primary == Direction.Right
                ? Player.Position.Y >= enemy.Position.Y ? Direction.Up : Direction.Down
                : Player.Position.X >= enemy.Position.X ? Direction.Right : Direction.Left;

            if (Map.TryMoveActor(enemy, primary))
            {
                Log($"{enemy.ActorName} moves {primary}.");
                return;
            }

            if (Map.TryMoveActor(enemy, secondary))
            {
                Log($"{enemy.ActorName} moves {secondary}.");
            }
        }

        private void DealDamage(Actor attacker, Actor target, int powerPercent, ElementType elementType, string actionName)
        {
            DamageResult result = DamageCalculator.Calculate(attacker, target, powerPercent, elementType);
            if (result.IsEvaded)
            {
                Log($"{target.ActorName} evades {attacker.ActorName}'s {actionName}.");
                return;
            }

            target.TakeDamage(result.Amount);
            string criticalText = result.IsCritical ? " Critical!" : string.Empty;
            Log($"{attacker.ActorName} uses {actionName} on {target.ActorName}: {result.Amount} damage.{criticalText}");
            CleanupAfterPlayerAction();
        }

        private void CleanupAfterPlayerAction()
        {
            foreach (EnemyActor defeated in enemies.Where(enemy => !enemy.IsAlive && Map.Actors.Contains(enemy)).ToList())
            {
                Log($"{defeated.ActorName} is defeated.");
                Player.EquippedWeapon?.AddExperience(10);
                DropWeapon(defeated.Position);
            }

            Map.RemoveDeadActors();
        }

        private void DropWeapon(GridPosition position)
        {
            WeaponInstance weapon = weaponGenerator.GenerateForDungeon(1, 5);
            WeaponDrop drop = new WeaponDrop(position, weapon);
            weaponDrops.Add(drop);
            Log($"Weapon dropped: Rank {weapon.Rank}, Lv {weapon.Level}, ATK {weapon.CurrentStats.Attack}, Ability {weapon.UniqueAbility.DisplayName}.");
        }

        private void TryPickupWeaponAtPlayerPosition()
        {
            WeaponDrop drop = weaponDrops.FirstOrDefault(weaponDrop => weaponDrop.Position == Player.Position);
            if (drop == null)
            {
                return;
            }

            Player.Equip(drop.Weapon);
            weaponDrops.Remove(drop);
            WeaponInstance weapon = Player.EquippedWeapon;
            Log($"Equipped weapon: Rank {weapon.Rank}, Lv {weapon.Level}, ATK {weapon.CurrentStats.Attack}, Ability {weapon.UniqueAbility.DisplayName}.");
        }

        private void FaceTarget(Actor actor, GridPosition targetPosition)
        {
            GridPosition delta = targetPosition - actor.Position;
            if (Math.Abs(delta.X) >= Math.Abs(delta.Y))
            {
                actor.Face(delta.X >= 0 ? Direction.Right : Direction.Left);
            }
            else
            {
                actor.Face(delta.Y >= 0 ? Direction.Up : Direction.Down);
            }
        }

        private void AnnounceBattleState()
        {
            if (!Player.IsAlive)
            {
                Log("Player is defeated.");
                return;
            }

            if (enemies.All(enemy => !enemy.IsAlive))
            {
                Log("All enemies defeated.");
            }
        }

        private void Log(string message)
        {
            LogEmitted?.Invoke(message);
        }
    }
}
