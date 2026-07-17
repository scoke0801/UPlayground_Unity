using System;
using UnityEngine;
using UPlayGround.Data.Event;

namespace UPlayGround.Animation.Editor
{
    /// <summary>
    /// 이벤트 타입별 색상/아이콘 중앙 관리.
    /// 새 이벤트 타입 추가 시 여기만 수정하면 타임라인·인스펙터 양쪽에 반영된다.
    /// </summary>
    public static class MotionEventStyle
    {
        static readonly Color COL_COLLISION  = new Color(1.00f, 0.35f, 0.35f); // 빨강
        static readonly Color COL_PARTICLE   = new Color(1.00f, 0.82f, 0.25f); // 노랑
        static readonly Color COL_CAMERA     = new Color(0.25f, 0.85f, 0.65f); // 민트
        static readonly Color COL_INVINCIBLE = new Color(0.98f, 0.55f, 0.15f); // 주황
        static readonly Color COL_SOUND      = new Color(0.65f, 0.55f, 1.00f); // 보라
        static readonly Color COL_LOOKAT     = new Color(0.35f, 0.75f, 1.00f); // 하늘
        static readonly Color COL_MOVEMENT   = new Color(0.40f, 1.00f, 0.55f); // 초록
        static readonly Color COL_MISC       = new Color(0.60f, 0.60f, 0.65f); // 회색

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
            if (type == typeof(BeginCollisionEvent))     return Make(COL_COLLISION,  "⚔");
            if (type == typeof(DisableCollisionEvent))   return Make(COL_COLLISION,  "⚔");
            if (type == typeof(BeginParticleEvent))      return Make(COL_PARTICLE,   "✦");
            if (type == typeof(CameraEffectEvent))       return Make(COL_CAMERA,     "📷");
            if (type == typeof(CameraLookAtSocketEvent)) return Make(COL_LOOKAT,     "🎯");
            if (type == typeof(InvincibilityEvent))      return Make(COL_INVINCIBLE, "🛡");
            if (type == typeof(PlaySoundEvent))          return Make(COL_SOUND,      "♪");
            if (type == typeof(FootstepEvent))           return Make(COL_SOUND,      "👣");
            if (type == typeof(AddForceEvent))           return Make(COL_MOVEMENT,   "↗");
            if (type == typeof(AnimationSpeedEvent))     return Make(COL_MOVEMENT,   "⏩");
            if (type == typeof(TimeScaleEvent))          return Make(COL_MISC,       "⏱");
            if (type == typeof(ComboWindowEvent))        return Make(COL_INVINCIBLE, "🔓");
            if (type == typeof(CancelWindowEvent))       return Make(COL_LOOKAT,     "✂");
            if (type == typeof(SlashVFXEvent))           return Make(COL_PARTICLE,   "✦");
            if (type == typeof(SpawnProjectileEvent))    return Make(COL_COLLISION,  "🚀");
            if (type == typeof(SpawnSkillEvent))         return Make(COL_PARTICLE,   "⚡");
            if (type == typeof(HealSkillEvent))          return Make(COL_MOVEMENT,   "💚");
            if (type == typeof(HideTargetEvent))         return Make(COL_MISC,       "👁");
            if (type == typeof(FinishAttackEvent))       return Make(COL_COLLISION,  "✔");
            if (type == typeof(SpecialBreakAttackEvent)) return Make(COL_COLLISION,  "◆");
            if (type == typeof(FinishSideViewEvent))     return Make(COL_CAMERA,     "🎬");
            if (type == typeof(CustomCallbackEvent))     return Make(COL_MISC,       "⚙");
            if (type == typeof(LoopEvent))               return Make(COL_LOOKAT,     "🔁");
            return Make(COL_MISC, "▸");
        }

        static EventVisual Make(Color col, string icon) => new EventVisual
        {
            color  = col,
            dimmed = new Color(col.r * 0.4f, col.g * 0.4f, col.b * 0.4f, 0.55f),
            icon   = icon,
        };
    }
}
