#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Tool.Editor.Balance
{
    /// <summary>
    /// Balance Designer 상세 패널의 몬테카를로 시뮬레이션 섹션.
    /// 추정(기대값) 옆에 N회 시뮬레이션의 TTK 분포/사망률을 보여 분산까지 확인한다.
    /// </summary>
    public static class BalanceSimulationSection
    {
        private static int _runs = 500;
        private static int _seed = 12345;
        private static bool _expanded;
        private static readonly Dictionary<string, BalanceMonteCarloSimulator.SimulationResult> _cache = new();

        public static void Draw(BalanceScenarioResult result, BalanceScenarioAsset scenario, BalanceScenarioInput fallback)
        {
            if (result?.Actor == null)
                return;

            EditorGUILayout.Space(4f);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    _expanded = EditorGUILayout.Foldout(_expanded, "몬테카를로 시뮬레이션", true);
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField("Runs", GUILayout.Width(36f));
                    _runs = Mathf.Clamp(EditorGUILayout.IntField(_runs, GUILayout.Width(56f)), 50, 10000);
                    EditorGUILayout.LabelField("Seed", GUILayout.Width(34f));
                    _seed = EditorGUILayout.IntField(_seed, GUILayout.Width(64f));
                    if (GUILayout.Button("실행", GUILayout.Width(48f)))
                    {
                        _cache[result.Actor.actorId] = BalanceMonteCarloSimulator.Run(result.Actor, scenario, fallback, _runs, _seed);
                        _expanded = true;
                    }
                }

                if (!_expanded)
                    return;

                if (!_cache.TryGetValue(result.Actor.actorId, out BalanceMonteCarloSimulator.SimulationResult sim))
                {
                    EditorGUILayout.LabelField("[실행]을 눌러 N회 전투를 시뮬레이션합니다. 수식 추정이 못 잡는 분산(운에 따른 TTK 편차, 사망률)을 확인하는 용도입니다.", EditorStyles.centeredGreyMiniLabel);
                    return;
                }

                if (!string.IsNullOrEmpty(sim.Error))
                {
                    EditorGUILayout.HelpBox(sim.Error, MessageType.Warning);
                    return;
                }

                EditorGUILayout.LabelField(
                    $"{sim.Runs}회 — 처치 {sim.KillRate * 100f:F0}% / 플레이어 사망 {sim.DeathRate * 100f:F0}% / 타임아웃 {sim.Timeouts}",
                    EditorStyles.boldLabel);

                if (sim.KillTimes.Count > 0)
                {
                    float estimated = !float.IsPositiveInfinity(result.MonsterTimeToDeathWithBreak) && result.MonsterTimeToDeathWithBreak > 0f
                        ? result.MonsterTimeToDeathWithBreak
                        : result.MonsterTimeToDeath;

                    EditorGUILayout.LabelField(
                        $"TTK 분포: P10 {sim.KillP10:F1}s / P50 {sim.KillP50:F1}s / P90 {sim.KillP90:F1}s (평균 {sim.KillAvg:F1}s)  |  추정 {BalanceCombatEstimator.FormatTime(estimated)} / 목표 {result.TargetDuration:F0}s",
                        EditorStyles.miniLabel);
                    EditorGUILayout.LabelField(
                        $"인카운터당 받은 피해 평균 {sim.AvgDamageTakenPerFight:F0} (플레이어 HP {result.PlayerHealth:F0})",
                        EditorStyles.miniLabel);

                    DrawHistogram(sim, result.TargetDuration);
                }
                else
                {
                    EditorGUILayout.LabelField("처치 성공 케이스가 없습니다 — 플레이어 DPS/생존 가정을 확인하세요.", EditorStyles.miniLabel);
                }
            }
        }

        private static void DrawHistogram(BalanceMonteCarloSimulator.SimulationResult sim, float targetDuration)
        {
            if (sim.Histogram == null || sim.Histogram.Length == 0)
                return;

            const float height = 56f;
            Rect area = GUILayoutUtility.GetRect(0f, height + 18f, GUILayout.ExpandWidth(true));
            if (Event.current.type != EventType.Repaint)
                return;

            EditorGUI.DrawRect(new Rect(area.x, area.y, area.width, height), new Color(0.12f, 0.12f, 0.14f));

            int maxCount = 1;
            for (int i = 0; i < sim.Histogram.Length; i++)
                maxCount = Mathf.Max(maxCount, sim.Histogram[i]);

            float barWidth = area.width / sim.Histogram.Length;
            for (int i = 0; i < sim.Histogram.Length; i++)
            {
                float barHeight = height * sim.Histogram[i] / maxCount;
                var barRect = new Rect(area.x + i * barWidth + 1f, area.y + height - barHeight, barWidth - 2f, barHeight);
                EditorGUI.DrawRect(barRect, new Color(0.35f, 0.6f, 0.85f, 0.9f));
            }

            // 목표 TTK 기준선
            float range = sim.HistogramMax - sim.HistogramMin;
            if (range > 0f && targetDuration >= sim.HistogramMin && targetDuration <= sim.HistogramMax)
            {
                float x = area.x + (targetDuration - sim.HistogramMin) / range * area.width;
                EditorGUI.DrawRect(new Rect(x - 0.5f, area.y, 1.5f, height), new Color(0.95f, 0.75f, 0.3f));
            }

            GUI.Label(new Rect(area.x, area.y + height, 120f, 16f), $"{sim.HistogramMin:F1}s", EditorStyles.miniLabel);
            var rightStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleRight };
            GUI.Label(new Rect(area.xMax - 120f, area.y + height, 120f, 16f), $"{sim.HistogramMax:F1}s", rightStyle);
        }
    }
}
#endif
