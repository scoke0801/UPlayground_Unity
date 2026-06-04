using System.Collections.Generic;
using UnityEngine;
using UPlayGround.CameraSystem;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Path;

namespace UPlayGround.Data
{
    [CreateAssetMenu(fileName = "CombatCameraProfile", menuName = "UPlayGround/SO/Camera/Combat Camera Profile")]
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
            softTargetYawDuration = Mathf.Max(0.01f, softTargetYawDuration);
            manualInputSuppressDuration = Mathf.Max(0f, manualInputSuppressDuration);
        }
    }
}
