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

        [Tooltip("대화·연출이 끝난 뒤 가이드를 띄우기까지 기다릴 시간(초). "
                 + "연출 도중 대화가 잠깐 끊기는 구간에서 팝업이 새어 나오지 않게 한다.")]
        [SerializeField, Range(0f, 3f)] private float _presentationSettleSeconds = 0.5f;

        public IReadOnlyList<FirstTimeGuideEntry> Entries => _entries;

        /// <summary>연출이 멈춘 뒤 가이드 출력을 허용하기까지의 대기 시간(초).</summary>
        public float PresentationSettleSeconds => _presentationSettleSeconds;

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
