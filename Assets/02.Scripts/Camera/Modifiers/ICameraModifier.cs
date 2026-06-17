namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// 카메라 기능의 최소 단위. 하나의 작은 책임만 담당한다(추적/락온/충돌/FOV 등).
    ///
    /// 규칙:
    /// - 자신이 언제 켜지는지 판단하지 않는다(상태 판단은 Director/Behavior 책임).
    /// - 다른 Modifier의 내부 값을 직접 참조하지 않는다. CameraFrame(State/Effects/Pose)만 읽고 쓴다.
    /// - 프레임 간 보간 연속성이 필요한 상태(SmoothDamp velocity, blend timer 등)는
    ///   이 Modifier *인스턴스의 필드*로 보유한다. stateless로 만들면 SmoothDamp 연속성이 끊긴다.
    /// </summary>
    public interface ICameraModifier
    {
        /// <summary>실행 순서. 작을수록 먼저 실행된다. 권장 순서는 마이그레이션 설계서 §3.2 참조.</summary>
        int Priority { get; }

        void Apply(ref CameraFrame frame);
    }
}
