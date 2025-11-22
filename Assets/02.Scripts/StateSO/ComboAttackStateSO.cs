using UnityEngine;
using Animancer;

namespace Game.FSM
{
    [CreateAssetMenu(fileName = "State_ComboAttack", menuName = "FSM/States/Combo Attack")]
    public class ComboAttackStateSO : StateSO
    {
        [Header("Combo Settings")]
        public string[] ComboAnimationKeys;
        
        [Header("Timing Settings")]
        [Range(0, 1)] public float HitStart = 0.3f; // 공격 판정 시작 (30% 지점)
        [Range(0, 1)] public float HitEnd = 0.6f;   // 공격 판정 끝 (60% 지점)
        public float ComboResetTime = 2.0f;         // 콤보 유지 시간

        public override void OnEnter(CharacterBrain brain)
        {
            // 1. 블랙보드에서 데이터 가져오기
            int comboIndex = brain.GetData<int>("ComboIndex", 0);
            float lastAttackTime = brain.GetData<float>("LastAttackTime", 0f);

            // 2. 콤보 리셋 조건 (시간 초과 or 콤보 끝)
            if (Time.time - lastAttackTime > ComboResetTime || comboIndex >= ComboAnimationKeys.Length)
            {
                comboIndex = 0;
            }
            
            //애니메이션 데이터 가져오기
            if (ComboAnimationKeys == null || ComboAnimationKeys.Length <= comboIndex)
            {
                Debug.LogError("콤보 애니메이션 키 설정이 잘못되었습니다.");
                brain.ChangeState(brain.DefaultState);
                return;
            }
            
            string animKey = ComboAnimationKeys[comboIndex];
            ClipTransition currentAnim = brain.AnimData.GetClipTransition(animKey);
            
            if (currentAnim.Clip == null)
            {
                Debug.LogError($"콤보 인덱스 {comboIndex}의 클립({animKey})이 null입니다.");
                brain.ChangeState(brain.DefaultState);
                return;
            }
            // 3. 애니메이션 재생
            var animState = brain.Animancer.Play(currentAnim);
            
            // 4. 이벤트 바인딩 (히트박스 켜고 끄기)
            if (animState.Events(brain, out AnimancerEvent.Sequence events))
            {
                events.Clear();
                events.Add(HitStart, () => brain.SetHitBox(true));
                events.Add(HitEnd, () => brain.SetHitBox(false));
                
                // 애니메이션 끝나면 기본 상태(Move)로 복귀
                events.OnEnd = () => 
                {
                    brain.SetHitBox(false); // 안전장치
                    brain.ChangeState(brain.DefaultState);
                };
            }

            // 5. 다음 콤보를 위해 데이터 저장
            brain.SetData("ComboIndex", comboIndex + 1);
            brain.SetData("LastAttackTime", Time.time);
        }

        public override void OnExit(CharacterBrain brain)
        {
            // 피격 등으로 상태가 강제로 끊길 경우 히트박스 끄기
            brain.SetHitBox(false);
        }
    }
}