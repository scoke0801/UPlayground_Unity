using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Editor
{
    /// <summary>
    /// VitalOrbTriggerConfig 커스텀 에디터.
    /// - 트리거 항목을 시각적 카드 형태로 표시
    /// - 30초 전투 기대값 시뮬레이션 내장
    /// </summary>
    [CustomEditor(typeof(VitalOrbTriggerConfig))]
    public class VitalOrbTriggerConfigEditor : UnityEditor.Editor
    {
        // 30초 기준 각 트리거 예상 발생 횟수 (설계서 7장 기준)
        private static readonly Dictionary<VitalOrbTrigger, int> SimCounts = new()
        {
            { VitalOrbTrigger.FinishAttackHit, 1 },
            { VitalOrbTrigger.KillKillCam,     2 },
            { VitalOrbTrigger.PerfectGuard,    3 },
            { VitalOrbTrigger.Dodge,           8 },
            { VitalOrbTrigger.Guard,           5 },
            { VitalOrbTrigger.HeavyAttackHit, 10 },
            { VitalOrbTrigger.LightAttackHit, 30 },
        };

        private static readonly Color ColorS   = new Color(1.00f, 0.84f, 0.00f, 1f); // 금색
        private static readonly Color ColorA   = new Color(0.00f, 0.75f, 1.00f, 1f); // 하늘색
        private static readonly Color ColorB   = new Color(0.85f, 0.85f, 0.85f, 1f); // 흰색
        private static readonly Color ColorBg  = new Color(0.15f, 0.18f, 0.22f, 1f);

        private bool _showSimulation = true;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawDefaultInspector();

            EditorGUILayout.Space(16);
            DrawDivider();

            DrawTriggerSummary();

            EditorGUILayout.Space(8);
            DrawDivider();

            DrawSimulation();

            serializedObject.ApplyModifiedProperties();
        }

        // -----------------------------------------------------------
        // 트리거 요약 카드
        // -----------------------------------------------------------
        private void DrawTriggerSummary()
        {
            EditorGUILayout.LabelField("트리거 요약", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            var config = (VitalOrbTriggerConfig)target;
            if (config.entries == null || config.entries.Count == 0)
            {
                EditorGUILayout.HelpBox("트리거 항목이 없습니다.", MessageType.Info);
                return;
            }

            foreach (var entry in config.entries)
            {
                if (entry.dropData == null) continue;

                Color gradeColor = GetGradeColor(entry.dropData.grade);

                var boxStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    padding = new RectOffset(10, 10, 6, 6)
                };

                EditorGUILayout.BeginVertical(boxStyle);

                // 헤더 행
                EditorGUILayout.BeginHorizontal();
                DrawGradeBadge(entry.dropData.grade);
                GUILayout.Label(entry.trigger.ToString(), EditorStyles.boldLabel, GUILayout.ExpandWidth(true));
                GUILayout.Label($"{entry.dropData.name}", EditorStyles.miniLabel, GUILayout.Width(80));
                EditorGUILayout.EndHorizontal();

                // 수치 행
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(32);

                DrawLabeledValue("확률",   $"{entry.probability * 100:F0}%");
                DrawLabeledValue("중첩",   $"최대 {entry.maxStack}개");
                DrawLabeledValue("쿨다운", entry.cooldown > 0f ? $"{entry.cooldown}s" : "없음");
                DrawLabeledValue("HP 회복", $"+{entry.dropData.healAmount * 100:F0}%");
                DrawLabeledValue("게이지", $"+{entry.dropData.gaugeAmount:F0}");

                EditorGUILayout.EndHorizontal();

                // 확률 바
                EditorGUILayout.Space(3);
                DrawProbabilityBar(entry.probability, gradeColor);

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(3);
            }
        }

        // -----------------------------------------------------------
        // 시뮬레이션
        // -----------------------------------------------------------
        private void DrawSimulation()
        {
            _showSimulation = EditorGUILayout.Foldout(_showSimulation, "⚡ 30초 전투 기대값 시뮬레이션", true, EditorStyles.foldoutHeader);
            if (!_showSimulation) return;

            EditorGUILayout.Space(4);

            var config = (VitalOrbTriggerConfig)target;

            // 테이블 헤더
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("트리거",     EditorStyles.miniLabel, GUILayout.Width(140));
            GUILayout.Label("횟수",       EditorStyles.miniLabel, GUILayout.Width(36));
            GUILayout.Label("확률",       EditorStyles.miniLabel, GUILayout.Width(44));
            GUILayout.Label("기대 드롭",  EditorStyles.miniLabel, GUILayout.Width(60));
            GUILayout.Label("HP 회복",    EditorStyles.miniLabel, GUILayout.Width(60));
            GUILayout.Label("게이지",     EditorStyles.miniLabel, GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();

            DrawDivider();

            float totalDrops = 0f, totalHp = 0f, totalGauge = 0f;

            foreach (var entry in config.entries)
            {
                if (entry.dropData == null) continue;
                if (!SimCounts.TryGetValue(entry.trigger, out int count)) continue;

                float expectedDrops = count * entry.probability;
                float expHp         = expectedDrops * entry.dropData.healAmount * 100f;
                float expGauge      = expectedDrops * entry.dropData.gaugeAmount;

                totalDrops += expectedDrops;
                totalHp    += expHp;
                totalGauge += expGauge;

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(entry.trigger.ToString(), GUILayout.Width(140));
                GUILayout.Label($"{count}회",             GUILayout.Width(36));
                GUILayout.Label($"{entry.probability * 100:F0}%", GUILayout.Width(44));
                GUILayout.Label($"{expectedDrops:F1}개",  GUILayout.Width(60));

                var oldColor = GUI.contentColor;
                GUI.contentColor = Color.green;
                GUILayout.Label($"+{expHp:F1}%", GUILayout.Width(60));
                GUI.contentColor = new Color(0.4f, 0.7f, 1f);
                GUILayout.Label($"+{expGauge:F1}", GUILayout.Width(50));
                GUI.contentColor = oldColor;

                EditorGUILayout.EndHorizontal();
            }

            DrawDivider();

            // 합계 행
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("합계", EditorStyles.boldLabel, GUILayout.Width(140));
            GUILayout.Space(80);

            var c = GUI.contentColor;
            GUI.contentColor = Color.yellow;
            GUILayout.Label($"~{totalDrops:F1}개", EditorStyles.boldLabel, GUILayout.Width(60));
            GUI.contentColor = Color.green;
            GUILayout.Label($"+{totalHp:F1}%",     EditorStyles.boldLabel, GUILayout.Width(60));
            GUI.contentColor = new Color(0.4f, 0.7f, 1f);
            GUILayout.Label($"+{totalGauge:F0}",   EditorStyles.boldLabel, GUILayout.Width(50));
            GUI.contentColor = c;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);

            // HP 회복 바
            DrawSimBar("HP 회복 (숙련 30초 기준)", totalHp, 100f, Color.green);
            DrawSimBar("게이지 획득 (100 기준)",   totalGauge, 200f, new Color(0.4f, 0.7f, 1f));
        }

        // -----------------------------------------------------------
        // UI 헬퍼
        // -----------------------------------------------------------
        private static void DrawGradeBadge(VitalOrbGrade grade)
        {
            var style = new GUIStyle(EditorStyles.miniButtonMid)
            {
                fixedWidth  = 22,
                fixedHeight = 18,
                fontStyle   = FontStyle.Bold,
                normal = { textColor = GetGradeColor(grade) }
            };
            GUILayout.Label(grade.ToString(), style, GUILayout.Width(22));
        }

        private static void DrawLabeledValue(string label, string value)
        {
            GUILayout.Label($"{label}: ", EditorStyles.miniLabel, GUILayout.Width(48));
            GUILayout.Label(value, EditorStyles.miniBoldLabel, GUILayout.Width(52));
        }

        private static void DrawProbabilityBar(float prob, Color color)
        {
            Rect rect = GUILayoutUtility.GetRect(0, 5, GUILayout.ExpandWidth(true));
            rect.x      += 32;
            rect.width  -= 32;

            EditorGUI.DrawRect(rect, new Color(0.1f, 0.1f, 0.1f, 0.5f));
            var fillRect = rect;
            fillRect.width *= Mathf.Clamp01(prob);
            EditorGUI.DrawRect(fillRect, color);
        }

        private static void DrawSimBar(string label, float value, float max, Color color)
        {
            EditorGUILayout.LabelField(label, EditorStyles.miniLabel);
            Rect rect = GUILayoutUtility.GetRect(0, 10, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(0.1f, 0.1f, 0.1f, 0.5f));
            var fill = rect;
            fill.width *= Mathf.Clamp01(value / max);
            EditorGUI.DrawRect(fill, color);
            EditorGUILayout.Space(4);
        }

        private static void DrawDivider()
        {
            var rect = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(0.3f, 0.3f, 0.3f, 1f));
            EditorGUILayout.Space(4);
        }

        private static Color GetGradeColor(VitalOrbGrade grade) => grade switch
        {
            VitalOrbGrade.S => ColorS,
            VitalOrbGrade.A => ColorA,
            _           => ColorB,
        };
    }
}
