using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Party;
using UPlayGround.Manager;

namespace UPlayGround.UI
{
    public sealed class UI_RestGrowth : UI_Base
    {
        [Serializable]
        private sealed class GrowthCard
        {
            public GrowthAttributeType attribute;
            public TextMeshProUGUI nameText;
            public TextMeshProUGUI rankText;
            public TextMeshProUGUI effectText;
            public TextMeshProUGUI milestoneText;
            public Button investButton;
        }

        [Header("헤더")]
        [SerializeField] private TextMeshProUGUI _characterNameText;
        [SerializeField] private TextMeshProUGUI _pointText;
        [SerializeField] private Button _closeButton;

        [Header("성장 카드")]
        [SerializeField] private List<GrowthCard> _cards = new();

        [Header("해금 알림")]
        [SerializeField] private TextMeshProUGUI _unlockText;

        private CharacterActorType _targetType;
        private readonly Dictionary<Button, UnityAction> _investActions = new();
        private bool _pausedByThisPopup;

        protected override bool BlocksLowerInput => true;

        protected override void Awake()
        {
            base.Awake();

            // 초기 UI Toolkit 프리팹에서 전환된 에셋은 루트 scale/size가 0으로 남을 수 있다.
            // UIManager의 Popup Canvas 자식으로 생성되는 즉시 전체 화면 uGUI 루트로 정규화한다.
            if (_rectTransform == null) return;
            _rectTransform.localScale = Vector3.one;
            _rectTransform.anchorMin = Vector2.zero;
            _rectTransform.anchorMax = Vector2.one;
            _rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _rectTransform.anchoredPosition = Vector2.zero;
            _rectTransform.offsetMin = Vector2.zero;
            _rectTransform.offsetMax = Vector2.zero;
        }

        protected override void OnInit()
        {
            _layer = CanvasLayer.Popup;
            _closeButton?.onClick.AddListener(Hide);
            for (int i = 0; i < _cards.Count; i++)
            {
                GrowthCard card = _cards[i];
                if (card?.investButton == null) continue;
                GrowthAttributeType attribute = card.attribute;
                UnityAction action = () => Invest(attribute);
                _investActions[card.investButton] = action;
                card.investButton.onClick.AddListener(action);
            }
        }

        protected override void OnShow()
        {
            _pausedByThisPopup = false;
            if (GameTimeManager.Instance != null && !GameTimeManager.Instance.IsPaused)
            {
                GameTimeManager.Instance.SetPause(true);
                _pausedByThisPopup = true;
            }

            _targetType = PartyManager.Instance?.ActiveCharacterType ?? CharacterActorType.None;
            Subscribe();
            if (_unlockText != null) _unlockText.text = string.Empty;
            RefreshView();
        }

        protected override void OnHide()
        {
            Unsubscribe();

            if (_pausedByThisPopup)
            {
                GameTimeManager.Instance?.SetPause(false);
                _pausedByThisPopup = false;
            }
        }

        protected override void OnDispose()
        {
            Unsubscribe();
            _closeButton?.onClick.RemoveListener(Hide);
            foreach (var pair in _investActions)
                if (pair.Key != null) pair.Key.onClick.RemoveListener(pair.Value);
            _investActions.Clear();
            base.OnDispose();
        }

        private void Invest(GrowthAttributeType attribute)
        {
            if (PartyManager.Instance?.TryInvestGrowthPoint(_targetType, attribute) == true)
                RefreshView();
        }

        private void Subscribe()
        {
            if (PartyManager.Instance == null) return;
            PartyManager.Instance.OnGrowthPointsChanged -= HandlePointsChanged;
            PartyManager.Instance.OnGrowthPointsChanged += HandlePointsChanged;
            PartyManager.Instance.OnGrowthUnlock -= HandleUnlock;
            PartyManager.Instance.OnGrowthUnlock += HandleUnlock;
        }

        private void Unsubscribe()
        {
            if (PartyManager.Instance == null) return;
            PartyManager.Instance.OnGrowthPointsChanged -= HandlePointsChanged;
            PartyManager.Instance.OnGrowthUnlock -= HandleUnlock;
        }

        private void HandlePointsChanged(CharacterActorType type, int _) { if (type == _targetType) RefreshView(); }

        private void HandleUnlock(CharacterActorType type, GrowthUnlockMilestone milestone)
        {
            if (type != _targetType || _unlockText == null) return;
            string name = string.IsNullOrWhiteSpace(milestone.displayName) ? milestone.unlockId : milestone.displayName;
            _unlockText.text = $"해금: {name}";
        }

        private void RefreshView()
        {
            PartyManager party = PartyManager.Instance;
            if (party == null || _targetType == CharacterActorType.None) return;
            if (_characterNameText != null) _characterNameText.text = $"{_targetType} 성장";
            if (_pointText != null) _pointText.text = $"사용 가능 포인트  {party.GetGrowthPoints(_targetType)}";
            for (int i = 0; i < _cards.Count; i++) RefreshCard(_cards[i]);
        }

        private void RefreshCard(GrowthCard card)
        {
            if (card == null) return;
            PartyManager party = PartyManager.Instance;
            PartyMemberGrowthSO growth = party.GetGrowthData(_targetType);
            if (growth == null) return;
            growth.TryGetInvestmentRule(card.attribute, out GrowthInvestmentRule rule);
            int rank = party.GetGrowthRank(_targetType, card.attribute);
            if (card.nameText != null) card.nameText.text = GetDisplayName(card.attribute);
            if (card.rankText != null) card.rankText.text = $"{rank} / {Mathf.Max(1, rule.maxRank)}";
            if (card.effectText != null) card.effectText.text = $"랭크당 +{FormatEffect(rule)}";
            if (card.milestoneText != null) card.milestoneText.text = GetNextMilestoneText(rule, rank);
            if (card.investButton != null)
                card.investButton.interactable = party.GetGrowthPoints(_targetType) > 0 && rank < Mathf.Max(1, rule.maxRank);
        }

        private static string GetDisplayName(GrowthAttributeType attribute) => attribute switch
        {
            GrowthAttributeType.Health => "체력",
            GrowthAttributeType.Defense => "방어력",
            GrowthAttributeType.Critical => "크리티컬",
            GrowthAttributeType.AttackSpeed => "공격속도",
            _ => "공격력",
        };

        private static string FormatEffect(GrowthInvestmentRule rule)
            => rule.statType is UPlayGround.Data.Stat.StatType.Defense
                or UPlayGround.Data.Stat.StatType.CritRate
                or UPlayGround.Data.Stat.StatType.AttackSpeed
                or UPlayGround.Data.Stat.StatType.AttackPower
                ? $"{rule.flatPerRank * 100f:0.#}%"
                : $"{rule.flatPerRank:0.#}";

        private static string GetNextMilestoneText(GrowthInvestmentRule rule, int rank)
        {
            if (rule.milestones == null || rule.milestones.Count == 0) return "마일스톤 없음";
            GrowthUnlockMilestone? next = null;
            for (int i = 0; i < rule.milestones.Count; i++)
            {
                GrowthUnlockMilestone milestone = rule.milestones[i];
                if (milestone.requiredRank <= rank) continue;
                if (!next.HasValue || milestone.requiredRank < next.Value.requiredRank) next = milestone;
            }
            if (!next.HasValue) return "모든 마일스톤 해금 완료";
            string name = string.IsNullOrWhiteSpace(next.Value.displayName) ? next.Value.unlockId : next.Value.displayName;
            return $"다음 해금: {next.Value.requiredRank}랭크 · {name}";
        }
    }
}
