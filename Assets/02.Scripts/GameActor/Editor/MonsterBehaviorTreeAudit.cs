#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Enemy;

namespace UPlayGround.AI.BehaviorTree.Editor
{
    /// <summary>BehaviorData 연결과 실제 액터 이동 방식을 대조하는 읽기 전용 감사.</summary>
    public static class MonsterBehaviorTreeAudit
    {
        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/비헤이비어 트리/검증/몬스터 BT 연결 감사")]
        public static void Run()
        {
            Debug.Log(BuildReport());
        }

        /// <summary>에셋 수정 없이 연결표·이상 항목·미연결 트리를 반환한다.</summary>
        public static string BuildReport()
        {
            var report = new StringBuilder("# 몬스터 BT 연결 감사\n\n| BehaviorData | BT | GUID | 판정 |\n|---|---|---|---|\n");
            var treeKinds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var file in Directory.GetFiles("Assets/10.Datas/AI/BehaviorTree/SourceJson", "*.json", SearchOption.AllDirectories))
            {
                var data = JsonUtility.FromJson<MonsterBehaviorTreeJson>(File.ReadAllText(file));
                treeKinds["BT_" + data.id] = data.actorKind;
            }
            var ground = new HashSet<EnemyBehaviorSO>();
            var flying = new HashSet<EnemyBehaviorSO>();
            var usedTrees = new HashSet<ScriptableObject>();
            var flyingConnections = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:ActorDefinitionSO", new[] { "Assets/10.Datas" }))
            {
                var definition = AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>(AssetDatabase.GUIDToAssetPath(guid));
                var prefab = definition.prefab;
                if (prefab == null || definition.EffectiveBehaviorData == null) continue;
                if (prefab.GetComponentInChildren<EnemyAIController>(true) != null)
                    ground.Add(definition.EffectiveBehaviorData);
                var controller = prefab.GetComponentInChildren<EnemyFlyingAIController>(true);
                if (controller == null) continue;
                flying.Add(definition.EffectiveBehaviorData);
                var property = new SerializedObject(controller).FindProperty("_behaviorTree");
                if (property?.objectReferenceValue is ScriptableObject tree)
                {
                    usedTrees.Add(tree);
                    string finding = treeKinds.TryGetValue(tree.name, out var kind)
                        ? string.Equals(kind, "Flying", StringComparison.OrdinalIgnoreCase) ? "정상" : "경고: 비행 액터가 Ground BT 사용"
                        : "정보: Rules 원본 매칭 없음";
                    flyingConnections.Add($"- {AssetDatabase.GetAssetPath(definition)} → {AssetDatabase.GetAssetPath(tree)}: {finding}");
                }
                else
                    flyingConnections.Add($"- {AssetDatabase.GetAssetPath(definition)}: 경고: 비행 컨트롤러 BT null/깨진 참조");
            }
            foreach (var guid in AssetDatabase.FindAssets("t:EnemyBehaviorSO", new[] { "Assets/10.Datas" }).OrderBy(AssetDatabase.GUIDToAssetPath))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var behavior = AssetDatabase.LoadAssetAtPath<EnemyBehaviorSO>(path);
                var property = new SerializedObject(behavior).FindProperty("behaviorTree");
                var tree = behavior.behaviorTree;
                string treePath = tree != null ? AssetDatabase.GetAssetPath(tree) : "";
                var findings = new List<string>();
                bool unused = flying.Contains(behavior) && !ground.Contains(behavior);
                if (unused) findings.Add("정보: 비행 컨트롤러의 미사용 칸");
                if (tree == null || string.IsNullOrEmpty(treePath))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(File.ReadAllText(path), @"behaviorTree:.*guid: ([a-f0-9]{32})");
                    bool broken = property.objectReferenceInstanceIDValue != 0 || match.Success;
                    findings.Add(unused ? "미사용 칸의 빈/깨진 연결" : broken ? "경고: 깨진 참조" : "경고: BT null");
                }
                else
                {
                    if (!unused) usedTrees.Add(tree);
                    if (treePath.IndexOf("Test", StringComparison.OrdinalIgnoreCase) >= 0)
                        findings.Add(unused ? "미사용 칸의 Test BT" : "경고: Test BT 연결");
                    if (!(tree is BehaviorTreeAsset)) findings.Add("경고: BehaviorTreeAsset 타입 아님");
                    if (treeKinds.TryGetValue(tree.name, out var kind)
                        && ground.Contains(behavior) && string.Equals(kind, "Flying", StringComparison.OrdinalIgnoreCase))
                        findings.Add("경고: 지상 액터가 Flying BT 사용");
                    if (!treeKinds.ContainsKey(tree.name)) findings.Add("정보: Rules 원본 매칭 없음(이동 방식 수동 대조)");
                }
                if (!ground.Contains(behavior) && !flying.Contains(behavior)) findings.Add("정보: ActorDefinition 사용 미확인");
                report.AppendLine($"| {path} | {treePath} | {AssetDatabase.AssetPathToGUID(treePath)} | {string.Join("; ", findings)} |");
            }
            report.AppendLine("\n## 실제 비행 컨트롤러 연결\n");
            foreach (string connection in flyingConnections) report.AppendLine(connection);
            report.AppendLine("\n## 현재 로드 씬의 그룹 비소속 지상 전투 액터\n");
            foreach (var controller in UnityEngine.Object.FindObjectsByType<EnemyAIController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!controller.gameObject.scene.IsValid() || controller.GetComponentInParent<UPlayGround.Group.MonsterGroupController>(true) != null) continue;
                report.AppendLine($"- 경고: {controller.gameObject.scene.path}/{controller.name}: 그룹 없이 배치됨(슬롯 제한 없음, 런타임 그룹 주입 여부 별도 확인)");
            }
            report.AppendLine("\n## 실제 소비 연결이 확인되지 않은 BT (삭제 대상 아님)\n");
            foreach (var guid in AssetDatabase.FindAssets("t:BehaviorTreeAsset", new[] { "Assets/10.Datas/AI/BehaviorTree" }).OrderBy(AssetDatabase.GUIDToAssetPath))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!usedTrees.Contains(AssetDatabase.LoadAssetAtPath<BehaviorTreeAsset>(path)))
                    report.AppendLine($"- {path} ({guid})");
            }
            return report.ToString();
        }
    }
}
#endif
