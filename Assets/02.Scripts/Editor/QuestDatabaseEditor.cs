#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Editor.Authoring;
using UPlayGround.Data.Quest;
using UPlayGround.Editor.Authoring;
using UPlayGround.Tool.Editor;

namespace UPlayGround.Editor
{
    /// <summary>
    /// QuestDatabase 인스펙터 커스텀 에디터.
    /// 폴더를 지정해 QuestSO를 일괄 스캔하거나 Quest Editor 창을 열 수 있다.
    /// </summary>
    [CustomEditor(typeof(QuestDatabase))]
    public class QuestDatabaseEditor : UnityEditor.Editor
    {
        private string _folderPath = "Assets/10.Datas/Quest";

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var db = (QuestDatabase)target;

            EditorGUILayout.Space(8);
            EditorGUILayout.BeginVertical("helpBox");
            EditorGUILayout.LabelField("Quest Database 관리", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("스캔 폴더", GUILayout.Width(72));
            _folderPath = EditorGUILayout.TextField(_folderPath);
            if (GUILayout.Button("...", GUILayout.Width(28)))
            {
                string selected = EditorUtility.OpenFolderPanel("QuestSO 폴더 선택", _folderPath, "");
                if (!string.IsNullOrEmpty(selected))
                {
                    string projectPath = System.IO.Path.GetFullPath(Application.dataPath + "/..");
                    if (selected.StartsWith(projectPath))
                        _folderPath = "Assets" + selected.Substring(projectPath.Length).Replace('\\', '/');
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            if (GUILayout.Button("DB 갱신 (폴더 스캔)", GUILayout.Height(28)))
                db.RefreshDatabase(_folderPath);

            if (GUILayout.Button("QuestIdType Enum 생성", GUILayout.Height(24)))
                GenerateEnum(db);

            if (GUILayout.Button("Quest Editor 열기", GUILayout.Height(24)))
                DataAuthoringHubWindow.Open(QuestDomainPanel.DomainKey);

            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(
                $"등록된 퀘스트: {db.QuestList.Count}개\n" +
                "① DB 갱신으로 QuestSO를 등록한 뒤\n" +
                "② Enum 생성으로 QuestIdType.cs를 갱신하세요.",
                MessageType.Info);

            EditorGUILayout.EndVertical();
        }

        private static void GenerateEnum(QuestDatabase db)
        {
            const string outputPath = "Assets/02.Scripts/Data/Quest/QuestIdType.cs";
            var raw = new List<(string, string)>();
            foreach (var q in db.QuestList)
            {
                if (q == null || string.IsNullOrEmpty(q.questId)) continue;
                raw.Add((q.questId, q.questId));
            }
            var entries = IdEnumGeneratorUtility.DeduplicateEntries(raw);
            bool ok = IdEnumGeneratorUtility.GenerateStringKeyEnum(
                "QuestIdType", "ToQuestId", "Quest ID",
                outputPath, "UPlayGround.Data.Quest", entries);

            if (ok)
            {
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("Enum 생성 완료",
                    $"QuestIdType 생성 완료 ({entries.Count}개)\n→ {outputPath}", "확인");
            }
        }
    }
}
#endif
