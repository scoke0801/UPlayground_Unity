using UnityEngine;
using Animancer;
using Game.Skills;
using Game.Data;

namespace Game.FSM
{
    /// <summary>
    /// 통합 액션 상태: 스킬 발동과 콤보 공격을 모두 처리합니다.
    /// 
    /// <para><b>스킬 모드:</b></para>
    /// - 블랙보드에 CurrentSkillIndex가 설정되어 있으면 스킬 시스템을 통해 실행
    /// - SkillSystem에서 스킬 데이터, 애니메이션, 쿨다운 관리
    /// - 차징 스킬은 별도의 SkillChargeStateSO에서 처리
    /// 
    /// <para><b>콤보 모드:</b></para>
    /// - ComboAnimationKeys 배열이 설정되어 있으면 순차적 콤보 공격 처리
    /// - 일반 공격(마우스 클릭)에 사용
    /// - 콤보 타이밍 내 추가 입력 시 다음 공격으로 연계
    /// 
    /// <para><b>사용 예시:</b></para>
    /// <code>
    /// // 일반 콤보 공격용 설정:
    /// ComboAnimationKeys = ["Attack1", "Attack2", "Attack3"]
    /// HitStart = 0.3f
    /// HitEnd = 0.6f
    /// ComboResetTime = 2.0f
    /// 
    /// // 스킬 실행은 PlayerBrain에서 자동으로 블랙보드 설정
    /// brain.SetData("CurrentSkillIndex", slotIndex);
    /// brain.ChangeState(skillActionState);
    /// </code>
    /// </summary>
    [CreateAssetMenu(fileName = "State_SkillAction", menuName = "FSM/States/Skill Action")]
    public class SkillActionStateSO : StateSO
    {
        [Header("Animation Settings")]
        [SerializeField] private float fadeDuration = 0.15f;
        
        [Header("Combo Settings (Optional)")]
        [Tooltip("콤보 공격용 애니메이션 키 배열 (일반 공격에 사용)\n예: [\"Attack1\", \"Attack2\", \"Attack3\"]")]
        public string[] ComboAnimationKeys;
        
        [Header("Hit Detection Settings")]
        [Tooltip("공격 판정 시작 시점 (애니메이션 진행률 0~1)")]
        [Range(0, 1)] public float HitStart = 0.3f;
        
        [Tooltip("공격 판정 종료 시점 (애니메이션 진행률 0~1)")]
        [Range(0, 1)] public float HitEnd = 0.6f;
        
        [Tooltip("히트박스를 사용할지 여부\n체크 해제 시 히트박스 제어를 건너뜁니다.")]
        public bool UseHitBox = true;
        
        [Header("Combo Timing")]
        [Tooltip("콤보 유지 시간 (초)\n이 시간 내에 다음 공격 입력이 없으면 첫 공격으로 리셋됩니다.")]
        public float ComboResetTime = 2.0f;

        // 블랙보드 키 상수
        private const string SKILL_INDEX_KEY = "CurrentSkillIndex";
        private const string CHARGE_RATIO_KEY = "ChargeRatio"; 
        private const string COMBO_INDEX_KEY = "ComboIndex";
        private const string LAST_ATTACK_TIME_KEY = "LastAttackTime";

        public override void OnEnter(CharacterBrain brain)
        {
            // 1. 스킬 슬롯 기반 실행 시도
            int slotIndex = brain.GetData<int>(SKILL_INDEX_KEY, 0);
            
            if (slotIndex > 0)
            {
                // 스킬 실행 경로
                ExecuteSkillAction(brain, slotIndex);
            }
            else if (ComboAnimationKeys != null && ComboAnimationKeys.Length > 0)
            {
                // 콤보 공격 경로
                ExecuteComboAttack(brain);
            }
            else
            {
                Debug.LogWarning("[SkillActionState] 스킬 슬롯도 콤보 애니메이션도 설정되지 않음. 기본 상태로 복귀.");
                CleanupAndTransition(brain, brain.DefaultState);
            }
        }

        public override void OnExit(CharacterBrain brain)
        {
            // 히트박스 안전 종료
            if (UseHitBox)
            {
                brain.SetHitBox(false);
            }
        }

        /// <summary>
        /// 스킬 시스템을 통한 스킬 실행
        /// 블랙보드의 CurrentSkillIndex를 기반으로 스킬을 실행합니다.
        /// </summary>
        /// <param name="brain">캐릭터 브레인</param>
        /// <param name="slotIndex">스킬 슬롯 인덱스 (1-4)</param>
        private void ExecuteSkillAction(CharacterBrain brain, int slotIndex)
        {
            float chargeRatio = brain.GetData<float>(CHARGE_RATIO_KEY, 0f);
            
            SkillSystem skillSystem = brain.GetComponent<SkillSystem>();
            if (skillSystem == null)
            {
                Debug.LogError("[SkillActionState] SkillSystem 컴포넌트를 찾을 수 없습니다.");
                CleanupAndTransition(brain, brain.DefaultState);
                return;
            }

            SkillSlot skillSlot = skillSystem.GetSkillSlot(slotIndex);
            if (skillSlot == null || !skillSlot.HasSkill)
            {
                Debug.LogError($"[SkillActionState] 슬롯 {slotIndex}에 스킬이 없습니다.");
                CleanupAndTransition(brain, brain.DefaultState);
                return;
            }
            
            string animKey = skillSlot.ActionAnimKey;
            ClipTransition animClip = brain.AnimData.GetClipTransition(animKey);
            
            if (animClip.Clip == null)
            {
                Debug.LogError($"[SkillActionState] 애니메이션 클립 '{animKey}'을(를) 찾을 수 없습니다.");
                CleanupAndTransition(brain, brain.DefaultState);
                return;
            }

            // 애니메이션 재생
            var animState = brain.Animancer.Play(animClip, fadeDuration);

            // 스킬 실행 (이펙트, 사운드, 쿨다운)
            skillSystem.ExecuteSkillAction(slotIndex, chargeRatio);
            
            // 히트박스 이벤트 바인딩
            SetupAnimationEvents(brain, animState, () => 
            {
                CleanupAndTransition(brain, brain.DefaultState);
            });
        }

        /// <summary>
        /// 순차적 콤보 공격 실행
        /// ComboAnimationKeys 배열을 기반으로 콤보를 진행합니다.
        /// </summary>
        /// <param name="brain">캐릭터 브레인</param>
        private void ExecuteComboAttack(CharacterBrain brain)
        {
            // 콤보 인덱스 및 마지막 공격 시간 가져오기
            int comboIndex = brain.GetData<int>(COMBO_INDEX_KEY, 0);
            float lastAttackTime = brain.GetData<float>(LAST_ATTACK_TIME_KEY, 0f);

            // 콤보 리셋 조건 체크
            if (Time.time - lastAttackTime > ComboResetTime || comboIndex >= ComboAnimationKeys.Length)
            {
                comboIndex = 0;
            }
            
            // 애니메이션 키 가져오기
            if (comboIndex >= ComboAnimationKeys.Length)
            {
                Debug.LogError($"[SkillActionState] 콤보 인덱스 {comboIndex}가 범위를 벗어났습니다.");
                brain.SetData(COMBO_INDEX_KEY, 0);
                CleanupAndTransition(brain, brain.DefaultState);
                return;
            }
            
            string animKey = ComboAnimationKeys[comboIndex];
            ClipTransition animClip = brain.AnimData.GetClipTransition(animKey);
            
            if (animClip.Clip == null)
            {
                Debug.LogError($"[SkillActionState] 콤보 애니메이션 '{animKey}'을(를) 찾을 수 없습니다.");
                CleanupAndTransition(brain, brain.DefaultState);
                return;
            }

            // 애니메이션 재생
            var animState = brain.Animancer.Play(animClip, fadeDuration);
            
            // 콤보 인덱스 및 시간 업데이트
            brain.SetData(COMBO_INDEX_KEY, comboIndex + 1);
            brain.SetData(LAST_ATTACK_TIME_KEY, Time.time);
            
            // 히트박스 이벤트 바인딩
            SetupAnimationEvents(brain, animState, () => 
            {
                brain.SetHitBox(false);
                brain.ChangeState(brain.DefaultState);
            });
        }

        /// <summary>
        /// 애니메이션 이벤트 설정 (히트박스 제어 포함)
        /// </summary>
        /// <param name="brain">캐릭터 브레인</param>
        /// <param name="animState">애니메이션 상태</param>
        /// <param name="onEndCallback">애니메이션 종료 시 실행할 콜백</param>
        private void SetupAnimationEvents(CharacterBrain brain, AnimancerState animState, System.Action onEndCallback)
        {
            if (!animState.Events(brain, out AnimancerEvent.Sequence events))
                return;
                
            events.Clear();
            
            // 히트박스 사용 시 타이밍 이벤트 추가
            if (UseHitBox)
            {
                events.Add(HitStart, () => brain.SetHitBox(true));
                events.Add(HitEnd, () => brain.SetHitBox(false));
            }
            
            // 애니메이션 종료 이벤트
            events.OnEnd = () => 
            {
                if (UseHitBox)
                {
                    brain.SetHitBox(false); // 안전 장치
                }
                onEndCallback?.Invoke();
            };
        }

        /// <summary>
        /// 블랙보드 정리 후 상태 전환
        /// </summary>
        /// <param name="brain">캐릭터 브레인</param>
        /// <param name="targetState">전환할 대상 상태</param>
        private void CleanupAndTransition(CharacterBrain brain, StateSO targetState)
        {
            brain.SetData(SKILL_INDEX_KEY, 0);
            brain.SetData(CHARGE_RATIO_KEY, 0f);
            brain.ChangeState(targetState);
        }
    }
}
