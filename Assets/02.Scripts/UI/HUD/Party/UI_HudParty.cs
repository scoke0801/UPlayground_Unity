using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Manager;

/// <summary>
/// HUD 파티 정보 UI — BattleOrder에 배치된 슬롯만 노출하고, 변경 시 자동 갱신.
/// </summary>
public class UI_HudParty : UI_Base
{
    [SerializeField] private List<UIHudPartyEntry> _entries = new();

    protected override void OnShow()
    {
        base.OnShow();

        if (PartyManager.Instance != null)
            PartyManager.Instance.OnBattleOrderChanged += Refresh;

        Refresh();
    }

    protected override void OnHide()
    {
        if (PartyManager.Instance != null)
            PartyManager.Instance.OnBattleOrderChanged -= Refresh;

        foreach (var entry in _entries)
            entry.Unbind();
    }

    protected override void OnDispose()
    {
        if (PartyManager.Instance != null)
            PartyManager.Instance.OnBattleOrderChanged -= Refresh;
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
    }
}
