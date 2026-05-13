#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace UPlayGround.AI.BehaviorTree.Editor
{
    /// <summary>
    /// 대형 그래프 환경에서 BT 에디터/저장 비용을 측정하기 위한 합성 그래프 생성기.
    /// 메뉴 명령으로 100/200/500 노드 그래프를 만들고 저장 시간을 즉시 콘솔에 출력한다.
    /// 측정 후 결과는 한국어 로그로 남으며, 측정용 Asset은 Assets/10.Datas/Perf/ 경로에 떨어진다.
    /// </summary>
    public static class BehaviorTreePerformanceProbe
    {
        private const string OutputFolder = "Assets/10.Datas/Perf";

        [MenuItem("UPlayGround/Character/AI/Behavior Tree Perf/100 Nodes")] private static void Generate100() => Generate(100);
        [MenuItem("UPlayGround/Character/AI/Behavior Tree Perf/200 Nodes")] private static void Generate200() => Generate(200);
        [MenuItem("UPlayGround/Character/AI/Behavior Tree Perf/500 Nodes")] private static void Generate500() => Generate(500);

        private static void Generate(int nodeCount)
        {
            EnsureFolder();

            var totalSw = Stopwatch.StartNew();
            var asset = ScriptableObject.CreateInstance<BehaviorTreeAsset>();
            var assetPath = $"{OutputFolder}/BT_Perf_{nodeCount}_{DateTime.Now:HHmmss}.asset";
            AssetDatabase.CreateAsset(asset, assetPath);

            var createSw = Stopwatch.StartNew();
            var root = ScriptableObject.CreateInstance<SequenceNode>();
            root.name = "Root";
            root.DisplayName = "Root";
            root.EnsureGuid();
            AssetDatabase.AddObjectToAsset(root, asset);
            asset.RootNode = root;
            asset.Nodes.Add(root);

            // 균형 잡힌 깊이를 만들기 위해 N분기 트리 구성
            const int branchFactor = 4;
            var queue = new System.Collections.Generic.Queue<BTNode>();
            queue.Enqueue(root);
            var remaining = nodeCount - 1;
            var positionIndex = 1;

            while (remaining > 0 && queue.Count > 0)
            {
                var parent = queue.Dequeue();
                for (var i = 0; i < branchFactor && remaining > 0; i++)
                {
                    BTNode child;
                    if (remaining > branchFactor * 2)
                    {
                        var composite = ScriptableObject.CreateInstance<SequenceNode>();
                        composite.name = $"Seq_{positionIndex}";
                        composite.DisplayName = composite.name;
                        queue.Enqueue(composite);
                        child = composite;
                    }
                    else
                    {
                        var leaf = ScriptableObject.CreateInstance<WaitNode>();
                        leaf.name = $"Wait_{positionIndex}";
                        leaf.DisplayName = leaf.name;
                        child = leaf;
                    }

                    child.EnsureGuid();
                    child.EditorPosition = new Vector2((positionIndex % 20) * 200f, (positionIndex / 20) * 160f);
                    AssetDatabase.AddObjectToAsset(child, asset);
                    asset.Nodes.Add(child);
                    parent.Children.Add(child);
                    positionIndex++;
                    remaining--;
                }
            }

            createSw.Stop();

            var saveSw = Stopwatch.StartNew();
            EditorUtility.SetDirty(asset);
            foreach (var node in asset.Nodes)
            {
                if (node != null)
                    EditorUtility.SetDirty(node);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            saveSw.Stop();
            totalSw.Stop();

            Debug.Log(
                $"[BT Perf] 합성 그래프 생성 완료 — 노드 {nodeCount}개\n" +
                $"  Asset 경로: {assetPath}\n" +
                $"  노드 생성: {createSw.Elapsed.TotalMilliseconds:F1} ms\n" +
                $"  AssetDatabase.SaveAssets: {saveSw.Elapsed.TotalMilliseconds:F1} ms\n" +
                $"  총 소요: {totalSw.Elapsed.TotalMilliseconds:F1} ms");

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        private static void EnsureFolder()
        {
            if (AssetDatabase.IsValidFolder(OutputFolder))
                return;

            if (!AssetDatabase.IsValidFolder("Assets/10.Datas"))
                AssetDatabase.CreateFolder("Assets", "10.Datas");
            AssetDatabase.CreateFolder("Assets/10.Datas", "Perf");
        }
    }
}
#endif
