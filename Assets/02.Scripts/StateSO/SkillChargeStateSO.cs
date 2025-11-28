using UnityEngine;
using Animancer;

namespace Game.FSM
{
    /// <summary>
    /// 스킬 버튼을 누르고 있는 동안 차징을 처리하는 상태입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "State_SkillCharge", menuName = "FSM/States/Skill Charge")]
    public class SkillChargeStateSO : StateSO
    {
        [Header("Animation")]
        [Tooltip("차징 중 반복 재생될 애니메이션 키")]
        public string ChargeAnimKey = "ChargeLoop";
        
        [Header("Charging Rules")]
        [Tooltip("발동으로 인정되는 최소 차징 시간")]
        public float MinChargeTime = 0.5f; 
        [Tooltip("자동 발동되는 최대 차징 시간")]
        public float MaxChargeTime = 2.0f; 

        [Header("Transitions")]
        [Tooltip("차징 성공 시 전환될 스킬 발동 상태")]
        public StateSO ChargedActionState; 

        // 블랙보드 키 (PlayerBrain과 공유)
        private const string CHARGE_TIMER_KEY = "CurrentChargeTime";
        private const string SKILL_INDEX_KEY = "CurrentSkillIndex"; 
        
        public override void OnEnter(CharacterBrain brain)
        {
            // 1. 애니메이션 재생
            ClipTransition clip = brain.AnimData.GetClipTransition(ChargeAnimKey);
            if (clip.Clip == null)
            {
                Debug.LogError($"[SkillCharge] '{ChargeAnimKey}' 클립이 없어 기본 상태로 복귀합니다.");
                brain.ChangeState(brain.DefaultState);
                return;
            }
            brain.Animancer.Play(clip, 0.15f); // 페이드 인 시간은 임의로 지정
            
            // 2. 차징 시간 초기화 및 시작
            brain.SetData(CHARGE_TIMER_KEY, 0f);
        }

        public override void OnUpdate(CharacterBrain brain)
        {
            // 1. Charge Timer 업데이트
            float currentChargeTime = brain.GetData<float>(CHARGE_TIMER_KEY, 0f);
            currentChargeTime += Time.deltaTime;
            brain.SetData(CHARGE_TIMER_KEY, currentChargeTime);
            
            // 2. 현재 상태에 진입할 때 사용된 스킬 인덱스를 가져옵니다.
            int skillIndex = brain.GetData<int>(SKILL_INDEX_KEY, 0); 
            if (skillIndex == 0) 
            {
                // 인덱스가 설정되지 않았다면 (PlayerBrain 설정 오류), 안전하게 종료
                CleanupAndTransition(brain, brain.DefaultState);
                return;
            }

            // 3. 최대 시간 초과 시 자동 발동 (Full Charge)
            if (currentChargeTime >= MaxChargeTime)
            {
                // BlackBoard에 차징 강도 설정 (Max)
                brain.SetData("ChargeRatio", 1.0f); 
                CleanupAndTransition(brain, ChargedActionState);
                return;
            }

            // 4. Released 신호 체크 (PlayerBrain이 설정한 BlackBoard 값)
            string releaseKey = $"Skill{skillIndex}Released";

            if (brain.GetData<bool>(releaseKey, false))
            {
                // Released 신호 소비 및 초기화
                brain.SetData(releaseKey, false); 
                
                // 5. 차지 레벨 평가 및 전환
                EvaluateChargeAndTransition(brain, currentChargeTime);
            }
        }

        private void EvaluateChargeAndTransition(CharacterBrain brain, float currentChargeTime)
        {
            if (currentChargeTime >= MinChargeTime)
            {
                // 최소 차징 시간 충족: ChargedActionState로 전환
                
                // 차징 강도 설정 (0~1 비율)
                float chargeRatio = Mathf.Clamp01(currentChargeTime / MaxChargeTime);
                brain.SetData("ChargeRatio", chargeRatio);
                
                Debug.Log($"[SkillCharge] 차징 성공. 비율: {chargeRatio:P0}");
                CleanupAndTransition(brain, ChargedActionState);
            }
            else
            {
                // 최소 시간 미달: 발동 실패, 기본 상태로 복귀
                Debug.Log($"[SkillCharge] 차징 실패 (시간 미달). 기본 상태 복귀.");
                CleanupAndTransition(brain, brain.DefaultState);
            }
        }
        
        /// <summary>
        /// 블랙보드 정리 후 상태 전환
        /// </summary>
        private void CleanupAndTransition(CharacterBrain brain, StateSO targetState)
        {
            // 차징 관련 데이터 정리
            brain.SetData(CHARGE_TIMER_KEY, 0f);
            brain.SetData(SKILL_INDEX_KEY, 0); // 인덱스 초기화 (CharacterBrain에 RemoveData가 없다고 가정)
            
            brain.ChangeState(targetState);
        }

        public override void OnExit(CharacterBrain brain)
        {
            // 상태를 벗어날 때 필요한 정리 작업
        }
    }
}