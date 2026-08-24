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
        private float _dodgeAvailableAt;

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
        public bool CanDodge =>
            _owner.ActorTime >= _dodgeAvailableAt
            && CanSpend(_settings.dodgeCost);

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
        public bool TrySpendDodge() =>
            CanDodge && TrySpend(_settings.dodgeCost);

        /// <summary>회피 동작이 끝난 시점부터 캐릭터 성장 효과가 반영된 재사용 대기를 시작한다.</summary>
        public void StartDodgeCooldown()
        {
            float multiplier = UPlayGround.Manager.Svc.Party
                ?.GetDodgeCooldownMultiplier(_owner.CharacterType) ?? 1f;
            _dodgeAvailableAt = _owner.ActorTime
                + _settings.dodgeCooldownSeconds * multiplier;
        }

        /// <summary>최대 스태미나 변경 뒤 현재값을 회복시키지 않고 새 상한 안으로 제한한다.</summary>
        public void ClampToMaximum()
        {
            if (Current > Maximum)
                SetCurrent(Maximum);
        }

        /// <summary>현재 행동에 맞춰 달리기 소비, 차지 중 회복 차단, 지연 회복을 진행한다.</summary>
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

        /// <summary>입력을 유지해 이득을 보류하는 차지 상태인지 반환한다.</summary>
        public static bool IsRecoveryBlockedState(ActorStateId stateId) =>
            stateId == ActorStateId.Charge;

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
