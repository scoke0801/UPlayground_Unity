using AYellowpaper.SerializedCollections;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Data.Ability;
using UPlayGround.Data.EnumType;
using UPlayGround.Gameplay.Tag;

namespace UPlayGround.Data.Actor.Animation
{
    /// <summary>
    /// Ability 실행 결과를 액터 소유 MotionSet에 연결하는 안정 키.
    /// 모션 에셋은 GAS Payload가 아니라 ActorAnimationMotionSet이 소유한다.
    /// </summary>
    [System.Serializable]
    public struct AbilityMotionKey : System.IEquatable<AbilityMotionKey>
    {
        public string abilityId;
        public string variantId;

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(abilityId)
            && !string.IsNullOrWhiteSpace(variantId);

        public AbilityMotionKey(string abilityId, string variantId)
        {
            this.abilityId = abilityId?.Trim();
            this.variantId = variantId?.Trim();
        }

        public static AbilityMotionKey From(
            GameplayAbilitySO ability,
            AbilityVariantDefinition variant) =>
            new(ability?.abilityId, variant?.variantId);

        /// <summary>
        /// 비교·해시 전에 공백을 떨어낸다. struct는 역직렬화와 인스펙터 편집이 생성자를
        /// 거치지 않으므로, 생성자 Trim만 믿으면 " Id"와 "Id"가 조용히 다른 키가 된다.
        /// </summary>
        private static string Normalize(string value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        public bool Equals(AbilityMotionKey other) =>
            string.Equals(
                Normalize(abilityId),
                Normalize(other.abilityId),
                System.StringComparison.Ordinal)
            && string.Equals(
                Normalize(variantId),
                Normalize(other.variantId),
                System.StringComparison.Ordinal);

        public override bool Equals(object obj) =>
            obj is AbilityMotionKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                string normalizedAbilityId = Normalize(abilityId);
                string normalizedVariantId = Normalize(variantId);
                int hash = 17;
                hash = hash * 31
                       + (normalizedAbilityId != null
                           ? System.StringComparer.Ordinal.GetHashCode(
                               normalizedAbilityId)
                           : 0);
                hash = hash * 31
                       + (normalizedVariantId != null
                           ? System.StringComparer.Ordinal.GetHashCode(
                               normalizedVariantId)
                           : 0);
                return hash;
            }
        }

        public override string ToString() =>
            IsValid ? $"{abilityId}::{variantId}" : "<Invalid Ability Motion Key>";

        public static bool operator ==(
            AbilityMotionKey left,
            AbilityMotionKey right) => left.Equals(right);

        public static bool operator !=(
            AbilityMotionKey left,
            AbilityMotionKey right) => !left.Equals(right);
    }

    [CreateAssetMenu(fileName = "ActorAnimationMotionSet", menuName = "UPlayGround/애니메이션/Actor")]
    public class ActorAnimationMotionSet : ScriptableObject
    {
        [Tooltip("이 SO에 없는 키는 여기서 탐색 (공용 휴머노이드 모션 등)")]
        public ActorAnimationMotionSet fallbackMotionSet;

        [Header("공격 모션")]
        [Tooltip("이 모션셋이 담당하는 공격 무기 타입입니다.")]
        public WeaponType attackWeaponType = WeaponType.NoWeapon;

        [Tooltip("이 액터 모션 세트에서 함께 저작할 공격 Ability 모음입니다.")]
        public AbilitySetSO attackAbilitySet;

        [Tooltip("Ability/Variant 키에 대응하는 공격 모션입니다. GAS는 키만 전달하고 실제 모션은 액터가 해석합니다.")]
        public SerializedDictionary<AbilityMotionKey, MotionSetAsset> abilityMotions;

        [Header("상태 모션")]
        [Tooltip("액터 상태가 사용하는 의미 슬롯 매핑입니다.")]
        public SerializedDictionary<GameplayTag, MotionSetAsset> motionSlots;

        public MotionSetAsset GetMotionSetAsset(GameplayTag slot, int depth = 0)
        {
            if (depth > 8 || !slot.IsValid()) return null;
            if (motionSlots != null
                && motionSlots.TryGetValue(slot, out MotionSetAsset result)
                && result != null)
                return result;
            return fallbackMotionSet?.GetMotionSetAsset(slot, depth + 1);
        }

        public MotionSet GetMotionSet(GameplayTag slot, int depth = 0) =>
            GetMotionSetAsset(slot, depth)?.motionSet;

        public MotionSetAsset GetAbilityMotionAsset(
            AbilityMotionKey key,
            int depth = 0)
        {
            if (depth > 8 || !key.IsValid)
                return null;
            if (abilityMotions != null
                && abilityMotions.TryGetValue(key, out MotionSetAsset result)
                && result != null)
                return result;
            return fallbackMotionSet?.GetAbilityMotionAsset(key, depth + 1);
        }

        public void SetAbilityMotionAsset(
            AbilityMotionKey key,
            MotionSetAsset motion)
        {
            if (!key.IsValid)
                throw new System.ArgumentException(
                    "유효한 Ability Motion Key가 필요합니다.",
                    nameof(key));

            abilityMotions ??=
                new SerializedDictionary<AbilityMotionKey, MotionSetAsset>();
            abilityMotions[key] = motion;
        }
    }
}

