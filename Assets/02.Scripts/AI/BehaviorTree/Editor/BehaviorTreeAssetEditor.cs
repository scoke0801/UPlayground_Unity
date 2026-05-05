#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.AI.BehaviorTree.Editor
{
    [CustomEditor(typeof(BehaviorTreeAsset))]
    public class BehaviorTreeAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            if (GUILayout.Button("Behavior Tree Editor 열기"))
                BehaviorTreeEditorWindow.Open(target as BehaviorTreeAsset);
        }
    }

    public static class BehaviorTreeTestDataGenerator
    {
        private const string EnemyGroundBasicPath = "Assets/10.Datas/AI/BehaviorTree/BT_EnemyGroundBasic_Test.asset";
        private const string TestPrefabPath = "Assets/03.Prefabs/AI/BehaviorTree/PF_BT_EnemyGroundBasic_TestRunner.prefab";

        [MenuItem("UPlayGround/Character/AI/Behavior Tree/Generate Enemy Ground Basic Test")]
        public static void GenerateEnemyGroundBasic()
        {
            var tree = AssetDatabase.LoadAssetAtPath<BehaviorTreeAsset>(EnemyGroundBasicPath);
            if (tree == null)
            {
                Debug.LogError($"Behavior Tree 테스트 에셋을 찾을 수 없습니다: {EnemyGroundBasicPath}");
                return;
            }

            var removedNullNodes = CleanNullReferences(tree);
            var addedSubAssets = SyncNodeListWithSubAssets(tree);
            ApplyEnemyGroundBasicAbortSettings(tree);
            var prefab = CreateOrUpdateTestRunnerPrefab(tree);

            EditorUtility.SetDirty(tree);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"BT_EnemyGroundBasic_Test 정리 완료: null node {removedNullNodes}개 제거, subasset node {addedSubAssets}개 동기화, prefab={(prefab != null ? TestPrefabPath : "생성 실패")}");
        }

        public static int CleanNullReferences(BehaviorTreeAsset tree)
        {
            if (tree == null)
                return 0;

            var removed = tree.Nodes.RemoveAll(node => node == null);
            foreach (var node in tree.Nodes)
            {
                if (node == null)
                    continue;

                node.EnsureGuid();
                node.Children.RemoveAll(child => child == null);
                EditorUtility.SetDirty(node);
            }

            return removed;
        }

        private static int SyncNodeListWithSubAssets(BehaviorTreeAsset tree)
        {
            var path = AssetDatabase.GetAssetPath(tree);
            var nodes = AssetDatabase.LoadAllAssetsAtPath(path).OfType<BTNode>().ToList();
            var added = 0;

            foreach (var node in nodes)
            {
                if (tree.Nodes.Contains(node))
                    continue;

                tree.Nodes.Add(node);
                added++;
            }

            return added;
        }

        private static void ApplyEnemyGroundBasicAbortSettings(BehaviorTreeAsset tree)
        {
            foreach (var node in tree.Nodes)
            {
                switch (node)
                {
                    case SelectorNode selector:
                        selector.AbortType = selector.DisplayName == "Root_EnemyGroundBasic" || selector.DisplayName == "Combat_Decision"
                            ? BTAbortType.LowerPriority
                            : BTAbortType.None;
                        EditorUtility.SetDirty(selector);
                        break;
                    case SequenceNode sequence:
                        sequence.AbortType = sequence.DisplayName.StartsWith("Branch_") || sequence.DisplayName.StartsWith("Combat_") || sequence.DisplayName.StartsWith("NonCombat_")
                            ? BTAbortType.Self
                            : BTAbortType.None;
                        EditorUtility.SetDirty(sequence);
                        break;
                }
            }
        }

        private static GameObject CreateOrUpdateTestRunnerPrefab(BehaviorTreeAsset tree)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(TestPrefabPath));

            var source = AssetDatabase.LoadAssetAtPath<GameObject>(TestPrefabPath);
            var instance = source != null
                ? PrefabUtility.InstantiatePrefab(source) as GameObject
                : new GameObject("PF_BT_EnemyGroundBasic_TestRunner");

            if (instance == null)
                return null;

            var runner = instance.GetComponent<BehaviorTreeRunner>();
            if (runner == null)
                runner = instance.AddComponent<BehaviorTreeRunner>();

            var serializedRunner = new SerializedObject(runner);
            serializedRunner.FindProperty("_treeAsset").objectReferenceValue = tree;
            serializedRunner.FindProperty("_startOnEnable").boolValue = false;
            serializedRunner.FindProperty("_tickMode").enumValueIndex = (int)BehaviorTreeRunnerMode.Manual;
            serializedRunner.FindProperty("_debugMode").boolValue = true;
            serializedRunner.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(instance, TestPrefabPath);
            UnityEngine.Object.DestroyImmediate(instance);
            return prefab;
        }
    }
}
#endif
