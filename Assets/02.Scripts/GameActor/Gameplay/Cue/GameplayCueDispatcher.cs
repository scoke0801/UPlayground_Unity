using System;
using UnityEngine;
using UPlayGround.Ability.Core;

namespace UPlayGround.Gameplay.Cue
{
    public enum AbilityCueEventType
    {
        Started,
        Failed,
        Ended,
        CooldownReady,
    }

    public readonly struct AbilityCueEvent
    {
        public readonly string CueId;
        public readonly AbilityCueEventType EventType;
        public readonly string AbilityId;
        public readonly string VariantId;
        public readonly AbilityActivationResult Result;

        public AbilityCueEvent(
            string cueId,
            AbilityCueEventType eventType,
            string abilityId,
            string variantId,
            AbilityActivationResult result)
        {
            CueId = cueId;
            EventType = eventType;
            AbilityId = abilityId;
            VariantId = variantId;
            Result = result;
        }
    }

    /// <summary>
    /// Ability 계산과 표현을 분리하는 액터 로컬 신호 허브.
    /// VFX/SFX/UI/카메라 어댑터는 이 이벤트만 구독하고 Ability 상태를 변경하지 않는다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameplayCueDispatcher : MonoBehaviour
    {
        public event Action<AbilityCueEvent> CueDispatched;

        public void Dispatch(in AbilityCueEvent cue)
        {
            if (string.IsNullOrWhiteSpace(cue.CueId))
                return;
            CueDispatched?.Invoke(cue);
        }
    }
}
