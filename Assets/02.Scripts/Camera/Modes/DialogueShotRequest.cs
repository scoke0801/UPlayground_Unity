using UnityEngine;
using UPlayGround.Data;

namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// 대화 노드 하나에 대한 카메라 요청.
    /// 대화 계층이 "무엇을 잡을지"만 기술하고, 실제 구도/전환 결정은 Director/Composer가 한다.
    /// </summary>
    public struct DialogueShotRequest
    {
        /// <summary>현재 말하는 인물.</summary>
        public Transform Speaker;

        /// <summary>대화 상대편. 화자가 플레이어면 NPC가, NPC면 플레이어가 들어온다.</summary>
        public Transform Listener;

        /// <summary>리액션 샷 대상. 지정되면 화자가 말하는 동안 이 인물을 잡는다.</summary>
        public Transform ReactionSubject;

        /// <summary>노드가 지정한 샷. Auto면 Director가 결정한다.</summary>
        public DialogueShotType ShotType;

        /// <summary>노드가 지정한 전환. Auto면 Director가 결정한다.</summary>
        public DialogueShotTransition Transition;

        /// <summary>0이 아니면 프리셋의 shoulderOffset을 대체한다.</summary>
        public Vector3 ShoulderOffsetOverride;

        /// <summary>0보다 크면 프리셋의 distance를 대체한다.</summary>
        public float DistanceOverride;

        /// <summary>선택지 제시 노드인지. 투샷 전환 판정에 쓴다.</summary>
        public bool IsChoicePhase;

        /// <summary>대사 길이(글자 수). 짧은 라인 컷 억제 판정에 쓴다.</summary>
        public int LineLength;

        /// <summary>
        /// 대화 세션 안에서의 노드 진입 순번. 같은 구도의 연속 라인도 별도 요청으로 구분한다.
        /// 0은 트리거·치트 등 세션 밖의 레거시 직접 호출에 예약한다.
        /// </summary>
        public int SequenceId;

        public bool HasShoulderOffsetOverride => ShoulderOffsetOverride.sqrMagnitude > 0.0001f;

        public static DialogueShotRequest FromTargets(Transform speaker, Transform listener, Vector3 offset)
        {
            return new DialogueShotRequest
            {
                Speaker = speaker,
                Listener = listener,
                ShotType = DialogueShotType.Auto,
                Transition = DialogueShotTransition.Auto,
                ShoulderOffsetOverride = offset
            };
        }

        /// <summary>같은 라인의 중복 진입인지 판정한다(모드 재진입 no-op 가드용).</summary>
        public bool Matches(in DialogueShotRequest other)
        {
            return Speaker == other.Speaker
                   && Listener == other.Listener
                   && ReactionSubject == other.ReactionSubject
                   && ShotType == other.ShotType
                   && Transition == other.Transition
                   && IsChoicePhase == other.IsChoicePhase
                   && LineLength == other.LineLength
                   && SequenceId == other.SequenceId
                   && Mathf.Approximately(DistanceOverride, other.DistanceOverride)
                   && ShoulderOffsetOverride == other.ShoulderOffsetOverride;
        }
    }
}
