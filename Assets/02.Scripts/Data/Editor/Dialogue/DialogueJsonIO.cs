using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UPlayGround.Dialogue;

namespace UPlayGround.Dialogue.Editor
{
    /// <summary>
    /// DialogueGraphSO ↔ JSON 변환 유틸리티.
    ///
    /// [Export] SO → JSON
    ///   - 에셋 참조(portrait, condition, eventActions, cameraRecording)는 AssetDatabase 경로 문자열로 저장
    ///
    /// [Import] JSON → SO
    ///   - 기존 그래프 SO에 노드를 덮어씀 (기존 노드 SO는 모두 삭제 후 재생성)
    ///   - 에셋 참조는 AssetDatabase.LoadAssetAtPath 로 복원
    ///
    /// DTO는 DialogueNodeSO의 저작 필드를 전부 담는다. 필드를 빠뜨리면 Import가 노드를 재생성하면서
    /// 누락 필드를 기본값으로 되돌려 조용히 데이터를 파괴하므로, 노드에 필드를 추가할 때 여기도 함께 넓힌다.
    /// </summary>
    public static class DialogueJsonIO
    {
        // ── JSON 직렬화용 DTO ─────────────────────────────────────────────

        [Serializable]
        private class GraphDto
        {
            public string graphId;
            public string graphName;
            public string startNodeId;
            public List<string> silentParticipantSpeakerIds = new();
            public List<NodeDto> nodes = new();
        }

        [Serializable]
        private class NodeDto
        {
            public string   nodeName;            // 노드 SO 에셋 이름
            public string   nodeId;
            public string   nodeType;
            public string   channel;
            public string   speakerId;
            public string   dialogueText;
            public string   portraitAssetPath;   // AssetDatabase 경로 or ""
            public float    typingSpeed;
            public float    autoAdvanceDuration;
            public string   nextNodeId;
            public string   trueNextNodeId;
            public string   falseNextNodeId;
            public List<ChoiceDto> choices = new();
            public string   conditionAssetPath;  // AssetDatabase 경로 or ""
            public List<string> eventActionAssetPaths = new(); // AssetDatabase 경로 목록

            // 연출 · 모션
            public string speakerMotionId;
            public string speakerMotionCategory;
            public string listenerMotionId;
            public string listenerMotionCategory;

            // 연출 · 카메라
            public string cameraRecordingAssetPath;
            public string shotType;
            public string shotTransition;
            public string listenerSpeakerId;
            public string reactionSpeakerId;
            public float  shotDistanceOverride;

            // 연출 · 포커스 컷어웨이
            public string focusSpeakerId;
            public float  focusHoldSeconds;
            public float  focusDelaySeconds;
            public string focusShotType;

            public float    editorX;
            public float    editorY;
        }

        [Serializable]
        private class ChoiceDto
        {
            public string choiceText;
            public string nextNodeId;
            public string displayConditionAssetPath;
            public bool   isGreyedOut;
        }

        // ── Export ────────────────────────────────────────────────────────

        /// <summary>그래프 SO를 JSON 파일로 내보냅니다.</summary>
        public static void ExportToJson(DialogueGraphSO graph)
        {
            string savePath = EditorUtility.SaveFilePanel(
                "Export Dialogue Graph", "Assets", graph.name, "json");
            if (string.IsNullOrEmpty(savePath)) return;

            File.WriteAllText(savePath, ToJson(graph));
            Debug.Log($"[DialogueJsonIO] Exported → {savePath}");
        }

        /// <summary>그래프 SO를 JSON 문자열로 직렬화한다. 파일 저장 없이 일괄 추출·비교에 쓴다.</summary>
        public static string ToJson(DialogueGraphSO graph)
        {
            if (graph == null) throw new ArgumentNullException(nameof(graph));
            return JsonUtility.ToJson(BuildDto(graph), prettyPrint: true);
        }

        private static GraphDto BuildDto(DialogueGraphSO graph)
        {
            var dto = new GraphDto
            {
                graphId     = graph.graphId,
                graphName   = graph.graphName,
                startNodeId = graph.startNodeId,
                silentParticipantSpeakerIds =
                    new List<string>(graph.silentParticipantSpeakerIds ?? new List<string>()),
            };

            foreach (var node in graph.nodes)
            {
                if (node == null) continue;

                var n = new NodeDto
                {
                    nodeName     = node.name,
                    nodeId       = node.nodeId,
                    nodeType     = node.nodeType.ToString(),
                    channel      = node.channel.ToString(),
                    speakerId    = node.speakerId,
                    dialogueText = node.dialogueText,
                    typingSpeed  = node.typingSpeed,
                    autoAdvanceDuration = node.autoAdvanceDuration,
                    nextNodeId   = node.nextNodeId,
                    trueNextNodeId  = node.trueNextNodeId,
                    falseNextNodeId = node.falseNextNodeId,
                    conditionAssetPath  = AssetPath(node.condition),
                    portraitAssetPath   = AssetPath(node.portrait),

                    speakerMotionId        = node.speakerMotionId,
                    speakerMotionCategory  = node.speakerMotionCategory.ToString(),
                    listenerMotionId       = node.listenerMotionId,
                    listenerMotionCategory = node.listenerMotionCategory.ToString(),

                    cameraRecordingAssetPath = AssetPath(node.cameraRecording),
                    shotType             = node.shotType.ToString(),
                    shotTransition       = node.shotTransition.ToString(),
                    listenerSpeakerId    = node.listenerSpeakerId,
                    reactionSpeakerId    = node.reactionSpeakerId,
                    shotDistanceOverride = node.shotDistanceOverride,

                    focusSpeakerId    = node.focusSpeakerId,
                    focusHoldSeconds  = node.focusHoldSeconds,
                    focusDelaySeconds = node.focusDelaySeconds,
                    focusShotType     = node.focusShotType.ToString(),

                    editorX = node.editorPosition.x,
                    editorY = node.editorPosition.y,
                };

                foreach (var c in node.choices)
                    n.choices.Add(new ChoiceDto
                    {
                        choiceText                = c.choiceText,
                        nextNodeId                = c.nextNodeId,
                        displayConditionAssetPath = AssetPath(c.displayCondition),
                        isGreyedOut               = c.isGreyedOut,
                    });

                foreach (var a in node.eventActions)
                    n.eventActionAssetPaths.Add(AssetPath(a));

                dto.nodes.Add(n);
            }
            return dto;
        }

        // ── Import ────────────────────────────────────────────────────────

        /// <summary>
        /// JSON 파일을 읽어 기존 그래프 SO를 덮어씁니다.
        /// graph가 null이면 새 SO를 저장할 경로를 묻습니다.
        /// </summary>
        public static void ImportFromJson(DialogueGraphSO targetGraph = null)
        {
            string loadPath = EditorUtility.OpenFilePanel("Import Dialogue JSON", "Assets", "json");
            if (string.IsNullOrEmpty(loadPath)) return;

            string json = File.ReadAllText(loadPath);
            var dto = JsonUtility.FromJson<GraphDto>(json);
            if (dto == null)
            {
                Debug.LogError("[DialogueJsonIO] JSON 파싱 실패 — 형식을 확인해주세요.");
                return;
            }

            // 대상 그래프 결정: 인자로 받거나 새로 생성
            if (targetGraph == null)
            {
                string savePath = EditorUtility.SaveFilePanelInProject(
                    "Save Graph SO", dto.graphName ?? "DLG_New", "asset", "저장할 위치를 선택하세요");
                if (string.IsNullOrEmpty(savePath)) return;

                targetGraph = ScriptableObject.CreateInstance<DialogueGraphSO>();
                AssetDatabase.CreateAsset(targetGraph, savePath);
            }

            ApplyDto(dto, targetGraph);
            AssetDatabase.SaveAssets();
            Debug.Log($"[DialogueJsonIO] Imported → {AssetDatabase.GetAssetPath(targetGraph)}");
        }

        private static void ApplyDto(GraphDto dto, DialogueGraphSO graph)
        {
            Undo.RecordObject(graph, "Import Dialogue JSON");

            // 기존 노드 SO 제거
            foreach (var old in graph.nodes)
            {
                if (old != null)
                    Undo.DestroyObjectImmediate(old);
            }

            graph.graphId     = dto.graphId;
            graph.graphName   = dto.graphName;
            graph.startNodeId = dto.startNodeId;
            graph.silentParticipantSpeakerIds =
                new List<string>(dto.silentParticipantSpeakerIds ?? new List<string>());
            graph.nodes.Clear();
            graph.InvalidateCache();

            string graphAssetPath = AssetDatabase.GetAssetPath(graph);

            foreach (var n in dto.nodes)
            {
                var node = ScriptableObject.CreateInstance<DialogueNodeSO>();
                node.nodeType       = ParseEnum(n.nodeType, NodeType.Talk);
                node.name           = string.IsNullOrEmpty(n.nodeName) ? $"Node_{node.nodeType}" : n.nodeName;
                node.nodeId         = n.nodeId;
                node.channel        = ParseEnum(n.channel, DialogueChannel.Main);
                node.speakerId      = n.speakerId;
                node.dialogueText   = n.dialogueText;
                node.typingSpeed    = n.typingSpeed;
                node.autoAdvanceDuration = n.autoAdvanceDuration;
                node.nextNodeId     = n.nextNodeId;
                node.trueNextNodeId  = n.trueNextNodeId;
                node.falseNextNodeId = n.falseNextNodeId;
                node.editorPosition  = new Vector2(n.editorX, n.editorY);

                node.speakerMotionId        = n.speakerMotionId;
                node.speakerMotionCategory  = ParseEnum(n.speakerMotionCategory, DialogueMotionCategory.Neutral);
                node.listenerMotionId       = n.listenerMotionId;
                node.listenerMotionCategory = ParseEnum(n.listenerMotionCategory, DialogueMotionCategory.Neutral);

                node.cameraRecording      = LoadAsset<UPlayGround.Data.DialogueCameraRecordingSO>(n.cameraRecordingAssetPath);
                node.shotType             = ParseEnum(n.shotType, UPlayGround.Data.DialogueShotType.Auto);
                node.shotTransition       = ParseEnum(n.shotTransition, UPlayGround.Data.DialogueShotTransition.Auto);
                node.listenerSpeakerId    = n.listenerSpeakerId;
                node.reactionSpeakerId    = n.reactionSpeakerId;
                node.shotDistanceOverride = n.shotDistanceOverride;

                node.focusSpeakerId    = n.focusSpeakerId;
                node.focusHoldSeconds  = n.focusHoldSeconds;
                node.focusDelaySeconds = n.focusDelaySeconds;
                node.focusShotType     = ParseEnum(n.focusShotType, UPlayGround.Data.DialogueShotType.Auto);

                node.portrait  = LoadAsset<Sprite>(n.portraitAssetPath);
                node.condition = LoadAsset<ConditionSO>(n.conditionAssetPath);

                node.choices.Clear();
                foreach (var c in n.choices)
                    node.choices.Add(new ChoiceData
                    {
                        choiceText       = c.choiceText,
                        nextNodeId       = c.nextNodeId,
                        displayCondition = LoadAsset<ConditionSO>(c.displayConditionAssetPath),
                        isGreyedOut      = c.isGreyedOut,
                    });

                node.eventActions.Clear();
                foreach (var path in n.eventActionAssetPaths)
                {
                    var action = LoadAsset<DialogueActionSO>(path);
                    if (action != null) node.eventActions.Add(action);
                }

                Undo.RegisterCreatedObjectUndo(node, "Import Node");
                AssetDatabase.AddObjectToAsset(node, graphAssetPath);
                graph.nodes.Add(node);
            }

            EditorUtility.SetDirty(graph);
        }

        // ── 헬퍼 ─────────────────────────────────────────────────────────

        // UnityEngine.Object → "Assets/..." 경로. null이면 ""
        private static string AssetPath(UnityEngine.Object obj)
            => obj != null ? AssetDatabase.GetAssetPath(obj) : string.Empty;

        // "Assets/..." 경로 → T 에셋. 경로가 비거나 로드 실패 시 null
        private static T LoadAsset<T>(string path) where T : UnityEngine.Object
            => string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<T>(path);

        // 값이 비었거나 알 수 없는 이름이면 기본값. 구버전 JSON을 읽어도 노드가 깨지지 않도록 한다.
        private static T ParseEnum<T>(string value, T fallback) where T : struct, Enum
            => Enum.TryParse(value, out T parsed) ? parsed : fallback;
    }
}
