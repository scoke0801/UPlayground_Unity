using UnityEngine;
using UPlayGround.Data.Ability;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;

namespace UPlayGround.Gameplay.Passive
{
    /// <summary>
    /// 현재 플레이어 캐릭터의 조건부 패시브를 방어 성공 이벤트와 연결한다.
    /// 상시 수치 패시브는 각 소비 지점에서 서비스 계약으로 조회한다.
    /// </summary>
    public sealed class PassiveAbilityController : MonoBehaviour
    {
        private PlayerActor _owner;
        private CharacterPassiveSetSO _currentSet;
        private System.Collections.Generic.IReadOnlyList<PassiveAbilitySO> _grantedPassives;

        private void Awake()
        {
            _owner = GetComponent<PlayerActor>();
            if (_owner != null)
                _owner.PassiveActivationSucceeded += OnPassiveActivationSucceeded;
        }

        public void RefreshForCharacter(CharacterActorType characterType)
        {
            _currentSet = Svc.Passives?.GetPassiveSet(characterType);
            _grantedPassives = Svc.Passives?.GetGrantedPassives(characterType);
        }

        private void OnPassiveActivationSucceeded(PassiveActivationType activationType)
        {
            if (_owner?.Effects == null)
                return;

            var seen = new System.Collections.Generic.HashSet<PassiveAbilitySO>();
            if (_currentSet?.passives != null)
                for (int i = 0; i < _currentSet.passives.Count; i++)
                    if (_currentSet.passives[i] != null
                        && seen.Add(_currentSet.passives[i]))
                        ApplyTriggeredPassive(_currentSet.passives[i], activationType);

            if (_grantedPassives == null)
                return;
            for (int i = 0; i < _grantedPassives.Count; i++)
                if (_grantedPassives[i] != null
                    && seen.Add(_grantedPassives[i]))
                    ApplyTriggeredPassive(_grantedPassives[i], activationType);
        }

        private void ApplyTriggeredPassive(
            PassiveAbilitySO passive,
            PassiveActivationType activationType)
        {
            if (passive == null
                || passive.activationType != activationType
                || passive.triggeredEffects == null)
                return;
            for (int j = 0; j < passive.triggeredEffects.Count; j++)
            {
                GameplayEffectSO effect = passive.triggeredEffects[j];
                if (effect != null)
                {
                    _owner.Effects.ApplyEffect(
                        effect,
                        _owner,
                        new GameplayEffectApplicationOptions(
                            passive.triggeredEffectHudVisibility));
                }
            }
        }

        private void OnDestroy()
        {
            if (_owner != null)
                _owner.PassiveActivationSucceeded -= OnPassiveActivationSucceeded;
        }
    }
}
