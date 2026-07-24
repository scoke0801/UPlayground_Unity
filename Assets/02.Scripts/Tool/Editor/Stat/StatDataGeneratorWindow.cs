#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Actor;
using UPlayGround.Tool.Editor.Balance;

namespace UPlayGround.Tool.Editor.Stat
{
    /// <summary>
    /// 구 스탯 생성기 메뉴를 Attribute Profile 저작 흐름으로 연결하는 전환 창.
    /// ActorDefinition의 수치 원본은 AttributeProfileSO이며 이 창은 레거시 Stat 에셋을 만들지 않는다.
    /// </summary>
    public sealed class StatDataGeneratorWindow : EditorWindow
    {
        public static void Open()
        {
            var window = GetWindow<StatDataGeneratorWindow>();
            window.titleContent = new GUIContent("Attribute Profile");
            window.minSize = new Vector2(460f, 180f);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "ActorDefinition의 레거시 스탯 생성 경로는 제거되었습니다. " +
                "몬스터 기본값은 Attribute Profile 생성기에서 갱신하고, " +
                "개별 Profile은 Inspector에서 직접 편집하세요.",
                MessageType.Info);

            if (GUILayout.Button("몬스터 Attribute Profile 생성기 열기", GUILayout.Height(30f)))
                MonsterStatGeneratorWindow.Open();

            if (GUILayout.Button("ActorDefinition Profile 누락 검증", GUILayout.Height(26f)))
                ValidateStatDataCoverageMenu();
        }

        public static void ValidateStatDataCoverageMenu()
        {
            var missing = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:ActorDefinitionSO"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ActorDefinitionSO definition =
                    AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>(path);
                if (definition != null && definition.attributeProfile == null)
                    missing.Add(path);
            }

            if (missing.Count == 0)
            {
                Debug.Log("[Attribute Profile] ActorDefinition Profile 누락 0");
                EditorUtility.DisplayDialog(
                    "Attribute Profile 검증",
                    "모든 ActorDefinition에 Attribute Profile이 연결되어 있습니다.",
                    "확인");
                return;
            }

            string message = $"Attribute Profile 누락 {missing.Count}개\n" +
                             string.Join("\n", missing);
            Debug.LogError($"[Attribute Profile] {message}");
            EditorUtility.DisplayDialog("Attribute Profile 검증 실패", message, "확인");
        }
    }
}
#endif
