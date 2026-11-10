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
    }
}
