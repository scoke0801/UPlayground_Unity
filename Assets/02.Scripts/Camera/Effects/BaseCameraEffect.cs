using UnityEngine;
using UPlayGround.Data;

namespace UPlayGround
{
    /// <summary>
    /// 카메라 이펙트 추상 베이스 클래스
    /// Phase 머신(Idle→BlendIn→Active→BlendOut→Finished)과 가중치 보간을 공통 처리한다.
    /// 구체 이펙트는 OnPlay, OnUpdateEffect, Apply 등을 오버라이드하여 고유 동작을 구현한다.
    /// </summary>
    public abstract class BaseCameraEffect : ICameraEffect
    {
        protected enum Phase { Idle, BlendIn, Active, BlendOut, Finished }

        // 상태
        protected Phase _currentPhase = Phase.Idle;
        protected ICameraStateAccessor _cameraState;
        protected float _elapsedTime;
        protected float _phaseTime;
        protected float _currentWeight;

        // 데이터 캐싱
        private readonly string _effectId;
        private readonly int _priority;
        private readonly float _blendInDuration;
        private readonly float _blendOutDuration;
        private readonly AnimationCurve _blendInCurve;
        private readonly AnimationCurve _blendOutCurve;
        private readonly bool _useUnscaledTime;

        /// <summary>총 지속 시간 (0 = 무한)</summary>
        protected readonly float _duration;

        // ICameraEffect 구현
        public string EffectId => _effectId;
        public int Priority => _priority;
        public float Weight => _currentWeight;
        public bool IsActive => _currentPhase != Phase.Idle && _currentPhase != Phase.Finished;
        public bool IsFinished => _currentPhase == Phase.Finished;

        protected BaseCameraEffect(CameraEffectData data)
        {
            _effectId = data.effectKey;
            _priority = data.priority;
            _duration = data.duration;
            _blendInDuration = data.blendInDuration;
            _blendOutDuration = data.blendOutDuration;
            _blendInCurve = data.blendInCurve;
            _blendOutCurve = data.blendOutCurve;
            _useUnscaledTime = data.useUnscaledTime;
        }

        public void Init(ICameraStateAccessor cameraState)
        {
            _cameraState = cameraState;
            OnInit();
        }

        public void Play()
        {
            _elapsedTime = 0f;
            _phaseTime = 0f;

            if (_blendInDuration > 0f)
            {
                _currentPhase = Phase.BlendIn;
                _currentWeight = 0f;
            }
            else
            {
                _currentPhase = Phase.Active;
                _currentWeight = 1f;
            }

            OnPlay();
        }

        public void Stop(bool immediate = false)
        {
            if (immediate || _blendOutDuration <= 0f)
            {
                _currentPhase = Phase.Finished;
                _currentWeight = 0f;
                OnStop();
                return;
            }

            if (_currentPhase != Phase.BlendOut && _currentPhase != Phase.Finished)
            {
                _currentPhase = Phase.BlendOut;
                _phaseTime = 0f;
                OnStop();
            }
        }

        public void UpdateEffect(float deltaTime)
        {
            float dt = _useUnscaledTime ? Time.unscaledDeltaTime : deltaTime;
            _elapsedTime += dt;

            switch (_currentPhase)
            {
                case Phase.BlendIn:
                {
                    _phaseTime += dt;
                    float t = Mathf.Clamp01(_phaseTime / _blendInDuration);
                    _currentWeight = _blendInCurve.Evaluate(t);

                    if (t >= 1f)
                    {
                        _currentPhase = Phase.Active;
                        _phaseTime = 0f;
                        _currentWeight = 1f;
                    }
                    break;
                }

                case Phase.Active:
                {
                    _phaseTime += dt;
                    _currentWeight = 1f;

                    // duration > 0 이면 자동 종료
                    if (_duration > 0f && _elapsedTime >= _duration)
                    {
                        Stop(false);
                    }
                    break;
                }

                case Phase.BlendOut:
                {
                    _phaseTime += dt;
                    float t = Mathf.Clamp01(_phaseTime / _blendOutDuration);
                    _currentWeight = _blendOutCurve.Evaluate(t);

                    if (t >= 1f)
                    {
                        _currentPhase = Phase.Finished;
                        _currentWeight = 0f;
                    }
                    break;
                }
            }

            OnUpdateEffect(dt);
        }

        public virtual void ForceDispose()
        {
            _currentPhase = Phase.Finished;
            _currentWeight = 0f;
        }

        // 구체 클래스 오버라이드 포인트
        public abstract CameraEffectChannel AffectedChannels { get; }
        public abstract void Apply(ref CameraEffectState state);

        protected virtual void OnInit() { }
        protected virtual void OnPlay() { }
        protected virtual void OnStop() { }
        protected virtual void OnUpdateEffect(float dt) { }
    }
}
