using System;
using UnityEngine;
using UPlayGround.Data.Combat;

namespace UPlayGround.Data
{
    /// <summary>교전 구도의 거리·시야·피벗과 전환 예산.</summary>
    [Serializable]
    public sealed class EncounterCameraProfile
    {
        public Vector2 distanceRange = new Vector2(5f, 5f);
        public float pitch = 14f;
        public float fieldOfView = 55f;
        public Vector3 pivotOffset = new Vector3(0f, 1.3f, 0f);
        public float maxDistance = 9.5f;
        [Min(0f)] public float hitAssistMaxAngle = 6f;
        [Min(0.01f)] public float transitionTime = 1f;
    }

    public partial class CameraSettings : ISerializationCallbackReceiver
    {
        [Header("교전 구도")]
        public EncounterCameraProfile explore = new EncounterCameraProfile();
        public EncounterCameraProfile sweep = new EncounterCameraProfile
        {
            distanceRange = new Vector2(7f, 9f), pitch = 24f, fieldOfView = 60f,
            pivotOffset = new Vector3(0f, 1.2f, 0f), hitAssistMaxAngle = 3f,
        };
        public EncounterCameraProfile duel = new EncounterCameraProfile
        {
            distanceRange = new Vector2(5.5f, 5.5f), pitch = 16f, fieldOfView = 56f,
            pivotOffset = new Vector3(0f, 1.35f, 0f),
        };
        [SerializeField, HideInInspector] private bool _hasEncounterProfiles;
        [Min(1)] public int crowdCountForMaxDistance = 10;
        [Min(0f)] public float crowdDistanceDeadZone = 0.15f;
        [Range(0f, 1f)] public float crowdPivotRatio = 0.25f;
        [Min(0f)] public float crowdPivotMaxDistance = 1.5f;
        [Min(1f)] public float crowdAttackingWeight = 2f;
        public bool enableLockOnFixedDistance = true;

        // 기존 API는 프리셋을 편집한다. 직렬화 원본 필드는 마이그레이션에만 사용한다.
        public Vector3 defaultOffset { get => explore.pivotOffset; set => explore.pivotOffset = value; }
        public Vector3 combatOffset { get => sweep.pivotOffset; set => sweep.pivotOffset = value; }
        public float fovExplore { get => explore.fieldOfView; set => explore.fieldOfView = value; }
        public float fovCombat { get => sweep.fieldOfView; set => sweep.fieldOfView = value; }
        public float explorePitch { get => explore.pitch; set => explore.pitch = value; }
        public float combatPitch { get => sweep.pitch; set => sweep.pitch = value; }

        /// <summary>어댑터가 제공한 교전 종류에 대응하는 구도를 반환한다.</summary>
        public EncounterCameraProfile GetEncounterProfile(CombatEncounterKind kind) => kind switch
        {
            CombatEncounterKind.Sweep => sweep,
            CombatEncounterKind.Duel => duel,
            _ => explore,
        };

        public void OnBeforeSerialize() => _hasEncounterProfiles = true;
        /// <summary>이전 에셋의 조정값을 한 번만 프리셋으로 옮긴다.</summary>
        public void OnAfterDeserialize()
        {
            if (_hasEncounterProfiles) return;
            explore ??= new EncounterCameraProfile();
            sweep ??= new EncounterCameraProfile();
            duel ??= new EncounterCameraProfile();
            explore.pivotOffset = _legacyDefaultOffset;
            sweep.pivotOffset = _legacyCombatOffset;
            explore.fieldOfView = _legacyFovExplore;
            sweep.fieldOfView = _legacyFovCombat;
            explore.pitch = _legacyExplorePitch;
            sweep.pitch = _legacyCombatPitch;
            explore.distanceRange = new Vector2(defaultDistance, defaultDistance);
            _hasEncounterProfiles = true;
        }
    }
}
