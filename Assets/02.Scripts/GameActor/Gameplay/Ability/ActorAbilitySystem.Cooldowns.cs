using System.Collections.Generic;
using UPlayGround.Ability.Core;
using UPlayGround.Data.Ability;

namespace UPlayGround.Gameplay.Ability
{
    public sealed partial class ActorAbilitySystem
    {
        /// <summary>캐릭터 교체에서 파티 공용 지원 쿨다운을 유지하고 캐릭터 쿨다운만 초기화한다.</summary>
        public void ClearCharacterCooldowns()
        {
            var retained = CapturePlayerActorCooldowns();
            _cooldowns.Clear(); RestorePlayerActorCooldowns(retained);
        }
        private List<AbilityCooldownSnapshot> CapturePlayerActorCooldowns()
        {
            var result = new List<AbilityCooldownSnapshot>();
            _cooldowns.Capture(result);
            result.RemoveAll(entry => !IsPlayerActorCooldown(entry.GroupId));
            return result;
        }
        private void RestorePlayerActorCooldowns(List<AbilityCooldownSnapshot> entries)
        {
            foreach (var entry in entries)
                _cooldowns.Restore(entry.GroupId, entry.RemainingSeconds, entry.AvailableCharges,
                    entry.MaxCharges, entry.RechargeDurationSeconds);
        }
        private bool IsPlayerActorCooldown(string groupId)
        {
            foreach (var pair in _temporaryAbilities)
                if (MatchesActorCooldown(pair.Key, groupId)) return true;
            if (_abilitySet != null)
                foreach (var ability in _abilitySet.EnumerateAll())
                    if (MatchesActorCooldown(ability, groupId)) return true;
            return false;
        }
        private static bool MatchesActorCooldown(GameplayAbilitySO ability, string groupId) =>
            ability?.cooldown?.scope == AbilityCooldownScope.PlayerActor
            && ability.cooldown.ResolveGroupId(ability.abilityId) == groupId;
    }
}
