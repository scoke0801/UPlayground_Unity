using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Editor
{
    /// <summary>
    /// VitalOrbDataSO 커스텀 에디터.
    /// 등급별 컬러 강조 + 수치 미리보기를 제공합니다.
    /// </summary>
    [CustomEditor(typeof(VitalOrbDataSO))]
    public class VitalOrbDataEditor : UnityEditor.Editor
    {
        private static readonly Color ColorS = new Color(1.00f, 0.84f, 0.00f, 0.15f);
        private static readonly Color ColorA = new Color(0.00f, 0.75f, 1.00f, 0.12f);
        private static readonly Color ColorB = new Color(0.85f, 0.85f, 0.85f, 0.08f);

        public override void OnInspectorGUI()
        {
            var data = (VitalOrbDataSO)target;

            // 등급별 배경 컬러 헤더
            DrawGradeHeader(data.grade);

            EditorGUILayout.Space(4);
            DrawDefaultInspector();
            EditorGUILayout.Space(12);
            DrawPreviewBox(data);
        }

        private static void DrawGradeHeader(VitalOrbGrade grade)
        {
            Color bg = grade switch
            {
                VitalOrbGrade.S => ColorS,
                VitalOrbGrade.A => ColorA,
                _           => ColorB,
            };

            string label = grade switch
            {
                VitalOrbGrade.S => "★ S등급 — Soul Orb",
                VitalOrbGrade.A => "◆ A등급 — Guard Shard",
                _           => "● B등급 — Battle Chip",
            };

            var rect = GUILayoutUtility.GetRect(0, 28, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, bg);

            var style = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize  = 13,
                alignment = TextAnchor.MiddleCenter,
            };
            EditorGUI.LabelField(rect, label, style);
        }

        private static void DrawPreviewBox(VitalOrbDataSO data)
        {
            EditorGUILayout.LabelField("수치 미리보기", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            DrawRow("체력 회복",   $"최대 HP의 {data.healAmount * 100:F1}%");
            DrawRow("게이지 회복", $"{data.gaugeAmount:F0} / 100");
            DrawRow("탐지 반경",   $"{data.collectRadius:F1} m");
            DrawRow("흡입 속도",   $"{data.attractSpeed:F1} m/s");
            DrawRow("수명",        $"{data.lifetime:F1} 초");

            EditorGUILayout.Space(4);

            // 회복량 시각 바
            float hpRatio = Mathf.Clamp01(data.healAmount);
            DrawBar("HP", hpRatio, Color.green);
            DrawBar("게이지", data.gaugeAmount / 100f, new Color(0.4f, 0.7f, 1f));

            EditorGUILayout.EndVertical();
        }

        private static void DrawRow(string label, string value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(80));
            EditorGUILayout.LabelField(value, EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawBar(string label, float ratio, Color color)
        {
            EditorGUILayout.LabelField(label, EditorStyles.miniLabel);
            var rect = GUILayoutUtility.GetRect(0, 8, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(0.1f, 0.1f, 0.1f, 0.4f));
            var fill = rect;
            fill.width *= Mathf.Clamp01(ratio);
            EditorGUI.DrawRect(fill, color);
            EditorGUILayout.Space(2);
        }
    }
}
