using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;
using UPlayGround.Data;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;

namespace UPlayGround.Data.Event
{
    internal static class MotionEventEnemyScope
    {
        /// <summary>
        /// Allowed만 몬스터 실행을 허용한다. Ignored는 정상적인 무시, Forbidden은
        /// 검증 오류이지만 런타임에서는 전역 연출 누수를 막기 위해 동일하게 차단한다.
        /// </summary>
        public static bool ShouldSkip(
            GameObject target,
            MotionEventEnemyExecutionPolicy policy)
        {
            if (policy == MotionEventEnemyExecutionPolicy.Allowed || target == null)
                return false;

            // 명시적인 MotionEventTarget이 액터의 자식일 때도 실제 소유자를 판별한다.
            GameActor actor = target.GetComponentInParent<GameActor>();
            return actor != null && actor.HasActorType(ActorType.Monster);
        }

        public static int GetTargetKey(GameObject target)
        {
            return target != null ? target.GetInstanceID() : 0;
        }
    }

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

        [FormerlySerializedAs("ignoreWhenEnemy")]
        [Tooltip("몬스터가 이 모션을 재생할 때의 처리. 금지는 검증 오류이며 런타임에서도 차단된다.")]
        public MotionEventEnemyExecutionPolicy enemyExecutionPolicy =
            MotionEventEnemyExecutionPolicy.Ignored;

        private sealed class ActiveCameraEffectState
        {
            public readonly List<ICameraEffect> handles = new List<ICameraEffect>();
            public bool inputLocked;
        }

        [NonSerialized]
        private Dictionary<int, ActiveCameraEffectState> _activeStates;

        public override MotionEventEnemyExecutionPolicy EnemyExecutionPolicy => enemyExecutionPolicy;

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
            if (MotionEventEnemyScope.ShouldSkip(target, EnemyExecutionPolicy))
                return;

            if (effectDataList == null || effectDataList.Count == 0)
            {
                Debug.LogWarning("[CameraEffectEvent] effectDataList가 비어있습니다.");
                return;
            }

            CameraManager cameraManager = CameraManager.Instance;
            if (cameraManager == null)
                return;

            int targetKey = MotionEventEnemyScope.GetTargetKey(target);
            ReleaseActiveState(targetKey, cameraManager);

            var state = new ActiveCameraEffectState();
            foreach (var data in effectDataList)
            {
                if (data == null) continue;
                state.handles.Add(cameraManager.PlayEffect(data));
            }

            if (lockCameraInput)
            {
                cameraManager.SetInputLock(true);
                state.inputLocked = true;
            }

            if (state.handles.Count > 0 || state.inputLocked)
            {
                _activeStates ??= new Dictionary<int, ActiveCameraEffectState>();
                _activeStates[targetKey] = state;
            }
        }

        public override void OnCompleteEvent(GameObject target)
        {
            ReleaseActiveState(
                MotionEventEnemyScope.GetTargetKey(target),
                CameraManager.Instance);
        }

        /// <summary>
        /// 실제로 시작된 대상의 이펙트만 정리한다. 적이라서 무시됐거나 시작에 실패한
        /// 실행은 상태가 없으므로 다른 카메라 연출을 건드리지 않는다.
        /// </summary>
        private void ReleaseActiveState(int targetKey, CameraManager cameraManager)
        {
            if (_activeStates == null
                || !_activeStates.TryGetValue(targetKey, out ActiveCameraEffectState state))
                return;

            _activeStates.Remove(targetKey);
            if (_activeStates.Count == 0)
                _activeStates = null;

            if (cameraManager == null)
                return;

            foreach (ICameraEffect handle in state.handles)
                cameraManager.StopEffect(handle);

            if (state.inputLocked)
                cameraManager.SetInputLock(false);
        }
    }
}
