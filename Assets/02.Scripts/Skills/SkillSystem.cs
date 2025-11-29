using System;
using UnityEngine;

namespace Game.Skills
{
    public class SkillSystem : MonoBehaviour
    {
        [Header("초기 스킬 설정")]
        [SerializeField] private SkillData skill1Data;
        [SerializeField] private SkillData skill2Data;
        [SerializeField] private SkillData skill3Data;
        [SerializeField] private SkillData skill4Data;
        
        // 스킬 슬롯 관리
        private SkillSlot[] skillSlots = new SkillSlot[4];
        
        private void Awake()
        {
            InitializeSkillSlots();
        }
        
        private void Start()
        {
            EquipInitialSkills();
        }
        
        private void Update()
        {
            // 쿨타임 갱신
            foreach (var slot in skillSlots)
            {
                slot.Update(Time.deltaTime);
            }
        }

        private void InitializeSkillSlots()
        {
            for (int i = 0; i < skillSlots.Length; i++)
            {
                skillSlots[i] = new SkillSlot(i + 1);
            }
        }
        
        private void EquipInitialSkills()
        {
            if (skill1Data != null) skillSlots[0].EquipSkill(skill1Data);
            if (skill2Data != null) skillSlots[1].EquipSkill(skill2Data);
            if (skill3Data != null) skillSlots[2].EquipSkill(skill3Data);
            if (skill4Data != null) skillSlots[3].EquipSkill(skill4Data);
        }

        /// <summary>
        /// PlayerBrain에서 호출: 스킬 사용 시도
        /// </summary>
        /// <param name="slotIndex">1~4번 슬롯</param>
        /// <param name="data">성공 시 사용할 스킬 데이터 반환</param>
        /// <returns>사용 성공 여부</returns>
        public bool TryUseSkill(int slotIndex, out SkillData data)
        {
            data = null;
            if (slotIndex < 1 || slotIndex > 4) return false;

            SkillSlot slot = skillSlots[slotIndex - 1];

            // 1. 사용 조건 확인 (쿨타임, 자원 등)
            if (slot.CanUseSkill())
            {
                // 2. 자원 소모 및 쿨타임 시작
                slot.ConsumeResources();
                
                // 3. 데이터 반환 (Brain이 State 전환에 사용)
                data = slot.Data;
                return true;
            }

            return false;
        }
        
        // SkillSystem.cs 내부에 추가될 메서드
        public void ExecuteSkillAction(int slotIndex, float chargeRatio)
        {
            if (slotIndex < 1 || slotIndex > 4) return;
    
            SkillSlot slot = skillSlots[slotIndex - 1];
    
            // 1. 쿨다운 시작
            slot.StartCooldown();
    
            // 2. 실제 스킬 로직 실행 (chargeRatio를 활용)
            ExecuteSkillLogic(slot.Data, chargeRatio);
    
            Debug.Log($"[SkillSystem] 스킬 {slot.Data.SkillName} 실행 완료. 차징 비율: {chargeRatio:P0}");
    
            // ...
        }
        
        // UI 연동 등을 위한 접근자
        public SkillSlot GetSkillSlot(int slotIndex)
        {
             if (slotIndex < 1 || slotIndex > 4) return null;
             return skillSlots[slotIndex - 1];
        }
        
        /// <summary>
        /// 슬롯 인덱스로 스킬 데이터를 가져옵니다.
        /// </summary>
        public SkillData GetSkillData(int slotIndex)
        {
            if (slotIndex < 1 || slotIndex > 4) return null;
     
            // SkillSlot 배열은 0부터 시작하므로 인덱스 조정
            return skillSlots[slotIndex - 1].Data; 
        }
        
        /// <summary>
        /// 실제 스킬 로직 실행 (이펙트, 데미지 등)
        /// </summary>
        private void ExecuteSkillLogic(SkillData skillData, float chargeRatio)
        {
            if (skillData == null) return;
            
            // 이펙트 생성 (ChargeRatio에 따라 크기/위치/위력 조절 가능)
            if (skillData.SkillEffectPrefab != null)
            {
                // 예: Instantiate(skillData.SkillEffectPrefab, transform.position, Quaternion.identity);
            }
            
            // 사운드 재생
            
            // 여기에 실제 스킬 효과 구현 (예: 데미지 계산 시 chargeRatio 반영)
            Debug.Log($"[SkillSystem] 스킬 실행: {skillData.SkillName}, 차징 비율: {chargeRatio:P0}");
        }

    }
}