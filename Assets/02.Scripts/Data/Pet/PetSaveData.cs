using System;
using System.Collections.Generic;

namespace UPlayGround.Data.Pet
{
    /// <summary>세이브 단위의 보유·외형·친밀도. 경로와 월드 위치는 저장하지 않는다.</summary>
    [Serializable]
    public sealed class PetSaveData
    {
        public List<PetOwnedEntry> owned = new();
        public string selectedPetId;
        public bool isAccompanying;
        public int progressionVersion;
        public string modelId;
        public bool isAutoPickupEnabled = true;
        public string selectedSupportId = PetSkillCatalogSO.Heal;
        public List<string> supportSlots = new();
        public List<UPlayGround.Ability.Core.GasCooldownSaveEntry> supportCooldowns = new();
        public float supportMinimumIntervalRemaining;
        public string SelectedSupportId => supportSlots != null && supportSlots.Count > 0
            ? supportSlots[0] : selectedSupportId;
        public List<PetSkillProgress> skills = new();
        public float supportCooldownRemaining;
        public List<UPlayGround.Ability.Core.ActiveEffectSaveEntry> supportEffects = new();
    }

    [Serializable]
    public sealed class PetSkillProgress
    {
        public string skillId;
        public int level = 1;
    }

    [Serializable]
    public sealed class PetOwnedEntry
    {
        public string petId;
        public string appearanceId;
        public List<string> unlockedAppearances = new();
        public long bondPoints;
    }

    /// <summary>누락 콘텐츠는 보존하고 중복 항목과 미보유 선택만 정리한다.</summary>
    public static class PetSaveRules
    {
        /// <summary>원본 저장을 바꾸지 않고 알려지지 않은 ID까지 보존한 안전한 사본을 만든다.</summary>
        public static PetSaveData Normalize(PetSaveData source)
        {
            var result = new PetSaveData
            { selectedPetId = source?.selectedPetId, isAccompanying = source?.isAccompanying ?? false };
            result.progressionVersion = Math.Max(3, source?.progressionVersion ?? 0);
            result.modelId = source?.modelId;
            bool migrated = source == null || source.progressionVersion < 2;
            result.isAutoPickupEnabled = migrated || source.isAutoPickupEnabled;
            result.selectedSupportId = source?.selectedSupportId ?? PetSkillCatalogSO.Heal;
            if (source?.supportSlots != null)
                result.supportSlots.AddRange(source.supportSlots);
            if (result.supportSlots.Count == 0) result.supportSlots.Add(result.selectedSupportId);
            result.selectedSupportId = result.supportSlots[0];
            result.supportMinimumIntervalRemaining = SafeRemaining(source?.supportMinimumIntervalRemaining ?? 0f);
            if (source?.supportCooldowns != null)
                foreach (var entry in source.supportCooldowns)
                {
                    if (entry == null || string.IsNullOrWhiteSpace(entry.groupId)) continue;
                    result.supportCooldowns.Add(new UPlayGround.Ability.Core.GasCooldownSaveEntry
                    { groupId = entry.groupId, remainingSeconds = SafeRemaining(entry.remainingSeconds),
                        availableCharges = entry.availableCharges, maxCharges = entry.maxCharges,
                        rechargeDurationSeconds = SafeRemaining(entry.rechargeDurationSeconds) });
                }
            result.supportCooldownRemaining = source != null && !float.IsNaN(source.supportCooldownRemaining)
                && !float.IsInfinity(source.supportCooldownRemaining) ? Math.Max(0f, source.supportCooldownRemaining) : 0f;
            if (source?.skills != null)
                foreach (var skill in source.skills)
                {
                    if (skill == null || string.IsNullOrWhiteSpace(skill.skillId)) continue;
                    var existing = result.skills.Find(value => value.skillId == skill.skillId);
                    bool known = Array.IndexOf(PetSkillCatalogSO.SkillIds, skill.skillId) >= 0;
                    int level = known ? Math.Max(1, Math.Min(PetSkillCatalogSO.MaxLevel, skill.level)) : skill.level;
                    if (existing == null) result.skills.Add(new PetSkillProgress { skillId = skill.skillId, level = level });
                    else existing.level = Math.Max(existing.level, level);
                }
            foreach (string id in PetSkillCatalogSO.SkillIds)
                if (!result.skills.Exists(skill => skill.skillId == id))
                    result.skills.Add(new PetSkillProgress { skillId = id });
            if (source?.supportEffects != null)
                foreach (var entry in source.supportEffects)
                {
                    if (entry == null) continue;
                    var copy = new UPlayGround.Ability.Core.ActiveEffectSaveEntry
                    { effectId = entry.effectId, sourceActorId = entry.sourceActorId,
                        remainingSeconds = entry.remainingSeconds, stackCount = entry.stackCount,
                        hudVisibility = entry.hudVisibility, specLevel = entry.specLevel };
                    if (entry.setByCaller != null)
                        foreach (var magnitude in entry.setByCaller)
                            if (magnitude != null) copy.setByCaller.Add(new UPlayGround.Ability.Core.SetByCallerSaveEntry { key = magnitude.key, value = magnitude.value });
                    result.supportEffects.Add(copy);
                }
            if (source?.owned != null)
            {
                var byId = new Dictionary<string, PetOwnedEntry>(StringComparer.Ordinal);
                foreach (var entry in source.owned)
                {
                    if (entry == null || string.IsNullOrWhiteSpace(entry.petId)) continue;
                    if (!byId.TryGetValue(entry.petId, out var merged))
                    {
                        merged = new PetOwnedEntry { petId = entry.petId, appearanceId = entry.appearanceId };
                        byId.Add(entry.petId, merged);
                        result.owned.Add(merged);
                    }
                    merged.bondPoints = Math.Max(merged.bondPoints, Math.Max(0, entry.bondPoints));
                    if (entry.unlockedAppearances == null) continue;
                    foreach (string appearance in entry.unlockedAppearances)
                        if (!string.IsNullOrWhiteSpace(appearance) && !merged.unlockedAppearances.Contains(appearance))
                            merged.unlockedAppearances.Add(appearance);
                }
                if (result.selectedPetId != null && byId.ContainsKey(result.selectedPetId)) return result;
            }
            result.selectedPetId = null;
            result.isAccompanying = false;
            return result;
        }

        private static float SafeRemaining(float value)
            => float.IsNaN(value) || float.IsInfinity(value) ? 0f : Math.Max(0f, value);
    }
}
