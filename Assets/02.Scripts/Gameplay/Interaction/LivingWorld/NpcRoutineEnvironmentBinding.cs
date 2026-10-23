using UnityEngine;
using UPlayGround.Components.LivingWorld;
using UPlayGround.Gameplay.World.LivingWorld;

namespace UPlayGround.Gameplay.Interaction.LivingWorld
{
    /// <summary>D의 환경 생산자를 C 일과 입력에 연결한다. Actor 모듈에는 D 구현을 노출하지 않는다.</summary>
    public sealed class NpcRoutineEnvironmentBinding : MonoBehaviour
    {
        [SerializeField] private EnvironmentController _environment;
        [SerializeField] private NpcRoutine[] _routines = System.Array.Empty<NpcRoutine>();
        [Min(0f), SerializeField] private float _rainEnter = 0.15f;
        [Min(0f), SerializeField] private float _rainExit = 0.05f;
        [Range(0f, 1f), SerializeField] private float _windEnter = 0.65f;
        [Range(0f, 1f), SerializeField] private float _windExit = 0.5f;
        private EnvironmentController _boundEnvironment;
        private bool _needsShelter;
        private object _sourceToken;

        private void OnEnable() => BindEnvironment(_environment);

        /// <summary>씬 생산자 교체 시 기존 구독과 소비 토큰을 해제하고 현재 상태부터 읽는다.</summary>
        public void BindEnvironment(EnvironmentController environment)
        {
            UnbindEnvironment();
            _environment = environment;
            if (!isActiveAndEnabled || environment == null) return;
            _boundEnvironment = environment;
            _sourceToken = new object();
            _needsShelter = false;
            for (int i = 0; i < _routines.Length; i++) _routines[i]?.BindEnvironment(_sourceToken);
            environment.OnStateChanged += OnStateChanged;
            OnStateChanged(environment);
        }

        private void OnStateChanged(EnvironmentController environment)
        {
            if (environment != _boundEnvironment) return;
            if (environment.IsReady)
            {
                float rainThreshold = _needsShelter ? _rainExit : _rainEnter;
                float windThreshold = _needsShelter ? _windExit : _windEnter;
                _needsShelter = environment.Current.Rain > rainThreshold
                    || environment.Current.Wind.sqrMagnitude > windThreshold * windThreshold;
            }
            for (int i = 0; i < _routines.Length; i++)
                _routines[i]?.ApplyEnvironment(_sourceToken, environment.MapId, environment.Revision,
                    environment.IsReady, _needsShelter);
        }

        private void UnbindEnvironment()
        {
            if (_boundEnvironment != null) _boundEnvironment.OnStateChanged -= OnStateChanged;
            for (int i = 0; i < _routines.Length; i++) _routines[i]?.ReleaseEnvironment(_sourceToken);
            _sourceToken = null;
            _boundEnvironment = null;
        }

        private void OnDisable() => UnbindEnvironment();
    }
}
