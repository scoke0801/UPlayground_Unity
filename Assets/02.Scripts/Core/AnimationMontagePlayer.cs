using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Animancer;

namespace Core.Animation
{
    /// <summary>
    /// Animation Montage를 재생하는 컴포넌트
    /// Animancer를 사용하여 언리얼 스타일의 Montage 시스템 구현
    /// </summary>
    [RequireComponent(typeof(AnimancerComponent))]
    public class AnimationMontagePlayer : MonoBehaviour
    {
        private AnimancerComponent animancer;
        private AnimationMontage currentMontage;
        private MontageSection currentSection;
        private AnimancerState currentState;
        private Coroutine playCoroutine;
        
        private float montageStartTime;
        private int currentLoopCount;
        private HashSet<float> triggeredEvents = new HashSet<float>();

        // 이벤트
        public event MontageEndedDelegate OnMontageEnded;
        public event MontageEventDelegate OnMontageEvent;

        private void Awake()
        {
            animancer = GetComponent<AnimancerComponent>();
        }

        /// <summary>
        /// Montage 재생
        /// </summary>
        public void PlayMontage(AnimationMontage montage, string startSection = null)
        {
            if (montage == null)
            {
                Debug.LogError("Montage가 null입니다.");
                return;
            }

            // 이전 Montage 중지
            StopMontage();

            currentMontage = montage;
            montageStartTime = Time.time;
            triggeredEvents.Clear();

            // 시작 섹션 결정
            if (string.IsNullOrEmpty(startSection) && montage.Sections.Count > 0)
            {
                currentSection = montage.Sections[0];
            }
            else
            {
                currentSection = montage.GetSection(startSection);
            }

            if (currentSection == null)
            {
                Debug.LogError($"섹션을 찾을 수 없습니다: {startSection}");
                return;
            }

            playCoroutine = StartCoroutine(PlayMontageCoroutine());
        }

        /// <summary>
        /// 특정 섹션으로 점프
        /// </summary>
        public void JumpToSection(string sectionName)
        {
            if (currentMontage == null) return;

            var section = currentMontage.GetSection(sectionName);
            if (section == null)
            {
                Debug.LogWarning($"섹션을 찾을 수 없습니다: {sectionName}");
                return;
            }

            currentSection = section;
            currentLoopCount = 0;
            
            // 새 섹션 재생
            PlaySection(currentSection);
        }

        /// <summary>
        /// Montage 중지
        /// </summary>
        public void StopMontage(float blendOutTime = -1)
        {
            if (playCoroutine != null)
            {
                StopCoroutine(playCoroutine);
                playCoroutine = null;
            }

            if (currentState != null)
            {
                float fadeTime = blendOutTime >= 0 ? blendOutTime : 
                    (currentMontage?.BlendOutTime ?? 0.25f);
                
                // Animancer의 올바른 Fade out 방법
                currentState.StartFade(0, fadeTime);
            }

            var endedMontage = currentMontage;
            currentMontage = null;
            currentSection = null;
            currentState = null;

            OnMontageEnded?.Invoke(endedMontage);
        }

        /// <summary>
        /// Montage 재생 속도 설정
        /// </summary>
        public void SetPlayRate(float rate)
        {
            if (currentState != null)
            {
                currentState.Speed = rate;
            }
        }

        /// <summary>
        /// 현재 재생 중인지 확인
        /// </summary>
        public bool IsPlaying()
        {
            return currentState != null && currentState.IsPlaying;
        }

        /// <summary>
        /// 현재 재생 진행률 (0~1)
        /// </summary>
        public float GetPlaybackPosition()
        {
            if (currentState == null || currentMontage == null) return 0f;
            return currentState.NormalizedTime;
        }

        private IEnumerator PlayMontageCoroutine()
        {
            while (currentSection != null)
            {
                currentLoopCount = 0;

                // 섹션 재생
                do
                {
                    PlaySection(currentSection);

                    // 섹션이 끝날 때까지 대기
                    yield return new WaitWhile(() => 
                        currentState != null && 
                        currentState.IsPlaying && 
                        currentState.NormalizedTime < 1f);

                    currentLoopCount++;

                    // 이벤트 체크
                    CheckAndTriggerEvents();

                } while (ShouldLoopSection());

                // 다음 섹션으로 전환
                if (!string.IsNullOrEmpty(currentSection.NextSection))
                {
                    currentSection = currentMontage.GetSection(currentSection.NextSection);
                }
                else
                {
                    break; // 더 이상 재생할 섹션이 없음
                }
            }

            // Montage 종료
            StopMontage();
        }

        private void PlaySection(MontageSection section)
        {
            if (section?.Clip == null) return;

            currentState = animancer.Play(
                section.Clip, 
                currentMontage.BlendInTime
            );

            if (currentState != null)
            {
                currentState.Speed = section.PlaybackSpeed;
                currentState.Time = section.StartTime;
            }
        }

        private bool ShouldLoopSection()
        {
            if (!currentSection.Loop) return false;
            if (currentSection.LoopCount < 0) return true; // 무한 루프
            return currentLoopCount < currentSection.LoopCount;
        }

        private void CheckAndTriggerEvents()
        {
            if (currentMontage == null || currentState == null) return;

            float currentTime = Time.time - montageStartTime;
            var eventsToTrigger = currentMontage.GetEventsAtTime(currentState.Time);

            foreach (var evt in eventsToTrigger)
            {
                float eventKey = evt.TriggerTime;
                if (!triggeredEvents.Contains(eventKey))
                {
                    triggeredEvents.Add(eventKey);
                    OnMontageEvent?.Invoke(evt.EventName, evt.EventParameter);
                }
            }
        }

        /// <summary>
        /// 현재 재생 중인 Montage
        /// </summary>
        public AnimationMontage GetCurrentMontage()
        {
            return currentMontage;
        }

        /// <summary>
        /// 현재 재생 중인 섹션
        /// </summary>
        public MontageSection GetCurrentSection()
        {
            return currentSection;
        }
    }
}
