using System;
using UnityEngine;
using Game.Data;

namespace Game.Skills
{
    public class SkillSystem : MonoBehaviour
    {
        [Header("스킬 설정")]
        [Tooltip("Json에서 로드할 스킬 ID 배열")]
        [SerializeField] private int[] skillIDs = new int[4] { 1, 2, 3, 4 };
        
        // 스킬 슬롯 관리
        private SkillSlot[] skillSlots = new SkillSlot[4];
        
        private void Awake()
        {
            InitializeSkillSlots();
        }
        
        private void Start()
        {
            LoadSkillsFromJson();
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
        
        /// <summary>
        /// Json 데이터로부터 스킬 로드
        /// </summary>
        private void LoadSkillsFromJson()
        {
            var jsonManager = JsonDataManager.Instance;
            if (jsonManager == null)
            {
                Debug.LogError("[SkillSystem] JsonDataManager를 찾을 수 없습니다!");
                return;
            }

            for (int i = 0; i < skillIDs.Length && i < 4; i++)
            {
                var jsonData = jsonManager.GetData<SkillJsonData>(skillIDs[i]);
                if (jsonData != null)
                {
                    skillSlots[i].EquipSkill(jsonData);
                    Debug.Log($"[SkillSystem] 슬롯 {i + 1}에 스킬 '{jsonData.SkillName}' 장착 완료");
                }
                else
                {
                    Debug.LogWarning($"[SkillSystem] ID {skillIDs[i]}인 스킬 데이터를 찾을 수 없습니다.");
                }
            }
        }

        /// <summary>
        /// PlayerBrain에서 호출: 스킬 사용 시도
        /// </summary>
        public bool TryUseSkill(int slotIndex, out SkillJsonData data)
        {
            data = null;
            if (slotIndex < 1 || slotIndex > 4) return false;

            SkillSlot slot = skillSlots[slotIndex - 1];

            if (slot.CanUseSkill())
            {
                slot.ConsumeResources();
                data = slot.JsonData;
                return true;
            }

            return false;
        }
        
        /// <summary>
        /// 스킬 실행 액션
        /// </summary>
        public void ExecuteSkillAction(int slotIndex, float chargeRatio)
        {
            if (slotIndex < 1 || slotIndex > 4) return;
    
            SkillSlot slot = skillSlots[slotIndex - 1];
            if (!slot.HasSkill) return;
    
            // 쿨다운 시작
            slot.StartCooldown();
    
            // 실제 스킬 로직 실행
            ExecuteSkillLogic(slot, chargeRatio);
    
            Debug.Log($"[SkillSystem] 스킬 {slot.SkillName} 실행 완료. 차징 비율: {chargeRatio:P0}");
        }
        
        // UI 연동 등을 위한 접근자
        public SkillSlot GetSkillSlot(int slotIndex)
        {
             if (slotIndex < 1 || slotIndex > 4) return null;
             return skillSlots[slotIndex - 1];
        }
        
        /// <summary>
        /// 슬롯 인덱스로 스킬 Json 데이터를 가져옵니다.
        /// </summary>
        public SkillJsonData GetSkillData(int slotIndex)
        {
            if (slotIndex < 1 || slotIndex > 4) return null;
            return skillSlots[slotIndex - 1].JsonData;
        }
        
        /// <summary>
        /// 실제 스킬 로직 실행 (이펙트, 데미지 등)
        /// </summary>
        private void ExecuteSkillLogic(SkillSlot slot, float chargeRatio)
        {
            if (slot == null || !slot.HasSkill) return;
            
            // 이펙트 생성
            if (slot.EffectPrefab != null)
            {
                GameObject effect = Instantiate(slot.EffectPrefab, transform.position, Quaternion.identity);
                // chargeRatio에 따라 이펙트 크기 조절
                effect.transform.localScale *= (1f + chargeRatio * 0.5f);
            }
            
            // 사운드 재생
            if (slot.Sound != null)
            {
                // AudioSource.PlayClipAtPoint(slot.Sound, transform.position);
            }
            
            Debug.Log($"[SkillSystem] 스킬 실행: {slot.SkillName}, 차징 비율: {chargeRatio:P0}");
        }
        
        /// <summary>
        /// 런타임 중 스킬 변경 (Json ID 기반)
        /// </summary>
        public void ChangeSkillById(int slotIndex, int skillID)
        {
            if (slotIndex < 1 || slotIndex > 4) return;
            
            var jsonData = JsonDataManager.Instance.GetData<SkillJsonData>(skillID);
            if (jsonData != null)
            {
                skillSlots[slotIndex - 1].EquipSkill(jsonData);
                Debug.Log($"[SkillSystem] 슬롯 {slotIndex}의 스킬이 '{jsonData.SkillName}'으로 변경되었습니다.");
            }
        }
    }
}
