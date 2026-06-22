using System;
using UnityEngine;

namespace UPlayGround.Data.Event
{
    /// <summary>
    /// 모션 이벤트 기본 추상 클래스
    /// 모든 모션 이벤트는 이 클래스를 상속받아야 함
    /// </summary>
    [Serializable]
    public abstract class MotionEventBase
    {
        public float startTime;
        public float endTime;
            
        // 이전 모션들의 누적 시간 (글로벌 타임라인에서의 오프셋)
        [HideInInspector] public float globalStartTimeOffset = 0f;
        
        /// <summary>
        /// 이벤트가 특정 시간(글로벌 시간)에 활성화되는지 확인
        /// 앞선 모션에서 흐른 시간을 더해 절대적인 글로벌 시간으로 비교
        /// </summary>
        public bool IsActiveAtGlobal(float globalTime)
        {
            float absoluteStartTime = startTime + globalStartTimeOffset;
            float absoluteEndTime = endTime + globalStartTimeOffset;
            
            return globalTime >= absoluteStartTime && globalTime <= absoluteEndTime;
        }
        
        /// <summary>
        /// 이벤트가 특정 시간(글로벌 시간)에 활성화되는지 확인
        /// 앞선 모션에서 흐른 시간을 더해 절대적인 글로벌 시간으로 비교
        /// </summary>
        public bool IsActiveAt(float localTime)
        {
            return localTime >= startTime && localTime <= endTime;
        }
        
        /// <summary>
        /// 이벤트의 표시 이름 (에디터용)
        /// </summary>
        public abstract string GetDisplayName();

        /// <summary>
        /// 이벤트의 짧은 설명 (타임라인 바에 표시)
        /// </summary>
        public virtual string GetShortLabel() => GetDisplayName();

        /// <summary>
        /// true면 이벤트 발화 결정(Update)과 실제 Execute를 분리해, 본(스켈레톤) 평가가 끝난 뒤
        /// LateUpdate에서 실행한다. 블레이드 본 등 라이브 트랜스폼의 월드 포즈를 즉석 샘플링하는
        /// 공간 이벤트(SlashVFX 등)는 Update 시점에 실행하면 직전 프레임 포즈를 읽어 위치가
        /// 프레임 타이밍에 따라 흔들리므로 반드시 본 평가 후에 샘플링해야 한다.
        /// 콜리전/워프/Freeze 등 타이밍이 민감한 이벤트는 false를 유지해 기존 Update 타이밍을 보존한다.
        /// </summary>
        public virtual bool RequiresPostEvaluation => false;

        /// <summary>
        /// 이벤트 실행 (런타임)
        /// </summary>
        public abstract void Execute(GameObject target);
        public abstract void OnCompleteEvent(GameObject target);
    }

}