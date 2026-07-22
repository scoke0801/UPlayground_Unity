using System;
using UnityEngine;

namespace UPlayGround.FlowGraph
{
    /// <summary>
    /// string 필드를 그래프 Blackboard 선언 변수 드롭다운으로 그리게 하는 마커.
    /// 문자열 직접 입력으로 인한 리네임 유실을 방지한다 (BT BlackboardKeySelector 참조).
    /// </summary>
    public sealed class FlowVariableNameAttribute : PropertyAttribute
    {
    }

    public enum FlowVariableType
    {
        Bool = 0,
        Int = 1,
        Float = 2,
        String = 3,
    }

    /// <summary>
    /// 그래프 스코프 블랙보드 변수 선언 (FlowCanvas Blackboard 참조).
    /// 에셋에는 선언+기본값만 저장되고, 실제 값은 발화마다 FlowContext 블랙보드에 복사되어
    /// 실행 간 오염이 없다. 노드는 이름으로 참조한다.
    /// </summary>
    [Serializable]
    public sealed class FlowVariableDef
    {
        public string name;
        public FlowVariableType type = FlowVariableType.Bool;

        public bool boolValue;
        public int intValue;
        public float floatValue;
        public string stringValue;

        public object GetDefaultValue()
        {
            return type switch
            {
                FlowVariableType.Bool => boolValue,
                FlowVariableType.Int => intValue,
                FlowVariableType.Float => floatValue,
                FlowVariableType.String => stringValue ?? string.Empty,
                _ => null,
            };
        }
    }

    /// <summary>변수 노드가 공유하는 타입별 값 필드 세트 (비교/대입 값 저작용).</summary>
    [Serializable]
    public sealed class FlowVariableValue
    {
        public FlowVariableType type = FlowVariableType.Bool;
        public bool boolValue;
        public int intValue;
        public float floatValue;
        public string stringValue;

        public object Get()
        {
            return type switch
            {
                FlowVariableType.Bool => boolValue,
                FlowVariableType.Int => intValue,
                FlowVariableType.Float => floatValue,
                FlowVariableType.String => stringValue ?? string.Empty,
                _ => null,
            };
        }

        public bool Matches(object blackboardValue)
        {
            return type switch
            {
                FlowVariableType.Bool => blackboardValue is bool b && b == boolValue,
                FlowVariableType.Int => blackboardValue is int i && i == intValue,
                FlowVariableType.Float => blackboardValue is float f && Mathf.Approximately(f, floatValue),
                FlowVariableType.String => blackboardValue is string s && s == (stringValue ?? string.Empty),
                _ => false,
            };
        }

        public override string ToString()
        {
            return type switch
            {
                FlowVariableType.Bool => boolValue.ToString(),
                FlowVariableType.Int => intValue.ToString(),
                FlowVariableType.Float => floatValue.ToString("0.###"),
                FlowVariableType.String => stringValue,
                _ => string.Empty,
            };
        }
    }
}
