using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround;
using UPlayGround.Data.EnumType;
using UPlayGround.InputDefine;
using UPlayGround.Manager;

namespace UPlayGround.UI
{
    /// <summary>
    /// 파티원 선택 / 편성 화면.
    /// 클릭은 _pendingOrder(초안)만 수정하며, 저장 버튼으로 PartyManager에 반영한다.
    /// </summary>
    public class UI_PartyMenu : UI_SceneBase
    {
        // 매니저 참조 캐싱 — 반복 Instance 조회(락 경합) 방지, 파괴 시 fake-null로 재조회
        private IUIPartyService _cachedPartyManager;
        private IUIPartyService PartyMgr => _cachedPartyManager != null ? _cachedPartyManager : (_cachedPartyManager = UISvc.Party);
        private IUIInventoryService _cachedInventoryManager;
        private IUIInventoryService InventoryMgr => _cachedInventoryManager != null ? _cachedInventoryManager : (_cachedInventoryManager = UISvc.Inventory);


        [Header("캐릭터 목록")]
        [SerializeField] private Transform        _content;
        [SerializeField] private UIPartyMenuEntry _partyMenuEntryPrefab;

        [Header("전투원 구성")]
        [SerializeField] private List<UIPartyBattleEntry> _partyBattleEntries;

        [Header("버튼")]
        [SerializeField] private Button _saveButton;
        [SerializeField] private Button _autoOrganizationButton;
        [SerializeField] private Button _disbandBattleButton;   // 출전 해제 (선택 캐릭터 제외)
        [SerializeField] private Button _disbandPartyButton;     // 파티 해제 (리더만 남김)
        [SerializeField] private Button _closeButton;            // 닫기 (저장 안 함)

        [Header("텍스트")]
        [SerializeField] private TextMeshProUGUI _partyCombatPowerText;
        [SerializeField] private TextMeshProUGUI _rosterCountText;      // 보유 동료 수
        [SerializeField] private TextMeshProUGUI _battlePartyCountText; // 출전 파티 N / 최대 (중앙 헤더)
        [SerializeField] private TextMeshProUGUI _battleMemberCountText;// 출전 인원 N / 최대 (하단)
        [SerializeField] private TextMeshProUGUI _partyWeightSummaryText; // 파티 무게 구성 (예: 경량1·표준1·중량1)

        [Header("상세")]
        [SerializeField] private UI_PartyDetailPanel _detailPanel;

        [Header("어시스트 (사이클 보스 영입 동료)")]
        [SerializeField] private MonoBehaviour _assistPanel;

        [Header("옵션")]
        [Tooltip("편성 화면을 여는 동안 게임을 일시정지한다. 사이클 런 타이머도 함께 멈춘다.")]
        [SerializeField] private bool _pauseGameOnShow = true;

        private readonly List<UIPartyMenuEntry> _menuEntries  = new();
        private readonly List<CharacterActorType> _pendingOrder = new();

        private CharacterActorType _selectedType = CharacterActorType.None;
        private bool _didPauseGame;

        // ─── 생명주기 ─────────────────────────────────────────────────────

        protected override void OnInit()
        {
            base.OnInit();

            foreach (var battleEntry in _partyBattleEntries)
                battleEntry.OnSelectRequested += OnBattleEntrySelected;

            _saveButton?.onClick.AddListener(OnSaveClicked);
            _autoOrganizationButton?.onClick.AddListener(OnAutoOrganizationClicked);
            _disbandBattleButton?.onClick.AddListener(OnDisbandBattleClicked);
            _disbandPartyButton?.onClick.AddListener(OnDisbandPartyClicked);
            _closeButton?.onClick.AddListener(Hide);
        }

        // 입력 레이어 상승/복원은 UI_Base가 BlocksLowerInput 기준으로 일괄 처리한다.
        protected override bool BlocksLowerInput => true;

        protected override void OnShow()
        {
            base.OnShow();

            // 이미 다른 UI가 일시정지한 상태면 우리가 재개 책임을 지지 않는다(단순 bool 모델이라 이중 해제 방지).
            var timeMgr = Svc.GameTime;
            if (_pauseGameOnShow && timeMgr != null && !timeMgr.IsPaused)
            {
                timeMgr.SetPause(true);
                _didPauseGame = true;
            }

            if (PartyMgr != null)
            {
                PartyMgr.OnSwapCompleted += OnSwapCompleted;
                PartyMgr.OnPartyProgressionChanged += OnPartyProgressionChanged;
                PartyMgr.OnRosterChanged += OnRosterChanged;
            }
            if (InventoryMgr != null)
                InventoryMgr.OnPartyEquipmentChanged += OnPartyEquipmentChanged;

            // 현재 BattleOrder를 초안으로 복사
            _pendingOrder.Clear();
            if (PartyMgr != null)
                _pendingOrder.AddRange(PartyMgr.BattleOrder);

            // 기본 선택: 첫 출전 멤버
            _selectedType = _pendingOrder.Count > 0 ? _pendingOrder[0] : CharacterActorType.None;

            RebuildMenuEntries();
            Refresh();
        }

        protected override void OnHide()
        {
            base.OnHide();

            if (PartyMgr != null)
            {
                PartyMgr.OnSwapCompleted -= OnSwapCompleted;
                PartyMgr.OnPartyProgressionChanged -= OnPartyProgressionChanged;
                PartyMgr.OnRosterChanged -= OnRosterChanged;
            }
            if (InventoryMgr != null)
                InventoryMgr.OnPartyEquipmentChanged -= OnPartyEquipmentChanged;

            if (_didPauseGame)
            {
                Svc.GameTime?.SetPause(false);
                _didPauseGame = false;
            }
        }

        protected override void OnDispose()
        {
            base.OnDispose();

            if (PartyMgr != null)
            {
                PartyMgr.OnSwapCompleted -= OnSwapCompleted;
                PartyMgr.OnPartyProgressionChanged -= OnPartyProgressionChanged;
                PartyMgr.OnRosterChanged -= OnRosterChanged;
            }
            if (InventoryMgr != null)
                InventoryMgr.OnPartyEquipmentChanged -= OnPartyEquipmentChanged;

            foreach (var entry in _menuEntries)
                entry.OnToggleRequested -= OnEntryToggleRequested;

            foreach (var battleEntry in _partyBattleEntries)
                battleEntry.OnSelectRequested -= OnBattleEntrySelected;
        }

        public override bool PerformBackFunction()
        {
            Hide(); // 저장하지 않고 닫기
            return false;
        }

        // ─── 버튼 핸들러 ─────────────────────────────────────────────────

        private void OnSaveClicked()
        {
            PartyMgr?.SetBattleOrder(_pendingOrder);
            Hide();
        }

        private void OnAutoOrganizationClicked()
        {
            var pm = PartyMgr;
            if (pm == null) return;

            _pendingOrder.Clear();
            foreach (var type in pm.Roster)
            {
                if (_pendingOrder.Count >= pm.MaxBattleSize) break;
                _pendingOrder.Add(type);
            }

            Refresh();
        }

        // ─── 엔트리 이벤트 ───────────────────────────────────────────────

        // 목록 클릭: 상세 선택 + (미편성이고 자리 있으면) 출전 파티에 추가. 제거는 하단 버튼으로.
        private void OnEntryToggleRequested(CharacterActorType type)
        {
            _selectedType = type;

            if (!_pendingOrder.Contains(type) &&
                _pendingOrder.Count < (PartyMgr?.MaxBattleSize ?? 4))
            {
                _pendingOrder.Add(type);
            }

            Refresh();
        }

        // 출전 슬롯 클릭: 상세 선택만.
        private void OnBattleEntrySelected(CharacterActorType type)
        {
            _selectedType = type;
            Refresh();
        }

        // 출전 해제: 선택 캐릭터를 편성에서 제외 (최소 1명 유지).
        private void OnDisbandBattleClicked()
        {
            if (_selectedType == CharacterActorType.None) return;
            if (!_pendingOrder.Contains(_selectedType)) return;
            if (_pendingOrder.Count <= 1) return;

            _pendingOrder.Remove(_selectedType);
            Refresh();
        }

        // 파티 해제: 리더(첫 슬롯)만 남기고 전원 제외.
        private void OnDisbandPartyClicked()
        {
            if (_pendingOrder.Count <= 1) return;

            var leader = _pendingOrder[0];
            _pendingOrder.Clear();
            _pendingOrder.Add(leader);
            Refresh();
        }

        private void OnSwapCompleted(PlayerActor _) => RefreshBattleEntries();
        private void OnPartyProgressionChanged(CharacterActorType _) => Refresh();
        private void OnPartyEquipmentChanged() => Refresh();

        private void OnRosterChanged()
        {
            RebuildMenuEntries();
            Refresh();
        }

        // ─── 갱신 ────────────────────────────────────────────────────────

        private void Refresh()
        {
            RefreshBattleEntries();
            RefreshMenuEntries();
            RefreshPartyCombatPower();
            RefreshCounts();
            RefreshWeightSummary();
            RefreshDetail();
            (_assistPanel as IUIRefreshable)?.Refresh();
        }

        /// <summary>
        /// 보유(Roster) 캐릭터만으로 목록을 재구성한다.
        /// 사이클 전환 후 영입은 BossAssist 전용이라 미보유 캐릭터는 게임 내 획득 수단이 없으므로 노출하지 않는다.
        /// </summary>
        private void RebuildMenuEntries()
        {
            var pm = PartyMgr;
            if (pm == null || _partyMenuEntryPrefab == null || _content == null) return;

            foreach (var entry in _menuEntries)
            {
                if (entry == null) continue;
                entry.OnToggleRequested -= OnEntryToggleRequested;
                Destroy(entry.gameObject);
            }
            _menuEntries.Clear();

            foreach (var type in pm.Roster)
            {
                var entry = Instantiate(_partyMenuEntryPrefab, _content);
                if (entry == null) continue;

                entry.Init(type);
                entry.OnToggleRequested += OnEntryToggleRequested;
                _menuEntries.Add(entry);
            }
        }

        private void RefreshCounts()
        {
            var pm = PartyMgr;
            if (pm == null) return;

            int max = pm.MaxBattleSize;
            if (_rosterCountText != null)
                _rosterCountText.text = pm.Roster.Count.ToString("N0", CultureInfo.InvariantCulture);
            if (_battlePartyCountText != null)
                _battlePartyCountText.text = $"{_pendingOrder.Count} / {max}";
            if (_battleMemberCountText != null)
                _battleMemberCountText.text = $"{_pendingOrder.Count} / {max}";
        }

        private void RefreshDetail()
        {
            if (_detailPanel == null) return;

            if (_selectedType == CharacterActorType.None)
                _detailPanel.Clear();
            else
                _detailPanel.Show(_selectedType);
        }

        private void RefreshBattleEntries()
        {
            var memberData = PartyMgr?.PartyMemberDataSO;
            bool canRemove = _pendingOrder.Count > 1;

            for (int i = 0; i < _partyBattleEntries.Count; i++)
            {
                if (i < _pendingOrder.Count)
                    _partyBattleEntries[i].Bind(_pendingOrder[i], memberData, i, canRemove);
                else
                    _partyBattleEntries[i].Unbind();
            }
        }

        private void RefreshMenuEntries()
        {
            foreach (var entry in _menuEntries)
                entry.RefreshBattleStatus(_pendingOrder, _selectedType);
        }

        private void RefreshPartyCombatPower()
        {
            if (_partyCombatPowerText == null) return;

            long combatPower = PartyMgr?.GetPartyCombatPower(_pendingOrder) ?? 0L;
            _partyCombatPowerText.text = combatPower.ToString("#,0", CultureInfo.InvariantCulture);
        }

        /// <summary>초안 출전 파티의 무게 클래스 구성 요약 (사이클 03 스펙 파생).</summary>
        private void RefreshWeightSummary()
        {
            if (_partyWeightSummaryText == null) return;

            int light = 0, standard = 0, heavy = 0, unknown = 0;
            foreach (var type in _pendingOrder)
            {
                var profile = UIPartyWeightUtil.FindProfile(type);
                if (profile == null) { unknown++; continue; }
                switch (profile.weightClass)
                {
                    case Data.Cycle.CharacterWeightClass.Light:  light++;    break;
                    case Data.Cycle.CharacterWeightClass.Heavy:  heavy++;    break;
                    default:                                     standard++; break;
                }
            }

            string text = $"경량 {light} / 표준 {standard} / 중량 {heavy}";
            if (unknown > 0) text += $" / 미분류 {unknown}";
            _partyWeightSummaryText.text = text;
        }
    }
}
