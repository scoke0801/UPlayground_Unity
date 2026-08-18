using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UPlayGround.Dialogue;

namespace UPlayGround.Dialogue.Editor
{
    /// <summary>
    /// 대화 그래프 전체를 훑어 초상화가 해석되지 않는 화자를 찾아낸다.
    /// 노드 오버라이드도, SpeakerPortraitTable 등록도 없는 화자는 인게임에서 초상화가 통째로 사라지므로
    /// 데이터 저작 단계에서 잡아야 한다.
    /// </summary>
    public static class DialoguePortraitValidator
    {
        private const string PortraitTableFilter = "t:SpeakerPortraitTableSO";
        private const string GraphFilter = "t:DialogueGraphSO";

        [MenuItem("Tools/UPlayGround/대화/초상화 미해석 화자 검사")]
        public static void Validate()
        {
            SpeakerPortraitTableSO table = LoadPortraitTable();
            if (table == null)
            {
                Debug.LogError("[DialoguePortraitValidator] SpeakerPortraitTable 에셋을 찾지 못했습니다.");
                return;
            }

            // 화자별로 "테이블·노드 어디에서도 초상화를 못 찾은 대사" 수를 센다.
            var unresolved = new Dictionary<string, int>(StringComparer.Ordinal);
            var sampleGraph = new Dictionary<string, string>(StringComparer.Ordinal);
            int checkedLines = 0;

            foreach (string guid in AssetDatabase.FindAssets(GraphFilter))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var graph = AssetDatabase.LoadAssetAtPath<DialogueGraphSO>(path);
                if (graph == null) continue;

                foreach (var node in graph.nodes)
                {
                    if (node == null || node.nodeType != NodeType.Talk) continue;
                    if (string.IsNullOrEmpty(node.speakerId)) continue;

                    // 플레이어·주인공 화자는 파티 데이터에서 해석되므로 이 검사 대상이 아니다.
                    if (DialogueSpeakerResolver.IsActivePlayerSpeaker(node.speakerId)
                        || DialogueSpeakerResolver.IsProtagonistSpeaker(node.speakerId))
                        continue;

                    checkedLines++;
                    if (node.portrait != null) continue;
                    if (table.GetPortrait(node.speakerId) != null) continue;

                    unresolved.TryGetValue(node.speakerId, out int count);
                    unresolved[node.speakerId] = count + 1;
                    if (!sampleGraph.ContainsKey(node.speakerId))
                        sampleGraph[node.speakerId] = path;
                }
            }

            if (unresolved.Count == 0)
            {
                Debug.Log($"[DialoguePortraitValidator] NPC 대사 {checkedLines}줄 모두 초상화가 해석됩니다.");
                return;
            }

            var report = new StringBuilder();
            report.AppendLine(
                $"[DialoguePortraitValidator] 초상화가 해석되지 않는 화자 {unresolved.Count}명 (NPC 대사 {checkedLines}줄 검사)");
            foreach (var pair in unresolved.OrderByDescending(p => p.Value))
                report.AppendLine($"  · {pair.Key} — {pair.Value}줄 (예: {sampleGraph[pair.Key]})");
            report.Append("SpeakerPortraitTable에 등록하거나 노드의 portrait를 지정하세요.");

            Debug.LogWarning(report.ToString());
        }

        private static SpeakerPortraitTableSO LoadPortraitTable()
        {
            string[] guids = AssetDatabase.FindAssets(PortraitTableFilter);
            if (guids.Length == 0) return null;

            if (guids.Length > 1)
                Debug.LogWarning("[DialoguePortraitValidator] SpeakerPortraitTable이 여러 개입니다. 첫 번째를 사용합니다.");

            return AssetDatabase.LoadAssetAtPath<SpeakerPortraitTableSO>(
                AssetDatabase.GUIDToAssetPath(guids[0]));
        }
    }
}
