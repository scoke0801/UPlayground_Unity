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
            float specialBreakFixedDamage)
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
        }
    }

    public readonly struct SwapResidualAttackRequest
    {
        public readonly PlayerResidualAttackSnapshot Snapshot;
        public readonly float MaxLifetime;
        public readonly float FadeOutDuration;
        public readonly bool AllowHitStop;
        public readonly bool UseRootMotion;

        public SwapResidualAttackRequest(
            PlayerResidualAttackSnapshot snapshot,
            float maxLifetime,
            float fadeOutDuration,
            bool allowHitStop,
            bool useRootMotion)
        {
            Snapshot = snapshot;
            MaxLifetime = maxLifetime;
            FadeOutDuration = fadeOutDuration;
            AllowHitStop = allowHitStop;
            UseRootMotion = useRootMotion;
        }
    }
}
