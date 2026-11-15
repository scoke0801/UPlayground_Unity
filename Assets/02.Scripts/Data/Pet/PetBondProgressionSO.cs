using System;
using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Data.Pet
{
    public enum PetBondSource { Combat, Lifestyle, Quest }

    /// <summary>유대 단계와 세 원천의 효율·보호 한도를 한곳에서 조정한다.</summary>
    [CreateAssetMenu(menuName = "UPlayGround/Pet/Bond Progression")]
    public sealed class PetBondProgressionSO : ScriptableObject
    {
        public long secondSlotPoints = 100;
        public long signaturePoints = 400;
        [Min(0f)] public float supportPoints = 2f;
        [Min(0f)] public float encounterPoints = 2f;
        [Min(0f)] public float sweepWeight = 1f;
        [Min(0f)] public float duelWeight = 1.5f;
        [Min(0f)] public float encounterCap = 8f;
        [Min(0f)] public float minimumEncounterSeconds = 15f;
        [Min(1f)] public float lifestyleSecondsPerPoint = 60f;
        [Min(0f)] public float recentActivitySeconds = 5f;
        [Min(0f)] public float objectivePoints = 5f;
        [Min(0f)] public float questPoints = 15f;
        public float[] sourceCaps = { 30f, 30f, 60f };
        [Range(0f, 1f)] public float reducedEfficiency = .2f;
        [Min(1f)] public float activeWindowSeconds = 1800f;
        [Min(0f)] public float movementMinimumDistance = .01f;
        [Min(0f)] public float movementMaximumSampleDistance = 8f;
        public int GetStage(long points) => points >= signaturePoints ? 3 : points >= secondSlotPoints ? 2 : 1;
        public int GetSlotCount(long points) => GetStage(points) >= 2 ? 2 : 1;
        public float GetSupportPoints(PetSupportTriggerKind trigger, bool hasRecentActivity)
            => trigger == PetSupportTriggerKind.HealthBelow
                || trigger == PetSupportTriggerKind.HeavyHitTaken && !hasRecentActivity ? 0f : supportPoints;
        public float GetCap(PetBondSource source) => sourceCaps != null && (int)source < sourceCaps.Length
            ? Mathf.Max(0f, sourceCaps[(int)source]) : 0f;
    }

    [Serializable]
    public sealed class PetBondClaim
    {
        public string questId;
        public string objectiveId;
    }

    /// <summary>날짜 변경과 별개인 실활동 창 및 영구 퀘스트 청구 기록이다.</summary>
    [Serializable]
    public sealed class PetBondLedger
    {
        public int day = -1;
        public float[] daily = new float[3];
        public float[] window = new float[3];
        public float windowActiveSeconds;
        public float lifestyleRemainder;
        public float fractionalPoints;
        public float encounterEarned;
        public float encounterSeconds;
        public float encounterPresentSeconds;
        public bool encounterHadAction;
        public bool encounterActive;
        public List<PetBondClaim> claims = new();
    }

    public static class PetBondRules
    {
        /// <summary>실활동 창과 생활 적립을 분리한다. 유효 동행·일시정지 여부는 호출자가 먼저 검사한다.</summary>
        public static float AdvanceActivity(PetBondLedger ledger, PetBondProgressionSO data, float seconds,
            bool lifestyleActivity, bool combatActivity)
        {
            if ((!lifestyleActivity && !combatActivity) || !float.IsFinite(seconds) || seconds <= 0f) return 0f;
            ledger.windowActiveSeconds += seconds;
            float window = Mathf.Max(1f, data.activeWindowSeconds);
            if (ledger.windowActiveSeconds >= window)
            {
                ledger.windowActiveSeconds %= window;
                Array.Clear(ledger.window, 0, ledger.window.Length);
            }
            if (!lifestyleActivity) return 0f;
            ledger.lifestyleRemainder += seconds;
            float period = Mathf.Max(1f, data.lifestyleSecondsPerPoint);
            float points = Mathf.Floor(ledger.lifestyleRemainder / period);
            ledger.lifestyleRemainder %= period;
            return points;
        }

        /// <summary>각 한도의 초과 구간에만 감쇠를 적용하며 감쇠율을 중복 곱하지 않는다.</summary>
        public static float Award(PetSaveData state, PetBondProgressionSO data, PetBondSource source,
            float amount, int day, bool encounter = false)
        {
            if (state == null || data == null || !float.IsFinite(amount) || amount <= 0f) return 0f;
            var ledger = state.bondLedger;
            if (ledger.day != day) { ledger.day = day; Array.Clear(ledger.daily, 0, 3); }
            int index = (int)source;
            float full = Mathf.Min(Mathf.Max(0f, data.GetCap(source) - ledger.daily[index]),
                Mathf.Max(0f, data.GetCap(source) - ledger.window[index]));
            if (encounter) full = Mathf.Min(full, Mathf.Max(0f, data.encounterCap - ledger.encounterEarned));
            full = Mathf.Min(amount, full);
            float awarded = full + (amount - full) * data.reducedEfficiency;
            ledger.daily[index] += amount;
            ledger.window[index] += amount;
            if (encounter) ledger.encounterEarned += amount;
            ledger.fractionalPoints += awarded;
            long whole = (long)Mathf.Floor(ledger.fractionalPoints + .00001f);
            ledger.fractionalPoints = Mathf.Max(0f, ledger.fractionalPoints - whole);
            state.sharedBondPoints = state.sharedBondPoints > long.MaxValue - whole
                ? long.MaxValue : state.sharedBondPoints + whole;
            return awarded;
        }

        public static bool Claim(PetBondLedger ledger, string questId, string objectiveId)
        {
            if (string.IsNullOrWhiteSpace(questId)) return false;
            for (int i = 0; i < ledger.claims.Count; i++)
                if (ledger.claims[i].questId == questId && ledger.claims[i].objectiveId == objectiveId) return false;
            ledger.claims.Add(new PetBondClaim { questId = questId, objectiveId = objectiveId });
            return true;
        }

        public static PetBondLedger Clone(PetBondLedger source)
        {
            if (source == null) return new PetBondLedger();
            var result = new PetBondLedger { day = source.day,
                windowActiveSeconds = Safe(source.windowActiveSeconds), lifestyleRemainder = Safe(source.lifestyleRemainder),
                fractionalPoints = Safe(source.fractionalPoints) % 1f, encounterEarned = Safe(source.encounterEarned),
                encounterSeconds = Safe(source.encounterSeconds), encounterPresentSeconds = Safe(source.encounterPresentSeconds),
                encounterHadAction = source.encounterHadAction, encounterActive = source.encounterActive };
            for (int i = 0; i < 3; i++)
            {
                result.daily[i] = source.daily != null && i < source.daily.Length ? Safe(source.daily[i]) : 0f;
                result.window[i] = source.window != null && i < source.window.Length ? Safe(source.window[i]) : 0f;
            }
            if (source.claims != null)
                foreach (var claim in source.claims)
                    if (claim != null) Claim(result, claim.questId, claim.objectiveId);
            return result;
        }
        private static float Safe(float value) => float.IsFinite(value) ? Mathf.Max(0f, value) : 0f;
    }
}
