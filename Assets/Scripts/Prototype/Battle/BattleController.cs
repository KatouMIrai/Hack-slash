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
        private WeaponDrop pendingWeaponDrop;

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
        public WeaponDrop PendingWeaponDrop => pendingWeaponDrop;
        public bool IsWeaponChoiceActive => pendingWeaponDrop != null;
        public bool IsBattleOver => !Player.IsAlive || enemies.All(enemy => !enemy.IsAlive);

        public static BattleController CreateDefault()
        {
            string[] layout =
            {
                "##########",
                "#P.......#",
                "#..##....#",
                "#........#",
                "#....##..#",
                "#..E...E.#",
                "##########"
            };

            WeaponGenerator weaponGenerator = new WeaponGenerator();
            WeaponInstance weapon = weaponGenerator.GenerateForDungeon(1, 5);
            StatBlock playerBaseStats = new StatBlock(100, 30, 10, 5, 5, 150, 0, 0);
            GridMap map = new GridMap(layout[0].Length, layout.Length);
            GridPosition playerPosition = new GridPosition(1, 1);
            List<EnemyActor> enemies = new List<EnemyActor>();

            int enemyIndex = 1;
            for (int row = 0; row < layout.Length; row++)
            {
                int y = layout.Length - 1 - row;
                for (int x = 0; x < layout[row].Length; x++)
                {
                    GridPosition position = new GridPosition(x, y);
                    char tile = layout[row][x];
                    if (tile == '#')
                    {
                        map.SetWall(position, true);
                    }
                    else if (tile == 'P')
                    {
                        playerPosition = position;
                    }
                    else if (tile == 'E')
                    {
                        enemies.Add(new EnemyActor($"スライム{enemyIndex}", position, new StatBlock(50 + enemyIndex * 5, 0, 9 + enemyIndex, 2 + enemyIndex, 0, 100, 0, 0)));
                        enemyIndex++;
                    }
                }
            }

            PlayerActor player = new PlayerActor(playerPosition, playerBaseStats, weapon);
            BattleController battle = new BattleController(map, player, enemies);
            battle.Log($"初期武器: ランク {weapon.Rank}, Lv {weapon.Level}, 固有アビリティ {weapon.UniqueAbility.DisplayName}");
            battle.Log($"現在ステータス: HP {player.MaxHp}, MP {player.MaxMp}, 攻撃力 {player.GetTotalStats().Attack}, 防御力 {player.GetTotalStats().Defense}");
            battle.Log("固定テスト部屋を生成した。# は壁");
            return battle;
        }

        public bool TryMovePlayer(Direction direction)
        {
            if (!turnManager.IsPlayerTurn || IsBattleOver || IsWeaponChoiceActive)
            {
                return false;
            }

            Player.Face(direction);
            GridPosition targetPosition = Player.Position + direction.ToGridOffset();
            if (Map.IsWall(targetPosition))
            {
                Log("壁があるため移動できない");
                return false;
            }

            bool moved = Map.TryMoveActor(Player, direction);
            Log(moved ? $"プレイヤーは {direction} に移動した" : $"そのマスには誰かがいるため移動できない");
            if (!moved)
            {
                return false;
            }

            BeginWeaponChoiceAtPlayerPosition();
            if (!IsWeaponChoiceActive)
            {
                RunEnemyTurn();
            }

            return true;
        }

        public bool TryFacePlayer(Direction direction)
        {
            if (IsBattleOver || IsWeaponChoiceActive || Player.Direction == direction)
            {
                return false;
            }

            Player.Face(direction);
            Log($"プレイヤーは {direction} を向いた");
            return true;
        }

        public bool TryPlayerNormalAttack()
        {
            if (IsWeaponChoiceActive)
            {
                return false;
            }

            return turnManager.TryRunPlayerAction(
                () =>
                {
                    GridPosition targetPosition = Player.Position + Player.Direction.ToGridOffset();
                    if (Map.IsWall(targetPosition))
                    {
                        Log("プレイヤーの通常攻撃！ しかし壁に阻まれた");
                        return true;
                    }

                    Actor target = Map.GetActorAt(targetPosition);
                    if (target == null || !target.IsOpponentOf(Player))
                    {
                        Log("プレイヤーの通常攻撃！ しかし前方に敵はいない");
                        return true;
                    }

                    DealDamage(Player, target, 100, ElementType.Physical, "通常攻撃");
                    CleanupAfterPlayerAction();
                    return true;
                },
                RunEnemyTurn);
        }

        public bool TryUseUniqueAbility()
        {
            if (IsWeaponChoiceActive)
            {
                return false;
            }

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
                    DealDamage(enemy, Player, 100, ElementType.Physical, "攻撃");
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

            List<Direction> candidates = new List<Direction>();
            AddDirectionCandidate(candidates, primary);
            AddDirectionCandidate(candidates, secondary);
            AddDirectionCandidate(candidates, Direction.Up);
            AddDirectionCandidate(candidates, Direction.Right);
            AddDirectionCandidate(candidates, Direction.Down);
            AddDirectionCandidate(candidates, Direction.Left);

            foreach (Direction direction in candidates)
            {
                if (Map.TryMoveActor(enemy, direction))
                {
                    Log($"{enemy.ActorName} は {direction} に移動した");
                    return;
                }
            }

            Log($"{enemy.ActorName} は壁や他のキャラに阻まれて動けない");
        }

        private static void AddDirectionCandidate(List<Direction> candidates, Direction direction)
        {
            if (!candidates.Contains(direction))
            {
                candidates.Add(direction);
            }
        }

        private void DealDamage(Actor attacker, Actor target, int powerPercent, ElementType elementType, string actionName)
        {
            DamageResult result = DamageCalculator.Calculate(attacker, target, powerPercent, elementType);
            if (result.IsEvaded)
            {
                Log(attacker.Faction == Faction.Player
                    ? $"{target.ActorName} は {attacker.ActorName} の {actionName} を回避！"
                    : $"{attacker.ActorName} の {actionName}！ {target.ActorName} は攻撃を回避！");
                return;
            }

            target.TakeDamage(result.Amount);
            string criticalText = result.IsCritical ? " 会心！" : string.Empty;
            Log(attacker.Faction == Faction.Player
                ? $"{attacker.ActorName}の{actionName}！{criticalText} {target.ActorName}に {result.Amount} ダメージ"
                : $"{attacker.ActorName}の{actionName}！{criticalText} {target.ActorName}に {result.Amount} ダメージ");
            CleanupAfterPlayerAction();
        }

        private void CleanupAfterPlayerAction()
        {
            foreach (EnemyActor defeated in enemies.Where(enemy => !enemy.IsAlive && Map.Actors.Contains(enemy)).ToList())
            {
                Log($"{defeated.ActorName}を倒した！");
                Player.EquippedWeapon?.AddExperience(10);
                Log("装備武器に経験値 +10");
                DropWeapon(defeated.Position);
            }

            Map.RemoveDeadActors();
        }

        private void DropWeapon(GridPosition position)
        {
            WeaponInstance weapon = weaponGenerator.GenerateForDungeon(1, 5);
            WeaponDrop drop = new WeaponDrop(position, weapon);
            weaponDrops.Add(drop);
            Log($"武器がドロップした: ランク {weapon.Rank}, Lv {weapon.Level}, 攻撃力 {weapon.CurrentStats.Attack}, 固有 {weapon.UniqueAbility.DisplayName}");
        }

        public bool TryEquipPendingWeapon()
        {
            if (pendingWeaponDrop == null)
            {
                return false;
            }

            Player.Equip(pendingWeaponDrop.Weapon);
            weaponDrops.Remove(pendingWeaponDrop);
            pendingWeaponDrop = null;

            WeaponInstance weapon = Player.EquippedWeapon;
            Log($"武器を拾って装備した: ランク {weapon.Rank}, Lv {weapon.Level}, 攻撃力 {weapon.CurrentStats.Attack}, 固有 {weapon.UniqueAbility.DisplayName}");
            return true;
        }

        public bool TryCloseWeaponChoice()
        {
            if (pendingWeaponDrop == null)
            {
                return false;
            }

            Log("落ちている武器を拾わず、その場に残した");
            pendingWeaponDrop = null;
            return true;
        }

        private void BeginWeaponChoiceAtPlayerPosition()
        {
            WeaponDrop drop = weaponDrops.FirstOrDefault(weaponDrop => weaponDrop.Position == Player.Position);
            if (drop == null)
            {
                return;
            }

            pendingWeaponDrop = drop;
            Log("落ちている武器を発見！ Eで装備 / Qで拾わない");
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
                Log("プレイヤーは倒れた");
                return;
            }

            if (enemies.All(enemy => !enemy.IsAlive))
            {
                Log("すべての敵を倒した！");
            }
        }

        private void Log(string message)
        {
            LogEmitted?.Invoke(message);
        }
    }
}
