using System.Collections.Generic;
using UPlayGround.Data;

namespace UPlayGround
{
    /// <summary>
    /// 활성 카메라 이펙트를 관리하는 매니저
    /// CameraManager에 의해 소유되며, LateUpdate에서 매 프레임 호출된다.
    /// 싱글톤이 아닌 일반 C# 클래스로 CameraManager에 합성된다.
    /// </summary>
    public class CameraEffectManager
    {
        private readonly List<ICameraEffect> _activeEffects = new List<ICameraEffect>(8);
        private readonly List<ICameraEffect> _pendingRemoval = new List<ICameraEffect>(4);
        private readonly ICameraStateAccessor _cameraState;

        public CameraEffectManager(ICameraStateAccessor cameraState)
        {
            _cameraState = cameraState;
        }

        /// <summary>
        /// ScriptableObject 데이터로 이펙트를 생성하여 재생한다.
        /// 반환된 핸들로 수동 Stop이 가능하다.
        /// </summary>
        public ICameraEffect PlayEffect(CameraEffectData data)
        {
            ICameraEffect effect = data.CreateEffect();
            effect.Init(_cameraState);
            effect.Play();
            _activeEffects.Add(effect);
            return effect;
        }

        /// <summary>
        /// 미리 생성된 이펙트 인스턴스를 재생한다.
        /// </summary>
        public void PlayEffect(ICameraEffect effect)
        {
            effect.Init(_cameraState);
            effect.Play();
            _activeEffects.Add(effect);
        }

        /// <summary>
        /// 특정 이펙트를 정지한다 (BlendOut 시작).
        /// </summary>
        public void StopEffect(ICameraEffect effect, bool immediate = false)
        {
            effect?.Stop(immediate);
        }

        /// <summary>
        /// effectId가 일치하는 모든 이펙트를 정지한다.
        /// </summary>
        public void StopEffectById(string effectId, bool immediate = false)
        {
            for (int i = 0; i < _activeEffects.Count; i++)
            {
                if (_activeEffects[i].EffectId == effectId)
                    _activeEffects[i].Stop(immediate);
            }
        }

        /// <summary>
        /// 모든 활성 이펙트를 정지한다.
        /// </summary>
        public void StopAll(bool immediate = false)
        {
            for (int i = 0; i < _activeEffects.Count; i++)
                _activeEffects[i].Stop(immediate);
        }

        /// <summary>
        /// 모든 이펙트를 갱신하고, 블렌딩된 CameraEffectState를 계산하여 반환한다.
        /// CameraManager.OnLateUpdate()에서 프레임당 1회 호출한다.
        /// </summary>
        public CameraEffectState UpdateAndComputeState(float deltaTime)
        {
            // 1. 모든 이펙트 갱신
            for (int i = 0; i < _activeEffects.Count; i++)
            {
                _activeEffects[i].UpdateEffect(deltaTime);
            }

            // 2. 우선순위 오름차순 정렬 (높은 Priority가 마지막에 적용되어 오버라이드 우선권)
            _activeEffects.Sort((a, b) => a.Priority.CompareTo(b.Priority));

            // 3. 델타 누적
            CameraEffectState state = default;

            for (int i = 0; i < _activeEffects.Count; i++)
            {
                var effect = _activeEffects[i];

                if (effect.IsFinished)
                {
                    _pendingRemoval.Add(effect);
                    continue;
                }

                if (effect.Weight > 0f)
                {
                    effect.Apply(ref state);
                }
            }

            // 4. 종료된 이펙트 제거
            for (int i = 0; i < _pendingRemoval.Count; i++)
            {
                _activeEffects.Remove(_pendingRemoval[i]);
            }
            _pendingRemoval.Clear();

            return state;
        }

        /// <summary>
        /// 모든 이펙트를 강제 정리한다 (씬 전환, 매니저 종료).
        /// </summary>
        public void DisposeAll()
        {
            for (int i = 0; i < _activeEffects.Count; i++)
                _activeEffects[i].ForceDispose();
            _activeEffects.Clear();
        }

        public bool HasActiveEffects => _activeEffects.Count > 0;

        /// <summary>
        /// 특정 effectId의 이펙트가 활성 상태인지 확인한다.
        /// </summary>
        public bool IsEffectActive(string effectId)
        {
            for (int i = 0; i < _activeEffects.Count; i++)
            {
                if (_activeEffects[i].EffectId == effectId && _activeEffects[i].IsActive)
                    return true;
            }
            return false;
        }
    }
}
