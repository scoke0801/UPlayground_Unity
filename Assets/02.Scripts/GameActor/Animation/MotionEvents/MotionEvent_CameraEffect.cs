using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UPlayGround.Data;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;

namespace UPlayGround.Data.Event
{
    /// <summary>
    /// 범용 카메라 이펙트 이벤트
    /// CameraEffectData SO를 지정하면 startTime에 PlayEffect, endTime에 StopEffect 자동 호출.
    /// Shake, Zoom, FOV, Rotation, TimeScale 등 모든 이펙트 타입에 사용 가능.
    /// </summary>
    [Serializable]
    [MovedFrom(true, sourceAssembly: "Assembly-CSharp")]
    [MotionEventDescriptor("CameraEffect", "Camera", 0, "카메라 흔들림, 줌, FOV 효과를 재생합니다.", "shake", "zoom", "fov", "카메라", "흔들림")]
    public class CameraEffectEvent : MotionEventBase
    {
        [Tooltip("재생할 CameraEffectData SO 목록")]
        public List<CameraEffectData> effectDataList = new List<CameraEffectData>();

        [Tooltip("이펙트 재생 중 카메라 수동 조작 잠금")]
        public bool lockCameraInput = false;

        [Tooltip("적(Monster)이 공격하는 모션이면 이 이펙트를 무시한다. 기본값 true.")]
        public bool ignoreWhenEnemy = true;

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
            if (ShouldSkipForEnemy(target)) return;

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
            if (ShouldSkipForEnemy(target)) return;

            foreach (var handle in _activeHandles)
                CameraManager.Instance?.StopEffect(handle);

            _activeHandles.Clear();

            if (lockCameraInput)
                CameraManager.Instance?.SetInputLock(false);
        }

        /// <summary>
        /// ignoreWhenEnemy가 켜져 있고 모션 소유자가 적(Monster)이면 true.
        /// 이 경우 카메라 이펙트를 재생/정리하지 않는다.
        /// Execute와 OnCompleteEvent에서 동일하게 판정해 잠금이 비대칭으로 남지 않도록 한다.
        /// </summary>
        private bool ShouldSkipForEnemy(GameObject target)
        {
            if (!ignoreWhenEnemy || target == null) return false;
            GameActor actor = target.GetComponent<GameActor>();
            return actor != null && actor.HasActorType(ActorType.Monster);
        }
    }
}
