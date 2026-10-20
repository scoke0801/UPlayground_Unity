using System.Collections;
using UnityEngine;
using UPlayGround.Manager;

namespace UPlayGround.TriggerSystem
{
    /// <summary>TriggerComposer와 FlowGraph의 RunTriggerAction에서 보스 아레나 상태를 지정한다.</summary>
    [CreateAssetMenu(menuName = "UPlayGround/트리거/액션/Combat Arena")]
    public sealed class SetCombatArenaTriggerActionSO : TriggerActionSO
    {
        public string arenaId;
        public bool isActive = true;
        public override bool CanExecute(TriggerContext context) => !string.IsNullOrWhiteSpace(arenaId)
            && Services.TryGet<ICombatEncounterService>(out _);
        public override IEnumerator Execute(TriggerContext context)
        {
            if (Services.TryGet<ICombatEncounterService>(out var encounter)) encounter.SetArenaActive(arenaId, isActive);
            yield break;
        }
    }
}
