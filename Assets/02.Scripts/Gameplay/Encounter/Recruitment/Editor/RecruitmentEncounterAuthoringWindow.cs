using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Story;
using UPlayGround.Dialogue;
using UPlayGround.EditorTools;
using UPlayGround.FlowGraph;
using UPlayGround.Group;

namespace UPlayGround.Gameplay.Encounter.Editor
{
    /// <summary>영입 조우 데이터, 씬 참가자와 표준 FlowGraph를 한 작업 공간에서 생성·연결·검증한다.</summary>
    public sealed partial class RecruitmentEncounterAuthoringWindow : EditorWindow
    {
        private const string ToolId = "UPlayGround/게임플레이/흐름/영입 조우 저작";
        private const string DefaultStoryFolder = "Assets/10.Datas/Story/Recruitment";
        private const string DefaultFlowFolder = "Assets/10.Datas/Flow/Recruitment";
        private const string DefaultResumeEntryId = "Resume";

        private static readonly RecruitmentAllyFailurePolicy[] s_allyFailurePolicyValues =
        {
            RecruitmentAllyFailurePolicy.Incapacitate,
        };

        private static readonly GUIContent[] s_allyFailurePolicyOptions =
        {
            new(
                "전투 불능 상태로 유지",
                "아군의 체력이 소진되어도 사망시키지 않고 전투 불능 상태로 남깁니다."),
        };

        private static readonly RecruitmentEncounterCombatMode[] s_combatModeValues =
        {
            RecruitmentEncounterCombatMode.HostileRecruitTarget,
            RecruitmentEncounterCombatMode.CooperativeBattle,
        };

        private static readonly GUIContent[] s_combatModeOptions =
        {
            new(
                "적대 조우 후 합류",
                "영입 대상과 먼저 대화한 뒤 직접 싸워 승리하면 파티에 합류합니다."),
            new(
                "공동 전투 후 합류",
                "영입 대상과 함께 적을 처치한 뒤 대화를 완료하면 파티에 합류합니다."),
        };

        private static readonly RecruitmentIncapacitationRule[] s_incapacitationRuleValues =
        {
            RecruitmentIncapacitationRule.FinishAttack,
            RecruitmentIncapacitationRule.AnyFatalDamage,
        };

        private static readonly GUIContent[] s_incapacitationRuleOptions =
        {
            new(
                "브레이크 후 피니시 공격",
                "체력 소진 시 브레이크 노출을 열고, 플레이어가 피니시 공격을 성공시켜야 제압합니다."),
            new(
                "치명 피해 즉시 제압",
                "체력을 소진한 공격을 곧바로 제압으로 처리합니다. 기존 호환 또는 비전투 연출용입니다."),
        };

        private static readonly RecruitmentEncounterResetScope[] s_resetScopeValues =
        {
            RecruitmentEncounterResetScope.PersistUntilNewGame,
            RecruitmentEncounterResetScope.ResetOnCycle,
        };

        private static readonly GUIContent[] s_resetScopeOptions =
        {
            new(
                "새 게임 전까지 유지",
                "완료 여부와 전투 진행을 저장하며, 사이클이 바뀌어도 초기화하지 않습니다."),
            new(
                "새 사이클마다 초기화",
                "사이클이 시작될 때 조우 진행을 처음 상태로 되돌립니다."),
        };

        [Serializable]
        private sealed class HostileDraft
        {
            public MonsterActor actor;
            public string participantId;
        }

        [SerializeField] private RecruitmentEncounterAnchor _anchor;
        [SerializeField] private RecruitmentEncounterDefinitionSO _definition;
        [SerializeField] private FlowGraphSO _flowGraph;
        [SerializeField] private DialogueGraphSO _dialogue;
        [SerializeField] private DialogueGraphSO _postRecruitmentDialogue;

        [SerializeField] private Transform _sceneParent;
        [SerializeField] private FlowGraphRunner _flowRunner;
        [SerializeField] private FlowGraphTriggerVolume _entryVolume;
        [SerializeField] private MonsterActor _allyActor;
        [SerializeField] private MonsterGroupController _hostileGroup;
        [SerializeField] private Transform _dialogueAnchor;

        [SerializeField] private string _encounterId;
        [SerializeField] private string _prerequisiteEncounterId;
        [SerializeField] private RecruitmentEncounterCombatMode _combatMode =
            RecruitmentEncounterCombatMode.HostileRecruitTarget;
        [SerializeField] private RecruitmentIncapacitationRule _incapacitationRule =
            RecruitmentIncapacitationRule.FinishAttack;
        [SerializeField] private CharacterActorType _recruitCharacter;
        [SerializeField] private CombatFactionSO _allyFaction;
        [SerializeField] private RecruitmentAllyFailurePolicy _allyFailurePolicy =
            RecruitmentAllyFailurePolicy.Incapacitate;
        [SerializeField] private RecruitmentEncounterResetScope _resetScope =
            RecruitmentEncounterResetScope.PersistUntilNewGame;
        [SerializeField] private float _postCombatSettleSeconds = 1.25f;
        [SerializeField] private float _dialogueApproachDistance = 2.8f;
        [SerializeField] private float _dialogueApproachSpeedMultiplier = 0.65f;
        [SerializeField] private float _dialogueApproachTimeoutSeconds = 6f;

        [SerializeField] private bool _snapParticipantsToGround = true;

        [SerializeField] private string _resumeEntryId = DefaultResumeEntryId;
        [SerializeField] private string _entryVolumeId;
        [SerializeField] private Vector3 _entryVolumeSize = new(10f, 3f, 10f);
        [SerializeField] private string _allyParticipantId;
        [SerializeField] private List<HostileDraft> _hostiles = new();

        [SerializeField] private string _storyFolder = DefaultStoryFolder;
        [SerializeField] private string _flowFolder = DefaultFlowFolder;
        [SerializeField] private bool _showFieldHelp = true;

        private int _groundSnappedCount;
        private int _groundSnapFailedCount;

        [SerializeField] private bool _showExistingSceneBindings;
        [SerializeField] private bool _showAdvancedEntrySettings;
        [SerializeField] private bool _showAssetPaths;

        private readonly List<RecruitmentEncounterAuthoringIssue> _issues = new();
        private Vector2 _scrollPosition;
        private string _statusMessage;
        private MessageType _statusType = MessageType.Info;

        [UPlaygroundTool(ToolId)]
        public static void Open()
        {
            RecruitmentEncounterAuthoringWindow window = GetWindow<RecruitmentEncounterAuthoringWindow>(
                "영입 조우 저작");
            window.minSize = new Vector2(720f, 720f);
            window.Show();
            window.TryLoadSelectedAnchor();
        }

        public static void OpenForAnchor(RecruitmentEncounterAnchor anchor)
        {
            RecruitmentEncounterAuthoringWindow window = GetWindow<RecruitmentEncounterAuthoringWindow>(
                "영입 조우 저작");
            window.minSize = new Vector2(720f, 720f);
            window.Show();
            window.LoadAnchor(anchor);
        }

        private void OnEnable()
        {
            _storyFolder = string.IsNullOrWhiteSpace(_storyFolder)
                ? DefaultStoryFolder
                : _storyFolder;
            _flowFolder = string.IsNullOrWhiteSpace(_flowFolder)
                ? DefaultFlowFolder
                : _flowFolder;
            _resumeEntryId = string.IsNullOrWhiteSpace(_resumeEntryId)
                ? DefaultResumeEntryId
                : _resumeEntryId;
            _allyFaction ??= FindFactionById(CombatFactionRules.PlayerPartyId);
        }

        private void CreateGUI()
        {
            UPlaygroundEditorUX.BuildLegacyWindow(
                rootVisualElement,
                "영입 조우 저작",
                "적대 조우 또는 공동 전투 영입 흐름을 데이터·씬·FlowGraph 단위로 만들고 진행 불능 구성을 검증합니다.",
                "d_SceneViewFx",
                DrawWindow,
                "up-recruitment-encounter-authoring");
        }

        private void DrawWindow()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            DrawTargetSection();
            DrawDefinitionSection();
            DrawSceneSection();
            DrawActionSection();
            DrawValidationSection();
            EditorGUILayout.EndScrollView();
        }

        private void DrawTargetSection()
        {
            DrawSectionTitle("작업 대상");
            _showFieldHelp = EditorGUILayout.ToggleLeft(
                "항목 설명 표시",
                _showFieldHelp,
                EditorStyles.boldLabel);
            if (_showFieldHelp)
            {
                EditorGUILayout.HelpBox(
                    "처음 만드는 조우는 ① 조우 방식·대화·해금 캐릭터 선택 → "
                    + "② 씬의 영입 대상 몬스터와 필요하면 추가 적 지정 → ③ '신규 조우 전체 생성' 순서로 진행하세요. "
                    + "Anchor·FlowGraph·실행기·진입 볼륨은 비워 두면 자동으로 만듭니다.",
                    MessageType.Info);
            }

            EditorGUI.BeginChangeCheck();
            RecruitmentEncounterAnchor nextAnchor = (RecruitmentEncounterAnchor)EditorGUILayout.ObjectField(
                FieldLabel(
                    "기존 조우 Anchor (선택)",
                    "이미 만들어진 영입 조우를 수정할 때 씬의 RecruitmentEncounterAnchor를 지정합니다. 신규 조우는 비워 둡니다."),
                _anchor,
                typeof(RecruitmentEncounterAnchor),
                true);
            if (EditorGUI.EndChangeCheck())
                LoadAnchor(nextAnchor);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent(
                    "선택한 기존 조우 불러오기",
                    "Hierarchy에서 선택한 오브젝트의 상위 Anchor를 찾아 편집 대상으로 불러옵니다.")))
                TryLoadSelectedAnchor(showMessage: true);
            if (GUILayout.Button(new GUIContent(
                    "새 조우 시작",
                    "현재 창의 입력값을 초기화하고 신규 조우 저작을 시작합니다.")))
                ResetDraft();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox(
                _anchor == null
                    ? "새 조우 모드입니다. 필수 데이터를 지정한 뒤 '전체 스캐폴드 생성'을 실행하세요."
                    : $"기존 조우 '{_anchor.EncounterId}'를 편집 중입니다. 기존 encounterId와 에셋 GUID는 보존됩니다.",
                MessageType.Info);
        }

        private void DrawDefinitionSection()
        {
            DrawSectionTitle("1. 조우 데이터와 흐름");
            if (_showFieldHelp)
            {
                EditorGUILayout.HelpBox(
                    "신규 조우에서는 조우 방식에 맞는 필수 대화와 해금할 캐릭터가 핵심 입력입니다. "
                    + "기존 정의와 흐름 그래프는 수정 작업일 때만 지정하세요.",
                    MessageType.None);
            }

            EditorGUI.BeginChangeCheck();
            RecruitmentEncounterDefinitionSO nextDefinition =
                (RecruitmentEncounterDefinitionSO)EditorGUILayout.ObjectField(
                    FieldLabel(
                        "기존 조우 정의 (선택)",
                        "수정할 RecruitmentEncounterDefinitionSO입니다. 신규 조우는 비워 두면 자동 생성합니다."),
                    _definition,
                    typeof(RecruitmentEncounterDefinitionSO),
                    false);
            if (EditorGUI.EndChangeCheck())
                LoadDefinition(nextDefinition);

            EditorGUI.BeginChangeCheck();
            FlowGraphSO nextGraph = (FlowGraphSO)EditorGUILayout.ObjectField(
                FieldLabel(
                    "기존 흐름 그래프 (선택)",
                    "수정하거나 재사용할 FlowGraphSO입니다. 신규 조우는 비워 두면 표준 흐름을 자동 생성합니다."),
                _flowGraph,
                typeof(FlowGraphSO),
                false);
            if (EditorGUI.EndChangeCheck())
            {
                _flowGraph = nextGraph;
                LoadDialogueFromGraph();
                _issues.Clear();
            }

            _dialogue = (DialogueGraphSO)EditorGUILayout.ObjectField(
                FieldLabel(
                    _combatMode == RecruitmentEncounterCombatMode.HostileRecruitTarget
                        ? "전투 전 조우 대화 (필수)"
                        : "영입 확정 대화 (필수)",
                    _combatMode == RecruitmentEncounterCombatMode.HostileRecruitTarget
                        ? "영입 대상과 싸우기 전에 재생하며, 정상 종료되어야 전투가 시작되는 DialogueGraphSO입니다."
                        : "공동 전투가 끝난 뒤 재생하며, 정상 종료되어야 캐릭터 해금이 확정되는 DialogueGraphSO입니다."),
                _dialogue,
                typeof(DialogueGraphSO),
                false);
            _postRecruitmentDialogue = (DialogueGraphSO)EditorGUILayout.ObjectField(
                FieldLabel(
                    "획득 후 대화 (선택)",
                    "캐릭터가 실제 파티에 해금된 뒤 같은 월드 액터와 이어서 재생할 대화입니다. 취소되면 완료 처리하지 않고 저장 후 다시 재생합니다."),
                _postRecruitmentDialogue,
                typeof(DialogueGraphSO),
                false);

            if (_definition == null)
                DrawNewDefinitionDraft();
            else
                DrawExistingDefinition();

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(_definition == null))
            {
                if (GUILayout.Button("정의 에셋 찾기"))
                    EditorGUIUtility.PingObject(_definition);
            }
            using (new EditorGUI.DisabledScope(_flowGraph == null))
            {
                if (GUILayout.Button("FlowGraph 열기"))
                    AssetDatabase.OpenAsset(_flowGraph);
                if (GUILayout.Button(new GUIContent(
                        "그래프 ID·대화 동기화",
                        "그래프 안 영입 조우 노드의 ID와 필수 대화 참조를 현재 값으로 맞춥니다. 그래프 구조는 바꾸지 않습니다.")))
                    SynchronizeGraphReferences();
            }
            EditorGUILayout.EndHorizontal();

            _showAssetPaths = EditorGUILayout.Foldout(_showAssetPaths, "신규 에셋 저장 위치", true);
            if (_showAssetPaths)
            {
                EditorGUI.indentLevel++;
                _storyFolder = EditorGUILayout.TextField(
                    FieldLabel("조우 정의 저장 폴더", "새 RecruitmentEncounterDefinitionSO를 저장할 Assets/ 아래 폴더입니다."),
                    _storyFolder);
                _flowFolder = EditorGUILayout.TextField(
                    FieldLabel("흐름 그래프 저장 폴더", "새 FlowGraphSO를 저장할 Assets/ 아래 폴더입니다."),
                    _flowFolder);
                EditorGUI.indentLevel--;
            }
        }

        private void DrawNewDefinitionDraft()
        {
            _combatMode = DrawCombatModePopup(_combatMode);
            if (_combatMode == RecruitmentEncounterCombatMode.HostileRecruitTarget)
                _incapacitationRule = DrawIncapacitationRulePopup(_incapacitationRule);
            _encounterId = EditorGUILayout.TextField(
                FieldLabel(
                    "조우 저장 ID",
                    "저장/로드에서 이 조우를 구분하는 고유 ID입니다. 출시 뒤에는 바꾸지 않습니다. 예: story.recruitment.komoe"),
                _encounterId);
            _prerequisiteEncounterId = EditorGUILayout.TextField(
                FieldLabel(
                    "선행 조우 ID (선택)",
                    "이 ID의 영입 조우가 완료된 뒤에만 진입을 엽니다. 먼저 도착하거나 범위 안에서 로드해도 완료 직후 자동으로 열립니다."),
                _prerequisiteEncounterId);
            _recruitCharacter = (CharacterActorType)EditorGUILayout.EnumPopup(
                FieldLabel(
                    "해금할 캐릭터",
                    "확정 대화를 완료했을 때 플레이어블로 해금할 CharacterActorType입니다."),
                _recruitCharacter);
            if (_combatMode == RecruitmentEncounterCombatMode.CooperativeBattle)
            {
                _allyFaction = (CombatFactionSO)EditorGUILayout.ObjectField(
                    FieldLabel(
                        "공동 전투 아군 진영",
                        "전투 중 영입 대상에게 임시로 적용할 진영입니다. 플레이어와 Ally, 적 참가자와 Hostile 관계여야 합니다."),
                    _allyFaction,
                    typeof(CombatFactionSO),
                    false);
            }
            if (_combatMode == RecruitmentEncounterCombatMode.CooperativeBattle)
                _allyFailurePolicy = DrawAllyFailurePolicyPopup(_allyFailurePolicy);
            _resetScope = DrawResetScopePopup(_resetScope);
            _postCombatSettleSeconds = EditorGUILayout.FloatField(
                FieldLabel(
                    "전투 종료 확인 시간 (초)",
                    "마지막 적 처치 직후의 사망·피격 처리가 끝날 때까지 기다린 뒤 대화 준비로 넘어가는 시간입니다."),
                Mathf.Max(0f, _postCombatSettleSeconds));
            _dialogueApproachDistance = EditorGUILayout.FloatField(
                FieldLabel(
                    "대화 접근 거리",
                    "0보다 크면 마지막 전투가 끝난 뒤 영입 대상이 플레이어의 이 거리까지 직접 이동합니다."),
                Mathf.Max(0f, _dialogueApproachDistance));
            _dialogueApproachSpeedMultiplier = EditorGUILayout.FloatField(
                FieldLabel(
                    "대화 접근 속도 배율",
                    "영입 대상의 달리기 속도에 곱할 연출 이동 배율입니다."),
                Mathf.Max(0.1f, _dialogueApproachSpeedMultiplier));
            _dialogueApproachTimeoutSeconds = EditorGUILayout.FloatField(
                FieldLabel(
                    "대화 접근 제한 시간 (초)",
                    "길이 막혀도 대화 흐름이 영구 정지하지 않도록 접근을 기다리는 최대 시간입니다."),
                Mathf.Max(0.1f, _dialogueApproachTimeoutSeconds));

            if (_showFieldHelp)
            {
                EditorGUILayout.HelpBox(
                    GetEncounterResolutionDescription()
                    + "\n"
                    + $"진행 초기화: {GetResetScopeDescription(_resetScope)}",
                    MessageType.None);
            }

            if (GUILayout.Button(new GUIContent(
                    "비어 있는 저장 ID 자동 채우기 (권장)",
                    "조우·진입 볼륨·참가자의 비어 있는 ID만 현재 캐릭터와 ActorId를 기준으로 채웁니다. 기존 ID는 바꾸지 않습니다.")))
                SuggestStableIds();
        }

        private void DrawExistingDefinition()
        {
            var serializedDefinition = new SerializedObject(_definition);
            serializedDefinition.Update();

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(
                    serializedDefinition.FindProperty("_encounterId"),
                    FieldLabel("조우 저장 ID", "저장 키이므로 기존 조우에서는 변경할 수 없습니다."));
            }
            EditorGUILayout.PropertyField(
                serializedDefinition.FindProperty("_prerequisiteEncounterId"),
                FieldLabel("선행 조우 ID (선택)", "지정한 영입 조우가 완료된 뒤 이 조우의 진입을 엽니다."));
            EditorGUILayout.PropertyField(
                serializedDefinition.FindProperty("_combatMode"),
                FieldLabel("조우 방식", "영입 대상과 직접 싸울지, 같은 편으로 함께 싸울지 결정합니다."));
            if (_definition.CombatMode == RecruitmentEncounterCombatMode.HostileRecruitTarget)
            {
                SerializedProperty incapacitationRule = serializedDefinition.FindProperty(
                    "_incapacitationRule");
                incapacitationRule.enumValueIndex = (int)DrawIncapacitationRulePopup(
                    (RecruitmentIncapacitationRule)incapacitationRule.enumValueIndex);
            }
            EditorGUILayout.PropertyField(
                serializedDefinition.FindProperty("_recruitCharacter"),
                FieldLabel("해금할 캐릭터", "확정 대화 완료 시 플레이어블로 해금할 캐릭터입니다."));
            if (_definition.CombatMode == RecruitmentEncounterCombatMode.CooperativeBattle)
            {
                EditorGUILayout.PropertyField(
                    serializedDefinition.FindProperty("_allyFaction"),
                    FieldLabel("공동 전투 아군 진영", "전투 중 영입 대상에게 임시로 적용할 진영입니다."));
            }

            if (_definition.CombatMode == RecruitmentEncounterCombatMode.CooperativeBattle)
            {
                SerializedProperty allyFailurePolicy = serializedDefinition.FindProperty("_allyFailurePolicy");
                allyFailurePolicy.enumValueIndex = (int)DrawAllyFailurePolicyPopup(
                    (RecruitmentAllyFailurePolicy)allyFailurePolicy.enumValueIndex);
            }
            SerializedProperty resetScope = serializedDefinition.FindProperty("_resetScope");
            resetScope.enumValueIndex = (int)DrawResetScopePopup(
                (RecruitmentEncounterResetScope)resetScope.enumValueIndex);
            EditorGUILayout.PropertyField(
                serializedDefinition.FindProperty("_noticeRadius"),
                FieldLabel(
                    "목격 반경",
                    "이 거리 안에서 참가자가 화면에 잡히면 목격으로 보고 대치 장면을 세웁니다. 0이면 목격 판정을 쓰지 않고 진입 볼륨만 사용합니다."));
            EditorGUILayout.PropertyField(
                serializedDefinition.FindProperty("_requireLineOfSight"),
                FieldLabel("시선 검사 사용", "목격 판정에 시선 차단 검사를 요구합니다. 끄면 벽 너머로도 화면에 잡히면 목격으로 봅니다."));
            EditorGUILayout.PropertyField(
                serializedDefinition.FindProperty("_noticeObstacleLayer"),
                FieldLabel("시야 차단 레이어", "시선을 가로막는 것으로 볼 레이어입니다. 비워 두면 차단 검사를 건너뜁니다."));
            EditorGUILayout.PropertyField(
                serializedDefinition.FindProperty("_commitRadius"),
                FieldLabel("개입 거리", "목격한 뒤 플레이어가 이 거리까지 다가오면 전투를 시작합니다. 0이면 거리로 시작하지 않습니다."));
            EditorGUILayout.PropertyField(
                serializedDefinition.FindProperty("_requireNoticeBeforeCommit"),
                FieldLabel(
                    "개입 전 목격 필요",
                    "끄면 목격 없이 개입 거리만으로 전투를 시작합니다. 반드시 발생해야 하는 스토리 조우에 사용합니다."));
            EditorGUILayout.PropertyField(
                serializedDefinition.FindProperty("_entryRevealTransition"),
                FieldLabel("등장 전환", "참가자가 화면 안에서 등장할 때 그 순간을 가릴 전환입니다. None이면 가리지 않습니다."));
            EditorGUILayout.PropertyField(
                serializedDefinition.FindProperty("_entryRevealCoverSeconds"),
                FieldLabel("등장 덮기 시간 (초)", "등장을 가리기 위해 화면을 덮는 데 걸리는 시간입니다."));
            EditorGUILayout.PropertyField(
                serializedDefinition.FindProperty("_entryRevealHoldSeconds"),
                FieldLabel("등장 유지 시간 (초)", "완전히 덮인 상태를 유지하며 참가자 배치를 끝내는 시간입니다."));
            EditorGUILayout.PropertyField(
                serializedDefinition.FindProperty("_entryRevealSeconds"),
                FieldLabel("등장 걷기 시간 (초)", "덮은 화면을 다시 걷어내는 시간입니다."));
            EditorGUILayout.PropertyField(
                serializedDefinition.FindProperty("_entryStandoffSeconds"),
                FieldLabel("등장 후 대치 시간 (초)", "등장과 전투 시작이 같은 프레임에 겹치지 않도록 진입 볼륨을 여는 것을 늦추는 시간입니다."));
            EditorGUILayout.PropertyField(
                serializedDefinition.FindProperty("_postCombatSettleSeconds"),
                FieldLabel("전투 종료 확인 시간 (초)", "마지막 전투 처리가 끝난 뒤 대화를 준비하기 전까지 기다리는 시간입니다."));
            EditorGUILayout.PropertyField(
                serializedDefinition.FindProperty("_dialogueApproachDistance"),
                FieldLabel("대화 접근 거리", "0보다 크면 영입 대상이 플레이어에게 직접 다가온 뒤 대화를 시작합니다."));
            EditorGUILayout.PropertyField(
                serializedDefinition.FindProperty("_dialogueApproachSpeedMultiplier"),
                FieldLabel("대화 접근 속도 배율", "영입 대상의 달리기 속도에 적용할 배율입니다."));
            EditorGUILayout.PropertyField(
                serializedDefinition.FindProperty("_dialogueApproachTimeoutSeconds"),
                FieldLabel("대화 접근 제한 시간 (초)", "길 막힘에도 진행 불능이 생기지 않도록 기다리는 최대 시간입니다."));
            if (serializedDefinition.ApplyModifiedProperties())
            {
                LoadDefinitionDraft(_definition);
                _issues.Clear();
            }

            if (_showFieldHelp)
            {
                EditorGUILayout.HelpBox(
                    GetEncounterResolutionDescription()
                    + "\n"
                    + $"진행 초기화: {GetResetScopeDescription(_resetScope)}",
                    MessageType.None);
            }

            EditorGUILayout.HelpBox(
                "encounterId는 저장 키이므로 기존 에셋에서는 이 창으로 변경할 수 없습니다.",
                MessageType.None);
        }

        private void DrawSceneSection()
        {
            DrawSectionTitle("2. 씬 바인딩과 참가자");
            if (_showFieldHelp)
            {
                EditorGUILayout.HelpBox(
                    _combatMode == RecruitmentEncounterCombatMode.HostileRecruitTarget
                        ? "Hierarchy에 배치된 영입 대상 MonsterActor를 지정하세요. 함께 덤빌 추가 적은 선택 사항입니다. 참가자는 조우가 열릴 때만 활성화됩니다."
                        : "Hierarchy에 배치된 영입 대상 MonsterActor와 함께 싸울 적을 지정하세요. 참가자는 조우가 열릴 때만 활성화됩니다.",
                    MessageType.None);
            }

            _showExistingSceneBindings = EditorGUILayout.Foldout(
                _showExistingSceneBindings,
                "기존 씬 구성 재사용 (선택)",
                true);
            if (_showExistingSceneBindings)
            {
                EditorGUI.indentLevel++;
                if (_showFieldHelp)
                {
                    EditorGUILayout.HelpBox(
                        "기존 조우를 수리하거나 이미 배치한 오브젝트를 재사용할 때만 지정합니다. "
                        + "신규 조우는 모두 비워 두면 자동 생성합니다.",
                        MessageType.None);
                }

                _sceneParent = (Transform)EditorGUILayout.ObjectField(
                    FieldLabel("생성 위치 부모", "새 조우 Root를 이 Transform 아래에 생성합니다. 비우면 씬 최상위에 생성합니다."),
                    _sceneParent,
                    typeof(Transform),
                    true);
                _flowRunner = (FlowGraphRunner)EditorGUILayout.ObjectField(
                    FieldLabel("기존 흐름 실행기", "재사용할 FlowGraphRunner입니다. 비우면 조우 Root에 생성합니다."),
                    _flowRunner,
                    typeof(FlowGraphRunner),
                    true);
                _entryVolume = (FlowGraphTriggerVolume)EditorGUILayout.ObjectField(
                    FieldLabel("기존 진입 볼륨", "재사용할 FlowGraphTriggerVolume입니다. 비우면 BoxCollider와 함께 생성합니다."),
                    _entryVolume,
                    typeof(FlowGraphTriggerVolume),
                    true);
                _hostileGroup = (MonsterGroupController)EditorGUILayout.ObjectField(
                    FieldLabel("기존 적 전술 그룹", "적의 그룹 전술을 담당할 MonsterGroupController입니다. 비우면 생성합니다."),
                    _hostileGroup,
                    typeof(MonsterGroupController),
                    true);
                _dialogueAnchor = (Transform)EditorGUILayout.ObjectField(
                    FieldLabel("기존 대화 위치", "전투 종료 후 영입 대상이 이동할 위치입니다. 비우면 대상의 현재 위치에 생성합니다."),
                    _dialogueAnchor,
                    typeof(Transform),
                    true);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(3f);
            _snapParticipantsToGround = EditorGUILayout.Toggle(
                FieldLabel(
                    "참가자 지면 스냅",
                    "씬 바인딩 시 참가자와 대화 위치를 발밑 지면 높이로 맞춥니다. "
                    + "경사지에서 참가자가 지면 아래에 묻힌 채 등장하는 것을 막습니다."),
                _snapParticipantsToGround);

            EditorGUILayout.Space(3f);
            EditorGUILayout.LabelField("플레이어 진입 범위", EditorStyles.boldLabel);
            _entryVolumeSize = EditorGUILayout.Vector3Field(
                FieldLabel("자동 생성 볼륨 크기", "플레이어가 들어오면 조우를 시작하는 BoxCollider의 가로·높이·세로 크기입니다."),
                _entryVolumeSize);
            _showAdvancedEntrySettings = EditorGUILayout.Foldout(
                _showAdvancedEntrySettings,
                "저장·복원 ID (고급)",
                true);
            if (_showAdvancedEntrySettings)
            {
                EditorGUI.indentLevel++;
                _resumeEntryId = EditorGUILayout.TextField(
                    FieldLabel("로드 재개 진입 ID", "저장된 전투/대화 단계를 복원할 때 실행할 FlowGraph Manual Entry ID입니다."),
                    _resumeEntryId);
                _entryVolumeId = EditorGUILayout.TextField(
                    FieldLabel("진입 볼륨 ID", "진입 볼륨과 FlowGraph 진입 노드를 연결하는 고유 ID입니다."),
                    _entryVolumeId);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(3f);
            EditorGUILayout.LabelField("영입 대상", EditorStyles.boldLabel);
            _allyActor = (MonsterActor)EditorGUILayout.ObjectField(
                FieldLabel(
                    "영입 대상 몬스터",
                    _combatMode == RecruitmentEncounterCombatMode.HostileRecruitTarget
                        ? "전투 전 대화 상대이자 플레이어가 쓰러뜨릴 적으로 등장할 씬의 MonsterActor입니다."
                        : "플레이어와 함께 싸우고 전투 종료 후 대화 상대가 될 씬의 MonsterActor입니다."),
                _allyActor,
                typeof(MonsterActor),
                true);
            _allyParticipantId = EditorGUILayout.TextField(
                FieldLabel(
                    "영입 대상 저장 ID",
                    "전투 상태와 대화 상대를 저장/로드에서 추적하는 ID입니다. ActorId를 기준으로 자동 채울 수 있습니다."),
                _allyParticipantId);

            EditorGUILayout.Space(3f);
            EditorGUILayout.LabelField(
                _combatMode == RecruitmentEncounterCombatMode.HostileRecruitTarget
                    ? $"추가 적 (선택, {_hostiles.Count})"
                    : $"함께 배치할 적 ({_hostiles.Count})",
                EditorStyles.boldLabel);
            if (_hostiles.Count > 0)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("씬의 적 MonsterActor", EditorStyles.miniLabel, GUILayout.MinWidth(180f));
                EditorGUILayout.LabelField("적 저장 ID", EditorStyles.miniLabel, GUILayout.MinWidth(160f));
                GUILayout.Space(30f);
                EditorGUILayout.EndHorizontal();
            }
            int removeIndex = -1;
            for (int i = 0; i < _hostiles.Count; i++)
            {
                HostileDraft hostile = _hostiles[i] ??= new HostileDraft();
                EditorGUILayout.BeginHorizontal();
                hostile.actor = (MonsterActor)EditorGUILayout.ObjectField(
                    hostile.actor,
                    typeof(MonsterActor),
                    true,
                    GUILayout.MinWidth(180f));
                hostile.participantId = EditorGUILayout.TextField(
                    hostile.participantId,
                    GUILayout.MinWidth(160f));
                if (GUILayout.Button("−", GUILayout.Width(26f)))
                    removeIndex = i;
                EditorGUILayout.EndHorizontal();
            }
            if (removeIndex >= 0)
                _hostiles.RemoveAt(removeIndex);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("적 슬롯 추가", "비어 있는 적 참가자 입력칸을 하나 추가합니다.")))
                _hostiles.Add(new HostileDraft());
            if (GUILayout.Button(new GUIContent("선택한 몬스터 추가", "Hierarchy에서 선택한 MonsterActor를 적 참가자로 추가합니다.")))
                AddSelectedHostiles();
            if (GUILayout.Button(new GUIContent("적 그룹에서 모두 가져오기", "지정한 적 그룹 아래의 모든 MonsterActor를 적 참가자로 추가합니다.")))
                ScanHostileGroupChildren();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawActionSection()
        {
            DrawSectionTitle("3. 생성·연결");
            if (_showFieldHelp)
            {
                EditorGUILayout.HelpBox(
                    "새 조우는 '신규 조우 전체 생성'을 사용하세요. "
                    + "아래의 두 분리 작업은 데이터나 씬 구성 중 한쪽만 다시 만들 때 사용합니다.",
                    MessageType.None);
            }

            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            {
                if (GUILayout.Button(
                        new GUIContent(
                            "신규 조우 전체 생성 (권장)",
                            "비어 있는 ID를 자동 채우고 조우 정의, 표준 FlowGraph, 씬 Anchor와 참가자 연결을 한 번에 생성합니다."),
                        GUILayout.Height(36f)))
                {
                    CreateCompleteScaffold();
                }

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(
                        new GUIContent(
                            "데이터 에셋만 생성",
                            "씬은 바꾸지 않고 누락된 조우 정의와 표준 FlowGraph만 생성합니다."),
                        GUILayout.Height(30f)))
                    CreateMissingAssets();
                if (GUILayout.Button(
                        new GUIContent(
                            "현재 씬 연결만 적용",
                            "선택된 기존 데이터 에셋을 사용해 Anchor, 볼륨과 참가자 연결만 생성하거나 갱신합니다."),
                        GUILayout.Height(30f)))
                    BuildSceneBinding();
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.HelpBox(
                "전체 생성은 기존 에셋을 삭제하거나 그래프를 덮어쓰지 않습니다. 새 에셋만 만들고 씬 변경은 하나의 Undo 그룹으로 적용합니다.",
                MessageType.None);

            if (!string.IsNullOrWhiteSpace(_statusMessage))
                EditorGUILayout.HelpBox(_statusMessage, _statusType);
        }

        private void DrawValidationSection()
        {
            DrawSectionTitle("4. 누락·진행 불능 검사");
            if (_showFieldHelp)
            {
                EditorGUILayout.HelpBox(
                    "생성 전에는 아직 입력하지 않은 필수 항목을 안내합니다. 생성 후에는 저장 ID 중복, "
                    + "진영 관계, 필수 대화 우회 경로와 씬 참조 누락까지 검사합니다.",
                    MessageType.None);
            }
            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(_anchor == null))
            {
                if (GUILayout.Button("현재 조우 다시 검사"))
                    RefreshValidation(includeProjectIdScan: true);
            }
            if (GUILayout.Button("선택 대상 찾기"))
                SelectCurrentTarget();
            EditorGUILayout.EndHorizontal();

            if (_anchor == null)
            {
                SuggestStableIds();
                List<string> draftErrors = CollectDraftErrors(requireAssets: false);
                if (draftErrors.Count == 0)
                    EditorGUILayout.HelpBox("초안 필수 입력이 준비됐습니다.", MessageType.Info);
                else
                {
                    for (int i = 0; i < draftErrors.Count; i++)
                        EditorGUILayout.HelpBox(draftErrors[i], MessageType.Warning);
                }
                return;
            }

            if (_issues.Count == 0)
                RefreshValidation(includeProjectIdScan: false);

            for (int i = 0; i < _issues.Count; i++)
                DrawIssue(_issues[i]);
        }

        private static void DrawIssue(RecruitmentEncounterAuthoringIssue issue)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.HelpBox(issue.Message, ToMessageType(issue.Severity));
            using (new EditorGUI.DisabledScope(issue.Context == null))
            {
                if (GUILayout.Button("선택", GUILayout.Width(48f), GUILayout.Height(38f)))
                {
                    Selection.activeObject = issue.Context;
                    EditorGUIUtility.PingObject(issue.Context);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void LoadAnchor(RecruitmentEncounterAnchor anchor)
        {
            _anchor = anchor;
            _issues.Clear();
            if (anchor == null)
                return;

            _showExistingSceneBindings = true;
            _showAdvancedEntrySettings = true;

            var serializedAnchor = new SerializedObject(anchor);
            _definition = ReadReference<RecruitmentEncounterDefinitionSO>(serializedAnchor, "_definition");
            _flowRunner = ReadReference<FlowGraphRunner>(serializedAnchor, "_flowRunner");
            _entryVolume = ReadReference<FlowGraphTriggerVolume>(serializedAnchor, "_entryVolume");
            _allyActor = ReadReference<MonsterActor>(serializedAnchor, "_allyActor");
            _hostileGroup = ReadReference<MonsterGroupController>(serializedAnchor, "_hostileGroup");
            _dialogueAnchor = ReadReference<Transform>(serializedAnchor, "_dialogueAnchor");
            _resumeEntryId = serializedAnchor.FindProperty("_resumeEntryId")?.stringValue
                             ?? DefaultResumeEntryId;
            _flowGraph = _flowRunner != null ? _flowRunner.Graph : null;
            _sceneParent = anchor.transform.parent;

            LoadDefinitionDraft(_definition);
            LoadVolumeDraft();
            LoadParticipantDrafts(serializedAnchor.FindProperty("_participants"));
            LoadDialogueFromGraph();
            RefreshValidation(includeProjectIdScan: false);
        }

        private void LoadDefinition(RecruitmentEncounterDefinitionSO definition)
        {
            _definition = definition;
            LoadDefinitionDraft(definition);
            _issues.Clear();
        }

        private void LoadDefinitionDraft(RecruitmentEncounterDefinitionSO definition)
        {
            if (definition == null)
                return;

            _encounterId = definition.EncounterId;
            _combatMode = definition.CombatMode;
            _incapacitationRule = definition.IncapacitationRule;
            _recruitCharacter = definition.RecruitCharacter;
            _allyFaction = definition.AllyFaction;
            _allyFailurePolicy = definition.AllyFailurePolicy;
            _resetScope = definition.ResetScope;
            _postCombatSettleSeconds = definition.PostCombatSettleSeconds;
            _dialogueApproachDistance = definition.DialogueApproachDistance;
            _dialogueApproachSpeedMultiplier = definition.DialogueApproachSpeedMultiplier;
            _dialogueApproachTimeoutSeconds = definition.DialogueApproachTimeoutSeconds;
            _prerequisiteEncounterId = definition.PrerequisiteEncounterId;
            if (string.IsNullOrWhiteSpace(_entryVolumeId))
                _entryVolumeId = $"{_encounterId}.entry";
        }

        private void LoadParticipantDrafts(SerializedProperty participants)
        {
            _hostiles.Clear();
            _allyParticipantId = null;
            if (participants == null || !participants.isArray)
                return;

            for (int i = 0; i < participants.arraySize; i++)
            {
                RecruitmentEncounterParticipant participant = participants
                    .GetArrayElementAtIndex(i)
                    .objectReferenceValue as RecruitmentEncounterParticipant;
                if (participant == null)
                    continue;

                if (participant.Role is RecruitmentEncounterRole.RequiredAlly
                    or RecruitmentEncounterRole.RecruitTarget)
                {
                    _allyActor = participant.Actor;
                    _allyParticipantId = participant.ParticipantId;
                }
                else
                {
                    _hostiles.Add(new HostileDraft
                    {
                        actor = participant.Actor,
                        participantId = participant.ParticipantId,
                    });
                }
            }
        }

        private void LoadVolumeDraft()
        {
            if (_entryVolume == null)
                return;

            var serializedVolume = new SerializedObject(_entryVolume);
            _entryVolumeId = serializedVolume.FindProperty("_volumeId")?.stringValue;
            Collider volumeCollider = serializedVolume.FindProperty("_volumeCollider")?.objectReferenceValue
                                      as Collider;
            volumeCollider ??= _entryVolume.GetComponent<Collider>();
            if (volumeCollider is BoxCollider box)
                _entryVolumeSize = box.size;
        }

        private void LoadDialogueFromGraph()
        {
            if (_flowGraph == null)
                return;

            for (int i = 0; i < _flowGraph.nodes.Count; i++)
            {
                if (_flowGraph.nodes[i] is PlayDialogueRequiredNode dialogue
                    && (string.IsNullOrWhiteSpace(_encounterId)
                        || string.Equals(dialogue.encounterId, _encounterId, StringComparison.Ordinal)))
                {
                    _dialogue = dialogue.dialogue;
                }
                else if (_flowGraph.nodes[i] is PlayRecruitmentPostDialogueNode postDialogue
                         && (string.IsNullOrWhiteSpace(_encounterId)
                             || string.Equals(
                                 postDialogue.encounterId,
                                 _encounterId,
                                 StringComparison.Ordinal)))
                {
                    _postRecruitmentDialogue = postDialogue.dialogue;
                }
            }
        }

        private void ResetDraft()
        {
            _anchor = null;
            _definition = null;
            _flowGraph = null;
            _dialogue = null;
            _postRecruitmentDialogue = null;
            _flowRunner = null;
            _entryVolume = null;
            _allyActor = null;
            _hostileGroup = null;
            _dialogueAnchor = null;
            _encounterId = null;
            _prerequisiteEncounterId = null;
            _recruitCharacter = CharacterActorType.None;
            _combatMode = RecruitmentEncounterCombatMode.HostileRecruitTarget;
            _incapacitationRule = RecruitmentIncapacitationRule.FinishAttack;
            _allyFaction = FindFactionById(CombatFactionRules.PlayerPartyId);
            _allyFailurePolicy = RecruitmentAllyFailurePolicy.Incapacitate;
            _resetScope = RecruitmentEncounterResetScope.PersistUntilNewGame;
            _postCombatSettleSeconds = 1.25f;
            _dialogueApproachDistance = 2.8f;
            _dialogueApproachSpeedMultiplier = 0.65f;
            _dialogueApproachTimeoutSeconds = 6f;
            _resumeEntryId = DefaultResumeEntryId;
            _entryVolumeId = null;
            _entryVolumeSize = new Vector3(10f, 3f, 10f);
            _snapParticipantsToGround = true;
            _allyParticipantId = null;
            _hostiles.Clear();
            _showExistingSceneBindings = false;
            _showAdvancedEntrySettings = false;
            _issues.Clear();
            SetStatus("새 영입 조우 초안을 시작했습니다.", MessageType.Info);
        }

        private void TryLoadSelectedAnchor(bool showMessage = false)
        {
            RecruitmentEncounterAnchor selected = Selection.activeGameObject != null
                ? Selection.activeGameObject.GetComponentInParent<RecruitmentEncounterAnchor>(true)
                : null;
            if (selected != null)
            {
                LoadAnchor(selected);
                if (showMessage)
                    SetStatus($"'{selected.EncounterId}' 조우를 불러왔습니다.", MessageType.Info);
                return;
            }

            if (showMessage)
                SetStatus("현재 선택에서 RecruitmentEncounterAnchor를 찾지 못했습니다.", MessageType.Warning);
        }

        private void RefreshValidation(bool includeProjectIdScan)
        {
            RecruitmentEncounterAuthoringValidator.ValidateAnchor(
                _anchor,
                _issues,
                includeProjectIdScan);
            Repaint();
        }

        private void SelectCurrentTarget()
        {
            UnityEngine.Object target = _anchor != null
                ? _anchor
                : _definition != null
                    ? _definition
                    : _flowGraph;
            if (target == null)
                return;

            Selection.activeObject = target;
            EditorGUIUtility.PingObject(target);
        }

        private void SetStatus(string message, MessageType type)
        {
            _statusMessage = message;
            _statusType = type;
            Repaint();
        }

        private static T ReadReference<T>(SerializedObject serializedObject, string propertyName)
            where T : UnityEngine.Object =>
            serializedObject.FindProperty(propertyName)?.objectReferenceValue as T;

        private static RecruitmentAllyFailurePolicy DrawAllyFailurePolicyPopup(
            RecruitmentAllyFailurePolicy current)
        {
            int currentIndex = Array.IndexOf(s_allyFailurePolicyValues, current);
            int selectedIndex = EditorGUILayout.Popup(
                FieldLabel(
                    "영입 대상 치명타 처리",
                    "전투 중 영입 대상의 체력이 모두 소진됐을 때 사망 대신 적용할 처리입니다."),
                Mathf.Max(0, currentIndex),
                s_allyFailurePolicyOptions);
            return s_allyFailurePolicyValues[Mathf.Clamp(
                selectedIndex,
                0,
                s_allyFailurePolicyValues.Length - 1)];
        }

        private static RecruitmentEncounterCombatMode DrawCombatModePopup(
            RecruitmentEncounterCombatMode current)
        {
            int currentIndex = Array.IndexOf(s_combatModeValues, current);
            int selectedIndex = EditorGUILayout.Popup(
                FieldLabel(
                    "조우 방식",
                    "영입 대상이 적으로 등장해 플레이어와 싸울지, 같은 편으로 공동 전투를 할지 결정합니다."),
                Mathf.Max(0, currentIndex),
                s_combatModeOptions);
            return s_combatModeValues[Mathf.Clamp(
                selectedIndex,
                0,
                s_combatModeValues.Length - 1)];
        }

        private static RecruitmentIncapacitationRule DrawIncapacitationRulePopup(
            RecruitmentIncapacitationRule current)
        {
            int currentIndex = Array.IndexOf(s_incapacitationRuleValues, current);
            int selectedIndex = EditorGUILayout.Popup(
                FieldLabel(
                    "영입 대상 제압 조건",
                    "적대 영입 대상의 체력 소진을 어떤 플레이어 행동으로 확정할지 결정합니다."),
                Mathf.Max(0, currentIndex),
                s_incapacitationRuleOptions);
            return s_incapacitationRuleValues[Mathf.Clamp(
                selectedIndex,
                0,
                s_incapacitationRuleValues.Length - 1)];
        }

        private static RecruitmentEncounterResetScope DrawResetScopePopup(
            RecruitmentEncounterResetScope current)
        {
            int currentIndex = Array.IndexOf(s_resetScopeValues, current);
            int selectedIndex = EditorGUILayout.Popup(
                FieldLabel(
                    "진행 초기화 시점",
                    "저장된 조우 진행을 새 게임까지 유지할지, 새 사이클마다 다시 시작할지 결정합니다."),
                Mathf.Max(0, currentIndex),
                s_resetScopeOptions);
            return s_resetScopeValues[Mathf.Clamp(
                selectedIndex,
                0,
                s_resetScopeValues.Length - 1)];
        }

        private static string GetAllyFailurePolicyDescription(
            RecruitmentAllyFailurePolicy policy) =>
            policy switch
            {
                RecruitmentAllyFailurePolicy.Incapacitate =>
                    "체력이 소진돼도 사망하지 않고 전투 불능 상태로 남습니다.",
                _ => "현재 도구가 설명하지 못하는 정책입니다.",
            };

        private string GetEncounterResolutionDescription()
        {
            return _combatMode == RecruitmentEncounterCombatMode.HostileRecruitTarget
                ? $"영입 대상 제압: {GetIncapacitationRuleDescription(_incapacitationRule)}"
                : $"영입 대상 전투불능: {GetAllyFailurePolicyDescription(_allyFailurePolicy)}";
        }

        private static string GetIncapacitationRuleDescription(
            RecruitmentIncapacitationRule rule) =>
            rule switch
            {
                RecruitmentIncapacitationRule.FinishAttack =>
                    "체력 소진 뒤 브레이크 노출 상태에서 피니시 공격을 성공시켜야 합니다.",
                RecruitmentIncapacitationRule.AnyFatalDamage =>
                    "체력을 소진한 공격을 즉시 제압으로 처리합니다.",
                _ => "현재 도구가 설명하지 못하는 제압 조건입니다.",
            };

        private static string GetResetScopeDescription(RecruitmentEncounterResetScope scope) =>
            scope switch
            {
                RecruitmentEncounterResetScope.PersistUntilNewGame =>
                    "완료 여부와 전투 진행을 새 게임 전까지 유지합니다.",
                RecruitmentEncounterResetScope.ResetOnCycle =>
                    "새 사이클이 시작되면 조우를 처음 상태로 되돌립니다.",
                _ => "현재 도구가 설명하지 못하는 초기화 범위입니다.",
            };

        private static GUIContent FieldLabel(string text, string tooltip) =>
            new(text, tooltip);

        private static MessageType ToMessageType(RecruitmentEncounterIssueSeverity severity) =>
            severity switch
            {
                RecruitmentEncounterIssueSeverity.Error => MessageType.Error,
                RecruitmentEncounterIssueSeverity.Warning => MessageType.Warning,
                _ => MessageType.Info,
            };

        private static void DrawSectionTitle(string title)
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            Rect line = EditorGUILayout.GetControlRect(false, 1f);
            EditorGUI.DrawRect(line, EditorGUIUtility.isProSkin
                ? new Color(0.28f, 0.28f, 0.28f)
                : new Color(0.65f, 0.65f, 0.65f));
            EditorGUILayout.Space(3f);
        }
    }
}
