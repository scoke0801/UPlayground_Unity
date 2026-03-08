using System;
using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;

namespace UPlayGround.Data.Event
{
    /// <summary>
    /// startTime 동안 Target(GameActor)의 소켓을 카메라 피벗으로 사용하는 이벤트.
    /// endTime에 자동으로 원래 추적(플레이어)으로 복귀한다.
    /// </summary>
    [Serializable]
    public class CameraLookAtSocketEvent : MotionEventBase
    {
        [Tooltip("주시할 소켓 타입")]
        public ActorSocketType socketType = ActorSocketType.Head;

        [Tooltip("소켓 위치에 추가할 오프셋 (월드 스페이스)")]
        public Vector3 offset = Vector3.zero;

        [Tooltip("이벤트 재생 중 카메라 수동 조작 잠금")]
        public bool lockCameraInput = false;

        public override string GetDisplayName() => "Camera LookAt Socket";

        public override string GetShortLabel() => $"LookAt: {socketType}";

        public override void Execute(GameObject target)
        {
            if (CameraManager.Instance == null) return;

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

            CameraManager.Instance.SetLookAtOverride(socket, offset);

            if (lockCameraInput)
                CameraManager.Instance.SetInputLock(true);
        }

        public override void OnCompleteEvent(GameObject target)
        {
            if (CameraManager.Instance == null) return;

            CameraManager.Instance.ClearLookAtOverride();

            if (lockCameraInput)
                CameraManager.Instance.SetInputLock(false);
        }
    }
}
