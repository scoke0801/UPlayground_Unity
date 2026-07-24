#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using UPlayGround.Ability.Core;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Party;
using UPlayGround.Data.Stat;

namespace UPlayGround.Manager
{
    /// <summary>CheatManager — 플레이어 스탯 치트. 개발 빌드 전용.</summary>
    public partial class CheatManager
    {
        /// <summary>
        /// 활성 플레이어 캐릭터의 base 스탯을 즉시 변경한다.
        /// PlayerActor.RefreshGrowthStatsLive 를 재사용하므로 장비/버프 modifier는 보존되고,
        /// MaxHealth 변경 시 현재 HP/HUD가 자동 갱신된다(풀 회복).
        /// </summary>
        public bool SetPlayerAttribute(AttributeId attributeId, float value)
        {
            var player = PartyManager.Instance != null ? PartyManager.Instance.ActiveCharacter : null;
            if (player == null)
                return false;

            player.RefreshGrowthStatsLive(
                new Dictionary<AttributeId, float> { { attributeId, value } });
            Log(CheatCategory.Stat, $"{attributeId} = {value:0.##}");
            return true;
        }

        /// <summary> 활성 플레이어의 현재 base 스탯 값. 없으면 0. </summary>
        public float GetPlayerAttribute(AttributeId attributeId)
        {
            var player = PartyManager.Instance != null ? PartyManager.Instance.ActiveCharacter : null;
            if (player?.AbilitySystem == null)
                return 0f;
            return player.AbilitySystem.TryGetAttribute(
                attributeId, current: false, out float value) ? value : 0f;
        }

        public bool AddGrowthPoints(CharacterActorType type, int amount)
        {
            bool ok = PartyManager.Instance != null
                      && PartyManager.Instance.AddGrowthPointsForDebug(type, amount);
            if (ok)
                Log(CheatCategory.Stat, $"성장 포인트 변경: {type} {(amount >= 0 ? "+" : "")}{amount}");
            return ok;
        }

        public bool SetGrowthRank(CharacterActorType type, GrowthAttributeType attribute, int rank)
        {
            bool ok = PartyManager.Instance != null
                      && PartyManager.Instance.SetGrowthRankForDebug(type, attribute, rank);
            if (ok)
                Log(CheatCategory.Stat,
                    $"성장 능력치 적용: {type} {attribute} Rank {PartyManager.Instance.GetGrowthRank(type, attribute)}");
            return ok;
        }
    }
}
#endif
