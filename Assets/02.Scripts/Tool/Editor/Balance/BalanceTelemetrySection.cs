#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Tool.Editor.Balance
{
    /// <summary>
    /// Balance Designer 상세 패널의 "추정 vs 실측" 비교 섹션.
    /// 추정치(BalanceCombatEstimator)와 실플레이 텔레메트리(CombatTelemetryImporter)의 괴리를 표시해
    /// 수식 가정(피격률/회피율/플레이어 DPS)이 실제 플레이와 맞는지 검증한다.
    /// </summary>
    public static class BalanceTelemetrySection
    {
        public static void Draw(BalanceScenarioResult result, BalanceScenarioAsset scenario)
        {
            if (result?.Actor == null)
                return;

            EditorGUILayout.Space(4f);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("실측 텔레메트리", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    string loadInfo = CombatTelemetryImporter.HasData
                        ? $"세션 {CombatTelemetryImporter.SessionCount} / 인카운터 {CombatTelemetryImporter.TotalEncounters} (로드 {CombatTelemetryImporter.LoadedAt})"
                        : "데이터 없음";
                    EditorGUILayout.LabelField(loadInfo, EditorStyles.miniLabel, GUILayout.Width(240f));
                    if (GUILayout.Button("새로고침", GUILayout.Width(64f)))
                        CombatTelemetryImporter.Reload();
                    if (GUILayout.Button("폴더", GUILayout.Width(40f)))
                    {
                        System.IO.Directory.CreateDirectory(CombatTelemetryImporter.DirectoryPath);
                        EditorUtility.RevealInFinder(CombatTelemetryImporter.DirectoryPath);
                    }
                }

                if (!CombatTelemetryImporter.HasData)
                {
                    EditorGUILayout.LabelField("플레이 세션 종료 시 persistentDataPath/CombatTelemetry에 자동 저장됩니다. 플레이 후 [새로고침]을 누르세요.", EditorStyles.centeredGreyMiniLabel);
                    return;
                }

                if (!CombatTelemetryImporter.TryGet(result.Actor.actorId, out CombatTelemetryImporter.ActorTelemetry telemetry))
                {
                    EditorGUILayout.LabelField($"'{result.Actor.actorId}'의 실측 인카운터가 없습니다.", EditorStyles.centeredGreyMiniLabel);
                    return;
                }

                EditorGUILayout.LabelField(
                    $"인카운터 {telemetry.EncounterCount} / 처치 {telemetry.KillCount} / 플레이어 사망 {telemetry.PlayerDeathCount}",
                    EditorStyles.miniLabel);

                float estimatedKill = !float.IsPositiveInfinity(result.MonsterTimeToDeathWithBreak) && result.MonsterTimeToDeathWithBreak > 0f
                    ? result.MonsterTimeToDeathWithBreak
                    : result.MonsterTimeToDeath;

                if (telemetry.KillCount > 0)
                {
                    DrawComparisonRow("처치 시간 (TTK)", estimatedKill, telemetry.MedianKillTime, "s",
                        "실측 중앙값이 추정과 0.65~1.6배 범위면 수식 가정이 유효합니다.");
                }
                else
                {
                    EditorGUILayout.LabelField("처치 기록 없음 — TTK 비교 불가", EditorStyles.miniLabel);
                }

                if (telemetry.KillCount > 0 && telemetry.MedianKillTime > 0f && result.EnemyExpectedDps > 0f)
                {
                    float estimatedDamageTaken = result.EnemyExpectedDps * telemetry.MedianKillTime;
                    DrawComparisonRow("받은 피해 (인카운터당)", estimatedDamageTaken, telemetry.AvgDamageToPlayer, "", null);
                }

                // 방어 가정 vs 실측 — 시나리오의 확률 가정을 실플레이 비율과 비교
                float assumedHitRate = scenario != null ? scenario.hitReceiveRate : 0.45f;
                float assumedDodge = scenario != null ? scenario.dodgeSuccessRate : 0.15f;
                float assumedParry = scenario != null ? scenario.parrySuccessRate : 0.05f;
                EditorGUILayout.LabelField(
                    $"피격률 가정 {assumedHitRate * 100f:F0}% vs 실측 {telemetry.HitReceiveRate * 100f:F0}%  |  " +
                    $"회피 가정 {assumedDodge * 100f:F0}% vs 실측 {telemetry.DodgeRate * 100f:F0}%  |  " +
                    $"패리 가정 {assumedParry * 100f:F0}% vs 실측 {telemetry.ParryRate * 100f:F0}%  |  " +
                    $"가드 실측 {telemetry.GuardRate * 100f:F0}%",
                    EditorStyles.miniLabel);
            }
        }

        private static void DrawComparisonRow(string label, float estimated, float measured, string unit, string tooltip)
        {
            float ratio = estimated > 0f ? measured / estimated : 0f;
            Color color = ratio >= 0.65f && ratio <= 1.6f
                ? new Color(0.55f, 0.85f, 0.55f)
                : new Color(0.95f, 0.55f, 0.45f);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(new GUIContent(label, tooltip), GUILayout.Width(160f));
                EditorGUILayout.LabelField($"추정 {estimated:F1}{unit}", GUILayout.Width(110f));
                EditorGUILayout.LabelField($"실측 {measured:F1}{unit}", GUILayout.Width(110f));

                Color previous = GUI.contentColor;
                GUI.contentColor = color;
                EditorGUILayout.LabelField(ratio > 0f ? $"×{ratio:F2}" : "-", EditorStyles.boldLabel, GUILayout.Width(60f));
                GUI.contentColor = previous;
            }
        }
    }
}
#endif
