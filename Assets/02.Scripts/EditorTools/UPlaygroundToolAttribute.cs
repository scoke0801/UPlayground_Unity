using System;

namespace UPlayGround.EditorTools
{
    /// <summary>
    /// UPlayGround 툴 런처가 자동 발견할 에디터 도구 실행 메서드를 표시한다.
    /// Unity 상단 메뉴에는 노출하지 않으며, 정적 매개변수 없는 메서드에만 사용한다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class UPlaygroundToolAttribute : Attribute
    {
        public string Id { get; }
        public bool IsValidateFunction { get; }

        // 기존 MenuItem의 명명 인수와 호환해 기계적인 이관이 가능하도록 유지한다.
        public int priority { get; set; }

        public UPlaygroundToolAttribute(string id)
            : this(id, false, 0)
        {
        }

        public UPlaygroundToolAttribute(string id, bool isValidateFunction)
            : this(id, isValidateFunction, 0)
        {
        }

        public UPlaygroundToolAttribute(string id, bool isValidateFunction, int priority)
        {
            Id = id;
            IsValidateFunction = isValidateFunction;
            this.priority = priority;
        }
    }
}
