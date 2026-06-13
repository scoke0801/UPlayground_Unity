#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Tool.Editor.Balance
{
    /// <summary>
    /// 밸런스 수치 CSV 왕복 편집 창.
    /// 몬스터 스탯 / 적 스킬 데이터를 CSV로 내보내 Excel·시트에서 일괄 수정한 뒤 다시 적용한다.
    /// 가져오기 전 Balance Audit 창에서 베이스라인을 저장해 두면 diff로 변경 검증이 가능하다.
    /// </summary>
    public sealed class BalanceCsvWindow : EditorWindow
    {
        private readonly List<string> _report = new();
        private Vector2 _reportScroll;

        [MenuItem("UPlayGround/게임플레이/밸런스/밸런스 CSV 편집", priority = UPlaygroundMenuPriority.GameplayBalance + 2)]
        public static void Open()
        {
            var window = GetWindow<BalanceCsvWindow>();
            window.titleContent = new GUIContent("Balance CSV");
            window.minSize = new Vector2(560f, 360f);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("몬스터 스탯 (actorId 기준, stat:* 컬럼)", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("내보내기", GUILayout.Height(24f)))
                {
                    string path = EditorUtility.SaveFilePanel("몬스터 스탯 CSV 내보내기", "", "monster_stats.csv", "csv");
                    if (!string.IsNullOrEmpty(path))
                        RunWithReport(() => BalanceCsvService.ExportMonsterStats(path, _report));
                }

                if (GUILayout.Button("가져오기", GUILayout.Height(24f)))
                {
                    string path = EditorUtility.OpenFilePanel("몬스터 스탯 CSV 가져오기", "", "csv");
                    if (!string.IsNullOrEmpty(path) && ConfirmImport())
                        RunWithReport(() => BalanceCsvService.ImportMonsterStats(path, _report));
                }
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("적 스킬 (attackDataPath + skillIndex/phaseIndex 기준)", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("내보내기", GUILayout.Height(24f)))
                {
                    string path = EditorUtility.SaveFilePanel("적 스킬 CSV 내보내기", "", "enemy_skills.csv", "csv");
                    if (!string.IsNullOrEmpty(path))
                        RunWithReport(() => BalanceCsvService.ExportEnemySkills(path, _report));
                }

                if (GUILayout.Button("가져오기", GUILayout.Height(24f)))
                {
                    string path = EditorUtility.OpenFilePanel("적 스킬 CSV 가져오기", "", "csv");
                    if (!string.IsNullOrEmpty(path) && ConfirmImport())
                        RunWithReport(() => BalanceCsvService.ImportEnemySkills(path, _report));
                }
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.HelpBox(
                "규칙: 빈 칸은 건드리지 않음 · animKey가 에셋과 다른 행은 건너뜀(순서 변경 안전장치) · " +
                "스킬 단위 필드(weight/cooldown/range/level)는 phaseIndex 0 행만 반영 · 가져오기는 Undo 가능.",
                MessageType.Info);

            EditorGUILayout.LabelField("리포트", EditorStyles.boldLabel);
            _reportScroll = EditorGUILayout.BeginScrollView(_reportScroll, EditorStyles.helpBox);
            if (_report.Count == 0)
                EditorGUILayout.LabelField("아직 실행한 작업이 없습니다.", EditorStyles.centeredGreyMiniLabel);
            for (int i = 0; i < _report.Count; i++)
                EditorGUILayout.LabelField(_report[i], EditorStyles.miniLabel);
            EditorGUILayout.EndScrollView();
        }

        private static bool ConfirmImport()
        {
            return EditorUtility.DisplayDialog(
                "CSV 가져오기",
                "CSV 값을 에셋에 적용합니다. 가져오기 전에 Balance Audit 창에서 베이스라인 저장을 권장합니다.\n계속할까요?",
                "적용",
                "취소");
        }

        private void RunWithReport(System.Action action)
        {
            _report.Clear();
            action();
            Repaint();
        }
    }
}
#endif
