using System;
using UnityEngine;

namespace Game.Skills
{
    [Serializable]
    public class SkillSlot
    {
        [SerializeField] private int slotIndex;           // 슬롯 번호 (1-4)
        [SerializeField] private SkillData skillData;     // 장착된 스킬 데이터
        
        private float cooldownRemaining;                  // 남은 쿨다운 시간
        
        // UI 업데이트용 이벤트
        public event Action<int, float> OnCooldownStart;  
        public event Action<int> OnCooldownEnd;           
        
        public SkillSlot(int index)
        {
            slotIndex = index;
        }
        
        // 프로퍼티
        public int SlotIndex => slotIndex;
        public SkillData Data => skillData;
        public bool IsOnCooldown => cooldownRemaining > 0f;
        public float CooldownRemaining => cooldownRemaining;

        // 스킬 장착
        public void EquipSkill(SkillData skill)
        {
            skillData = skill;
            cooldownRemaining = 0f;
        }
        
        // 스킬 해제
        public void UnequipSkill()
        {
            skillData = null;
            cooldownRemaining = 0f;
        }
        
        // 사용 가능 여부 체크 (단순 검사)
        public bool CanUseSkill()
        {
            if (skillData == null) return false;
            if (IsOnCooldown) return false;
            
            // TODO: 마나/스태미나 등 추가 자원 체크 로직이 있다면 여기에 추가
            // if (currentMana < skillData.ManaCost) return false;
            
            return true;
        }

        // 자원 소모 및 쿨타임 시작 (실제 사용 확정 시 호출)
        public void ConsumeResources()
        {
            if (skillData == null) return;
            
            // 쿨타임 적용
            if (skillData.CooldownTime > 0)
            {
                cooldownRemaining = skillData.CooldownTime;
                OnCooldownStart?.Invoke(slotIndex, cooldownRemaining);
            }
            
            // TODO: 마나 감소 로직 등 추가
        }
        
        // 쿨타임 업데이트
        public void Update(float deltaTime)
        {
            if (cooldownRemaining > 0f)
            {
                cooldownRemaining -= deltaTime;
                
                if (cooldownRemaining <= 0f)
                {
                    cooldownRemaining = 0f;
                    OnCooldownEnd?.Invoke(slotIndex);
                }
            }
        }
    }
}