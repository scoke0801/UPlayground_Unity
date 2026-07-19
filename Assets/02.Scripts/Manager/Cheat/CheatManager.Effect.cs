#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Contracts.Ability;
using UPlayGround.Data.Ability;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Manager
{
    /// <summary>CheatManager — 활성 플레이어 버프·디버프 발급/제거.</summary>
    public partial class CheatManager
    {
        private readonly HashSet<GameplayEffectSO> _effectCatalogSet = new();

        public void CopyAvailableGameplayEffects(List<GameplayEffectSO> destination)
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));

            destination.Clear();
            _effectCatalogSet.Clear();

            GameplayEffectSO[] loaded =
                Resources.FindObjectsOfTypeAll<GameplayEffectSO>();
            for (int i = 0; i < loaded.Length; i++)
                AddAvailableEffect(loaded[i], destination);

            PartyManager party = PartyManager.Instance;
            if (party != null)
            {
                foreach (CharacterActorType type in
                         Enum.GetValues(typeof(CharacterActorType)))
                {
                    CharacterPassiveSetSO set = party.GetPassiveSet(type);
                    if (set?.passives == null)
                        continue;

                    for (int i = 0; i < set.passives.Count; i++)
                    {
                        PassiveAbilitySO passive = set.passives[i];
                        if (passive?.triggeredEffects == null)
                            continue;

                        for (int j = 0; j < passive.triggeredEffects.Count; j++)
                            AddAvailableEffect(
                                passive.triggeredEffects[j],
                                destination);
                    }
                }
            }

            destination.Sort(CompareEffects);
        }

        public bool GrantGameplayEffect(
            GameplayEffectSO effect,
            GameplayEffectHudVisibility hudVisibility =
                GameplayEffectHudVisibility.UseDefinition)
        {
            PlayerActor player = PartyManager.Instance?.ActiveCharacter;
            if (player?.Effects == null || effect == null)
            {
                Log(CheatCategory.Effect, "Effect 발급 실패: 플레이어 또는 데이터 없음");
                return false;
            }

            var handle = player.Effects.ApplyEffect(
                effect,
                player,
                new GameplayEffectApplicationOptions(hudVisibility));
            bool applied = effect.durationType == GameplayEffectDurationType.Instant
                || handle.IsValid;
            Log(
                CheatCategory.Effect,
                applied
                    ? $"Effect 발급: {GetEffectName(effect)} ({effect.effectId})"
                    : $"Effect 발급 실패: {GetEffectName(effect)} ({effect.effectId})");
            return applied;
        }

        public bool RemoveGameplayEffect(ulong runtimeId)
        {
            PlayerActor player = PartyManager.Instance?.ActiveCharacter;
            bool removed = player?.Effects != null
                           && player.Effects.RemoveEffectByRuntimeId(runtimeId);
            Log(
                CheatCategory.Effect,
                removed
                    ? $"활성 Effect 제거: runtime {runtimeId}"
                    : $"활성 Effect 제거 실패: runtime {runtimeId}");
            return removed;
        }

        public int RemoveAllGameplayEffects()
        {
            PlayerActor player = PartyManager.Instance?.ActiveCharacter;
            if (player?.Effects == null)
            {
                Log(CheatCategory.Effect, "전체 Effect 제거 실패: 활성 캐릭터 없음");
                return 0;
            }

            var active = new List<GameplayEffectViewState>();
            player.Effects.CopyActiveEffects(active);
            player.Effects.RemoveAll();
            Log(CheatCategory.Effect, $"활성 Effect 전체 제거: {active.Count}개");
            return active.Count;
        }

        private void AddAvailableEffect(
            GameplayEffectSO effect,
            List<GameplayEffectSO> destination)
        {
            if (effect == null
                || string.IsNullOrWhiteSpace(effect.effectId)
                || effect.durationType == GameplayEffectDurationType.Instant
                || !_effectCatalogSet.Add(effect))
            {
                return;
            }

            destination.Add(effect);
        }

        private static int CompareEffects(
            GameplayEffectSO left,
            GameplayEffectSO right)
        {
            int polarity = left.polarity.CompareTo(right.polarity);
            if (polarity != 0)
                return polarity;
            return string.CompareOrdinal(left.effectId, right.effectId);
        }

        private static string GetEffectName(GameplayEffectSO effect) =>
            !string.IsNullOrWhiteSpace(effect?.presentation?.displayName)
            && !string.Equals(
                effect.presentation.displayName.Trim(),
                "새 Effect",
                StringComparison.Ordinal)
                ? effect.presentation.displayName
                : effect?.name ?? "Effect";
    }
}
#endif
