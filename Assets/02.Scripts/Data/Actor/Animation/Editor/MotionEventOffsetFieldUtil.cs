using UnityEngine;
using UPlayGround.Data.Event;

namespace UPlayGround.Animation.Editor
{
    /// <summary>
    /// MotionEvent의 offset/rotation 필드 전용 렌더링에 공통으로 쓰이는 판별·라벨·각도 유틸.
    /// MotionSetDrawer(reflection 기반)와 MotionSetEditor(SerializedProperty 기반)가
    /// 동일 로직을 공유하기 위한 단일 소스다.
    /// </summary>
    public static class MotionEventOffsetFieldUtil
    {
        /// <summary>해당 필드가 로컬 위치 오프셋(Blade/Spawn 기준)으로 그려야 하는 필드인지.</summary>
        public static bool IsLocalOffset(object owner, string fieldName)
        {
            return owner is BeginParticleEvent && fieldName == nameof(BeginParticleEvent.offset)
                   || owner is SlashVFXEvent && fieldName == nameof(SlashVFXEvent.positionOffset);
        }

        /// <summary>해당 필드가 회전 오프셋(Euler)으로 그려야 하는 필드인지.</summary>
        public static bool IsRotationOffset(object owner, string fieldName)
        {
            return owner is BeginParticleEvent && fieldName == nameof(BeginParticleEvent.rotationOffset)
                   || owner is SlashVFXEvent && fieldName == nameof(SlashVFXEvent.rotationOffset);
        }

        /// <summary>위치 오프셋 위젯에 표시할 좌표 공간 라벨.</summary>
        public static string GetLocalOffsetSpaceLabel(object owner)
        {
            return owner is SlashVFXEvent slash && slash.positionSpace == SlashVFXPositionSpace.World
                ? "World"
                : owner is SlashVFXEvent ? "Blade" : "Spawn Point";
        }

        /// <summary>회전 오프셋 위젯에 표시할 좌표 공간 라벨.</summary>
        public static string GetRotationOffsetSpaceLabel(object owner)
        {
            return owner is SlashVFXEvent slash && slash.rotationSpace == SlashVFXRotationSpace.World
                ? "World Euler"
                : owner is SlashVFXEvent ? "Blade Offset" : "Spawn Point Offset";
        }

        /// <summary>각도를 -180~180 범위로 정규화.</summary>
        public static float NormalizeAngle(float angle)
        {
            angle %= 360f;
            if (angle > 180f)
                angle -= 360f;
            if (angle < -180f)
                angle += 360f;
            return angle;
        }
    }
}
