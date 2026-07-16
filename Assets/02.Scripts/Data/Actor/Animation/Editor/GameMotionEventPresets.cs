using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Event;

namespace UPlayGround.Animation.Editor
{
    /// <summary>
    /// 게임 빌트인 MotionEvent 프리셋.
    /// 패키지 팝업(MotionEventAddPopup)에 제공자로 등록된다 — 구체 이벤트 타입은 게임 어셈블리에 있으므로
    /// 패키지가 직접 생성할 수 없고, 이 파일이 팩토리를 제공한다.
    /// </summary>
    [InitializeOnLoad]
    internal static class GameMotionEventPresets
    {
        static GameMotionEventPresets()
        {
            MotionEventPresetProviders.Register(Create);
        }

        static IEnumerable<MotionEventAddPopup.EventPreset> Create()
        {
            yield return new MotionEventAddPopup.EventPreset(
                "melee_basic",
                "근접 공격 기본",
                "Collision, ComboWindow를 기본 타이밍으로 추가합니다.",
                start => new MotionEventBase[]
                {
                    Timed(new BeginCollisionEvent(), start + 0.10f, 0.15f),
                    Timed(new ComboWindowEvent(), start + 0.25f, 0.25f),
                },
                "melee", "attack", "combo", "근접", "공격", "콤보");

            yield return new MotionEventAddPopup.EventPreset(
                "camera_melee",
                "카메라 연출 공격",
                "CameraEffect, Collision, FinishAttack을 함께 추가합니다.",
                start => new MotionEventBase[]
                {
                    Timed(new CameraEffectEvent(), start + 0.00f, 0.25f),
                    Timed(new BeginCollisionEvent(), start + 0.12f, 0.15f),
                    Timed(new FinishAttackEvent(), start + 0.45f, 0.05f),
                },
                "camera", "shake", "attack", "카메라", "연출");

            yield return new MotionEventAddPopup.EventPreset(
                "special_break_attack",
                "브레이크 특수공격",
                "SpecialBreakAttackEvent와 CameraEffect를 기본 타이밍으로 추가합니다.",
                start => new MotionEventBase[]
                {
                    Timed(new CameraEffectEvent(), start + 0.00f, 0.25f),
                    Timed(new SpecialBreakAttackEvent(), start + 0.18f, 0.05f),
                },
                "break", "special", "finish", "브레이크", "특수공격", "처형");

            yield return new MotionEventAddPopup.EventPreset(
                "slash_vfx_basic",
                "Slash VFX 기본",
                "무기 Blade_Base / Blade_Tip 기준으로 Slash VFX를 1회 생성합니다.",
                start => new MotionEventBase[]
                {
                    Timed(new SlashVFXEvent(), start + 0.00f, 0.05f),
                },
                "slash", "weapon", "blade", "vfx", "검기", "참격");

            yield return new MotionEventAddPopup.EventPreset(
                "projectile_basic",
                "투사체 발사 기본",
                "PlaySound와 SpawnProjectile을 기본 타이밍으로 추가합니다.",
                start => new MotionEventBase[]
                {
                    Timed(new PlaySoundEvent(), start + 0.00f, 0.05f),
                    Timed(new SpawnProjectileEvent(), start + 0.10f, 0.05f),
                },
                "projectile", "shoot", "arrow", "bullet", "투사체", "발사");
        }

        static MotionEventBase Timed(MotionEventBase evt, float start, float duration)
        {
            evt.startTime = start;
            evt.endTime = start + Mathf.Max(0.01f, duration);
            return evt;
        }
    }
}
