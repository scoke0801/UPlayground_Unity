using System.Collections.Generic;
using UnityEngine;
using UPlayGround;
using UPlayGround.Manager;

/// <summary>
/// HUD 파티 정보 UI — BattleOrder에 배치된 슬롯만 노출하고, 변경 시 자동 갱신.
/// </summary>
public class UI_HudParty : UI_Base
{
    [SerializeField] private List<UIHudPartyEntry> _entries = new();

    private PlayerActor _subscribedPlayer;

    protected override void OnShow()
    {
        base.OnShow();

        if (PartyManager.Instance != null)
        {
            PartyManager.Instance.OnBattleOrderChanged += Refresh;
            PartyManager.Instance.OnSwapCompleted      += OnSwapCompleted;
            PartyManager.Instance.OnPartySkillGaugeChanged += OnPartySkillGaugeChanged;
        }

        Refresh();
    }

    protected override void OnHide()
    {
        UnsubscribePlayer();

        if (PartyManager.Instance != null)
        {
            PartyManager.Instance.OnBattleOrderChanged -= Refresh;
            PartyManager.Instance.OnSwapCompleted      -= OnSwapCompleted;
            PartyManager.Instance.OnPartySkillGaugeChanged -= OnPartySkillGaugeChanged;
        }

        foreach (var entry in _entries)
            entry.Unbind();
    }

    protected override void OnDispose()
    {
        UnsubscribePlayer();

        if (PartyManager.Instance != null)
        {
            PartyManager.Instance.OnBattleOrderChanged -= Refresh;
            PartyManager.Instance.OnSwapCompleted      -= OnSwapCompleted;
            PartyManager.Instance.OnPartySkillGaugeChanged -= OnPartySkillGaugeChanged;
        }
    }

    private void Refresh()
    {
        var pm = PartyManager.Instance;
        if (pm == null) return;

        var battleOrder = pm.BattleOrder;
        var memberData  = pm.PartyMemberDataSO;

        for (int i = 0; i < _entries.Count; i++)
        {
            if (i < battleOrder.Count)
                _entries[i].Bind(battleOrder[i], memberData);
            else
                _entries[i].Unbind();
        }

        SubscribePlayer(pm.ActiveCharacter);
        RefreshEntryValues();
    }

    private void SubscribePlayer(PlayerActor player)
    {
        UnsubscribePlayer();
        if (player == null) return;
        _subscribedPlayer                    = player;
        player.OnHpChanged                  += OnActiveHpChanged;
        player.OnSkillGaugeChanged          += OnActiveSkillGaugeChanged;
    }

    private void UnsubscribePlayer()
    {
        if (_subscribedPlayer == null) return;
        _subscribedPlayer.OnHpChanged         -= OnActiveHpChanged;
        _subscribedPlayer.OnSkillGaugeChanged -= OnActiveSkillGaugeChanged;
        _subscribedPlayer = null;
    }

    private void OnSwapCompleted(PlayerActor player)
    {
        RefreshEntryValues();
    }

    private void OnActiveHpChanged(float current, float max)
    {
        var activeType = PartyManager.Instance?.ActiveCharacterType
                         ?? UPlayGround.Data.EnumType.CharacterActorType.None;
        foreach (var entry in _entries)
        {
            if (entry != null && entry.BoundType == activeType)
            {
                entry.SetHealth(current, max);
                break;
            }
        }
    }

    private void OnActiveSkillGaugeChanged(float current, float max)
    {
        var activeType = PartyManager.Instance?.ActiveCharacterType
                         ?? UPlayGround.Data.EnumType.CharacterActorType.None;
        foreach (var entry in _entries)
        {
            if (entry != null && entry.BoundType == activeType)
            {
                entry.SetSkillGauge(current, max);
                break;
            }
        }
    }

    private void OnPartySkillGaugeChanged(UPlayGround.Data.EnumType.CharacterActorType type, float current, float max)
    {
        foreach (var entry in _entries)
        {
            if (entry != null && entry.BoundType == type)
            {
                entry.SetSkillGauge(current, max);
                break;
            }
        }
    }

    private void RefreshEntryValues()
    {
        var pm = PartyManager.Instance;
        var player = pm?.ActiveCharacter;
        if (pm == null || player == null) return;

        for (int i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];
            if (entry == null || entry.BoundType == UPlayGround.Data.EnumType.CharacterActorType.None)
                continue;

            var type = entry.BoundType;
            entry.SetHealth(
                player.GetHealthForCharacter(type),
                player.GetMaxHealthForCharacter(type));

            entry.SetSkillGauge(
                player.GetSkillGaugeForCharacter(type),
                player.GetMaxSkillGaugeForCharacter(type));
        }
    }
}
