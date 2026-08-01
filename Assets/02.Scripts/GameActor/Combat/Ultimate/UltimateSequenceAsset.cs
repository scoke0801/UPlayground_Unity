using UnityEngine;
using System.Collections.Generic;
using UPlayGround.Animation;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Cinematic;

namespace UPlayGround.Data
{
    [System.Serializable]
    public class UltimateGameplayLockSettings
    {
        public bool lockPlayerInput = true;
        public bool lockCameraInput = true;
        public bool pauseEnemyAI = true;
        [Min(0f)] public float enemyFreezeRadius = 30f;
        public bool freezeTargets = true;
        public bool ignoreCasterDamage = true;
        public bool ignoreTargetReactions = true;
        public bool hideHud = true;
        public bool releaseLockOnOnEnter = false;
    }

    public enum UltimateTargetMode
    {
        CurrentLockOn,
        NearestEnemy,
        ForwardCone,
        ManualTransform,
        None
    }

    [System.Serializable]
    public class UltimateTargetPolicy
    {
        public UltimateTargetMode mode = UltimateTargetMode.CurrentLockOn;
        [Min(0f)] public float searchRadius = 15f;
        [Range(0f, 360f)] public float coneAngle = 120f;
        public LayerMask targetLayer = -1;
        public bool requireTarget = true;
        public bool includeMultipleTargets;
        [Min(1)] public int maxTargets = 1;
    }

    [System.Serializable]
    public class UltimatePlacementSettings
    {
        public bool warpCaster;
        public bool warpPrimaryTarget;
        public Vector3 casterOffsetFromTarget = new(0f, 0f, -2f);
        public Vector3 targetOffsetFromCaster = new(0f, 0f, 2f);
        public bool faceTarget = true;
        [Min(0f)] public float placementBlendDuration;
        public bool restorePositionsOnFinish;
    }

    /// <summary>
    /// 캐릭터별 궁극기 연출의 최소 실행 데이터.
    /// 후속 단계에서 잠금, 배치, 연출 이벤트, 복구 정책을 이 에셋에 확장한다.
    /// </summary>
    [CreateAssetMenu(
        fileName = "UltimateSequence",
        menuName = "UPlayGround/전투/Ultimate Sequence")]
    public class UltimateSequenceAsset : ScriptableObject
    {
        [Header("소유 캐릭터")]
        public CharacterActorType ownerType = CharacterActorType.None;

        [Header("1단계: 기본 재생")]
        public MotionSetAsset motionSet;
        public CameraSnapshotProfile cameraProfile;

        [Min(0f)]
        public float motionFadeDuration = 0.1f;

        [Tooltip("실제 입력 실행 시 Ultimate 슬롯의 게이지와 쿨타임을 소비한다.")]
        public bool consumeUltimateGauge = true;

        [Header("2단계: 게임플레이 잠금")]
        public UltimateGameplayLockSettings lockSettings = new();

        [Header("3단계: 타겟/배치")]
        public UltimateTargetPolicy targetPolicy = new();
        public UltimatePlacementSettings placementSettings = new();

        [Header("연출 스테이지")]
        public CinematicStageSettings cinematicStage = new();

        [Header("4단계: 연출 이벤트")]
        [Tooltip("끄면 ActorAnimator의 MotionSet 시간축을 사용해 타격/연출 타이밍을 정확히 맞춘다.")]
        public bool timelineUseUnscaledTime;
        [SerializeReference] public List<UltimateTimelineEvent> events = new();

        public bool IsValid(out string error)
        {
            if (ownerType == CharacterActorType.None)
            {
                error = "ownerType이 지정되지 않았습니다.";
                return false;
            }

            if (motionSet == null || motionSet.motionSet == null || !motionSet.motionSet.IsValid())
            {
                error = "유효한 MotionSetAsset이 필요합니다.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private void OnValidate()
        {
            motionFadeDuration = Mathf.Max(0f, motionFadeDuration);
            lockSettings ??= new UltimateGameplayLockSettings();
            lockSettings.enemyFreezeRadius = Mathf.Max(0f, lockSettings.enemyFreezeRadius);
            targetPolicy ??= new UltimateTargetPolicy();
            targetPolicy.searchRadius = Mathf.Max(0f, targetPolicy.searchRadius);
            targetPolicy.coneAngle = Mathf.Clamp(targetPolicy.coneAngle, 0f, 360f);
            targetPolicy.maxTargets = Mathf.Max(1, targetPolicy.maxTargets);
            placementSettings ??= new UltimatePlacementSettings();
            placementSettings.placementBlendDuration =
                Mathf.Max(0f, placementSettings.placementBlendDuration);
            cinematicStage ??= new CinematicStageSettings();
            events ??= new List<UltimateTimelineEvent>();
            foreach (UltimateTimelineEvent timelineEvent in events)
            {
                if (timelineEvent == null)
                    continue;
                timelineEvent.startTime = Mathf.Max(0f, timelineEvent.startTime);
                timelineEvent.duration = Mathf.Max(0f, timelineEvent.duration);
            }
        }
    }
}
