using System;
using UnityEngine;

namespace UPlayGround.Animation
{
    public enum MotionSectionEndPolicy
    {
        Continue,
        Stop,
        Hold,
        LoopSelf,
    }

    public enum MotionSetEndReason
    {
        Completed,
        Interrupted,
        Stopped,
        Invalidated,
    }

    public enum MotionInterruptionPolicy
    {
        Allow,
        InterruptSameGroup,
        RejectWhilePlaying,
    }

    public enum MotionMarkerKind
    {
        Generic,
        Anticipation,
        Impact,
        Recovery,
        CancelOpen,
        CancelClose,
        LeftFoot,
        RightFoot,
    }

    public enum MotionCurveChannel
    {
        PlaybackRate,
        LayerWeight,
        WarpTranslationWeight,
        WarpRotationWeight,
        CameraWeight,
        VfxIntensity,
        TimeStretch,
    }

    public enum MotionSyncRole
    {
        CanLead,
        Leader,
        Follower,
    }

    public enum MotionSyncFallback
    {
        None,
        NormalizedTime,
    }

    [Serializable]
    public sealed class MotionSection
    {
        public string id;
        public string displayName = "Section";
        [Min(0f)] public float startTime;
        public string defaultNextId;
        public MotionSectionEndPolicy endPolicy = MotionSectionEndPolicy.Continue;
    }

    [Serializable]
    public sealed class MotionMarker
    {
        public string id;
        public string displayName = "Marker";
        [Range(0f, 1f)] public float normalizedTime;
        public MotionMarkerKind kind;
    }

    [Serializable]
    public sealed class MotionCurveTrack
    {
        public string id;
        public string displayName = "Curve";
        public MotionCurveChannel channel;
        public string targetId;
        public bool enabled = true;
        public AnimationCurve curve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

        public float Evaluate(float normalizedTime, float fallback = 1f)
        {
            return enabled && curve != null
                ? curve.Evaluate(Mathf.Clamp01(normalizedTime))
                : fallback;
        }
    }

    [Serializable]
    public sealed class MotionSyncSettings
    {
        public string groupId;
        public MotionSyncRole role = MotionSyncRole.CanLead;
        public MotionSyncFallback fallback = MotionSyncFallback.NormalizedTime;
        public bool triggerFollowerEvents;
    }

    [Serializable]
    public sealed class MotionTimeStretchSettings
    {
        public bool enabled;
        public bool protectImpact = true;
        [Min(0f)] public float protectionBefore = 0.03f;
        [Min(0f)] public float protectionAfter = 0.05f;
        [Min(0.01f)] public float minimumRate = 0.1f;
        [Min(0.01f)] public float maximumRate = 4f;
    }

    [Serializable]
    public readonly struct MotionSectionRange
    {
        public readonly MotionSection section;
        public readonly float startTime;
        public readonly float endTime;

        public MotionSectionRange(MotionSection section, float startTime, float endTime)
        {
            this.section = section;
            this.startTime = startTime;
            this.endTime = endTime;
        }

        public float Duration => Mathf.Max(0f, endTime - startTime);
        public bool Contains(float time) => time >= startTime && time < endTime;
    }

    public readonly struct MotionPlaybackRequest
    {
        public readonly MotionSetAsset asset;
        public readonly string startSectionId;
        public readonly float playRate;
        public readonly float? blendInOverride;

        public MotionPlaybackRequest(
            MotionSetAsset asset,
            string startSectionId = null,
            float playRate = 1f,
            float? blendInOverride = null)
        {
            this.asset = asset;
            this.startSectionId = startSectionId;
            this.playRate = playRate > 0f ? playRate : 1f;
            this.blendInOverride = blendInOverride;
        }
    }
}
