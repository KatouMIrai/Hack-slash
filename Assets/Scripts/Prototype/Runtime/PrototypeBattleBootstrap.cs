using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace WeaponMazeAlchemy.Prototype
{
    public class PrototypeBattleBootstrap : MonoBehaviour
    {
        private const int MaxLogLines = 24;
        private const int VisibleLogLines = 10;
        private const float LogLineHeight = 20f;

        private readonly List<string> logLines = new List<string>();
        private BattleController battle;
        private PrototypeBattleView view;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (FindFirstObjectByType<PrototypeBattleBootstrap>() != null)
            {
                return;
            }

            GameObject bootstrapObject = new GameObject("Prototype Battle Bootstrap");
            bootstrapObject.AddComponent<PrototypeBattleBootstrap>();
        }

        private void Start()
        {
            view = gameObject.AddComponent<PrototypeBattleView>();
            SetupBattle();
        }

        private void Update()
        {
            if (Keyboard.current == null)
            {
                return;
            }

            if (WasPressed(Keyboard.current.rKey))
            {
                SetupBattle();
                return;
            }

            if (battle == null || battle.IsBattleOver)
            {
                return;
            }

            if (battle.IsWeaponChoiceActive)
            {
                bool choiceChanged = false;
                if (WasPressed(Keyboard.current.eKey))
                {
                    choiceChanged = battle.TryEquipPendingWeapon();
                }
                else if (WasPressed(Keyboard.current.qKey))
                {
                    choiceChanged = battle.TryCloseWeaponChoice();
                }

                if (choiceChanged)
                {
                    view.Render();
                }

                return;
            }

            bool rotateOnly = IsPressed(Keyboard.current.leftShiftKey) || IsPressed(Keyboard.current.rightShiftKey);
            bool acted = false;
            if (WasPressed(Keyboard.current.wKey) || WasPressed(Keyboard.current.upArrowKey))
            {
                acted = rotateOnly ? battle.TryFacePlayer(Direction.Up) : battle.TryMovePlayer(Direction.Up);
            }
            else if (WasPressed(Keyboard.current.dKey) || WasPressed(Keyboard.current.rightArrowKey))
            {
                acted = rotateOnly ? battle.TryFacePlayer(Direction.Right) : battle.TryMovePlayer(Direction.Right);
            }
            else if (WasPressed(Keyboard.current.sKey) || WasPressed(Keyboard.current.downArrowKey))
            {
                acted = rotateOnly ? battle.TryFacePlayer(Direction.Down) : battle.TryMovePlayer(Direction.Down);
            }
            else if (WasPressed(Keyboard.current.aKey) || WasPressed(Keyboard.current.leftArrowKey))
            {
                acted = rotateOnly ? battle.TryFacePlayer(Direction.Left) : battle.TryMovePlayer(Direction.Left);
            }
            else if (WasPressed(Keyboard.current.spaceKey))
            {
                acted = battle.TryPlayerNormalAttack();
            }
            else if (WasPressed(Keyboard.current.eKey))
            {
                acted = battle.TryUseUniqueAbility();
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

            GUI.Box(new Rect(10, 10, 430, 224), string.Empty);
            GUILayout.BeginArea(new Rect(20, 18, 410, 206));
            PlayerActor player = battle.Player;
            WeaponInstance weapon = player.EquippedWeapon;
            GUILayout.Label($"Floor: {battle.CurrentFloor}");
            GUILayout.Label($"HP {player.CurrentHp}/{player.MaxHp}   MP {player.CurrentMp}/{player.MaxMp}");
            GUILayout.Label($"Weapon Rank {weapon.Rank}  Lv {weapon.Level}  EXP {weapon.Experience}/{weapon.ExperienceToNextLevel()}");
            GUILayout.Label($"Weapon ATK {weapon.CurrentStats.Attack}  Growth {weapon.GrowthRatePercent}%  Slots {weapon.GenericAbilitySlotCount}");
            GUILayout.Label($"Ability {weapon.UniqueAbility.DisplayName}  MP {weapon.UniqueAbility.MpCost}  Power {weapon.UniqueAbility.PowerPercent}%");
            GUILayout.Label($"Enemies {CountLivingEnemies()}/{battle.Enemies.Count}");
            GUILayout.Label($"Dropped Weapons {battle.WeaponDrops.Count}");
            GUILayout.EndArea();

            DrawEnemyStatusPanel();
            DrawWeaponChoicePanel();
            DrawLogPanel();
        }

        private void DrawLogPanel()
        {
            float width = 700f;
            float height = VisibleLogLines * LogLineHeight + 20f;
            Rect panelRect = new Rect(10f, Screen.height - height - 10f, width, height);
            GUI.Box(panelRect, string.Empty);

            int firstLine = Mathf.Max(0, logLines.Count - VisibleLogLines);
            int visibleCount = logLines.Count - firstLine;
            for (int i = 0; i < visibleCount; i++)
            {
                Rect lineRect = new Rect(panelRect.x + 10f, panelRect.y + 8f + i * LogLineHeight, width - 20f, LogLineHeight);
                GUI.Label(lineRect, logLines[firstLine + i]);
            }
        }

        private void DrawEnemyStatusPanel()
        {
            float width = 300f;
            GUI.Box(new Rect(Screen.width - width - 10f, 10f, width, 112f), string.Empty);
            GUILayout.BeginArea(new Rect(Screen.width - width, 18f, width - 20f, 96f));
            GUILayout.Label("敵HP");
            foreach (EnemyActor enemy in battle.Enemies)
            {
                string state = enemy.IsAlive ? $"{enemy.CurrentHp}/{enemy.MaxHp}" : "撃破";
                GUILayout.Label($"{enemy.ActorName}: {state}");
            }
            GUILayout.EndArea();
        }

        private void DrawWeaponChoicePanel()
        {
            WeaponDrop pendingDrop = battle.PendingWeaponDrop;
            if (pendingDrop == null)
            {
                return;
            }

            float width = Mathf.Min(760f, Screen.width - 40f);
            float height = Mathf.Min(560f, Screen.height - 80f);
            Rect rect = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
            GUI.Box(rect, string.Empty);

            GUILayout.BeginArea(new Rect(rect.x + 16f, rect.y + 12f, rect.width - 32f, rect.height - 24f));
            GUILayout.Label("落ちている武器");
            GUILayout.Label("E: 拾って装備   Q: 拾わず閉じる");
            GUILayout.Label("表示: 現在の武器 → 落ちている武器 (差分)");
            GUILayout.Space(8f);

            WeaponInstance current = battle.Player.EquippedWeapon;
            WeaponInstance dropped = pendingDrop.Weapon;
            StatBlock currentStats = current.CurrentStats;
            StatBlock droppedStats = dropped.CurrentStats;

            DrawComparisonLine("ランク", current.Rank, dropped.Rank);
            DrawComparisonLine("レベル", current.Level, dropped.Level);
            DrawComparisonLine("経験値", current.Experience, dropped.Experience);
            DrawComparisonLine("攻撃力", currentStats.Attack, droppedStats.Attack);
            DrawComparisonLine("防御力", currentStats.Defense, droppedStats.Defense);
            DrawComparisonLine("HP", currentStats.Hp, droppedStats.Hp);
            DrawComparisonLine("MP", currentStats.Mp, droppedStats.Mp);
            DrawComparisonLine("会心率", currentStats.CriticalRate, droppedStats.CriticalRate, "%");
            DrawComparisonLine("会心ダメージ", currentStats.CriticalDamage, droppedStats.CriticalDamage, "%");
            DrawComparisonLine("属性ダメージバフ", currentStats.ElementalDamageBuff, droppedStats.ElementalDamageBuff, "%");
            DrawComparisonLine("回避率", currentStats.EvasionRate, droppedStats.EvasionRate, "%");
            DrawComparisonLine("成長率", current.GrowthRatePercent, dropped.GrowthRatePercent, "%");
            DrawComparisonLine("汎用アビリティ枠数", current.GenericAbilitySlotCount, dropped.GenericAbilitySlotCount);
            GUILayout.Space(6f);
            DrawTextComparisonLine("固有アビリティ名", current.UniqueAbility.DisplayName, dropped.UniqueAbility.DisplayName);
            DrawComparisonLine("固有アビリティ消費MP", current.UniqueAbility.MpCost, dropped.UniqueAbility.MpCost);
            DrawComparisonLine("固有アビリティ倍率", current.UniqueAbility.PowerPercent, dropped.UniqueAbility.PowerPercent, "%");
            GUILayout.EndArea();
        }

        private void DrawComparisonLine(string label, int currentValue, int droppedValue, string suffix = "")
        {
            int diff = droppedValue - currentValue;
            string diffText = diff > 0 ? $"+{diff}" : diff.ToString();
            GUILayout.Label($"{label}: {currentValue}{suffix} → {droppedValue}{suffix} ({diffText}{suffix})");
        }

        private void DrawTextComparisonLine(string label, string currentValue, string droppedValue)
        {
            GUILayout.Label($"{label}: {currentValue} → {droppedValue}");
        }

        private void SetupBattle()
        {
            logLines.Clear();
            battle = BattleController.CreateDefault();
            battle.LogEmitted += AddLog;
            battle.FloorChanged += HandleFloorChanged;
            view.Bind(battle);
            SetupCamera();

            WeaponInstance weapon = battle.Player.EquippedWeapon;
            AddLog($"Floor {battle.CurrentFloor} 開始。初期武器 ランク {weapon.Rank}, 固有 {weapon.UniqueAbility.DisplayName}");
            AddLog($"プレイヤー HP {battle.Player.MaxHp}, MP {battle.Player.MaxMp}, 攻撃力 {battle.Player.GetTotalStats().Attack}");
        }

        private void HandleFloorChanged()
        {
            view.Bind(battle);
            SetupCamera();
        }

        private void SetupCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
            }

            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(4.5f, battle.Map.Height * 0.65f);
            camera.transform.position = new Vector3((battle.Map.Width - 1) * 0.5f, (battle.Map.Height - 1) * 0.5f, -10f);
            camera.backgroundColor = new Color(0.04f, 0.05f, 0.06f);
        }

        private void AddLog(string message)
        {
            logLines.Add(message);
            while (logLines.Count > MaxLogLines)
            {
                logLines.RemoveAt(0);
            }
        }

        private int CountLivingEnemies()
        {
            int count = 0;
            foreach (EnemyActor enemy in battle.Enemies)
            {
                if (enemy.IsAlive)
                {
                    count++;
                }
            }

            return count;
        }

        private static bool WasPressed(KeyControl key)
        {
            return key != null && key.wasPressedThisFrame;
        }

        private static bool IsPressed(KeyControl key)
        {
            return key != null && key.isPressed;
        }
    }
}
