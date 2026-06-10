using System.Collections.Generic;
using UnityEngine;
using UPlayGround;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;

/// <summary>
/// HUD 파티 정보 UI — BattleOrder에 배치된 슬롯만 노출하고, 변경 시 자동 갱신.
/// </summary>
public class UI_HudParty : UI_Base
{
    [SerializeField] private List<UIHudPartyEntry> _entries = new();

    private const int UltimateSkillSlot = (int)UPlayGround.Data.Combat.PlayerSkillSlot.Ultimate;
    private PlayerActor _subscribedPlayer;
    private bool _isSubscribedToPartyEvents;
    private bool _hasSwapCooldownVisible;

    protected override void OnShow()
    {
        base.OnShow();

        SubscribePartyEvents();
        Refresh();
    }

    protected override void OnHide()
    {
        UnsubscribePlayer();
        UnsubscribePartyEvents();

        foreach (var entry in _entries)
            entry.Unbind();
    }

    protected override void OnDispose()
    {
        UnsubscribePlayer();
        UnsubscribePartyEvents();
    }

    private void SubscribePartyEvents()
    {
        if (_isSubscribedToPartyEvents || PartyManager.Instance == null) return;

        PartyManager.Instance.OnBattleOrderChanged += Refresh;
        PartyManager.Instance.OnSwapCompleted += OnSwapCompleted;
        PartyManager.Instance.OnPartySkillGaugeChanged += OnPartySkillGaugeChanged;
        PartyManager.Instance.OnSwapCooldownChanged += OnSwapCooldownChanged;
        PartyManager.Instance.OnPartyHealthRefreshed += RefreshEntryValues;
        _isSubscribedToPartyEvents = true;
    }

    private void UnsubscribePartyEvents()
    {
        if (!_isSubscribedToPartyEvents || PartyManager.Instance == null)
        {
            _isSubscribedToPartyEvents = false;
            return;
        }

        PartyManager.Instance.OnBattleOrderChanged -= Refresh;
        PartyManager.Instance.OnSwapCompleted -= OnSwapCompleted;
        PartyManager.Instance.OnPartySkillGaugeChanged -= OnPartySkillGaugeChanged;
        PartyManager.Instance.OnSwapCooldownChanged -= OnSwapCooldownChanged;
        PartyManager.Instance.OnPartyHealthRefreshed -= RefreshEntryValues;
        _isSubscribedToPartyEvents = false;
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
        RefreshSwapCooldown();
    }

    protected override void Update()
    {
        base.Update();

        if (IsVisible && (_hasSwapCooldownVisible || PartyManager.Instance?.IsSwapOnCooldown == true))
            RefreshSwapCooldown();
    }

    private void SubscribePlayer(PlayerActor player)
    {
        UnsubscribePlayer();
        if (player == null) return;
        _subscribedPlayer                    = player;
        player.OnHpChanged                  += OnActiveHpChanged;
        player.OnSkillGaugeChanged          += OnActiveSkillGaugeChanged;
        if (player.SkillGauge != null)
            player.SkillGauge.OnCooldownChanged += OnActiveSkillCooldownChanged;
    }

    private void UnsubscribePlayer()
    {
        if (_subscribedPlayer == null) return;
        _subscribedPlayer.OnHpChanged         -= OnActiveHpChanged;
        _subscribedPlayer.OnSkillGaugeChanged -= OnActiveSkillGaugeChanged;
        if (_subscribedPlayer.SkillGauge != null)
            _subscribedPlayer.SkillGauge.OnCooldownChanged -= OnActiveSkillCooldownChanged;
        _subscribedPlayer = null;
    }

    private void OnSwapCompleted(PlayerActor player)
    {
        SubscribePlayer(player);
        RefreshEntryValues();
        RefreshSwapCooldown();
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
        RefreshUltimateReady(PartyManager.Instance?.ActiveCharacterType ?? CharacterActorType.None);
    }

    private void OnActiveSkillCooldownChanged(int skillSlot, float remaining, float duration)
    {
        if (skillSlot == UltimateSkillSlot)
            RefreshUltimateReady(PartyManager.Instance?.ActiveCharacterType ?? CharacterActorType.None);
    }

    private void RefreshUltimateReady(CharacterActorType type)
    {
        if (type == CharacterActorType.None) return;

        var player = PartyManager.Instance?.ActiveCharacter;
        foreach (var entry in _entries)
        {
            if (entry != null && entry.BoundType == type)
            {
                entry.SetUltimateReady(player != null && player.IsUltimateReadyForCharacter(type));
                break;
            }
        }
    }

    private void OnPartySkillGaugeChanged(CharacterActorType type, float current, float max)
    {
        foreach (var entry in _entries)
        {
            if (entry != null && entry.BoundType == type)
            {
                var player = PartyManager.Instance?.ActiveCharacter;
                entry.SetUltimateReady(player != null
                    ? player.IsUltimateReadyForCharacter(type)
                    : max > 0f && current >= max);
                break;
            }
        }
    }

    private void OnSwapCooldownChanged(CharacterActorType type, float remaining, float duration)
    {
        _hasSwapCooldownVisible = remaining > 0f || _hasSwapCooldownVisible;

        foreach (var entry in _entries)
        {
            if (entry != null && entry.BoundType == type)
            {
                entry.SetSwapCooldown(remaining, duration);
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
            entry.SetUltimateReady(player.IsUltimateReadyForCharacter(type));
        }

        RefreshSpawnedState();
        RefreshSwapCooldown();
    }

    private void RefreshSpawnedState()
    {
        var activeType = PartyManager.Instance?.ActiveCharacterType ?? CharacterActorType.None;
        for (int i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];
            if (entry == null) continue;
            entry.SetSpawned(entry.BoundType != CharacterActorType.None && entry.BoundType == activeType);
        }
    }

    private void RefreshSwapCooldown()
    {
        var pm = PartyManager.Instance;
        if (pm == null)
        {
            _hasSwapCooldownVisible = false;
            foreach (var entry in _entries)
                entry?.SetSwapCooldown(0f, 0f);
            return;
        }

        float duration = pm.SwapCooldownDuration;
        bool hasVisibleCooldown = false;

        for (int i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];
            if (entry == null || entry.BoundType == UPlayGround.Data.EnumType.CharacterActorType.None)
                continue;

            float remaining = pm.GetSwapCooldownRemaining(entry.BoundType);
            if (remaining > 0f) hasVisibleCooldown = true;
            entry.SetSwapCooldown(remaining, duration);
        }

        _hasSwapCooldownVisible = hasVisibleCooldown;
    }
}
