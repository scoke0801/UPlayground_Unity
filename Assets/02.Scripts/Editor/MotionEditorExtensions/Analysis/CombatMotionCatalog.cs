using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Data.Ability;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Animation.Editor
{
    /// <summary>동일 체형·장비 조건에서 재생성하는 전투 모션 측정 묶음. 전투 원본에는 기록하지 않는다.</summary>
    public sealed class CombatMotionCatalog : ScriptableObject
    {
        public GameObject modelPrefab;
        public WeaponType weapon = WeaponType.Katana;
        public string weaponPath;
        public int drawnConstraintSource;
        public string bladeBasePath;
        public string bladeTipPath;
        [Range(15, 240)] public int sampleRate = 120;
        [Min(0.01f)] public float directionMinimumTravel = 0.15f;
        [Min(1f)] public float directionDominance = 1.5f;
        [Min(0.01f)] public float movementThreshold = 0.1f;
        public List<CombatMotionEntry> entries = new();
    }

    /// <summary>사용처와 수동 검수는 재분석 결과와 별도로 보존한다.</summary>
    [Serializable]
    public sealed class CombatMotionEntry
    {
        public GameplayAbilitySO ability;
        public string variantId;
        public string usage;
        public string manualTags;
        public bool isReviewed;
        [TextArea] public string reviewNotes;
        [HideInInspector] public CombatMotionMeasurement measurement;
    }

    /// <summary>원본 GUID와 sub-asset ID는 정밀도 손실이 없는 문자열로 내보낸다.</summary>
    [Serializable]
    public sealed class MotionSourceIdentity
    {
        public string path, guid, localFileId, dependencyHash;
    }

    /// <summary>좌표는 시작 모델 원점 기준 +X 오른쪽/+Y 위/+Z 앞, m·s·degree 단위다.</summary>
    [Serializable]
    public sealed class CombatMotionMeasurement
    {
        public int version;
        public string generatedUtc, fingerprint, motionKey, resolvedOwner, avatar;
        public bool usedNoWeaponFallback, hasSinglePassTiming;
        public string coordinateSystem = "시작 모델 좌표: +X 오른쪽/+Y 위/+Z 앞; m, s, degree; 포즈 비교는 현재 루트 상대";
        public string timeBasis = "MotionSet 저작 타임라인 초. 1배속 무중단 순회; 히트스톱/시간 보정/분기의 실제 경과 시간이 아님";
        public string samplingMode = "MotionSetPlaybackController Seek + Animancer 수동 평가; 이벤트 실행기 없음; 클립 경계 크로스페이드 제외";
        public MotionSourceIdentity motionSet, prefab, ability, payload;
        public List<MotionClipMeasurement> clips = new();
        public List<MotionSourceIdentity> dependencies = new();
        public List<string> sharedUsages = new();
        public List<string> limitations = new();
        public List<string> diagnostics = new();
        public List<MotionTimingWindow> windows = new();
        public List<MotionPoseSample> samples = new();
        public List<MotionBoundaryPose> boundaries = new();
        public float duration, beforeFirstHit = -1, afterLastHit = -1;
        public float rootPathLength, rootYaw, peakTipSpeed, peakTipTime;
        public Vector3 rootDisplacement;
        public string suggestedDirection = "미분류", suggestedMovement = "미분류";
        public string classificationStatus = "궤적 기반 자동 제안 / 미검수";
    }

    /// <summary>원본 전체 클립 1배속과 저작된 재생 구간을 분리한다.</summary>
    [Serializable]
    public sealed class MotionClipMeasurement
    {
        public MotionSourceIdentity source;
        public string name, layer, mask, importSettings;
        public float clipStart, clipEnd, speed, timelineStart, duration, layerWeight;
        public bool isHumanoid;
        public List<MotionPoseSample> originalClipSamples = new();
    }

    /// <summary>저작 이벤트와 런타임 마스크에서 파생한 행동별 허용 구간.</summary>
    [Serializable]
    public sealed class MotionTimingWindow
    {
        public string kind, label, actions, source, direction = "미분류";
        public float start, end, gapFromPrevious;
        public int hitPhaseIndex = -1;
        public List<string> groups = new();
        public bool hasBounds;
        public Vector3 boundsMin, boundsMax;
    }

    /// <summary>포즈 평가 직후 측정한 무기 세 점과 부착 판정 형상.</summary>
    [Serializable]
    public sealed class MotionPoseSample
    {
        public float time, clipTime, tipSpeed, rootYaw;
        public int motionIndex;
        public Vector3 rootPosition, bladeBase, bladeMiddle, bladeTip;
        public Quaternion rootRotation;
        public Vector3 baseVelocity, middleVelocity, tipVelocity;
        public List<MotionHitboxSample> hitboxes = new();
    }

    /// <summary>HitBox 형상은 실제 Collider에서 추출하며 겹침 질의나 피해 판정은 실행하지 않는다.</summary>
    [Serializable]
    public sealed class MotionHitboxSample
    {
        public string path, group, shape;
        public Vector3 center, halfExtents, point0, point1;
        public Quaternion rotation;
        public float radius;
    }

    /// <summary>시작·종료·타격·캔슬 경계의 Humanoid 관절 위치·회전·속도.</summary>
    [Serializable]
    public sealed class MotionBoundaryPose
    {
        public string label;
        public float time;
        public List<MotionJointPose> joints = new();
    }

    /// <summary>현재 루트 기준 관절 특징. 후보 비교용이며 콤보를 자동 변경하지 않는다.</summary>
    [Serializable]
    public sealed class MotionJointPose
    {
        public string bone;
        public Vector3 position, velocity;
        public Quaternion rotation;
    }
}
