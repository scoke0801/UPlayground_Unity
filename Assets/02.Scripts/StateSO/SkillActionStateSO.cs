using UnityEngine;
using Animancer;

namespace Game.FSM
{
    [CreateAssetMenu(fileName = "State_SkillAction", menuName = "FSM/States/Skill Action")]
    public class SkillActionStateSO : StateSO
    {
        [Header("Animation")]
        [Tooltip("CharacterAnimationData에 등록된 클립 이름")]
        public string AnimationKey;
        public float FadeDuration = 0.2f;

        [Header("Settings")]
        public bool CanRotate = false; // 스킬 시전 중 회전 가능 여부
        
        public override void OnEnter(CharacterBrain brain)
        {
            // 1. 애니메이션 찾기
            ClipTransition clip = brain.AnimData.GetClipTransition(AnimationKey);
            
            // 안전장치: 클립이 없으면 즉시 기본 상태로 복귀
            if (clip.Clip == null)
            {
                Debug.LogError($"[SkillAction] '{AnimationKey}' 키에 해당하는 클립을 찾을 수 없습니다.");
                brain.ChangeState(brain.DefaultState);
                return;
            }

            // 2. 애니메이션 재생
            var state = brain.Animancer.Play(clip, FadeDuration);

            // 3. 종료 이벤트 바인딩 (애니메이션 끝나면 복귀)
            // Animancer Event를 사용하여 정확한 종료 시점 캐치
            if (state.Events(brain, out AnimancerEvent.Sequence events))
            {
                events.OnEnd = () => 
                {
                    brain.ChangeState(brain.DefaultState);
                };
            }
            
            // 4. (옵션) 이펙트나 사운드 처리는 여기서 하거나 Animation Event로 처리
        }

        public override void OnUpdate(CharacterBrain brain)
        {
            // 스킬 시전 중 방향 전환이 가능한 경우 (예: 이동 사격)
            if (CanRotate && brain.InputDirection.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(brain.InputDirection);
                brain.transform.rotation = Quaternion.Slerp(brain.transform.rotation, targetRot, Time.deltaTime * 10f);
            }
        }
        
        public override void OnExit(CharacterBrain brain)
        {
            // 상태 종료 시 히트박스 해제 등 정리 작업
            brain.SetHitBox(false);
        }
    }
}