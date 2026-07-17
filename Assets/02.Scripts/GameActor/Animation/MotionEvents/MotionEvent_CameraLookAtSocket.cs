using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;

namespace UPlayGround.Data.Event
{
    /// <summary>
    /// startTime 동안 Target(GameActor)의 소켓을 카메라 피벗으로 사용하는 이벤트.
    /// 방향 오버라이드, 전환 속도(상수/커브), 종료 시 원복 여부를 지정할 수 있다.
    /// </summary>
    [Serializable]
    [MovedFrom(true, sourceAssembly: "Assembly-CSharp")]
    public class CameraLookAtSocketEvent : MotionEventBase
    {
        [Tooltip("주시할 소켓 타입")]
        public ActorSocketType socketType = ActorSocketType.Head;

        [Tooltip("소켓 위치에 추가할 오프셋 (월드 스페이스)")]
        public Vector3 offset = Vector3.zero;

        [Header("Direction Override")]
        [Tooltip("활성화 시 아래 설정으로 카메라 방향을 전환한다")]
        public bool overrideDirection = false;

        [Tooltip(
            "플레이어 forward 기준 수평 방향 오프셋 (도)\n" +
            "  0   = 정면 (얼굴을 마주봄)\n" +
            " 45   = 우측 전방 45°\n" +
            " 90   = 우측면\n" +
            "135   = 우측 후방 45°\n" +
            "180   = 정후방 (뒤통수)\n" +
            "225   = 좌측 후방 45°\n" +
            "270   = 좌측면\n" +
            "315   = 좌측 전방 45°")]
        [Range(0f, 360f)]
        public float angleOffset = 0f;

        [Tooltip("카메라 수직 각도 보정 (도)\n양수 = 위에서 내려다봄, 음수 = 아래서 올려봄")]
        [Range(-60f, 60f)]
        public float pitchOffset = 15f;

        [Tooltip("방향 전환 소요 시간 (초)")]
        public float lookDuration = 0.5f;

        [Tooltip("방향 전환 커브 (null이면 내부 SmoothStep 사용)\nX축=시간(0~1), Y축=보간 비율(0~1)")]
        public AnimationCurve lookCurve = null;

        [Header("Restore")]
        [Tooltip("이벤트 종료 시 진입 전 카메라 방향으로 복원")]
        public bool restoreOnComplete = false;

        [Tooltip("복원 소요 시간 (초)")]
        public float restoreDuration = 0.3f;

        [Header("Lock")]
        [Tooltip("이벤트 재생 중 카메라 수동 조작 잠금")]
        public bool lockCameraInput = false;

        // 복원용 진입 시점 각도 저장
        private float _savedYaw;
        private float _savedPitch;

        public override string GetDisplayName() => "Camera LookAt Socket";
        public override string GetShortLabel() => $"LookAt: {socketType} ({angleOffset}°)";

        public override void Execute(GameObject target)
        {
            var cam = CameraManager.Instance;
            if (cam == null) return;

            var actor = target.GetComponent<GameActor>();
            if (actor == null)
            {
                Debug.LogWarning("[CameraLookAtSocketEvent] target에 GameActor가 없습니다.");
                return;
            }

            if (!actor.TryGetSocket(socketType, out Transform socket))
            {
                Debug.LogWarning($"[CameraLookAtSocketEvent] '{socketType}' 소켓을 찾을 수 없습니다.");
                return;
            }

            if (restoreOnComplete)
            {
                _savedYaw   = cam.GetCurrentYaw();
                _savedPitch = cam.GetCurrentPitch();
            }

            cam.SetLookAtOverride(socket, offset);

            if (overrideDirection)
            {
                // 플레이어 forward 기준으로 angleOffset 회전한 방향에서 소켓을 바라보는 Yaw
                // +180f: angleOffset 방향의 반대편(= 그 방향에서 소켓을 향하는 시점)
                float worldYaw   = actor.transform.eulerAngles.y + angleOffset + 180f;
                float worldPitch = cam.GetCurrentPitch() + pitchOffset;

                Debug.Log($"[CameraLookAt] Execute → actorYaw={actor.transform.eulerAngles.y:F1} angleOffset={angleOffset} " +
                          $"worldYaw={worldYaw:F1} currentPitch={cam.GetCurrentPitch():F1} pitchOffset={pitchOffset} worldPitch={worldPitch:F1} duration={lookDuration}");

                cam.SetRotationSmooth(worldYaw, worldPitch, lookDuration, lookCurve);

                Debug.Log($"[CameraLookAt] After SetRotationSmooth → _rotTransitionActive 확인 필요");
            }

            // 입력 잠금은 회전 전환 설정 이후에 걸어야
            // SetRotationSmooth 내부 상태가 잠금 영향을 받지 않는다
            if (lockCameraInput)
                cam.SetInputLock(true);
        }

        public override void OnCompleteEvent(GameObject target)
        {
            var cam = CameraManager.Instance;
            if (cam == null) return;

            cam.ClearLookAtOverride();

            if (lockCameraInput)
                cam.SetInputLock(false);

            if (restoreOnComplete)
                cam.SetRotationSmooth(_savedYaw, _savedPitch, restoreDuration, null);
        }
    }
}
