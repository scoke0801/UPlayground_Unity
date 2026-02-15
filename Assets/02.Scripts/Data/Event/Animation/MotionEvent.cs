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

        /// <summary>
        /// 이벤트가 특정 시간에 활성화되는지 확인
        /// </summary>
        public bool IsActiveAt(float time) => time >= startTime && time <= endTime;

        /// <summary>
        /// 이벤트의 표시 이름 (에디터용)
        /// </summary>
        public abstract string GetDisplayName();

        /// <summary>
        /// 이벤트의 짧은 설명 (타임라인 바에 표시)
        /// </summary>
        public virtual string GetShortLabel() => GetDisplayName();

        /// <summary>
        /// 이벤트 실행 (런타임)
        /// </summary>
        public abstract void Execute(GameObject target);
        public abstract void OnCompleteEvent(GameObject target);
    }

}