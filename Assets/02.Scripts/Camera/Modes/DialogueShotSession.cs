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
    /// </summary>
    public sealed class DialogueShotSession
    {
        /// <summary>플레이어(대개 청자).</summary>
        public Transform Player { get; private set; }

        /// <summary>현재 대화 상대. 3인 이상 대화에서는 마지막으로 말한 비(非)플레이어가 된다.</summary>
        public Transform Partner { get; private set; }

        /// <summary>가상선 확보 여부.</summary>
        public bool HasAxis { get; private set; }

        /// <summary>가상선 방향(플레이어 → 상대, 수평). 세션 동안 고정한다.</summary>
        public Vector3 AxisForward { get; private set; } = Vector3.forward;

        /// <summary>가상선의 오른쪽 벡터. AxisForward에서 파생한다.</summary>
        public Vector3 AxisRight { get; private set; } = Vector3.right;

        /// <summary>카메라가 머무르는 가상선 쪽(+1/-1). 세션 동안 절대 뒤집지 않는다.</summary>
        public float SideSign { get; private set; } = 1f;

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

        public bool HasBothActors => Player != null && Partner != null;

        public void Reset(Transform player, Transform partner)
        {
            Player = player;
            Partner = partner;
            HasAxis = false;
            IntroConsumed = false;
            LineIndex = 0;
            LastSubject = null;
            LastSpeaker = null;
            LastShotType = DialogueShotType.Auto;
            ConsecutiveShortLines = 0;
        }

        /// <summary>
        /// 대화 상대를 교체한다(3인 이상 대화). 축은 새 상대 기준으로 다시 잡고 현재 카메라의
        /// 물리적 위치에서 새 SideSign을 구한다. 숫자 부호만 보존하면 축 반전 시 180° 선을 넘는다.
        /// 인트로 소진과 라인 카운터는 유지해 세션 연속성을 지킨다.
        /// </summary>
        public void SetPartner(Transform partner, Vector3 cameraPosition)
        {
            if (Partner == partner)
                return;

            Partner = partner;
            CaptureAxis(cameraPosition, preserveSide: false);
        }

        /// <summary>
        /// 가상선을 확보한다.
        /// preserveSide=false(최초 확보)면 현재 카메라가 서 있는 쪽을 그대로 채택해
        /// 대화 진입 컷이 화면 좌우를 뒤집지 않게 한다.
        /// </summary>
        public void CaptureAxis(Vector3 cameraPosition, bool preserveSide)
        {
            if (!HasBothActors)
                return;

            Vector3 forward = Partner.position - Player.position;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Partner.forward;
                forward.y = 0f;
                if (forward.sqrMagnitude < 0.0001f)
                    forward = Vector3.forward;
            }

            AxisForward = forward.normalized;
            AxisRight = Vector3.Cross(Vector3.up, AxisForward).normalized;

            if (!preserveSide || !HasAxis)
            {
                Vector3 center = Center;
                float side = Vector3.Dot(cameraPosition - center, AxisRight);
                SideSign = side >= 0f ? 1f : -1f;
            }

            HasAxis = true;
        }

        /// <summary>
        /// 인물이 대화 중 이동해 가상선이 크게 틀어졌으면 축만 다시 잡는다.
        /// 카메라 쪽(SideSign)은 유지하므로 시선 매칭은 깨지지 않는다.
        /// </summary>
        public void RefreshAxisIfDeviated(float recaptureAngleDegrees)
        {
            if (!HasAxis || !HasBothActors)
                return;

            Vector3 current = Partner.position - Player.position;
            current.y = 0f;
            if (current.sqrMagnitude < 0.0001f)
                return;

            current.Normalize();
            if (Vector3.Angle(AxisForward, current) < recaptureAngleDegrees)
                return;

            AxisForward = current;
            AxisRight = Vector3.Cross(Vector3.up, AxisForward).normalized;
        }

        /// <summary>두 인물의 중점(현재 위치 기준). 축 방향과 달리 매 프레임 따라간다.</summary>
        public Vector3 Center
        {
            get
            {
                if (Player == null)
                    return Partner != null ? Partner.position : Vector3.zero;
                if (Partner == null)
                    return Player.position;

                return (Player.position + Partner.position) * 0.5f;
            }
        }
    }
}
