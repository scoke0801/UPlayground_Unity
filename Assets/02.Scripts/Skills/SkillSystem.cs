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
        
        // UI 연동 등을 위한 접근자
        public SkillSlot GetSkillSlot(int slotIndex)
        {
             if (slotIndex < 1 || slotIndex > 4) return null;
             return skillSlots[slotIndex - 1];
        }
    }
}