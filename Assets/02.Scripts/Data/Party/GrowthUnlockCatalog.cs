using System;
using UnityEngine;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data.Party
{
    /// <summary>해금 난이도 티어. 티어→요구 랭크만 난이도를 결정한다(랜덤화와 무관).</summary>
    public enum GrowthUnlockTier
    {
        Free,   // 투자 없이 항상 해금
        Easy,   // 낮은 요구 랭크
        Medium, // 중간 요구 랭크
        Hard,   // 높은 요구 랭크
    }

    /// <summary>
    /// 컨텐츠 해금 카탈로그 + 시드 기반 결정적 리졸버.
    ///
    /// 설계:
    /// - 콘텐츠별 "난이도 티어"는 고정(=전체 난이도 유지).
    /// - "어느 능력치에 투자해야 해금되는가(속성)"만 (seed, characterType, unlockId) 해시로 결정.
    ///   시드는 새 게임마다 바뀌므로 해금 속성 배치가 런마다 달라진다.
    /// - 테이블을 저장하지 않는 무상태 순수 함수. string.GetHashCode는 런 간 불안정할 수 있어
    ///   FNV-1a 자체 해시를 사용한다.
    ///
    /// 기본 해금(무료): 약공격 5타까지, 강공격 2타까지.
    /// </summary>
    public static class GrowthUnlockCatalog
    {
        // ── 무료 하한 ─────────────────────────────────────────────
        public const int FreeLightSteps = 5; // 약공격 5타까지 기본 해금
        public const int FreeHeavySteps = 2; // 강공격 2타까지 기본 해금

        // ── 티어별 요구 랭크(난이도 상수. 속성 maxRank 기본 20 이내) ──
        public const int EasyRequiredRank = 3;
        public const int MediumRequiredRank = 6;
        public const int HardRequiredRank = 12;

        // 속성 후보(순서 고정 — 해시 인덱스 안정성). GrowthAttributeType 전체.
        private static readonly GrowthAttributeType[] Attributes =
        {
            GrowthAttributeType.Health,
            GrowthAttributeType.Defense,
            GrowthAttributeType.Critical,
            GrowthAttributeType.AttackSpeed,
            GrowthAttributeType.AttackPower,
        };

        /// <summary>해당 콤보 스텝이 투자 없이 기본 해금인지.</summary>
        public static bool IsComboStepFree(GrowthComboType comboType, int step)
        {
            int freeSteps = comboType == GrowthComboType.Heavy ? FreeHeavySteps : FreeLightSteps;
            return step <= freeSteps;
        }

        /// <summary>티어 → 요구 랭크.</summary>
        public static int TierRequiredRank(GrowthUnlockTier tier) => tier switch
        {
            GrowthUnlockTier.Free => 0,
            GrowthUnlockTier.Easy => EasyRequiredRank,
            GrowthUnlockTier.Medium => MediumRequiredRank,
            GrowthUnlockTier.Hard => HardRequiredRank,
            _ => MediumRequiredRank,
        };

        /// <summary>unlockId가 어느 티어(난이도)에 속하는지 결정.</summary>
        public static GrowthUnlockTier TierFor(GrowthUnlockType unlockType, string unlockId)
        {
            if (string.IsNullOrWhiteSpace(unlockId)) return GrowthUnlockTier.Medium;

            if (unlockType == GrowthUnlockType.Skill)
            {
                if (unlockId == GrowthUnlockIds.Skill(GrowthSkillType.Ultimate))
                    return GrowthUnlockTier.Hard;          // 궁극: 어렵게
                // Ability, ElementalImbue(속성 스킬): 낮게
                return GrowthUnlockTier.Easy;
            }

            // Combo 계열: ComboRoute(약+강 조합), 그리고 무료 하한을 넘는 콤보 스텝
            if (unlockId.StartsWith(GrowthUnlockIds.RoutePrefix, StringComparison.Ordinal))
                return GrowthUnlockTier.Medium;

            if (TryParseComboStep(unlockId, out GrowthComboType comboType, out int step))
                return IsComboStepFree(comboType, step) ? GrowthUnlockTier.Free : GrowthUnlockTier.Medium;

            return GrowthUnlockTier.Medium;
        }

        /// <summary>이 콘텐츠가 투자 없이 항상 해금(무료)인지.</summary>
        public static bool IsFree(GrowthUnlockType unlockType, string unlockId)
            => TierFor(unlockType, unlockId) == GrowthUnlockTier.Free;

        /// <summary>
        /// (seed, characterType, unlockId) → 해금에 필요한 (속성, 요구 랭크).
        /// 속성은 시드 해시로 셔플되고, 요구 랭크는 티어로 고정된다.
        /// </summary>
        public static (GrowthAttributeType attribute, int requiredRank) Resolve(
            int seed,
            CharacterActorType type,
            GrowthUnlockType unlockType,
            string unlockId)
        {
            GrowthUnlockTier tier = TierFor(unlockType, unlockId);
            int requiredRank = TierRequiredRank(tier);
            if (tier == GrowthUnlockTier.Free)
                return (GrowthAttributeType.AttackPower, 0);

            uint hash = Fnv1a(seed, (int)type, unlockId);
            GrowthAttributeType attribute = Attributes[hash % (uint)Attributes.Length];
            return (attribute, requiredRank);
        }

        /// <summary>"Combo.Light.3" 형태의 unlockId를 (comboType, step)으로 파싱.</summary>
        public static bool TryParseComboStep(string unlockId, out GrowthComboType comboType, out int step)
        {
            comboType = GrowthComboType.Light;
            step = 0;
            if (string.IsNullOrEmpty(unlockId)) return false;

            string[] parts = unlockId.Split('.');
            if (parts.Length != 3 || parts[0] != "Combo") return false;
            if (!Enum.TryParse(parts[1], out comboType)) return false;
            return int.TryParse(parts[2], out step);
        }

        // FNV-1a 32bit: 프로세스/런 간 안정적인 결정적 해시.
        private static uint Fnv1a(int seed, int typeValue, string unlockId)
        {
            const uint offset = 2166136261;
            const uint prime = 16777619;
            uint hash = offset;

            hash = MixInt(hash, seed, prime);
            hash = MixInt(hash, typeValue, prime);

            if (!string.IsNullOrEmpty(unlockId))
            {
                for (int i = 0; i < unlockId.Length; i++)
                {
                    hash ^= unlockId[i];
                    hash *= prime;
                }
            }
            return hash;
        }

        private static uint MixInt(uint hash, int value, uint prime)
        {
            unchecked
            {
                uint v = (uint)value;
                for (int b = 0; b < 4; b++)
                {
                    hash ^= v & 0xFF;
                    hash *= prime;
                    v >>= 8;
                }
            }
            return hash;
        }
    }
}
