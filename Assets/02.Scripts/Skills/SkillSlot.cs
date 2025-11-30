using System;
using UnityEngine;
using Game.Data;

namespace Game.Skills
{
    [Serializable]
    public class SkillSlot
    {
        [SerializeField] private int slotIndex;           // 슬롯 번호 (1-4)
        
        // Json 데이터 참조
        private SkillJsonData skillJsonData;
        
        private float cooldownRemaining;
        
        // UI 업데이트용 이벤트
        public event Action<int, float> OnCooldownStart;  
        public event Action<int> OnCooldownEnd;           
        
        public SkillSlot(int index)
        {
            slotIndex = index;
        }
        
        // 프로퍼티
        public int SlotIndex => slotIndex;
        public bool IsOnCooldown => cooldownRemaining > 0f;
        public float CooldownRemaining => cooldownRemaining;
        
        // Json 데이터 기반 프로퍼티
        public SkillJsonData JsonData => skillJsonData;
        public bool HasSkill => skillJsonData != null;
        
        // 통합 프로퍼티
        public string SkillName => skillJsonData?.SkillName ?? "Empty";
        public int SkillID => skillJsonData?.SkillID ?? 0;
        public SkillType Type => skillJsonData?.Type ?? SkillType.Instant;
        public float CooldownTime => skillJsonData?.CooldownTime ?? 0f;
        public float ChargeTime => skillJsonData?.ChargeTime ?? 0f;
        public int ManaCost => skillJsonData?.ManaCost ?? 0;
        public int EnergyCost => skillJsonData?.EnergyCost ?? 0;
        public Sprite Icon => skillJsonData?.Icon;
        public GameObject EffectPrefab => skillJsonData?.SkillEffectPrefab;
        public AudioClip Sound => skillJsonData?.SkillSound;
        public string ActionAnimKey => skillJsonData?.ActionAnimKey ?? "Default";
        public string ExecutionStatePath => skillJsonData?.executionStatePath ?? "";
        
        // Json 데이터로 스킬 장착
        public void EquipSkill(SkillJsonData jsonData)
        {
            skillJsonData = jsonData;
            cooldownRemaining = 0f;
        }
        
        // 스킬 해제
        public void UnequipSkill()
        {
            skillJsonData = null;
            cooldownRemaining = 0f;
        }
        
        // 사용 가능 여부 체크
        public bool CanUseSkill()
        {
            if (!HasSkill) return false;
            if (IsOnCooldown) return false;
            
            // TODO: 마나/스태미나 등 추가 자원 체크
            
            return true;
        }
        
        // 쿨다운 시작
        public void StartCooldown()
        {
            if (!HasSkill) return;
            
            float cooldown = CooldownTime;
            if (cooldown > 0)
            {
                cooldownRemaining = cooldown;
                OnCooldownStart?.Invoke(slotIndex, cooldownRemaining);
            }
        }
        
        // 자원 소모
        public void ConsumeResources()
        {
            if (!HasSkill) return;
            
            // 쿨타임 적용
            float cooldown = CooldownTime;
            if (cooldown > 0)
            {
                cooldownRemaining = cooldown;
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
