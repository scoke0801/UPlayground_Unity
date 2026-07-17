using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UPlayGround;
using UPlayGround.Data.EnumType;
using UPlayGround.InputDefine;
using UPlayGround.Manager;

namespace UPlayGround.UI
{
    /// <summary>
    /// 파티원 선택 / 편성 화면.
    /// - 스왑 모드(기본): 출전 슬롯 클릭 시 즉시 활성 캐릭터 교체.
    /// - 편성 모드: 출전 슬롯 / 후보 슬롯을 조작해 BattleOrder 를 변경. 즉시 반영.
    /// 자세한 규칙: docs/party-formation-system.md
    /// </summary>
    public class UI_PartySelect : UI_Base
    {
        [Header("Slot")]
        [SerializeField] private UI_PartyMemberSlot _slotPrefab;
        [SerializeField] private Transform _slotRoot;

        [Header("Roster Drawer")]
        [FormerlySerializedAs("_candidatePrefab")]
        [SerializeField] private UI_PartyMemberSlot _rosterSlotPrefab;
        [FormerlySerializedAs("_candidateRoot")]
        [SerializeField] private Transform _rosterRoot;
        [FormerlySerializedAs("_candidatePanel")]
        [SerializeField] private GameObject _rosterPanel;
        [SerializeField] private Button _rosterToggleButton;
        [SerializeField] private Button _rosterCloseButton;
        [SerializeField] private TextMeshProUGUI _rosterToggleText;
        [SerializeField] private TextMeshProUGUI _rosterCountText;

        [Header("Current")]
        [SerializeField] private RawImage _characterPreview;
        [SerializeField] private UICharacterPreviewRenderer _previewRenderer;
        [SerializeField] private TextMeshProUGUI _currentNameText;
        [SerializeField] private TextMeshProUGUI _currentHpText;
        [SerializeField] private Image _currentHpFill;

        [Header("Header")]
        [SerializeField] private TextMeshProUGUI _battleSizeText;
        [SerializeField] private TextMeshProUGUI _selectedSlotText;
        [SerializeField] private Toggle _formationToggle;

        [Header("Party Field")]
        [SerializeField] private RawImage[] _fieldPreviews;
        [SerializeField] private UICharacterPreviewRenderer[] _fieldPreviewRenderers;

        [Header("Button")]
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _removeSlotButton;

        [Header("Option")]
        [SerializeField] private bool _pauseGameOnShow = true;
        [SerializeField] private bool _hideAfterSelect = true;

        private readonly List<UI_PartyMemberSlot> _slots = new();
        private readonly List<UI_PartyMemberSlot> _rosterSlots = new();
        private int _previewBattleIndex = -1;
        private int _selectedBattleIndex = -1;
        private bool _rosterOpen = false;

        protected override void Awake()
        {
            base.Awake();

            _layer = CanvasLayer.Scene;

            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(Hide);
            }

            if (_rosterToggleButton != null)
            {
                _rosterToggleButton.onClick.AddListener(ToggleRosterDrawer);
            }

            if (_rosterCloseButton != null)
            {
                _rosterCloseButton.onClick.AddListener(CloseRosterDrawer);
            }

            if (_removeSlotButton != null)
            {
                _removeSlotButton.onClick.AddListener(RemoveSelectedBattleSlot);
            }

            if (_formationToggle != null)
            {
                _formationToggle.onValueChanged.AddListener(SetFormationMode);
            }

            if (_previewRenderer != null && _characterPreview != null)
            {
                _characterPreview.texture = _previewRenderer.GetRenderTexture();
            }

            BindFieldPreviewTextures();
        }

        // 입력 레이어 상승/복원은 UI_Base가 BlocksLowerInput 기준으로 일괄 처리한다.
        protected override bool BlocksLowerInput => true;

        protected override void OnShow()
        {
            base.OnShow();

            if (_pauseGameOnShow)
            {
                Svc.GameTime?.SetPause(true);
            }

            var partyManager = UISvc.Party;
            if (partyManager != null)
            {
                partyManager.OnSwapCompleted += OnSwapCompleted;
                partyManager.OnCharacterUnlocked += OnCharacterUnlocked;
                partyManager.OnRosterChanged += OnRosterChanged;
                partyManager.OnBattleOrderChanged += OnBattleOrderChanged;
            }

            _selectedBattleIndex = -1;
            SetRosterDrawer(false);
            Refresh();
            PreviewMember(partyManager?.ActiveIndex ?? 0);
        }

        protected override void OnHide()
        {
            var partyManager = UISvc.Party;
            if (partyManager != null)
            {
                partyManager.OnSwapCompleted -= OnSwapCompleted;
                partyManager.OnCharacterUnlocked -= OnCharacterUnlocked;
                partyManager.OnRosterChanged -= OnRosterChanged;
                partyManager.OnBattleOrderChanged -= OnBattleOrderChanged;
            }

            if (_pauseGameOnShow)
            {
                Svc.GameTime?.SetPause(false);
            }

            if (_previewRenderer != null)
            {
                _previewRenderer.HidePreview();
            }

            HideFieldPreviews();

            base.OnHide();
        }

        public override bool PerformBackFunction()
        {
            Hide();
            return false;
        }

        public void SetFormationMode(bool on)
        {
            SetRosterDrawer(on);
        }

        public void Refresh()
        {
            var partyManager = UISvc.Party;
            PlayerActor player = partyManager?.ActiveCharacter;

            if (partyManager == null || player == null)
            {
                SetCurrentInfo(CharacterActorType.None, 0f, 0f);
                SetSlotCount(0);
                SetRosterSlotCount(0);
                UpdateBattleSizeText(0, 0);
                UpdateRosterHeader(0, 0);
                UpdateSelectedSlotState();
                HideFieldPreviews();
                return;
            }

            CharacterActorType activeType = partyManager.ActiveCharacterType;
            SetCurrentInfo(activeType, player.GetHealthForCharacter(activeType), player.GetMaxHealthForCharacter(activeType));

            IReadOnlyList<CharacterActorType> battleOrder = partyManager.BattleOrder;
            int maxBattle = partyManager.MaxBattleSize;
            int slotCount = maxBattle;

            SetSlotCount(slotCount);
            UpdateBattleSizeText(battleOrder.Count, maxBattle);
            UpdateFieldPreviews(battleOrder);

            for (int i = 0; i < slotCount; ++i)
            {
                if (i < battleOrder.Count)
                {
                    CharacterActorType type = battleOrder[i];
                    float maxHp = player.GetMaxHealthForCharacter(type);
                    float currentHp = player.HasHealthRecordForCharacter(type)
                        ? player.GetHealthForCharacter(type)
                        : maxHp;
                    bool isActive = (i == partyManager.ActiveIndex);

                    bool canSelect = _rosterOpen || partyManager.CanSwapTo(i);

                    _slots[i].InitBattle(this, i, type, currentHp, maxHp, isActive, canSelect);
                }
                else
                {
                    _slots[i].InitEmpty(this, i, _rosterOpen);
                }

                _slots[i].SetFocused(_rosterOpen
                    ? i == _selectedBattleIndex
                    : i == _previewBattleIndex);
            }

            RefreshRoster(player);
            UpdateSelectedSlotState();
        }

        private void RefreshRoster(PlayerActor player)
        {
            var partyManager = UISvc.Party;
            if (partyManager == null)
            {
                SetRosterSlotCount(0);
                return;
            }

            if (_rosterPanel != null)
            {
                _rosterPanel.SetActive(_rosterOpen);
            }

            if (!_rosterOpen || _rosterSlotPrefab == null || _rosterRoot == null)
            {
                SetRosterSlotCount(0);
                UpdateRosterHeader(partyManager.Roster.Count, partyManager.BattleOrder.Count);
                return;
            }

            IReadOnlyList<CharacterActorType> roster = partyManager.Roster;
            IReadOnlyList<CharacterActorType> battleOrder = partyManager.BattleOrder;

            SetRosterSlotCount(roster.Count);
            UpdateRosterHeader(roster.Count, battleOrder.Count);

            for (int i = 0; i < roster.Count; ++i)
            {
                CharacterActorType type = roster[i];
                float maxHp = player.GetMaxHealthForCharacter(type);
                float currentHp = player.HasHealthRecordForCharacter(type)
                    ? player.GetHealthForCharacter(type)
                    : maxHp;
                bool inBattle = battleOrder.Contains(type);
                bool canSelect = !inBattle && (battleOrder.Count < partyManager.MaxBattleSize || _selectedBattleIndex >= 0);

                _rosterSlots[i].InitRoster(this, i, type, currentHp, maxHp, inBattle, canSelect);
            }
        }

        public void PreviewMember(int index)
        {
            var partyManager = UISvc.Party;
            IReadOnlyList<CharacterActorType> battleOrder = partyManager?.BattleOrder;

            if (battleOrder == null || index < 0 || index >= battleOrder.Count)
            {
                return;
            }

            _previewBattleIndex = index;

            CharacterActorType type = battleOrder[index];
            ShowPreviewFor(type);

            if (!_rosterOpen)
            {
                for (int i = 0; i < _slots.Count; ++i)
                {
                    _slots[i].SetFocused(i == _previewBattleIndex);
                }
            }
        }

        public void PreviewCandidate(int candidateIndex)
        {
            var partyManager = UISvc.Party;
            if (partyManager == null) return;

            var roster = partyManager.Roster;
            if (candidateIndex < 0 || candidateIndex >= roster.Count) return;

            ShowPreviewFor(roster[candidateIndex]);
        }

        /// <summary>
        /// 출전 슬롯 클릭. 모드에 따라 분기.
        /// </summary>
        public void OnBattleSlotClicked(int slotIndex)
        {
            if (!_rosterOpen)
            {
                // 스왑 모드: 즉시 교체
                SelectMember(slotIndex);
                return;
            }

            // 편성 모드: 슬롯 선택 토글 (다시 클릭하면 해제)
            _selectedBattleIndex = (_selectedBattleIndex == slotIndex) ? -1 : slotIndex;
            PreviewMember(slotIndex);
            Refresh();
        }

        /// <summary>
        /// 후보 슬롯 클릭. 편성 모드 전용.
        /// 선택된 출전 슬롯이 있으면 그 자리와 교체, 없으면 빈 슬롯에 추가.
        /// </summary>
        public void OnCandidateClicked(int candidateIndex)
        {
            if (!_rosterOpen) return;

            var partyManager = UISvc.Party;
            if (partyManager == null) return;

            var roster = partyManager.Roster;
            if (candidateIndex < 0 || candidateIndex >= roster.Count) return;

            CharacterActorType type = roster[candidateIndex];
            if (partyManager.BattleOrder.Contains(type))
            {
                ShowPreviewFor(type);
                return;
            }

            if (_selectedBattleIndex >= 0 && _selectedBattleIndex < partyManager.BattleOrder.Count)
            {
                partyManager.ReplaceBattleSlot(_selectedBattleIndex, type);
                _selectedBattleIndex = -1;
            }
            else
            {
                partyManager.AddToBattle(type);
            }
            // OnBattleOrderChanged 이벤트로 Refresh 됨
        }

        public void SelectMember(int index)
        {
            var partyManager = UISvc.Party;
            if (partyManager == null) return;

            if (partyManager.RequestSwapTo(index))
            {
                _previewBattleIndex = partyManager.ActiveIndex;

                if (_hideAfterSelect)
                {
                    Hide();
                }
                else
                {
                    Refresh();
                }
            }
            else
            {
                Refresh();
            }
        }

        public void ToggleRosterDrawer()
        {
            SetRosterDrawer(!_rosterOpen);
        }

        public void CloseRosterDrawer()
        {
            SetRosterDrawer(false);
        }

        public void RemoveSelectedBattleSlot()
        {
            var partyManager = UISvc.Party;
            if (partyManager == null) return;

            IReadOnlyList<CharacterActorType> battleOrder = partyManager.BattleOrder;
            if (_selectedBattleIndex < 0 || _selectedBattleIndex >= battleOrder.Count) return;

            CharacterActorType type = battleOrder[_selectedBattleIndex];
            if (partyManager.RemoveFromBattle(type))
            {
                _selectedBattleIndex = -1;
            }
            else
            {
                Refresh();
            }
        }

        private void SetRosterDrawer(bool open)
        {
            _rosterOpen = open;
            if (!open)
            {
                _selectedBattleIndex = -1;
            }

            if (_formationToggle != null && _formationToggle.isOn != open)
            {
                _formationToggle.SetIsOnWithoutNotify(open);
            }

            if (_rosterPanel != null)
            {
                _rosterPanel.SetActive(open);
            }

            Refresh();
        }

        private void ShowPreviewFor(CharacterActorType type)
        {
            var partyManager = UISvc.Party;
            PlayerActor player = partyManager?.ActiveCharacter;
            if (player == null) return;

            float maxHp = player.GetMaxHealthForCharacter(type);
            float currentHp = player.HasHealthRecordForCharacter(type)
                ? player.GetHealthForCharacter(type)
                : maxHp;
            SetCurrentInfo(type, currentHp, maxHp);

            if (_previewRenderer != null)
            {
                _previewRenderer.ShowPreview(type);
            }
        }

        private void SetCurrentInfo(CharacterActorType characterType, float currentHp, float maxHp)
        {
            if (_currentNameText != null)
            {
                _currentNameText.text = characterType == CharacterActorType.None ? string.Empty : characterType.ToString();
            }

            if (_currentHpText != null)
            {
                _currentHpText.text = maxHp > 0f
                    ? $"{Mathf.CeilToInt(currentHp)}/{Mathf.CeilToInt(maxHp)}"
                    : string.Empty;
            }

            if (_currentHpFill != null)
            {
                _currentHpFill.fillAmount = maxHp > 0f ? Mathf.Clamp01(currentHp / maxHp) : 0f;
            }
        }

        private void UpdateBattleSizeText(int battleCount, int maxBattle)
        {
            if (_battleSizeText != null)
            {
                _battleSizeText.text = $"{battleCount} / {maxBattle}";
            }
        }

        private void UpdateRosterHeader(int rosterCount, int battleCount)
        {
            if (_rosterToggleText != null)
            {
                _rosterToggleText.text = _rosterOpen ? "편성 완료" : "일괄 편성";
            }

            if (_rosterCountText != null)
            {
                _rosterCountText.text = $"ROSTER - {rosterCount} 캐릭터";
            }
        }

        private void UpdateSelectedSlotState()
        {
            bool hasSelection = _rosterOpen && _selectedBattleIndex >= 0;
            if (_selectedSlotText != null)
            {
                _selectedSlotText.text = hasSelection ? $"슬롯 {_selectedBattleIndex + 1} 선택 중" : string.Empty;
            }

            if (_removeSlotButton != null)
            {
                var battleOrder = UISvc.Party?.BattleOrder;
                bool canRemove = hasSelection && battleOrder != null && _selectedBattleIndex < battleOrder.Count;
                _removeSlotButton.gameObject.SetActive(canRemove);
            }
        }

        private void SetSlotCount(int count)
        {
            if (_slotPrefab == null || _slotRoot == null) return;

            while (_slots.Count < count)
            {
                UI_PartyMemberSlot slot = Instantiate(_slotPrefab, _slotRoot);
                _slots.Add(slot);
            }

            for (int i = 0; i < _slots.Count; ++i)
            {
                _slots[i].gameObject.SetActive(i < count);
            }
        }

        private void SetRosterSlotCount(int count)
        {
            Transform root = _rosterRoot;
            UI_PartyMemberSlot prefab = _rosterSlotPrefab != null ? _rosterSlotPrefab : _slotPrefab;
            if (prefab == null || root == null) return;

            while (_rosterSlots.Count < count)
            {
                UI_PartyMemberSlot slot = Instantiate(prefab, root);
                _rosterSlots.Add(slot);
            }

            for (int i = 0; i < _rosterSlots.Count; ++i)
            {
                _rosterSlots[i].gameObject.SetActive(i < count);
            }
        }

        private void BindFieldPreviewTextures()
        {
            if (_fieldPreviews == null || _fieldPreviewRenderers == null) return;

            int count = Mathf.Min(_fieldPreviews.Length, _fieldPreviewRenderers.Length);
            for (int i = 0; i < count; ++i)
            {
                if (_fieldPreviews[i] != null && _fieldPreviewRenderers[i] != null)
                {
                    _fieldPreviews[i].texture = _fieldPreviewRenderers[i].GetRenderTexture();
                }
            }
        }

        private void UpdateFieldPreviews(IReadOnlyList<CharacterActorType> battleOrder)
        {
            if (_fieldPreviewRenderers == null || _fieldPreviewRenderers.Length == 0) return;

            for (int i = 0; i < _fieldPreviewRenderers.Length; ++i)
            {
                if (_fieldPreviewRenderers[i] == null) continue;

                if (battleOrder != null && i < battleOrder.Count)
                {
                    _fieldPreviewRenderers[i].ShowPreview(battleOrder[i]);
                    if (_fieldPreviews != null && i < _fieldPreviews.Length && _fieldPreviews[i] != null)
                    {
                        _fieldPreviews[i].gameObject.SetActive(true);
                    }
                }
                else
                {
                    _fieldPreviewRenderers[i].HidePreview();
                    if (_fieldPreviews != null && i < _fieldPreviews.Length && _fieldPreviews[i] != null)
                    {
                        _fieldPreviews[i].gameObject.SetActive(false);
                    }
                }
            }
        }

        private void HideFieldPreviews()
        {
            if (_fieldPreviewRenderers == null) return;

            for (int i = 0; i < _fieldPreviewRenderers.Length; ++i)
            {
                if (_fieldPreviewRenderers[i] != null)
                {
                    _fieldPreviewRenderers[i].HidePreview();
                }

                if (_fieldPreviews != null && i < _fieldPreviews.Length && _fieldPreviews[i] != null)
                {
                    _fieldPreviews[i].gameObject.SetActive(false);
                }
            }
        }

        private void OnSwapCompleted(PlayerActor player)         => Refresh();
        private void OnCharacterUnlocked(CharacterActorType t)   => Refresh();
        private void OnRosterChanged()                           => Refresh();
        private void OnBattleOrderChanged()                      => Refresh();
    }
}
