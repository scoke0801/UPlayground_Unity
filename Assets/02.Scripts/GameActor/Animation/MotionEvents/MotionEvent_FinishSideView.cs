using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UPlayGround.Manager;
using UPlayGround.MovementController;
using UPlayGround.State;

namespace UPlayGround.Data.Event
{
    /// <summary>
    /// FinishAttack 측면 카메라 이벤트
    /// 처형 대상과 플레이어를 측면에서 바라보도록 카메라 Yaw를 회전시킨다.
    ///
    /// [동작]
    /// - Execute  : 플레이어→처형타겟 방향 축에서 sideAngleOffset 만큼 회전한 Yaw로 카메라 전환
    /// - OnComplete: restoreOnComplete가 true이면 이전 Yaw/Pitch로 복원
    ///
    /// [측면 각도 선택]
    ///  sideAngleOffset = +90 → 플레이어-타겟 축의 왼쪽
    ///  sideAngleOffset = -90 → 플레이어-타겟 축의 오른쪽
    ///
    /// [에디터 테스트]
    ///  editorTestTarget을 지정하면 PlayerFinishAttackState 없이도
    ///  모션 에디터(PlayMode) 타임라인 스크럽으로 카메라 방향을 미리 확인할 수 있다.
    /// </summary>
    [Serializable]
    [MovedFrom(true, sourceAssembly: "Assembly-CSharp")]
    public class FinishSideViewEvent : MotionEventBase
    {
        [Tooltip("플레이어→타겟 방향 기준 측면 오프셋 각도\n+90 = 왼쪽 측면  /  -90 = 오른쪽 측면")]
        public float sideAngleOffset = 90f;

        [Tooltip("카메라 Pitch(상하 각도) 설정값 (도)")]
        public float pitchOverride = 10f;

        [Tooltip("이벤트 종료 시 이전 카메라 Yaw/Pitch로 복원할지 여부")]
        public bool restoreOnComplete = true;

        [Tooltip("이벤트 재생 중 카메라 수동 조작 잠금")]
        public bool lockCameraInput = true;

        [Tooltip("카메라 회전 전환 시간 (초).\n0이면 즉시 스냅, 값이 클수록 천천히 전환됨")]
        public float transitionDuration = 0.25f;

#if UNITY_EDITOR
        [Tooltip("에디터 테스트용 처형 타겟\n" +
                 "PlayMode + 모션 에디터에서 PlayerFinishAttackState 없이 테스트할 때 지정")]
        public Transform editorTestTarget;
#endif

        // 복원용 저장값
        private float _savedYaw;
        private float _savedPitch;

        public override string GetDisplayName() => "Finish Side View";

        public override string GetShortLabel() =>
            $"SideView ({sideAngleOffset:+0;-0}°, pitch {pitchOverride:F0}°)";

        // ─────────────────────────────────────────────
        public override void Execute(GameObject target)
        {
            if (CameraManager.Instance == null) return;

            // 1. 처형 타겟 결정
            Transform finishTarget = ResolveFinishTarget(target);
            if (finishTarget == null)
            {
                Debug.LogWarning("[FinishSideViewEvent] 처형 타겟을 찾을 수 없습니다. " +
                                 "PlayerFinishAttackState.FinishTarget 또는 editorTestTarget을 확인하세요.");
                return;
            }

            // 2. 현재 카메라 Yaw/Pitch 저장 (복원용)
            var accessor = CameraManager.Instance as ICameraStateAccessor;
            if (accessor != null)
            {
                _savedYaw   = accessor.CurrentYaw;
                _savedPitch = accessor.CurrentPitch;
            }

            // 3. 플레이어 → 처형타겟 방향의 Yaw 계산
            Vector3 toTarget = finishTarget.position - target.transform.position;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude < 0.001f) return;

            float attackAxisYaw = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg;

            // 4. 측면 Yaw = 공격 축 ± sideAngleOffset
            float sideYaw = attackAxisYaw + sideAngleOffset;

            // 5. 카메라 회전 적용 (스무스 전환)
            CameraManager.Instance.SetRotationSmooth(sideYaw, pitchOverride, transitionDuration);

            if (lockCameraInput)
                CameraManager.Instance.SetInputLock(true);
        }

        public override void OnCompleteEvent(GameObject target)
        {
            if (CameraManager.Instance == null) return;

            if (restoreOnComplete)
            {
                // 이전 Yaw/Pitch로 스무스 복원
                // lockCameraInput == true이면 복원 전환 완료 후 입력 잠금 자동 해제
                // lockCameraInput == false이면 즉시 해제하지 않아도 되므로 unlockOnComplete: false
                CameraManager.Instance.SetRotationSmooth(
                    _savedYaw, _savedPitch, transitionDuration,
                    unlockOnComplete: lockCameraInput);
            }
            else
            {
                // 복원 없음 → 즉시 입력 잠금 해제
                if (lockCameraInput)
                    CameraManager.Instance.SetInputLock(false);
            }
        }

        /// <summary>
        /// 처형 타겟 결정 우선순위
        /// 1순위: PlayerFinishAttackState.FinishTarget (런타임 실전)
        /// 2순위: editorTestTarget (에디터 테스트, #if UNITY_EDITOR)
        /// </summary>
        private Transform ResolveFinishTarget(GameObject target)
        {
            var movCtrl = target.GetComponent<ActorMovementController>();
            if (movCtrl?.CurrentState is PlayerFinishAttackState finishState)
                return finishState.FinishTarget;

#if UNITY_EDITOR
            if (editorTestTarget != null)
                return editorTestTarget;
#endif
            return null;
        }
    }
}
