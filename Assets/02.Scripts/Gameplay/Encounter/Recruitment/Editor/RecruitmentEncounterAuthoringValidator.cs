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
        // 캡슐 접지 오차와 지형 표면의 미세 요철을 허용한다.
        internal const float GroundPlacementTolerance = 0.15f;

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
            bool stagesParticipantsBeforeEntry =
                serializedAnchor.FindProperty("_stageParticipantsBeforeEntry")?.boolValue == true;
            bool placesAllyAtDialogueAnchor =
                serializedAnchor.FindProperty("_placeAllyAtDialogueAnchor")?.boolValue == true;

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
                AddError(issues, "대화 파트너이자 영입 대상이 될 MonsterActor가 필요합니다.", anchor);
            else
                ValidateAllyActor(allyActor, issues);
            if (hostileGroup == null)
                AddError(issues, "적 전술과 잠복 활성화를 담당할 MonsterGroupController가 필요합니다.", anchor);
            if (!stagesParticipantsBeforeEntry)
            {
                AddWarning(
                    issues,
                    "진입 전 참가자 대치 노출이 꺼져 있습니다. 참가자 위치가 카메라에 보이면 전투 시작 순간 갑자기 나타날 수 있습니다.",
                    anchor);
            }
            if (placesAllyAtDialogueAnchor)
            {
                AddWarning(
                    issues,
                    "대화 앵커 강제 배치는 전투 종료 위치에서 순간이동을 만들 수 있습니다. 고정 카메라 컷으로 이동을 가리는 조우에서만 사용하세요.",
                    anchor);
            }

            ValidateParticipants(participants, allyActor, definition, issues);
            ValidateEntryVolume(entryVolume, runner, issues);
            ValidateFlowGraph(
                runner?.Graph,
                definition,
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
            if (!Enum.IsDefined(
                    typeof(RecruitmentIncapacitationRule),
                    definition.IncapacitationRule))
            {
                AddError(issues, "정의의 영입 대상 제압 조건이 유효하지 않습니다.", definition);
            }
            if (string.Equals(
                    definition.EncounterId,
                    definition.PrerequisiteEncounterId,
                    StringComparison.Ordinal))
            {
                AddError(issues, "선행 조우 ID는 현재 encounterId와 같을 수 없습니다.", definition);
            }
            if (definition.CombatMode == RecruitmentEncounterCombatMode.CooperativeBattle)
            {
                if (definition.AllyFaction == null)
                    AddError(issues, "임시 아군 CombatFaction이 지정되지 않았습니다.", definition);
                else
                    ValidatePlayerAllyRelation(definition.AllyFaction, definition, issues);
            }

            if (includeProjectIdScan && !string.IsNullOrWhiteSpace(definition.EncounterId))
            {
                ValidateDefinitionIdUniqueness(definition, issues);
                ValidatePrerequisite(definition, issues);
            }
        }

        private static void ValidatePrerequisite(
            RecruitmentEncounterDefinitionSO definition,
            List<RecruitmentEncounterAuthoringIssue> issues)
        {
            if (string.IsNullOrWhiteSpace(definition.PrerequisiteEncounterId))
                return;

            string[] guids = AssetDatabase.FindAssets("t:RecruitmentEncounterDefinitionSO");
            for (int i = 0; i < guids.Length; i++)
            {
                RecruitmentEncounterDefinitionSO candidate =
                    AssetDatabase.LoadAssetAtPath<RecruitmentEncounterDefinitionSO>(
                        AssetDatabase.GUIDToAssetPath(guids[i]));
                if (candidate != null
                    && string.Equals(
                        candidate.EncounterId,
                        definition.PrerequisiteEncounterId,
                        StringComparison.Ordinal))
                {
                    return;
                }
            }

            AddError(
                issues,
                $"선행 조우 ID '{definition.PrerequisiteEncounterId}'에 해당하는 정의 에셋이 없습니다.",
                definition);
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
                AddError(issues, "영입 대상의 ActorId가 비어 있어 대화 파트너를 찾을 수 없습니다.", allyActor);
            if (allyActor.RecruitableAs != CharacterActorType.None)
            {
                AddError(
                    issues,
                    "영입 대상의 recruitableAs는 None이어야 합니다. 영입은 조우 커밋만 담당합니다.",
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
            int recruitTargetCount = 0;
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

                ValidateGroundPlacement(participant, issues);

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
                else if (participant.Role == RecruitmentEncounterRole.RecruitTarget)
                {
                    recruitTargetCount++;
                    if (participant.Actor != allyActor)
                        AddError(issues, "RecruitTarget 참가자와 Anchor의 영입 대상 참조가 다릅니다.", participant);
                    if (definition?.IncapacitationRule
                            == RecruitmentIncapacitationRule.FinishAttack
                        && !SupportsFinishAttackIncapacitation(participant.Actor))
                    {
                        AddError(
                            issues,
                            "피니시 공격 제압 대상은 활성화된 BreakGauge 데이터가 필요합니다.",
                            participant);
                    }
                    ValidatePlayerHostileRelation(participant.Actor, participant, issues);
                }
                else
                {
                    hostileCount++;
                    if (definition?.CombatMode
                        == RecruitmentEncounterCombatMode.HostileRecruitTarget)
                    {
                        ValidatePlayerHostileRelation(participant.Actor, participant, issues);
                    }
                    else
                    {
                        ValidateHostileRelation(participant.Actor, definition, participant, issues);
                    }
                }
            }

            if (definition?.CombatMode == RecruitmentEncounterCombatMode.HostileRecruitTarget)
            {
                if (recruitTargetCount != 1 || allyCount != 0)
                {
                    AddError(
                        issues,
                        $"적대 결투형은 RecruitTarget 1명과 RequiredAlly 0명이 필요합니다. 현재 RecruitTarget {recruitTargetCount}, RequiredAlly {allyCount}명입니다.");
                }
            }
            else
            {
                if (allyCount != 1 || recruitTargetCount != 0)
                {
                    AddError(
                        issues,
                        $"공동 전투형은 RequiredAlly 1명과 RecruitTarget 0명이 필요합니다. 현재 RequiredAlly {allyCount}, RecruitTarget {recruitTargetCount}명입니다.");
                }
                if (hostileCount == 0)
                    AddError(issues, "공동 전투형은 Hostile 참가자가 최소 1명 필요합니다.");
            }
        }

        /// <summary>
        /// 참가자가 지면에 닿아 있는지 확인한다.
        /// 조우 프리팹은 루트만 지면에 맞추고 참가자를 로컬 y=0으로 두므로,
        /// 경사지에 놓으면 참가자가 지면 아래에 묻힌 채 등장한다 — 런타임 보정이 있어도 저작 시점에 잡는 편이 낫다.
        /// </summary>
        private static void ValidateGroundPlacement(
            RecruitmentEncounterParticipant participant,
            List<RecruitmentEncounterAuthoringIssue> issues)
        {
            if (!TryMeasureGroundOffset(participant.transform, out float offset))
            {
                AddWarning(
                    issues,
                    $"참가자 '{participant.name}' 아래에서 지면을 찾지 못했습니다. 배치 위치가 지형 밖이거나 너무 깊이 묻혀 있는지 확인하세요.",
                    participant);
                return;
            }

            if (Mathf.Abs(offset) <= GroundPlacementTolerance)
                return;

            string direction = offset > 0f ? "떠 있습니다" : "묻혀 있습니다";
            AddWarning(
                issues,
                $"참가자 '{participant.name}'이 지면에서 {Mathf.Abs(offset):0.00}m {direction}. "
                + "인스펙터의 '참가자를 지면에 맞추기'로 배치 높이를 정리하세요.",
                participant);
        }

        /// <summary>
        /// 배치 높이와 지면 높이의 차이를 잰다. 양수면 공중, 음수면 지면 아래다.
        /// 대상이 활성 상태면 자기 콜라이더가 지면으로 잡히므로 대상 하위는 탐지에서 제외한다.
        /// </summary>
        internal static bool TryMeasureGroundOffset(Transform target, out float offset)
        {
            offset = 0f;
            if (target == null)
                return false;

            Vector3 position = target.position;
            if (!ActorStagePlacement.TryProbeGroundIgnoringHeight(position, target, out Vector3 grounded))
                return false;

            offset = position.y - grounded.y;
            return true;
        }

        private static bool SupportsFinishAttackIncapacitation(MonsterActor actor)
        {
            if (actor == null)
                return false;
            if (actor.BreakGauge != null && actor.BreakGauge.UseBreakGauge)
                return true;

            return actor.Definition?.EffectiveBreakGaugeData is { useBreakGauge: true };
        }

        private static void ValidatePlayerHostileRelation(
            MonsterActor hostile,
            UnityEngine.Object context,
            List<RecruitmentEncounterAuthoringIssue> issues)
        {
            if (hostile == null)
                return;

            CombatRelation relation = ResolveRelation(
                hostile.CombatFactionId,
                CombatFactionRules.PlayerPartyId);
            if (relation != CombatRelation.Hostile)
            {
                AddError(
                    issues,
                    $"적대 참가자 '{hostile.name}'의 진영 '{hostile.CombatFactionId}'과 PlayerParty 관계가 Hostile이 아닙니다.",
                    context);
            }
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
            RecruitmentEncounterDefinitionSO definition,
            string resumeEntryId,
            FlowGraphTriggerVolume entryVolume,
            List<RecruitmentEncounterAuthoringIssue> issues)
        {
            if (graph == null)
                return;

            string encounterId = definition?.EncounterId;
            RecruitmentEncounterCombatMode combatMode = definition != null
                ? definition.CombatMode
                : RecruitmentEncounterCombatMode.CooperativeBattle;

            var graphErrors = new List<string>();
            if (!graph.Validate(graphErrors))
            {
                for (int i = 0; i < graphErrors.Count; i++)
                    AddError(issues, graphErrors[i], graph);
            }

            var introductionDialogueNodeIds = new HashSet<string>(StringComparer.Ordinal);
            var recruitmentDialogueNodeIds = new HashSet<string>(StringComparer.Ordinal);
            var startCombatNodeIds = new HashSet<string>(StringComparer.Ordinal);
            var commitNodeIds = new HashSet<string>(StringComparer.Ordinal);
            var victoryCommitNodeIds = new HashSet<string>(StringComparer.Ordinal);
            var postDialogueNodeIds = new HashSet<string>(StringComparer.Ordinal);
            var finalizeNodeIds = new HashSet<string>(StringComparer.Ordinal);
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
                            if (dialogue.stage
                                == RecruitmentRequiredDialogueStage.CombatIntroduction)
                            {
                                introductionDialogueNodeIds.Add(dialogue.id);
                            }
                            else
                            {
                                recruitmentDialogueNodeIds.Add(dialogue.id);
                            }
                            if (dialogue.dialogue == null)
                                AddError(issues, "Play Dialogue Required 노드의 대화 그래프가 비어 있습니다.", graph);
                        }
                        break;
                    case StartRecruitmentCombatNode startCombat:
                        ValidateNodeEncounterId(
                            startCombat.encounterId,
                            encounterId,
                            startCombat.DisplayName,
                            graph,
                            issues);
                        if (MatchesEncounter(startCombat.encounterId, encounterId))
                            startCombatNodeIds.Add(startCombat.id);
                        break;
                    case CommitRecruitmentEncounterNode commit:
                        ValidateNodeEncounterId(commit.encounterId, encounterId, commit.DisplayName, graph, issues);
                        if (MatchesEncounter(commit.encounterId, encounterId))
                            commitNodeIds.Add(commit.id);
                        break;
                    case CommitRecruitmentAfterVictoryNode victoryCommit:
                        ValidateNodeEncounterId(
                            victoryCommit.encounterId,
                            encounterId,
                            victoryCommit.DisplayName,
                            graph,
                            issues);
                        if (MatchesEncounter(victoryCommit.encounterId, encounterId))
                            victoryCommitNodeIds.Add(victoryCommit.id);
                        break;
                    case PlayRecruitmentPostDialogueNode postDialogue:
                        ValidateNodeEncounterId(
                            postDialogue.encounterId,
                            encounterId,
                            postDialogue.DisplayName,
                            graph,
                            issues);
                        if (MatchesEncounter(postDialogue.encounterId, encounterId))
                        {
                            postDialogueNodeIds.Add(postDialogue.id);
                            if (postDialogue.dialogue == null)
                                AddError(issues, "Play Post Dialogue 노드의 대화 그래프가 비어 있습니다.", graph);
                        }
                        break;
                    case FinalizeRecruitmentEncounterNode finalize:
                        ValidateNodeEncounterId(
                            finalize.encounterId,
                            encounterId,
                            finalize.DisplayName,
                            graph,
                            issues);
                        if (MatchesEncounter(finalize.encounterId, encounterId))
                            finalizeNodeIds.Add(finalize.id);
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
            if (finalizeNodeIds.Count == 0)
                AddError(issues, "같은 encounterId의 Finalize 노드가 없습니다.", graph);

            HashSet<string> effectiveCommitNodeIds;
            if (combatMode == RecruitmentEncounterCombatMode.HostileRecruitTarget)
            {
                effectiveCommitNodeIds = victoryCommitNodeIds;
                if (introductionDialogueNodeIds.Count == 0)
                    AddError(issues, "적대 결투형에는 전투 전 필수 대화 노드가 필요합니다.", graph);
                if (startCombatNodeIds.Count == 0)
                    AddError(issues, "적대 결투형에는 Start Combat 노드가 필요합니다.", graph);
                if (victoryCommitNodeIds.Count == 0)
                    AddError(issues, "적대 결투형에는 Commit After Victory 노드가 필요합니다.", graph);

                if (introductionDialogueNodeIds.Count > 0
                    && startCombatNodeIds.Count > 0
                    && !HasPath(graph, introductionDialogueNodeIds, startCombatNodeIds))
                {
                    AddError(issues, "전투 전 필수 대화 완료 뒤 Start Combat으로 이어지는 경로가 없습니다.", graph);
                }

                if (introductionDialogueNodeIds.Count > 0
                    && startCombatNodeIds.Count > 0
                    && HasPathBypassingStops(
                        graph,
                        entryNodeIds,
                        startCombatNodeIds,
                        introductionDialogueNodeIds))
                {
                    AddError(issues, "전투 전 필수 대화를 거치지 않고 Start Combat에 도달 가능한 경로가 있습니다.", graph);
                }

                if (startCombatNodeIds.Count > 0
                    && victoryCommitNodeIds.Count > 0
                    && !HasPath(graph, startCombatNodeIds, victoryCommitNodeIds))
                {
                    AddError(issues, "Start Combat 뒤 전투 완료와 승리 커밋으로 이어지는 경로가 없습니다.", graph);
                }
            }
            else
            {
                effectiveCommitNodeIds = commitNodeIds;
                if (recruitmentDialogueNodeIds.Count == 0)
                    AddError(issues, "공동 전투형에는 영입 확정 필수 대화 노드가 필요합니다.", graph);
                if (commitNodeIds.Count == 0)
                    AddError(issues, "공동 전투형에는 Commit 노드가 필요합니다.", graph);

                if (recruitmentDialogueNodeIds.Count > 0
                    && commitNodeIds.Count > 0
                    && HasPathBypassingStops(
                        graph,
                        entryNodeIds,
                        commitNodeIds,
                        recruitmentDialogueNodeIds))
                {
                    AddError(issues, "필수 대화를 거치지 않고 Commit에 도달 가능한 경로가 있습니다.", graph);
                }

                if (recruitmentDialogueNodeIds.Count > 0
                    && commitNodeIds.Count > 0
                    && !HasPath(graph, recruitmentDialogueNodeIds, commitNodeIds))
                {
                    AddError(issues, "필수 대화 완료 뒤 Commit으로 이어지는 경로가 없습니다.", graph);
                }
            }

            if (effectiveCommitNodeIds.Count > 0
                && finalizeNodeIds.Count > 0
                && !HasPath(graph, effectiveCommitNodeIds, finalizeNodeIds))
            {
                AddError(issues, "파티 해금 뒤 조우 완료로 이어지는 경로가 없습니다.", graph);
            }

            if (postDialogueNodeIds.Count > 0
                && finalizeNodeIds.Count > 0
                && HasPathBypassingStops(
                    graph,
                    effectiveCommitNodeIds,
                    finalizeNodeIds,
                    postDialogueNodeIds))
            {
                AddError(issues, "획득 후 대화를 거치지 않고 Finalize에 도달 가능한 경로가 있습니다.", graph);
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
