using UnityEngine;
using Animancer;
using Game.Skills; // SkillSystem 참조를 위해 필요

namespace Game.FSM
{
    /// <summary>
    /// 스킬 발동 애니메이션을 재생하고 실제 스킬 로직을 실행하는 상태입니다.
    /// ChargedStateSO에서 전환될 때 사용됩니다.
    /// </summary>
    [CreateAssetMenu(fileName = "State_SkillAction", menuName = "FSM/States/Skill Action")]
    public class SkillActionStateSO : StateSO
    {
        [Header("Animation")]
        [SerializeField] private float fadeDuration = 0.15f;

        // 블랙보드 키 (ChargeStateSO와 동일)
        private const string SKILL_INDEX_KEY = "CurrentSkillIndex";
        private const string CHARGE_RATIO_KEY = "ChargeRatio"; 

        public override void OnEnter(CharacterBrain brain)
        {
            // 1. 블랙보드에서 데이터 추출
            int slotIndex = brain.GetData<int>(SKILL_INDEX_KEY, 0);
            // ChargeRatio는 차징 스킬이 아닐 경우 0f가 들어갈 수 있습니다. (Instants 스킬과 공유할 경우)
            float chargeRatio = brain.GetData<float>(CHARGE_RATIO_KEY, 0f); 
            
// SkillSystem 참조
            SkillSystem skillSystem = brain.GetComponent<SkillSystem>();
            if (skillSystem == null || slotIndex == 0)
            {
                Debug.LogError("[SkillActionState] 스킬 인덱스/시스템 없음. 기본 상태로 복귀.");
                CleanupAndTransition(brain, brain.DefaultState);
                return;
            }

            // 🌟 2. SkillSystem을 통해 SkillData를 가져옵니다. 🌟
            SkillData skillData = skillSystem.GetSkillData(slotIndex);
            if (skillData == null)
            {
                Debug.LogError($"[SkillActionState] 슬롯 {slotIndex}에 스킬 데이터가 없습니다.");
                CleanupAndTransition(brain, brain.DefaultState);
                return;
            }
            
            // 🌟 3. SkillData에서 애니메이션 키를 가져와 사용합니다. 🌟
            string animKey = skillData.ActionAnimKey; // 새로 추가된 프로퍼티 사용!
            
            // 4. 애니메이션 재생
            ClipTransition animClip = brain.AnimData.GetClipTransition(animKey);
            if (animClip.Clip == null)
            {
                Debug.LogError($"[SkillActionState] 스킬 데이터의 '{animKey}' 클립 없음. 기본 상태로 복귀.");
                CleanupAndTransition(brain, brain.DefaultState);
                return;
            }

            var animState = brain.Animancer.Play(animClip, fadeDuration);

            // 5. 스킬 시스템에 최종 발동 요청 (쿨다운 및 로직 처리)
            skillSystem.ExecuteSkillAction(slotIndex, chargeRatio);
            if (skillSystem != null)
            {
                // SkillSystem은 ChargeRatio를 받아 위력, 범위 등을 조절할 수 있습니다.
                skillSystem.ExecuteSkillAction(slotIndex, chargeRatio); 
            }
            
            // 4. 애니메이션 종료 이벤트 바인딩
            if (animState.Events(brain, out AnimancerEvent.Sequence events))
            {
                events.OnEnd = () => 
                {
                    // 애니메이션이 끝나면 기본 상태로 복귀
                    CleanupAndTransition(brain, brain.DefaultState);
                };
            }
        }

        public override void OnExit(CharacterBrain brain)
        {
            // 애니메이션 이벤트 등 정리
        }
        
        /// <summary>
        /// 블랙보드 정리 후 상태 전환
        /// </summary>
        private void CleanupAndTransition(CharacterBrain brain, StateSO targetState)
        {
            // 사용한 블랙보드 키 초기화
            brain.SetData(SKILL_INDEX_KEY, 0); 
            brain.SetData(CHARGE_RATIO_KEY, 0f);
            brain.ChangeState(targetState);
        }
    }
}