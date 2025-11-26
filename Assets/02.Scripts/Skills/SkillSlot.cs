using System;
using UnityEngine;

namespace Game.Skills
{
    /// <summary>
    /// 스킬 슬롯 (쿨다운 관리)
    /// </summary>
    [Serializable]
    public class SkillSlot
    {
        [SerializeField] private int slotIndex;           // 슬롯 번호 (1-4)
        [SerializeField] private SkillData skillData;     // 장착된 스킬 데이터
        
        private float cooldownRemaining;                  // 남은 쿨다운 시간
        private float chargeTime;                         // 차징 시간
        private bool isCharging;                          // 차징 중인지
        private bool isCasting;                           // 시전 중인지
        
        // 이벤트
        public event Action<int> OnSkillActivated;        // 스킬 발동
        public event Action<int> OnSkillCharged;          // 차징 완료
        public event Action<int, float> OnCooldownStart;  // 쿨다운 시작
        public event Action<int> OnCooldownEnd;           // 쿨다운 종료
        
        public SkillSlot(int index)
        {
            slotIndex = index;
        }
        
        // 프로퍼티
        public int SlotIndex => slotIndex;
        public SkillData Data => skillData;
        public bool IsOnCooldown => cooldownRemaining > 0f;
        public float CooldownRemaining => cooldownRemaining;
        public float CooldownProgress => skillData != null ? 1f - (cooldownRemaining / skillData.CooldownTime) : 1f;
        public bool IsCharging => isCharging;
        public float ChargeProgress => skillData != null ? chargeTime / skillData.ChargeTime : 0f;
        public bool IsCasting => isCasting;
        
        /// <summary>
        /// 스킬 장착
        /// </summary>
        public void EquipSkill(SkillData skill)
        {
            skillData = skill;
            cooldownRemaining = 0f;
            chargeTime = 0f;
            isCharging = false;
            isCasting = false;
        }
        
        /// <summary>
        /// 스킬 해제
        /// </summary>
        public void UnequipSkill()
        {
            skillData = null;
            cooldownRemaining = 0f;
            chargeTime = 0f;
            isCharging = false;
            isCasting = false;
        }
        
        /// <summary>
        /// 스킬 사용 가능 체크
        /// </summary>
        public bool CanUseSkill()
        {
            if (skillData == null) return false;
            if (IsOnCooldown) return false;
            if (isCasting) return false;
            
            // TODO: 마나/에너지 체크
            return true;
        }
        
        /// <summary>
        /// 스킬 사용 시도
        /// </summary>
        public bool TryUseSkill()
        {
            if (!CanUseSkill())
                return false;
            
            switch (skillData.Type)
            {
                case SkillType.Instant:
                    ActivateSkill();
                    return true;
                    
                case SkillType.Charged:
                    StartCharging();
                    return true;
                    
                case SkillType.Toggle:
                case SkillType.Channeling:
                    ActivateSkill();
                    return true;
                    
                default:
                    return false;
            }
        }
        
        /// <summary>
        /// 차징 시작
        /// </summary>
        private void StartCharging()
        {
            isCharging = true;
            chargeTime = 0f;
        }
        
        /// <summary>
        /// 차징 종료 (키를 떼면)
        /// </summary>
        public void StopCharging()
        {
            if (!isCharging) return;
            
            isCharging = false;
            
            // 최소 차징 시간 충족 시 발동
            if (chargeTime >= skillData.ChargeTime)
            {
                ActivateSkill();
                OnSkillCharged?.Invoke(slotIndex);
            }
            
            chargeTime = 0f;
        }
        
        /// <summary>
        /// 스킬 발동
        /// </summary>
        private void ActivateSkill()
        {
            if (skillData == null) return;
            
            // 쿨다운 시작
            StartCooldown();
            
            // 이벤트 발행
            OnSkillActivated?.Invoke(slotIndex);
            
            Debug.Log($"[SkillSlot] 스킬 발동: {skillData.SkillName} (슬롯 {slotIndex})");
        }
        
        /// <summary>
        /// 쿨다운 시작
        /// </summary>
        private void StartCooldown()
        {
            if (skillData == null) return;
            
            cooldownRemaining = skillData.CooldownTime;
            OnCooldownStart?.Invoke(slotIndex, cooldownRemaining);
        }
        
        /// <summary>
        /// 쿨다운 강제 시작 (외부에서 호출)
        /// </summary>
        public void ForceCooldown(float duration)
        {
            cooldownRemaining = duration;
            OnCooldownStart?.Invoke(slotIndex, cooldownRemaining);
        }
        
        /// <summary>
        /// 쿨다운 초기화
        /// </summary>
        public void ResetCooldown()
        {
            cooldownRemaining = 0f;
            OnCooldownEnd?.Invoke(slotIndex);
        }
        
        /// <summary>
        /// 매 프레임 업데이트
        /// </summary>
        public void Update(float deltaTime)
        {
            // 쿨다운 감소
            if (cooldownRemaining > 0f)
            {
                cooldownRemaining -= deltaTime;
                
                if (cooldownRemaining <= 0f)
                {
                    cooldownRemaining = 0f;
                    OnCooldownEnd?.Invoke(slotIndex);
                }
            }
            
            // 차징 증가
            if (isCharging && skillData != null)
            {
                chargeTime += deltaTime;
            }
        }
    }
}
