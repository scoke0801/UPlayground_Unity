using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Components
{
    /// <summary>낙하 소품의 수면 통과를 공유 샘플러에서 검사한다. 개별 Update와 물 Collider가 필요 없다.</summary>
    [DisallowMultipleComponent]
    public sealed class WaterContactEmitter : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _strength = 1f;
        [SerializeField, Min(0.01f)] private float _minimumInterval = 0.2f;
        private Vector3 _previousPosition;
        private float _nextContactTime;
        private static readonly List<WaterContactEmitter> Emitters = new();
        private static int _sampledFrame = -1;

        private void OnEnable()
        {
            _previousPosition = transform.position;
            _nextContactTime = 0f;
            if (!Emitters.Contains(this)) Emitters.Add(this);
        }

        private void OnDisable() => Emitters.Remove(this);

        /// <summary>여러 렌더 카메라가 있어도 게임 프레임당 한 번만 접촉을 기록한다.</summary>
        public static void SampleRegistered()
        {
            if (!Application.isPlaying || _sampledFrame == Time.frameCount) return;
            _sampledFrame = Time.frameCount;
            for (int i = 0; i < Emitters.Count; i++)
            {
                var emitter = Emitters[i];
                if (emitter == null || !emitter.isActiveAndEnabled) continue;
                Vector3 position = emitter.transform.position;
                if (Time.time >= emitter._nextContactTime
                    && FootstepWaterSurface.TryResolveSurface(position, out _, out var water)
                    && emitter._previousPosition.y > water.y && position.y <= water.y)
                {
                    WaterContactStamps.TryEmit(position, emitter._strength);
                    emitter._nextContactTime = Time.time + emitter._minimumInterval;
                }
                emitter._previousPosition = position;
            }
        }
    }
}
