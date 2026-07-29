using System;
using UPlayGround.Data.Event;

namespace UPlayGround.Animation.Editor
{
    /// <summary>
    /// SlashVFXEvent의 offset 필드 좌표 공간 선언.
    /// 프로젝트 어셈블리에 있으므로 필드/enum을 타입 안전하게 참조한다.
    /// </summary>
    public sealed class SlashVFXOffsetFieldProvider : IMotionEventOffsetFieldProvider
    {
        public Type EventType => typeof(SlashVFXEvent);

        public bool IsLocalOffset(object motionEvent, string fieldName) =>
            motionEvent is SlashVFXEvent &&
            fieldName == nameof(SlashVFXEvent.positionOffset);

        public bool IsRotationOffset(object motionEvent, string fieldName) =>
            motionEvent is SlashVFXEvent &&
            fieldName == nameof(SlashVFXEvent.rotationOffset);

        public string GetLocalOffsetSpaceLabel(object motionEvent) =>
            motionEvent is SlashVFXEvent slash &&
            slash.positionSpace == SlashVFXPositionSpace.World
                ? "World"
                : "Blade";

        public string GetRotationOffsetSpaceLabel(object motionEvent) =>
            motionEvent is SlashVFXEvent slash &&
            slash.rotationSpace == SlashVFXRotationSpace.World
                ? "World Euler"
                : "Blade Offset";
    }

    /// <summary>
    /// BeginParticleEvent의 offset 필드 좌표 공간 선언.
    /// 파티클은 항상 Spawn Point 기준이라 좌표 공간 선택지가 없다.
    /// </summary>
    public sealed class BeginParticleOffsetFieldProvider :
        IMotionEventOffsetFieldProvider
    {
        public Type EventType => typeof(BeginParticleEvent);

        public bool IsLocalOffset(object motionEvent, string fieldName) =>
            motionEvent is BeginParticleEvent &&
            fieldName == nameof(BeginParticleEvent.offset);

        public bool IsRotationOffset(object motionEvent, string fieldName) =>
            motionEvent is BeginParticleEvent &&
            fieldName == nameof(BeginParticleEvent.rotationOffset);

        public string GetLocalOffsetSpaceLabel(object motionEvent) => "Spawn Point";

        public string GetRotationOffsetSpaceLabel(object motionEvent) =>
            "Spawn Point Offset";
    }
}
