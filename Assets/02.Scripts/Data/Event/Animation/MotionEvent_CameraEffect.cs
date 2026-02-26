using System;
using UnityEngine;
using UPlayGround.Data;
using UPlayGround.Manager;

namespace UPlayGround.Data.Event
{
    /// <summary>
    /// 범용 카메라 이펙트 이벤트
    /// CameraEffectData SO를 지정하면 startTime에 PlayEffect, endTime에 StopEffect 자동 호출.
    /// Shake, Zoom, FOV, Rotation, TimeScale 등 모든 이펙트 타입에 사용 가능.
    /// </summary>
    [Serializable]
    public class CameraEffectEvent : MotionEventBase
    {
        [Tooltip("재생할 CameraEffectData SO")]
        public CameraEffectData effectData;

        [Tooltip("이펙트 재생 중 카메라 수동 조작 잠금")]
        public bool lockCameraInput = false;

        // 재생 중인 이펙트 핸들 (OnCompleteEvent에서 정지용)
        private ICameraEffect _activeHandle;

        public override string GetDisplayName() => "Camera Effect";

        public override string GetShortLabel() =>
            effectData != null ? $"Cam: {effectData.effectKey}" : "Cam: (None)";

        public override void Execute(GameObject target)
        {
            if (effectData == null)
            {
                Debug.LogWarning("[CameraEffectEvent] effectData가 설정되지 않았습니다.");
                return;
            }

            if (CameraManager.Instance == null) return;

            _activeHandle = CameraManager.Instance.PlayEffect(effectData);

            if (lockCameraInput)
                CameraManager.Instance.SetInputLock(true);
        }

        public override void OnCompleteEvent(GameObject target)
        {
            if (_activeHandle != null)
            {
                CameraManager.Instance?.StopEffect(_activeHandle);
                _activeHandle = null;
            }

            if (lockCameraInput)
                CameraManager.Instance?.SetInputLock(false);
        }
    }
}
