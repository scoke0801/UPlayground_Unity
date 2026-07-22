using UnityEditor;
using UnityEngine;
using UPlayGround.FlowGraph;
using UPlayGround.TriggerSystem;

namespace UPlayGround.EditorTools
{
    /// <summary>
    /// 씬의 TriggerComposer(Source+Condition+Action 1:1:1)를 Entry→Condition→Action 선형
    /// FlowGraph 에셋으로 변환하는 선택적 승격 도구. 기존 Condition/Action SO 에셋은
    /// EvaluateTriggerCondition/RunTriggerAction 범용 노드로 그대로 재활용한다(자산 재생성 없음).
    /// 원본 TriggerComposer는 삭제하지 않는다 — 병존 원칙.
    /// </summary>
    public static class FlowGraphComposerConverter
    {
        private const string MenuPath = "GameObject/UPlayGround/TriggerComposer → FlowGraph 변환";

        [MenuItem(MenuPath, true)]
        private static bool Validate()
        {
            return Selection.activeGameObject != null
                && Selection.activeGameObject.GetComponent<TriggerComposer>() != null;
        }

        [MenuItem(MenuPath, false, 30)]
        private static void Convert()
        {
            var composer = Selection.activeGameObject.GetComponent<TriggerComposer>();
            var serialized = new SerializedObject(composer);

            string triggerId = serialized.FindProperty("_triggerId").stringValue;
            var source = serialized.FindProperty("_source").objectReferenceValue as TriggerSourceSO;
            var condition = serialized.FindProperty("_condition").objectReferenceValue as TriggerConditionSO;
            var action = serialized.FindProperty("_action").objectReferenceValue as TriggerActionSO;
            var repeat = (TriggerRepeatPolicy)serialized.FindProperty("_repeat").enumValueIndex;
            float cooldown = serialized.FindProperty("_cooldownSeconds").floatValue;

            string defaultName = $"FLOW_{Sanitize(string.IsNullOrEmpty(triggerId) ? composer.name : triggerId)}";
            string path = EditorUtility.SaveFilePanelInProject(
                "FlowGraph 변환 저장", defaultName, "asset",
                "변환된 FlowGraph 에셋 저장 위치를 선택하세요.");
            if (string.IsNullOrEmpty(path))
                return;

            var graph = ScriptableObject.CreateInstance<FlowGraphSO>();
            graph.graphId = string.IsNullOrEmpty(triggerId) ? composer.name : triggerId;

            // 1) 진입점 — 소스 타입별 매핑 (TriggerRepeatPolicy와 FlowRepeatPolicy는 값 호환)
            EntryNode entry = CreateEntry(source, graph.graphId);
            entry.repeatPolicy = (FlowRepeatPolicy)(int)repeat;
            entry.cooldownSeconds = cooldown;
            entry.editorPosition = new Vector2(0, 100);
            graph.nodes.Add(entry);
            string lastNodeId = entry.id;
            string lastPort = FlowPort.Out;

            // 2) 조건 — 기존 SO 재활용
            if (condition != null)
            {
                var conditionNode = new EvaluateTriggerConditionNode
                {
                    condition = condition,
                    editorPosition = new Vector2(280, 100),
                };
                graph.nodes.Add(conditionNode);
                graph.connections.Add(NewConnection(lastNodeId, lastPort, conditionNode.id));
                lastNodeId = conditionNode.id;
                lastPort = FlowPort.True;
            }

            // 3) 액션 — 기존 SO 재활용
            if (action != null)
            {
                var actionNode = new RunTriggerActionNode
                {
                    action = action,
                    editorPosition = new Vector2(condition != null ? 560 : 280, 100),
                };
                graph.nodes.Add(actionNode);
                graph.connections.Add(NewConnection(lastNodeId, lastPort, actionNode.id));
            }

            AssetDatabase.CreateAsset(graph, path);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(graph);

            string volumeGuide = entry is OnTriggerVolumeEntryNode
                ? $" 콜라이더 소스이므로 씬에 FlowGraphRunner + FlowGraphTriggerVolume(volumeId=\"{graph.graphId}\")를 배치해야 발화된다."
                : " 씬에 FlowGraphRunner를 배치하고 그래프를 연결할 것.";
            Debug.Log($"[FlowGraph] TriggerComposer '{composer.name}' → '{path}' 변환 완료." +
                      $" 원본 Composer는 유지된다(병존 원칙, 중복 발화 방지를 위해 한쪽만 활성화).{volumeGuide}", graph);
        }

        private static EntryNode CreateEntry(TriggerSourceSO source, string graphId)
        {
            return source switch
            {
                ColliderEnterTriggerSourceSO => new OnTriggerVolumeEntryNode
                {
                    volumeId = graphId,
                    phase = FlowVolumePhase.Enter,
                },
                ColliderExitTriggerSourceSO => new OnTriggerVolumeEntryNode
                {
                    volumeId = graphId,
                    phase = FlowVolumePhase.Exit,
                },
                // GroupDefeated 등 나머지 소스는 Manual 진입점으로 두고 발화 배선은 저작자가 결정
                _ => new ManualEntryNode { entryId = graphId },
            };
        }

        private static FlowConnection NewConnection(string fromId, string fromPort, string toId)
        {
            return new FlowConnection
            {
                fromNodeId = fromId,
                fromPort = fromPort,
                toNodeId = toId,
                toPort = FlowPort.In,
            };
        }

        private static string Sanitize(string value)
        {
            foreach (char c in System.IO.Path.GetInvalidFileNameChars())
                value = value.Replace(c, '_');
            return value.Replace('/', '_');
        }
    }
}
