using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;

namespace WeaponMazeAlchemy.Game
{
    public class GameBattleBootstrap : MonoBehaviour
    {
        private const string TargetSceneName = "TestScene";
        private const int MaxLogLines = 12;

        [SerializeField] private EnemyActorDefinition enemyDefinition = null;

        private readonly List<string> logs = new List<string>();
        private BattleController battle;
        private GameBattleView view;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (!IsTargetScene())
            {
                return;
            }

            if (FindFirstObjectByType<GameBattleBootstrap>() != null)
            {
                return;
            }

            GameObject bootstrapObject = new GameObject("Game Battle Bootstrap");
            bootstrapObject.AddComponent<GameBattleBootstrap>();
        }

        private void Start()
        {
            if (!IsTargetScene())
            {
                Destroy(gameObject);
                return;
            }

            view = gameObject.AddComponent<GameBattleView>();
            SetupBattle();
        }

        private static bool IsTargetScene()
        {
            return SceneManager.GetActiveScene().name == TargetSceneName;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (WasPressed(keyboard.rKey))
            {
                SetupBattle();
                return;
            }

            if (battle == null || battle.IsBattleOver)
            {
                return;
            }

            bool acted = false;
            if (TryGetPressedDirection(keyboard, out Direction pressedDirection))
            {
                acted = IsRotateModifierPressed(keyboard)
                    ? battle.TryFacePlayer(pressedDirection)
                    : battle.TryMovePlayer(pressedDirection);
            }
            else if (WasRotateModifierPressed(keyboard) && TryGetHeldDirection(keyboard, out Direction heldDirection))
            {
                acted = battle.TryFacePlayer(heldDirection);
            }
            else if (WasPressed(keyboard.spaceKey))
            {
                acted = battle.TryPlayerNormalAttack();
            }

            if (acted)
            {
                view.Render();
            }
        }

        private void OnGUI()
        {
            if (battle == null)
            {
                return;
            }

            float width = 420f;
            float height = 310f;
            Rect panelRect = new Rect(10f, 10f, width, height);
            GUI.Box(panelRect, string.Empty);
            GUILayout.BeginArea(new Rect(panelRect.x + 10f, panelRect.y + 8f, width - 20f, height - 16f));
            GUILayout.Label("正式実装テスト: 移動 / ターン / 通常攻撃");
            GUILayout.Label("WASD/矢印: 移動   Shift+方向: その場回転   Space: 通常攻撃   R: リセット");
            GUILayout.Space(6f);

            PlayerActor player = battle.Player;
            GUILayout.Label($"Player HP {player.CurrentHp}/{player.MaxHp}  ATK {player.Attack}  DEF {player.Defense}  Dir {player.Direction.ToDisplayText()}");
            foreach (EnemyActor enemy in battle.Enemies)
            {
                string state = enemy.IsAlive
                    ? $"HP {enemy.CurrentHp}/{enemy.MaxHp}  ATK {enemy.Attack}  DEF {enemy.Defense}  Move {enemy.MoveType}"
                    : "撃破";
                GUILayout.Label($"{enemy.ActorName}: {state}");
            }

            GUILayout.Space(6f);
            GUILayout.Label("Log");
            foreach (string line in logs)
            {
                GUILayout.Label(line);
            }

            GUILayout.EndArea();
        }

        private void SetupBattle()
        {
            logs.Clear();
            EnemyActorDefinition definition = enemyDefinition != null ? enemyDefinition : CreateRuntimeEnemyDefinition();

            GridMap map = new GridMap(10, 7);
            GridPosition playerPosition = new GridPosition(1, 5);
            GridPosition enemyPosition = new GridPosition(7, 3);
            BuildFixedMap(map);

            PlayerActor player = new PlayerActor(playerPosition);
            EnemyActor enemy = new EnemyActor(definition, enemyPosition);
            battle = new BattleController(map, player, enemy);
            battle.LogEmitted += AddLog;

            AddLog("正式実装テストを開始");
            AddLog($"敵定義SO: {definition.DisplayName} / {definition.MoveType}");
            view.Bind(battle);
            SetupCamera(map);
        }

        private static EnemyActorDefinition CreateRuntimeEnemyDefinition()
        {
            EnemyActorDefinition definition = ScriptableObject.CreateInstance<EnemyActorDefinition>();
            definition.InitializeRuntime("Test Slime", EnemyMoveType.Standard, 36, 9, 2);
            return definition;
        }

        private static void BuildFixedMap(GridMap map)
        {
            string[] rows =
            {
                "##########",
                "#P.......#",
                "#..##....#",
                "#.....E..#",
                "#........#",
                "#........#",
                "##########"
            };

            for (int row = 0; row < rows.Length; row++)
            {
                int y = rows.Length - 1 - row;
                for (int x = 0; x < rows[row].Length; x++)
                {
                    if (rows[row][x] == '#')
                    {
                        map.SetWall(new GridPosition(x, y), true);
                    }
                }
            }
        }

        private static void SetupCamera(GridMap map)
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                camera = cameraObject.AddComponent<Camera>();
                camera.tag = "MainCamera";
            }

            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(map.Width, map.Height) * 0.6f;
            camera.transform.position = new Vector3((map.Width - 1) * 0.5f, (map.Height - 1) * 0.5f, -10f);
        }

        private void AddLog(string message)
        {
            logs.Add(message);
            while (logs.Count > MaxLogLines)
            {
                logs.RemoveAt(0);
            }
        }

        private static bool TryGetPressedDirection(Keyboard keyboard, out Direction direction)
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

            direction = Direction.Down;
            return false;
        }

        private static bool TryGetHeldDirection(Keyboard keyboard, out Direction direction)
        {
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            {
                direction = Direction.Up;
                return true;
            }

            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            {
                direction = Direction.Down;
                return true;
            }

            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            {
                direction = Direction.Left;
                return true;
            }

            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            {
                direction = Direction.Right;
                return true;
            }

            direction = Direction.Down;
            return false;
        }

        private static bool IsRotateModifierPressed(Keyboard keyboard)
        {
            return IsPressed(keyboard.leftShiftKey) || IsPressed(keyboard.rightShiftKey);
        }

        private static bool WasRotateModifierPressed(Keyboard keyboard)
        {
            return WasPressed(keyboard.leftShiftKey) || WasPressed(keyboard.rightShiftKey);
        }

        private static bool IsPressed(ButtonControl key)
        {
            return key != null && key.isPressed;
        }

        private static bool WasPressed(ButtonControl key)
        {
            return key != null && key.wasPressedThisFrame;
        }
    }
}
