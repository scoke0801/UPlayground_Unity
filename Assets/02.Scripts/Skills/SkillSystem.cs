using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Input;

namespace Game.Skills
{
    /// <summary>
    /// 스킬 시스템 (플레이어 스킬 관리)
    /// </summary>
    public class SkillSystem : MonoBehaviour
    {
        [Header("스킬 슬롯 설정")]
        [SerializeField] private SkillData skill1Data;
        [SerializeField] private SkillData skill2Data;
        [SerializeField] private SkillData skill3Data;
        [SerializeField] private SkillData skill4Data;
        
        [Header("참조")]
        [SerializeField] private InputReader inputReader;
        
        // 스킬 슬롯
        private SkillSlot[] skillSlots = new SkillSlot[4];
        
        // 이벤트
        public event Action<int, SkillData> OnSkillUsed;      // 스킬 사용
        public event Action<int, float> OnSkillCooldown;      // 쿨다운 시작
        
        private void Awake()
        {
            InitializeSkillSlots();
        }
        
        private void Start()
        {
            // InputReader 자동 찾기
            if (inputReader == null)
            {
                inputReader = GetComponent<InputReader>();
                if (inputReader == null)
                {
                    Debug.LogError("[SkillSystem] InputReader를 찾을 수 없습니다!");
                    return;
                }
            }
            
            // 입력 이벤트 구독
            inputReader.OnSkillPressed += HandleSkillPressed;
            inputReader.OnSkillReleased += HandleSkillReleased;
            
            // 초기 스킬 장착
            EquipInitialSkills();
        }
        
        private void Update()
        {
            UpdateSkillSlots();
        }
        
        private void OnDestroy()
        {
            // 입력 이벤트 구독 해제
            if (inputReader != null)
            {
                inputReader.OnSkillPressed -= HandleSkillPressed;
                inputReader.OnSkillReleased -= HandleSkillReleased;
            }
        }
        
        #region 초기화
        
        private void InitializeSkillSlots()
        {
            for (int i = 0; i < skillSlots.Length; i++)
            {
                skillSlots[i] = new SkillSlot(i + 1); // 슬롯 번호는 1부터 시작
                
                // 이벤트 구독
                skillSlots[i].OnSkillActivated += OnSkillActivated;
                skillSlots[i].OnCooldownStart += OnSkillCooldownStart;
            }
            
            Debug.Log("[SkillSystem] 스킬 슬롯 초기화 완료");
        }
        
        private void EquipInitialSkills()
        {
            if (skill1Data != null) EquipSkill(1, skill1Data);
            if (skill2Data != null) EquipSkill(2, skill2Data);
            if (skill3Data != null) EquipSkill(3, skill3Data);
            if (skill4Data != null) EquipSkill(4, skill4Data);
        }
        
        #endregion
        
        #region 스킬 슬롯 관리
        
        /// <summary>
        /// 스킬 장착
        /// </summary>
        public void EquipSkill(int slotIndex, SkillData skillData)
        {
            if (slotIndex < 1 || slotIndex > 4)
            {
                Debug.LogWarning($"[SkillSystem] 잘못된 슬롯 번호: {slotIndex}");
                return;
            }
            
            skillSlots[slotIndex - 1].EquipSkill(skillData);
            Debug.Log($"[SkillSystem] 스킬 장착: {skillData.SkillName} -> 슬롯 {slotIndex}");
        }
        
        /// <summary>
        /// 스킬 해제
        /// </summary>
        public void UnequipSkill(int slotIndex)
        {
            if (slotIndex < 1 || slotIndex > 4)
            {
                Debug.LogWarning($"[SkillSystem] 잘못된 슬롯 번호: {slotIndex}");
                return;
            }
            
            skillSlots[slotIndex - 1].UnequipSkill();
            Debug.Log($"[SkillSystem] 스킬 해제: 슬롯 {slotIndex}");
        }
        
        /// <summary>
        /// 스킬 슬롯 가져오기
        /// </summary>
        public SkillSlot GetSkillSlot(int slotIndex)
        {
            if (slotIndex < 1 || slotIndex > 4)
                return null;
                
            return skillSlots[slotIndex - 1];
        }
        
        /// <summary>
        /// 모든 스킬 슬롯 가져오기
        /// </summary>
        public SkillSlot[] GetAllSkillSlots()
        {
            return skillSlots;
        }
        
        #endregion
        
        #region 입력 처리
        
        private void HandleSkillPressed(int skillIndex)
        {
            if (skillIndex < 1 || skillIndex > 4)
                return;
            
            SkillSlot slot = skillSlots[skillIndex - 1];
            
            // 스킬 사용 시도
            if (slot.TryUseSkill())
            {
                // 입력 소비
                inputReader.ConsumeSkillInput(skillIndex);
            }
        }
        
        private void HandleSkillReleased(int skillIndex)
        {
            if (skillIndex < 1 || skillIndex > 4)
                return;
            
            SkillSlot slot = skillSlots[skillIndex - 1];
            
            // 차징 스킬인 경우 차징 종료
            if (slot.Data != null && slot.Data.Type == SkillType.Charged)
            {
                slot.StopCharging();
            }
        }
        
        #endregion
        
        #region 업데이트
        
        private void UpdateSkillSlots()
        {
            foreach (var slot in skillSlots)
            {
                slot.Update(Time.deltaTime);
            }
        }
        
        #endregion
        
        #region 이벤트 콜백
        
        private void OnSkillActivated(int slotIndex)
        {
            SkillSlot slot = GetSkillSlot(slotIndex);
            if (slot == null || slot.Data == null) return;
            
            // 스킬 사용 이벤트 발행
            OnSkillUsed?.Invoke(slotIndex, slot.Data);
            
            // 여기서 실제 스킬 로직 실행
            ExecuteSkill(slot.Data);
        }
        
        private void OnSkillCooldownStart(int slotIndex, float duration)
        {
            // 쿨다운 시작 이벤트 발행 (UI 업데이트 등)
            OnSkillCooldown?.Invoke(slotIndex, duration);
        }
        
        #endregion
        
        #region 스킬 실행
        
        /// <summary>
        /// 실제 스킬 로직 실행
        /// </summary>
        private void ExecuteSkill(SkillData skillData)
        {
            if (skillData == null) return;
            
            // 이펙트 생성
            if (skillData.SkillEffectPrefab != null)
            {
                Instantiate(skillData.SkillEffectPrefab, transform.position, Quaternion.identity);
            }
            
            // 사운드 재생
            if (skillData.SkillSound != null)
            {
                // TODO: AudioManager와 연동
                // AudioManager.Instance.PlaySound(skillData.SkillSound);
            }
            
            // 여기에 실제 스킬 효과 구현
            // 예: 데미지, 버프, 힐 등
            Debug.Log($"[SkillSystem] 스킬 실행: {skillData.SkillName}");
        }
        
        #endregion
        
        #region 유틸리티
        
        /// <summary>
        /// 모든 쿨다운 초기화 (치트/디버그용)
        /// </summary>
        public void ResetAllCooldowns()
        {
            foreach (var slot in skillSlots)
            {
                slot.ResetCooldown();
            }
            
            Debug.Log("[SkillSystem] 모든 쿨다운 초기화");
        }
        
        /// <summary>
        /// 스킬 사용 가능 여부 체크
        /// </summary>
        public bool CanUseSkill(int slotIndex)
        {
            if (slotIndex < 1 || slotIndex > 4)
                return false;
                
            return skillSlots[slotIndex - 1].CanUseSkill();
        }
        
        #endregion
    }
}
