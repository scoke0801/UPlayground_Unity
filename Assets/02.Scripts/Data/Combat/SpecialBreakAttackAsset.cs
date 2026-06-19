using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Path;

namespace UPlayGround.Data.Combat
{
    [CreateAssetMenu(fileName = "SpecialBreakAttack", menuName = "UPlayGround/전투/Special Break Attack")]
    public class SpecialBreakAttackAsset : ScriptableObject
    {
        [Header("Motion")]
        public AnimKey animKey = AnimKey.BreakAttack;
        [Min(0.1f)] public float duration = 1.2f;
        [Tooltip("SpecialBreakAttackEvent가 없는 임시 모션을 위한 백스톱 시각. 이벤트가 박힌 클립은 이 시각 전에 이벤트가 발화하므로, 임팩트 프레임보다 '늦게'(duration에 근접하게) 잡아 이벤트가 항상 우선되도록 한다. 이벤트가 끝내 없으면 이 시각에 한 번 적용된다.")]
        [Min(0f)] public float fallbackHitTime = 1.0f;

        [Header("Camera")]
        public CameraSnapshotProfile cameraProfile;

        [Header("Targeting")]
        [Min(0.1f)] public float searchRange = 4f;
        [Range(0f, 180f)] public float searchAngle = 110f;
        [Min(0f)] public float startDistance = 1.5f;
        [Min(0f)] public float maxSlideSpeed = 18f;
        [Min(0f)] public float slideDuration = 0.25f;

        [Header("Target Reaction")]
        [Tooltip("특수 브레이크 피해자 모션 시작 시 공격자 반대 방향으로 밀려나는 거리.")]
        [Min(0f)] public float victimKnockbackDistance = 0.75f;
        [Tooltip("특수 브레이크 피해자 밀림이 지속되는 시간.")]
        [Min(0f)] public float victimKnockbackDuration = 0.18f;
        [Tooltip("특수 브레이크 피해자 밀림의 최대 속도.")]
        [Min(0f)] public float victimMaxKnockbackSpeed = 7f;

        [Header("Damage")]
        [Min(0f)] public float damageByMaxHpRate = 0.2f;
        [Min(0f)] public float fixedDamage = 0f;
        [Tooltip("비율 피해 계산용 기준 HP 하한. 최대 HP가 이 값보다 '낮은' 적만 이 HP를 가진 것처럼 계산해 약한 적의 타격감을 보장한다. 따라서 버프하려는 약한 적의 HP보다 '높게' 잡아야 동작한다(이하로 잡으면 아무 적에게도 적용되지 않음). 즉사를 피하려면 (기준HP × 피해율 + 고정 피해)가 가장 약한 적의 HP보다 작아야 한다. 예: 최약체 120HP·피해율 0.2 → 기준HP는 120 초과 600 미만. (0이면 비활성)")]
        [Min(0f)] public float minReferenceHealth = 0f;

        [Header("Feedback")]
        [Min(0f)] public float hitStopDuration = 0.08f;
        [Range(0.001f, 1f)] public float hitStopScale = 0.01f;
        [Min(0f)] public float globalHitStopDuration = 0.055f;
        [Range(0.001f, 1f)] public float globalHitStopScale = 0.02f;
        public CameraShakeIdType cameraShakeKey = CameraShakeIdType.CriticalHit;
        [Min(0f)] public float cameraPunchStrength = 0.26f;
        [Min(0f)] public float cameraPunchDuration = 0.16f;
        public string startVfxKey;
        public string hitVfxKey;
        public string finishVfxKey;
    }
}
