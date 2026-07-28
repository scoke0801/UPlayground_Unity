using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UPlayGround.Data.Event;

namespace UPlayGround.Animation.Editor
{
    public readonly struct MotionEventDescriptor
    {
        public Type EventType { get; }
        public string DisplayName { get; }
        public string Category { get; }
        public int Order { get; }

        public MotionEventDescriptor(
            Type eventType,
            string displayName,
            string category,
            int order)
        {
            EventType = eventType;
            DisplayName = displayName;
            Category = category;
            Order = order;
        }
    }

    /// <summary>
    /// Core를 상속한 이벤트를 어셈블리 경계와 무관하게 검색하는 범용 카탈로그.
    /// </summary>
    public static class MotionEventCatalog
    {
        private static IReadOnlyList<MotionEventDescriptor> _descriptors;

        public static IReadOnlyList<MotionEventDescriptor> Descriptors =>
            _descriptors ??= Build();

        public static void Refresh()
        {
            _descriptors = Build();
        }

        public static MotionEventBase Create(Type eventType)
        {
            if (eventType == null ||
                eventType.IsAbstract ||
                !typeof(MotionEventBase).IsAssignableFrom(eventType))
                return null;
            return Activator.CreateInstance(eventType) as MotionEventBase;
        }

        private static IReadOnlyList<MotionEventDescriptor> Build()
        {
            // 중첩 private 타입은 테스트/검증용 더미이므로 저작 카탈로그에서 제외한다.
            return TypeCache.GetTypesDerivedFrom<MotionEventBase>()
                .Where(type => type.IsClass && !type.IsAbstract && !type.IsNestedPrivate)
                .Select(CreateDescriptor)
                .OrderBy(descriptor => descriptor.Category, StringComparer.Ordinal)
                .ThenBy(descriptor => descriptor.Order)
                .ThenBy(descriptor => descriptor.DisplayName, StringComparer.Ordinal)
                .ToArray();
        }

        private static MotionEventDescriptor CreateDescriptor(Type type)
        {
            MotionEventDescriptorAttribute attribute =
                type.GetCustomAttributes(
                        typeof(MotionEventDescriptorAttribute),
                        false)
                    .FirstOrDefault() as MotionEventDescriptorAttribute;
            string displayName = !string.IsNullOrEmpty(attribute?.DisplayName)
                ? attribute.DisplayName
                : GetFriendlyName(type);
            return new MotionEventDescriptor(
                type,
                displayName,
                attribute?.Category ?? "기타",
                attribute?.Order ?? 0);
        }

        private static string GetFriendlyName(Type type)
        {
            string name = type.Name;
            if (name.EndsWith("Event", StringComparison.Ordinal))
                name = name[..^5];
            return name;
        }
    }
}
