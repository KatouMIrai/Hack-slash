using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace WeaponMazeAlchemy.Prototype
{
    public class PrototypeBattleBootstrap : MonoBehaviour
    {
        private const int MaxLogLines = 8;

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

            GUI.Box(new Rect(10, 10, 420, 204), string.Empty);
            GUILayout.BeginArea(new Rect(20, 18, 400, 186));
            PlayerActor player = battle.Player;
            WeaponInstance weapon = player.EquippedWeapon;
            GUILayout.Label($"HP {player.CurrentHp}/{player.MaxHp}   MP {player.CurrentMp}/{player.MaxMp}");
            GUILayout.Label($"Weapon Rank {weapon.Rank}  Lv {weapon.Level}  EXP {weapon.Experience}/{weapon.ExperienceToNextLevel()}");
            GUILayout.Label($"Weapon ATK {weapon.CurrentStats.Attack}  Growth {weapon.GrowthRatePercent}%  Slots {weapon.GenericAbilitySlotCount}");
            GUILayout.Label($"Ability {weapon.UniqueAbility.DisplayName}  MP {weapon.UniqueAbility.MpCost}  Power {weapon.UniqueAbility.PowerPercent}%");
            GUILayout.Label($"Enemies {CountLivingEnemies()}/{battle.Enemies.Count}");
            GUILayout.Label($"Dropped Weapons {battle.WeaponDrops.Count}");
            GUILayout.EndArea();

            GUI.Box(new Rect(10, Screen.height - 182, 560, 172), string.Empty);
            GUILayout.BeginArea(new Rect(20, Screen.height - 174, 540, 154));
            foreach (string line in logLines)
            {
                GUILayout.Label(line);
            }
            GUILayout.EndArea();
        }

        private void SetupBattle()
        {
            logLines.Clear();
            battle = BattleController.CreateDefault();
            battle.LogEmitted += AddLog;
            view.Bind(battle);
            SetupCamera();

            WeaponInstance weapon = battle.Player.EquippedWeapon;
            AddLog($"New battle. Weapon Rank {weapon.Rank}, Ability {weapon.UniqueAbility.DisplayName}.");
            AddLog($"Player HP {battle.Player.MaxHp}, MP {battle.Player.MaxMp}, ATK {battle.Player.GetTotalStats().Attack}.");
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
