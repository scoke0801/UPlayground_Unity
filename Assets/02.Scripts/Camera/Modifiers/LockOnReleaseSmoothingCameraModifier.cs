using UnityEngine;

namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// (660) 락온 해제 직후 일정 시간 위치 스무딩을 유지하기 위한 정책 결정.
    /// 결과를 frame.KeepPositionSmoothing에 기록하면 Follow(700)가 posSmoothTime 결정에 사용하므로
    /// Follow(700) 이전 슬롯에 둔다.
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
            bool hasResidualPivotOffset = context.LockOn?.HasResidualPivotOffset ?? false;

            if (wasLockOn && !isLockOn)
            {
                float releaseSmoothTime = Mathf.Max(
                    context.Settings.lockOnTransitionDuration,
                    context.Settings.lockOnPairFocusSmoothTime * 2.5f);
                _lockOnReleaseSmoothTimer = Mathf.Max(_lockOnReleaseSmoothTimer, releaseSmoothTime);
            }

            // 대상 없는 align(탐색 중 락온 키)은 회전만 보정하면 충분하다. align 중 위치 스무딩을 켜면
            // 이동 시 피벗이 뒤처지다가 align 종료(posSmoothTime 0 복귀) 순간 따라잡으며 카메라가 끊긴다.
            // 락온 해제 직후의 위치 스무딩은 _lockOnReleaseSmoothTimer/hasResidualPivotOffset가 담당한다.
            frame.KeepPositionSmoothing = _lockOnReleaseSmoothTimer > 0f || hasResidualPivotOffset;

            _wasLockOnLastFrame = isLockOn;
            if (_lockOnReleaseSmoothTimer > 0f)
                _lockOnReleaseSmoothTimer = Mathf.Max(0f, _lockOnReleaseSmoothTimer - frame.DeltaTime);
        }
    }
}
