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
        public Sprite portrait;
        [Range(0.01f, 0.2f)] public float typingSpeed = 0.04f;
        [Tooltip("타이핑 완료 후 자동 진행까지 대기 시간(초). 0이면 입력 대기.")]
        [Min(0f)] public float autoAdvanceDuration = 0f;

        [Header("Routing")]
        public string nextNodeId;       // Talk, Event
        public string trueNextNodeId;   // Condition 참
        public string falseNextNodeId;  // Condition 거짓
        public List<ChoiceData> choices = new();

        [Header("Condition")]
        public ConditionSO condition;

        [Header("Events")]
        public List<DialogueActionSO> eventActions = new();

        [Header("Camera (Optional)")]
        [Tooltip("지정 시 이 노드에서 자동 추종 대신 사전 녹화 카메라를 화자 기준으로 재생한다. " +
                 "연속된 여러 노드가 같은 녹화를 가리키면 처음부터 재시작하지 않고 한 번에 이어서 재생된다(장면 단위 연출). " +
                 "완료 후 마지막 프레임을 유지하다가 다음 노드가 카메라를 교체한다. Main 채널에서만 동작.")]
        public UPlayGround.Data.DialogueCameraRecordingSO cameraRecording;

        [Tooltip("이 라인의 구도. Auto면 자동 디렉터가 결정한다(기본: 화자 OTS, 화자가 바뀌면 리버스 샷).")]
        public UPlayGround.Data.DialogueShotType shotType = UPlayGround.Data.DialogueShotType.Auto;

        [Tooltip("이전 샷에서 넘어오는 방식. Auto면 대상 변경=Cut, 동일 대상=Blend, 대화 진입=Establish.")]
        public UPlayGround.Data.DialogueShotTransition shotTransition = UPlayGround.Data.DialogueShotTransition.Auto;

        [Tooltip("비우지 않으면 화자가 말하는 동안 이 speakerId의 인물을 잡는 리액션 샷이 된다.")]
        public string reactionSpeakerId;

        [Tooltip("0보다 크면 이 라인의 카메라 거리(m)를 프리셋 대신 사용한다.")]
        [Min(0f)] public float shotDistanceOverride = 0f;

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
