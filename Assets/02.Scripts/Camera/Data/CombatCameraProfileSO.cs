using System.Collections.Generic;
using UnityEngine;
using UPlayGround.CameraSystem;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Path;

namespace UPlayGround.Data
{
    [CreateAssetMenu(fileName = "CombatCameraProfile", menuName = "UPlayGround/카메라/Combat Profile")]
    public class CombatCameraProfileSO : ScriptableObject
    {
        public CombatCameraIntentType intentType = CombatCameraIntentType.LightHit;
        public int priority = 0;

        [Header("Context Override")]
        public bool requireAttackerMonsterGrade = false;
        public MonsterActorGrade attackerMonsterGrade = MonsterActorGrade.Normal;
        public bool requireVictimMonsterGrade = false;
        public MonsterActorGrade victimMonsterGrade = MonsterActorGrade.Normal;
        [Range(0f, 1f)] public float triggerChance = 1f;

        [Header("Effects")]
        public List<CameraEffectData> effects = new List<CameraEffectData>();

        [Header("Shake / Punch")]
        public CameraShakeIdType shakeKey = CameraShakeIdType.None;
        public bool usePunch = false;
        public float punchStrength = 0.15f;
        public float punchDuration = 0.12f;

        [Header("Snapshot")]
        public bool useSnapshotSequence = false;
        public CameraSnapshotProfile snapshotProfile;

        [Header("Input")]
        public bool lockInput = false;

        [Header("Soft Target Assist")]
        public bool enableSoftTargetAssist = false;
        [Tooltip("화면 안전 영역 경계까지 필요한 회전량에 곱하는 강도. 공통 배율·회전량·각속도 제한이 추가 적용됩니다.")]
        [Range(0f, 1f)] public float softTargetYawStrength = 0.25f;
        [Tooltip("플레이어에서 적으로 향하는 방향과 카메라의 최대 허용 각도. 실제 화면 밖·가려진 대상은 제외합니다.")]
        [Range(0f, 180f)] public float softTargetMaxAngle = 60f;
        [Tooltip("보정의 최소 진행 시간. 회전 속도 상한을 지키기 위해 더 길어질 수 있습니다.")]
        public float softTargetYawDuration = 0.12f;
        public float manualInputSuppressDuration = 0.35f;

        public bool HasPlayableContent()
        {
            bool hasEffects = effects != null && effects.Exists(effect => effect != null);
            return hasEffects
                   || shakeKey != CameraShakeIdType.None
                   || usePunch
                   || (useSnapshotSequence && snapshotProfile != null)
                   || enableSoftTargetAssist;
        }

        private void OnValidate()
        {
            priority = Mathf.Max(0, priority);
            triggerChance = Mathf.Clamp01(triggerChance);
            punchStrength = Mathf.Max(0f, punchStrength);
            punchDuration = Mathf.Max(0f, punchDuration);
            softTargetYawStrength = Mathf.Clamp01(softTargetYawStrength);
            softTargetMaxAngle = Mathf.Clamp(softTargetMaxAngle, 0f, 180f);
            softTargetYawDuration = Mathf.Max(0.01f, softTargetYawDuration);
            manualInputSuppressDuration = Mathf.Max(0f, manualInputSuppressDuration);
        }
    }
}
