using System;

namespace UPlayGround.Data.Event
{
    /// <summary>
    /// 프로젝트별 구체 이벤트가 MotionSet Editor에 표시 메타데이터를 제공하는 선택적 특성.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class MotionEventDescriptorAttribute : Attribute
    {
        public string DisplayName { get; }
        public string Category { get; }
        public int Order { get; }

        public MotionEventDescriptorAttribute(
            string displayName,
            string category = "기타",
            int order = 0)
        {
            DisplayName = displayName;
            Category = category;
            Order = order;
        }
    }
}
