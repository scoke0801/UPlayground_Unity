using UnityEngine;

namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// (660) 락온 해제 직후 일정 시간 위치 스무딩을 유지하기 위한 정책 결정.
    /// Align 갱신(300) 이후의 context.IsAligning 상태를 읽어야 하므로 이 슬롯(클램프 이후, Follow 이전)에 둔다.
    /// 결과를 frame.KeepPositionSmoothing에 기록하면 Follow(700)가 posSmoothTime 결정에 사용한다.
    /// 원본: InGameCameraMode.EvaluatePose 라인 111-118, 134-136 + OnEnter 초기화
    /// 보간 상태(_wasLockOnLastFrame/_lockOnReleaseSmoothTimer)를 인스턴스로 보유한다.
    /// </summary>
    public sealed class LockOnReleaseSmoothingCameraModifier : ICameraModifier, ICameraModifierLifecycle
    {
        private bool _wasLockOnLastFrame;
        private float _lockOnReleaseSmoothTimer;

        public int Priority => 660;

        public void OnEnter(CameraContext context, CameraModeEnterParams enterParams)
        {
            _wasLockOnLastFrame = context?.LockOn?.IsActive ?? false;
            _lockOnReleaseSmoothTimer = 0f;
        }

        public void OnExit(CameraContext context) { }

        public void Apply(ref CameraFrame frame)
        {
            CameraContext context = frame.Context;
            if (context?.Settings == null) return;

            bool isLockOn = context.LockOn?.IsActive ?? false;
            bool wasLockOn = _wasLockOnLastFrame;

            if (wasLockOn && !isLockOn)
                _lockOnReleaseSmoothTimer = Mathf.Max(_lockOnReleaseSmoothTimer, context.Settings.lockOnTransitionDuration);

            frame.KeepPositionSmoothing = _lockOnReleaseSmoothTimer > 0f || context.IsAligning;

            _wasLockOnLastFrame = isLockOn;
            if (_lockOnReleaseSmoothTimer > 0f)
                _lockOnReleaseSmoothTimer = Mathf.Max(0f, _lockOnReleaseSmoothTimer - frame.DeltaTime);
        }
    }
}
