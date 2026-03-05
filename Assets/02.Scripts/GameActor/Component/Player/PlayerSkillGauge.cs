using System;
using UnityEngine;
using UPlayGround.Data;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Component
{
    /// <summary>
    /// 플레이어 스킬 게이지 관리
    /// - 약/강/점프/대시/마무리 공격 히트 시 AttackKind별 충전
    /// - 스킬 사용 시 슬롯별 비용 소모
    /// - 스킬(SkillAttack)은 게이지를 충전하지 않음
    /// </summary>
    public class PlayerSkillGauge : PlayerActorComponent
    {
        [Serializable]
        public struct ChargeTable
        {
            public float normalAttack;   // 약 공격 1타당
            public float heavyAttack;    // 강 공격 1타당
            public float jumpAttack;     // 점프 공격
            public float dashAttack;     // 대시 공격
            public float finishAttack;   // 마무리 공격
        }

        [Header("Gauge Settings")]
        [SerializeField] private float _maxGauge = 100f;
        [SerializeField] private float _currentGauge;

        [Header("Charge Per Hit")]
        [SerializeField] private ChargeTable _chargeTable = new ChargeTable
        {
            normalAttack = 4f,
            heavyAttack  = 10f,
            jumpAttack   = 8f,
            dashAttack   = 8f,
            finishAttack = 30f,
        };

        [Header("Skill Cost (Slot 0~4)")]
        [SerializeField] private float[] _skillCost = { 25f, 40f, 60f, 80f, 100f };

        public event Action<float, float> OnGaugeChanged;

        public float MaxGauge     => _maxGauge;
        public float CurrentGauge => _currentGauge;
        public float GaugeRatio   => _currentGauge / _maxGauge;

        private PlayerCombat _combat;

        private void Awake()
        {
            _combat = GetComponent<PlayerCombat>();
        }

        private void OnEnable()
        {
            if (_combat != null)
                _combat.OnAttackHit += HandleAttackHit;
        }

        private void OnDisable()
        {
            if (_combat != null)
                _combat.OnAttackHit -= HandleAttackHit;
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
                AttackKind.SkillAttack   => 0f,   // 스킬은 충전 없음
                _                        => 0f,
            };

            if (charge > 0f)
                AddGauge(charge);
        }

        /// <summary>스킬 슬롯 사용 가능 여부 (UI 버튼 활성화 등에 활용)</summary>
        public bool CanUseSkill(int skillSlot)
        {
            if ((uint)skillSlot >= (uint)_skillCost.Length) return false;
            return _currentGauge >= _skillCost[skillSlot];
        }

        /// <summary>
        /// 스킬 게이지 소모. 성공하면 true 반환.
        /// PlayerAttackState.GetAnimKey()에서 스킬 실행 직전 호출.
        /// </summary>
        public bool ConsumeSkill(int skillSlot)
        {
            if (!CanUseSkill(skillSlot)) return false;

            _currentGauge = Mathf.Max(0f, _currentGauge - _skillCost[skillSlot]);
            OnGaugeChanged?.Invoke(_currentGauge, _maxGauge);
            return true;
        }

        private void AddGauge(float amount)
        {
            _currentGauge = Mathf.Min(_currentGauge + amount, _maxGauge);
            OnGaugeChanged?.Invoke(_currentGauge, _maxGauge);
        }
    }
}
