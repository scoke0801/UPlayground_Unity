using UnityEngine;
using Animancer;
using Core.Animation;
using System.Collections.Generic;

namespace Characters
{
    /// <summary>
    /// 언리얼 스타일의 Animation Sequence와 Montage를 사용하는
    /// 고급 캐릭터 애니메이션 컨트롤러
    /// </summary>
    [RequireComponent(typeof(AnimancerComponent))]
    [RequireComponent(typeof(AnimationMontagePlayer))]
    public class AdvancedCharacterAnimationController : MonoBehaviour
    {
        [Header("Animation Sequences - 기본 이동")]
        [SerializeField] private AnimationSequence idleSequence;
        [SerializeField] private AnimationSequence walkSequence;
        [SerializeField] private AnimationSequence runSequence;
        
        [Header("Animation Montages - 액션")]
        [SerializeField] private AnimationMontage attackMontage;
        [SerializeField] private AnimationMontage jumpMontage;
        [SerializeField] private AnimationMontage skillMontage;
        [SerializeField] private AnimationMontage hurtMontage;
        [SerializeField] private AnimationMontage deathMontage;
        
        [Header("Blend Settings")]
        [SerializeField] private float movementBlendSpeed = 5f;
        [SerializeField] private LinearMixerTransition movementMixer;
        
        [Header("Layers")]
        [SerializeField] private AvatarMask upperBodyMask;
        [SerializeField] private AvatarMask lowerBodyMask;

        private AnimancerComponent animancer;
        private AnimationMontagePlayer montagePlayer;
        private AnimancerState currentMovementState;
        private float currentMoveSpeed;
        
        // 애니메이션 상태
        public enum CharacterState
        {
            Idle,
            Moving,
            Jumping,
            Attacking,
            UsingSkill,
            Hurt,
            Dead
        }
        
        private CharacterState currentState = CharacterState.Idle;

        private void Awake()
        {
            animancer = GetComponent<AnimancerComponent>();
            montagePlayer = GetComponent<AnimationMontagePlayer>();
            
            // 이벤트 구독
            montagePlayer.OnMontageEnded += OnMontageCompleted;
            montagePlayer.OnMontageEvent += OnMontageEventTriggered;
        }

        private void OnDestroy()
        {
            if (montagePlayer != null)
            {
                montagePlayer.OnMontageEnded -= OnMontageCompleted;
                montagePlayer.OnMontageEvent -= OnMontageEventTriggered;
            }
        }

        private void Start()
        {
            SetupMovementMixer();
            PlayIdle();
        }

        #region Movement Animations (Sequences)

        /// <summary>
        /// 이동 믹서 설정 - 블렌드 트리처럼 동작
        /// </summary>
        private void SetupMovementMixer()
        {
            if (movementMixer == null) return;

            // 믹서에 애니메이션 추가 (속도 기반 블렌딩)
            // 0 = Idle, 0.5 = Walk, 1 = Run
        }

        /// <summary>
        /// Idle 애니메이션 재생
        /// </summary>
        public void PlayIdle()
        {
            if (idleSequence?.Clip == null) return;
            if (currentState == CharacterState.Dead) return;

            currentMovementState = animancer.Play(idleSequence.Clip, 0.25f);
            currentState = CharacterState.Idle;
            currentMoveSpeed = 0f;
        }

        /// <summary>
        /// 이동 속도에 따른 애니메이션 재생
        /// </summary>
        public void UpdateMovement(float speed, bool isRunning)
        {
            if (currentState == CharacterState.Dead) return;
            if (currentState == CharacterState.Attacking || 
                currentState == CharacterState.UsingSkill) return;

            if (speed <= 0.1f)
            {
                PlayIdle();
                return;
            }

            currentMoveSpeed = speed;
            currentState = CharacterState.Moving;

            // 속도에 따라 Walk/Run 선택
            var targetSequence = isRunning ? runSequence : walkSequence;
            if (targetSequence?.Clip == null) return;

            if (currentMovementState == null || 
                currentMovementState.Clip != targetSequence.Clip)
            {
                currentMovementState = animancer.Play(
                    targetSequence.Clip, 
                    0.25f
                );
            }

            // 속도에 따른 재생 속도 조정
            if (currentMovementState != null)
            {
                currentMovementState.Speed = targetSequence.PlaybackSpeed;
            }
        }

        #endregion

        #region Action Animations (Montages)

        /// <summary>
        /// 공격 Montage 재생
        /// </summary>
        public void PlayAttack(string comboSection = null)
        {
            if (attackMontage == null) return;
            if (currentState == CharacterState.Dead) return;

            currentState = CharacterState.Attacking;
            montagePlayer.PlayMontage(attackMontage, comboSection);
        }

        /// <summary>
        /// 점프 Montage 재생
        /// </summary>
        public void PlayJump()
        {
            if (jumpMontage == null) return;
            if (currentState == CharacterState.Dead) return;

            currentState = CharacterState.Jumping;
            montagePlayer.PlayMontage(jumpMontage);
        }

        /// <summary>
        /// 스킬 Montage 재생
        /// </summary>
        public void PlaySkill(string skillSection = null)
        {
            if (skillMontage == null) return;
            if (currentState == CharacterState.Dead) return;

            currentState = CharacterState.UsingSkill;
            montagePlayer.PlayMontage(skillMontage, skillSection);
        }

        /// <summary>
        /// 피격 Montage 재생
        /// </summary>
        public void PlayHurt()
        {
            if (hurtMontage == null) return;
            if (currentState == CharacterState.Dead) return;

            currentState = CharacterState.Hurt;
            montagePlayer.PlayMontage(hurtMontage);
        }

        /// <summary>
        /// 사망 Montage 재생
        /// </summary>
        public void PlayDeath()
        {
            if (deathMontage == null) return;

            currentState = CharacterState.Dead;
            montagePlayer.PlayMontage(deathMontage);
        }

        /// <summary>
        /// 현재 Montage 중지
        /// </summary>
        public void StopCurrentMontage()
        {
            montagePlayer.StopMontage();
        }

        /// <summary>
        /// Montage 섹션으로 점프
        /// </summary>
        public void JumpToMontageSection(string sectionName)
        {
            montagePlayer.JumpToSection(sectionName);
        }

        #endregion

        #region Multi-Layer Animation

        /// <summary>
        /// 상체 애니메이션 재생 (레이어 사용)
        /// 예: 이동하면서 상체만 공격 애니메이션
        /// </summary>
        public void PlayUpperBodyAnimation(AnimationClip clip, AvatarMask mask = null, float fadeTime = 0.25f)
        {
            if (clip == null) return;

            // Layer 1에 접근하면 자동으로 생성됨
            var layer = animancer.Layers[1];
            
            // 마스크 설정 (한 번만)
            if (mask != null && layer.Mask == null)
            {
                layer.Mask = mask;
            }

            layer.Play(clip, fadeTime);
        }

        #endregion

        #region Event Callbacks

        /// <summary>
        /// Montage 재생 완료 콜백
        /// </summary>
        private void OnMontageCompleted(AnimationMontage montage)
        {
            Debug.Log($"Montage 완료: {montage?.MontageName}");

            // Montage 완료 후 Idle로 복귀
            if (currentState != CharacterState.Dead)
            {
                PlayIdle();
            }
        }

        /// <summary>
        /// Montage 이벤트 콜백
        /// </summary>
        private void OnMontageEventTriggered(string eventName, string parameter)
        {
            Debug.Log($"Montage 이벤트: {eventName}, 파라미터: {parameter}");

            // 이벤트 처리
            switch (eventName)
            {
                case "AttackHit":
                    // 공격 히트 처리
                    OnAttackHit();
                    break;
                case "PlaySound":
                    // 사운드 재생
                    PlayEventSound(parameter);
                    break;
                case "SpawnEffect":
                    // 이펙트 생성
                    SpawnEventEffect(parameter);
                    break;
            }
        }

        private void OnAttackHit()
        {
            // 실제 공격 판정 로직
            Debug.Log("공격 히트!");
        }

        private void PlayEventSound(string soundName)
        {
            // 사운드 재생 로직
            Debug.Log($"사운드 재생: {soundName}");
        }

        private void SpawnEventEffect(string effectName)
        {
            // 이펙트 생성 로직
            Debug.Log($"이펙트 생성: {effectName}");
        }

        #endregion

        #region State Queries

        /// <summary>
        /// 현재 애니메이션 상태
        /// </summary>
        public CharacterState GetCurrentState()
        {
            return currentState;
        }

        /// <summary>
        /// 애니메이션 재생 중인지 확인
        /// </summary>
        public bool IsPlayingMontage()
        {
            return montagePlayer.IsPlaying();
        }

        /// <summary>
        /// 이동 가능한 상태인지 확인
        /// </summary>
        public bool CanMove()
        {
            return currentState != CharacterState.Attacking &&
                   currentState != CharacterState.UsingSkill &&
                   currentState != CharacterState.Dead;
        }

        /// <summary>
        /// 공격 가능한 상태인지 확인
        /// </summary>
        public bool CanAttack()
        {
            return currentState != CharacterState.Attacking &&
                   currentState != CharacterState.Dead;
        }

        #endregion

        #region Debug/Test Input

#if UNITY_EDITOR
        private void Update()
        {
            TestInput();
        }

        private void TestInput()
        {
            // 이동
            if (Input.GetKey(KeyCode.W))
            {
                UpdateMovement(1f, Input.GetKey(KeyCode.LeftShift));
            }
            else
            {
                UpdateMovement(0f, false);
            }

            // 액션
            if (Input.GetKeyDown(KeyCode.Alpha1))
                PlayAttack();
            
            if (Input.GetKeyDown(KeyCode.Alpha2))
                PlaySkill();
            
            if (Input.GetKeyDown(KeyCode.Space))
                PlayJump();
            
            if (Input.GetKeyDown(KeyCode.H))
                PlayHurt();
            
            if (Input.GetKeyDown(KeyCode.K))
                PlayDeath();
        }
#endif

        #endregion
    }
}
