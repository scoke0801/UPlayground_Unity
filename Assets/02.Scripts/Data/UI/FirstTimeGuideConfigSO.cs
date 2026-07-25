using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.Event;

namespace UPlayGround.Data.UI
{
    [Serializable]
    public sealed class FirstTimeGuideEntry
    {
        [SerializeField] private string _guideId;
        [SerializeField] private GameMilestoneEvent _milestoneEvent;
        [SerializeField] private GuidePopupDataSO _popup;

        public string GuideId => _guideId?.Trim() ?? string.Empty;
        public GameMilestoneEvent MilestoneEvent => _milestoneEvent;
        public GuidePopupDataSO Popup => _popup;
        public bool IsValid => !string.IsNullOrEmpty(GuideId) && _popup != null;
    }

    [CreateAssetMenu(fileName = "FirstTimeGuideConfig", menuName = "UPlayGround/UI/First Time Guide Config")]
    public sealed class FirstTimeGuideConfigSO : ScriptableObject
    {
        [SerializeField] private List<FirstTimeGuideEntry> _entries = new();

        public IReadOnlyList<FirstTimeGuideEntry> Entries => _entries;

        public bool TryGet(
            GameMilestoneEvent milestoneEvent,
            out FirstTimeGuideEntry entry)
        {
            if (_entries == null)
            {
                entry = null;
                return false;
            }

            for (int i = 0; i < _entries.Count; i++)
            {
                FirstTimeGuideEntry candidate = _entries[i];
                if (candidate?.IsValid == true
                    && candidate.MilestoneEvent == milestoneEvent)
                {
                    entry = candidate;
                    return true;
                }
            }

            entry = null;
            return false;
        }
    }
}
