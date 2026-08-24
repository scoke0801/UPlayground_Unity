using System;
using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Dialogue
{
    public enum NodeType { Talk, Choice, Condition, Event, End }

    /// <summary>
    /// 어떤 UI 채널로 출력할지 결정.
    /// Main = 캐릭터 대화, System = 게임 알림, Monologue = 주인공 독백
    /// </summary>
    public enum DialogueChannel { Main, System, Monologue }

    /// <summary>대사 본문을 일반 대화, 시네마틱 자막, 지역 타이틀 중 어떤 형태로 표시할지 정한다.</summary>
    public enum DialogueTextPresentation
    {
        Standard,
        CinematicNarration,
        CinematicLocationTitle
    }

    [Serializable]
    public class ChoiceData
    {
        public string choiceText;
        public string nextNodeId;
        [Tooltip("null이면 항상 표시")]
        public ConditionSO displayCondition;
        [Tooltip("조건 미충족 시 숨김 대신 비활성화 표시")]
        public bool isGreyedOut;
    }

    // 노드 하나 = 대화 그래프의 상태 하나
    [CreateAssetMenu(menuName = "UPlayGround/대화/Node", fileName = "Node_")]
    public class DialogueNodeSO : ScriptableObject
    {
        [HideInInspector] public string nodeId;
        public NodeType nodeType;
        public DialogueChannel channel = DialogueChannel.Main;

        [Header("Talk / Choice")]
        public string speakerId;
        [TextArea(2, 5)] public string dialogueText;
        [Tooltip("비워두면 SpeakerPortraitTable의 화자 기본 초상화를 사용한다. 이 대사만 다른 표정을 쓸 때만 지정한다.")]
        public Sprite portrait;
        [Range(0.01f, 0.2f)] public float typingSpeed = 0.04f;
        [Tooltip("타이핑 완료 후 자동 진행까지 대기 시간(초). 0이면 입력 대기.")]
        [Min(0f)] public float autoAdvanceDuration = 0f;
        [Tooltip("시네마틱 삽화 위에서만 사용한다. 일반 대사는 Standard를 유지한다.")]
        public DialogueTextPresentation textPresentation = DialogueTextPresentation.Standard;

        [Header("Routing")]
        public string nextNodeId;       // Talk, Event
        public string trueNextNodeId;   // Condition 참
        public string falseNextNodeId;  // Condition 거짓
        public List<ChoiceData> choices = new();

        [Header("Condition")]
        public ConditionSO condition;

        [Header("Events")]
        public List<DialogueActionSO> eventActions = new();

        [Header("Motion (Optional)")]
        [Tooltip("이 라인에서 화자가 취할 제스처의 카탈로그 ID. 비우면 아래 카테고리에서 랜덤으로 뽑는다.")]
        public string speakerMotionId;

        [Tooltip("화자 제스처를 지정하지 않았을 때 랜덤 추출에 쓸 정서 분류.")]
        public DialogueMotionCategory speakerMotionCategory = DialogueMotionCategory.Neutral;

        [Tooltip("이 라인에서 청자가 취할 제스처의 카탈로그 ID. 비우면 아래 카테고리에서 랜덤으로 뽑는다.")]
        public string listenerMotionId;

        [Tooltip("청자 제스처를 지정하지 않았을 때 랜덤 추출에 쓸 정서 분류.")]
        public DialogueMotionCategory listenerMotionCategory = DialogueMotionCategory.Neutral;

        [Header("Camera (Optional)")]
        [Tooltip("지정 시 이 노드에서 자동 추종 대신 사전 녹화 카메라를 화자 기준으로 재생한다. " +
                 "연속된 여러 노드가 같은 녹화를 가리키면 처음부터 재시작하지 않고 한 번에 이어서 재생된다(장면 단위 연출). " +
                 "완료 후 마지막 프레임을 유지하다가 다음 노드가 카메라를 교체한다. Main 채널에서만 동작.")]
        public UPlayGround.Data.DialogueCameraRecordingSO cameraRecording;

        [Tooltip("이 라인의 구도. Auto면 자동 디렉터가 결정한다(기본: 화자 OTS, 화자가 바뀌면 리버스 샷).")]
        public UPlayGround.Data.DialogueShotType shotType = UPlayGround.Data.DialogueShotType.Auto;

        [Tooltip("이전 샷에서 넘어오는 방식. Auto면 대상 변경=Cut, 동일 대상=Blend, 대화 진입=Establish.")]
        public UPlayGround.Data.DialogueShotTransition shotTransition = UPlayGround.Data.DialogueShotTransition.Auto;

        [Tooltip("비우면 자동(화자가 플레이어면 마지막 비플레이어 화자, 아니면 플레이어). " +
                 "채우면 이 speakerId의 인물을 이 라인의 대화 상대로 삼아 가상선을 정의한다. " +
                 "NPC끼리 주고받는 라인에 사용한다.")]
        public string listenerSpeakerId;

        [Tooltip("비우지 않으면 화자가 말하는 동안 이 speakerId의 인물을 잡는 리액션 샷이 된다. " +
                 "listenerSpeakerId가 가상선을 정하고, 이 값은 그 축 위에서 누구를 잡을지를 정한다.")]
        public string reactionSpeakerId;

        [Tooltip("0보다 크면 이 라인의 카메라 거리(m)를 프리셋 대신 사용한다.")]
        [Min(0f)] public float shotDistanceOverride = 0f;

        [Header("Camera Focus (Optional)")]
        [Tooltip("이 라인 동안 잠시 잡아 보여줄 인물의 speakerId. " +
                 "\"저기 있는 저 아이\"처럼 대사가 제3의 인물을 가리킬 때 사용한다. " +
                 "비우면 주목 컷을 쓰지 않는다.")]
        public string focusSpeakerId;

        [Tooltip("주목 대상을 잡고 있을 시간(초). 0이면 focusSpeakerId를 채워도 주목 컷을 쓰지 않는다. " +
                 "이 시간이 지나면 카메라가 라인 구도로 돌아오고 대화는 그대로 이어진다.")]
        [Min(0f)] public float focusHoldSeconds = 0f;

        [Tooltip("주목 컷으로 넘어가기 전 라인 구도를 유지할 시간(초). " +
                 "0이면 라인 진입과 동시에 대상으로 넘어간다. 화자를 먼저 보여주고 싶을 때만 채운다.")]
        [Min(0f)] public float focusDelaySeconds = 0f;

        [Tooltip("주목 컷의 구도. Auto면 대상을 잡는 기본 구도(OTS)를 쓴다.")]
        public UPlayGround.Data.DialogueShotType focusShotType = UPlayGround.Data.DialogueShotType.Auto;

        // 에디터 전용 — 런타임에서 참조하지 않음
        [HideInInspector] public Vector2 editorPosition;

#if UNITY_EDITOR
        // 에디터에서 SO 생성 직후 자동 ID 부여
        public void AssignNewId()
        {
            if (string.IsNullOrEmpty(nodeId))
                nodeId = Guid.NewGuid().ToString();
        }
#endif
    }
}
