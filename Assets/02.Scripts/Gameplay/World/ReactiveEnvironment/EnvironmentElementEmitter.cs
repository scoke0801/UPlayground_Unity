using UnityEngine;
using UPlayGround.Manager;

namespace UPlayGround.Gameplay.World.ReactiveEnvironment
{
    /// <summary>모닥불·물 분사 등 지속 공급원을 중앙 환경 tick에 등록한다.</summary>
    public sealed class EnvironmentElementEmitter : MonoBehaviour
    {
        [Min(0.01f)] public float radius = 3;
        [Min(0)] public float heatPerSecond;
        [Min(0)] public float waterPerSecond;
        [Tooltip("0은 무기한. 게임 시간 배율의 초로 감소합니다.")]
        [Min(0)] public float lifetime;
        public int surfaceLayerId;
        public LayerMask blockerLayers;
        private WorldStateManager _world;
        private bool _hasStarted;
        private float _elapsed;
        public bool IsEmitting => isActiveAndEnabled && (lifetime == 0 || _elapsed < lifetime);
        private void Start() { _hasStarted = true; Register(); }
        private void OnEnable() { _elapsed = 0; if (_hasStarted) Register(); }
        private void Register() { _world = WorldStateManager.Instance; _world?.RegisterEnvironmentEmitter(this); }
        private void OnDisable() { if (_world != null) _world.UnregisterEnvironmentEmitter(this); }
        /// <summary>개별 Update 없이 도메인 작성자가 수명을 진행한다.</summary>
        public float AdvanceLifetime(float seconds)
        {
            float duration = lifetime == 0 ? seconds : Mathf.Min(seconds, Mathf.Max(0, lifetime - _elapsed));
            _elapsed += seconds;
            return seconds > 0 ? duration / seconds : 0;
        }
    }
}
