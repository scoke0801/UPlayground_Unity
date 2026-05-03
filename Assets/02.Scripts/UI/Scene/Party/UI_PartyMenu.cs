using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround;
using UPlayGround.Data.EnumType;
using UPlayGround.InputDefine;
using UPlayGround.Manager;

/// <summary>
/// 파티원 선택 / 편성 화면.
/// </summary>
public class UI_PartyMenu : UI_Base
{
    [Header("캐릭터 목록")]
    [SerializeField] private Transform      _content;
    [SerializeField] private UIPartyMenuEntry _partyMenuEntryPrefab;

    [Header("전투원 구성")]
    [SerializeField] private List<UIPartyBattleEntry> _partyBattleEntries;

    private readonly List<UIPartyMenuEntry> _menuEntries = new();

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
            _menuEntries.Add(entry);
        }
    }

    protected override void OnShow()
    {
        base.OnShow();
        InputManager.Instance.SetInputLayer(_layer.ToInputLayer());

        if (PartyManager.Instance != null)
        {
            PartyManager.Instance.OnBattleOrderChanged += Refresh;
            PartyManager.Instance.OnSwapCompleted      += OnSwapCompleted;
        }

        Refresh();
    }

    protected override void OnHide()
    {
        if (PartyManager.Instance != null)
        {
            PartyManager.Instance.OnBattleOrderChanged -= Refresh;
            PartyManager.Instance.OnSwapCompleted      -= OnSwapCompleted;
        }

        InputManager.Instance.SetInputLayer(InputLayer.None);
    }

    protected override void OnDispose()
    {
        if (PartyManager.Instance != null)
        {
            PartyManager.Instance.OnBattleOrderChanged -= Refresh;
            PartyManager.Instance.OnSwapCompleted      -= OnSwapCompleted;
        }
    }

    public override bool PerformBackFunction()
    {
        Hide();
        return false;
    }

    // ─── 갱신 ────────────────────────────────────────────────────────

    private void OnSwapCompleted(PlayerActor _) => RefreshBattleEntries();

    private void Refresh()
    {
        RefreshBattleEntries();
        RefreshMenuEntries();
    }

    private void RefreshBattleEntries()
    {
        var pm = PartyManager.Instance;
        if (pm == null) return;

        var battleOrder = pm.BattleOrder;
        var memberData  = pm.PartyMemberDataSO;

        for (int i = 0; i < _partyBattleEntries.Count; i++)
        {
            if (i < battleOrder.Count)
                _partyBattleEntries[i].Bind(battleOrder[i], memberData, i);
            else
                _partyBattleEntries[i].Unbind();
        }
    }

    private void RefreshMenuEntries()
    {
        foreach (var entry in _menuEntries)
            entry.RefreshBattleStatus();
    }
}
