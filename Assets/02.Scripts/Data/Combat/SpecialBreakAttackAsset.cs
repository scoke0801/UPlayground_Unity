using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data.Combat
{
    [CreateAssetMenu(fileName = "SpecialBreakAttack", menuName = "UPlayGround/SO/Combat/Special Break Attack")]
    public class SpecialBreakAttackAsset : ScriptableObject
    {
        [Header("Owner")]
        public CharacterActorType ownerType = CharacterActorType.None;

        [Header("Motion")]
        public AnimKey animKey = AnimKey.FinishAttack;
        public MotionSetAsset motionSet;
        [Min(0.1f)] public float duration = 1.2f;
        [Tooltip("SpecialBreakAttackEvent가 없는 임시 모션에서 피해를 적용할 폴백 시간.")]
        [Min(0f)] public float fallbackHitTime = 0.15f;

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

        [Header("Feedback")]
        [Min(0f)] public float hitStopDuration = 0.08f;
        public string startVfxKey;
        public string hitVfxKey;
        public string finishVfxKey;
    }
}
