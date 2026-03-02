using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Rendering;
using UnityEngine.ResourceManagement.AsyncOperations;
using UPlayGround.Data.Path;

namespace UPlayGround.Manager.Handler
{
    /// <summary>
    /// HitStop(타격 정지) 효과를 전역적으로 관리하는 매니저
    /// Time.timeScale을 조작
    /// 액터 단위 Animation 속도 제어
    /// </summary>
    public class GameHitStopManager: BaseManager<GameHitStopManager>, IManager
    {
        /// <summary>
        /// HitStop 강도 Enum
        /// </summary>
        public enum HitStopIntensity
        {
            Light,      // 0.05초 - 약 공격
            Medium,     // 0.08초 - 중 공격
            Heavy,      // 0.12초 - 강 공격
            Critical,   // 0.15초 - 크리티컬/피니셔
            PlayerDie,  // 1초   - 플레이어 사망
            
            PlayerGuard, // 1초
        }
        
        [Header("HitStop Settings")]
        [SerializeField] private float _defaultHitStopDuration = 0.08f;
        [SerializeField] private float _defaultTimeScale = 0.1f;

        private AsyncOperationHandle<GameObject> _volumeHandle;
        private Volume _volume;
        private GameObject _volumeInstance;
        
        private Coroutine _currentHitStopCoroutine;
        private bool _isHitStopping;
        
        private float _transitionTime = 0.05f; // 전환 속도
        
        private float _targetWeight = 0f;
        private float _currentWeight = 0f;
        private float _weightVelocity = 0f;
        
        // GameActor별 코루틴 캐싱
        private Dictionary<GameActor, Coroutine> _actorHitStopCoroutines = new Dictionary<GameActor, Coroutine>();
        
        private const float NORMAL_TIME_SCALE = 1.0f;
        
        public bool IsHitStopping => _isHitStopping;

        public void Init()
        {
            Time.timeScale = NORMAL_TIME_SCALE;
            _actorHitStopCoroutines.Clear();

            LoadVolume();
        }

        public void AfterInit()
        {
        }

        public void Dispose()
        {            
            Stop();
            StopAllActors();
            
            // 볼륨 인스턴스 파괴
            if (_volumeInstance != null)
            {
                Destroy(_volumeInstance);
                _volumeInstance = null;
            }

            // 어드레서블 메모리 해제
            if (_volumeHandle.IsValid())
            {
                Addressables.Release(_volumeHandle);
            }
        }

        public void OnUpdate()
        {
            if (_volume == null) return;

            // Time.unscaledDeltaTime을 사용하여 현실 시간 기준으로 보간
            _currentWeight = Mathf.SmoothDamp(
                _currentWeight, 
                _targetWeight, 
                ref _weightVelocity, 
                _transitionTime, 
                Mathf.Infinity, 
                Time.unscaledDeltaTime
            );

            // Volume의 투명도(강도) 적용
            _volume.weight = _currentWeight;
        }

        public void OnFixedUpdate()
        {
        }

        public void OnLateUpdate()
        {
        }

        public void OnSceneChanged(string sceneType)
        {
            Stop();
            StopAllActors();
        }
        
        /// <summary>
        /// HitStop 실행
        /// </summary>
        public void Execute()
        {
            Execute(_defaultHitStopDuration, _defaultTimeScale);
        }
        
        /// <summary>
        /// HitStop 실행 (강도 지정)
        /// </summary>
        /// <param name="intensity">강도 (Light=0.05s, Medium=0.08s, Heavy=0.12s)</param>
        public void Execute(HitStopIntensity intensity)
        {
            switch (intensity)
            {
                case HitStopIntensity.Light:
                    Execute(0.05f, 0.15f);
                    break;
                case HitStopIntensity.Medium:
                    Execute(0.08f, 0.1f);
                    break;
                case HitStopIntensity.Heavy:
                    Execute(0.12f, 0.05f);
                    break;
                case HitStopIntensity.Critical:
                    Execute(0.15f, 0.02f);
                    break;
                case HitStopIntensity.PlayerDie:
                    Execute(1.0f, 0.02f);
                    break;
                case HitStopIntensity.PlayerGuard:
                    _targetWeight = 0f;
                    _currentWeight = 1f;
                    _transitionTime = 3f;
                    GameObjectManager.Instance?.SetGlobalTimeScaleExceptPlayer(0.05f, 3f);
                    //Execute(1f, 0.05f);
                    break;
            }
        }

        public void ResetActorTimeScale()
        {
            if (_currentWeight > 0)
            {
                _targetWeight = 0f;
                _transitionTime = 0f;
                GameObjectManager.Instance.ResetTimeScale();
            }
        }
        /// <summary>
        /// HitStop 실행 (커스텀 파라미터)
        /// </summary>
        /// <param name="duration">지속 시간 (초)</param>
        /// <param name="timeScale">시간 스케일 (0~1, 낮을수록 느림)</param>
        public void Execute(float duration, float timeScale = 0.1f)
        {
            // 이미 HitStop 중이면 중단하고 새로 시작 (더 강한 타격이 덮어씀)
            if (_currentHitStopCoroutine != null)
            {
                StopCoroutine(_currentHitStopCoroutine);
            }
            
            _currentHitStopCoroutine = StartCoroutine(HitStopCoroutine(duration, timeScale));
        }
        
        /// <summary>
        /// 현재 전역 HitStop 강제 종료
        /// </summary>
        public void Stop()
        {
            if (_currentHitStopCoroutine != null)
            {
                StopCoroutine(_currentHitStopCoroutine);
                _currentHitStopCoroutine = null;
            }

            Time.timeScale = NORMAL_TIME_SCALE;
            _isHitStopping = false;
        }

        /// <summary>
        /// 특정 GameActor만 느려지도록 (Animator 속도 조작)
        /// </summary>
        public void ExecuteActorOnly(GameActor actor, float duration, float animSpeed = 0.1f)
        {
            if (actor == null) return;
            
            // 이미 실행 중인 코루틴 정리
            StopActor(actor);
            
            Coroutine coroutine = StartCoroutine(ActorOnlyHitStopCoroutine(actor, duration, animSpeed));
            _actorHitStopCoroutines[actor] = coroutine;
        }
        
        /// <summary>
        /// 특정 GameActor의 HitStop 강제 종료
        /// </summary>
        public void StopActor(GameActor actor)
        {
            if (actor == null) return;
            
            if (_actorHitStopCoroutines.TryGetValue(actor, out Coroutine coroutine))
            {
                if (coroutine != null)
                {
                    StopCoroutine(coroutine);
                }
                
                _actorHitStopCoroutines.Remove(actor);
                
                // Animator 속도 복구
                Animator animator = actor.Animator?.GetAnimator;
                if (animator != null)
                {
                    animator.speed = 1.0f;
                }
            }
        }
        
        /// <summary>
        /// 모든 GameActor의 HitStop 강제 종료
        /// </summary>
        public void StopAllActors()
        {
            foreach (var kvp in _actorHitStopCoroutines)
            {
                if (kvp.Value != null)
                {
                    StopCoroutine(kvp.Value);
                }
                
                // Animator 속도 복구
                if (kvp.Key != null)
                {
                    Animator animator = kvp.Key.Animator?.GetAnimator;
                    if (animator != null)
                    {
                        animator.speed = 1.0f;
                    }
                }
            }
            
            _actorHitStopCoroutines.Clear();
        }
        
        /// <summary>
        /// 특정 GameActor가 HitStop 중인지 확인
        /// </summary>
        public bool IsActorHitStopping(GameActor actor)
        {
            if (actor == null) return false;
            return _actorHitStopCoroutines.ContainsKey(actor);
        }
    
        private IEnumerator ActorOnlyHitStopCoroutine(GameActor actor, float duration, float animSpeed)
        {
            if (actor == null) yield break;
            
            Animator animator = actor.Animator?.GetAnimator;
            if (animator == null)
            {
                _actorHitStopCoroutines.Remove(actor);
                yield break;
            }
        
            float originalSpeed = animator.speed;
            animator.speed = animSpeed;
        
            yield return new WaitForSecondsRealtime(duration);
            
            // 코루틴 종료 전 액터와 애니메이터가 여전히 유효한지 확인
            if (actor != null && animator != null)
            {
                animator.speed = originalSpeed;
            }
            
            // 딕셔너리에서 제거
            if (actor != null)
            {
                _actorHitStopCoroutines.Remove(actor);
            }
        }

        private async void LoadVolume()
        {
            // 이미 로드 중이거나 로드된 상태라면 중복 실행 방지
            if (_volumeHandle.IsValid() || _volume != null)
            {
                return;
            }

            _volumeHandle = Addressables.LoadAssetAsync<GameObject>("SlowMoveVolume");
    
            try
            {
                GameObject go = await _volumeHandle.Task;

                if (go == null)
                {
                    Debug.LogError("[CameraManager] SlowMoveVolume 에셋이 Null입니다.");
                    return;
                }

                _volumeInstance = Instantiate(go, transform.position, Quaternion.identity, transform);
                _volumeInstance.name = "Action_SlowMo_Volume";
        
                _volume = _volumeInstance.GetComponent<Volume>();
        
                if (_volume != null)
                {
                    _volume.weight = 0f;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[CameraManager] LoadVolume 로드 실패: {e.Message}");
        
                // 로드 실패 시 핸들 메모리 해제
                if (_volumeHandle.IsValid())
                {
                    Addressables.Release(_volumeHandle);
                }
            }
        }

        private IEnumerator HitStopCoroutine(float duration, float timeScale)
        {
            _isHitStopping = true;
            
            // HitStop 적용
            Time.timeScale = timeScale;
            
            // 실제 시간 기준으로 대기
            yield return new WaitForSecondsRealtime(duration);
            
            Time.timeScale = NORMAL_TIME_SCALE;
            
            _isHitStopping = false;
            _currentHitStopCoroutine = null;
        }
    }
}