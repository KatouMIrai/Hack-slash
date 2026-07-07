using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;

namespace WeaponMazeAlchemy.Prototype
{
    public class PrototypeBattleBootstrap : MonoBehaviour
    {
        private const string TargetSceneName = "SampleScene";
        private const int MaxLogLines = 24;
        private const int VisibleLogLines = 10;
        private const float LogLineHeight = 20f;

        private static readonly Color PositiveColor = new Color(0.35f, 1f, 0.45f);
        private static readonly Color NegativeColor = new Color(1f, 0.35f, 0.35f);
        private static readonly Color NeutralColor = new Color(0.78f, 0.78f, 0.78f);
        private static readonly Color EmphasisColor = new Color(1f, 0.9f, 0.35f);
        private static readonly Color ParchmentColor = new Color(0.78f, 0.66f, 0.43f, 0.96f);
        private static readonly Color ParchmentInnerColor = new Color(0.92f, 0.84f, 0.62f, 0.98f);
        private static readonly Color ParchmentBorderColor = new Color(0.32f, 0.19f, 0.08f, 0.98f);
        private static readonly Color InkColor = new Color(0.13f, 0.08f, 0.03f);
        private static readonly Color ParchmentPositiveColor = new Color(0.05f, 0.42f, 0.13f);
        private static readonly Color ParchmentNegativeColor = new Color(0.66f, 0.08f, 0.05f);
        private static readonly Color ParchmentNeutralColor = new Color(0.35f, 0.28f, 0.2f);

        private readonly List<string> logLines = new List<string>();
        private BattleController battle;
        private PrototypeBattleView view;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (!IsTargetScene())
            {
                return;
            }

            if (FindFirstObjectByType<PrototypeBattleBootstrap>() != null)
            {
                return;
            }

            GameObject bootstrapObject = new GameObject("Prototype Battle Bootstrap");
            bootstrapObject.AddComponent<PrototypeBattleBootstrap>();
        }

        private void Start()
        {
            if (!IsTargetScene())
            {
                Destroy(gameObject);
                return;
            }

            view = gameObject.AddComponent<PrototypeBattleView>();
            SetupBattle();
        }

        private static bool IsTargetScene()
        {
            return SceneManager.GetActiveScene().name == TargetSceneName;
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
                if (WasPressed(Keyboard.current.bKey))
                {
                    choiceChanged = battle.TryAddPendingWeaponToInventory();
                }
                else if (TryGetInventoryWeaponIndexPressed(Keyboard.current, out int swapIndex))
                {
                    choiceChanged = battle.TrySwapPendingWeaponWithInventory(swapIndex);
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

            bool acted = false;
            if (TryGetPressedDirection(Keyboard.current, out Direction pressedDirection))
            {
                acted = IsRotateModifierPressed(Keyboard.current)
                    ? battle.TryFacePlayer(pressedDirection)
                    : battle.TryMovePlayer(pressedDirection);
            }
            else if (WasRotateModifierPressed(Keyboard.current) && TryGetHeldDirection(Keyboard.current, out Direction heldDirection))
            {
                acted = battle.TryFacePlayer(heldDirection);
            }
            else if (WasPressed(Keyboard.current.spaceKey))
            {
                acted = battle.TryPlayerNormalAttack();
            }
            else if (WasPressed(Keyboard.current.eKey))
            {
                acted = battle.TryUseUniqueAbility();
            }
            else if (TryGetInventoryWeaponIndexPressed(Keyboard.current, out int inventoryIndex))
            {
                acted = battle.TryEquipInventoryWeapon(inventoryIndex);
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
            if (weapon != null)
            {
                GUILayout.Label($"Equipped Bag Slot: {battle.EquippedInventoryWeaponIndex + 1}");
                GUILayout.Label($"Weapon Rank {weapon.Rank}  Lv {weapon.Level}  EXP {weapon.Experience}/{weapon.ExperienceToNextLevel()}");
                GUILayout.Label($"Weapon ATK {weapon.CurrentStats.Attack}  Growth {weapon.GrowthRatePercent}%  Slots {weapon.GenericAbilitySlotCount}");
                GUILayout.Label($"Ability {weapon.UniqueAbility.DisplayName}  MP {weapon.UniqueAbility.MpCost}  Power {weapon.UniqueAbility.PowerPercent}%");
            }
            else
            {
                GUILayout.Label("Equipped Weapon: None");
            }

            GUILayout.Label($"Enemies {CountLivingEnemies()}/{battle.Enemies.Count}");
            GUILayout.Label($"Dropped Weapons {battle.WeaponDrops.Count}");
            GUILayout.EndArea();

            DrawEnemyStatusPanel();
            DrawInventoryPanel();
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
            float width = 360f;
            float height = 56f + battle.Enemies.Count * 22f;
            GUI.Box(new Rect(Screen.width - width - 10f, 10f, width, height), string.Empty);
            GUILayout.BeginArea(new Rect(Screen.width - width, 18f, width - 20f, height - 16f));
            GUILayout.Label("敵ステータス");
            foreach (EnemyActor enemy in battle.Enemies)
            {
                StatBlock stats = enemy.GetTotalStats();
                string aiLabel = enemy.Definition.AiDisplayName;
                string state = enemy.IsAlive
                    ? $"HP {enemy.CurrentHp}/{enemy.MaxHp}  ATK {stats.Attack}  DEF {stats.Defense} / {aiLabel}"
                    : $"撃破 / {aiLabel}";
                GUILayout.Label($"{enemy.ActorName}: {state}");
            }
            GUILayout.EndArea();
        }

        private void DrawInventoryPanel()
        {
            float width = 430f;
            float height = 64f + battle.MaxInventoryWeaponCount * 24f;
            GUI.Box(new Rect(10f, 244f, width, height), string.Empty);
            GUILayout.BeginArea(new Rect(20f, 252f, width - 20f, height - 16f));
            DrawColoredLabel($"Inventory {battle.InventoryWeapons.Count} / {battle.MaxInventoryWeaponCount}", EmphasisColor, FontStyle.Bold);
            GUILayout.Label("1-3: バッグ内武器を装備");

            for (int i = 0; i < battle.MaxInventoryWeaponCount; i++)
            {
                if (i < battle.InventoryWeapons.Count)
                {
                    WeaponInstance weapon = battle.InventoryWeapons[i];
                    string abilityName = weapon.UniqueAbility != null ? weapon.UniqueAbility.DisplayName : "なし";
                    string equippedText = battle.EquippedInventoryWeaponIndex == i ? "★装備中 " : string.Empty;
                    Color color = battle.EquippedInventoryWeaponIndex == i ? EmphasisColor : Color.white;
                    DrawColoredLabel($"[{i + 1}] {equippedText}Rank {weapon.Rank} Lv{weapon.Level} ATK {weapon.CurrentStats.Attack} / {abilityName}", color, battle.EquippedInventoryWeaponIndex == i ? FontStyle.Bold : FontStyle.Normal);
                }
                else
                {
                    DrawColoredLabel($"[{i + 1}] 空き", NeutralColor);
                }
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

            float width = Mathf.Min(980f, Screen.width - 40f);
            float height = Mathf.Min(720f, Screen.height - 80f);
            Rect rect = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
            DrawFilledRect(rect, new Color(0.05f, 0.035f, 0.02f, 0.88f));
            GUI.Box(rect, string.Empty);

            WeaponInstance current = battle.Player.EquippedWeapon;
            WeaponInstance dropped = pendingDrop.Weapon;
            if (current == null || dropped == null)
            {
                GUILayout.BeginArea(new Rect(rect.x + 16f, rect.y + 12f, rect.width - 32f, rect.height - 24f));
                GUILayout.Label("比較できる武器がありません");
                GUILayout.EndArea();
                return;
            }

            Rect helpRect = new Rect(rect.x + 18f, rect.y + 12f, rect.width - 36f, 54f);
            GUILayout.BeginArea(helpRect);
            DrawColoredLabel("落ちている武器の確認", EmphasisColor, FontStyle.Bold);
            DrawWeaponChoiceHelp();
            GUILayout.EndArea();

            float bagHeight = 154f;
            Rect parchmentRect = new Rect(rect.x + 28f, helpRect.yMax + 10f, rect.width - 56f, rect.height - helpRect.height - bagHeight - 48f);
            Rect bagRect = new Rect(rect.x + 28f, parchmentRect.yMax + 12f, rect.width - 56f, bagHeight);
            DrawParchmentComparison(parchmentRect, current, dropped);
            DrawBagComparisonPanel(bagRect, dropped);
        }

        private void DrawWeaponChoiceHelp()
        {
            bool hasSpace = battle.InventoryWeapons.Count < battle.MaxInventoryWeaponCount;
            GUILayout.BeginHorizontal();
            DrawColoredLabel(hasSpace ? "B: バッグに入れる" : "B: バッグ満杯", hasSpace ? PositiveColor : NegativeColor, FontStyle.Bold);
            GUILayout.Label("  1: Bag[1]と交換   2: Bag[2]と交換   3: Bag[3]と交換   Q: 拾わない   R: リセット");
            GUILayout.EndHorizontal();
        }

        private void DrawWeaponSummaryBlock(string title, WeaponInstance weapon, string prefix)
        {
            DrawColoredLabel(title, EmphasisColor, FontStyle.Bold);
            if (weapon == null)
            {
                GUILayout.Label("なし");
                return;
            }

            StatBlock stats = weapon.CurrentStats;
            string abilityName = weapon.UniqueAbility != null ? weapon.UniqueAbility.DisplayName : "なし";
            GUILayout.Label($"{prefix}Rank {weapon.Rank} / Lv {weapon.Level} / {abilityName}");
            GUILayout.Label($"ATK {stats.Attack} / DEF {stats.Defense} / HP +{stats.Hp} / MP +{stats.Mp}");
            GUILayout.Label($"Crit {stats.CriticalRate}% / CritDmg {stats.CriticalDamage}% / Evasion {stats.EvasionRate}%");
        }

        private void DrawParchmentComparison(Rect rect, WeaponInstance current, WeaponInstance dropped)
        {
            DrawParchmentRect(rect);

            float contentPadding = 24f;
            float columnGap = 28f;
            float columnWidth = (rect.width - contentPadding * 2f - columnGap) * 0.5f;
            Rect leftRect = new Rect(rect.x + contentPadding, rect.y + 20f, columnWidth, rect.height - 40f);
            Rect rightRect = new Rect(leftRect.xMax + columnGap, rect.y + 20f, columnWidth, rect.height - 40f);
            DrawFilledRect(new Rect(leftRect.xMax + columnGap * 0.5f - 1f, rect.y + 18f, 2f, rect.height - 36f), new Color(0.35f, 0.2f, 0.08f, 0.55f));

            DrawCurrentWeaponOnParchment(leftRect, current);
            DrawDroppedWeaponOnParchment(rightRect, current, dropped);
        }

        private void DrawCurrentWeaponOnParchment(Rect rect, WeaponInstance weapon)
        {
            GUILayout.BeginArea(rect);
            DrawParchmentLabel("現在装備中", InkColor, FontStyle.Bold);
            DrawParchmentLabel($"Bag[{battle.EquippedInventoryWeaponIndex + 1}]", ParchmentNeutralColor, FontStyle.Bold);
            GUILayout.Space(8f);

            StatBlock stats = weapon.CurrentStats;
            string abilityName = weapon.UniqueAbility != null ? weapon.UniqueAbility.DisplayName : "なし";
            DrawParchmentValueLine("Rank", weapon.Rank.ToString());
            DrawParchmentValueLine("Lv", weapon.Level.ToString());
            DrawParchmentValueLine("固有", abilityName);
            GUILayout.Space(4f);
            DrawParchmentValueLine("ATK", stats.Attack.ToString(), true);
            DrawParchmentValueLine("DEF", stats.Defense.ToString(), true);
            DrawParchmentValueLine("HP", $"+{stats.Hp}", true);
            DrawParchmentValueLine("MP", $"+{stats.Mp}", true);
            GUILayout.Space(4f);
            DrawParchmentValueLine("会心率", $"{stats.CriticalRate}%");
            DrawParchmentValueLine("会心ダメージ", $"{stats.CriticalDamage}%");
            DrawParchmentValueLine("属性バフ", $"{stats.ElementalDamageBuff}%");
            DrawParchmentValueLine("回避率", $"{stats.EvasionRate}%");
            DrawParchmentValueLine("成長率", $"{weapon.GrowthRatePercent}%");
            DrawParchmentValueLine("汎用枠", weapon.GenericAbilitySlotCount.ToString());
            DrawParchmentValueLine("消費MP", weapon.UniqueAbility.MpCost.ToString());
            DrawParchmentValueLine("倍率", $"{weapon.UniqueAbility.PowerPercent}%");
            GUILayout.EndArea();
        }

        private void DrawDroppedWeaponOnParchment(Rect rect, WeaponInstance current, WeaponInstance dropped)
        {
            GUILayout.BeginArea(rect);
            DrawParchmentLabel("落ちている武器", InkColor, FontStyle.Bold);
            DrawParchmentLabel($"評価: {EvaluateDroppedWeapon(current, dropped)}", ParchmentNeutralColor, FontStyle.Bold);
            GUILayout.Space(8f);

            StatBlock currentStats = current.CurrentStats;
            StatBlock droppedStats = dropped.CurrentStats;
            string currentAbility = current.UniqueAbility != null ? current.UniqueAbility.DisplayName : "なし";
            string droppedAbility = dropped.UniqueAbility != null ? dropped.UniqueAbility.DisplayName : "なし";

            DrawParchmentDiffLine("Rank", dropped.Rank.ToString(), dropped.Rank - current.Rank);
            DrawParchmentDiffLine("Lv", dropped.Level.ToString(), dropped.Level - current.Level);
            DrawParchmentTextDiffLine("固有", droppedAbility, currentAbility);
            GUILayout.Space(4f);
            DrawParchmentDiffLine("ATK", droppedStats.Attack.ToString(), droppedStats.Attack - currentStats.Attack, true);
            DrawParchmentDiffLine("DEF", droppedStats.Defense.ToString(), droppedStats.Defense - currentStats.Defense, true);
            DrawParchmentDiffLine("HP", $"+{droppedStats.Hp}", droppedStats.Hp - currentStats.Hp, true);
            DrawParchmentDiffLine("MP", $"+{droppedStats.Mp}", droppedStats.Mp - currentStats.Mp, true);
            GUILayout.Space(4f);
            DrawParchmentDiffLine("会心率", $"{droppedStats.CriticalRate}%", droppedStats.CriticalRate - currentStats.CriticalRate);
            DrawParchmentDiffLine("会心ダメージ", $"{droppedStats.CriticalDamage}%", droppedStats.CriticalDamage - currentStats.CriticalDamage);
            DrawParchmentDiffLine("属性バフ", $"{droppedStats.ElementalDamageBuff}%", droppedStats.ElementalDamageBuff - currentStats.ElementalDamageBuff);
            DrawParchmentDiffLine("回避率", $"{droppedStats.EvasionRate}%", droppedStats.EvasionRate - currentStats.EvasionRate);
            DrawParchmentDiffLine("成長率", $"{dropped.GrowthRatePercent}%", dropped.GrowthRatePercent - current.GrowthRatePercent);
            DrawParchmentDiffLine("汎用枠", dropped.GenericAbilitySlotCount.ToString(), dropped.GenericAbilitySlotCount - current.GenericAbilitySlotCount);
            DrawParchmentDiffLine("消費MP", dropped.UniqueAbility.MpCost.ToString(), dropped.UniqueAbility.MpCost - current.UniqueAbility.MpCost);
            DrawParchmentDiffLine("倍率", $"{dropped.UniqueAbility.PowerPercent}%", dropped.UniqueAbility.PowerPercent - current.UniqueAbility.PowerPercent);
            GUILayout.EndArea();
        }

        private void DrawBagComparisonPanel(Rect rect, WeaponInstance dropped)
        {
            DrawFilledRect(rect, new Color(0.09f, 0.07f, 0.05f, 0.92f));
            GUI.Box(rect, string.Empty);
            GUILayout.BeginArea(new Rect(rect.x + 14f, rect.y + 10f, rect.width - 28f, rect.height - 18f));
            DrawColoredLabel("【バッグ比較: 床の武器 - バッグ内武器】", EmphasisColor, FontStyle.Bold);
            for (int i = 0; i < battle.MaxInventoryWeaponCount; i++)
            {
                if (i >= battle.InventoryWeapons.Count)
                {
                    DrawColoredLabel($"Bag[{i + 1}] 空き  Bキーでバッグに入れる", PositiveColor);
                    continue;
                }

                WeaponInstance bagWeapon = battle.InventoryWeapons[i];
                StatBlock bagStats = bagWeapon.CurrentStats;
                string abilityName = bagWeapon.UniqueAbility != null ? bagWeapon.UniqueAbility.DisplayName : "なし";
                string equippedText = battle.EquippedInventoryWeaponIndex == i ? " ★装備中" : string.Empty;
                DrawColoredLabel($"Bag[{i + 1}]{equippedText} Rank {bagWeapon.Rank} Lv{bagWeapon.Level} ATK {bagStats.Attack} / {abilityName}", battle.EquippedInventoryWeaponIndex == i ? EmphasisColor : Color.white, battle.EquippedInventoryWeaponIndex == i ? FontStyle.Bold : FontStyle.Normal);

                GUILayout.BeginHorizontal();
                GUILayout.Label("  床武器との差:", GUILayout.Width(100f));
                DrawInlineDelta("Rank", dropped.Rank - bagWeapon.Rank);
                DrawInlineDelta("ATK", dropped.CurrentStats.Attack - bagStats.Attack);
                DrawInlineDelta("DEF", dropped.CurrentStats.Defense - bagStats.Defense);
                DrawInlineDelta("HP", dropped.CurrentStats.Hp - bagStats.Hp);
                DrawInlineDelta("MP", dropped.CurrentStats.Mp - bagStats.Mp);
                GUILayout.EndHorizontal();
            }
            GUILayout.EndArea();
        }

        private void DrawComparisonLine(string label, int currentValue, int droppedValue, string suffix = "", bool important = false)
        {
            int diff = droppedValue - currentValue;
            string diffText = FormatSigned(diff);
            DrawColoredLabel($"{label}: {currentValue}{suffix} → {droppedValue}{suffix} ({diffText}{suffix})", GetDiffColor(diff), important ? FontStyle.Bold : FontStyle.Normal);
        }

        private void DrawTextComparisonLine(string label, string currentValue, string droppedValue)
        {
            Color color = currentValue == droppedValue ? NeutralColor : EmphasisColor;
            DrawColoredLabel($"{label}: {currentValue} → {droppedValue}", color);
        }

        private void DrawInlineDelta(string label, int diff)
        {
            DrawColoredLabel($"{label} {FormatSigned(diff)}", GetDiffColor(diff), FontStyle.Bold, GUILayout.Width(78f));
        }

        private void DrawParchmentRect(Rect rect)
        {
            DrawFilledRect(rect, ParchmentBorderColor);
            DrawFilledRect(new Rect(rect.x + 5f, rect.y + 5f, rect.width - 10f, rect.height - 10f), ParchmentColor);
            DrawFilledRect(new Rect(rect.x + 14f, rect.y + 14f, rect.width - 28f, rect.height - 28f), ParchmentInnerColor);
        }

        private void DrawParchmentValueLine(string label, string value, bool important = false)
        {
            GUILayout.BeginHorizontal();
            DrawParchmentLabel(label, InkColor, important ? FontStyle.Bold : FontStyle.Normal, GUILayout.Width(110f));
            DrawParchmentLabel(value, InkColor, important ? FontStyle.Bold : FontStyle.Normal);
            GUILayout.EndHorizontal();
        }

        private void DrawParchmentDiffLine(string label, string value, int diff, bool important = false)
        {
            GUILayout.BeginHorizontal();
            DrawParchmentLabel(label, InkColor, important ? FontStyle.Bold : FontStyle.Normal, GUILayout.Width(110f));
            DrawParchmentLabel(value, InkColor, important ? FontStyle.Bold : FontStyle.Normal, GUILayout.Width(58f));
            DrawParchmentLabel(FormatSigned(diff), GetParchmentDiffColor(diff), FontStyle.Bold);
            GUILayout.EndHorizontal();
        }

        private void DrawParchmentTextDiffLine(string label, string value, string baseValue)
        {
            bool changed = value != baseValue;
            GUILayout.BeginHorizontal();
            DrawParchmentLabel(label, InkColor, FontStyle.Normal, GUILayout.Width(110f));
            DrawParchmentLabel(value, InkColor, FontStyle.Normal, GUILayout.Width(120f));
            DrawParchmentLabel(changed ? "変更" : "同じ", changed ? ParchmentPositiveColor : ParchmentNeutralColor, FontStyle.Bold);
            GUILayout.EndHorizontal();
        }

        private void DrawParchmentLabel(string text, Color color, FontStyle fontStyle = FontStyle.Normal, params GUILayoutOption[] options)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                normal = { textColor = color },
                fontStyle = fontStyle
            };
            GUILayout.Label(text, style, options);
        }

        private void DrawColoredLabel(string text, Color color, FontStyle fontStyle = FontStyle.Normal, params GUILayoutOption[] options)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                normal = { textColor = color },
                fontStyle = fontStyle
            };
            GUILayout.Label(text, style, options);
        }

        private static Color GetDiffColor(int diff)
        {
            if (diff > 0)
            {
                return PositiveColor;
            }

            if (diff < 0)
            {
                return NegativeColor;
            }

            return NeutralColor;
        }

        private static Color GetParchmentDiffColor(int diff)
        {
            if (diff > 0)
            {
                return ParchmentPositiveColor;
            }

            if (diff < 0)
            {
                return ParchmentNegativeColor;
            }

            return ParchmentNeutralColor;
        }

        private static void DrawFilledRect(Rect rect, Color color)
        {
            Color previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

        private static string FormatSigned(int value)
        {
            return value > 0 ? $"+{value}" : value.ToString();
        }

        private static string EvaluateDroppedWeapon(WeaponInstance current, WeaponInstance dropped)
        {
            StatBlock currentStats = current.CurrentStats;
            StatBlock droppedStats = dropped.CurrentStats;
            int attackDiff = droppedStats.Attack - currentStats.Attack;
            int defenseDiff = droppedStats.Defense - currentStats.Defense;
            int hpDiff = droppedStats.Hp - currentStats.Hp;
            string currentAbility = current.UniqueAbility != null ? current.UniqueAbility.DisplayName : string.Empty;
            string droppedAbility = dropped.UniqueAbility != null ? dropped.UniqueAbility.DisplayName : string.Empty;

            if (attackDiff >= 8 || attackDiff >= Mathf.Max(4, currentStats.Attack / 3))
            {
                return currentAbility != droppedAbility ? "攻撃寄り / アビリティ変更あり" : "攻撃寄り";
            }

            if (defenseDiff >= 5 || hpDiff >= 40)
            {
                return currentAbility != droppedAbility ? "防御寄り / アビリティ変更あり" : "防御寄り";
            }

            if (currentAbility != droppedAbility)
            {
                return "アビリティ変更あり";
            }

            return "バランス型";
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

        private static bool IsRotateModifierPressed(Keyboard keyboard)
        {
            return IsPressed(keyboard.leftShiftKey) || IsPressed(keyboard.rightShiftKey);
        }

        private static bool WasRotateModifierPressed(Keyboard keyboard)
        {
            return WasPressed(keyboard.leftShiftKey) || WasPressed(keyboard.rightShiftKey);
        }

        private static bool TryGetPressedDirection(Keyboard keyboard, out Direction direction)
        {
            if (WasPressed(keyboard.wKey) || WasPressed(keyboard.upArrowKey))
            {
                direction = Direction.Up;
                return true;
            }

            if (WasPressed(keyboard.dKey) || WasPressed(keyboard.rightArrowKey))
            {
                direction = Direction.Right;
                return true;
            }

            if (WasPressed(keyboard.sKey) || WasPressed(keyboard.downArrowKey))
            {
                direction = Direction.Down;
                return true;
            }

            if (WasPressed(keyboard.aKey) || WasPressed(keyboard.leftArrowKey))
            {
                direction = Direction.Left;
                return true;
            }

            direction = Direction.Up;
            return false;
        }

        private static bool TryGetHeldDirection(Keyboard keyboard, out Direction direction)
        {
            if (IsPressed(keyboard.wKey) || IsPressed(keyboard.upArrowKey))
            {
                direction = Direction.Up;
                return true;
            }

            if (IsPressed(keyboard.dKey) || IsPressed(keyboard.rightArrowKey))
            {
                direction = Direction.Right;
                return true;
            }

            if (IsPressed(keyboard.sKey) || IsPressed(keyboard.downArrowKey))
            {
                direction = Direction.Down;
                return true;
            }

            if (IsPressed(keyboard.aKey) || IsPressed(keyboard.leftArrowKey))
            {
                direction = Direction.Left;
                return true;
            }

            direction = Direction.Up;
            return false;
        }

        private static bool TryGetInventoryWeaponIndexPressed(Keyboard keyboard, out int inventoryIndex)
        {
            if (WasPressed(keyboard.digit1Key) || WasPressed(keyboard.numpad1Key))
            {
                inventoryIndex = 0;
                return true;
            }

            if (WasPressed(keyboard.digit2Key) || WasPressed(keyboard.numpad2Key))
            {
                inventoryIndex = 1;
                return true;
            }

            if (WasPressed(keyboard.digit3Key) || WasPressed(keyboard.numpad3Key))
            {
                inventoryIndex = 2;
                return true;
            }

            inventoryIndex = -1;
            return false;
        }
    }
}
