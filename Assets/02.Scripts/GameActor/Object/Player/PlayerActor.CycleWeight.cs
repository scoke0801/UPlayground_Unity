using UnityEngine;
using UPlayGround.Data.Combat;
using UPlayGround.Data.Cycle;
using UPlayGround.Manager;
using UPlayGround.Ability.Core;

namespace UPlayGround
{
    public partial class PlayerActor
    {
        private ActiveGameplayEffectHandle _cycleWeightEffectHandle;
        private CharacterWeightProfileSO _weightProfile;

        public CharacterWeightProfileSO WeightProfile => _weightProfile;
        public float WeightDamageMultiplier => _weightProfile != null ? _weightProfile.damageMultiplier : 1f;
        public float WeightBreakDamageMultiplier => _weightProfile != null ? _weightProfile.breakDamageMultiplier : 1f;
        public float CurrentDodgeIFrameSeconds => _weightProfile != null ? _weightProfile.dodgeIFrameSeconds : 0.35f;

        /// <summary>프로필이 있으면 기본 바이탈 오브 설정 대신 체급별 정책으로 스폰을 시도한다.</summary>
        public bool TrySpawnWeightRecovery(
            Vector3 position,
            VitalOrbTrigger trigger,
            bool specialBreak)
        {
            VitalRecoveryPolicySO policy = _weightProfile != null ? _weightProfile.recoveryPolicy : null;
            if (policy == null) return false;

            ActorSvc.Combat?.TrySpawnVitalOrbByPolicy(
                trigger,
                position,
                specialBreak ? policy.specialBreakSpawnChance : policy.normalHitSpawnChance,
                specialBreak ? policy.specialBreakOrbCount : policy.normalHitOrbCount,
                specialBreak ? policy.specialBreakHealScale : policy.normalHitHealScale);
            return true;
        }

        private void ApplyCharacterWeight(CharacterWeightProfileSO profile)
        {
            if (_cycleWeightEffectHandle.IsValid)
                AbilitySystem.RemoveEffect(_cycleWeightEffectHandle);
            _weightProfile = profile;
            if (_weightProfile == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"[PlayerActor] {_characterActorType}의 무게 프로필이 없어 표준 배율로 폴백합니다.", this);
#endif
                return;
            }

            if (!_weightProfile.Validate(out string error))
            {
                Debug.LogError($"[PlayerActor] 무게 프로필 '{_weightProfile.name}' 오류: {error}", _weightProfile);
                return;
            }

            _cycleWeightEffectHandle = AbilitySystem.ApplyAttributeEffect(
                $"CycleWeight.{_characterActorType}",
                new[]
                {
                    new AttributeModifierValue(
                        global::UPlayGround.Data.Stat.Attributes.Movement.MoveSpeed,
                        AttributeModifierOperation.Multiply,
                        _weightProfile.moveSpeedMultiplier),
                });
        }
    }
}
