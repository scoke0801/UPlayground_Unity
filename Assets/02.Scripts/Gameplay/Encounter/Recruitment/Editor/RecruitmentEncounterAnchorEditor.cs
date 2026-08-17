using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Story;
using UPlayGround.FlowGraph;

namespace UPlayGround.Gameplay.Encounter.Editor
{
    /// <summary>영입 조우의 진행 불능 구성을 씬 저작 시점에 검증한다.</summary>
    [CustomEditor(typeof(RecruitmentEncounterAnchor))]
    public sealed class RecruitmentEncounterAnchorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

            var errors = new List<string>();
            ValidateConfiguration(errors);
            if (errors.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "영입 조우 필수 참조와 FlowGraph 노드 구성이 유효합니다.",
                    MessageType.Info);
                return;
            }

            for (int i = 0; i < errors.Count; i++)
                EditorGUILayout.HelpBox(errors[i], MessageType.Error);
        }

        private void ValidateConfiguration(List<string> errors)
        {
            SerializedProperty definitionProperty = serializedObject.FindProperty("_definition");
            SerializedProperty runnerProperty = serializedObject.FindProperty("_flowRunner");
            SerializedProperty resumeEntryProperty = serializedObject.FindProperty("_resumeEntryId");
            SerializedProperty allyProperty = serializedObject.FindProperty("_allyActor");
            SerializedProperty groupProperty = serializedObject.FindProperty("_hostileGroup");
            SerializedProperty participantsProperty = serializedObject.FindProperty("_participants");

            var definition = definitionProperty.objectReferenceValue
                as RecruitmentEncounterDefinitionSO;
            var runner = runnerProperty.objectReferenceValue as FlowGraphRunner;
            var allyActor = allyProperty.objectReferenceValue as MonsterActor;

            if (definition == null)
                errors.Add("RecruitmentEncounterDefinitionSO가 필요합니다.");
            else
            {
                if (string.IsNullOrWhiteSpace(definition.EncounterId))
                    errors.Add("정의의 encounterId가 비어 있습니다.");
                if (definition.RecruitCharacter == CharacterActorType.None)
                    errors.Add("영입할 CharacterActorType이 지정되지 않았습니다.");
                if (definition.AllyFaction == null)
                    errors.Add("임시 아군 CombatFaction이 지정되지 않았습니다.");
            }

            if (runner == null)
                errors.Add("FlowGraphRunner가 필요합니다.");
            if (string.IsNullOrWhiteSpace(resumeEntryProperty.stringValue))
                errors.Add("로드 복원용 Manual Entry ID가 비어 있습니다.");
            if (allyActor == null)
                errors.Add("대화 파트너가 될 필수 아군 MonsterActor가 필요합니다.");
            else
            {
                if (string.IsNullOrWhiteSpace(allyActor.ActorId))
                    errors.Add("필수 아군의 ActorId가 비어 있어 대화 파트너를 찾을 수 없습니다.");
                if (allyActor.RecruitableAs != CharacterActorType.None)
                    errors.Add("필수 아군의 recruitableAs는 None이어야 합니다. 영입은 대화 커밋만 담당합니다.");
            }
            if (groupProperty.objectReferenceValue == null)
                errors.Add("적 전술과 잠복 활성화를 담당할 MonsterGroupController가 필요합니다.");

            ValidateParticipants(participantsProperty, allyActor, errors);
            ValidateFlowGraph(
                runner?.Graph,
                definition?.EncounterId,
                resumeEntryProperty.stringValue,
                errors);
        }

        private static void ValidateParticipants(
            SerializedProperty participants,
            MonsterActor allyActor,
            List<string> errors)
        {
            if (participants == null || !participants.isArray || participants.arraySize == 0)
            {
                errors.Add("조우 참가자가 없습니다.");
                return;
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            int allyCount = 0;
            int hostileCount = 0;
            for (int i = 0; i < participants.arraySize; i++)
            {
                var participant = participants.GetArrayElementAtIndex(i).objectReferenceValue
                    as RecruitmentEncounterParticipant;
                if (participant == null)
                {
                    errors.Add($"참가자 배열 {i}번 참조가 비어 있습니다.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(participant.ParticipantId))
                    errors.Add($"참가자 {participant.name}의 participantId가 비어 있습니다.");
                else if (!ids.Add(participant.ParticipantId))
                    errors.Add($"participantId '{participant.ParticipantId}'가 중복됩니다.");
                if (participant.Actor == null)
                    errors.Add($"참가자 {participant.name}에 MonsterActor가 없습니다.");
                if (participant.gameObject.activeSelf)
                    errors.Add($"참가자 {participant.name}은 씬 시작 전에 AI가 동작하지 않도록 비활성 상태로 저작해야 합니다.");

                if (participant.Role == RecruitmentEncounterRole.RequiredAlly)
                {
                    allyCount++;
                    if (participant.Actor != allyActor)
                        errors.Add("RequiredAlly 참가자와 Anchor의 필수 아군 참조가 다릅니다.");
                }
                else
                {
                    hostileCount++;
                }
            }

            if (allyCount != 1)
                errors.Add($"RequiredAlly 참가자는 정확히 1명이어야 합니다. 현재 {allyCount}명입니다.");
            if (hostileCount == 0)
                errors.Add("Hostile 참가자가 최소 1명 필요합니다.");
        }

        private static void ValidateFlowGraph(
            FlowGraphSO graph,
            string encounterId,
            string resumeEntryId,
            List<string> errors)
        {
            if (graph == null)
            {
                if (errors.TrueForAll(error => !error.StartsWith("FlowGraphRunner", StringComparison.Ordinal)))
                    errors.Add("FlowGraphRunner에 Graph가 지정되지 않았습니다.");
                return;
            }
            if (graph.nodes == null)
            {
                errors.Add("FlowGraph의 노드 목록이 유실됐습니다.");
                return;
            }

            var dialogueNodeIds = new HashSet<string>(StringComparer.Ordinal);
            var commitNodeIds = new HashSet<string>(StringComparer.Ordinal);
            bool hasResume = false;
            bool hasWait = false;
            bool hasPrepare = false;
            bool hasManualResumeEntry = false;

            for (int i = 0; i < graph.nodes.Count; i++)
            {
                FlowNode node = graph.nodes[i];
                switch (node)
                {
                    case ManualEntryNode entry:
                        hasManualResumeEntry |= string.Equals(
                            entry.entryId,
                            resumeEntryId,
                            StringComparison.Ordinal);
                        break;
                    case ResumeRecruitmentEncounterNode resume:
                        hasResume |= MatchesEncounter(resume.encounterId, encounterId);
                        break;
                    case WaitRecruitmentCombatResolvedNode wait:
                        hasWait |= MatchesEncounter(wait.encounterId, encounterId);
                        break;
                    case PrepareRecruitmentDialogueNode prepare:
                        hasPrepare |= MatchesEncounter(prepare.encounterId, encounterId);
                        break;
                    case PlayDialogueRequiredNode dialogue when MatchesEncounter(dialogue.encounterId, encounterId):
                        dialogueNodeIds.Add(dialogue.id);
                        if (dialogue.dialogue == null)
                            errors.Add("Play Dialogue Required 노드의 대화 그래프가 비어 있습니다.");
                        break;
                    case CommitRecruitmentEncounterNode commit when MatchesEncounter(commit.encounterId, encounterId):
                        commitNodeIds.Add(commit.id);
                        break;
                }
            }

            if (!hasManualResumeEntry)
                errors.Add($"로드 복원용 Manual Entry '{resumeEntryId}'가 Graph에 없습니다.");
            if (!hasResume)
                errors.Add("같은 encounterId의 Resume 노드가 없습니다.");
            if (!hasWait)
                errors.Add("같은 encounterId의 Wait Combat Resolved 노드가 없습니다.");
            if (!hasPrepare)
                errors.Add("같은 encounterId의 Prepare Dialogue 노드가 없습니다.");
            if (dialogueNodeIds.Count == 0)
                errors.Add("같은 encounterId의 Play Dialogue Required 노드가 없습니다.");
            if (commitNodeIds.Count == 0)
                errors.Add("같은 encounterId의 Commit 노드가 없습니다.");
            if (dialogueNodeIds.Count > 0
                && commitNodeIds.Count > 0
                && HasCommitPathBypassingDialogue(graph, dialogueNodeIds, commitNodeIds))
            {
                errors.Add("필수 대화를 거치지 않고 Commit에 도달 가능한 경로가 있습니다.");
            }
        }

        private static bool HasCommitPathBypassingDialogue(
            FlowGraphSO graph,
            HashSet<string> dialogueNodeIds,
            HashSet<string> commitNodeIds)
        {
            var pending = new Stack<string>();
            var visited = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < graph.nodes.Count; i++)
            {
                if (graph.nodes[i] is EntryNode entry && entry != null)
                    pending.Push(entry.id);
            }

            while (pending.Count > 0)
            {
                string nodeId = pending.Pop();
                if (!visited.Add(nodeId) || dialogueNodeIds.Contains(nodeId))
                    continue;
                if (commitNodeIds.Contains(nodeId))
                    return true;

                if (graph.connections == null)
                    continue;
                for (int i = 0; i < graph.connections.Count; i++)
                {
                    FlowConnection connection = graph.connections[i];
                    if (connection != null
                        && string.Equals(connection.fromNodeId, nodeId, StringComparison.Ordinal))
                    {
                        pending.Push(connection.toNodeId);
                    }
                }
            }

            return false;
        }

        private static bool MatchesEncounter(string nodeEncounterId, string encounterId) =>
            !string.IsNullOrWhiteSpace(encounterId)
            && string.Equals(nodeEncounterId?.Trim(), encounterId.Trim(), StringComparison.Ordinal);
    }
}
