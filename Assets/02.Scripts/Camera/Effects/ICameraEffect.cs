namespace UPlayGround
{
    /// <summary>
    /// 카메라 이펙트 인터페이스
    /// 모든 카메라 이펙트(Rotation, Zoom, Shake, FOV 등)가 구현해야 하는 공통 계약
    /// </summary>
    public interface ICameraEffect
    {
        /// <summary>이펙트 식별 키</summary>
        string EffectId { get; }

        /// <summary>우선순위 (높을수록 우선)</summary>
        int Priority { get; }

        /// <summary>현재 블렌드 가중치 (0 = 비활성, 1 = 완전 적용)</summary>
        float Weight { get; }

        /// <summary>이펙트 실행 중 여부 (BlendIn, Active, BlendOut 중 하나)</summary>
        bool IsActive { get; }

        /// <summary>이펙트 완료 여부 (BlendOut 완료 후)</summary>
        bool IsFinished { get; }

        /// <summary>이 이펙트가 영향을 주는 카메라 채널</summary>
        CameraEffectChannel AffectedChannels { get; }

        /// <summary>카메라 상태 접근자로 초기화</summary>
        void Init(ICameraStateAccessor cameraState);

        /// <summary>이펙트 재생 시작 (BlendIn 시작)</summary>
        void Play();

        /// <summary>이펙트 정지 요청 (BlendOut 시작). immediate=true이면 즉시 종료</summary>
        void Stop(bool immediate = false);

        /// <summary>매 프레임 호출. 내부 상태 갱신 및 블렌드 가중치 계산</summary>
        void UpdateEffect(float deltaTime);

        /// <summary>CameraEffectState에 이 이펙트의 델타를 누적</summary>
        void Apply(ref CameraEffectState state);

        /// <summary>강제 정리 (씬 전환 등)</summary>
        void ForceDispose();
    }
}
