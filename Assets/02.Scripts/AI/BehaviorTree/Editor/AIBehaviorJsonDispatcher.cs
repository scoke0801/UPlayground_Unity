#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.AI.BehaviorTree.Editor
{
    /// <summary>
    /// JSON을 각 포맷 데이터 클래스로 try-parse하여 BT-node 포맷(BehaviorTreeJsonUtility)과
    /// Rules 포맷(MonsterBehaviorTreeJsonImporter) 중 알맞은 importer로 라우팅한다.
    /// 사용자가 두 포맷을 의식하지 않고 단일 메뉴로 import할 수 있도록 한다.
    /// </summary>
    public static class AIBehaviorJsonDispatcher
    {
        public enum BehaviorJsonFormat
        {
            Unknown,
            BehaviorTreeNode,
            MonsterBehaviorRules
        }

        [MenuItem("UPlayGround/Behavior Tree/Json/Import AI Json (Auto Detect)", false)]
        public static void ImportFromFilePanel()
        {
            var jsonPath = EditorUtility.OpenFilePanel("AI Json Import (Auto Detect)", Application.dataPath, "json");
            if (string.IsNullOrWhiteSpace(jsonPath))
                return;

            Import(jsonPath);
        }

        [MenuItem("Assets/UPlayGround/AI/Import AI Json (Auto Detect)", false, 2200)]
        public static void ImportSelectedAsset()
        {
            var assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrWhiteSpace(assetPath))
                return;

            Import(Path.GetFullPath(assetPath));
        }

        [MenuItem("Assets/UPlayGround/AI/Import AI Json (Auto Detect)", true)]
        public static bool CanImportSelectedAsset()
        {
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            return !string.IsNullOrWhiteSpace(path) && path.EndsWith(".json", StringComparison.OrdinalIgnoreCase);
        }

        public static BehaviorTreeAsset Import(string absoluteJsonPath)
        {
            if (!File.Exists(absoluteJsonPath))
            {
                Debug.LogError($"[BT] AI Json Import 실패: 파일을 찾을 수 없습니다. {absoluteJsonPath}");
                return null;
            }

            var json = File.ReadAllText(absoluteJsonPath);
            var format = DetectFormat(json);

            switch (format)
            {
                case BehaviorJsonFormat.BehaviorTreeNode:
                    return ImportBehaviorTreeNode(absoluteJsonPath);

                case BehaviorJsonFormat.MonsterBehaviorRules:
                    return ImportMonsterBehaviorRules(absoluteJsonPath);

                default:
                    Debug.LogError(
                        $"[BT] AI Json Import 실패: 포맷을 인식할 수 없습니다. " +
                        $"BT-node 포맷은 \"rootGuid\"+\"nodes\"가, Rules 포맷은 \"id\"+\"groups\"가 필요합니다. {absoluteJsonPath}");
                    return null;
            }
        }

        public static BehaviorJsonFormat DetectFormat(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return BehaviorJsonFormat.Unknown;

            // JsonUtility는 매칭되지 않는 필드는 기본값으로 둔다. 각 포맷의 식별 필드가
            // 채워졌는지로 판별하면 substring 매칭이 nested 키와 충돌하는 문제를 피할 수 있다.
            try
            {
                var btNode = JsonUtility.FromJson<BehaviorTreeJsonData>(json);
                if (btNode != null
                    && !string.IsNullOrEmpty(btNode.rootGuid)
                    && btNode.nodes != null
                    && btNode.nodes.Count > 0)
                {
                    return BehaviorJsonFormat.BehaviorTreeNode;
                }
            }
            catch (ArgumentException)
            {
                // 다음 포맷 시도
            }

            try
            {
                var rules = JsonUtility.FromJson<MonsterBehaviorTreeJson>(json);
                if (rules != null
                    && !string.IsNullOrEmpty(rules.id)
                    && ((rules.groups != null && rules.groups.Count > 0)
                        || (rules.rules != null && rules.rules.Count > 0)))
                {
                    return BehaviorJsonFormat.MonsterBehaviorRules;
                }
            }
            catch (ArgumentException)
            {
                // Unknown 반환
            }

            return BehaviorJsonFormat.Unknown;
        }

        private static BehaviorTreeAsset ImportBehaviorTreeNode(string absoluteJsonPath)
        {
            var assetPath = PromptForAssetPath(absoluteJsonPath, "BT-node");
            if (string.IsNullOrWhiteSpace(assetPath))
                return null;

            BehaviorTreeAsset tree;
            try
            {
                tree = BehaviorTreeJsonUtility.ImportFromJsonFile(absoluteJsonPath, assetPath);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[BT] BT-node JSON Import 실패: {absoluteJsonPath}\n{exception.Message}");
                return null;
            }

            if (tree == null)
                return null;

            EditorGUIUtility.PingObject(tree);
            BehaviorTreeEditorWindow.Open(tree);
            BehaviorTreeJsonUtility.LogValidation(tree);
            return tree;
        }

        private static BehaviorTreeAsset ImportMonsterBehaviorRules(string absoluteJsonPath)
        {
            // Rules 포맷은 자체적으로 Generated/ 경로를 결정하므로 경로 프롬프트를 띄우지 않는다.
            BehaviorTreeAsset tree;
            try
            {
                tree = MonsterBehaviorTreeJsonImporter.ImportFromMonsterBehaviorJson(absoluteJsonPath, null);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[BT] Rules JSON Import 실패: {absoluteJsonPath}\n{exception.Message}");
                return null;
            }

            if (tree != null)
            {
                EditorGUIUtility.PingObject(tree);
                BehaviorTreeEditorWindow.Open(tree);
                BehaviorTreeJsonUtility.LogValidation(tree);
            }
            return tree;
        }

        private static string PromptForAssetPath(string absoluteJsonPath, string formatLabel)
        {
            var defaultName = Path.GetFileNameWithoutExtension(absoluteJsonPath) + ".asset";
            return EditorUtility.SaveFilePanelInProject(
                $"Behavior Tree Asset 저장 경로 ({formatLabel})",
                defaultName,
                "asset",
                "Import 결과 BehaviorTreeAsset을 저장할 경로를 지정하세요.");
        }
    }
}
#endif
