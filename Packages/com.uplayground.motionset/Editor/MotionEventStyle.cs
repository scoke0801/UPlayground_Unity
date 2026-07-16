using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.Event;

namespace UPlayGround.Animation.Editor
{
    /// <summary>
    /// 이벤트 타입별 색상/아이콘 중앙 관리.
    /// 각 이벤트 클래스의 [MotionEventMeta] Color/Icon을 읽어 타임라인·인스펙터 양쪽에 반영한다.
    /// 미지정 타입은 기본값(회색 ▸)으로 표시된다.
    /// </summary>
    public static class MotionEventStyle
    {
        static readonly Color COL_MISC = new Color(0.60f, 0.60f, 0.65f); // 회색

        static readonly Dictionary<Type, EventVisual> _cache = new Dictionary<Type, EventVisual>();

        public struct EventVisual
        {
            public Color  color;   // 바 강조색 (solid)
            public Color  dimmed;  // 바 배경색 (alpha 낮춤)
            public string icon;    // 트랙 레이블 아이콘
        }

        public static EventVisual Get(MotionEventBase evt)
        {
            if (evt == null) return Make(COL_MISC, "?");
            return GetByType(evt.GetType());
        }

        public static EventVisual GetByType(Type type)
        {
            if (type == null) return Make(COL_MISC, "?");
            if (_cache.TryGetValue(type, out var cached)) return cached;

            var meta = (MotionEventMetaAttribute)Attribute.GetCustomAttribute(type, typeof(MotionEventMetaAttribute));

            Color color = meta?.Color != null && meta.Color.Length >= 3
                ? new Color(meta.Color[0], meta.Color[1], meta.Color[2])
                : COL_MISC;
            string icon = string.IsNullOrEmpty(meta?.Icon) ? "▸" : meta.Icon;

            var visual = Make(color, icon);
            _cache[type] = visual;
            return visual;
        }

        static EventVisual Make(Color col, string icon) => new EventVisual
        {
            color  = col,
            dimmed = new Color(col.r * 0.4f, col.g * 0.4f, col.b * 0.4f, 0.55f),
            icon   = icon,
        };
    }
}
