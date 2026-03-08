using System;
using System.Collections.Generic;
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
        [Tooltip("재생할 CameraEffectData SO 목록")]
        public List<CameraEffectData> effectDataList = new List<CameraEffectData>();

        [Tooltip("이펙트 재생 중 카메라 수동 조작 잠금")]
        public bool lockCameraInput = false;

        private readonly List<ICameraEffect> _activeHandles = new List<ICameraEffect>();

        public override string GetDisplayName() => "Camera Effect";

        public override string GetShortLabel()
        {
            if (effectDataList == null || effectDataList.Count == 0)
                return "Cam: (None)";
            if (effectDataList.Count == 1)
                return $"Cam: {effectDataList[0]?.effectKey ?? "None"}";
            return $"Cam: {effectDataList[0]?.effectKey ?? "None"} +{effectDataList.Count - 1}";
        }

        public override void Execute(GameObject target)
        {
            if (effectDataList == null || effectDataList.Count == 0)
            {
                Debug.LogWarning("[CameraEffectEvent] effectDataList가 비어있습니다.");
                return;
            }

            if (CameraManager.Instance == null) return;

            _activeHandles.Clear();
            foreach (var data in effectDataList)
            {
                if (data == null) continue;
                _activeHandles.Add(CameraManager.Instance.PlayEffect(data));
            }

            if (lockCameraInput)
                CameraManager.Instance.SetInputLock(true);
        }

        public override void OnCompleteEvent(GameObject target)
        {
            foreach (var handle in _activeHandles)
                CameraManager.Instance?.StopEffect(handle);

            _activeHandles.Clear();

            if (lockCameraInput)
                CameraManager.Instance?.SetInputLock(false);
        }
    }
}
