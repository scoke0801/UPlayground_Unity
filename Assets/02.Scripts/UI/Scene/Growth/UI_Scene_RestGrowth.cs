using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UPlayGround.Ability.Core;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Party;
using UPlayGround.Manager;

namespace UPlayGround.UI
{
    public sealed class UI_Scene_RestGrowth : UI_Base
    {
        [Serializable]
        private sealed class GrowthCard
        {
            public GameObject root;
            [AttributeIdSelector]
            public string attributeId;
            public TextMeshProUGUI nameText;
            public TextMeshProUGUI rankText;
            public TextMeshProUGUI effectText;
            public TextMeshProUGUI milestoneText;
            public Button investButton;

            public AttributeId AttributeId => new(attributeId);

            public GameObject Root =>
                root != null
                    ? root
                    : investButton != null
                        ? investButton.transform.parent.gameObject
                        : nameText != null
                            ? nameText.transform.parent.gameObject
                            : null;
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

            // 이전 빌더로 생성된 프리팹도 재생성 없이 긴 장비 보정 문구를 수용한다.
            for (int i = 0; i < _cards.Count; i++)
                NormalizeCardLayout(_cards[i]);
        }

        protected override void OnInit()
        {
            _layer = CanvasLayer.Popup;
            _closeButton?.onClick.AddListener(Hide);
            for (int i = 0; i < _cards.Count; i++)
            {
                GrowthCard card = _cards[i];
                if (card?.investButton == null) continue;
                int cardIndex = i;
                UnityAction action = () => ActivateCard(cardIndex);
                _investActions[card.investButton] = action;
                card.investButton.onClick.AddListener(action);
            }
        }

        protected override void OnShow()
        {
            base.OnShow();
            _pausedByThisPopup = false;
            if (Svc.GameTime != null && !Svc.GameTime.IsPaused)
            {
                Svc.GameTime.SetPause(true);
                _pausedByThisPopup = true;
            }

            _targetType = UISvc.Party?.ActiveCharacterType ?? CharacterActorType.None;
            UISvc.Party?.SetSkillTreeAccessAllowed(true);
            Subscribe();
            if (_unlockText != null) _unlockText.text = string.Empty;
            RefreshView();
        }

        protected override void OnHide()
        {
            Unsubscribe();
            UISvc.Party?.SetSkillTreeAccessAllowed(false);

            if (_pausedByThisPopup)
            {
                Svc.GameTime?.SetPause(false);
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

        private void ActivateCard(int cardIndex)
        {
            IUIPartyService party = UISvc.Party;
            CharacterSkillTreeSO tree = party?.GetSkillTree(_targetType);
            if (tree?.nodes != null && cardIndex >= 0 && cardIndex < tree.nodes.Count)
            {
                SkillNodeDefinition node = tree.nodes[cardIndex];
                if (node != null && party.TryTakeSkillNode(_targetType, node.nodeId))
                    RefreshView();
                return;
            }

            if (cardIndex >= 0
                && cardIndex < _cards.Count
                && party?.TryInvestGrowthPoint(
                    _targetType,
                    _cards[cardIndex].AttributeId) == true)
                RefreshView();
        }

        private void Subscribe()
        {
            if (UISvc.Party == null) return;
            UISvc.Party.OnGrowthPointsChanged -= HandlePointsChanged;
            UISvc.Party.OnGrowthPointsChanged += HandlePointsChanged;
            UISvc.Party.OnGrowthUnlock -= HandleUnlock;
            UISvc.Party.OnGrowthUnlock += HandleUnlock;
            UISvc.Party.OnSkillProgressChanged -= HandleSkillProgressChanged;
            UISvc.Party.OnSkillProgressChanged += HandleSkillProgressChanged;
            UISvc.Party.OnPartyProgressionChanged -= HandleProgressionChanged;
            UISvc.Party.OnPartyProgressionChanged += HandleProgressionChanged;
        }

        private void Unsubscribe()
        {
            if (UISvc.Party == null) return;
            UISvc.Party.OnGrowthPointsChanged -= HandlePointsChanged;
            UISvc.Party.OnGrowthUnlock -= HandleUnlock;
            UISvc.Party.OnSkillProgressChanged -= HandleSkillProgressChanged;
            UISvc.Party.OnPartyProgressionChanged -= HandleProgressionChanged;
        }

        private void HandlePointsChanged(CharacterActorType type, int _) { if (type == _targetType) RefreshView(); }
        private void HandleProgressionChanged(CharacterActorType type) { if (type == _targetType) RefreshView(); }
        private void HandleSkillProgressChanged(CharacterActorType type) { if (type == _targetType) RefreshView(); }

        private void HandleUnlock(CharacterActorType type, GrowthUnlockMilestone milestone)
        {
            if (type != _targetType || _unlockText == null) return;
            string name = string.IsNullOrWhiteSpace(milestone.displayName) ? milestone.unlockId : milestone.displayName;
            _unlockText.text = $"해금: {name}";
        }

        private void RefreshView()
        {
            IUIPartyService party = UISvc.Party;
            if (party == null || _targetType == CharacterActorType.None) return;
            CharacterSkillTreeSO tree = party.GetSkillTree(_targetType);
            bool useSkillTree = tree?.nodes != null && tree.nodes.Count > 0;
            if (_characterNameText != null)
                _characterNameText.text = useSkillTree
                    ? $"{_targetType} 스킬 트리"
                    : $"{_targetType} 성장";
            if (_pointText != null)
                _pointText.text = useSkillTree
                    ? $"사용 가능 스킬 포인트  {party.GetAvailableSkillPoints(_targetType)}"
                    : $"사용 가능 포인트  {party.GetGrowthPoints(_targetType)}";
            for (int i = 0; i < _cards.Count; i++)
            {
                if (useSkillTree)
                    RefreshSkillNodeCard(_cards[i], tree, i);
                else
                    RefreshCard(_cards[i]);
            }
            RebuildNavigation();
        }

        private void RefreshSkillNodeCard(
            GrowthCard card,
            CharacterSkillTreeSO tree,
            int index)
        {
            if (card == null || tree?.nodes == null || index >= tree.nodes.Count)
            {
                SetCardVisible(card, false);
                return;
            }

            SkillNodeDefinition node = tree.nodes[index];
            if (node == null)
            {
                SetCardVisible(card, false);
                return;
            }

            SetCardVisible(card, true);
            IUIPartyService party = UISvc.Party;
            int rank = party.GetSkillNodeRank(_targetType, node.nodeId);
            if (card.nameText != null)
                card.nameText.text = string.IsNullOrWhiteSpace(node.displayNameKey)
                    ? node.nodeId
                    : node.displayNameKey;
            if (card.rankText != null)
                card.rankText.text = $"{rank} / {Mathf.Max(1, node.maxRank)}";
            if (card.effectText != null)
                card.effectText.text = DescribeNode(node, Mathf.Min(rank + 1, Mathf.Max(1, node.maxRank)));
            bool canTake = party.CanTakeSkillNode(
                _targetType,
                node.nodeId,
                out SkillNodeBlockReason reason);
            if (card.milestoneText != null)
                card.milestoneText.text = canTake
                    ? "취득 가능"
                    : DescribeBlockReason(reason, node);
            if (card.investButton != null)
                card.investButton.interactable = canTake;
        }

        private static string DescribeNode(SkillNodeDefinition node, int previewRank)
        {
            if (node?.effects == null || node.effects.Count == 0)
                return string.IsNullOrWhiteSpace(node.descriptionKey)
                    ? "효과 없음"
                    : node.descriptionKey;
            var descriptions = new List<string>();
            for (int i = 0; i < node.effects.Count; i++)
                if (node.effects[i] != null)
                    descriptions.Add(node.effects[i].Describe(previewRank));
            return descriptions.Count > 0
                ? string.Join(" / ", descriptions)
                : "효과 없음";
        }

        private static string DescribeBlockReason(
            SkillNodeBlockReason reason,
            SkillNodeDefinition node) => reason switch
        {
            SkillNodeBlockReason.InsufficientPoints => $"포인트 부족 (비용 {Mathf.Max(1, node.cost)})",
            SkillNodeBlockReason.MissingPrerequisite => "선행 노드 필요",
            SkillNodeBlockReason.LevelTooLow => $"레벨 {Mathf.Max(1, node.requiredLevel)} 필요",
            SkillNodeBlockReason.MaxRank => "최대 랭크",
            SkillNodeBlockReason.NotInSafeZone => "안전 지역에서만 가능",
            _ => "취득 불가",
        };

        private void RebuildNavigation()
        {
            var buttons = new List<Selectable>();
            for (int i = 0; i < _cards.Count; i++)
            {
                Button button = _cards[i]?.investButton;
                if (button != null)
                    buttons.Add(button);
            }
            buttons.Add(_closeButton);
            UIFocusNavigation.ConfigureVertical(buttons);
            SetDefaultFocus(UIFocusNavigation.FirstNavigable(buttons.ToArray()), IsVisible);
        }

        private void RefreshCard(GrowthCard card)
        {
            if (card == null) return;
            IUIPartyService party = UISvc.Party;
            PartyMemberGrowthSO growth = party.GetGrowthData(_targetType);
            if (growth == null)
            {
                SetCardVisible(card, false);
                return;
            }

            AttributeId attributeId = card.AttributeId;
            if (!attributeId.IsValid
                || !growth.TryGetInvestmentRule(
                    attributeId,
                    out GrowthInvestmentRule rule))
            {
                SetCardVisible(card, false);
                return;
            }

            SetCardVisible(card, true);
            int rank = party.GetGrowthRank(_targetType, attributeId);
            int effectiveRank =
                party.GetEffectiveGrowthRank(_targetType, attributeId);
            int equipmentRank = Mathf.Max(0, effectiveRank - rank);
            if (card.nameText != null)
                card.nameText.text =
                    GrowthAttributeCatalog.GetDisplayName(attributeId);
            if (card.rankText != null)
                card.rankText.text = equipmentRank > 0
                    ? $"{effectiveRank} (투자 {rank} + 장비 {equipmentRank})"
                    : $"{rank} / {Mathf.Max(1, rule.maxRank)}";
            if (card.effectText != null) card.effectText.text = $"랭크당 +{FormatEffect(rule)}";
            if (card.milestoneText != null)
                card.milestoneText.text = GetNextMilestoneText(
                    party.GetGrowthUnlockMilestones(
                        _targetType,
                        attributeId),
                    effectiveRank);
            if (card.investButton != null)
                card.investButton.interactable = party.GetGrowthPoints(_targetType) > 0 && rank < Mathf.Max(1, rule.maxRank);
        }

        private static void SetCardVisible(GrowthCard card, bool visible)
        {
            GameObject root = card?.Root;
            if (root != null && root.activeSelf != visible)
                root.SetActive(visible);
            if (!visible && card?.investButton != null)
                card.investButton.interactable = false;
        }

        private static void NormalizeCardLayout(GrowthCard card)
        {
            if (card == null) return;

            SetPreferredWidth(card.nameText, 140f);
            SetPreferredWidth(card.rankText, 240f);
            SetPreferredWidth(card.effectText, 160f);

            if (card.rankText != null)
            {
                card.rankText.enableAutoSizing = true;
                card.rankText.fontSizeMin = 17f;
                card.rankText.fontSizeMax = 23f;
            }

            SetSafeSingleLineOverflow(card.nameText);
            SetSafeSingleLineOverflow(card.rankText);
            SetSafeSingleLineOverflow(card.effectText);
            SetSafeSingleLineOverflow(card.milestoneText);
        }

        private static void SetPreferredWidth(TextMeshProUGUI text, float width)
        {
            if (text == null || !text.TryGetComponent(out LayoutElement layout)) return;
            layout.minWidth = width;
            layout.preferredWidth = width;
        }

        private static void SetSafeSingleLineOverflow(TextMeshProUGUI text)
        {
            if (text == null) return;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
        }

        private static string FormatEffect(GrowthInvestmentRule rule)
            => rule.AttributeId == global::UPlayGround.Data.Stat.Attributes.Combat.Defense
                || rule.AttributeId == global::UPlayGround.Data.Stat.Attributes.Combat.CritRate
                || rule.AttributeId == global::UPlayGround.Data.Stat.Attributes.Combat.AttackSpeed
                || rule.AttributeId == global::UPlayGround.Data.Stat.Attributes.Combat.AttackPower
                ? $"{rule.flatPerRank * 100f:0.#}%"
                : $"{rule.flatPerRank:0.#}";

        private static string GetNextMilestoneText(List<GrowthUnlockMilestone> milestones, int rank)
        {
            if (milestones == null || milestones.Count == 0) return "마일스톤 없음";
            GrowthUnlockMilestone? next = null;
            for (int i = 0; i < milestones.Count; i++)
            {
                GrowthUnlockMilestone milestone = milestones[i];
                if (milestone.requiredRank <= rank) continue;
                if (!next.HasValue || milestone.requiredRank < next.Value.requiredRank) next = milestone;
            }
            if (!next.HasValue) return "모든 마일스톤 해금 완료";
            string name = string.IsNullOrWhiteSpace(next.Value.displayName) ? next.Value.unlockId : next.Value.displayName;
            // 기본 TMP 폰트에 가운데점(·) 글리프가 없으면 네모(missing glyph)로 표시된다.
            // 폰트 폴백 구성에 의존하지 않는 ASCII 구분자를 사용한다.
            return $"다음 해금: {next.Value.requiredRank}랭크 - {name}";
        }
    }
}
