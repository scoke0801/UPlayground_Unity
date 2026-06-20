using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Data;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Component
{
    public readonly struct PlayerResidualAttackSnapshot
    {
        public readonly PlayerActor OwnerPlayer;
        public readonly CharacterModelData SourceModel;
        public readonly CharacterActorType CharacterType;
        public readonly AttackData CurrentAttackData;
        public readonly AttackInfoBase CurrentAttackInfoBase;
        public readonly IReadOnlyList<HitPhaseData> HitPhases;
        public readonly ActorAnimator.MotionPlaybackSnapshot PlaybackSnapshot;
        public readonly LayerMask TargetLayerMask;
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;
        public readonly MonsterActor FinishTarget;
        public readonly MonsterActor SpecialBreakTarget;
        public readonly float SpecialBreakDamageByMaxHpRate;
        public readonly float SpecialBreakFixedDamage;
        public readonly float SpecialBreakMinReferenceHealth;
        // 캐릭터 공통 호밍/워프 reach — 잔상은 스왑 후에도 살아있어 활성 PlayerCombat을 라이브로 읽을 수 없으므로 스냅샷 시점 값을 캡처한다.
        public readonly float HomingReachRange;
        public readonly float HomingReachAngle;
        public readonly float WarpSearchRange;

        public PlayerResidualAttackSnapshot(
            PlayerActor ownerPlayer,
            CharacterModelData sourceModel,
            CharacterActorType characterType,
            AttackData currentAttackData,
            AttackInfoBase currentAttackInfoBase,
            IReadOnlyList<HitPhaseData> hitPhases,
            ActorAnimator.MotionPlaybackSnapshot playbackSnapshot,
            LayerMask targetLayerMask,
            Vector3 position,
            Quaternion rotation,
            MonsterActor finishTarget,
            MonsterActor specialBreakTarget,
            float specialBreakDamageByMaxHpRate,
            float specialBreakFixedDamage,
            float specialBreakMinReferenceHealth,
            float homingReachRange,
            float homingReachAngle,
            float warpSearchRange)
        {
            OwnerPlayer = ownerPlayer;
            SourceModel = sourceModel;
            CharacterType = characterType;
            CurrentAttackData = currentAttackData;
            CurrentAttackInfoBase = currentAttackInfoBase;
            HitPhases = hitPhases;
            PlaybackSnapshot = playbackSnapshot;
            TargetLayerMask = targetLayerMask;
            Position = position;
            Rotation = rotation;
            FinishTarget = finishTarget;
            SpecialBreakTarget = specialBreakTarget;
            SpecialBreakDamageByMaxHpRate = specialBreakDamageByMaxHpRate;
            SpecialBreakFixedDamage = specialBreakFixedDamage;
            SpecialBreakMinReferenceHealth = specialBreakMinReferenceHealth;
            HomingReachRange = homingReachRange;
            HomingReachAngle = homingReachAngle;
            WarpSearchRange = warpSearchRange;
        }
    }

    public readonly struct SwapResidualAttackRequest
    {
        public readonly PlayerResidualAttackSnapshot Snapshot;
        public readonly float MaxLifetime;
        public readonly float MinVisibleLifetime;
        public readonly float FadeOutDuration;
        public readonly Color DissolveColor;
        public readonly Texture DissolveNoiseMask;
        public readonly float DissolveNoiseStrength;
        public readonly Vector4 DissolveNoiseScrollRotate;
        public readonly bool AllowHitStop;
        public readonly bool UseRootMotion;
        public readonly float RootMotionMaxDistance;
        public readonly LayerMask RootMotionBlocker;
        public readonly float FeedbackMinInterval;
        public readonly float HitStopDuration;
        public readonly float HitStopTimeScale;
        public readonly bool ShowCharacterOnDamageFloater;

        public SwapResidualAttackRequest(
            PlayerResidualAttackSnapshot snapshot,
            float maxLifetime,
            float minVisibleLifetime,
            float fadeOutDuration,
            Color dissolveColor,
            Texture dissolveNoiseMask,
            float dissolveNoiseStrength,
            Vector4 dissolveNoiseScrollRotate,
            bool allowHitStop,
            bool useRootMotion,
            float rootMotionMaxDistance,
            LayerMask rootMotionBlocker,
            float feedbackMinInterval,
            float hitStopDuration,
            float hitStopTimeScale,
            bool showCharacterOnDamageFloater)
        {
            Snapshot = snapshot;
            MaxLifetime = maxLifetime;
            MinVisibleLifetime = minVisibleLifetime;
            FadeOutDuration = fadeOutDuration;
            DissolveColor = dissolveColor;
            DissolveNoiseMask = dissolveNoiseMask;
            DissolveNoiseStrength = dissolveNoiseStrength;
            DissolveNoiseScrollRotate = dissolveNoiseScrollRotate;
            AllowHitStop = allowHitStop;
            UseRootMotion = useRootMotion;
            RootMotionMaxDistance = rootMotionMaxDistance;
            RootMotionBlocker = rootMotionBlocker;
            FeedbackMinInterval = feedbackMinInterval;
            HitStopDuration = hitStopDuration;
            HitStopTimeScale = hitStopTimeScale;
            ShowCharacterOnDamageFloater = showCharacterOnDamageFloater;
        }
    }
}
