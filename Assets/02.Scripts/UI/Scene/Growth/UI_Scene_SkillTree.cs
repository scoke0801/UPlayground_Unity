using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Party;
using UPlayGround.Data.Sound;
using UPlayGround.Manager;
using UPlayGround.UI.HUD.Notification;

namespace UPlayGround.UI
{
    /// <summary>캐릭터별 고정 스킬 트리를 표시하는 전용 팝업.</summary>
    public sealed class UI_Scene_SkillTree : UI_PopupBase
    {
        public const string UIKey = "SkillTree";

        private static readonly Color Panel = new(0.055f, 0.075f, 0.105f, 0.98f);
        private static readonly Color Card = new(0.11f, 0.14f, 0.19f, 1f);
        private static readonly Color Acquired = new(0.25f, 0.70f, 0.48f, 1f);
        private static readonly Color Available = new(0.30f, 0.62f, 0.90f, 1f);
        private static readonly Color Locked = new(0.22f, 0.25f, 0.30f, 1f);
        private static readonly Color LevelLocked = new(0.38f, 0.27f, 0.24f, 1f);
        private static readonly Color Cyan = new(0.25f, 0.75f, 1f, 1f);
        private static readonly Color Gold = new(0.95f, 0.68f, 0.25f, 1f);
        private static readonly Color TextMuted = new(0.62f, 0.70f, 0.78f, 1f);
        private static readonly Vector2 NodeSize = new(190f, 176f);

        [Header("시안 스타일")]
        [SerializeField] private Sprite _nodeFrameSprite;
        [SerializeField] private Sprite _actionButtonSprite;

        private RectTransform _tabRoot;
        private RectTransform _edgeRoot;
        private RectTransform _nodeRoot;
        private TextMeshProUGUI _title;
        private TextMeshProUGUI _points;
        private TextMeshProUGUI _detailName;
        private TextMeshProUGUI _detailState;
        private TextMeshProUGUI _detailEffects;
        private TextMeshProUGUI _preview;
        private TextMeshProUGUI _rankGauge;
        private TextMeshProUGUI _accessNotice;
        private Button _acquireButton;
        private Button _respecButton;
        private Button _closeButton;

        private readonly List<Button> _tabs = new();
        private readonly List<NodeView> _nodes = new();
        private CharacterActorType _targetType;
        private string _selectedNodeId;
        private bool _pausedByThisPopup;
        private bool _respecArmed;
        private bool _missingTreeReported;
        private Sequence _feedbackSequence;
        private Transform _feedbackTarget;

        protected override bool BlocksLowerInput => true;

        protected override void Awake()
        {
            BuildRuntimeView();
            _layer = CanvasLayer.Popup;
            base.Awake();
        }

        public void Configure(CharacterActorType type)
        {
            _targetType = type;
            if (IsVisible)
                RebuildAll();
        }

        protected override void OnInit()
        {
            _closeButton.onClick.AddListener(Hide);
            _acquireButton.onClick.AddListener(TakeSelectedNode);
            _respecButton.onClick.AddListener(Respec);
        }

        protected override void OnDispose()
        {
            KillFeedbackTween();
            if (_closeButton != null) _closeButton.onClick.RemoveListener(Hide);
            if (_acquireButton != null) _acquireButton.onClick.RemoveListener(TakeSelectedNode);
            if (_respecButton != null) _respecButton.onClick.RemoveListener(Respec);
            base.OnDispose();
        }

        protected override void OnShow()
        {
            base.OnShow();
            if (Svc.GameTime != null && !Svc.GameTime.IsPaused)
            {
                Svc.GameTime.SetPause(true);
                _pausedByThisPopup = true;
            }
            if (_targetType == CharacterActorType.None)
                _targetType = UISvc.Party?.ActiveCharacterType ?? CharacterActorType.None;
            if (UISvc.Party != null)
                UISvc.Party.OnSkillProgressChanged += OnProgressChanged;
            RebuildAll();
        }

        protected override void OnHide()
        {
            KillFeedbackTween();
            if (UISvc.Party != null)
                UISvc.Party.OnSkillProgressChanged -= OnProgressChanged;
            if (_pausedByThisPopup)
            {
                Svc.GameTime?.SetPause(false);
                _pausedByThisPopup = false;
            }
            _respecArmed = false;
            base.OnHide();
        }

        private void OnProgressChanged(CharacterActorType type)
        {
            if (type == _targetType) RebuildAll();
        }

        private void RebuildAll()
        {
            RebuildTabs();
            RebuildGraph();
            RefreshDetail();
            ConfigureSpatialNavigation();
        }

        private void RebuildTabs()
        {
            ClearChildren(_tabRoot);
            _tabs.Clear();
            IReadOnlyList<CharacterActorType> roster = UISvc.Party?.Roster;
            if (roster == null) return;
            for (int i = 0; i < roster.Count; i++)
            {
                CharacterActorType type = roster[i];
                int points = UISvc.Party.GetAvailableSkillPoints(type);
                Button tab = MakeCharacterTab(type, points);
                CharacterActorType captured = type;
                tab.onClick.AddListener(() =>
                {
                    Svc.Sound?.PlayUi(GameSoundKey.UiClick);
                    SelectCharacter(captured);
                });
                bool selected = type == _targetType;
                tab.image.color = selected
                    ? new Color(0.10f, 0.28f, 0.42f, 1f)
                    : new Color(0.055f, 0.075f, 0.10f, 0.96f);
                Outline outline = tab.GetComponent<Outline>();
                if (outline != null) outline.enabled = selected;
                _tabs.Add(tab);
            }
            UIFocusNavigation.ConfigureVertical(_tabs);
        }

        private Button MakeCharacterTab(CharacterActorType type, int points)
        {
            GameObject go = NewUI($"Character_{type}", _tabRoot);
            Image background = go.AddComponent<Image>();
            background.color = Card;
            Button button = go.AddComponent<Button>();
            button.targetGraphic = background;
            Outline outline = go.AddComponent<Outline>();
            outline.effectColor = Cyan;
            outline.effectDistance = new Vector2(2f, -2f);
            outline.enabled = false;
            LayoutElement layout = go.AddComponent<LayoutElement>();
            layout.preferredHeight = 88f;
            layout.minHeight = 88f;

            Sprite portrait = UISvc.Party?.PartyMemberDataSO?.GetHeadSprite(type);
            GameObject portraitObject = NewUI("Portrait", go.transform);
            SetTopLeft(portraitObject.transform, new Vector2(8f, -8f), new Vector2(72f, 72f));
            Image portraitImage = portraitObject.AddComponent<Image>();
            portraitImage.sprite = portrait;
            portraitImage.preserveAspect = true;
            portraitImage.color = portrait != null ? Color.white : new Color(0.18f, 0.22f, 0.28f, 1f);
            portraitImage.raycastTarget = false;

            string displayName = UISvc.Party?.PartyMemberDataSO?.GetName(type);
            if (string.IsNullOrWhiteSpace(displayName)) displayName = type.ToString();
            TextMeshProUGUI name = MakeText(go.transform, "Name", displayName, 22f,
                new Vector2(88f, -10f), new Vector2(180f, 34f));
            name.fontStyle = FontStyles.Bold;
            TextMeshProUGUI level = MakeText(go.transform, "Level", $"Lv. {UISvc.Party?.GetLevel(type) ?? 0}", 16f,
                new Vector2(88f, -48f), new Vector2(105f, 26f));
            level.color = TextMuted;

            TextMeshProUGUI pointBadge = MakeText(go.transform, "Points", points > 0 ? $"◆ {points}" : "· 0", 20f,
                new Vector2(276f, -25f), new Vector2(58f, 36f));
            pointBadge.alignment = TextAlignmentOptions.Center;
            pointBadge.color = points > 0 ? Cyan : TextMuted;
            return button;
        }

        private void SelectCharacter(CharacterActorType type)
        {
            _targetType = type;
            _selectedNodeId = null;
            _respecArmed = false;
            RebuildAll();
        }

        private void RebuildGraph()
        {
            ClearChildren(_edgeRoot);
            ClearChildren(_nodeRoot);
            _nodes.Clear();
            CharacterSkillTreeSO tree = UISvc.Party?.GetSkillTree(_targetType);
            _title.text = "성장 보드";
            _points.text = $"◆  잔여 포인트   {UISvc.Party?.GetAvailableSkillPoints(_targetType) ?? 0}";
            _points.color = Cyan;
            _accessNotice.text = "메뉴에서 언제든 투자 가능 · 초기화 무료";
            _accessNotice.color = new Color(0.55f, 0.85f, 0.38f, 1f);
            if (tree?.nodes == null || tree.nodes.Count == 0) return;

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_nodeRoot);
            Dictionary<string, Vector2> displayPositions = BuildDisplayPositions(tree, out bool isFlatDraft);
            if (isFlatDraft)
            {
                _accessNotice.text = "초안 보드 · 선행/Ability 노드 미저작";
                _accessNotice.color = Gold;
            }

            if (string.IsNullOrWhiteSpace(_selectedNodeId)
                || tree.FindNode(_selectedNodeId) == null)
            {
                _selectedNodeId = tree.nodes[0]?.NormalizedId;
            }

            for (int i = 0; i < tree.nodes.Count; i++)
            {
                SkillNodeDefinition node = tree.nodes[i];
                if (node?.requiredNodeIds == null) continue;
                for (int j = 0; j < node.requiredNodeIds.Count; j++)
                {
                    string requiredId = node.requiredNodeIds[j]?.Trim() ?? string.Empty;
                    if (displayPositions.TryGetValue(requiredId, out Vector2 from)
                        && displayPositions.TryGetValue(node.NormalizedId, out Vector2 to))
                    {
                        MakeEdge(from, to);
                    }
                }
            }

            for (int i = 0; i < tree.nodes.Count; i++)
            {
                SkillNodeDefinition node = tree.nodes[i];
                if (node == null) continue;
                if (!displayPositions.TryGetValue(node.NormalizedId, out Vector2 position))
                    position = Vector2.zero;
                Button button = MakeNodeButton(node, position);
                Outline selection = button.targetGraphic != null
                    ? button.targetGraphic.GetComponent<Outline>()
                    : null;
                _nodes.Add(new NodeView(node, button, position, selection));
            }
        }

        private Button MakeNodeButton(SkillNodeDefinition node, Vector2 position)
        {
            int rank = UISvc.Party.GetSkillNodeRank(_targetType, node.NormalizedId);
            bool canTake = CanTakeForDisplay(node.NormalizedId, out SkillNodeBlockReason reason);
            string label = string.IsNullOrWhiteSpace(node.displayNameKey) ? node.NormalizedId : node.displayNameKey;
            GameObject go = NewUI($"Node_{node.NormalizedId}", _nodeRoot);
            Button button = go.AddComponent<Button>();
            RectTransform rect = (RectTransform)button.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = NodeSize;
            rect.anchoredPosition = position;

            GameObject frameObject = NewUI("Frame", go.transform);
            RectTransform frameRect = (RectTransform)frameObject.transform;
            frameRect.anchorMin = frameRect.anchorMax = new Vector2(0.5f, 0.5f);
            frameRect.sizeDelta = new Vector2(112f, 112f);
            frameRect.anchoredPosition = new Vector2(0f, 24f);
            Image frame = frameObject.AddComponent<Image>();
            frame.sprite = _nodeFrameSprite;
            frame.preserveAspect = true;
            frame.color = rank > 0
                ? Acquired
                : canTake ? Available
                : reason == SkillNodeBlockReason.LevelTooLow ? LevelLocked : Locked;
            button.targetGraphic = frame;

            Outline selection = frameObject.AddComponent<Outline>();
            selection.effectColor = Cyan;
            selection.effectDistance = new Vector2(4f, -4f);
            selection.enabled = string.Equals(_selectedNodeId, node.NormalizedId, StringComparison.Ordinal);

            if (node.icon != null)
            {
                GameObject iconObject = NewUI("Icon", frameObject.transform);
                RectTransform iconRect = (RectTransform)iconObject.transform;
                iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                iconRect.sizeDelta = new Vector2(58f, 58f);
                Image icon = iconObject.AddComponent<Image>();
                icon.sprite = node.icon;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
            }
            else
            {
                TextMeshProUGUI glyph = MakeText(frameObject.transform, "Glyph", NodeGlyph(node.NormalizedId), 25f,
                    new Vector2(0f, 0f), new Vector2(78f, 42f));
                RectTransform glyphRect = glyph.rectTransform;
                glyphRect.anchorMin = glyphRect.anchorMax = new Vector2(0.5f, 0.5f);
                glyphRect.pivot = new Vector2(0.5f, 0.5f);
                glyphRect.anchoredPosition = Vector2.zero;
                glyph.alignment = TextAlignmentOptions.Center;
                glyph.fontStyle = FontStyles.Bold;
            }

            TextMeshProUGUI nameText = MakeText(go.transform, "Name", label, 18f,
                new Vector2(0f, -43f), new Vector2(190f, 34f));
            RectTransform nameRect = nameText.rectTransform;
            nameRect.anchorMin = nameRect.anchorMax = new Vector2(0.5f, 0.5f);
            nameRect.pivot = new Vector2(0.5f, 0.5f);
            nameRect.anchoredPosition = new Vector2(0f, -48f);
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.fontStyle = FontStyles.Bold;

            TextMeshProUGUI rankText = MakeText(go.transform, "Rank", $"{rank} / {Mathf.Max(1, node.maxRank)}", 16f,
                new Vector2(0f, -72f), new Vector2(150f, 26f));
            RectTransform rankRect = rankText.rectTransform;
            rankRect.anchorMin = rankRect.anchorMax = new Vector2(0.5f, 0.5f);
            rankRect.pivot = new Vector2(0.5f, 0.5f);
            rankRect.anchoredPosition = new Vector2(0f, -78f);
            rankText.alignment = TextAlignmentOptions.Center;
            rankText.color = rank > 0 ? Acquired : canTake ? Cyan : TextMuted;

            string captured = node.NormalizedId;
            button.onClick.AddListener(() =>
            {
                Svc.Sound?.PlayUi(GameSoundKey.UiClick);
                _selectedNodeId = captured;
                _respecArmed = false;
                RefreshNodeSelection();
                RefreshDetail();
                ConfigureSpatialNavigation();
                PlayFeedback(button.targetGraphic?.transform, includePoints: false);
            });
            return button;
        }

        private static string NodeGlyph(string nodeId)
        {
            if (nodeId.Contains("Vitality", StringComparison.OrdinalIgnoreCase)
                || nodeId.Contains("MaxHealth", StringComparison.OrdinalIgnoreCase)) return "HP";
            if (nodeId.Contains("Endurance", StringComparison.OrdinalIgnoreCase)
                || nodeId.Contains("Stamina", StringComparison.OrdinalIgnoreCase)) return "STA";
            if (nodeId.Contains("Evasion", StringComparison.OrdinalIgnoreCase)) return "EVA";
            if (nodeId.Contains("Defense", StringComparison.OrdinalIgnoreCase)) return "DEF";
            if (nodeId.Contains("KeenEye", StringComparison.OrdinalIgnoreCase)
                || nodeId.Contains("CritRate", StringComparison.OrdinalIgnoreCase)) return "CRIT";
            if (nodeId.Contains("AttackSpeed", StringComparison.OrdinalIgnoreCase)) return "SPD";
            if (nodeId.Contains("SharpenedEdge", StringComparison.OrdinalIgnoreCase)
                || nodeId.Contains("AttackPower", StringComparison.OrdinalIgnoreCase)) return "ATK";
            if (nodeId.Contains("Flowing", StringComparison.OrdinalIgnoreCase)) return "COMBO";
            if (nodeId.Contains("HeavyFinisher", StringComparison.OrdinalIgnoreCase)) return "FIN";
            if (nodeId.Contains("HeavenlyBlade", StringComparison.OrdinalIgnoreCase)) return "ULT";
            return "SKILL";
        }

        private void RefreshNodeSelection()
        {
            for (int i = 0; i < _nodes.Count; i++)
            {
                if (_nodes[i].SelectionOutline != null)
                {
                    _nodes[i].SelectionOutline.enabled = string.Equals(
                        _nodes[i].Definition.NormalizedId,
                        _selectedNodeId,
                        StringComparison.Ordinal);
                }
            }
        }

        private void RefreshDetail()
        {
            CharacterSkillTreeSO tree = UISvc.Party?.GetSkillTree(_targetType);
            SkillNodeDefinition node = tree?.FindNode(_selectedNodeId);
            if (node == null)
            {
                _detailName.text = tree == null ? "성장 경로 없음" : "노드를 선택하세요";
                _detailState.text = string.Empty;
                _detailEffects.text = tree == null
                    ? "아직 펼쳐진 성장 경로가 없습니다."
                    : string.Empty;
                if (tree == null && !_missingTreeReported)
                {
                    _missingTreeReported = true;
                    Debug.LogWarning(
                        $"[SkillTree] {_targetType} 성장 데이터가 연결되지 않았습니다.",
                        this);
                }
                else if (tree != null)
                {
                    _missingTreeReported = false;
                }
                _preview.text = string.Empty;
                if (_rankGauge != null) _rankGauge.text = string.Empty;
                _acquireButton.interactable = false;
                _respecButton.interactable = false;
                return;
            }
            int rank = UISvc.Party.GetSkillNodeRank(_targetType, node.NormalizedId);
            bool canTake = CanTakeForDisplay(node.NormalizedId, out SkillNodeBlockReason reason);
            _detailName.text = string.IsNullOrWhiteSpace(node.displayNameKey) ? node.NormalizedId : node.displayNameKey;
            int maxRank = Mathf.Max(1, node.maxRank);
            _detailState.text = $"랭크 {rank} / {maxRank}   │   비용 ◆ {Mathf.Max(1, node.cost)}   │   {StateLabel(canTake, reason)}";
            _detailState.color = canTake ? Cyan : reason == SkillNodeBlockReason.LevelTooLow ? Gold : TextMuted;
            _detailEffects.text = string.IsNullOrWhiteSpace(node.descriptionKey) ? "효과" : node.descriptionKey;

            var currentEffects = new List<string>();
            var nextEffects = new List<string>();
            int previewRank = Mathf.Min(rank + 1, maxRank);
            if (node.effects != null)
            {
                for (int i = 0; i < node.effects.Count; i++)
                {
                    if (node.effects[i] == null) continue;
                    if (rank > 0) currentEffects.Add(node.effects[i].Describe(rank));
                    nextEffects.Add(node.effects[i].Describe(previewRank));
                }
            }

            var requirements = new List<string>();
            CharacterSkillTreeSO selectedTree = UISvc.Party?.GetSkillTree(_targetType);
            if (node.requiredNodeIds != null)
            {
                for (int i = 0; i < node.requiredNodeIds.Count; i++)
                {
                    string requiredId = node.requiredNodeIds[i]?.Trim() ?? string.Empty;
                    if (string.IsNullOrEmpty(requiredId)) continue;
                    SkillNodeDefinition required = selectedTree?.FindNode(requiredId);
                    string requiredName = required != null && !string.IsNullOrWhiteSpace(required.displayNameKey)
                        ? required.displayNameKey
                        : requiredId;
                    int requiredRank = UISvc.Party?.GetSkillNodeRank(_targetType, requiredId) ?? 0;
                    requirements.Add($"{(requiredRank > 0 ? "충족" : "미충족")}  {requiredName}   {requiredRank} / 1");
                }
            }
            if (node.requiredLevel > 0)
            {
                int level = UISvc.Party?.GetLevel(_targetType) ?? 0;
                requirements.Add($"{(level >= node.requiredLevel ? "충족" : "미충족")}  캐릭터 레벨   {level} / {node.requiredLevel}");
            }
            if (requirements.Count == 0) requirements.Add("선행 조건 없음");

            string currentText = currentEffects.Count > 0 ? string.Join("\n", currentEffects) : "아직 적용된 효과가 없습니다.";
            string nextText = nextEffects.Count > 0 ? string.Join("\n", nextEffects) : "효과 없음";
            string nextRankPreview = rank >= maxRank
                ? "최대 랭크에 도달했습니다."
                : $"랭크 {rank}  →  {previewRank}\n{nextText}";
            _preview.text =
                $"<color=#58C8FF>현재 효과</color>\n{currentText}\n\n" +
                $"<color=#58C8FF>다음 랭크 미리보기</color>\n{nextRankPreview}\n\n" +
                $"<color=#58C8FF>선행 조건</color>\n{string.Join("\n", requirements)}";

            if (_rankGauge != null)
                _rankGauge.text = BuildRankGauge(rank, maxRank);
            SetButtonLabel(_acquireButton, $"노드 취득     ◆ {Mathf.Max(1, node.cost)}");
            if (!_respecArmed) SetButtonLabel(_respecButton, "전체 리스펙");
            _acquireButton.interactable = canTake;
            _respecButton.interactable = HasSpentNodes(tree);
        }

        private static string BuildRankGauge(int rank, int maxRank)
        {
            const int segments = 5;
            int filled = maxRank <= 1
                ? (rank > 0 ? segments : 0)
                : Mathf.Clamp(Mathf.CeilToInt(rank / (float)maxRank * segments), 0, segments);
            return $"랭크 진행     <color=#58C8FF>{new string('●', filled)}</color>" +
                   $"<color=#506070>{new string('○', segments - filled)}</color>     {rank} / {maxRank}";
        }

        private bool HasSpentNodes(CharacterSkillTreeSO tree)
        {
            if (tree?.nodes == null) return false;
            for (int i = 0; i < tree.nodes.Count; i++)
                if (tree.nodes[i] != null && UISvc.Party.GetSkillNodeRank(_targetType, tree.nodes[i].NormalizedId) > 0)
                    return true;
            return false;
        }

        private bool CanTakeForDisplay(
            string nodeId,
            out SkillNodeBlockReason reason) =>
            UISvc.Party.CanTakeSkillNode(_targetType, nodeId, out reason);

        private void TakeSelectedNode()
        {
            CharacterSkillTreeSO tree = UISvc.Party?.GetSkillTree(_targetType);
            SkillNodeDefinition node = tree?.FindNode(_selectedNodeId);
            int previousRank = node == null
                ? 0
                : UISvc.Party.GetSkillNodeRank(_targetType, node.NormalizedId);
            if (node == null
                || UISvc.Party?.TryTakeSkillNode(_targetType, _selectedNodeId) != true)
                return;

            PlayFeedback(FindSelectedNodeTransform(), includePoints: true);
            if (Svc.Sound?.HasSound(GameSoundKey.LevelUp) == true)
                Svc.Sound.PlayUi(GameSoundKey.LevelUp);
            if (previousRank == 0 && GrantsNewAction(node))
            {
                UI_Scene_Notification.ShowSystemMessage(
                    "새 기술 획득",
                    string.IsNullOrWhiteSpace(node.displayNameKey)
                        ? "새로운 전투 기술을 사용할 수 있습니다."
                        : node.displayNameKey,
                    node.icon);
            }
        }

        private void Respec()
        {
            if (!_respecArmed)
            {
                _respecArmed = true;
                SetButtonLabel(_respecButton, "다시 눌러 전체 초기화");
                return;
            }
            bool reset = UISvc.Party?.TryRespecSkillTree(_targetType) == true;
            _respecArmed = false;
            SetButtonLabel(_respecButton, "전체 리스펙");
            if (!reset) return;

            PlayFeedback(null, includePoints: true);
            UI_Scene_Notification.ShowSystemMessage(
                "성장 초기화",
                "사용한 포인트를 모두 돌려받았습니다.");
        }

        private static bool GrantsNewAction(SkillNodeDefinition node)
        {
            if (node?.effects == null) return false;
            for (int i = 0; i < node.effects.Count; i++)
                if (node.effects[i] is AbilityUnlockEffect or PassiveGrantEffect)
                    return true;
            return false;
        }

        private static string StateLabel(bool canTake, SkillNodeBlockReason reason) => canTake ? "취득 가능" : reason switch
        {
            SkillNodeBlockReason.MaxRank => "취득 완료",
            SkillNodeBlockReason.MissingPrerequisite => "선행 미충족",
            SkillNodeBlockReason.LevelTooLow => "레벨 미달",
            SkillNodeBlockReason.InsufficientPoints => "포인트 부족",
            _ => "취득 불가",
        };

        private void ConfigureSpatialNavigation()
        {
            Button selectedTab = FindSelectedCharacterTab();
            Selectable detailEntry = UIFocusNavigation.FirstNavigable(
                _acquireButton,
                _respecButton,
                _closeButton);
            for (int i = 0; i < _nodes.Count; i++)
            {
                Navigation nav = new() { mode = Navigation.Mode.Explicit };
                nav.selectOnLeft = FindDirectional(i, Vector2.left);
                nav.selectOnRight = FindDirectional(i, Vector2.right);
                nav.selectOnUp = FindDirectional(i, Vector2.up);
                nav.selectOnDown = FindDirectional(i, Vector2.down);
                nav.selectOnLeft ??= selectedTab;
                nav.selectOnRight ??= detailEntry;
                _nodes[i].Button.navigation = nav;
            }

            Button selectedNode = FindSelectedNodeButton();
            for (int i = 0; i < _tabs.Count; i++)
            {
                Navigation tabNavigation = _tabs[i].navigation;
                tabNavigation.mode = Navigation.Mode.Explicit;
                tabNavigation.selectOnRight = selectedNode;
                _tabs[i].navigation = tabNavigation;
            }

            ConfigureActionNavigation(selectedNode);
            SetDefaultFocus(selectedNode ?? (_nodes.Count > 0 ? _nodes[0].Button : _closeButton), IsVisible);
        }

        private void ConfigureActionNavigation(Button selectedNode)
        {
            if (_acquireButton != null)
            {
                Navigation acquire = new() { mode = Navigation.Mode.Explicit };
                acquire.selectOnLeft = selectedNode;
                acquire.selectOnUp = _closeButton;
                acquire.selectOnDown = UIFocusNavigation.IsNavigable(_respecButton)
                    ? _respecButton
                    : _closeButton;
                _acquireButton.navigation = acquire;
            }
            if (_respecButton != null)
            {
                Navigation respec = new() { mode = Navigation.Mode.Explicit };
                respec.selectOnLeft = selectedNode;
                respec.selectOnUp = UIFocusNavigation.IsNavigable(_acquireButton)
                    ? _acquireButton
                    : _closeButton;
                respec.selectOnDown = _closeButton;
                _respecButton.navigation = respec;
            }
            if (_closeButton != null)
            {
                Navigation close = new() { mode = Navigation.Mode.Explicit };
                close.selectOnLeft = selectedNode;
                close.selectOnDown = UIFocusNavigation.FirstNavigable(
                    _acquireButton,
                    _respecButton,
                    selectedNode);
                _closeButton.navigation = close;
            }
        }

        private Button FindSelectedCharacterTab()
        {
            IReadOnlyList<CharacterActorType> roster = UISvc.Party?.Roster;
            if (roster == null) return _tabs.Count > 0 ? _tabs[0] : null;
            for (int i = 0; i < roster.Count && i < _tabs.Count; i++)
                if (roster[i] == _targetType)
                    return _tabs[i];
            return _tabs.Count > 0 ? _tabs[0] : null;
        }

        private Button FindSelectedNodeButton()
        {
            for (int i = 0; i < _nodes.Count; i++)
                if (string.Equals(
                        _nodes[i].Definition.NormalizedId,
                        _selectedNodeId,
                        StringComparison.Ordinal))
                    return _nodes[i].Button;
            return null;
        }

        private Transform FindSelectedNodeTransform() =>
            FindSelectedNodeButton()?.targetGraphic?.transform;

        private Selectable FindDirectional(int sourceIndex, Vector2 direction)
        {
            Vector2 origin = _nodes[sourceIndex].Position;
            float best = float.MaxValue;
            Button result = null;
            for (int i = 0; i < _nodes.Count; i++)
            {
                if (i == sourceIndex) continue;
                Vector2 delta = _nodes[i].Position - origin;
                float forward = Vector2.Dot(delta, direction);
                if (forward <= 0f) continue;
                float score = forward + Mathf.Abs(Vector2.Dot(delta, new Vector2(-direction.y, direction.x))) * 2f;
                if (score < best) { best = score; result = _nodes[i].Button; }
            }
            return result;
        }

        private void PlayFeedback(Transform target, bool includePoints)
        {
            KillFeedbackTween();
            _feedbackTarget = target;
            if (_feedbackTarget != null)
                _feedbackTarget.localScale = Vector3.one;
            if (_points != null)
            {
                _points.rectTransform.localScale = Vector3.one;
                _points.color = includePoints ? Gold : Cyan;
            }

            _feedbackSequence = DOTween.Sequence().SetUpdate(true);
            if (_feedbackTarget != null)
            {
                _feedbackSequence.Append(
                    DOTween.To(
                            () => _feedbackTarget.localScale,
                            value =>
                            {
                                if (_feedbackTarget != null)
                                    _feedbackTarget.localScale = value;
                            },
                            Vector3.one * 1.14f,
                            0.12f)
                        .SetEase(Ease.OutBack)
                        .SetUpdate(true));
                _feedbackSequence.Append(
                    DOTween.To(
                            () => _feedbackTarget != null
                                ? _feedbackTarget.localScale
                                : Vector3.one,
                            value =>
                            {
                                if (_feedbackTarget != null)
                                    _feedbackTarget.localScale = value;
                            },
                            Vector3.one,
                            0.16f)
                        .SetEase(Ease.OutCubic)
                        .SetUpdate(true));
            }
            if (includePoints && _points != null)
            {
                _feedbackSequence.Join(
                    DOTween.To(
                            () => _points.rectTransform.localScale,
                            value => _points.rectTransform.localScale = value,
                            Vector3.one * 1.1f,
                            0.12f)
                        .SetEase(Ease.OutBack)
                        .SetUpdate(true));
                _feedbackSequence.Append(
                    DOTween.To(
                            () => _points.rectTransform.localScale,
                            value => _points.rectTransform.localScale = value,
                            Vector3.one,
                            0.16f)
                        .SetEase(Ease.OutCubic)
                        .SetUpdate(true));
                _feedbackSequence.Join(
                    DOTween.To(
                            () => _points.color,
                            value => _points.color = value,
                            Cyan,
                            0.16f)
                        .SetUpdate(true));
            }
        }

        private void KillFeedbackTween()
        {
            if (_feedbackSequence != null && _feedbackSequence.IsActive())
                _feedbackSequence.Kill();
            _feedbackSequence = null;
            if (_feedbackTarget != null)
                _feedbackTarget.localScale = Vector3.one;
            _feedbackTarget = null;
            if (_points != null)
            {
                _points.rectTransform.localScale = Vector3.one;
                _points.color = Cyan;
            }
        }

        private void MakeEdge(Vector2 from, Vector2 to)
        {
            GameObject go = NewUI("Edge", _edgeRoot);
            Image image = go.AddComponent<Image>();
            image.color = new Color(0.32f, 0.42f, 0.52f, 0.8f);
            image.raycastTarget = false;
            RectTransform rect = (RectTransform)go.transform;
            Vector2 delta = to - from;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = (from + to) * 0.5f;
            rect.sizeDelta = new Vector2(delta.magnitude, 4f);
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

            GameObject junction = NewUI("Junction", _edgeRoot);
            Image junctionImage = junction.AddComponent<Image>();
            junctionImage.color = image.color;
            junctionImage.raycastTarget = false;
            RectTransform junctionRect = (RectTransform)junction.transform;
            junctionRect.anchorMin = junctionRect.anchorMax = new Vector2(0.5f, 0.5f);
            junctionRect.anchoredPosition = (from + to) * 0.5f;
            junctionRect.sizeDelta = new Vector2(15f, 15f);
            junctionRect.localRotation = Quaternion.Euler(0f, 0f, 45f);
        }

        private Dictionary<string, Vector2> BuildDisplayPositions(
            CharacterSkillTreeSO tree,
            out bool isFlatDraft)
        {
            var positions = new Dictionary<string, Vector2>(StringComparer.Ordinal);
            var validNodes = new List<SkillNodeDefinition>();
            bool hasPrerequisite = false;
            for (int i = 0; i < tree.nodes.Count; i++)
            {
                SkillNodeDefinition node = tree.nodes[i];
                if (node == null || string.IsNullOrWhiteSpace(node.NormalizedId)) continue;
                validNodes.Add(node);
                hasPrerequisite |= node.requiredNodeIds != null && node.requiredNodeIds.Count > 0;
            }

            isFlatDraft = !hasPrerequisite && validNodes.Count > 1;
            Vector2 graphSize = _nodeRoot.rect.size;
            if (graphSize.x < 600f || graphSize.y < 400f)
                graphSize = new Vector2(1400f, 900f);

            if (isFlatDraft)
            {
                BuildFlatBoardPositions(validNodes, graphSize, positions);
                return positions;
            }

            BuildHierarchyPositions(validNodes, graphSize, positions);
            return positions;
        }

        private static void BuildFlatBoardPositions(
            List<SkillNodeDefinition> nodes,
            Vector2 graphSize,
            Dictionary<string, Vector2> positions)
        {
            int columns = Mathf.Min(3, Mathf.Max(1, nodes.Count));
            int rows = Mathf.CeilToInt(nodes.Count / (float)columns);
            float horizontalGap = Mathf.Min(420f, (graphSize.x - NodeSize.x - 140f) / Mathf.Max(1, columns - 1));
            float verticalGap = Mathf.Min(320f, (graphSize.y - NodeSize.y - 160f) / Mathf.Max(1, rows - 1));

            int index = 0;
            for (int row = 0; row < rows; row++)
            {
                int countInRow = Mathf.Min(columns, nodes.Count - index);
                float rowWidth = (countInRow - 1) * horizontalGap;
                float y = rows == 1 ? 0f : ((rows - 1) * 0.5f - row) * verticalGap;
                for (int column = 0; column < countInRow; column++, index++)
                {
                    float x = -rowWidth * 0.5f + column * horizontalGap;
                    positions[nodes[index].NormalizedId] = new Vector2(x, y);
                }
            }
        }

        private static void BuildHierarchyPositions(
            List<SkillNodeDefinition> nodes,
            Vector2 graphSize,
            Dictionary<string, Vector2> positions)
        {
            var byId = new Dictionary<string, SkillNodeDefinition>(StringComparer.Ordinal);
            for (int i = 0; i < nodes.Count; i++) byId[nodes[i].NormalizedId] = nodes[i];

            var depths = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int pass = 0; pass < nodes.Count; pass++)
            {
                for (int i = 0; i < nodes.Count; i++)
                {
                    SkillNodeDefinition node = nodes[i];
                    int depth = 0;
                    if (node.requiredNodeIds != null)
                    {
                        for (int j = 0; j < node.requiredNodeIds.Count; j++)
                        {
                            string requiredId = node.requiredNodeIds[j]?.Trim() ?? string.Empty;
                            if (byId.ContainsKey(requiredId) && depths.TryGetValue(requiredId, out int requiredDepth))
                                depth = Mathf.Max(depth, requiredDepth + 1);
                        }
                    }
                    depths[node.NormalizedId] = depth;
                }
            }

            int maxDepth = 0;
            var columns = new Dictionary<int, List<SkillNodeDefinition>>();
            for (int i = 0; i < nodes.Count; i++)
            {
                int depth = depths[nodes[i].NormalizedId];
                maxDepth = Mathf.Max(maxDepth, depth);
                if (!columns.TryGetValue(depth, out List<SkillNodeDefinition> column))
                    columns.Add(depth, column = new List<SkillNodeDefinition>());
                column.Add(nodes[i]);
            }

            float usableWidth = Mathf.Max(0f, graphSize.x - NodeSize.x - 160f);
            float usableHeight = Mathf.Max(0f, graphSize.y - NodeSize.y - 140f);
            foreach (KeyValuePair<int, List<SkillNodeDefinition>> pair in columns)
            {
                List<SkillNodeDefinition> column = pair.Value;
                column.Sort((a, b) => b.layoutPosition.y.CompareTo(a.layoutPosition.y));
                float x = maxDepth == 0 ? 0f : -usableWidth * 0.5f + usableWidth * pair.Key / maxDepth;
                for (int row = 0; row < column.Count; row++)
                {
                    float y = column.Count == 1
                        ? 0f
                        : usableHeight * 0.5f - usableHeight * row / (column.Count - 1);
                    positions[column[row].NormalizedId] = new Vector2(x, y);
                }
            }
        }

        private void BuildRuntimeView()
        {
            // 프리팹 빌드/재직렬화 과정에서 루트 Transform 값이 오염돼도
            // UIManager의 Canvas 아래에서 화면 전체를 정상적으로 채우도록 복구한다.
            RectTransform self = (RectTransform)transform;
            self.localScale = Vector3.one;
            self.localRotation = Quaternion.identity;
            self.anchorMin = Vector2.zero;
            self.anchorMax = Vector2.one;
            self.anchoredPosition = Vector2.zero;
            self.offsetMin = Vector2.zero;
            self.offsetMax = Vector2.zero;

            Transform existing = transform.Find("SkillTreeRuntimeRoot");
            if (existing != null)
            {
                ApplyResponsiveLayout(existing);
                BindRuntimeView(existing);
                return;
            }
            GameObject root = NewUI("SkillTreeRuntimeRoot", transform); Stretch(root);
            root.AddComponent<Image>().color = new Color(0.008f, 0.018f, 0.026f, 0.985f);
            GameObject window = NewUI("Window", root.transform); Stretch(window); window.AddComponent<Image>().color = Panel;

            _title = MakeText(window.transform, "Title", "성장 보드", 32, new Vector2(28, -20), new Vector2(520, 55));
            _title.fontStyle = FontStyles.Bold;
            _points = MakeText(window.transform, "Points", "◆  잔여 포인트   0", 27, new Vector2(760, -22), new Vector2(400, 48));
            _points.alignment = TextAlignmentOptions.Center;
            _accessNotice = MakeText(window.transform, "Access", string.Empty, 18, new Vector2(1100, -24), new Vector2(440, 44));
            _accessNotice.alignment = TextAlignmentOptions.Right;
            _closeButton = MakeButton(window.transform, "Close", "×"); Place(_closeButton, new Vector2(1520, -14), new Vector2(58, 58));
            TextMeshProUGUI closeLabel = _closeButton.GetComponentInChildren<TextMeshProUGUI>();
            if (closeLabel != null) closeLabel.fontSize = 34f;

            GameObject headerLine = NewUI("HeaderLine", window.transform);
            Image headerLineImage = headerLine.AddComponent<Image>();
            headerLineImage.color = new Color(0.25f, 0.58f, 0.78f, 0.32f);
            headerLineImage.raycastTarget = false;

            GameObject tabs = NewUI("CharacterTabs", window.transform); Place(tabs, new Vector2(20, -90), new Vector2(340, 790));
            VerticalLayoutGroup tabLayout = tabs.AddComponent<VerticalLayoutGroup>();
            ConfigureTabLayout(tabLayout);
            _tabRoot = (RectTransform)tabs.transform;
            GameObject graph = NewUI("Graph", window.transform); Place(graph, new Vector2(380, -90), new Vector2(900, 790));
            graph.AddComponent<Image>().color = new Color(0.018f, 0.028f, 0.042f, 1f);
            graph.AddComponent<Outline>().effectColor = new Color(0.25f, 0.48f, 0.62f, 0.35f);
            graph.AddComponent<RectMask2D>();
            GameObject edges = NewUI("Edges", graph.transform); Stretch(edges); _edgeRoot = (RectTransform)edges.transform;
            GameObject nodes = NewUI("Nodes", graph.transform); Stretch(nodes); _nodeRoot = (RectTransform)nodes.transform;
            GameObject detail = NewUI("Detail", window.transform); Place(detail, new Vector2(1190, -90), new Vector2(520, 790));
            detail.AddComponent<Image>().color = new Color(0.045f, 0.065f, 0.09f, 0.99f);
            detail.AddComponent<Outline>().effectColor = new Color(0.28f, 0.48f, 0.62f, 0.55f);
            _detailName = MakeText(detail.transform, "Name", "노드 선택", 30, new Vector2(24, -26), new Vector2(472, 48));
            _detailName.fontStyle = FontStyles.Bold;
            _detailState = MakeText(detail.transform, "State", string.Empty, 18, new Vector2(24, -84), new Vector2(472, 58));
            _detailEffects = MakeText(detail.transform, "Description", string.Empty, 19, new Vector2(24, -150), new Vector2(472, 92));
            _detailEffects.color = TextMuted;
            _preview = MakeText(detail.transform, "Preview", string.Empty, 18, new Vector2(24, -264), new Vector2(472, 300));
            _rankGauge = MakeText(detail.transform, "RankGauge", "랭크 진행", 18, new Vector2(24, -600), new Vector2(472, 48));
            _rankGauge.alignment = TextAlignmentOptions.MidlineLeft;
            _acquireButton = MakeButton(detail.transform, "Acquire", "노드 취득"); Place(_acquireButton, new Vector2(24, -665), new Vector2(472, 64));
            _respecButton = MakeButton(detail.transform, "Respec", "전체 리스펙"); Place(_respecButton, new Vector2(24, -735), new Vector2(472, 58));
            StyleActionButton(_acquireButton, Cyan);
            StyleActionButton(_respecButton, Gold);

            ApplyResponsiveLayout(root.transform);
        }

#if UNITY_EDITOR
        public void RebuildEditorPreview()
        {
            Transform existing = transform.Find("SkillTreeRuntimeRoot");
            if (existing != null)
                DestroyImmediate(existing.gameObject);
            BuildRuntimeView();
        }
#endif

        private void BindRuntimeView(Transform root)
        {
            Transform window = root.Find("Window");
            Transform graph = window?.Find("Graph");
            Transform detail = window?.Find("Detail");
            _title = window?.Find("Title")?.GetComponent<TextMeshProUGUI>();
            _points = window?.Find("Points")?.GetComponent<TextMeshProUGUI>();
            _accessNotice = window?.Find("Access")?.GetComponent<TextMeshProUGUI>();
            _closeButton = window?.Find("Close")?.GetComponent<Button>();
            _tabRoot = window?.Find("CharacterTabs") as RectTransform;
            _edgeRoot = graph?.Find("Edges") as RectTransform;
            _nodeRoot = graph?.Find("Nodes") as RectTransform;
            _detailName = detail?.Find("Name")?.GetComponent<TextMeshProUGUI>();
            _detailState = detail?.Find("State")?.GetComponent<TextMeshProUGUI>();
            _detailEffects = detail?.Find("Description")?.GetComponent<TextMeshProUGUI>();
            _preview = detail?.Find("Preview")?.GetComponent<TextMeshProUGUI>();
            _rankGauge = detail?.Find("RankGauge")?.GetComponent<TextMeshProUGUI>();
            _acquireButton = detail?.Find("Acquire")?.GetComponent<Button>();
            _respecButton = detail?.Find("Respec")?.GetComponent<Button>();
        }

        private static void ApplyResponsiveLayout(Transform root)
        {
            Transform window = root?.Find("Window");
            if (window == null) return;

            Stretch(window.gameObject);
            SetTopLeft(window.Find("Title"), new Vector2(34f, -20f), new Vector2(520f, 55f));
            SetTopCenter(window.Find("Points"), new Vector2(0f, -22f), new Vector2(430f, 48f));
            SetTopRight(window.Find("Access"), new Vector2(-104f, -26f), new Vector2(440f, 40f));
            SetTopRight(window.Find("Close"), new Vector2(-24f, -14f), new Vector2(58f, 58f));
            SetTopLeft(window.Find("HeaderLine"), new Vector2(20f, -78f), new Vector2(2520f, 2f));

            SetLeftStretchRect(window.Find("CharacterTabs"),
                new Vector2(24f, 24f), new Vector2(364f, -96f));
            VerticalLayoutGroup tabLayout = window.Find("CharacterTabs")?.GetComponent<VerticalLayoutGroup>();
            if (tabLayout != null) ConfigureTabLayout(tabLayout);
            SetStretchRect(window.Find("Graph"),
                new Vector2(384f, 24f), new Vector2(-564f, -96f));
            Transform graph = window.Find("Graph");
            if (graph != null && graph.GetComponent<RectMask2D>() == null)
                graph.gameObject.AddComponent<RectMask2D>();
            Transform detail = window.Find("Detail");
            SetRightStretchRect(detail,
                new Vector2(-544f, 24f), new Vector2(-24f, -96f));

            if (detail == null) return;
            SetStretchRect(detail.Find("Preview"),
                new Vector2(24f, 270f), new Vector2(-24f, -264f));
            SetBottomLeft(detail.Find("RankGauge"), new Vector2(24f, 190f), new Vector2(472f, 48f));
            SetBottomLeft(detail.Find("Acquire"), new Vector2(24f, 100f), new Vector2(472f, 64f));
            SetBottomLeft(detail.Find("Respec"), new Vector2(24f, 26f), new Vector2(472f, 58f));
        }

        private static void ConfigureTabLayout(VerticalLayoutGroup layout)
        {
            layout.spacing = 8f;
            layout.padding = new RectOffset(4, 4, 4, 4);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        private void StyleActionButton(Button button, Color accent)
        {
            if (button == null) return;
            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = _actionButtonSprite;
                image.color = new Color(accent.r * 0.42f, accent.g * 0.42f, accent.b * 0.42f, 1f);
            }
            Outline outline = button.GetComponent<Outline>() ?? button.gameObject.AddComponent<Outline>();
            outline.effectColor = accent;
            outline.effectDistance = new Vector2(1f, -1f);
            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.fontSize = 21f;
                label.fontStyle = FontStyles.Bold;
            }
        }

        private static void SetTopLeft(Transform target, Vector2 position, Vector2 size)
        {
            if (target is not RectTransform rect) return;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void SetTopCenter(Transform target, Vector2 position, Vector2 size)
        {
            if (target is not RectTransform rect) return;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void SetTopRight(Transform target, Vector2 position, Vector2 size)
        {
            if (target is not RectTransform rect) return;
            rect.anchorMin = rect.anchorMax = Vector2.one;
            rect.pivot = Vector2.one;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void SetBottomLeft(Transform target, Vector2 position, Vector2 size)
        {
            if (target is not RectTransform rect) return;
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void SetStretchRect(Transform target, Vector2 offsetMin, Vector2 offsetMax)
        {
            if (target is not RectTransform rect) return;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void SetLeftStretchRect(Transform target, Vector2 offsetMin, Vector2 offsetMax)
        {
            if (target is not RectTransform rect) return;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void SetRightStretchRect(Transform target, Vector2 offsetMin, Vector2 offsetMax)
        {
            if (target is not RectTransform rect) return;
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static GameObject NewUI(string name, Transform parent) { var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false); return go; }
        private static void Stretch(GameObject go) { RectTransform r=(RectTransform)go.transform; r.anchorMin=Vector2.zero; r.anchorMax=Vector2.one; r.offsetMin=r.offsetMax=Vector2.zero; }
        private static void Place(Component component, Vector2 topLeft, Vector2 size) => Place(component.gameObject, topLeft, size);
        private static void Place(GameObject go, Vector2 topLeft, Vector2 size) { RectTransform r=(RectTransform)go.transform; r.anchorMin=r.anchorMax=new Vector2(0,1); r.pivot=new Vector2(0,1); r.anchoredPosition=topLeft; r.sizeDelta=size; }
        private static TextMeshProUGUI MakeText(Transform parent, string name, string value, float size, Vector2 pos, Vector2 bounds) { GameObject go=NewUI(name,parent); Place(go,pos,bounds); var text=go.AddComponent<TextMeshProUGUI>(); text.text=value; text.fontSize=size; text.color=Color.white; text.textWrappingMode=TextWrappingModes.Normal; return text; }
        private static Button MakeButton(Transform parent, string name, string label) { GameObject go=NewUI(name,parent); var image=go.AddComponent<Image>(); image.color=Card; var button=go.AddComponent<Button>(); GameObject textGo=NewUI("Label",go.transform); Stretch(textGo); var text=textGo.AddComponent<TextMeshProUGUI>(); text.text=label; text.fontSize=20; text.alignment=TextAlignmentOptions.Center; text.color=Color.white; return button; }
        private static void SetButtonLabel(Button button, string value) { TextMeshProUGUI text=button?.GetComponentInChildren<TextMeshProUGUI>(); if (text != null) text.text=value; }
        private static void ClearChildren(Transform root)
        {
            if (root == null) return;
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                GameObject child = root.GetChild(i).gameObject;
                // Destroy는 프레임 끝 지연 파괴라 같은 프레임에 구 오브젝트가 남는다.
                // 비활성화 후 부모에서 즉시 떼어내 레이아웃 배치와 rect 계산에서 바로 제외한다.
                child.SetActive(false);
                child.transform.SetParent(null, false);
                Destroy(child);
            }
        }

        private readonly struct NodeView
        {
            public SkillNodeDefinition Definition { get; }
            public Button Button { get; }
            public Vector2 Position { get; }
            public Outline SelectionOutline { get; }
            public NodeView(
                SkillNodeDefinition definition,
                Button button,
                Vector2 position,
                Outline selectionOutline)
            {
                Definition = definition;
                Button = button;
                Position = position;
                SelectionOutline = selectionOutline;
            }
        }
    }
}
