using System;
using UnityEngine;

namespace UPlayGround.Data.Event
{
    /// <summary>
    /// MotionEvent 인스펙터에서 같은 이벤트의 다른 enum/bool 필드 값에 따라 이 필드를 조건부로 표시한다.
    /// MotionSet Editor의 이벤트 속성 패널이 해석하며, 조건 필드를 찾지 못하면 항상 표시한다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class MotionEventShowIfAttribute : PropertyAttribute
    {
        /// <summary>조건이 되는 형제 필드 이름.</summary>
        public string ConditionFieldName { get; }

        /// <summary>이 값들 중 하나와 일치할 때만 표시한다. enum은 정수 값, bool은 0/1로 비교한다.</summary>
        public int[] VisibleValues { get; }

        public MotionEventShowIfAttribute(string conditionFieldName, params int[] visibleValues)
        {
            ConditionFieldName = conditionFieldName;
            VisibleValues = visibleValues ?? Array.Empty<int>();
        }

        public bool IsVisible(int conditionValue)
        {
            if (VisibleValues.Length == 0)
                return true;

            for (int i = 0; i < VisibleValues.Length; i++)
            {
                if (VisibleValues[i] == conditionValue)
                    return true;
            }
            return false;
        }
    }
}
