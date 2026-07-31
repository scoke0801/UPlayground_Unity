#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Gameplay.Ability;
using UPlayGround.EditorTools;

namespace UPlayGround.Tool.Editor.Balance
{
    /// <summary>
    /// 공격별 리스크(쿨다운/선후딜) vs 리워드(데미지/경직/브레이크) 산점도.
    /// "후딜은 짧은데 데미지는 높은" 지배적 공격(저리스크·고리워드)이 시각적으로 드러나도록
    /// 중앙값 기준 사분면을 표시하고 지배적 후보를 목록으로 나열한다.
    /// 프레임 축은 HitPhase의 AttackReactionProfile.analysis(선딜/후딜)를 사용하며,
    /// 자동 분석이 생성되지 않은 공격은 프레임 축에서 제외된다.
    /// </summary>
    public sealed class BalanceRiskRewardWindow : EditorWindow
    {
        private enum DataMode
        {
            EnemySkills,
            PlayerAttacks,
        }

        private enum RiskAxis
        {
            Cooldown,           // 적 전용 의미가 크지만 플레이어는 제외 처리
            StartupPlusRecovery,
            TotalDuration,
        }

        private enum RewardAxis
        {
            Damage,
            DamagePlusPoise,
            DamagePlusBreak,
            DamagePerCooldown,
        }

        private sealed class PlotPoint
        {
            public string Name;
            public string Category;
            public float Risk;
            public float Reward;
            public Color Color;
            public bool Dominant;
            public Rect ScreenRect;
        }

        private DataMode _mode = DataMode.EnemySkills;
        private RiskAxis _riskAxis = RiskAxis.Cooldown;
        private RewardAxis _rewardAxis = RewardAxis.Damage;
        private AbilitySetSO _enemyData;
        private AbilitySetSO _playerData;
        private readonly List<PlotPoint> _points = new();
        private readonly List<string> _excluded = new();
        private Vector2 _listScroll;

        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/게임플레이/밸런스/리스크·리워드 산점도", priority = UPlaygroundMenuPriority.GameplayBalance + 3)]
        public static void Open()
        {
            var window = GetWindow<BalanceRiskRewardWindow>();
            window.titleContent = new GUIContent("Risk / Reward");
            window.minSize = new Vector2(720f, 520f);
            window.wantsMouseMove = true;
            window.Show();
        }

        private void OnGUI()
        {
            DrawToolbar();
            RebuildPoints();

            if (_points.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    _mode == DataMode.EnemySkills
                        ? "몬스터 AbilitySetSO를 선택하세요."
                    : "AbilitySetSO를 선택하세요.",
                    MessageType.Info);
                DrawExcluded();
                return;
            }

            DrawPlot();
            DrawDominantList();
            DrawExcluded();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                _mode = (DataMode)EditorGUILayout.EnumPopup(_mode, EditorStyles.toolbarPopup, GUILayout.Width(110f));

                if (_mode == DataMode.EnemySkills)
                    _enemyData = (AbilitySetSO)EditorGUILayout.ObjectField(_enemyData, typeof(AbilitySetSO), false, GUILayout.Width(220f));
                else
            _playerData = (AbilitySetSO)EditorGUILayout.ObjectField(_playerData, typeof(AbilitySetSO), false, GUILayout.Width(220f));

                GUILayout.Space(8f);
                EditorGUILayout.LabelField("리스크(X)", GUILayout.Width(56f));
                _riskAxis = (RiskAxis)EditorGUILayout.EnumPopup(_riskAxis, EditorStyles.toolbarPopup, GUILayout.Width(150f));
                EditorGUILayout.LabelField("리워드(Y)", GUILayout.Width(58f));
                _rewardAxis = (RewardAxis)EditorGUILayout.EnumPopup(_rewardAxis, EditorStyles.toolbarPopup, GUILayout.Width(150f));
                GUILayout.FlexibleSpace();
            }
        }

        #region Point Building

        private void RebuildPoints()
        {
            _points.Clear();
            _excluded.Clear();

            if (_mode == DataMode.EnemySkills)
                BuildEnemyPoints();
            else
                BuildPlayerPoints();

            MarkDominant();
        }

        private void BuildEnemyPoints()
        {
            List<AbilityAttackEditorUtility.Entry> entries =
                AbilityAttackEditorUtility.Collect(_enemyData, true);
            for (int i = 0; i < entries.Count; i++)
            {
                AbilityAttackInfo skill = entries[i].AttackInfo;
                if (skill?.baseInfo == null || skill.skillType != SkillType.Attack)
                    continue;

                string name = $"[{i}] {skill.baseInfo.motionKey}";
                float cooldown = Mathf.Max(
                    0.05f,
                    entries[i].Ability?.cooldown?.durationSeconds ?? 0f);
                if (!TryGetRisk(skill.baseInfo, cooldown, name, out float risk))
                    continue;

                float damage = BalanceAttackAnalyzer.SumDamage(skill.baseInfo);
                float reward = _rewardAxis switch
                {
                    RewardAxis.DamagePlusPoise => damage + BalanceAttackAnalyzer.SumPoiseDamage(skill.baseInfo),
                    RewardAxis.DamagePlusBreak => damage + BalanceAttackAnalyzer.SumBreakDamage(skill.baseInfo),
                    RewardAxis.DamagePerCooldown => damage / cooldown,
                    _ => damage,
                };

                _points.Add(new PlotPoint
                {
                    Name = name,
                    Category = skill.attackCategory.ToString(),
                    Risk = risk,
                    Reward = reward,
                    Color = GetEnemyCategoryColor(skill),
                });
            }
        }

        private void BuildPlayerPoints()
        {
            if (_playerData == null)
                return;

            PlayerCombatAbilityDataView view =
                PlayerCombatAbilityDataView.Build(_playerData);
            AddPlayerList("lite", view.liteComboAttackList, new Color(0.45f, 0.7f, 0.95f));
            AddPlayerList("heavy", view.heavyComboAttackList, new Color(0.95f, 0.6f, 0.3f));
            AddPlayerList("jump", view.jumpAttackList, new Color(0.6f, 0.85f, 0.5f));
            AddPlayerList("dash", view.dashAttackList, new Color(0.55f, 0.55f, 0.95f));
            AddPlayerList("skill", view.skillAttackList, new Color(0.9f, 0.4f, 0.5f));
            AddPlayerOne("counter", view.counterAttack, new Color(0.85f, 0.8f, 0.4f));
            AddPlayerOne("parryCounter", view.parryCounterAttack, new Color(0.85f, 0.8f, 0.4f));
            AddPlayerOne("entry", view.entryAttack, new Color(0.7f, 0.7f, 0.7f));
            AddPlayerOne("swapEvadeCounter", view.swapEvadeCounterAttack, new Color(0.85f, 0.8f, 0.4f));
            AddPlayerOne("swapSpecial", view.swapSpecialAttack, new Color(0.95f, 0.45f, 0.85f));
        }

        private void AddPlayerList(string prefix, List<AbilityAttackInfo> list, Color color)
        {
            if (list == null)
                return;
            for (int i = 0; i < list.Count; i++)
                AddPlayerOne($"{prefix}[{i}]", list[i], color);
        }

        private void AddPlayerOne(string slot, AbilityAttackInfo info, Color color)
        {
            if (info?.baseInfo == null)
                return;

            float damage = BalanceAttackAnalyzer.SumDamage(info.baseInfo);
            if (damage <= 0f)
                return;

            string name = $"{slot} {info.baseInfo.motionKey}";

            // 플레이어 공격에는 쿨다운 개념이 없어 쿨다운 축이면 프레임 축으로 대체 안내
            if (_riskAxis == RiskAxis.Cooldown)
            {
                _excluded.Add($"{name} — 플레이어 공격에는 쿨다운이 없습니다. 리스크 축을 선딜+후딜로 변경하세요.");
                return;
            }

            if (!TryGetRisk(info.baseInfo, 0f, name, out float risk))
                return;

            float reward = _rewardAxis switch
            {
                RewardAxis.DamagePlusPoise => damage + BalanceAttackAnalyzer.SumPoiseDamage(info.baseInfo),
                RewardAxis.DamagePlusBreak => damage + BalanceAttackAnalyzer.SumBreakDamage(info.baseInfo),
                RewardAxis.DamagePerCooldown => damage, // 쿨다운 없음 — 데미지로 대체
                _ => damage,
            };

            _points.Add(new PlotPoint
            {
                Name = name,
                Category = slot,
                Risk = risk,
                Reward = reward,
                Color = color,
            });
        }

        private bool TryGetRisk(AttackInfoBase baseInfo, float cooldown, string name, out float risk)
        {
            risk = 0f;
            if (_riskAxis == RiskAxis.Cooldown)
            {
                risk = cooldown;
                return true;
            }

            AttackMotionAnalysisResult analysis = baseInfo.GetHitPhase(0)?.reactionProfile?.analysis;
            bool hasFrameData = analysis != null && (analysis.startupDuration > 0f || analysis.recoveryDuration > 0f || analysis.activeDuration > 0f);
            if (!hasFrameData)
            {
                _excluded.Add($"{name} — 프레임 분석 데이터 없음 (Attack Generator의 자동 리액션 분석 필요)");
                return false;
            }

            risk = _riskAxis == RiskAxis.StartupPlusRecovery
                ? analysis.startupDuration + analysis.recoveryDuration
                : analysis.startupDuration + analysis.activeDuration + analysis.recoveryDuration;
            return true;
        }

        private static Color GetEnemyCategoryColor(AbilityAttackInfo skill)
        {
            if (BalanceAttackAnalyzer.IsStrongEnemyAttack(skill))
                return skill.attackCategory == AbilityAttackCategory.Skill
                    ? new Color(0.9f, 0.35f, 0.35f)
                    : new Color(0.95f, 0.6f, 0.3f);
            return new Color(0.45f, 0.7f, 0.95f);
        }

        /// <summary>중앙값 기준 저리스크·고리워드 사분면에 있는 점을 지배적 후보로 표시.</summary>
        private void MarkDominant()
        {
            if (_points.Count < 3)
                return;

            float medianRisk = Median(p => p.Risk);
            float medianReward = Median(p => p.Reward);
            for (int i = 0; i < _points.Count; i++)
                _points[i].Dominant = _points[i].Risk < medianRisk && _points[i].Reward > medianReward;
        }

        private float Median(System.Func<PlotPoint, float> selector)
        {
            var values = new List<float>(_points.Count);
            for (int i = 0; i < _points.Count; i++)
                values.Add(selector(_points[i]));
            values.Sort();
            return values[values.Count / 2];
        }

        #endregion

        #region Drawing

        private void DrawPlot()
        {
            Rect area = GUILayoutUtility.GetRect(0f, 300f, GUILayout.ExpandWidth(true));
            var plot = new Rect(area.x + 56f, area.y + 10f, area.width - 76f, area.height - 42f);

            float minRisk = float.MaxValue, maxRisk = float.MinValue;
            float minReward = float.MaxValue, maxReward = float.MinValue;
            for (int i = 0; i < _points.Count; i++)
            {
                minRisk = Mathf.Min(minRisk, _points[i].Risk);
                maxRisk = Mathf.Max(maxRisk, _points[i].Risk);
                minReward = Mathf.Min(minReward, _points[i].Reward);
                maxReward = Mathf.Max(maxReward, _points[i].Reward);
            }
            if (maxRisk - minRisk < 0.001f) maxRisk = minRisk + 1f;
            if (maxReward - minReward < 0.001f) maxReward = minReward + 1f;

            // 보기 좋게 0부터 시작
            minRisk = Mathf.Min(minRisk, 0f);
            minReward = Mathf.Min(minReward, 0f);

            for (int i = 0; i < _points.Count; i++)
            {
                PlotPoint p = _points[i];
                float nx = (p.Risk - minRisk) / (maxRisk - minRisk);
                float ny = (p.Reward - minReward) / (maxReward - minReward);
                float x = plot.x + nx * plot.width;
                float y = plot.yMax - ny * plot.height;
                p.ScreenRect = new Rect(x - 4f, y - 4f, 8f, 8f);
            }

            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(plot, new Color(0.12f, 0.12f, 0.14f));

                // 중앙값 사분면 가이드
                if (_points.Count >= 3)
                {
                    float medianRisk = Median(p => p.Risk);
                    float medianReward = Median(p => p.Reward);
                    float gx = plot.x + (medianRisk - minRisk) / (maxRisk - minRisk) * plot.width;
                    float gy = plot.yMax - (medianReward - minReward) / (maxReward - minReward) * plot.height;
                    EditorGUI.DrawRect(new Rect(gx, plot.y, 1f, plot.height), new Color(1f, 1f, 1f, 0.12f));
                    EditorGUI.DrawRect(new Rect(plot.x, gy, plot.width, 1f), new Color(1f, 1f, 1f, 0.12f));
                }

                for (int i = 0; i < _points.Count; i++)
                {
                    PlotPoint p = _points[i];
                    if (p.Dominant)
                    {
                        var halo = new Rect(p.ScreenRect.x - 3f, p.ScreenRect.y - 3f, p.ScreenRect.width + 6f, p.ScreenRect.height + 6f);
                        EditorGUI.DrawRect(halo, new Color(1f, 0.3f, 0.3f, 0.45f));
                    }
                    EditorGUI.DrawRect(p.ScreenRect, p.Color);
                }

                // 축 라벨
                GUI.Label(new Rect(plot.x, plot.yMax + 4f, 160f, 16f), $"리스크 {minRisk:0.##} ~ {maxRisk:0.##}s", EditorStyles.miniLabel);
                var vertical = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.UpperLeft };
                GUI.Label(new Rect(area.x + 2f, plot.y, 54f, 16f), $"{maxReward:0.#}", vertical);
                GUI.Label(new Rect(area.x + 2f, plot.yMax - 16f, 54f, 16f), $"{minReward:0.#}", vertical);

                DrawHoverTooltip(plot);
            }

            if (Event.current.type == EventType.MouseMove && area.Contains(Event.current.mousePosition))
                Repaint();
        }

        private void DrawHoverTooltip(Rect plot)
        {
            Vector2 mouse = Event.current.mousePosition;
            if (!plot.Contains(mouse))
                return;

            PlotPoint nearest = null;
            float bestDistance = 14f;
            for (int i = 0; i < _points.Count; i++)
            {
                float distance = Vector2.Distance(mouse, _points[i].ScreenRect.center);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    nearest = _points[i];
                }
            }

            if (nearest == null)
                return;

            string text = $"{nearest.Name}  |  리스크 {nearest.Risk:0.##}s / 리워드 {nearest.Reward:0.#}";
            Vector2 size = EditorStyles.helpBox.CalcSize(new GUIContent(text));
            var tooltipRect = new Rect(
                Mathf.Min(mouse.x + 12f, plot.xMax - size.x),
                Mathf.Max(mouse.y - size.y - 4f, plot.y),
                size.x,
                size.y);
            GUI.Label(tooltipRect, text, EditorStyles.helpBox);
        }

        private void DrawDominantList()
        {
            var dominants = new List<PlotPoint>();
            for (int i = 0; i < _points.Count; i++)
                if (_points[i].Dominant)
                    dominants.Add(_points[i]);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField($"지배적 후보 (저리스크·고리워드) — {dominants.Count}건", EditorStyles.boldLabel);
                if (dominants.Count == 0)
                {
                    EditorGUILayout.LabelField("중앙값 기준 지배적 사분면에 있는 공격이 없습니다.", EditorStyles.centeredGreyMiniLabel);
                    return;
                }

                dominants.Sort((a, b) => b.Reward.CompareTo(a.Reward));
                for (int i = 0; i < dominants.Count; i++)
                    EditorGUILayout.LabelField($"• {dominants[i].Name} — 리스크 {dominants[i].Risk:0.##}s / 리워드 {dominants[i].Reward:0.#} ({dominants[i].Category})", EditorStyles.miniLabel);
            }
        }

        private void DrawExcluded()
        {
            if (_excluded.Count == 0)
                return;

            _listScroll = EditorGUILayout.BeginScrollView(_listScroll, GUILayout.MaxHeight(90f));
            EditorGUILayout.LabelField($"제외된 공격 {_excluded.Count}건", EditorStyles.boldLabel);
            for (int i = 0; i < _excluded.Count; i++)
                EditorGUILayout.LabelField($"· {_excluded[i]}", EditorStyles.miniLabel);
            EditorGUILayout.EndScrollView();
        }

        #endregion
    }
}
#endif
