using System;
using System.Collections.Generic;
using UPlayGround.Data.Cycle;

namespace UPlayGround.Cycle
{
    public readonly struct BossDefeatContext
    {
        public readonly string bossActorId;
        public readonly string cycleSpawnId;
        public readonly bool finishedBySpecialBreakAttack;
        public readonly bool noHit;
        public BossDefeatContext(string bossActorId, string cycleSpawnId, bool specialBreak, bool noHit)
        { this.bossActorId = bossActorId; this.cycleSpawnId = cycleSpawnId; finishedBySpecialBreakAttack = specialBreak; this.noHit = noHit; }
    }

    public enum BossRecruitTrigger
    {
        None,
        BreakFinish,
        NoHit,
        DefeatCount,
    }

    public readonly struct BossRecruitmentResult
    {
        public readonly string assistId;
        public readonly bool success;
        public readonly BossRecruitTrigger trigger;
        public readonly int defeatCountBefore;
        public readonly int defeatCountAfter;
        public readonly int requiredDefeatCount;
        public readonly AssistRecruitResult rosterResult;
        public BossRecruitmentResult(
            string assistId,
            bool success,
            BossRecruitTrigger trigger,
            int before,
            int after,
            int required,
            AssistRecruitResult roster)
        {
            this.assistId = assistId;
            this.success = success;
            this.trigger = trigger;
            defeatCountBefore = before;
            defeatCountAfter = after;
            requiredDefeatCount = required;
            rosterResult = roster;
        }
    }

    public sealed class BossRecruitmentService
    {
        private readonly Dictionary<string, int> _defeatCounts = new(StringComparer.Ordinal);
        public IReadOnlyDictionary<string, int> DefeatCounts => _defeatCounts;

        public BossRecruitmentResult Resolve(
            string assistId,
            in BossDefeatContext context,
            int requiredDefeatCount,
            AssistRosterService roster,
            int maxRosterSize = 4)
        {
            if (string.IsNullOrWhiteSpace(assistId) || roster == null)
                return new BossRecruitmentResult(
                    assistId, false, BossRecruitTrigger.None, 0, 0,
                    Math.Max(1, requiredDefeatCount), null);

            _defeatCounts.TryGetValue(assistId, out int before);
            int after = before + 1;
            _defeatCounts[assistId] = after;
            int required = Math.Max(1, requiredDefeatCount);
            BossRecruitTrigger trigger = context.finishedBySpecialBreakAttack
                ? BossRecruitTrigger.BreakFinish
                : context.noHit
                    ? BossRecruitTrigger.NoHit
                    : after >= required
                        ? BossRecruitTrigger.DefeatCount
                        : BossRecruitTrigger.None;
            bool success = trigger != BossRecruitTrigger.None;
            AssistRecruitResult rosterResult = success ? roster.TryRecruit(assistId, maxRosterSize) : null;
            return new BossRecruitmentResult(
                assistId, success, trigger, before, after, required, rosterResult);
        }

        public List<AssistDefeatCountEntry> Export()
        {
            List<AssistDefeatCountEntry> result = new();
            foreach ((string id, int count) in _defeatCounts)
                result.Add(new AssistDefeatCountEntry
                {
                    assistId = id,
                    defeatCount = count,
                });
            result.Sort((left, right) =>
                string.CompareOrdinal(left.assistId, right.assistId));
            return result;
        }

        public void Restore(IEnumerable<AssistDefeatCountEntry> values)
        {
            _defeatCounts.Clear();
            if (values == null) return;
            foreach (AssistDefeatCountEntry value in values)
                if (value != null && !string.IsNullOrWhiteSpace(value.assistId))
                    _defeatCounts[value.assistId] = Math.Max(0, value.defeatCount);
        }
    }
}
