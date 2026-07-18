#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Tool.Editor.Balance
{
    /// <summary>
    /// 플레이어 공격 데이터 / 몬스터 공격 데이터 / 플레이어 스탯 / 몬스터 스탯을
    /// 프로젝트 전체에서 추출·요약·CSV 내보내기 하는 에디터 창.
    /// </summary>
    public sealed class BalanceDataExtractorWindow : EditorWindow
    {
        private enum Tab
        {
            PlayerAttack,
            MonsterAttack,
            PlayerStat,
            MonsterStat,
        }

        private static readonly string[] TabLabels =
        {
            "플레이어 공격",
            "몬스터 공격",
            "플레이어 스탯",
            "몬스터 스탯",
        };

        private Tab _tab = Tab.PlayerAttack;
        private Vector2 _scroll;
        private string _filter = "";

        private List<BalanceDataExtractor.PlayerAttackSummary> _playerAttacks;
        private List<BalanceDataExtractor.MonsterAttackSummary> _monsterAttacks;
        private List<BalanceDataExtractor.StatSummary> _playerStats;
        private List<BalanceDataExtractor.StatSummary> _monsterStats;

        [MenuItem("UPlayGround/게임플레이/밸런스/밸런스 데이터 추출기", priority = UPlayGround.Tool.Editor.UPlaygroundMenuPriority.GameplayBalance + 1)]
        public static void Open()
        {
            var window = GetWindow<BalanceDataExtractorWindow>();
            window.titleContent = new GUIContent("Balance Data Extractor");
            window.minSize = new Vector2(820f, 520f);
            window.Show();
        }

        private void OnEnable() => RefreshActive();

        private void OnGUI()
        {
            DrawToolbar();

            EditorGUI.BeginChangeCheck();
            _tab = (Tab)GUILayout.Toolbar((int)_tab, TabLabels);
            if (EditorGUI.EndChangeCheck())
                RefreshActive();

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("검색", GUILayout.Width(34f));
                _filter = EditorGUILayout.TextField(_filter, EditorStyles.toolbarSearchField);
                if (GUILayout.Button("x", EditorStyles.toolbarButton, GUILayout.Width(22f)))
                    _filter = "";
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            switch (_tab)
            {
                case Tab.PlayerAttack: DrawPlayerAttacks(); break;
                case Tab.MonsterAttack: DrawMonsterAttacks(); break;
                case Tab.PlayerStat: DrawStats(_playerStats); break;
                case Tab.MonsterStat: DrawStats(_monsterStats); break;
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(80f)))
                    RefreshAll();
                if (GUILayout.Button("Refresh Tab", EditorStyles.toolbarButton, GUILayout.Width(90f)))
                    RefreshActive();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Export CSV", EditorStyles.toolbarButton, GUILayout.Width(90f)))
                    ExportCsv();
            }
        }

        private void RefreshAll()
        {
            _playerAttacks = BalanceDataExtractor.ExtractPlayerAttackData();
            _monsterAttacks = BalanceDataExtractor.ExtractMonsterAttackData();
            _playerStats = BalanceDataExtractor.ExtractStats(BalanceDataExtractor.StatOwner.Player);
            _monsterStats = BalanceDataExtractor.ExtractStats(BalanceDataExtractor.StatOwner.Monster);
        }

        private void RefreshActive()
        {
            switch (_tab)
            {
                case Tab.PlayerAttack: _playerAttacks = BalanceDataExtractor.ExtractPlayerAttackData(); break;
                case Tab.MonsterAttack: _monsterAttacks = BalanceDataExtractor.ExtractMonsterAttackData(); break;
                case Tab.PlayerStat: _playerStats = BalanceDataExtractor.ExtractStats(BalanceDataExtractor.StatOwner.Player); break;
                case Tab.MonsterStat: _monsterStats = BalanceDataExtractor.ExtractStats(BalanceDataExtractor.StatOwner.Monster); break;
            }
        }

        // ── Player Attack ──────────────────────────────────────────
        private void DrawPlayerAttacks()
        {
            if (_playerAttacks == null || _playerAttacks.Count == 0)
            {
                EditorGUILayout.HelpBox("전투 로드아웃 AbilitySetSO 에셋이 없습니다.", MessageType.Info);
                return;
            }

            DrawHeader("Asset", "Lite", "Heavy", "Jump", "Dash", "Skill", "Charge", "공격수", "Phase", "총DMG", "평균", "최대");
            for (int i = 0; i < _playerAttacks.Count; i++)
            {
                var s = _playerAttacks[i];
                if (!PassesFilter(s.AssetName))
                    continue;

                Rect row = BeginRow(i, s.Asset);
                DrawCells(row,
                    s.AssetName, s.LiteCount.ToString(), s.HeavyCount.ToString(), s.JumpCount.ToString(),
                    s.DashCount.ToString(), s.SkillCount.ToString(), s.ChargeStageCount.ToString(),
                    s.TotalAttacks.ToString(), s.TotalHitPhases.ToString(),
                    s.TotalDamage.ToString("F0"), s.AvgDamagePerAttack.ToString("F1"), s.MaxSingleAttackDamage.ToString("F0"));
            }
        }

        // ── Monster Attack ─────────────────────────────────────────
        private void DrawMonsterAttacks()
        {
            if (_monsterAttacks == null || _monsterAttacks.Count == 0)
            {
                EditorGUILayout.HelpBox("EnemyAttackDataSO 에셋이 없습니다.", MessageType.Info);
                return;
            }

            DrawHeader("Asset", "Skills", "Basic", "Heavy", "Skill", "Ranged", "강공%", "GCD", "총DMG", "평균", "최대", "DR누락");
            for (int i = 0; i < _monsterAttacks.Count; i++)
            {
                var s = _monsterAttacks[i];
                if (!PassesFilter(s.AssetName))
                    continue;

                Rect row = BeginRow(i, s.Asset);
                if (s.DangerRingMissing > 0 && Event.current.type == EventType.Repaint)
                    EditorGUI.DrawRect(row, new Color(0.86f, 0.58f, 0.18f, 0.15f));

                DrawCells(row,
                    s.AssetName, s.AttackSkillCount.ToString(), s.BasicCount.ToString(), s.HeavyCount.ToString(),
                    s.SkillCatCount.ToString(), s.RangedCount.ToString(), $"{s.StrongWeightShare * 100f:F0}%",
                    s.GlobalCooldown.ToString("F1"), s.TotalDamage.ToString("F0"), s.AvgDamagePerSkill.ToString("F1"),
                    s.MaxSingleAttackDamage.ToString("F0"), s.DangerRingMissing.ToString());
            }
        }

        // ── Stats ──────────────────────────────────────────────────
        private void DrawStats(List<BalanceDataExtractor.StatSummary> stats)
        {
            if (stats == null || stats.Count == 0)
            {
                EditorGUILayout.HelpBox("해당 분류의 ActorStatSO를 찾지 못했습니다. (ActorDefinitionSO.statData 참조 기준으로 분류)", MessageType.Info);
                return;
            }

            DrawHeader("Asset", "Owner", "HP", "ATK", "DEF", "Poise", "MoveSpd", "CritRate", "", "", "", "");
            for (int i = 0; i < stats.Count; i++)
            {
                var s = stats[i];
                if (!PassesFilter(s.AssetName))
                    continue;

                Rect row = BeginRow(i, s.Asset);
                DrawCells(row,
                    s.AssetName, s.Owner.ToString(), s.MaxHealth.ToString("F0"), s.AttackPower.ToString("F2"),
                    s.Defense.ToString("F2"), s.MaxPoise.ToString("F0"), s.MoveSpeed.ToString("F2"),
                    s.CritRate.ToString("F2"), "", "", "", "");
            }
        }

        // ── Row helpers ────────────────────────────────────────────
        private Rect BeginRow(int index, UnityEngine.Object pingTarget)
        {
            Rect row = GUILayoutUtility.GetRect(0f, 20f, GUILayout.ExpandWidth(true));
            if (index % 2 == 1 && Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(row, new Color(0f, 0f, 0f, 0.10f));

            if (Event.current.type == EventType.MouseDown && row.Contains(Event.current.mousePosition))
            {
                EditorGUIUtility.PingObject(pingTarget);
                Selection.activeObject = pingTarget;
                Event.current.Use();
            }
            return row;
        }

        private static void DrawHeader(params string[] cols)
        {
            Rect header = GUILayoutUtility.GetRect(0f, 22f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(header, new Color(0.13f, 0.13f, 0.15f));
            DrawCellsStyled(header, EditorStyles.boldLabel, cols);
        }

        private static void DrawCells(Rect rect, params string[] cols)
            => DrawCellsStyled(rect, EditorStyles.label, cols);

        private static void DrawCellsStyled(Rect rect, GUIStyle style, string[] cols)
        {
            float x = rect.x + 6f;
            for (int i = 0; i < cols.Length; i++)
            {
                float width = i == 0 ? 230f : 62f;
                GUI.Label(new Rect(x, rect.y + 2f, width, 16f), cols[i] ?? string.Empty, style);
                x += width + 4f;
            }
        }

        private bool PassesFilter(string name)
        {
            if (string.IsNullOrWhiteSpace(_filter))
                return true;
            return !string.IsNullOrEmpty(name) && name.IndexOf(_filter, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // ── CSV ────────────────────────────────────────────────────
        private void ExportCsv()
        {
            string defaultName = $"BalanceExtract_{_tab}.csv";
            string path = EditorUtility.SaveFilePanel("데이터 추출 CSV 저장", Application.dataPath, defaultName, "csv");
            if (string.IsNullOrWhiteSpace(path))
                return;

            var builder = new StringBuilder();
            switch (_tab)
            {
                case Tab.PlayerAttack: BuildPlayerAttackCsv(builder); break;
                case Tab.MonsterAttack: BuildMonsterAttackCsv(builder); break;
                case Tab.PlayerStat: BuildStatCsv(builder, _playerStats); break;
                case Tab.MonsterStat: BuildStatCsv(builder, _monsterStats); break;
            }

            File.WriteAllText(path, builder.ToString(), new UTF8Encoding(true));
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Export CSV", $"저장 완료\n{path}", "확인");
        }

        private void BuildPlayerAttackCsv(StringBuilder b)
        {
            b.AppendLine("asset,path,lite,heavy,jump,dash,skill,chargeStages,comboRoutes,totalAttacks,totalHitPhases,totalDamage,avgDamage,maxSingleDamage");
            if (_playerAttacks == null) return;
            foreach (var s in _playerAttacks)
            {
                b.Append(Esc(s.AssetName)).Append(',').Append(Esc(s.Path)).Append(',')
                 .Append(s.LiteCount).Append(',').Append(s.HeavyCount).Append(',').Append(s.JumpCount).Append(',')
                 .Append(s.DashCount).Append(',').Append(s.SkillCount).Append(',').Append(s.ChargeStageCount).Append(',')
                 .Append(s.ComboRouteCount).Append(',').Append(s.TotalAttacks).Append(',').Append(s.TotalHitPhases).Append(',')
                 .Append(s.TotalDamage.ToString("F2")).Append(',').Append(s.AvgDamagePerAttack.ToString("F2")).Append(',')
                 .Append(s.MaxSingleAttackDamage.ToString("F2")).AppendLine();
            }
        }

        private void BuildMonsterAttackCsv(StringBuilder b)
        {
            b.AppendLine("asset,path,attackSkills,basic,heavy,skill,ranged,globalCooldown,totalWeight,strongWeightShare,totalDamage,avgDamage,maxSingleDamage,dangerRingCovered,dangerRingMissing,telegraphCount");
            if (_monsterAttacks == null) return;
            foreach (var s in _monsterAttacks)
            {
                b.Append(Esc(s.AssetName)).Append(',').Append(Esc(s.Path)).Append(',')
                 .Append(s.AttackSkillCount).Append(',').Append(s.BasicCount).Append(',').Append(s.HeavyCount).Append(',')
                 .Append(s.SkillCatCount).Append(',').Append(s.RangedCount).Append(',').Append(s.GlobalCooldown.ToString("F2")).Append(',')
                 .Append(s.TotalWeight.ToString("F2")).Append(',').Append(s.StrongWeightShare.ToString("F4")).Append(',')
                 .Append(s.TotalDamage.ToString("F2")).Append(',').Append(s.AvgDamagePerSkill.ToString("F2")).Append(',')
                 .Append(s.MaxSingleAttackDamage.ToString("F2")).Append(',').Append(s.DangerRingCovered).Append(',')
                 .Append(s.DangerRingMissing).Append(',').Append(s.TelegraphCount).AppendLine();
            }
        }

        private void BuildStatCsv(StringBuilder b, List<BalanceDataExtractor.StatSummary> stats)
        {
            b.AppendLine("asset,path,owner,maxHealth,attackPower,defense,maxPoise,moveSpeed,critRate");
            if (stats == null) return;
            foreach (var s in stats)
            {
                b.Append(Esc(s.AssetName)).Append(',').Append(Esc(s.Path)).Append(',').Append(s.Owner).Append(',')
                 .Append(s.MaxHealth.ToString("F2")).Append(',').Append(s.AttackPower.ToString("F2")).Append(',')
                 .Append(s.Defense.ToString("F2")).Append(',').Append(s.MaxPoise.ToString("F2")).Append(',')
                 .Append(s.MoveSpeed.ToString("F2")).Append(',').Append(s.CritRate.ToString("F2")).AppendLine();
            }
        }

        private static string Esc(string value)
        {
            value ??= string.Empty;
            if (!value.Contains(",") && !value.Contains("\"") && !value.Contains("\n"))
                return value;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}
#endif
