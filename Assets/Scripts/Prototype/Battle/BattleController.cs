using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WeaponMazeAlchemy.Prototype
{
    public class BattleController
    {
        private const int DungeonDifficulty = 1;
        private const int StartingFloor = 1;
        private const int EnemyHpFloorGrowth = 5;
        private const int EnemyAttackFloorGrowth = 2;
        private const int EnemyDefenseFloorGrowth = 1;
        private const int InventoryWeaponCapacity = 3;

        private readonly TurnManager turnManager = new TurnManager();
        private readonly AbilityExecutor abilityExecutor;
        private readonly WeaponGenerator weaponGenerator;
        private readonly List<EnemyActor> enemies = new List<EnemyActor>();
        private readonly List<WeaponDrop> weaponDrops = new List<WeaponDrop>();
        private readonly PlayerInventory inventory = new PlayerInventory(InventoryWeaponCapacity);
        private bool floorClearedLogged;
        private string currentTemplateName;
        private WeaponDrop pendingWeaponDrop;

        public BattleController(GridMap map, PlayerActor player, IEnumerable<EnemyActor> enemies, GridPosition stairsPosition, int currentFloor, string currentTemplateName)
        {
            Map = map;
            Player = player;
            StairsPosition = stairsPosition;
            CurrentFloor = currentFloor;
            this.currentTemplateName = currentTemplateName;
            weaponGenerator = new WeaponGenerator();
            abilityExecutor = new AbilityExecutor(Log);
            InitializeInventoryFromEquippedWeapon();

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
        public event Action FloorChanged;

        public GridMap Map { get; private set; }
        public PlayerActor Player { get; }
        public int CurrentFloor { get; private set; }
        public GridPosition StairsPosition { get; private set; }
        public string CurrentTemplateName => currentTemplateName;
        public IReadOnlyList<EnemyActor> Enemies => enemies;
        public IReadOnlyList<WeaponDrop> WeaponDrops => weaponDrops;
        public IReadOnlyList<WeaponInstance> InventoryWeapons => inventory.Weapons;
        public int MaxInventoryWeaponCount => inventory.MaxWeaponCount;
        public int EquippedInventoryWeaponIndex => inventory.EquippedWeaponIndex;
        public WeaponDrop PendingWeaponDrop => pendingWeaponDrop;
        public bool IsWeaponChoiceActive => pendingWeaponDrop != null;
        public bool IsBattleOver => !Player.IsAlive;

        public static BattleController CreateDefault()
        {
            FloorBuildResult floor = BuildFloor(StartingFloor);
            WeaponGenerator weaponGenerator = new WeaponGenerator();
            WeaponInstance weapon = weaponGenerator.GenerateForDungeon(DungeonDifficulty, StartingFloor);
            StatBlock playerBaseStats = new StatBlock(100, 30, 10, 5, 5, 150, 0, 0);
            PlayerActor player = new PlayerActor(floor.PlayerPosition, playerBaseStats, weapon);
            BattleController battle = new BattleController(floor.Map, player, floor.Enemies, floor.StairsPosition, StartingFloor, floor.Template.Name);
            battle.Log($"初期武器: ランク {weapon.Rank}, Lv {weapon.Level}, 固有アビリティ {weapon.UniqueAbility.DisplayName}");
            battle.Log($"現在ステータス: HP {player.MaxHp}, MP {player.MaxMp}, 攻撃力 {player.GetTotalStats().Attack}, 防御力 {player.GetTotalStats().Defense}");
            battle.Log($"Floor {battle.CurrentFloor}: フロアテンプレート「{floor.Template.Name}」を生成した。S は階段");
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
                Log("壁があるため移動できない。向きだけ変更した");
                return true;
            }

            bool moved = Map.TryMoveActor(Player, direction);
            Log(moved ? $"プレイヤーは {direction} に移動した" : $"そのマスには誰かがいるため移動できない。向きだけ変更した");
            if (!moved)
            {
                return true;
            }

            BeginWeaponChoiceAtPlayerPosition();
            if (Player.Position == StairsPosition)
            {
                AdvanceToNextFloor();
                return true;
            }

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

                ExecuteEnemyAction(enemy);
            }

            AnnounceBattleState();
        }

        private void ExecuteEnemyAction(EnemyActor enemy)
        {
            if (enemy.ShouldSkipActionThisTurn())
            {
                Log($"{enemy.ActorName}は様子を見ている");
                return;
            }

            if (enemy.ManhattanDistanceTo(Player) == 1)
            {
                FaceTarget(enemy, Player.Position);
                DealDamage(enemy, Player, 100, ElementType.Physical, "攻撃");
                return;
            }

            switch (enemy.AiKind)
            {
                case EnemyAiKind.Aggressive:
                    TryMoveAggressiveEnemyTowardPlayer(enemy);
                    break;
                case EnemyAiKind.Standard:
                case EnemyAiKind.Slow:
                default:
                    TryMoveEnemyTowardPlayer(enemy);
                    break;
            }
        }

        private void TryMoveEnemyTowardPlayer(EnemyActor enemy)
        {
            List<Direction> candidates = BuildTowardPlayerDirectionCandidates(enemy);

            foreach (Direction direction in candidates)
            {
                GridPosition targetPosition = enemy.Position + direction.ToGridOffset();
                if (targetPosition == StairsPosition)
                {
                    continue;
                }

                if (Map.TryMoveActor(enemy, direction))
                {
                    Log($"{enemy.ActorName} は {direction} に移動した");
                    return;
                }
            }

            Log($"{enemy.ActorName} は壁や他のキャラに阻まれて動けない");
        }

        private void TryMoveAggressiveEnemyTowardPlayer(EnemyActor enemy)
        {
            List<Direction> candidates = BuildTowardPlayerDirectionCandidates(enemy);
            Direction bestDirection = Direction.Up;
            int bestDistance = int.MaxValue;
            int bestPriority = int.MaxValue;
            bool foundMove = false;

            for (int i = 0; i < candidates.Count; i++)
            {
                Direction direction = candidates[i];
                GridPosition targetPosition = enemy.Position + direction.ToGridOffset();
                if (targetPosition == StairsPosition || !Map.CanMoveTo(targetPosition))
                {
                    continue;
                }

                int distance = GetManhattanDistance(targetPosition, Player.Position);
                if (!foundMove || distance < bestDistance || distance == bestDistance && i < bestPriority)
                {
                    bestDirection = direction;
                    bestDistance = distance;
                    bestPriority = i;
                    foundMove = true;
                }
            }

            if (foundMove && Map.TryMoveActor(enemy, bestDirection))
            {
                Log($"{enemy.ActorName} は {bestDirection} に素早く移動した");
                return;
            }

            Log($"{enemy.ActorName} は壁や他のキャラに阻まれて動けない");
        }

        private List<Direction> BuildTowardPlayerDirectionCandidates(EnemyActor enemy)
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

            return candidates;
        }

        private static void AddDirectionCandidate(List<Direction> candidates, Direction direction)
        {
            if (!candidates.Contains(direction))
            {
                candidates.Add(direction);
            }
        }

        private static int GetManhattanDistance(GridPosition a, GridPosition b)
        {
            return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
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
                DropWeapon(defeated.Position, defeated.ActorName);
            }

            Map.RemoveDeadActors();
        }

        private void DropWeapon(GridPosition position, string defeatedEnemyName)
        {
            if (position == StairsPosition)
            {
                Log("階段の上なので武器はドロップしなかった");
                return;
            }

            WeaponInstance weapon = weaponGenerator.GenerateForDungeon(DungeonDifficulty, CurrentFloor);
            int baseRank = WeaponRankCalculator.GetBaseRank(DungeonDifficulty, CurrentFloor);
            WeaponDrop drop = new WeaponDrop(position, weapon);
            weaponDrops.Add(drop);
            Log($"{defeatedEnemyName}が武器をドロップした(Floor {CurrentFloor}基準 / 基準ランク {baseRank}): {FormatWeaponSummary(weapon)}");
        }

        public bool TryAddPendingWeaponToInventory()
        {
            if (pendingWeaponDrop == null)
            {
                return false;
            }

            if (inventory.IsFull)
            {
                Log($"バッグがいっぱい: {inventory.Count}/{inventory.MaxWeaponCount}");
                return true;
            }

            WeaponInstance weapon = pendingWeaponDrop.Weapon;
            if (!inventory.TryAdd(weapon))
            {
                Log("バッグに武器を入れられなかった");
                return true;
            }

            weaponDrops.Remove(pendingWeaponDrop);
            pendingWeaponDrop = null;
            SyncPlayerEquipmentWithInventory();

            Log($"武器をバッグに入れた [{inventory.Count}/{inventory.MaxWeaponCount}]: {FormatWeaponSummary(weapon)}");
            return true;
        }

        public bool TrySwapPendingWeaponWithInventory(int inventoryIndex)
        {
            if (pendingWeaponDrop == null)
            {
                return false;
            }

            if (!inventory.TryGetWeapon(inventoryIndex, out WeaponInstance bagWeapon))
            {
                Log($"バッグ[{inventoryIndex + 1}]には交換できる武器がない");
                return true;
            }

            WeaponDrop oldDrop = pendingWeaponDrop;
            WeaponInstance droppedWeapon = oldDrop.Weapon;
            if (!inventory.TryReplaceWeapon(inventoryIndex, droppedWeapon, out WeaponInstance replacedWeapon))
            {
                Log($"バッグ[{inventoryIndex + 1}]と交換できなかった");
                return true;
            }

            weaponDrops.Remove(oldDrop);
            WeaponDrop returnedDrop = new WeaponDrop(oldDrop.Position, replacedWeapon);
            weaponDrops.Add(returnedDrop);
            pendingWeaponDrop = null;
            SyncPlayerEquipmentWithInventory();

            Log($"落ちている武器をバッグ[{inventoryIndex + 1}]の武器と交換した");
            Log($"バッグに入った: {FormatWeaponSummary(droppedWeapon)}");
            Log($"床に置いた: {FormatWeaponSummary(bagWeapon)}");
            return true;
        }

        public bool TryEquipInventoryWeapon(int inventoryIndex)
        {
            if (!turnManager.IsPlayerTurn || IsBattleOver || IsWeaponChoiceActive)
            {
                return false;
            }

            if (!inventory.TryGetWeapon(inventoryIndex, out WeaponInstance bagWeapon))
            {
                return false;
            }

            if (!inventory.TryEquip(inventoryIndex))
            {
                return false;
            }

            SyncPlayerEquipmentWithInventory();
            Log($"バッグの武器[{inventoryIndex + 1}]を装備した: {FormatWeaponSummary(bagWeapon)}");
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
            Log("落ちている武器を発見！ Bでバッグ / 1-3でバッグ枠と交換 / Qで拾わない");
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

            if (!floorClearedLogged && enemies.All(enemy => !enemy.IsAlive))
            {
                floorClearedLogged = true;
                Log("すべての敵を倒した！ 階段へ向かおう");
            }
        }

        private void AdvanceToNextFloor()
        {
            CurrentFloor++;
            FloorBuildResult floor = BuildFloor(CurrentFloor);

            Map = floor.Map;
            StairsPosition = floor.StairsPosition;
            currentTemplateName = floor.Template.Name;
            floorClearedLogged = false;
            pendingWeaponDrop = null;
            weaponDrops.Clear();
            enemies.Clear();

            Player.SetPosition(floor.PlayerPosition);
            Player.Face(Direction.Right);
            Player.ClampVitalsToMax();
            Map.AddActor(Player);

            foreach (EnemyActor enemy in floor.Enemies)
            {
                if (Map.AddActor(enemy))
                {
                    enemies.Add(enemy);
                }
            }

            Log($"Floor {CurrentFloor} に進んだ");
            Log($"敵が少し強くなった: HP +{GetEnemyHpFloorBonus(CurrentFloor)}, 攻撃力 +{GetEnemyAttackFloorBonus(CurrentFloor)}, 防御力 +{GetEnemyDefenseFloorBonus(CurrentFloor)}");
            Log($"武器ドロップ基準ランク: {WeaponRankCalculator.GetBaseRank(DungeonDifficulty, CurrentFloor)}");
            Log($"フロアテンプレート「{currentTemplateName}」を生成した。S は階段");
            FloorChanged?.Invoke();
        }

        private static FloorBuildResult BuildFloor(int currentFloor)
        {
            FloorTemplate template = FloorTemplateDatabase.PickRandomDefault();
            GridMap map = new GridMap(template.Width, template.Height);
            GridPosition playerPosition = GridPosition.Zero;
            GridPosition stairsPosition = GridPosition.Zero;
            bool foundPlayer = false;
            bool foundStairs = false;
            List<EnemyActor> enemies = new List<EnemyActor>();

            int enemyIndex = 1;
            for (int row = 0; row < template.Height; row++)
            {
                int y = template.Height - 1 - row;
                string layoutRow = template.Rows[row];
                for (int x = 0; x < layoutRow.Length; x++)
                {
                    GridPosition position = new GridPosition(x, y);
                    char tile = layoutRow[x];
                    if (tile == '#')
                    {
                        map.SetWall(position, true);
                    }
                    else if (tile == 'P')
                    {
                        playerPosition = position;
                        foundPlayer = true;
                    }
                    else if (EnemyDefinition.IsEnemyTile(tile))
                    {
                        enemies.Add(CreateEnemy(enemyIndex, position, currentFloor, EnemyDefinition.FromTemplateTile(tile)));
                        enemyIndex++;
                    }
                    else if (tile == 'S')
                    {
                        stairsPosition = position;
                        foundStairs = true;
                    }
                }
            }

            if (!foundPlayer)
            {
                throw new InvalidOperationException($"Floor template '{template.Name}' does not contain a player start tile 'P'.");
            }

            if (!foundStairs)
            {
                throw new InvalidOperationException($"Floor template '{template.Name}' does not contain a stairs tile 'S'.");
            }

            return new FloorBuildResult(template, map, playerPosition, stairsPosition, enemies);
        }

        private static EnemyActor CreateEnemy(int enemyIndex, GridPosition position, int currentFloor, EnemyDefinition definition)
        {
            StatBlock stats = new StatBlock(
                definition.BaseHp + GetEnemyHpFloorBonus(currentFloor),
                0,
                definition.BaseAttack + GetEnemyAttackFloorBonus(currentFloor),
                definition.BaseDefense + GetEnemyDefenseFloorBonus(currentFloor),
                0,
                100,
                0,
                0);

            return new EnemyActor($"{definition.DisplayName}{enemyIndex}", position, stats, definition);
        }

        private static int GetEnemyHpFloorBonus(int currentFloor)
        {
            return GetFloorBonus(currentFloor) * EnemyHpFloorGrowth;
        }

        private static int GetEnemyAttackFloorBonus(int currentFloor)
        {
            return GetFloorBonus(currentFloor) * EnemyAttackFloorGrowth;
        }

        private static int GetEnemyDefenseFloorBonus(int currentFloor)
        {
            return GetFloorBonus(currentFloor) * EnemyDefenseFloorGrowth;
        }

        private static int GetFloorBonus(int currentFloor)
        {
            return Mathf.Max(0, currentFloor - 1);
        }

        private static string FormatWeaponSummary(WeaponInstance weapon)
        {
            if (weapon == null)
            {
                return "なし";
            }

            string abilityName = weapon.UniqueAbility != null ? weapon.UniqueAbility.DisplayName : "なし";
            return $"ランク {weapon.Rank}, Lv {weapon.Level}, 攻撃力 {weapon.CurrentStats.Attack}, 固有 {abilityName}";
        }

        private void InitializeInventoryFromEquippedWeapon()
        {
            if (Player.EquippedWeapon == null)
            {
                return;
            }

            inventory.TryAdd(Player.EquippedWeapon);
            inventory.TryEquip(0);
            SyncPlayerEquipmentWithInventory();
        }

        private void SyncPlayerEquipmentWithInventory()
        {
            Player.Equip(inventory.EquippedWeapon);
        }

        private void Log(string message)
        {
            LogEmitted?.Invoke(message);
        }

        private class FloorBuildResult
        {
            public FloorBuildResult(FloorTemplate template, GridMap map, GridPosition playerPosition, GridPosition stairsPosition, List<EnemyActor> enemies)
            {
                Template = template;
                Map = map;
                PlayerPosition = playerPosition;
                StairsPosition = stairsPosition;
                Enemies = enemies;
            }

            public FloorTemplate Template { get; }
            public GridMap Map { get; }
            public GridPosition PlayerPosition { get; }
            public GridPosition StairsPosition { get; }
            public List<EnemyActor> Enemies { get; }
        }
    }
}
