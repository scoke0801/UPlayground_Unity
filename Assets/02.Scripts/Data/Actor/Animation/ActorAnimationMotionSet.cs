using AYellowpaper.SerializedCollections;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Data.Ability;
using UPlayGround.Data.EnumType;
using UPlayGround.Gameplay.Tag;

namespace UPlayGround.Data.Actor.Animation
{
    /// <summary>
    /// 액터 소유 MotionSet을 조회하는 독립 키.
    /// Ability 식별자와 Variant 식별자를 포함하지 않으며, GAS는 이 값만 전달한다.
    /// </summary>
    [System.Serializable]
    public struct MotionKey : System.IEquatable<MotionKey>
    {
        public string value;

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(value);

        public MotionKey(string value)
        {
            this.value = value?.Trim();
        }

        /// <summary>
        /// 비교·해시 전에 공백을 떨어낸다. struct는 역직렬화와 인스펙터 편집이 생성자를
        /// 거치지 않으므로, 생성자 Trim만 믿으면 " Id"와 "Id"가 조용히 다른 키가 된다.
        /// </summary>
        private static string Normalize(string value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        public bool Equals(MotionKey other) =>
            string.Equals(
                Normalize(value),
                Normalize(other.value),
                System.StringComparison.Ordinal);

        public override bool Equals(object obj) =>
            obj is MotionKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                string normalizedValue = Normalize(value);
                return normalizedValue != null
                    ? System.StringComparer.Ordinal.GetHashCode(
                        normalizedValue)
                    : 0;
            }
        }

        public override string ToString() =>
            IsValid ? value : "<Invalid Motion Key>";

        public static bool operator ==(
            MotionKey left,
            MotionKey right) => left.Equals(right);

        public static bool operator !=(
            MotionKey left,
            MotionKey right) => !left.Equals(right);
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

        [Tooltip("독립 Motion Key에 대응하는 공격 모션입니다. GAS는 Key만 전달하고 실제 모션은 액터가 해석합니다.")]
        public SerializedDictionary<MotionKey, MotionSetAsset> abilityMotions;

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
            MotionKey key,
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
            MotionKey key,
            MotionSetAsset motion)
        {
            if (!key.IsValid)
                throw new System.ArgumentException(
                    "유효한 Ability Motion Key가 필요합니다.",
                    nameof(key));

            abilityMotions ??=
                new SerializedDictionary<MotionKey, MotionSetAsset>();
            abilityMotions[key] = motion;
        }
    }
}
