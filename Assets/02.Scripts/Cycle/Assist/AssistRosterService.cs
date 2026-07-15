using System;
using System.Collections.Generic;

namespace UPlayGround.Cycle
{
    public enum AssistRecruitStatus { Added, Duplicate, PendingRosterFull, Invalid }

    [Serializable]
    public sealed class AssistRecruitResult
    {
        public AssistRecruitStatus status;
        public string assistId;
    }

    public sealed class AssistRosterService
    {
        private readonly List<string> _roster = new();
        public IReadOnlyList<string> Roster => _roster;
        public string EquippedAssistId { get; private set; }
        public string PendingRecruitAssistId { get; private set; }

        public AssistRecruitResult TryRecruit(string assistId, int maxRosterSize)
        {
            if (string.IsNullOrWhiteSpace(assistId)) return new AssistRecruitResult { status = AssistRecruitStatus.Invalid };
            if (_roster.Contains(assistId)) return new AssistRecruitResult { status = AssistRecruitStatus.Duplicate, assistId = assistId };
            if (_roster.Count >= maxRosterSize)
            {
                PendingRecruitAssistId = assistId;
                return new AssistRecruitResult { status = AssistRecruitStatus.PendingRosterFull, assistId = assistId };
            }
            _roster.Add(assistId);
            if (string.IsNullOrEmpty(EquippedAssistId)) EquippedAssistId = assistId;
            return new AssistRecruitResult { status = AssistRecruitStatus.Added, assistId = assistId };
        }

        public bool Equip(string assistId)
        {
            if (!_roster.Contains(assistId)) return false;
            EquippedAssistId = assistId;
            return true;
        }

        public bool Release(string assistId)
        {
            if (!_roster.Remove(assistId)) return false;
            if (EquippedAssistId == assistId) EquippedAssistId = _roster.Count > 0 ? _roster[0] : null;
            return true;
        }

        public bool ResolvePending(string releaseAssistId, bool acceptNew)
        {
            if (string.IsNullOrEmpty(PendingRecruitAssistId)) return false;
            string pending = PendingRecruitAssistId;
            if (!acceptNew) { PendingRecruitAssistId = null; return true; }
            if (string.IsNullOrEmpty(releaseAssistId) || !_roster.Contains(releaseAssistId)) return false;
            if (!Release(releaseAssistId)) return false;
            _roster.Add(pending);
            EquippedAssistId = pending;
            PendingRecruitAssistId = null;
            return true;
        }

        public void Restore(IEnumerable<string> roster, string equipped, string pending)
        {
            _roster.Clear();
            if (roster != null)
                foreach (string id in roster) if (!string.IsNullOrWhiteSpace(id) && !_roster.Contains(id)) _roster.Add(id);
            EquippedAssistId = _roster.Contains(equipped) ? equipped : (_roster.Count > 0 ? _roster[0] : null);
            PendingRecruitAssistId = pending;
        }

        public void Clear() { _roster.Clear(); EquippedAssistId = null; PendingRecruitAssistId = null; }
    }
}
