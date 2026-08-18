using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data;

namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// 대화 한 세션(그래프 1회 실행) 동안 유지되는 카메라 연출 상태.
    ///
    /// 카메라 모드(behavior) 인스턴스가 아니라 세션이 이 상태를 소유하는 이유:
    /// Dialogue ↔ DialogueCameraReplay 전환은 SetMode로 처리되어 모드의 OnExit/OnEnter가 매번 불린다.
    /// 인트로 소진 여부나 가상선을 모드가 들고 있으면 녹화 노드를 한 번 거칠 때마다 초기화되어
    /// 대화 도중 인트로가 재생되고 카메라 쪽이 뒤집힌다.
    ///
    /// 3인 이상 대화는 참여자 전원을 담되 가상선은 "현재 말이 오가는 두 사람"(활성 pair)에서 잡는다.
    /// 축은 pair마다 다시 잡히지만 카메라가 머무는 쪽은 세션이 고정한 <see cref="StageRight"/>가 결정하므로,
    /// 화자 조합이 바뀌어도 카메라는 그룹 기준 같은 반평면에 남는다.
    /// </summary>
    public sealed class DialogueShotSession
    {
        private const float AxisEpsilonSqr = 0.0001f;

        /// <summary>
        /// 축의 측면 벡터가 StageRight와 이 값보다 더 직교에 가까우면 SideSign을 갱신하지 않는다.
        /// 인물이 카메라 정면으로 일렬로 선 배치에서 부호가 노이즈로 뒤집히는 것을 막는다.
        /// </summary>
        private const float SideSignStabilityThreshold = 0.05f;

        private readonly List<Transform> _participants = new List<Transform>();

        /// <summary>이 대화에 등장하는 인물 전원. 등장 순서를 유지한다.</summary>
        public IReadOnlyList<Transform> Participants => _participants;

        /// <summary>현재 가상선의 한쪽 끝. 보통 이번 라인의 화자다.</summary>
        public Transform ActiveSubject { get; private set; }

        /// <summary>현재 가상선의 반대쪽 끝. 보통 이번 라인의 청자다.</summary>
        public Transform ActivePartner { get; private set; }

        /// <summary>가상선 확보 여부.</summary>
        public bool HasAxis { get; private set; }

        /// <summary>가상선 방향(활성 pair 기준, 수평).</summary>
        public Vector3 AxisForward { get; private set; } = Vector3.forward;

        /// <summary>가상선의 오른쪽 벡터. AxisForward에서 파생한다.</summary>
        public Vector3 AxisRight { get; private set; } = Vector3.right;

        /// <summary>카메라가 머무르는 가상선 쪽(+1/-1). StageRight에서 유도하며 직접 설정하지 않는다.</summary>
        public float SideSign { get; private set; } = 1f;

        /// <summary>
        /// 세션이 고정한 "카메라가 머무는 쪽"의 기준 벡터(수평, 정규화).
        /// 활성 pair가 바뀌어 축이 재정의되어도 이 벡터는 유지되며, 새 축의 SideSign은 항상 이것에서 유도된다.
        /// 축과 무관한 절대 기준이라야 축이 바뀌어도 "카메라가 서 있는 쪽"이라는 의미가 보존된다.
        /// </summary>
        public Vector3 StageRight { get; private set; } = Vector3.right;

        /// <summary>직전 활성 pair 변경으로 가상선이 회전한 각도(도). 확립 전환 판정에 쓰고 소진한다.</summary>
        public float LastAxisChangeAngle { get; set; }

        /// <summary>인트로 시퀀스를 이미 소비했는지. 세션당 1회.</summary>
        public bool IntroConsumed { get; set; }

        /// <summary>Main 채널에서 카메라를 갱신한 라인 수.</summary>
        public int LineIndex { get; set; }

        /// <summary>직전 샷이 주시하던 인물. 컷/블렌드 판정에 쓴다.</summary>
        public Transform LastSubject { get; set; }

        /// <summary>직전 라인의 화자. 짧은 라인 누적 판정에 쓴다.</summary>
        public Transform LastSpeaker { get; set; }

        /// <summary>직전에 결정된 샷 종류.</summary>
        public DialogueShotType LastShotType { get; set; } = DialogueShotType.Auto;

        /// <summary>연속된 짧은 라인 수.</summary>
        public int ConsecutiveShortLines { get; set; }

        /// <summary>가상선을 정의할 두 인물이 확보됐는지. 플레이어 참여 여부와 무관하다.</summary>
        public bool HasActivePair => ActiveSubject != null && ActivePartner != null;

        /// <summary>
        /// 세션을 시작한다. 참여자 목록의 앞 두 명이 초기 활성 pair가 되므로
        /// 호출측은 대화의 기본 축이 될 두 인물(보통 플레이어와 첫 상대)을 앞에 둔다.
        /// </summary>
        public void Begin(IReadOnlyList<Transform> participants, Vector3 cameraPosition)
        {
            _participants.Clear();
            ActiveSubject = null;
            ActivePartner = null;
            HasAxis = false;
            AxisForward = Vector3.forward;
            AxisRight = Vector3.right;
            SideSign = 1f;
            LastAxisChangeAngle = 0f;
            IntroConsumed = false;
            LineIndex = 0;
            LastSubject = null;
            LastSpeaker = null;
            LastShotType = DialogueShotType.Auto;
            ConsecutiveShortLines = 0;

            if (participants != null)
            {
                for (int i = 0; i < participants.Count; i++)
                    RegisterParticipant(participants[i]);
            }

            // 진입 시점에 카메라가 서 있는 쪽을 세션의 기준 반평면으로 채택한다
            // → 대화 첫 컷이 화면 좌우를 뒤집지 않고, 이후 축이 바뀌어도 이 쪽을 유지한다.
            Vector3 fromCenter = cameraPosition - Center;
            fromCenter.y = 0f;
            StageRight = fromCenter.sqrMagnitude > AxisEpsilonSqr
                ? fromCenter.normalized
                : Vector3.right;

            if (_participants.Count >= 2)
                SetActivePair(_participants[0], _participants[1]);
        }

        /// <summary>참여자를 등록한다. 이미 등록됐으면 아무 것도 하지 않는다.</summary>
        public void RegisterParticipant(Transform participant)
        {
            if (participant == null || _participants.Contains(participant))
                return;

            _participants.Add(participant);
        }

        /// <summary>
        /// 이번 라인의 가상선을 정의하는 두 인물을 설정한다.
        /// 축은 pair에서 다시 잡되 카메라 쪽은 StageRight가 결정하므로 반평면이 유지된다.
        /// 반환값은 가상선이 회전한 각도(도) — 확립 전환 판정에 쓴다.
        /// </summary>
        public float SetActivePair(Transform subject, Transform partner)
        {
            if (subject == null || partner == null || subject == partner)
                return 0f;

            RegisterParticipant(subject);
            RegisterParticipant(partner);

            if (ActiveSubject == subject && ActivePartner == partner)
                return 0f;

            // 화자와 청자가 자리만 바꾼 리버스 샷은 같은 가상선이다. 축을 뒤집어 확립 전환을 유발하면 안 된다.
            bool isReversedPair = ActiveSubject == partner && ActivePartner == subject;

            ActiveSubject = subject;
            ActivePartner = partner;

            if (isReversedPair)
                return 0f;

            Vector3 previousAxis = AxisForward;
            bool hadAxis = HasAxis;

            RecaptureAxis();

            LastAxisChangeAngle = hadAxis ? UndirectedAngle(previousAxis, AxisForward) : 0f;
            return LastAxisChangeAngle;
        }

        /// <summary>
        /// 두 가상선 사이의 각도(0~90). 가상선은 방향이 아니라 선이므로 뒤집힌 축은 같은 선으로 본다.
        /// 세 인물이 일렬로 선 배치에서 pair가 바뀌어도 선 자체는 그대로이므로 확립 전환을 유발하지 않는다.
        /// </summary>
        private static float UndirectedAngle(Vector3 a, Vector3 b)
        {
            float angle = Vector3.Angle(a, b);
            return angle > 90f ? 180f - angle : angle;
        }

        /// <summary>
        /// 인물이 대화 중 이동해 가상선이 크게 틀어졌으면 축만 다시 잡는다.
        /// 활성 pair가 바뀐 것이 아니므로 LastAxisChangeAngle은 건드리지 않는다 — 확립 전환 대상이 아니다.
        /// </summary>
        public void RefreshAxisIfDeviated(float recaptureAngleDegrees)
        {
            if (!HasAxis || !HasActivePair)
                return;

            Vector3 current = ActivePartner.position - ActiveSubject.position;
            current.y = 0f;
            if (current.sqrMagnitude < AxisEpsilonSqr)
                return;

            current.Normalize();
            if (UndirectedAngle(AxisForward, current) < recaptureAngleDegrees)
                return;

            RecaptureAxis();
        }

        /// <summary>참여자 전원의 무게중심(현재 위치 기준). 축 방향과 달리 매 프레임 따라간다.</summary>
        public Vector3 Center
        {
            get
            {
                Vector3 sum = Vector3.zero;
                int count = 0;

                for (int i = 0; i < _participants.Count; i++)
                {
                    Transform participant = _participants[i];
                    if (participant == null)
                        continue;

                    sum += participant.position;
                    count++;
                }

                if (count > 0)
                    return sum / count;

                if (ActiveSubject != null)
                    return ActiveSubject.position;

                return ActivePartner != null ? ActivePartner.position : Vector3.zero;
            }
        }

        /// <summary>활성 pair에서 가상선을 다시 계산한다. 카메라 쪽은 StageRight가 결정한다.</summary>
        private void RecaptureAxis()
        {
            if (!HasActivePair)
                return;

            Vector3 forward = ActivePartner.position - ActiveSubject.position;
            forward.y = 0f;
            if (forward.sqrMagnitude < AxisEpsilonSqr)
            {
                forward = ActivePartner.forward;
                forward.y = 0f;
                if (forward.sqrMagnitude < AxisEpsilonSqr)
                    forward = Vector3.forward;
            }

            AxisForward = forward.normalized;
            AxisRight = Vector3.Cross(Vector3.up, AxisForward).normalized;

            // 카메라 현재 위치가 아니라 세션 고정 StageRight에서 부호를 유도하는 것이 핵심이다.
            // 카메라 위치에서 매번 재추론하면 pair마다 반평면이 새로 정해져 그룹 공간감이 무너진다.
            float alignment = Vector3.Dot(AxisRight, StageRight);
            if (!HasAxis || Mathf.Abs(alignment) >= SideSignStabilityThreshold)
                SideSign = alignment >= 0f ? 1f : -1f;

            HasAxis = true;
        }
    }
}
