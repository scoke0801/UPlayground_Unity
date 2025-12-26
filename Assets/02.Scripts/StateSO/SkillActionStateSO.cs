using UnityEngine;
using Animancer;
using Game.Skills;
using Game.Data;
using UnityEngine.InputSystem;

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
    /// - ComboID가 설정되어 있으면 Json 데이터에서 콤보 정보를 로드
    /// - 일반 공격(마우스 클릭)에 사용
    /// - 콤보 타이밍 내 추가 입력 시 다음 공격으로 연계
    /// 
    /// <para><b>스킬 체인 모드:</b></para>
    /// - SkillChainData를 통해 입력에 따라 다음 스킬로 분기
    /// - 예: X→X→X 또는 X→Y→X 등 다양한 조합
    /// - inputWindow 내에서만 입력 감지
    /// 
    /// <para><b>사용 예시:</b></para>
    /// <code>
    /// // 콤보 공격 설정:
    /// ComboID = 1001  // Json 데이터에서 로드
    /// 
    /// // 스킬 체인 실행:
    /// brain.SetData("CurrentSkillID", 2001);  // 시작 스킬 ID
    /// brain.ChangeState(skillActionState);
    /// </code>
    /// </summary>
    [CreateAssetMenu(fileName = "State_SkillAction", menuName = "UP/FSM/States/Skill Action")]
    public class SkillActionStateSO : StateSO
    {
        // [Header("Animation Settings")]
        // [SerializeField] private float fadeDuration = 0.15f;
        //
        // [Header("Combo Settings (Optional)")]
        // [Tooltip("콤보 ID (Json 데이터에서 로드)\n0이면 콤보 모드 비활성화")]
        // public int ComboID = 0;
        //
        // [Header("Input Settings")]
        // [Tooltip("스킬 체인에 사용할 입력 액션")]
        // public InputActionReference attackXAction;
        // public InputActionReference attackYAction;
        //
        // // 블랙보드 키 상수
        // private const string SKILL_INDEX_KEY = "CurrentSkillIndex";
        // private const string SKILL_ID_KEY = "CurrentSkillID";
        // private const string CHARGE_RATIO_KEY = "ChargeRatio"; 
        // private const string COMBO_INDEX_KEY = "ComboIndex";
        // private const string LAST_ATTACK_TIME_KEY = "LastAttackTime";
        // private const string CHAIN_INPUT_RECEIVED_KEY = "ChainInputReceived";
        // private const string NEXT_SKILL_ID_KEY = "NextSkillID";
        //
        // private bool isInInputWindow = false;
        // private string pendingInput = null;
        //
        // public override void OnEnter(CharacterBrain brain)
        // {
        //     // 상태 초기화
        //     isInInputWindow = false;
        //     pendingInput = null;
        //     brain.SetData(CHAIN_INPUT_RECEIVED_KEY, false);
        //     brain.SetData(NEXT_SKILL_ID_KEY, 0);
        //     
        //     // 1. 스킬 체인 모드 체크 (CurrentSkillID가 설정된 경우)
        //     int skillID = brain.GetData<int>(SKILL_ID_KEY, 0);
        //     if (skillID > 0)
        //     {
        //         ExecuteSkillChain(brain, skillID);
        //         return;
        //     }
        //     
        //     // 2. 스킬 슬롯 기반 실행 시도
        //     int slotIndex = brain.GetData<int>(SKILL_INDEX_KEY, 0);
        //     if (slotIndex > 0)
        //     {
        //         ExecuteSkillAction(brain, slotIndex);
        //         return;
        //     }
        //     
        //     // 3. 콤보 공격 경로
        //     if (ComboID > 0)
        //     {
        //         ExecuteComboAttack(brain);
        //         return;
        //     }
        //     
        //     Debug.LogWarning("[SkillActionState] 실행 가능한 액션이 없습니다.");
        //     CleanupAndTransition(brain, brain.DefaultState);
        // }
        //
        // public override void OnUpdate(CharacterBrain brain)
        // {
        //     // 입력 윈도우 내에서 입력 감지
        //     if (isInInputWindow && !string.IsNullOrEmpty(pendingInput))
        //     {
        //         int currentSkillID = brain.GetData<int>(SKILL_ID_KEY, 0);
        //         if (currentSkillID > 0)
        //         {
        //             SkillChainData chainData = JsonDataManager.Instance.GetData<SkillChainData>(currentSkillID);
        //             if (chainData != null && chainData.TryGetNextSkill(pendingInput, out int nextSkillID))
        //             {
        //                 brain.SetData(CHAIN_INPUT_RECEIVED_KEY, true);
        //                 brain.SetData(NEXT_SKILL_ID_KEY, nextSkillID);
        //                 Debug.Log($"[SkillChain] 입력 감지: {pendingInput} → 다음 스킬 ID: {nextSkillID}");
        //             }
        //         }
        //         
        //         pendingInput = null;
        //     }
        //     
        //     // 입력 감지
        //     if (isInInputWindow)
        //     {
        //         if (attackXAction != null && attackXAction.action.WasPressedThisFrame())
        //         {
        //             pendingInput = "X";
        //         }
        //         else if (attackYAction != null && attackYAction.action.WasPressedThisFrame())
        //         {
        //             pendingInput = "Y";
        //         }
        //     }
        // }
        //
        // public override void OnExit(CharacterBrain brain)
        // {
        //     // 히트박스 안전 종료
        //     brain.SetHitBox(false);
        //     isInInputWindow = false;
        //     pendingInput = null;
        // }
        //
        // /// <summary>
        // /// 스킬 체인 실행 (입력에 따라 분기)
        // /// </summary>
        // private void ExecuteSkillChain(CharacterBrain brain, int skillID)
        // {
        //     SkillJsonData skillData = JsonDataManager.Instance.GetData<SkillJsonData>(skillID);
        //     if (skillData == null)
        //     {
        //         Debug.LogError($"[SkillActionState] 스킬 데이터를 찾을 수 없습니다. (SkillID: {skillID})");
        //         CleanupAndTransition(brain, brain.DefaultState);
        //         return;
        //     }
        //     
        //     string animKey = skillData.ActionAnimKey;
        //     ITransition animClip = brain.AnimData.GetAnimation(animKey);
        //     
        //     if (animClip == null)
        //     {
        //         Debug.LogError($"[SkillActionState] 애니메이션 클립 '{animKey}'을(를) 찾을 수 없습니다.");
        //         CleanupAndTransition(brain, brain.DefaultState);
        //         return;
        //     }
        //
        //     // 애니메이션 재생
        //     var animState = brain.Animancer.Play(animClip, fadeDuration);
        //     
        //     // 이펙트/사운드 재생
        //     PlaySkillEffects(brain, skillData);
        //     
        //     // 스킬 체인 데이터 확인
        //     SkillChainData chainData = JsonDataManager.Instance.GetData<SkillChainData>(skillID);
        //     
        //     if (chainData != null && chainData.IsChainable())
        //     {
        //         // 입력 윈도우 설정
        //         SetupChainInputWindow(brain, animState, chainData);
        //     }
        //     
        //     // 히트박스 이벤트 바인딩 (기본값 사용)
        //     SetupAnimationEvents(brain, animState, 0.3f, 0.6f, true, () => 
        //     {
        //         OnSkillChainComplete(brain);
        //     });
        // }
        //
        // /// <summary>
        // /// 스킬 체인 완료 처리
        // /// </summary>
        // private void OnSkillChainComplete(CharacterBrain brain)
        // {
        //     bool hasNextSkill = brain.GetData<bool>(CHAIN_INPUT_RECEIVED_KEY, false);
        //     int nextSkillID = brain.GetData<int>(NEXT_SKILL_ID_KEY, 0);
        //     
        //     if (hasNextSkill && nextSkillID > 0)
        //     {
        //         // 다음 스킬로 연계
        //         brain.SetData(SKILL_ID_KEY, nextSkillID);
        //         brain.SetData(CHAIN_INPUT_RECEIVED_KEY, false);
        //         brain.SetData(NEXT_SKILL_ID_KEY, 0);
        //         brain.ChangeState(this); // 같은 상태로 재진입
        //     }
        //     else
        //     {
        //         // 체인 종료
        //         CleanupAndTransition(brain, brain.DefaultState);
        //     }
        // }
        //
        // /// <summary>
        // /// 체인 입력 윈도우 설정
        // /// </summary>
        // private void SetupChainInputWindow(CharacterBrain brain, AnimancerState animState, SkillChainData chainData)
        // {
        //     if (!animState.Events(brain, out AnimancerEvent.Sequence events))
        //         return;
        //     
        //     // 입력 윈도우 시작 이벤트
        //     events.Add(chainData.InputWindowStart, () => 
        //     {
        //         isInInputWindow = true;
        //         Debug.Log($"[SkillChain] 입력 윈도우 시작 ({chainData.InputWindowStart * 100:F0}%)");
        //     });
        //     
        //     // 입력 윈도우 종료 이벤트
        //     events.Add(chainData.InputWindowEnd, () => 
        //     {
        //         isInInputWindow = false;
        //         Debug.Log($"[SkillChain] 입력 윈도우 종료 ({chainData.InputWindowEnd * 100:F0}%)");
        //     });
        // }
        //
        // /// <summary>
        // /// 스킬 시스템을 통한 스킬 실행
        // /// 블랙보드의 CurrentSkillIndex를 기반으로 스킬을 실행합니다.
        // /// </summary>
        // private void ExecuteSkillAction(CharacterBrain brain, int slotIndex)
        // {
        //     float chargeRatio = brain.GetData<float>(CHARGE_RATIO_KEY, 0f);
        //     
        //     SkillSystem skillSystem = brain.GetComponent<SkillSystem>();
        //     if (skillSystem == null)
        //     {
        //         Debug.LogError("[SkillActionState] SkillSystem 컴포넌트를 찾을 수 없습니다.");
        //         CleanupAndTransition(brain, brain.DefaultState);
        //         return;
        //     }
        //
        //     SkillSlot skillSlot = skillSystem.GetSkillSlot(slotIndex);
        //     if (skillSlot == null || !skillSlot.HasSkill)
        //     {
        //         Debug.LogError($"[SkillActionState] 슬롯 {slotIndex}에 스킬이 없습니다.");
        //         CleanupAndTransition(brain, brain.DefaultState);
        //         return;
        //     }
        //     
        //     string animKey = skillSlot.ActionAnimKey;
        //     ClipTransition animClip = brain.AnimData.GetClipTransition(animKey);
        //     
        //     if (animClip.Clip == null)
        //     {
        //         Debug.LogError($"[SkillActionState] 애니메이션 클립 '{animKey}'을(를) 찾을 수 없습니다.");
        //         CleanupAndTransition(brain, brain.DefaultState);
        //         return;
        //     }
        //
        //     // 애니메이션 재생
        //     var animState = brain.Animancer.Play(animClip, fadeDuration);
        //
        //     // 스킬 실행 (이펙트, 사운드, 쿨다운)
        //     skillSystem.ExecuteSkillAction(slotIndex, chargeRatio);
        //     
        //     // 히트박스 이벤트 바인딩
        //     SetupAnimationEvents(brain, animState, 0.3f, 0.6f, true, () => 
        //     {
        //         CleanupAndTransition(brain, brain.DefaultState);
        //     });
        // }
        //
        // /// <summary>
        // /// 순차적 콤보 공격 실행
        // /// </summary>
        // private void ExecuteComboAttack(CharacterBrain brain)
        // {
        //     ComboJsonData comboData = JsonDataManager.Instance.GetData<ComboJsonData>(ComboID);
        //     if (comboData == null || !comboData.IsValid())
        //     {
        //         Debug.LogError($"[SkillActionState] 콤보 데이터를 찾을 수 없거나 유효하지 않습니다. (ComboID: {ComboID})");
        //         CleanupAndTransition(brain, brain.DefaultState);
        //         return;
        //     }
        //     
        //     int comboIndex = brain.GetData<int>(COMBO_INDEX_KEY, 0);
        //     float lastAttackTime = brain.GetData<float>(LAST_ATTACK_TIME_KEY, 0f);
        //
        //     if (Time.time - lastAttackTime > comboData.ComboResetTime || comboIndex >= comboData.AnimationKeys.Length)
        //     {
        //         comboIndex = 0;
        //     }
        //     
        //     if (comboIndex >= comboData.AnimationKeys.Length)
        //     {
        //         Debug.LogError($"[SkillActionState] 콤보 인덱스 {comboIndex}가 범위를 벗어났습니다.");
        //         brain.SetData(COMBO_INDEX_KEY, 0);
        //         CleanupAndTransition(brain, brain.DefaultState);
        //         return;
        //     }
        //     
        //     string animKey = comboData.AnimationKeys[comboIndex];
        //     ClipTransition animClip = brain.AnimData.GetClipTransition(animKey);
        //     
        //     if (animClip.Clip == null)
        //     {
        //         Debug.LogError($"[SkillActionState] 콤보 애니메이션 '{animKey}'을(를) 찾을 수 없습니다.");
        //         CleanupAndTransition(brain, brain.DefaultState);
        //         return;
        //     }
        //
        //     var animState = brain.Animancer.Play(animClip, fadeDuration);
        //     
        //     brain.SetData(COMBO_INDEX_KEY, comboIndex + 1);
        //     brain.SetData(LAST_ATTACK_TIME_KEY, Time.time);
        //     
        //     comboData.GetHitTiming(comboIndex, out float hitStart, out float hitEnd);
        //     
        //     SetupAnimationEvents(brain, animState, hitStart, hitEnd, comboData.UseHitBox, () => 
        //     {
        //         brain.SetHitBox(false);
        //         brain.ChangeState(brain.DefaultState);
        //     });
        // }
        //
        // /// <summary>
        // /// 스킬 이펙트/사운드 재생
        // /// </summary>
        // private void PlaySkillEffects(CharacterBrain brain, SkillJsonData skillData)
        // {
        //     // 이펙트 재생
        //     if (skillData.SkillEffectPrefab != null)
        //     {
        //         GameObject effect = GameObject.Instantiate(skillData.SkillEffectPrefab, brain.transform.position, Quaternion.identity);
        //         GameObject.Destroy(effect, 3f);
        //     }
        //     
        //     // 사운드 재생 (SoundManager가 있다면)
        //     if (skillData.SkillSound != null)
        //     {
        //         // TODO: SoundManager.Instance.PlaySFX(skillData.SkillSound);
        //     }
        // }
        //
        // /// <summary>
        // /// 애니메이션 이벤트 설정 (히트박스 제어 포함)
        // /// </summary>
        // private void SetupAnimationEvents(CharacterBrain brain, AnimancerState animState, 
        //     float hitStart, float hitEnd, bool useHitBox, System.Action onEndCallback)
        // {
        //     if (!animState.Events(brain, out AnimancerEvent.Sequence events))
        //         return;
        //         
        //     events.Clear();
        //     
        //     if (useHitBox)
        //     {
        //         events.Add(hitStart, () => brain.SetHitBox(true));
        //         events.Add(hitEnd, () => brain.SetHitBox(false));
        //     }
        //     
        //     events.OnEnd = () => 
        //     {
        //         if (useHitBox)
        //         {
        //             brain.SetHitBox(false);
        //         }
        //         onEndCallback?.Invoke();
        //     };
        // }
        //
        // /// <summary>
        // /// 블랙보드 정리 후 상태 전환
        // /// </summary>
        // private void CleanupAndTransition(CharacterBrain brain, StateSO targetState)
        // {
        //     brain.SetData(SKILL_INDEX_KEY, 0);
        //     brain.SetData(SKILL_ID_KEY, 0);
        //     brain.SetData(CHARGE_RATIO_KEY, 0f);
        //     brain.SetData(CHAIN_INPUT_RECEIVED_KEY, false);
        //     brain.SetData(NEXT_SKILL_ID_KEY, 0);
        //     brain.ChangeState(targetState);
        // }
    }
}
