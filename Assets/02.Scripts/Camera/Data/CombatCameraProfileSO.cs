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
        // 카메라 yaw 보정 강도(0~1). 0.25=20~30%만 따라감, 0.6=궁극기/스킬급 적극 보정.
        // 캐릭터 회전(모션워프 호밍)은 별개로 풀 회전하므로 이 값은 카메라 프레이밍에만 영향.
        [Range(0f, 1f)] public float softTargetYawStrength = 0.25f;
        // 앵글 게이트(도). 카메라 정면 기준 이 각도 이내의 적만 보정한다.
        // 게이트 = "범위"(뒤/극단 측면 적 제외) 용도. "확 돌아감" 억제는 게이트가 아니라
        // softTargetYawStrength가 담당하므로 넓게 둔다(호밍으로 적을 향해 풀 회전한 뒤라
        // 히트 시점 적은 전방 호 어딘가에 있음 — 좁으면 정상 전투까지 통째로 막힌다).
        [Range(0f, 180f)] public float softTargetMaxAngle = 60f;
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
