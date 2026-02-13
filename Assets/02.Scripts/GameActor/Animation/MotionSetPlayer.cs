using UnityEngine;
using Animancer;

namespace UPlayGround.Animation
{
    /// <summary>
    /// Animancer를 사용한 MotionSet 재생 테스트
    /// </summary>
    [RequireComponent(typeof(AnimancerComponent))]
    public class MotionSetPlayer : MonoBehaviour
    {
        [Header("Animancer Setup")]
        [SerializeField] AnimancerComponent _animancer;
        
        [Header("Motion Set")]
        [SerializeField] MotionSetAsset _motionSetAsset;
        
        [Header("Event Executor")]
        [SerializeField] MotionEventExecutor _eventExecutor;
        
        [Header("Test Controls")]
        [SerializeField] KeyCode _playKey = KeyCode.Space;
        [SerializeField] KeyCode _stopKey = KeyCode.Escape;
        
        private AnimancerState _currentState;
        private MotionSet CurrentMotionSet => _motionSetAsset?.motionSet;
        private int _currentMotionIndex;
        private float _globalTime;

        void Reset()
        {
            _animancer = GetComponent<AnimancerComponent>();
        }

        void Awake()
        {
            if (_animancer == null)
                _animancer = GetComponent<AnimancerComponent>();
            
            if (_eventExecutor == null)
                _eventExecutor = GetComponent<MotionEventExecutor>();
        }

        void Update()
        {
            // 테스트 입력
            if (UnityEngine.Input.GetKeyDown(_playKey))
            {
                PlayMotionSet();
            }
            
            if (UnityEngine.Input.GetKeyDown(_stopKey))
            {
                Stop();
            }

            // 타임라인 업데이트
            UpdateTimeline();
        }

        /// <summary>
        /// MotionSet 재생 시작
        /// </summary>
        public void PlayMotionSet()
        {
            if (CurrentMotionSet == null || !CurrentMotionSet.IsValid())
            {
                Debug.LogWarning("[MotionSetPlayer] Invalid MotionSet");
                return;
            }

            _currentMotionIndex = 0;
            _globalTime = 0f;
            
            // 이벤트 실행기 초기화
            _eventExecutor?.PlayMotionSet(CurrentMotionSet);
            
            // 첫 번째 모션 재생
            PlayMotionAtIndex(0);
            
            Debug.Log($"[MotionSetPlayer] Started playing: {CurrentMotionSet.motionSetName}");
        }

        /// <summary>
        /// 특정 인덱스의 모션 재생
        /// </summary>
        void PlayMotionAtIndex(int index)
        {
            if (CurrentMotionSet.motions == null || 
                index < 0 || 
                index >= CurrentMotionSet.motions.Count)
            {
                Debug.Log("[MotionSetPlayer] MotionSet playback completed");
                return;
            }

            var motion = CurrentMotionSet.motions[index];
            if (motion == null || !motion.IsValid())
            {
                Debug.LogWarning($"[MotionSetPlayer] Invalid motion at index {index}");
                _currentMotionIndex++;
                PlayMotionAtIndex(_currentMotionIndex);
                return;
            }

            _currentMotionIndex = index;
            
            // Animancer로 애니메이션 재생
            _currentState = _animancer.Play(motion.motionClip);
            
            // 모션 종료 시 다음 모션으로 전환
            _currentState.Events(this).OnEnd ??= OnMotionEnd;
            
            Debug.Log($"[MotionSetPlayer] Playing motion [{index}]: {motion.motionName} ({motion.Duration:F2}s)");
        }

        /// <summary>
        /// 모션 종료 콜백
        /// </summary>
        void OnMotionEnd()
        {
            _currentMotionIndex++;
            
            // 다음 모션이 있으면 재생, 없으면 종료
            if (_currentMotionIndex < CurrentMotionSet.motions.Count)
            {
                PlayMotionAtIndex(_currentMotionIndex);
            }
            else
            {
                Debug.Log("[MotionSetPlayer] MotionSet playback finished");
                _currentState = null;
            }
        }

        /// <summary>
        /// 타임라인 시간 업데이트
        /// </summary>
        void UpdateTimeline()
        {
            if (_currentState == null || CurrentMotionSet == null) return;

            // 현재 모션까지의 누적 시간 계산
            float accumulatedTime = 0f;
            for (int i = 0; i < _currentMotionIndex; i++)
            {
                if (CurrentMotionSet.motions[i] != null)
                    accumulatedTime += CurrentMotionSet.motions[i].Duration;
            }

            // 글로벌 타임라인 시간 = 누적 시간 + 현재 모션의 재생 시간
            _globalTime = accumulatedTime + _currentState.Time;
            
            // 이벤트 실행기에 시간 전달
            _eventExecutor?.UpdateTime(_globalTime);
        }

        /// <summary>
        /// 재생 정지
        /// </summary>
        public void Stop()
        {
            _animancer.Stop();
            _currentState = null;
            _currentMotionIndex = 0;
            _globalTime = 0f;
            _eventExecutor?.Stop();
            
            Debug.Log("[MotionSetPlayer] Stopped");
        }

        /// <summary>
        /// 특정 시간으로 이동 (에디터 프리뷰용)
        /// </summary>
        public void SeekToTime(float targetTime)
        {
            if (CurrentMotionSet == null) return;

            // 타겟 시간에 해당하는 모션 찾기
            if (CurrentMotionSet.GetMotionAtTime(targetTime, out int motionIndex, out float localTime))
            {
                _currentMotionIndex = motionIndex;
                _globalTime = targetTime;
                
                var motion = CurrentMotionSet.motions[motionIndex];
                _currentState = _animancer.Play(motion.motionClip);
                _currentState.Time = localTime;
                _currentState.Speed = 0; // 프리뷰 모드는 정지
                
                // 이벤트 실행기도 동기화
                _eventExecutor?.SeekTo(targetTime);
                
                Debug.Log($"[MotionSetPlayer] Seeked to {targetTime:F2}s (Motion {motionIndex}, Local {localTime:F2}s)");
            }
        }

#if UNITY_EDITOR
        void OnGUI()
        {
            if (CurrentMotionSet == null) return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 150));
            GUILayout.Box("MotionSet Player - Debug Info");
            GUILayout.Label($"MotionSet: {CurrentMotionSet.motionSetName}");
            GUILayout.Label($"Total Duration: {CurrentMotionSet.TotalDuration:F2}s");
            GUILayout.Label($"Current Motion: {_currentMotionIndex}");
            GUILayout.Label($"Global Time: {_globalTime:F2}s");
            
            if (_currentState != null)
            {
                GUILayout.Label($"Current Clip: {_currentState.Clip.name}");
                GUILayout.Label($"Local Time: {_currentState.Time:F2}s");
            }
            
            GUILayout.Label($"\nControls:");
            GUILayout.Label($"Play: {_playKey}");
            GUILayout.Label($"Stop: {_stopKey}");
            GUILayout.EndArea();
        }
#endif
    }
}