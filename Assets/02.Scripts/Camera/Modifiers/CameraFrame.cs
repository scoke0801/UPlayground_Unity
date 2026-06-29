using UnityEngine;

namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// Modifier 파이프라인이 한 프레임 동안 공유·변형하는 작업 단위.
    /// 각 ICameraModifier는 이 프레임을 ref로 받아 State/Pose를 누적 변형한다.
    ///
    /// 설계 메모:
    /// - plain struct + ref 전달 사용 (ref struct 아님). Span 의존이 없어 ref struct의
    ///   제약(필드 불가/람다·이터레이터 교차 불가)만 늘 뿐 이득이 없다.
    /// - Effects를 프레임에 포함시켜 Modifier가 파이프라인 도중 effect 델타를 읽게 한다.
    ///   (쉐이크 yaw/pitch가 Follow 위치 계산 *이전*에 반영되어야 하므로 "끝에서 일괄 합성"은
    ///    현행 거동을 재현하지 못한다.) 자세한 effect→슬롯 매핑은 마이그레이션 설계서 §3.2 참조.
    /// - State는 참조형(CameraState)이라 변형이 ref 없이도 보존되지만, Pose는 값형이므로
    ///   반드시 frame.Pose에 다시 써야 한다.
    /// </summary>
    public struct CameraFrame
    {
        /// <summary>모드/매니저가 제공하는 런타임 의존성 묶음.</summary>
        public CameraContext Context;

        /// <summary>프레임 간 누적되는 가변 카메라 상태(yaw/pitch/distance/offset/velocity).</summary>
        public CameraState State;

        /// <summary>이번 프레임 활성 이펙트들이 채운 가산/오버라이드 델타.</summary>
        public CameraEffectState Effects;

        /// <summary>Modifier들이 단계적으로 채우는 최종 포즈 결과.</summary>
        public CameraPose Pose;

        public float DeltaTime;

        // ── 프레임 내 Modifier 간 신호 (cross-frame 아님, 매 프레임 새로 채워짐) ──

        /// <summary>
        /// 락온 해제 직후/정렬 중 위치 스무딩을 유지할지. LockOnReleaseSmoothing(660)이 설정,
        /// Follow(700)가 posSmoothTime 결정에 소비한다.
        /// </summary>
        public bool KeepPositionSmoothing;

        /// <summary>
        /// 스무딩 적용 *이전*의 피벗 기준 위치(Target+offset+lockOnPivotOffset 또는 LookAtOverride).
        /// Follow(700)가 계산해 기록, Collision(800)의 SafeBack 스피어캐스트 원점으로 사용한다.
        /// LockOn.EvaluatePivotOffset()가 부작용(스무딩)을 가지므로 프레임당 1회만 계산하기 위함.
        /// </summary>
        public Vector3 PivotBase;

        /// <summary>
        /// 이번 프레임 카메라 거리 클램프 상한. 0이면 settings.maxDistance를 사용한다.
        /// LockOnFitDistance(670)가 상단·공중 대상 프레이밍을 위해 일반 max를 넘겨 설정하고,
        /// Follow(700)/Collision(800)이 거리 클램프 상한으로 소비한다.
        /// </summary>
        public float DistanceCeiling;
    }
}
