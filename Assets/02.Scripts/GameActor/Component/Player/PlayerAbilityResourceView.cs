using System;
using UnityEngine;
using UPlayGround.Data;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Ability.Core;
using UPlayGround.Gameplay.Ability;

namespace UPlayGround.Components
{
    /// <summary>
    /// 플레이어 스킬 게이지 관리
    /// - 약/강/점프/대시/마무리 공격 히트 시 AttackKind별 충전
    /// - 스킬 사용 시 슬롯별 비용 소모
    /// - 스킬(SkillAttack)은 게이지를 충전하지 않음
    /// </summary>
    public class PlayerAbilityResourceView : PlayerActorComponent
    {
        public const int SkillSlotCount = 3;
        public const int AbilitySkillSlot = (int)PlayerSkillSlot.Ability;
        public const int UltimateSkillSlot = (int)PlayerSkillSlot.Ultimate;
        public const int ElementalImbueSkillSlot = (int)PlayerSkillSlot.ElementalImbue;

        [Serializable]
        public struct ChargeTable
        {
            public float normalAttack;   // 약 공격 1타당
            public float heavyAttack;    // 강 공격 1타당
            public float jumpAttack;     // 점프 공격
            public float dashAttack;     // 대시 공격
            public float finishAttack;   // 마무리 공격
            public float chargeAttack;   // 차지 공격
        }

        private AbilitySystemComponent AbilitySystem =>
            GetComponent<PlayerActor>()?.AbilitySystem;
        private float _maxGauge =>
            AbilitySystem?.Attributes.GetCurrent(global::UPlayGround.Data.Stat.Attributes.Resource.MaxUltimateEnergy) ?? 0f;
        private float _currentGauge
        {
            get => AbilitySystem?.Attributes.GetCurrent(global::UPlayGround.Data.Stat.Attributes.Resource.UltimateEnergy) ?? 0f;
            set => AbilitySystem?.Attributes.SetBase(global::UPlayGround.Data.Stat.Attributes.Resource.UltimateEnergy, value);
        }

        [Header("Charge Per Hit")]
        [SerializeField] private ChargeTable _chargeTable = new ChargeTable
        {
            normalAttack = 4f,
            heavyAttack  = 10f,
            jumpAttack   = 8f,
            dashAttack   = 8f,
            finishAttack = 30f,
            chargeAttack = 15f,
        };

        public event Action<float, float> OnGaugeChanged;
        public event Action<int, float, float> OnCooldownChanged;

        public float MaxGauge     => _maxGauge;
        public float CurrentGauge => _currentGauge;
        public float GaugeRatio   => _maxGauge > 0f ? _currentGauge / _maxGauge : 0f;

        private PlayerCombat _combat;
        private bool[] _cooldownWasActive;

        private void Awake()
        {
            _combat = GetComponent<PlayerCombat>();
            EnsureCooldownBuffer();
        }

        private void OnEnable()
        {
            if (_combat != null)
                _combat.OnAttackHit += HandleAttackHit;
            if (AbilitySystem?.Attributes != null)
                AbilitySystem.Attributes.AttributeChanged += OnAttributeChanged;
        }

        private void OnDisable()
        {
            if (_combat != null)
                _combat.OnAttackHit -= HandleAttackHit;
            if (AbilitySystem?.Attributes != null)
                AbilitySystem.Attributes.AttributeChanged -= OnAttributeChanged;
        }

        private void OnAttributeChanged(AttributeChangedEvent change)
        {
            if (change.AttributeId == global::UPlayGround.Data.Stat.Attributes.Resource.UltimateEnergy
                || change.AttributeId == global::UPlayGround.Data.Stat.Attributes.Resource.MaxUltimateEnergy)
                OnGaugeChanged?.Invoke(_currentGauge, _maxGauge);
        }

        private void Update()
        {
            if (_cooldownWasActive == null || AbilitySystem?.Runtime == null) return;

            for (int i = 0; i < _cooldownWasActive.Length; i++)
            {
                bool active = GetSkillCooldownRemaining(i) > 0f;
                if (!_cooldownWasActive[i] || active)
                {
                    _cooldownWasActive[i] = active;
                    continue;
                }
                _cooldownWasActive[i] = false;
                OnCooldownChanged?.Invoke(i, 0f, GetSkillCooldownDuration(i));
            }
        }

        // -------------------------------------------------------

        private void HandleAttackHit(AttackData attackData)
        {
            float charge = attackData.attackKind switch
            {
                AttackKind.NormalAttack  => _chargeTable.normalAttack,
                AttackKind.HeavyAttack   => _chargeTable.heavyAttack,
                AttackKind.JumpAttack    => _chargeTable.jumpAttack,
                AttackKind.DashAttack    => _chargeTable.dashAttack,
                AttackKind.FinishAttack  => _chargeTable.finishAttack,
                AttackKind.ChargeAttack  => _chargeTable.chargeAttack,
                AttackKind.SkillAttack   => 0f,   // 스킬은 충전 없음
                _                        => 0f,
            };

            if (charge > 0f)
                AddGauge(charge);
            AbilitySystem?.ProjectAbilities?.ApplyResourceRules(
                AbilityResourceTrigger.AttackHit);
        }

        /// <summary>호환용 읽기 API. 판정 권위는 ActorAbilitySystem에 있다.</summary>
        public bool CanUseSkill(int skillSlot)
        {
            return TryGetSlotState(skillSlot, out AbilitySlotViewState state)
                   && state.IsReady;
        }

        [Obsolete("비용과 쿨다운은 ActorAbilitySystem.Commit에서만 소비합니다.")]
        public bool ConsumeSkill(int skillSlot)
        {
            return false;
        }

        public void AddGauge(float amount)
        {
            if (amount <= 0f) return;
            AbilitySystem?.ApplyResourceDelta(
                AbilityResourceType.UltimateEnergy,
                amount,
                "GE_UltimateEnergy.Generation");
        }

        /// <summary>
        /// 캐릭터 교체 복원 시 게이지 값을 직접 설정한다.
        /// </summary>
        public void SetGauge(float value)
        {
            _currentGauge = Mathf.Clamp(value, 0f, _maxGauge);
        }

        public float[] GetCooldownRemainingSnapshot()
        {
            EnsureCooldownBuffer();
            var snapshot = new float[_cooldownWasActive.Length];
            for (int i = 0; i < snapshot.Length; i++)
                snapshot[i] = GetSkillCooldownRemaining(i);
            return snapshot;
        }

        public void SetCooldownRemainingSnapshot(float[] remainingTimes)
        {
            EnsureCooldownBuffer();
            if (_cooldownWasActive == null || AbilitySystem?.Runtime == null) return;

            for (int i = 0; i < _cooldownWasActive.Length; i++)
            {
                float remaining = remainingTimes != null && i < remainingTimes.Length
                    ? Mathf.Max(0f, remainingTimes[i])
                    : 0f;

                if (remaining > 0f)
                    AbilitySystem.ProjectAbilities.RestorePlayerSlotCooldown(
                        (PlayerSkillSlot)i,
                        remaining);
                else
                    AbilitySystem.ProjectAbilities.RestorePlayerSlotCooldown(
                        (PlayerSkillSlot)i,
                        0f);
                _cooldownWasActive[i] = remaining > 0f;
                OnCooldownChanged?.Invoke(i, remaining, GetSkillCooldownDuration(i));
            }
        }

        public float GetSkillCost(int skillSlot)
        {
            return TryGetSlotState(skillSlot, out AbilitySlotViewState state)
                ? state.ResourceRequired
                : float.PositiveInfinity;
        }

        public float GetSkillCooldownDuration(int skillSlot)
        {
            return TryGetSlotState(skillSlot, out AbilitySlotViewState state)
                ? state.CooldownDuration
                : 0f;
        }

        public float GetSkillCooldownRemaining(int skillSlot)
        {
            EnsureCooldownBuffer();
            if (!IsValidSkillSlot(skillSlot) || AbilitySystem?.Runtime == null)
                return 0f;
            return TryGetSlotState(skillSlot, out AbilitySlotViewState state)
                ? state.CooldownRemaining
                : 0f;
        }

        public bool IsSkillOnCooldown(int skillSlot) => GetSkillCooldownRemaining(skillSlot) > 0f;

        private void EnsureCooldownBuffer()
        {
            int count = SkillSlotCount;
            if (count <= 0)
            {
                _cooldownWasActive = Array.Empty<bool>();
                return;
            }

            if (_cooldownWasActive != null && _cooldownWasActive.Length == count)
                return;

            _cooldownWasActive = new bool[count];
        }

        [Obsolete("슬롯 키는 폐기되었습니다. ActorAbilitySystem.GetPlayerSlotCooldownGroupId를 사용하세요.")]
        public static string GetSkillSlotCooldownGroupId(int skillSlot) =>
            $"Ability.SkillSlot.{skillSlot}";

        public static bool IsValidSkillSlot(int skillSlot)
            => skillSlot >= 0 && skillSlot < SkillSlotCount;

        public static bool UsesGaugeCost(int skillSlot)
            => skillSlot == UltimateSkillSlot;

        private bool TryGetSlotState(
            int skillSlot,
            out AbilitySlotViewState state)
        {
            state = default;
            return IsValidSkillSlot(skillSlot)
                   && AbilitySystem?.ProjectAbilities != null
                   && AbilitySystem.ProjectAbilities.TryGetPlayerSlotState(
                       (PlayerSkillSlot)skillSlot,
                       out state);
        }
    }
}
