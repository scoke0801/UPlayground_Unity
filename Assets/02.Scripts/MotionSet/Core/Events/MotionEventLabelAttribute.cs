using System;
using UnityEngine;

namespace UPlayGround.Data.Event
{
    /// <summary>
    /// MotionEvent 인스펙터에 표시할 한국어 필드 이름을 지정한다.
    /// 직렬화 이름은 그대로 두고 표시 라벨만 바꾸므로 기존 에셋에 영향이 없다.
    /// MotionSet Editor의 이벤트 속성 패널이 해석하며, 없으면 Unity 기본 표시 이름을 사용한다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class MotionEventLabelAttribute : PropertyAttribute
    {
        public string Label { get; }

        public MotionEventLabelAttribute(string label)
        {
            Label = label;
        }
    }
}
