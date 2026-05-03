using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround;
using UPlayGround.Data.EnumType;
using UPlayGround.InputDefine;
using UPlayGround.Manager;

/// <summary>
/// 파티원 선택 / 편성 화면.
/// 클릭은 _pendingOrder(초안)만 수정하며, 저장 버튼으로 PartyManager에 반영한다.
/// </summary>
public class UI_PartyMenu : UI_Base
{
    [Header("캐릭터 목록")]
    [SerializeField] private Transform        _content;
    [SerializeField] private UIPartyMenuEntry _partyMenuEntryPrefab;

    [Header("전투원 구성")]
    [SerializeField] private List<UIPartyBattleEntry> _partyBattleEntries;

    [Header("버튼")]
    [SerializeField] private Button _saveButton;
    [SerializeField] private Button _autoOrganizationButton;

    [Header("텍스트")]
    [SerializeField] private TextMeshProUGUI _partyCombatPowerText;
    
    private readonly List<UIPartyMenuEntry> _menuEntries  = new();
    private readonly List<CharacterActorType> _pendingOrder = new();

    // ─── 생명주기 ─────────────────────────────────────────────────────

    protected override void OnInit()
    {
        base.OnInit();

        foreach (CharacterActorType type in Enum.GetValues(typeof(CharacterActorType)))
        {
            if (type == CharacterActorType.None || type == CharacterActorType.H09)
                continue;

            var entry = Instantiate(_partyMenuEntryPrefab, _content);
            if (entry == null) continue;

            entry.Init(type);
            entry.OnToggleRequested += OnEntryToggleRequested;
            _menuEntries.Add(entry);
        }

        foreach (var battleEntry in _partyBattleEntries)
            battleEntry.OnRemoveRequested += OnBattleEntryRemoveRequested;

        _saveButton?.onClick.AddListener(OnSaveClicked);
        _autoOrganizationButton?.onClick.AddListener(OnAutoOrganizationClicked);
    }

    protected override void OnShow()
    {
        base.OnShow();
        InputManager.Instance.SetInputLayer(_layer.ToInputLayer());

        if (PartyManager.Instance != null)
        {
            PartyManager.Instance.OnSwapCompleted += OnSwapCompleted;
            PartyManager.Instance.OnPartyProgressionChanged += OnPartyProgressionChanged;
        }

        // 현재 BattleOrder를 초안으로 복사
        _pendingOrder.Clear();
        if (PartyManager.Instance != null)
            _pendingOrder.AddRange(PartyManager.Instance.BattleOrder);

        SortMenuEntries();
        Refresh();
    }

    protected override void OnHide()
    {
        if (PartyManager.Instance != null)
        {
            PartyManager.Instance.OnSwapCompleted -= OnSwapCompleted;
            PartyManager.Instance.OnPartyProgressionChanged -= OnPartyProgressionChanged;
        }

        InputManager.Instance.SetInputLayer(InputLayer.None);
    }

    protected override void OnDispose()
    {
        if (PartyManager.Instance != null)
        {
            PartyManager.Instance.OnSwapCompleted -= OnSwapCompleted;
            PartyManager.Instance.OnPartyProgressionChanged -= OnPartyProgressionChanged;
        }

        foreach (var entry in _menuEntries)
            entry.OnToggleRequested -= OnEntryToggleRequested;

        foreach (var battleEntry in _partyBattleEntries)
            battleEntry.OnRemoveRequested -= OnBattleEntryRemoveRequested;
    }

    public override bool PerformBackFunction()
    {
        Hide(); // 저장하지 않고 닫기
        return false;
    }

    // ─── 버튼 핸들러 ─────────────────────────────────────────────────

    private void OnSaveClicked()
    {
        PartyManager.Instance?.SetBattleOrder(_pendingOrder);
        Hide();
    }

    private void OnAutoOrganizationClicked()
    {
        var pm = PartyManager.Instance;
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

    private void OnEntryToggleRequested(CharacterActorType type)
    {
        if (_pendingOrder.Contains(type))
            _pendingOrder.Remove(type);
        else if (_pendingOrder.Count < (PartyManager.Instance?.MaxBattleSize ?? 4))
            _pendingOrder.Add(type);

        Refresh();
    }

    private void OnBattleEntryRemoveRequested(CharacterActorType type)
    {
        if (_pendingOrder.Count <= 1) return; // 마지막 슬롯은 해제 불가
        _pendingOrder.Remove(type);
        Refresh();
    }

    private void OnSwapCompleted(PlayerActor _) => RefreshBattleEntries();
    private void OnPartyProgressionChanged(CharacterActorType _) => Refresh();

    // ─── 갱신 ────────────────────────────────────────────────────────

    private void Refresh()
    {
        RefreshBattleEntries();
        RefreshMenuEntries();
        RefreshPartyCombatPower();
    }

    private void RefreshBattleEntries()
    {
        var memberData = PartyManager.Instance?.PartyMemberDataSO;
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
            entry.RefreshBattleStatus(_pendingOrder);
    }

    private void RefreshPartyCombatPower()
    {
        if (_partyCombatPowerText == null) return;

        long combatPower = PartyManager.Instance?.GetPartyCombatPower(_pendingOrder) ?? 0L;
        _partyCombatPowerText.text = combatPower.ToString("#,0", CultureInfo.InvariantCulture);
    }

    /// <summary>보유(Roster) 캐릭터가 목록 상단에 오도록 sibling 순서 재정렬.</summary>
    private void SortMenuEntries()
    {
        var pm = PartyManager.Instance;
        if (pm == null) return;

        int idx = 0;
        foreach (var entry in _menuEntries.OrderByDescending(e => pm.Roster.Contains(e.Type)))
            entry.transform.SetSiblingIndex(idx++);
    }
}
