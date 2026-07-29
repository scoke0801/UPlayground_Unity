using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.Event;

namespace UPlayGround.Animation.Editor
{
    /// <summary>
    /// 이벤트 타입별 색상/아이콘 중앙 관리.
    /// 프로젝트 타입을 직접 참조하지 않고 <see cref="MotionEventDescriptorAttribute"/>의
    /// Category / DisplayName만 사용한다. 새 이벤트 타입은 어트리뷰트만 붙이면 되고,
    /// 새 카테고리를 도입할 때만 여기 테이블을 늘린다.
    /// </summary>
    public static class MotionEventStyle
    {
        static readonly Color COL_COMBAT   = new(1.00f, 0.35f, 0.35f); // 빨강
        static readonly Color COL_VFX      = new(1.00f, 0.82f, 0.25f); // 노랑
        static readonly Color COL_CAMERA   = new(0.25f, 0.85f, 0.65f); // 민트
        static readonly Color COL_MOVEMENT = new(0.40f, 1.00f, 0.55f); // 초록
        static readonly Color COL_UTILITY  = new(0.60f, 0.60f, 0.65f); // 회색
        static readonly Color COL_UNKNOWN  = new(0.50f, 0.52f, 0.58f);

        /// <summary>카테고리별 기본 색상.</summary>
        static readonly Dictionary<string, Color> CategoryColors =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["Combat"] = COL_COMBAT,
                ["VFX / SFX"] = COL_VFX,
                ["Camera"] = COL_CAMERA,
                ["Movement / Time"] = COL_MOVEMENT,
                ["Utility"] = COL_UTILITY,
            };

        /// <summary>
        /// DisplayName별 아이콘. 카테고리 색상 위에 얹는 세부 구분이며,
        /// 없으면 카테고리 아이콘으로 폴백한다.
        /// </summary>
        static readonly Dictionary<string, string> DisplayIcons =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["Collision"] = "⚔",
                ["DisableCollision"] = "⚔",
                ["ComboWindow"] = "🔓",
                ["CancelWindow"] = "✂",
                ["FinishAttack"] = "✔",
                ["SpecialBreakAttack"] = "◆",
                ["Invincibility"] = "🛡",
                ["HealSkill"] = "💚",
                ["Interaction"] = "🤝",
                ["Particle"] = "✦",
                ["SlashVFX"] = "✦",
                ["Afterimage"] = "❃",
                ["SpawnProjectile"] = "🚀",
                ["SpawnSkill"] = "⚡",
                ["PlaySound"] = "♪",
                ["Footstep"] = "👣",
                ["CameraEffect"] = "📷",
                ["CameraLookAtSocket"] = "🎯",
                ["FinishSideView"] = "🎬",
                ["AddForce"] = "↗",
                ["AnimationSpeed"] = "⏩",
                ["TimeScale"] = "⏱",
                ["MotionWarp"] = "➤",
                ["Loop"] = "🔁",
                ["HideTarget"] = "👁",
                ["FreezeEnemy"] = "❄",
                ["CustomCallback"] = "⚙",
            };

        static readonly Dictionary<string, string> CategoryIcons =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["Combat"] = "⚔",
                ["VFX / SFX"] = "✦",
                ["Camera"] = "📷",
                ["Movement / Time"] = "↗",
                ["Utility"] = "⚙",
            };

        public struct EventVisual
        {
            public Color  color;   // 바 강조색 (solid)
            public Color  dimmed;  // 바 배경색 (alpha 낮춤)
            public string icon;    // 트랙 레이블 아이콘
        }

        public static EventVisual Get(MotionEventBase evt)
        {
            if (evt == null) return Make(COL_UNKNOWN, "?");
            return GetByType(evt.GetType());
        }

        public static EventVisual GetByType(Type type)
        {
            if (type == null)
                return Make(COL_UNKNOWN, "?");

            MotionEventDescriptorAttribute descriptor =
                (MotionEventDescriptorAttribute)Attribute.GetCustomAttribute(
                    type,
                    typeof(MotionEventDescriptorAttribute));
            string category = descriptor?.Category;
            string displayName = descriptor?.DisplayName;

            Color color = category != null &&
                          CategoryColors.TryGetValue(category, out Color mapped)
                ? mapped
                : COL_UNKNOWN;

            string icon = null;
            if (displayName != null)
                DisplayIcons.TryGetValue(displayName, out icon);
            if (icon == null && category != null)
                CategoryIcons.TryGetValue(category, out icon);

            return Make(color, icon ?? "▸");
        }

        static EventVisual Make(Color col, string icon) => new EventVisual
        {
            color  = col,
            dimmed = new Color(col.r * 0.4f, col.g * 0.4f, col.b * 0.4f, 0.55f),
            icon   = icon,
        };
    }
}
