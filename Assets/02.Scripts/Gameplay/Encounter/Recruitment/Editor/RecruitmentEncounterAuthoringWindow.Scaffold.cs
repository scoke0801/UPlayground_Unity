using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Story;
using UPlayGround.FlowGraph;
using UPlayGround.Group;

namespace UPlayGround.Gameplay.Encounter.Editor
{
    public sealed partial class RecruitmentEncounterAuthoringWindow
    {
        private void CreateMissingAssets()
        {
            SuggestStableIds();
            List<string> errors = CollectAssetErrors();
            if (!TryContinue(errors))
                return;

            var createdAssetPaths = new List<string>();
            try
            {
                CreateMissingAssetsCore(createdAssetPaths);
                AssetDatabase.SaveAssets();
                SetStatus("누락된 조우 정의와 표준 FlowGraph 에셋을 생성했습니다.", MessageType.Info);
            }
            catch (Exception exception)
            {
                DeleteCreatedAssets(createdAssetPaths);
                Debug.LogException(exception);
                SetStatus($"데이터 에셋 생성에 실패해 신규 에셋을 정리했습니다: {exception.Message}", MessageType.Error);
            }
        }

        private void BuildSceneBinding()
        {
            SuggestStableIds();
            List<string> errors = CollectDraftErrors(requireAssets: true);
            if (!TryContinue(errors))
                return;

            int undoGroup = BeginUndoGroup("영입 조우 씬 바인딩");
            try
            {
                BuildSceneBindingCore();
                Undo.CollapseUndoOperations(undoGroup);
                RefreshValidation(includeProjectIdScan: true);
                SetStatus("씬 바인딩과 참가자 구성을 적용했습니다. 검증 결과를 확인하세요.", MessageType.Info);
            }
            catch (Exception exception)
            {
                Undo.RevertAllDownToGroup(undoGroup);
                Debug.LogException(exception);
                SetStatus($"씬 바인딩 적용에 실패해 Undo 그룹을 롤백했습니다: {exception.Message}", MessageType.Error);
            }
        }

        private void CreateCompleteScaffold()
        {
            SuggestStableIds();
            List<string> errors = CollectDraftErrors(requireAssets: false);
            AddRange(errors, CollectAssetErrors());
            RemoveAssetPresenceErrors(errors);
            if (!TryContinue(errors))
                return;

            int undoGroup = BeginUndoGroup("영입 조우 전체 스캐폴드");
            var createdAssetPaths = new List<string>();
            try
            {
                CreateMissingAssetsCore(createdAssetPaths);
                BuildSceneBindingCore();
                AssetDatabase.SaveAssets();
                Undo.CollapseUndoOperations(undoGroup);
                RefreshValidation(includeProjectIdScan: true);
                SetStatus("조우 정의, 표준 FlowGraph와 씬 바인딩을 한 번에 생성했습니다.", MessageType.Info);
            }
            catch (Exception exception)
            {
                Undo.RevertAllDownToGroup(undoGroup);
                DeleteCreatedAssets(createdAssetPaths);
                Debug.LogException(exception);
                SetStatus($"전체 생성에 실패해 씬 변경과 신규 에셋을 롤백했습니다: {exception.Message}", MessageType.Error);
            }
        }

        private void CreateMissingAssetsCore(List<string> createdAssetPaths)
        {
            if (_definition == null)
            {
                EnsureAssetFolder(_storyFolder);
                _definition = CreateDefinitionAsset(createdAssetPaths);
                LoadDefinitionDraft(_definition);
            }

            if (_flowGraph == null)
            {
                EnsureAssetFolder(_flowFolder);
                _flowGraph = CreateFlowGraphAsset(createdAssetPaths);
            }
        }

        private RecruitmentEncounterDefinitionSO CreateDefinitionAsset(List<string> createdAssetPaths)
        {
            string assetName = $"RecruitmentEncounter_{SanitizeAssetName(_encounterId)}.asset";
            string path = AssetDatabase.GenerateUniqueAssetPath($"{_storyFolder}/{assetName}");
            var definition = CreateInstance<RecruitmentEncounterDefinitionSO>();
            definition.name = Path.GetFileNameWithoutExtension(path);

            var serializedDefinition = new SerializedObject(definition);
            serializedDefinition.FindProperty("_encounterId").stringValue = _encounterId.Trim();
            serializedDefinition.FindProperty("_prerequisiteEncounterId").stringValue =
                _prerequisiteEncounterId?.Trim() ?? string.Empty;
            serializedDefinition.FindProperty("_recruitCharacter").enumValueIndex = (int)_recruitCharacter;
            serializedDefinition.FindProperty("_allyFaction").objectReferenceValue = _allyFaction;
            serializedDefinition.FindProperty("_allyFailurePolicy").enumValueIndex = (int)_allyFailurePolicy;
            serializedDefinition.FindProperty("_resetScope").enumValueIndex = (int)_resetScope;
            serializedDefinition.FindProperty("_postCombatSettleSeconds").floatValue =
                Mathf.Max(0f, _postCombatSettleSeconds);
            serializedDefinition.FindProperty("_dialogueApproachDistance").floatValue =
                Mathf.Max(0f, _dialogueApproachDistance);
            serializedDefinition.FindProperty("_dialogueApproachSpeedMultiplier").floatValue =
                Mathf.Max(0.1f, _dialogueApproachSpeedMultiplier);
            serializedDefinition.FindProperty("_dialogueApproachTimeoutSeconds").floatValue =
                Mathf.Max(0.1f, _dialogueApproachTimeoutSeconds);
            serializedDefinition.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(definition, path);
            createdAssetPaths.Add(path);
            return definition;
        }

        private FlowGraphSO CreateFlowGraphAsset(List<string> createdAssetPaths)
        {
            string stableSuffix = SanitizeAssetName(_definition.EncounterId);
            string path = AssetDatabase.GenerateUniqueAssetPath(
                $"{_flowFolder}/FLOW_Recruitment_{stableSuffix}.asset");
            var graph = CreateInstance<FlowGraphSO>();
            graph.name = Path.GetFileNameWithoutExtension(path);
            graph.graphId = graph.name;
            BuildStandardFlowGraph(
                graph,
                _definition.EncounterId,
                _entryVolumeId,
                _resumeEntryId,
                _dialogue,
                _postRecruitmentDialogue);

            AssetDatabase.CreateAsset(graph, path);
            createdAssetPaths.Add(path);
            EditorUtility.SetDirty(graph);
            return graph;
        }

        private static void BuildStandardFlowGraph(
            FlowGraphSO graph,
            string encounterId,
            string volumeId,
            string resumeEntryId,
            UPlayGround.Dialogue.DialogueGraphSO dialogue,
            UPlayGround.Dialogue.DialogueGraphSO postRecruitmentDialogue)
        {
            var volumeEntry = new OnTriggerVolumeEntryNode
            {
                entryId = "Enter",
                repeatPolicy = FlowRepeatPolicy.Always,
                volumeId = volumeId,
                phase = FlowVolumePhase.Enter,
                editorPosition = new Vector2(20f, 20f),
                editorComment = "지역 진입 신호. 중복 실행은 조우 서비스 lease가 차단한다.",
            };
            var resumeEntry = new ManualEntryNode
            {
                entryId = resumeEntryId,
                repeatPolicy = FlowRepeatPolicy.Always,
                editorPosition = new Vector2(20f, 190f),
                editorComment = "저장된 CombatActive/CombatResolved 단계 복원 진입점.",
            };
            var resume = new ResumeRecruitmentEncounterNode
            {
                encounterId = encounterId,
                editorPosition = new Vector2(280f, 90f),
            };
            var wait = new WaitRecruitmentCombatResolvedNode
            {
                encounterId = encounterId,
                editorPosition = new Vector2(550f, 20f),
            };
            var prepare = new PrepareRecruitmentDialogueNode
            {
                encounterId = encounterId,
                editorPosition = new Vector2(800f, 90f),
            };
            var playDialogue = new PlayDialogueRequiredNode
            {
                encounterId = encounterId,
                dialogue = dialogue,
                editorPosition = new Vector2(1060f, 90f),
            };
            var commit = new CommitRecruitmentEncounterNode
            {
                encounterId = encounterId,
                editorPosition = new Vector2(1330f, 90f),
            };
            var postDialogue = postRecruitmentDialogue != null
                ? new PlayRecruitmentPostDialogueNode
                {
                    encounterId = encounterId,
                    dialogue = postRecruitmentDialogue,
                    editorPosition = new Vector2(1590f, 90f),
                }
                : null;
            var finalize = new FinalizeRecruitmentEncounterNode
            {
                encounterId = encounterId,
                editorPosition = new Vector2(postDialogue != null ? 1850f : 1590f, 90f),
            };

            graph.nodes = new List<FlowNode>
            {
                volumeEntry,
                resumeEntry,
                resume,
                wait,
                prepare,
                playDialogue,
                commit,
                finalize,
            };
            graph.connections = new List<FlowConnection>
            {
                Connect(volumeEntry, FlowPort.Out, resume, FlowPort.In),
                Connect(resumeEntry, FlowPort.Out, resume, FlowPort.In),
                Connect(resume, ResumeRecruitmentEncounterNode.CombatPort, wait, FlowPort.In),
                Connect(wait, WaitRecruitmentCombatResolvedNode.ResolvedPort, prepare, FlowPort.In),
                Connect(resume, ResumeRecruitmentEncounterNode.DialoguePort, prepare, FlowPort.In),
                Connect(prepare, PrepareRecruitmentDialogueNode.ReadyPort, playDialogue, FlowPort.In),
                Connect(playDialogue, PlayDialogueRequiredNode.CompletedPort, commit, FlowPort.In),
            };
            if (postDialogue != null)
            {
                graph.nodes.Insert(graph.nodes.Count - 1, postDialogue);
                graph.connections.Add(Connect(
                    commit,
                    CommitRecruitmentEncounterNode.CompletedPort,
                    postDialogue,
                    FlowPort.In));
                graph.connections.Add(Connect(
                    resume,
                    ResumeRecruitmentEncounterNode.PostDialoguePort,
                    postDialogue,
                    FlowPort.In));
                graph.connections.Add(Connect(
                    postDialogue,
                    PlayRecruitmentPostDialogueNode.CompletedPort,
                    finalize,
                    FlowPort.In));
            }
            else
            {
                graph.connections.Add(Connect(
                    commit,
                    CommitRecruitmentEncounterNode.CompletedPort,
                    finalize,
                    FlowPort.In));
                graph.connections.Add(Connect(
                    resume,
                    ResumeRecruitmentEncounterNode.PostDialoguePort,
                    finalize,
                    FlowPort.In));
            }
            graph.editorGroups = new List<FlowGraphGroup>
            {
                new()
                {
                    title = "영입 조우 표준 흐름",
                    position = new Vector2(0f, -40f),
                    nodeIds = new List<string>
                    {
                        volumeEntry.id,
                        resumeEntry.id,
                        resume.id,
                        wait.id,
                        prepare.id,
                        playDialogue.id,
                        commit.id,
                        finalize.id,
                    },
                },
            };
            if (postDialogue != null)
                graph.editorGroups[0].nodeIds.Insert(graph.editorGroups[0].nodeIds.Count - 1, postDialogue.id);
        }

        private static FlowConnection Connect(
            FlowNode source,
            string sourcePort,
            FlowNode target,
            string targetPort) =>
            new()
            {
                fromNodeId = source.id,
                fromPort = sourcePort,
                toNodeId = target.id,
                toPort = targetPort,
            };

        private void BuildSceneBindingCore()
        {
            RecruitmentEncounterAnchor anchor = ResolveOrCreateAnchor();
            FlowGraphRunner runner = ResolveOrCreateRunner(anchor);
            MonsterGroupController hostileGroup = ResolveOrCreateHostileGroup(anchor);
            FlowGraphTriggerVolume entryVolume = ResolveOrCreateEntryVolume(anchor, runner);
            Transform dialogueAnchor = ResolveOrCreateDialogueAnchor(anchor);

            var participants = new List<RecruitmentEncounterParticipant>(_hostiles.Count + 1)
            {
                ConfigureParticipant(
                    _allyActor,
                    _allyParticipantId,
                    RecruitmentEncounterRole.RequiredAlly),
            };
            for (int i = 0; i < _hostiles.Count; i++)
            {
                HostileDraft hostile = _hostiles[i];
                participants.Add(ConfigureParticipant(
                    hostile.actor,
                    hostile.participantId,
                    RecruitmentEncounterRole.Hostile));
            }

            ConfigureRunner(runner);
            ConfigureEntryVolume(entryVolume, runner);
            ConfigureAnchor(
                anchor,
                runner,
                entryVolume,
                hostileGroup,
                dialogueAnchor,
                participants);

            _anchor = anchor;
            _flowRunner = runner;
            _hostileGroup = hostileGroup;
            _entryVolume = entryVolume;
            _dialogueAnchor = dialogueAnchor;

            EditorSceneManager.MarkSceneDirty(anchor.gameObject.scene);
            Selection.activeGameObject = anchor.gameObject;
            EditorGUIUtility.PingObject(anchor);
        }

        private RecruitmentEncounterAnchor ResolveOrCreateAnchor()
        {
            if (_anchor != null)
            {
                if (_anchor.gameObject == _allyActor.gameObject || ContainsHostile(_anchor.gameObject))
                    throw new InvalidOperationException("Anchor는 참가자 GameObject와 분리된 활성 루트여야 합니다.");
                return _anchor;
            }

            string rootName = $"RecruitmentEncounter_{SanitizeAssetName(_definition.EncounterId)}";
            var rootObject = new GameObject(rootName);
            Undo.RegisterCreatedObjectUndo(rootObject, "Create Recruitment Encounter Root");
            if (_sceneParent != null)
                Undo.SetTransformParent(rootObject.transform, _sceneParent, "Parent Recruitment Encounter Root");
            rootObject.transform.position = _allyActor.transform.position;
            rootObject.transform.rotation = Quaternion.identity;
            return Undo.AddComponent<RecruitmentEncounterAnchor>(rootObject);
        }

        private FlowGraphRunner ResolveOrCreateRunner(RecruitmentEncounterAnchor anchor)
        {
            if (_flowRunner != null)
                return _flowRunner;
            FlowGraphRunner runner = anchor.GetComponent<FlowGraphRunner>();
            return runner != null ? runner : Undo.AddComponent<FlowGraphRunner>(anchor.gameObject);
        }

        private MonsterGroupController ResolveOrCreateHostileGroup(RecruitmentEncounterAnchor anchor)
        {
            if (_hostileGroup != null)
                return _hostileGroup;

            GameObject groupObject = CreateChildObject("HostileGroup", anchor.transform);
            return Undo.AddComponent<MonsterGroupController>(groupObject);
        }

        private FlowGraphTriggerVolume ResolveOrCreateEntryVolume(
            RecruitmentEncounterAnchor anchor,
            FlowGraphRunner runner)
        {
            if (_entryVolume != null)
                return _entryVolume;

            GameObject volumeObject = CreateChildObject("EntryVolume", anchor.transform);
            BoxCollider collider = Undo.AddComponent<BoxCollider>(volumeObject);
            collider.isTrigger = true;
            collider.size = ClampVolumeSize(_entryVolumeSize);
            FlowGraphTriggerVolume volume = Undo.AddComponent<FlowGraphTriggerVolume>(volumeObject);
            ConfigureEntryVolume(volume, runner);
            return volume;
        }

        private Transform ResolveOrCreateDialogueAnchor(RecruitmentEncounterAnchor anchor)
        {
            if (_dialogueAnchor != null)
                return _dialogueAnchor;

            GameObject anchorObject = CreateChildObject("DialogueAnchor", anchor.transform);
            anchorObject.transform.position = _allyActor.transform.position;
            anchorObject.transform.rotation = _allyActor.transform.rotation;
            return anchorObject.transform;
        }

        private RecruitmentEncounterParticipant ConfigureParticipant(
            MonsterActor actor,
            string participantId,
            RecruitmentEncounterRole role)
        {
            RecruitmentEncounterParticipant participant =
                actor.GetComponent<RecruitmentEncounterParticipant>();
            if (participant == null)
                participant = Undo.AddComponent<RecruitmentEncounterParticipant>(actor.gameObject);

            Undo.RecordObject(participant, "Configure Recruitment Encounter Participant");
            var serializedParticipant = new SerializedObject(participant);
            serializedParticipant.FindProperty("_participantId").stringValue = participantId.Trim();
            serializedParticipant.FindProperty("_role").enumValueIndex = (int)role;
            serializedParticipant.FindProperty("_actor").objectReferenceValue = actor;
            serializedParticipant.ApplyModifiedProperties();
            PrefabUtility.RecordPrefabInstancePropertyModifications(participant);

            if (role == RecruitmentEncounterRole.RequiredAlly)
            {
                Undo.RecordObject(actor, "Disable Runtime Death Recruitment");
                var serializedActor = new SerializedObject(actor);
                SerializedProperty recruitableAs = serializedActor.FindProperty("_recruitableAs");
                if (recruitableAs != null)
                {
                    recruitableAs.enumValueIndex = (int)CharacterActorType.None;
                    serializedActor.ApplyModifiedProperties();
                    PrefabUtility.RecordPrefabInstancePropertyModifications(actor);
                }
            }

            if (actor.gameObject.activeSelf)
            {
                Undo.RecordObject(actor.gameObject, "Set Recruitment Participant Dormant");
                actor.gameObject.SetActive(false);
            }
            EditorUtility.SetDirty(actor.gameObject);
            return participant;
        }

        private void ConfigureRunner(FlowGraphRunner runner)
        {
            Undo.RecordObject(runner, "Configure Recruitment FlowGraph Runner");
            var serializedRunner = new SerializedObject(runner);
            serializedRunner.FindProperty("_graph").objectReferenceValue = _flowGraph;
            serializedRunner.ApplyModifiedProperties();
            PrefabUtility.RecordPrefabInstancePropertyModifications(runner);
        }

        private void ConfigureEntryVolume(FlowGraphTriggerVolume volume, FlowGraphRunner runner)
        {
            Collider collider = volume.GetComponent<Collider>();
            if (collider == null)
                collider = Undo.AddComponent<BoxCollider>(volume.gameObject);

            Undo.RecordObject(collider, "Configure Recruitment Entry Volume Collider");
            collider.isTrigger = true;
            if (collider is BoxCollider box)
                box.size = ClampVolumeSize(_entryVolumeSize);

            Undo.RecordObject(volume, "Configure Recruitment Entry Volume");
            var serializedVolume = new SerializedObject(volume);
            serializedVolume.FindProperty("_runner").objectReferenceValue = runner;
            serializedVolume.FindProperty("_volumeId").stringValue = _entryVolumeId.Trim();
            serializedVolume.FindProperty("_volumeCollider").objectReferenceValue = collider;
            serializedVolume.FindProperty("_actorFilter").intValue = (int)ActorType.Player;
            serializedVolume.ApplyModifiedProperties();
            PrefabUtility.RecordPrefabInstancePropertyModifications(volume);
        }

        private void ConfigureAnchor(
            RecruitmentEncounterAnchor anchor,
            FlowGraphRunner runner,
            FlowGraphTriggerVolume entryVolume,
            MonsterGroupController hostileGroup,
            Transform dialogueAnchor,
            List<RecruitmentEncounterParticipant> participants)
        {
            Undo.RecordObject(anchor, "Configure Recruitment Encounter Anchor");
            var serializedAnchor = new SerializedObject(anchor);
            serializedAnchor.FindProperty("_definition").objectReferenceValue = _definition;
            serializedAnchor.FindProperty("_flowRunner").objectReferenceValue = runner;
            serializedAnchor.FindProperty("_entryVolume").objectReferenceValue = entryVolume;
            serializedAnchor.FindProperty("_resumeEntryId").stringValue = _resumeEntryId.Trim();
            serializedAnchor.FindProperty("_allyActor").objectReferenceValue = _allyActor;
            serializedAnchor.FindProperty("_hostileGroup").objectReferenceValue = hostileGroup;
            serializedAnchor.FindProperty("_dialogueAnchor").objectReferenceValue = dialogueAnchor;

            SerializedProperty participantArray = serializedAnchor.FindProperty("_participants");
            participantArray.arraySize = participants.Count;
            for (int i = 0; i < participants.Count; i++)
            {
                participantArray.GetArrayElementAtIndex(i).objectReferenceValue = participants[i];
            }

            serializedAnchor.ApplyModifiedProperties();
            PrefabUtility.RecordPrefabInstancePropertyModifications(anchor);
        }

        private void SynchronizeGraphReferences()
        {
            if (_flowGraph == null || string.IsNullOrWhiteSpace(CurrentEncounterId))
            {
                SetStatus("동기화할 FlowGraph와 encounterId가 필요합니다.", MessageType.Warning);
                return;
            }

            var existingIds = new HashSet<string>(StringComparer.Ordinal);
            int volumeEntryCount = 0;
            for (int i = 0; i < _flowGraph.nodes.Count; i++)
            {
                switch (_flowGraph.nodes[i])
                {
                    case ResumeRecruitmentEncounterNode resume:
                        AddNonEmpty(existingIds, resume.encounterId);
                        break;
                    case WaitRecruitmentCombatResolvedNode wait:
                        AddNonEmpty(existingIds, wait.encounterId);
                        break;
                    case PrepareRecruitmentDialogueNode prepare:
                        AddNonEmpty(existingIds, prepare.encounterId);
                        break;
                    case PlayDialogueRequiredNode dialogue:
                        AddNonEmpty(existingIds, dialogue.encounterId);
                        break;
                    case CommitRecruitmentEncounterNode commit:
                        AddNonEmpty(existingIds, commit.encounterId);
                        break;
                    case PlayRecruitmentPostDialogueNode postDialogue:
                        AddNonEmpty(existingIds, postDialogue.encounterId);
                        break;
                    case FinalizeRecruitmentEncounterNode finalize:
                        AddNonEmpty(existingIds, finalize.encounterId);
                        break;
                    case OnTriggerVolumeEntryNode:
                        volumeEntryCount++;
                        break;
                }
            }

            if (existingIds.Count > 1
                && !EditorUtility.DisplayDialog(
                    "여러 조우 ID가 있는 그래프",
                    "선택한 그래프에는 서로 다른 영입 조우 ID가 있습니다. 모든 영입 조우 노드를 현재 ID로 동기화할까요?",
                    "동기화",
                    "취소"))
            {
                return;
            }

            Undo.RecordObject(_flowGraph, "Synchronize Recruitment Encounter FlowGraph");
            for (int i = 0; i < _flowGraph.nodes.Count; i++)
            {
                switch (_flowGraph.nodes[i])
                {
                    case ResumeRecruitmentEncounterNode resume:
                        resume.encounterId = CurrentEncounterId;
                        break;
                    case WaitRecruitmentCombatResolvedNode wait:
                        wait.encounterId = CurrentEncounterId;
                        break;
                    case PrepareRecruitmentDialogueNode prepare:
                        prepare.encounterId = CurrentEncounterId;
                        break;
                    case PlayDialogueRequiredNode dialogue:
                        dialogue.encounterId = CurrentEncounterId;
                        dialogue.dialogue = _dialogue;
                        break;
                    case CommitRecruitmentEncounterNode commit:
                        commit.encounterId = CurrentEncounterId;
                        break;
                    case PlayRecruitmentPostDialogueNode postDialogue:
                        postDialogue.encounterId = CurrentEncounterId;
                        postDialogue.dialogue = _postRecruitmentDialogue;
                        break;
                    case FinalizeRecruitmentEncounterNode finalize:
                        finalize.encounterId = CurrentEncounterId;
                        break;
                    case OnTriggerVolumeEntryNode volumeEntry when volumeEntryCount == 1:
                        volumeEntry.volumeId = _entryVolumeId;
                        volumeEntry.phase = FlowVolumePhase.Enter;
                        break;
                }
            }

            EditorUtility.SetDirty(_flowGraph);
            AssetDatabase.SaveAssets();
            _issues.Clear();
            if (_anchor != null)
                RefreshValidation(includeProjectIdScan: true);
            SetStatus(
                volumeEntryCount <= 1
                    ? "영입 조우 노드의 ID, 대화와 진입 볼륨 참조를 동기화했습니다."
                    : "영입 조우 노드의 ID와 대화를 동기화했습니다. 진입 볼륨 노드가 여러 개라 volumeId는 보존했습니다.",
                volumeEntryCount <= 1 ? MessageType.Info : MessageType.Warning);
        }

        private List<string> CollectAssetErrors()
        {
            var errors = new List<string>();
            string encounterId = CurrentEncounterId;
            if (_definition == null)
            {
                if (string.IsNullOrWhiteSpace(encounterId))
                    errors.Add("신규 조우의 조우 저장 ID가 필요합니다.");
                else
                {
                    if (ContainsWhitespace(encounterId))
                        errors.Add("조우 저장 ID에는 공백을 사용할 수 없습니다.");
                    if (HasDuplicateDefinitionId(encounterId))
                        errors.Add($"조우 저장 ID '{encounterId}'를 사용하는 조우 정의가 이미 있습니다.");
                }

                if (_recruitCharacter == CharacterActorType.None)
                    errors.Add("영입 캐릭터를 지정하세요.");
                if (_allyFaction == null)
                    errors.Add("임시 아군 진영을 지정하세요.");
                if (!string.IsNullOrWhiteSpace(_prerequisiteEncounterId)
                    && string.Equals(
                        encounterId?.Trim(),
                        _prerequisiteEncounterId.Trim(),
                        StringComparison.Ordinal))
                {
                    errors.Add("선행 조우 ID는 현재 조우 ID와 같을 수 없습니다.");
                }
            }

            if (_flowGraph == null && _dialogue == null)
                errors.Add("표준 FlowGraph를 생성하려면 필수 대화 그래프를 지정하세요.");
            if (!IsAssetFolderPath(_storyFolder))
                errors.Add("Story 저장 폴더는 Assets/ 아래 경로여야 합니다.");
            if (!IsAssetFolderPath(_flowFolder))
                errors.Add("Flow 저장 폴더는 Assets/ 아래 경로여야 합니다.");
            if (string.IsNullOrWhiteSpace(_entryVolumeId))
                errors.Add("진입 볼륨 ID가 필요합니다.");
            if (string.IsNullOrWhiteSpace(_resumeEntryId))
                errors.Add("로드 재개 진입 ID가 필요합니다.");
            return errors;
        }

        private List<string> CollectDraftErrors(bool requireAssets)
        {
            var errors = new List<string>();
            if (EditorApplication.isPlaying)
                errors.Add("Play Mode에서는 영입 조우 데이터를 생성하거나 씬을 변경할 수 없습니다.");
            if (string.IsNullOrWhiteSpace(CurrentEncounterId))
                errors.Add("조우 저장 ID가 필요합니다.");
            else if (ContainsWhitespace(CurrentEncounterId))
                errors.Add("조우 저장 ID에는 공백을 사용할 수 없습니다.");
            if (string.IsNullOrWhiteSpace(_entryVolumeId))
                errors.Add("진입 볼륨 ID가 필요합니다.");
            if (string.IsNullOrWhiteSpace(_resumeEntryId))
                errors.Add("로드 재개 진입 ID가 필요합니다.");
            if (requireAssets && _definition == null)
                errors.Add("씬 바인딩 전에 조우 정의 에셋을 생성하거나 선택하세요.");
            if (requireAssets && _flowGraph == null)
                errors.Add("씬 바인딩 전에 FlowGraph 에셋을 생성하거나 선택하세요.");
            if (_allyActor == null)
                errors.Add("영입 대상 몬스터를 지정하세요.");
            if (_hostiles.Count == 0)
                errors.Add("함께 배치할 적을 한 명 이상 지정하세요.");

            var actorIds = new HashSet<int>();
            var participantIds = new HashSet<string>(StringComparer.Ordinal);
            if (_allyActor != null)
            {
                ValidateDraftParticipant(
                    _allyActor,
                    _allyParticipantId,
                    "영입 대상",
                    actorIds,
                    participantIds,
                    errors);
            }
            for (int i = 0; i < _hostiles.Count; i++)
            {
                HostileDraft hostile = _hostiles[i];
                if (hostile == null)
                {
                    errors.Add($"적 참가자 {i + 1}번 항목이 비어 있습니다.");
                    continue;
                }

                ValidateDraftParticipant(
                    hostile.actor,
                    hostile.participantId,
                    $"적 {i + 1}",
                    actorIds,
                    participantIds,
                    errors);
            }

            if (_anchor != null
                && (_anchor.gameObject == _allyActor?.gameObject || ContainsHostile(_anchor.gameObject)))
            {
                errors.Add("Anchor는 참가자 GameObject와 분리된 활성 루트여야 합니다.");
            }

            return errors;
        }

        private static void ValidateDraftParticipant(
            MonsterActor actor,
            string participantId,
            string label,
            HashSet<int> actorIds,
            HashSet<string> participantIds,
            List<string> errors)
        {
            if (actor == null)
            {
                errors.Add($"{label} 몬스터가 비어 있습니다.");
                return;
            }
            if (EditorUtility.IsPersistent(actor) || !actor.gameObject.scene.IsValid())
                errors.Add($"{label} '{actor.name}'는 프리팹 에셋이 아니라 현재 씬 인스턴스여야 합니다.");
            if (!actorIds.Add(actor.GetInstanceID()))
                errors.Add($"몬스터 '{actor.name}'가 참가자에 중복 지정됐습니다.");
            if (string.IsNullOrWhiteSpace(participantId))
                errors.Add($"{label} 저장 ID가 비어 있습니다.");
            else if (!participantIds.Add(participantId.Trim()))
                errors.Add($"저장 ID '{participantId}'가 중복됩니다.");
        }

        private void AddSelectedHostiles()
        {
            GameObject[] selectedObjects = Selection.gameObjects;
            int added = 0;
            for (int i = 0; i < selectedObjects.Length; i++)
            {
                MonsterActor actor = selectedObjects[i].GetComponent<MonsterActor>()
                                     ?? selectedObjects[i].GetComponentInChildren<MonsterActor>(true);
                if (TryAddHostile(actor))
                    added++;
            }

            SetStatus($"선택에서 적 참가자 {added}명을 추가했습니다.", MessageType.Info);
        }

        private void ScanHostileGroupChildren()
        {
            if (_hostileGroup == null)
            {
                SetStatus("먼저 적 그룹을 지정하세요.", MessageType.Warning);
                return;
            }

            MonsterActor[] actors = _hostileGroup.GetComponentsInChildren<MonsterActor>(true);
            int added = 0;
            for (int i = 0; i < actors.Length; i++)
            {
                if (TryAddHostile(actors[i]))
                    added++;
            }

            SetStatus($"적 그룹 자식에서 참가자 {added}명을 추가했습니다.", MessageType.Info);
        }

        private bool TryAddHostile(MonsterActor actor)
        {
            if (actor == null || actor == _allyActor)
                return false;
            for (int i = 0; i < _hostiles.Count; i++)
            {
                if (_hostiles[i]?.actor == actor)
                    return false;
            }

            _hostiles.Add(new HostileDraft
            {
                actor = actor,
                participantId = SuggestParticipantId(actor, "hostile"),
            });
            return true;
        }

        private void SuggestStableIds()
        {
            if (string.IsNullOrWhiteSpace(_encounterId)
                && _recruitCharacter != CharacterActorType.None)
            {
                _encounterId = $"story.recruitment.{_recruitCharacter.ToString().ToLowerInvariant()}";
            }

            if (string.IsNullOrWhiteSpace(_entryVolumeId) && !string.IsNullOrWhiteSpace(_encounterId))
                _entryVolumeId = $"{_encounterId}.entry";
            if (string.IsNullOrWhiteSpace(_allyParticipantId) && _allyActor != null)
                _allyParticipantId = SuggestParticipantId(_allyActor, "ally");
            for (int i = 0; i < _hostiles.Count; i++)
            {
                HostileDraft hostile = _hostiles[i];
                if (hostile != null
                    && hostile.actor != null
                    && string.IsNullOrWhiteSpace(hostile.participantId))
                {
                    hostile.participantId = SuggestParticipantId(hostile.actor, $"hostile_{i + 1}");
                }
            }
        }

        private static string SuggestParticipantId(MonsterActor actor, string fallback)
        {
            string source = !string.IsNullOrWhiteSpace(actor?.ActorId)
                ? actor.ActorId
                : actor != null
                    ? actor.name
                    : fallback;
            string normalized = SanitizeStableId(source);
            return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
        }

        private static CombatFactionSO FindFactionById(string factionId)
        {
            string[] guids = AssetDatabase.FindAssets("t:CombatFactionSO");
            for (int i = 0; i < guids.Length; i++)
            {
                CombatFactionSO faction = AssetDatabase.LoadAssetAtPath<CombatFactionSO>(
                    AssetDatabase.GUIDToAssetPath(guids[i]));
                if (faction != null
                    && string.Equals(faction.FactionId, factionId, StringComparison.Ordinal))
                {
                    return faction;
                }
            }

            return null;
        }

        private bool ContainsHostile(GameObject gameObject)
        {
            for (int i = 0; i < _hostiles.Count; i++)
            {
                if (_hostiles[i]?.actor != null && _hostiles[i].actor.gameObject == gameObject)
                    return true;
            }

            return false;
        }

        private string CurrentEncounterId => _definition != null
            ? _definition.EncounterId
            : _encounterId?.Trim();

        private static GameObject CreateChildObject(string name, Transform parent)
        {
            var child = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(child, $"Create {name}");
            Undo.SetTransformParent(child.transform, parent, $"Parent {name}");
            child.transform.localPosition = Vector3.zero;
            child.transform.localRotation = Quaternion.identity;
            child.transform.localScale = Vector3.one;
            return child;
        }

        private static Vector3 ClampVolumeSize(Vector3 size) =>
            new(
                Mathf.Max(0.1f, Mathf.Abs(size.x)),
                Mathf.Max(0.1f, Mathf.Abs(size.y)),
                Mathf.Max(0.1f, Mathf.Abs(size.z)));

        private static int BeginUndoGroup(string name)
        {
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(name);
            return group;
        }

        private static void EnsureAssetFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
                return;
            if (!IsAssetFolderPath(folderPath))
                throw new InvalidOperationException($"Assets/ 아래 경로가 아닙니다: {folderPath}");

            string[] parts = folderPath.Split('/');
            string current = "Assets";
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private void DeleteCreatedAssets(List<string> createdAssetPaths)
        {
            string definitionPath = _definition != null
                ? AssetDatabase.GetAssetPath(_definition)
                : null;
            string graphPath = _flowGraph != null
                ? AssetDatabase.GetAssetPath(_flowGraph)
                : null;
            bool clearDefinition = createdAssetPaths.Contains(definitionPath);
            bool clearGraph = createdAssetPaths.Contains(graphPath);

            for (int i = createdAssetPaths.Count - 1; i >= 0; i--)
            {
                string path = createdAssetPaths[i];
                if (!string.IsNullOrWhiteSpace(path) && AssetDatabase.LoadMainAssetAtPath(path) != null)
                    AssetDatabase.DeleteAsset(path);
            }
            AssetDatabase.SaveAssets();

            if (clearDefinition)
                _definition = null;
            if (clearGraph)
                _flowGraph = null;
        }

        private bool HasDuplicateDefinitionId(string encounterId)
        {
            string[] guids = AssetDatabase.FindAssets("t:RecruitmentEncounterDefinitionSO");
            for (int i = 0; i < guids.Length; i++)
            {
                RecruitmentEncounterDefinitionSO candidate =
                    AssetDatabase.LoadAssetAtPath<RecruitmentEncounterDefinitionSO>(
                        AssetDatabase.GUIDToAssetPath(guids[i]));
                if (candidate != null
                    && candidate != _definition
                    && string.Equals(candidate.EncounterId, encounterId.Trim(), StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryContinue(List<string> errors)
        {
            if (errors.Count == 0)
                return true;

            string message = string.Join("\n", errors);
            SetStatus(message, MessageType.Error);
            return false;
        }

        private static void AddRange(List<string> target, List<string> source)
        {
            for (int i = 0; i < source.Count; i++)
            {
                if (!target.Contains(source[i]))
                    target.Add(source[i]);
            }
        }

        private static void RemoveAssetPresenceErrors(List<string> errors)
        {
            errors.Remove("씬 바인딩 전에 조우 정의 에셋을 생성하거나 선택하세요.");
            errors.Remove("씬 바인딩 전에 FlowGraph 에셋을 생성하거나 선택하세요.");
        }

        private static bool IsAssetFolderPath(string path) =>
            !string.IsNullOrWhiteSpace(path)
            && (string.Equals(path, "Assets", StringComparison.Ordinal)
                || path.StartsWith("Assets/", StringComparison.Ordinal));

        private static bool ContainsWhitespace(string value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                if (char.IsWhiteSpace(value[i]))
                    return true;
            }

            return false;
        }

        private static string SanitizeAssetName(string value)
        {
            string stableId = SanitizeStableId(value);
            return string.IsNullOrWhiteSpace(stableId)
                ? "NewEncounter"
                : stableId.Replace('.', '_');
        }

        private static string SanitizeStableId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            char[] buffer = new char[value.Length];
            int length = 0;
            bool previousSeparator = false;
            for (int i = 0; i < value.Length; i++)
            {
                char character = char.ToLowerInvariant(value[i]);
                bool isAllowed = char.IsLetterOrDigit(character)
                                 || character is '.' or '-' or '_';
                if (isAllowed)
                {
                    buffer[length++] = character;
                    previousSeparator = false;
                }
                else if (!previousSeparator && length > 0)
                {
                    buffer[length++] = '_';
                    previousSeparator = true;
                }
            }

            return new string(buffer, 0, length).Trim('_', '.', '-');
        }

        private static void AddNonEmpty(HashSet<string> values, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                values.Add(value);
        }
    }
}
