using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Story;
using UPlayGround.FlowGraph;

namespace UPlayGround.Gameplay.Encounter.Editor
{
    internal enum RecruitmentEncounterIssueSeverity
    {
        Info,
        Warning,
        Error,
    }

    internal sealed class RecruitmentEncounterAuthoringIssue
    {
        public RecruitmentEncounterAuthoringIssue(
            RecruitmentEncounterIssueSeverity severity,
            string message,
            UnityEngine.Object context = null)
        {
            Severity = severity;
            Message = message;
            Context = context;
        }

        public RecruitmentEncounterIssueSeverity Severity { get; }
        public string Message { get; }
        public UnityEngine.Object Context { get; }
    }

    /// <summary>영입 조우의 저장 키, 참가자, 진영, 씬 바인딩과 FlowGraph 진행 경로를 함께 검증한다.</summary>
    internal static class RecruitmentEncounterAuthoringValidator
    {
        public static void ValidateAnchor(
            RecruitmentEncounterAnchor anchor,
            List<RecruitmentEncounterAuthoringIssue> issues,
            bool includeProjectIdScan = false)
        {
            issues.Clear();
            if (anchor == null)
            {
                AddError(issues, "검증할 RecruitmentEncounterAnchor가 없습니다.");
                return;
            }

            var serializedAnchor = new SerializedObject(anchor);
            RecruitmentEncounterDefinitionSO definition = GetReference<RecruitmentEncounterDefinitionSO>(
                serializedAnchor,
                "_definition");
            FlowGraphRunner runner = GetReference<FlowGraphRunner>(serializedAnchor, "_flowRunner");
            FlowGraphTriggerVolume entryVolume = GetReference<FlowGraphTriggerVolume>(
                serializedAnchor,
                "_entryVolume");
            MonsterActor allyActor = GetReference<MonsterActor>(serializedAnchor, "_allyActor");
            UnityEngine.Object hostileGroup = GetReference<UnityEngine.Object>(serializedAnchor, "_hostileGroup");
            SerializedProperty participants = serializedAnchor.FindProperty("_participants");
            string resumeEntryId = serializedAnchor.FindProperty("_resumeEntryId")?.stringValue;

            ValidateDefinition(definition, issues, includeProjectIdScan);
            if (runner == null)
                AddError(issues, "FlowGraphRunner가 필요합니다.", anchor);
            else if (runner.Graph == null)
                AddError(issues, "FlowGraphRunner에 Graph가 지정되지 않았습니다.", runner);

            if (entryVolume == null)
                AddError(issues, "플레이어 지역 진입을 처리할 FlowGraphTriggerVolume이 필요합니다.", anchor);
            if (string.IsNullOrWhiteSpace(resumeEntryId))
                AddError(issues, "로드 복원용 Manual Entry ID가 비어 있습니다.", anchor);
            if (allyActor == null)
                AddError(issues, "대화 파트너가 될 필수 아군 MonsterActor가 필요합니다.", anchor);
            else
                ValidateAllyActor(allyActor, issues);
            if (hostileGroup == null)
                AddError(issues, "적 전술과 잠복 활성화를 담당할 MonsterGroupController가 필요합니다.", anchor);

            ValidateParticipants(participants, allyActor, definition, issues);
            ValidateEntryVolume(entryVolume, runner, issues);
            ValidateFlowGraph(
                runner?.Graph,
                definition?.EncounterId,
                resumeEntryId,
                entryVolume,
                issues);

            if (!HasErrors(issues) && !HasWarnings(issues))
            {
                issues.Add(new RecruitmentEncounterAuthoringIssue(
                    RecruitmentEncounterIssueSeverity.Info,
                    "필수 참조, 참가자, 진영 관계와 FlowGraph 진행 경로가 유효합니다.",
                    anchor));
            }
        }

        public static bool HasErrors(IReadOnlyList<RecruitmentEncounterAuthoringIssue> issues)
        {
            for (int i = 0; i < issues.Count; i++)
            {
                if (issues[i].Severity == RecruitmentEncounterIssueSeverity.Error)
                    return true;
            }

            return false;
        }

        private static bool HasWarnings(IReadOnlyList<RecruitmentEncounterAuthoringIssue> issues)
        {
            for (int i = 0; i < issues.Count; i++)
            {
                if (issues[i].Severity == RecruitmentEncounterIssueSeverity.Warning)
                    return true;
            }

            return false;
        }

        private static void ValidateDefinition(
            RecruitmentEncounterDefinitionSO definition,
            List<RecruitmentEncounterAuthoringIssue> issues,
            bool includeProjectIdScan)
        {
            if (definition == null)
            {
                AddError(issues, "RecruitmentEncounterDefinitionSO가 필요합니다.");
                return;
            }

            if (string.IsNullOrWhiteSpace(definition.EncounterId))
                AddError(issues, "정의의 encounterId가 비어 있습니다.", definition);
            else if (ContainsWhitespace(definition.EncounterId))
                AddError(issues, "encounterId에는 공백을 사용할 수 없습니다.", definition);
            if (definition.RecruitCharacter == CharacterActorType.None)
                AddError(issues, "영입할 CharacterActorType이 지정되지 않았습니다.", definition);
            if (definition.AllyFaction == null)
                AddError(issues, "임시 아군 CombatFaction이 지정되지 않았습니다.", definition);
            else
                ValidatePlayerAllyRelation(definition.AllyFaction, definition, issues);

            if (includeProjectIdScan && !string.IsNullOrWhiteSpace(definition.EncounterId))
                ValidateDefinitionIdUniqueness(definition, issues);
        }

        private static void ValidateDefinitionIdUniqueness(
            RecruitmentEncounterDefinitionSO definition,
            List<RecruitmentEncounterAuthoringIssue> issues)
        {
            string[] guids = AssetDatabase.FindAssets("t:RecruitmentEncounterDefinitionSO");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                RecruitmentEncounterDefinitionSO candidate =
                    AssetDatabase.LoadAssetAtPath<RecruitmentEncounterDefinitionSO>(path);
                if (candidate == null
                    || candidate == definition
                    || !string.Equals(
                        candidate.EncounterId,
                        definition.EncounterId,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                AddError(
                    issues,
                    $"encounterId '{definition.EncounterId}'가 '{path}'와 중복됩니다.",
                    candidate);
            }
        }

        private static void ValidatePlayerAllyRelation(
            CombatFactionSO allyFaction,
            UnityEngine.Object context,
            List<RecruitmentEncounterAuthoringIssue> issues)
        {
            CombatRelation relation = ResolveRelation(
                allyFaction.FactionId,
                CombatFactionRules.PlayerPartyId);
            if (relation != CombatRelation.Ally)
            {
                AddError(
                    issues,
                    $"임시 아군 진영 '{allyFaction.FactionId}'과 PlayerParty 관계가 Ally가 아닙니다.",
                    context);
            }
        }

        private static void ValidateAllyActor(
            MonsterActor allyActor,
            List<RecruitmentEncounterAuthoringIssue> issues)
        {
            if (string.IsNullOrWhiteSpace(allyActor.ActorId))
                AddError(issues, "필수 아군의 ActorId가 비어 있어 대화 파트너를 찾을 수 없습니다.", allyActor);
            if (allyActor.RecruitableAs != CharacterActorType.None)
            {
                AddError(
                    issues,
                    "필수 아군의 recruitableAs는 None이어야 합니다. 영입은 대화 커밋만 담당합니다.",
                    allyActor);
            }
        }

        private static void ValidateParticipants(
            SerializedProperty participants,
            MonsterActor allyActor,
            RecruitmentEncounterDefinitionSO definition,
            List<RecruitmentEncounterAuthoringIssue> issues)
        {
            if (participants == null || !participants.isArray || participants.arraySize == 0)
            {
                AddError(issues, "조우 참가자가 없습니다.");
                return;
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            var actors = new HashSet<MonsterActor>();
            int allyCount = 0;
            int hostileCount = 0;
            for (int i = 0; i < participants.arraySize; i++)
            {
                RecruitmentEncounterParticipant participant = participants
                    .GetArrayElementAtIndex(i)
                    .objectReferenceValue as RecruitmentEncounterParticipant;
                if (participant == null)
                {
                    AddError(issues, $"참가자 배열 {i}번 참조가 비어 있습니다.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(participant.ParticipantId))
                    AddError(issues, $"참가자 '{participant.name}'의 participantId가 비어 있습니다.", participant);
                else if (!ids.Add(participant.ParticipantId))
                    AddError(issues, $"participantId '{participant.ParticipantId}'가 중복됩니다.", participant);

                if (participant.Actor == null)
                {
                    AddError(issues, $"참가자 '{participant.name}'에 MonsterActor가 없습니다.", participant);
                    continue;
                }

                if (!actors.Add(participant.Actor))
                    AddError(issues, $"MonsterActor '{participant.Actor.name}'가 참가자 배열에 중복 등록됐습니다.", participant);
                if (participant.gameObject.activeSelf)
                {
                    AddError(
                        issues,
                        $"참가자 '{participant.name}'은 씬 시작 전에 AI가 동작하지 않도록 비활성 상태로 저작해야 합니다.",
                        participant);
                }

                if (participant.GetComponent<SceneEntityId>() != null)
                {
                    AddWarning(
                        issues,
                        $"참가자 '{participant.name}'에 SceneEntityId가 있습니다. 일반 필드 재스폰/월드 처치 정책과 중복되지 않는지 확인하세요.",
                        participant);
                }

                if (participant.Role == RecruitmentEncounterRole.RequiredAlly)
                {
                    allyCount++;
                    if (participant.Actor != allyActor)
                        AddError(issues, "RequiredAlly 참가자와 Anchor의 필수 아군 참조가 다릅니다.", participant);
                }
                else
                {
                    hostileCount++;
                    ValidateHostileRelation(participant.Actor, definition, participant, issues);
                }
            }

            if (allyCount != 1)
                AddError(issues, $"RequiredAlly 참가자는 정확히 1명이어야 합니다. 현재 {allyCount}명입니다.");
            if (hostileCount == 0)
                AddError(issues, "Hostile 참가자가 최소 1명 필요합니다.");
        }

        private static void ValidateHostileRelation(
            MonsterActor hostile,
            RecruitmentEncounterDefinitionSO definition,
            UnityEngine.Object context,
            List<RecruitmentEncounterAuthoringIssue> issues)
        {
            if (definition?.AllyFaction == null || hostile == null)
                return;

            CombatRelation relation = ResolveRelation(
                definition.AllyFaction.FactionId,
                hostile.CombatFactionId);
            if (relation != CombatRelation.Hostile)
            {
                AddError(
                    issues,
                    $"적 '{hostile.name}'의 진영 '{hostile.CombatFactionId}'과 임시 아군 진영 관계가 Hostile이 아닙니다.",
                    context);
            }
        }

        private static CombatRelation ResolveRelation(string firstFactionId, string secondFactionId)
        {
            CombatFactionRelationTableSO table = Resources.Load<CombatFactionRelationTableSO>(
                "CombatFactionRelations");
            return table != null
                ? table.Resolve(firstFactionId, secondFactionId)
                : CombatFactionRules.ResolveDefaultRelation(firstFactionId, secondFactionId);
        }

        private static void ValidateEntryVolume(
            FlowGraphTriggerVolume entryVolume,
            FlowGraphRunner runner,
            List<RecruitmentEncounterAuthoringIssue> issues)
        {
            if (entryVolume == null)
                return;

            var serializedVolume = new SerializedObject(entryVolume);
            FlowGraphRunner volumeRunner = GetReference<FlowGraphRunner>(serializedVolume, "_runner");
            Collider volumeCollider = GetReference<Collider>(serializedVolume, "_volumeCollider")
                                      ?? entryVolume.GetComponent<Collider>();
            string volumeId = serializedVolume.FindProperty("_volumeId")?.stringValue;

            if (volumeRunner != runner)
                AddError(issues, "진입 볼륨과 Anchor가 서로 다른 FlowGraphRunner를 참조합니다.", entryVolume);
            if (string.IsNullOrWhiteSpace(volumeId))
                AddError(issues, "진입 볼륨의 volumeId가 비어 있습니다.", entryVolume);
            if (volumeCollider == null)
                AddError(issues, "진입 볼륨 Collider가 없습니다.", entryVolume);
            else
            {
                if (!volumeCollider.isTrigger)
                    AddError(issues, "진입 볼륨 Collider는 Is Trigger여야 합니다.", volumeCollider);
                if (volumeCollider is not BoxCollider && volumeCollider is not SphereCollider)
                {
                    AddWarning(
                        issues,
                        "위치 기반 진입 판정은 BoxCollider 또는 SphereCollider를 권장합니다.",
                        volumeCollider);
                }
            }
        }

        private static void ValidateFlowGraph(
            FlowGraphSO graph,
            string encounterId,
            string resumeEntryId,
            FlowGraphTriggerVolume entryVolume,
            List<RecruitmentEncounterAuthoringIssue> issues)
        {
            if (graph == null)
                return;

            var graphErrors = new List<string>();
            if (!graph.Validate(graphErrors))
            {
                for (int i = 0; i < graphErrors.Count; i++)
                    AddError(issues, graphErrors[i], graph);
            }

            var dialogueNodeIds = new HashSet<string>(StringComparer.Ordinal);
            var commitNodeIds = new HashSet<string>(StringComparer.Ordinal);
            var entryNodeIds = new HashSet<string>(StringComparer.Ordinal);
            bool hasResume = false;
            bool hasWait = false;
            bool hasPrepare = false;
            bool hasManualResumeEntry = false;
            bool hasMatchingVolumeEntry = entryVolume == null;
            string volumeId = GetVolumeId(entryVolume);

            for (int i = 0; i < graph.nodes.Count; i++)
            {
                FlowNode node = graph.nodes[i];
                if (node == null)
                    continue;

                if (node is EntryNode)
                    entryNodeIds.Add(node.id);

                switch (node)
                {
                    case ManualEntryNode entry:
                        hasManualResumeEntry |= string.Equals(
                            entry.entryId,
                            resumeEntryId,
                            StringComparison.Ordinal);
                        break;
                    case OnTriggerVolumeEntryNode volumeEntry:
                        hasMatchingVolumeEntry |= volumeEntry.phase == FlowVolumePhase.Enter
                                                  && string.Equals(
                                                      volumeEntry.volumeId,
                                                      volumeId,
                                                      StringComparison.Ordinal);
                        break;
                    case ResumeRecruitmentEncounterNode resume:
                        hasResume |= MatchesEncounter(resume.encounterId, encounterId);
                        ValidateNodeEncounterId(resume.encounterId, encounterId, resume.DisplayName, graph, issues);
                        break;
                    case WaitRecruitmentCombatResolvedNode wait:
                        hasWait |= MatchesEncounter(wait.encounterId, encounterId);
                        ValidateNodeEncounterId(wait.encounterId, encounterId, wait.DisplayName, graph, issues);
                        break;
                    case PrepareRecruitmentDialogueNode prepare:
                        hasPrepare |= MatchesEncounter(prepare.encounterId, encounterId);
                        ValidateNodeEncounterId(prepare.encounterId, encounterId, prepare.DisplayName, graph, issues);
                        break;
                    case PlayDialogueRequiredNode dialogue:
                        ValidateNodeEncounterId(dialogue.encounterId, encounterId, dialogue.DisplayName, graph, issues);
                        if (MatchesEncounter(dialogue.encounterId, encounterId))
                        {
                            dialogueNodeIds.Add(dialogue.id);
                            if (dialogue.dialogue == null)
                                AddError(issues, "Play Dialogue Required 노드의 대화 그래프가 비어 있습니다.", graph);
                        }
                        break;
                    case CommitRecruitmentEncounterNode commit:
                        ValidateNodeEncounterId(commit.encounterId, encounterId, commit.DisplayName, graph, issues);
                        if (MatchesEncounter(commit.encounterId, encounterId))
                            commitNodeIds.Add(commit.id);
                        break;
                }
            }

            if (!hasManualResumeEntry)
                AddError(issues, $"로드 복원용 Manual Entry '{resumeEntryId}'가 Graph에 없습니다.", graph);
            if (!hasMatchingVolumeEntry)
                AddError(issues, $"진입 볼륨 ID '{volumeId}'의 Enter 노드가 Graph에 없습니다.", graph);
            if (!hasResume)
                AddError(issues, "같은 encounterId의 Resume 노드가 없습니다.", graph);
            if (!hasWait)
                AddError(issues, "같은 encounterId의 Wait Combat Resolved 노드가 없습니다.", graph);
            if (!hasPrepare)
                AddError(issues, "같은 encounterId의 Prepare Dialogue 노드가 없습니다.", graph);
            if (dialogueNodeIds.Count == 0)
                AddError(issues, "같은 encounterId의 Play Dialogue Required 노드가 없습니다.", graph);
            if (commitNodeIds.Count == 0)
                AddError(issues, "같은 encounterId의 Commit 노드가 없습니다.", graph);

            if (dialogueNodeIds.Count > 0
                && commitNodeIds.Count > 0
                && HasPathBypassingStops(graph, entryNodeIds, commitNodeIds, dialogueNodeIds))
            {
                AddError(issues, "필수 대화를 거치지 않고 Commit에 도달 가능한 경로가 있습니다.", graph);
            }

            if (dialogueNodeIds.Count > 0
                && commitNodeIds.Count > 0
                && !HasPath(graph, dialogueNodeIds, commitNodeIds))
            {
                AddError(issues, "필수 대화 완료 뒤 Commit으로 이어지는 경로가 없습니다.", graph);
            }
        }

        private static void ValidateNodeEncounterId(
            string nodeEncounterId,
            string encounterId,
            string nodeName,
            UnityEngine.Object context,
            List<RecruitmentEncounterAuthoringIssue> issues)
        {
            if (MatchesEncounter(nodeEncounterId, encounterId))
                return;

            AddError(
                issues,
                $"{nodeName}의 encounterId '{nodeEncounterId}'가 정의 ID '{encounterId}'와 다릅니다.",
                context);
        }

        private static bool HasPathBypassingStops(
            FlowGraphSO graph,
            HashSet<string> starts,
            HashSet<string> targets,
            HashSet<string> stops)
        {
            var pending = new Stack<string>();
            var visited = new HashSet<string>(StringComparer.Ordinal);
            foreach (string start in starts)
                pending.Push(start);

            while (pending.Count > 0)
            {
                string nodeId = pending.Pop();
                if (!visited.Add(nodeId) || stops.Contains(nodeId))
                    continue;
                if (targets.Contains(nodeId))
                    return true;

                PushTargets(graph, nodeId, pending);
            }

            return false;
        }

        private static bool HasPath(
            FlowGraphSO graph,
            HashSet<string> starts,
            HashSet<string> targets)
        {
            var pending = new Stack<string>();
            var visited = new HashSet<string>(StringComparer.Ordinal);
            foreach (string start in starts)
                pending.Push(start);

            while (pending.Count > 0)
            {
                string nodeId = pending.Pop();
                if (!visited.Add(nodeId))
                    continue;
                if (targets.Contains(nodeId) && !starts.Contains(nodeId))
                    return true;

                PushTargets(graph, nodeId, pending);
            }

            return false;
        }

        private static void PushTargets(FlowGraphSO graph, string sourceNodeId, Stack<string> pending)
        {
            for (int i = 0; i < graph.connections.Count; i++)
            {
                FlowConnection connection = graph.connections[i];
                if (connection != null
                    && string.Equals(connection.fromNodeId, sourceNodeId, StringComparison.Ordinal))
                {
                    pending.Push(connection.toNodeId);
                }
            }
        }

        private static string GetVolumeId(FlowGraphTriggerVolume volume)
        {
            if (volume == null)
                return null;

            var serializedVolume = new SerializedObject(volume);
            return serializedVolume.FindProperty("_volumeId")?.stringValue;
        }

        private static T GetReference<T>(SerializedObject serializedObject, string propertyName)
            where T : UnityEngine.Object
        {
            return serializedObject.FindProperty(propertyName)?.objectReferenceValue as T;
        }

        private static bool MatchesEncounter(string nodeEncounterId, string encounterId) =>
            !string.IsNullOrWhiteSpace(encounterId)
            && string.Equals(
                nodeEncounterId?.Trim(),
                encounterId.Trim(),
                StringComparison.Ordinal);

        private static bool ContainsWhitespace(string value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                if (char.IsWhiteSpace(value[i]))
                    return true;
            }

            return false;
        }

        private static void AddError(
            List<RecruitmentEncounterAuthoringIssue> issues,
            string message,
            UnityEngine.Object context = null) =>
            issues.Add(new RecruitmentEncounterAuthoringIssue(
                RecruitmentEncounterIssueSeverity.Error,
                message,
                context));

        private static void AddWarning(
            List<RecruitmentEncounterAuthoringIssue> issues,
            string message,
            UnityEngine.Object context = null) =>
            issues.Add(new RecruitmentEncounterAuthoringIssue(
                RecruitmentEncounterIssueSeverity.Warning,
                message,
                context));
    }
}
