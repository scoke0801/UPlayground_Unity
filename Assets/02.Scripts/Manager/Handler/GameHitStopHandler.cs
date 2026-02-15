using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace UPlayGround.Manager.Handler
{
    /// <summary>
    /// HitStop(타격 정지) 효과를 전역적으로 관리하는 핸들러
    /// Time.timeScale을 조작
    /// </summary>
    public class GameHitStopHandler : GameHandlerBase
    {
        /// <summary>
        /// HitStop 강도 Enum
        /// </summary>
        public enum HitStopIntensity
        {
            Light,      // 0.05초 - 약 공격
            Medium,     // 0.08초 - 중 공격
            Heavy,      // 0.12초 - 강 공격
            Critical    // 0.15초 - 크리티컬/피니셔
        }
        
        [Header("HitStop Settings")]
        [SerializeField] private float _defaultHitStopDuration = 0.08f;
        [SerializeField] private float _defaultTimeScale = 0.1f;
        
        private Coroutine _currentHitStopCoroutine;
        private bool _isHitStopping;
        
        public bool IsHitStopping => _isHitStopping;

        public override void Init()
        {
            
        }

        public override void AfterInit()
        {
            
        }

        public override void Dispose()
        {
            
        }

        public override void Update()
        {
            
        }

        public override void FixedUpdate()
        {
            
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
                GameManager.Instance.StopCoroutine(_currentHitStopCoroutine);
            }
            
            _currentHitStopCoroutine =  GameManager.Instance.StartCoroutine(HitStopCoroutine(duration, timeScale));
        }
        
        /// <summary>
        /// 현재 HitStop 강제 종료
        /// </summary>
        public void Stop()
        {
            if (_currentHitStopCoroutine != null)
            {
                GameManager.Instance.StopCoroutine(_currentHitStopCoroutine);
                _currentHitStopCoroutine = null;
            }
            
            Time.timeScale = 1.0f;
            _isHitStopping = false;
        }

        private IEnumerator HitStopCoroutine(float duration, float timeScale)
        {
            _isHitStopping = true;
            
            // 이전 timeScale 저장
            float previousTimeScale = Time.timeScale;
            
            // HitStop 적용
            Time.timeScale = timeScale;
            
            // 실제 시간 기준으로 대기
            yield return new WaitForSecondsRealtime(duration);
            
            // 원래 timeScale로 복구
            Time.timeScale = previousTimeScale;
            
            _isHitStopping = false;
            _currentHitStopCoroutine = null;
        }
    }
}