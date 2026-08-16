using System;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Combat;
using UPlayGround.Gameplay.Ability;
using UPlayGround.State;

namespace UPlayGround.Components
{
    /// <summary>한 프레임의 스태미나 소비·회복 처리 방식을 나타낸다.</summary>
    public enum PlayerStaminaActivity
    {
        Resting = 0,
        Sprinting = 1,
        RecoveryBlocked = 2,
    }

    /// <summary>플레이어 스태미나의 행동 비용, 지연 회복, HUD 변경 알림을 관리한다.</summary>
    public sealed class PlayerStaminaRuntime : IDisposable
    {
        private readonly PlayerActor _owner;
        private readonly AbilitySystemComponent _abilitySystem;
        private readonly AttributeSetRuntime _attributes;
        private readonly PlayerStaminaSettingsSO _settings;
        private float _recoveryStartsAt;

        public event Action<float, float> Changed;

        public float Current =>
            _attributes.GetCurrent(
                global::UPlayGround.Data.Stat.Attributes.Resource.Stamina);
        public float Maximum =>
            _attributes.GetCurrent(
                global::UPlayGround.Data.Stat.Attributes.Resource.MaxStamina);
        public float Ratio => Maximum > 0f
            ? Mathf.Clamp01(Current / Maximum)
            : 0f;
        public bool CanStartSprint =>
            Current >= _settings.minimumSprintStartStamina;
        public bool CanDash => CanSpend(_settings.dashCost);
        public bool CanDodge => CanSpend(_settings.dodgeCost);

        /// <summary>플레이어 Ability Attribute와 이동 스태미나 정책을 연결한다.</summary>
        public PlayerStaminaRuntime(
            PlayerActor owner,
            AbilitySystemComponent abilitySystem,
            PlayerStaminaSettingsSO settings)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _abilitySystem = abilitySystem
                ?? throw new ArgumentNullException(nameof(abilitySystem));
            _settings = settings
                ?? throw new ArgumentNullException(nameof(settings));
            _abilitySystem.EnsureInitialized();
            _attributes = _abilitySystem.Attributes;
            _attributes.AttributeChanged += OnAttributeChanged;
        }

        /// <summary>대시 1회 비용을 지불한다.</summary>
        public bool TrySpendDash() => TrySpend(_settings.dashCost);

        /// <summary>회피 1회 비용을 지불한다.</summary>
        public bool TrySpendDodge() => TrySpend(_settings.dodgeCost);

        /// <summary>현재 행동에 맞춰 달리기 소비, 전투 중 회복 차단, 지연 회복을 진행한다.</summary>
        public bool Tick(float deltaTime, PlayerStaminaActivity activity)
        {
            if (deltaTime <= 0f)
                return activity != PlayerStaminaActivity.Sprinting
                    || Current > 0f;

            if (activity == PlayerStaminaActivity.Sprinting)
                return DrainSprint(deltaTime);
            if (activity == PlayerStaminaActivity.RecoveryBlocked)
            {
                DelayRecovery();
                return true;
            }

            if (_owner.ActorTime < _recoveryStartsAt || Current >= Maximum)
                return true;

            SetCurrent(Current + _settings.recoveryPerSecond * deltaTime);
            return true;
        }

        /// <summary>스태미나를 쓰는 행동이 진행되는 동안 회복을 차단해야 하는 상태인지 반환한다.</summary>
        public static bool IsRecoveryBlockedState(ActorStateId stateId) =>
            stateId is ActorStateId.Attack
                or ActorStateId.Charge
                or ActorStateId.Dash
                or ActorStateId.DashAttack
                or ActorStateId.Dodge
                or ActorStateId.FinishAttack
                or ActorStateId.JumpAttack
                or ActorStateId.JumpDashAttack
                or ActorStateId.SpecialBreakAttack
                or ActorStateId.Ultimate;

        /// <summary>Attribute 변경 구독과 HUD 알림 구독을 해제한다.</summary>
        public void Dispose()
        {
            _attributes.AttributeChanged -= OnAttributeChanged;
            Changed = null;
        }

        private bool CanSpend(float amount) =>
            amount <= 0f || Current >= amount;

        private bool TrySpend(float amount)
        {
            return amount <= 0f
                || _abilitySystem.TryApplyResourceCost(
                    AbilityResourceType.Stamina,
                    amount);
        }

        private bool DrainSprint(float deltaTime)
        {
            float current = Current;
            if (current <= 0f) return false;

            float drain = _settings.sprintCostPerSecond * deltaTime;
            if (drain <= 0f) return true;

            float next = Mathf.Max(0f, current - drain);
            SetCurrent(next);
            return next > 0f;
        }

        private void DelayRecovery()
        {
            _recoveryStartsAt = _owner.ActorTime
                + _settings.recoveryDelaySeconds;
        }

        private void SetCurrent(float value)
        {
            _attributes.SetBase(
                global::UPlayGround.Data.Stat.Attributes.Resource.Stamina,
                Mathf.Clamp(value, 0f, Maximum));
        }

        private void OnAttributeChanged(AttributeChangedEvent change)
        {
            if (change.AttributeId
                == global::UPlayGround.Data.Stat.Attributes.Resource.Stamina)
            {
                if (change.NewCurrent < change.OldCurrent)
                    DelayRecovery();
                Changed?.Invoke(Current, Maximum);
                return;
            }

            if (change.AttributeId
                == global::UPlayGround.Data.Stat.Attributes.Resource.MaxStamina)
                Changed?.Invoke(Current, Maximum);
        }
    }
}
