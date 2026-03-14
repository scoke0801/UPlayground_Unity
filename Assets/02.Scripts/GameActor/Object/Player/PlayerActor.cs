using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UPlayGround.Data.EnumType;
using UPlayGround.Animation;
using UPlayGround.Component;
using UPlayGround.Data;
using UPlayGround.Data.Event;
using UPlayGround.MovementController;
using UPlayGround.Input;
using UPlayGround.InputDefine;
using UPlayGround.Manager;
using UPlayGround.Manager.Handler;
using UPlayGround.State;
using Random = UnityEngine.Random;

namespace UPlayGround
{
    public partial class PlayerActor : GameActor, IDamageable
    {
        [SerializeField] private float _interactionRadius;
        [SerializeField] private LayerMask _interactionLayer;
        
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private float _currentHealth = 100f;
        [SerializeField] private bool _isInvincible = false;

        // 추가 컴포넌트
        [SerializeField] private PlayerEquipment _equipment;
        [SerializeField] private PlayerCombat _combat;
        [SerializeField] private PlayerSkillGauge _skillGauge;

        public event Action<float, float> OnHpChanged;
        public event Action<float, float> OnSkillGaugeChanged;
        
        protected PlayerMovementController PlayerMovementPlayerController;
        
        private Camera _camera;
        private PlayerActorAnimator _playerActorAnimator;
        
        private Vector2 _currentMoveInput;
        private InputCondition _jumpInputCondition;
        private InputCondition _crouchInputCondition;
        private InputCondition _dodgeInputCondition;
        private InputCondition _dashInputCondition;
        
        private InputCondition _attackInputCondition;
        private InputCondition _heavyInputCondition;
        
        private InputCondition _equipInputCondition;
        
        private InputCondition _interactionInputCondition;
        
        private InputCondition _guardInputCondition;
        
        private List<InputCondition> _skillInputCondition = new List<InputCondition> 
        { 
            InputCondition.None,    // 0
            InputCondition.None,    // 1
            InputCondition.None,    // 2
            InputCondition.None,    // 3
            InputCondition.None,    // 4
            InputCondition.None,    // 5
            InputCondition.None,    // 6
            InputCondition.None,    // 7
            InputCondition.None,    // 8
            InputCondition.None,    // 9
        };
        
        public override ActorAnimator Animator => _playerActorAnimator;
        public PlayerMovementController PlayerController => PlayerMovementPlayerController;
        
        public float InteractionRadius => _interactionRadius;
        public LayerMask InteractionLayer => _interactionLayer;

        public bool IsEquippedRightWeapon => _equipment.IsMainWeaponEquipped;
        public bool IsEquippedLeftWeapon => _equipment.IsSubWeaponEquipped;

        public bool IsInCombat => _combat?.IsInCombat ?? false;
        
        public float MaxHealth => _maxHealth;
        public float CurrentHealth => _currentHealth;
        
        public PlayerSkillGauge SkillGauge => _skillGauge;
    }
    /// <summary>
    /// 
    /// </summary>
    public partial class PlayerActor : GameActor, IDamageable
    {
        #region Mono
        protected override void Awake()
        {
            base.Awake();

            _actorType = ActorType.Player | ActorType.Combat;
            _camera = Camera.main;
            PlayerMovementPlayerController = MovementController as PlayerMovementController;

            _playerActorAnimator = _animator as PlayerActorAnimator;
            
            InitComponents();
            
            RegisterInputEvents();

            switch (_characterActorType)
            {
                case CharacterActorType.Bokusei:
                    _equipment?.SetWeaponType(WeaponType.Katana);
                    break;
                
                case CharacterActorType.Honoka:
                    _equipment?.SetWeaponType(WeaponType.DoubleAxe);
                    break;
                
                default:
                    break;
            }
        }

        private void OnDestroy()
        {
            UnRegisterInputEvents();
            
            CameraManager.Instance?.SetCombatStateProvider(null);
        }
        
        private void Update()
        {
            if (MovementController == null) return;

            // CameraManager를 통해 현재 메인 카메라의 회전값을 가져옴
            Quaternion cameraRotation = Quaternion.identity;
            if (_camera != null)
            {
                cameraRotation = _camera.transform.rotation;
            }

            PlayerCharacterInputs characterInputs = new PlayerCharacterInputs
            {
                // Build the CharacterInputs struct
                MoveInput = _currentMoveInput,
                CameraRotation = cameraRotation,
                CrouchInput = _crouchInputCondition,
                
                JumpInput = _jumpInputCondition,
                DodgeInput =  _dodgeInputCondition,
                
                AttackInput =  _attackInputCondition,
                HeavyAttackInput =  _heavyInputCondition,
                
                EquipInput = _equipInputCondition,
                InteractInput = _interactionInputCondition,
                GuardInput = _guardInputCondition,
                DashInput =  _dashInputCondition,
                
                SkillInput =  new List<InputCondition>()
                {
                    _skillInputCondition[0],
                    _skillInputCondition[1],
                    _skillInputCondition[2],
                    _skillInputCondition[3],
                    _skillInputCondition[4],
                    _skillInputCondition[5],
                    _skillInputCondition[6],
                    _skillInputCondition[7],
                    _skillInputCondition[8],
                    _skillInputCondition[9],
                },
            };

            // 이동 입력과 카메라 회전값을 함께 전달
            PlayerMovementPlayerController.SetInputs(characterInputs);
            
            // 전달 후 요청 초기화 (한 프레임만 유효)
            // [TODO] 어느정도 입력 버퍼 시간이 필요하다면... 바로 초기화를 하지 않아야한다.
            //_jumpInputCondition = InputCondition.None;
            _dodgeInputCondition = InputCondition.None;
            _dashInputCondition = InputCondition.None;
            _attackInputCondition = InputCondition.None;
            _heavyInputCondition = InputCondition.None;
            _equipInputCondition = InputCondition.None;
            _interactionInputCondition = InputCondition.None;

            for (int i = 0; i < _skillInputCondition.Count; ++i)
            {
                _skillInputCondition[i] = InputCondition.None;
            }
        }
        #endregion
    }

    // Input 처리
    public partial class PlayerActor : GameActor, IDamageable
    {
        private void RegisterInputEvents()
        {
            if (InputManager.Instance)
            {
                InputLayer layer = InputLayer.Level_0;
                InputManager.Instance.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Move,
                    OnInputMove, OnInputMove, OnInputMove, null, OnMoveCanceled, layer);
                
                InputManager.Instance.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Jump,
                    null, OnInputPerformedJump, null, null, null, layer);
                
                InputManager.Instance.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Walk,
                    null, OnInputPerformedWalk, null, null, null, layer);
                
                InputManager.Instance.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Sprint,
                    null, OnInputPerformedSprint, null, null, null, layer);
                
                InputManager.Instance.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Crouching,
                    null, OnInputPerformedCrouching, null, null, null, layer);
                                
                InputManager.Instance.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Dodge,
                    null, OnInputPerformedDodge, null, null, null, layer);
                
                InputManager.Instance.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Dash,
                    null, OnInputPerformedDash, null, null, null, layer);

                InputManager.Instance.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Attack,
                    null, OnInputPerformedAttack, null, null, null, layer);
                                
                InputManager.Instance.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.HeavyAttack,
                    null, OnInputPerformedHeavyAttack, null, null, null, layer);

                InputManager.Instance.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Skill_1,
                    null, OnInputPerformedSkill_1, null, null, null, layer);
              
                InputManager.Instance.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Skill_2,
                    null, OnInputPerformedSkill_2, null, null, null, layer);

                InputManager.Instance.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Skill_3,
                    null, OnInputPerformedSkill_3, null, null, null, layer);

                InputManager.Instance.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Skill_4,
                    null, OnInputPerformedSkill_4, null, null, null, layer);

                InputManager.Instance.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Skill_5,
                    null, OnInputPerformedSkill_5, null, null, null, layer);

                InputManager.Instance.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Skill_6,
                    null, OnInputPerformedSkill_6, null, null, null, layer);

                InputManager.Instance.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Skill_7,
                    null, OnInputPerformedSkill_7, null, null, null, layer);

                InputManager.Instance.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Skill_8,
                    null, OnInputPerformedSkill_8, null, null, null, layer);

                InputManager.Instance.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Skill_9,
                    null, OnInputPerformedSkill_9, null, null, null, layer);

                InputManager.Instance.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Equip,
                    null, OnInputPerformedEquipWeapon, null, null, null, layer);
                
                InputManager.Instance.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Interact,
                    null, OnInputPerformedInteraction, null, CanInputInteract, null, layer);
                
                InputManager.Instance.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Guard,
                    OnInputStartedGuard, null, OnInputFinishedGuard, null, null, layer);
            }
        }

        private void UnRegisterInputEvents()
        {   
            if (InputManager.Instance)
            {
                InputManager.Instance.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Move,
                    OnInputMove, OnInputMove, OnInputMove);
                
                InputManager.Instance.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Jump,
                    null, OnInputPerformedJump, null);
                
                InputManager.Instance.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Walk,
                    null, OnInputPerformedWalk, null);
                
                InputManager.Instance.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Sprint,
                    null, OnInputPerformedSprint, null);
                
                InputManager.Instance.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Crouching,
                    null, OnInputPerformedCrouching, null);
                
                InputManager.Instance.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Dodge,
                    null, OnInputPerformedDodge, null);
                
                InputManager.Instance.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Dash,
                    null, OnInputPerformedDash, null);
                
                InputManager.Instance.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Attack,
                    null, OnInputPerformedAttack, null);
                                
                InputManager.Instance.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.HeavyAttack,
                    null, OnInputPerformedHeavyAttack, null);
                
                InputManager.Instance.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Skill_1,
                    null, OnInputPerformedSkill_1, null);
                
                InputManager.Instance.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Skill_2,
                    null, OnInputPerformedSkill_2, null);
                
                InputManager.Instance.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Skill_3,
                    null, OnInputPerformedSkill_3, null);
                
                InputManager.Instance.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Skill_4,
                    null, OnInputPerformedSkill_4, null);
                
                InputManager.Instance.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Skill_5,
                    null, OnInputPerformedSkill_5, null);
                
                InputManager.Instance.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Skill_6,
                    null, OnInputPerformedSkill_6, null);

                InputManager.Instance.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Skill_7,
                    null, OnInputPerformedSkill_7, null);

                InputManager.Instance.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Skill_8,
                    null, OnInputPerformedSkill_8, null);

                InputManager.Instance.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Skill_9,
                    null, OnInputPerformedSkill_9, null);

                InputManager.Instance.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Equip,
                    null, OnInputPerformedEquipWeapon, null);
                
                InputManager.Instance.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Interact,
                    null, OnInputPerformedInteraction, null);
            }
        }
        
        #region InputCallback
        private void OnInputMove(InputAction.CallbackContext obj)
        {
            _currentMoveInput = obj.ReadValue<Vector2>();
        }
        
        private void OnMoveCanceled()
        {
            _currentMoveInput = Vector2.zero;
            PlayerMovementPlayerController.ClearInputAll();
        }
        
        private void OnInputPerformedJump(InputAction.CallbackContext obj)
        {
            _jumpInputCondition = InputCondition.Pressed;
        }
        
        private void OnInputPerformedCrouching(InputAction.CallbackContext obj)
        {
            _crouchInputCondition = (_crouchInputCondition == InputCondition.Pressed)
                ? InputCondition.None : InputCondition.Pressed;
        }
        
        private void OnInputPerformedDodge(InputAction.CallbackContext obj)
        {
            _dodgeInputCondition = InputCondition.Pressed;
        }
        private void OnInputPerformedDash(InputAction.CallbackContext obj)
        {
            _dashInputCondition = InputCondition.Pressed;
        }
        
        private void OnInputPerformedWalk(InputAction.CallbackContext obj)
        {
            MoveAnimType = MoveAnimType == BaseMoveAnimType.Walk ? BaseMoveAnimType.Run : BaseMoveAnimType.Walk;
        }
        
        private void OnInputPerformedSprint(InputAction.CallbackContext obj)
        {       
            if(MovementController.CurrentState.StateName == "GroundMove")
                MoveAnimType = MoveAnimType == BaseMoveAnimType.Sprint ? BaseMoveAnimType.Run : BaseMoveAnimType.Sprint;
        }
        
        private void OnInputPerformedHeavyAttack(InputAction.CallbackContext obj)
        {
            _heavyInputCondition = InputCondition.Pressed;
        }

        private void OnInputPerformedAttack(InputAction.CallbackContext obj)
        {
            _attackInputCondition = InputCondition.Pressed;
        }
        
        private void OnInputPerformedEquipWeapon(InputAction.CallbackContext obj)
        {
            _equipInputCondition = InputCondition.Pressed;
        }
        
        private void OnInputPerformedSkill_1(InputAction.CallbackContext obj)
        {
            _skillInputCondition[0] = InputCondition.Pressed;
        }
        private void OnInputPerformedSkill_2(InputAction.CallbackContext obj)
        {
            _skillInputCondition[1] = InputCondition.Pressed;
        }
        private void OnInputPerformedSkill_3(InputAction.CallbackContext obj)
        {
            _skillInputCondition[2] = InputCondition.Pressed;
        }
        private void OnInputPerformedSkill_4(InputAction.CallbackContext obj)
        {
            _skillInputCondition[3] = InputCondition.Pressed;
        }
        private void OnInputPerformedSkill_5(InputAction.CallbackContext obj)
        {
            _skillInputCondition[4] = InputCondition.Pressed;
        }
        
        private void OnInputPerformedSkill_6(InputAction.CallbackContext obj)
        {
            _skillInputCondition[5] = InputCondition.Pressed;
        }
        
        private void OnInputPerformedSkill_7(InputAction.CallbackContext obj)
        {
            _skillInputCondition[6] = InputCondition.Pressed;
        }
        
        private void OnInputPerformedSkill_8(InputAction.CallbackContext obj)
        {
            _skillInputCondition[7] = InputCondition.Pressed;
        }
        
        private void OnInputPerformedSkill_9(InputAction.CallbackContext obj)
        {
            _skillInputCondition[8] = InputCondition.Pressed;
        }
        private void OnInputPerformedInteraction(InputAction.CallbackContext obj)
        {
            _interactionInputCondition = InputCondition.Pressed;
        }
        
        private void OnInputStartedGuard(InputAction.CallbackContext obj)
        {
            _guardInputCondition = InputCondition.Pressed;
        }

        private void OnInputFinishedGuard(InputAction.CallbackContext obj)
        {
            _guardInputCondition = InputCondition.None;
        }
        #endregion

        public void ClearCrouchInput()
        {
            _crouchInputCondition = InputCondition.None;
            PlayerMovementPlayerController.ClearCrouchInput();
        }

        public void ClearJumpInput()
        {
            _jumpInputCondition = InputCondition.None;
            PlayerMovementPlayerController.ClearJumpInput();
        }
        
        /// <summary>
        /// Player가 인터렉션 할 수 있는 상태인가?
        /// </summary>
        /// <returns></returns>
        private bool CanInputInteract()
        {
            return GameObjectManager.Instance.CanInteract();
        }

    }

    // Component
    public partial class PlayerActor : GameActor, IDamageable
    {
        public PlayerEquipment GetPlayerEquipment() { return _equipment; }
        public PlayerCombat GetCombat() { return _combat; }

        private void InitComponents()
        {
            if (_combat == null)
                _combat = GetComponent<PlayerCombat>();

            if (_equipment == null)
                _equipment = GetComponent<PlayerEquipment>();

            if (_skillGauge == null)
                _skillGauge = GetComponent<PlayerSkillGauge>();

            if (_skillGauge != null)
                _skillGauge.OnGaugeChanged += (cur, max) => OnSkillGaugeChanged?.Invoke(cur, max);

            // 카메라에 전투 상태 조회 함수 등록 (매 프레임 폴링)
            CameraManager.Instance?.SetCombatStateProvider(() => _combat != null && _combat.IsInCombat);
        }
    }

    // IDamageable
    public partial class PlayerActor : GameActor, IDamageable
    {
        public void TakeDamage(AttackData attackData)
        {
            if (_combat.IsGuarding)
            {
                // Guard State가 처리하도록 위임
                if (MovementController.CurrentState is PlayerGuardState guardState)
                {
                    guardState.OnAttackBlocked(attackData);
                    return; // 데미지 처리 중단
                }
            }
            
            if (!CanTakeDamage())
            {
                Debug.Log($"[PlayerActor] {gameObject.name}는 현재 데미지를 받을 수 없습니다.");
                return;
            }
            
            float finalDamage = attackData.damage;
            
            // 크리티컬 처리
            if (attackData.criticalMultiplier > 1.0f)
            {
                finalDamage *= attackData.criticalMultiplier;
                Debug.Log($"[PlayerActor] 크리티컬 히트! 데미지: {finalDamage}");
            }

            _currentHealth = MathF.Max(0, _currentHealth - finalDamage);
            
            OnHpChanged?.Invoke(_currentHealth, _maxHealth);
            Debug.Log($"[PlayerActor] {gameObject.name}가 {finalDamage} 데미지를 받았습니다! (남은 체력: {_currentHealth}/{_maxHealth})");
            
            // 피격 이펙트, 사운드, 넉백 등 추가 가능
            OnDamaged(attackData);
            
            // 사망 처리
            if (_currentHealth <= 0)
            {
                OnDeath(attackData);
            }
        }

        public bool IsAlive()
        { 
            return _currentHealth > 0;
        }

        public void SetInvincible(bool invincible)
        {
            _isInvincible = invincible;
        }

        public bool CanTakeDamage()
        {
            // if (MovementController.CurrentState.StateName == "Hit")
            //     return false;
            if (MovementController.CurrentState.StateName == "Dodge")
                return false;
            if (MovementController.CurrentState.StateName == "Dash")
                return false;
            if (MovementController.CurrentState.StateName == "FinishAttack")
                return false;
            
            return IsAlive() && !_isInvincible;
        }

        public Transform GetTransform()
        {
            return transform;
        }

        public void LockOn()
        {
        }

        public void UnLockOn()
        {
        }

        public float GetHealthPercent()
        {
            return _currentHealth / _maxHealth;
        }

        public float GetCurrentHealth()
        {
            return _currentHealth;
        }

        public void Heal(float amount)
        {
            if (!IsAlive())
                return;

            float oldHealth = _currentHealth;
            _currentHealth = Mathf.Min(_currentHealth + amount, _maxHealth);

            float actualHeal = _currentHealth - oldHealth;
            if (actualHeal > 0)
            {
                OnHpChanged?.Invoke(_currentHealth, _maxHealth);
            }
        }

        public void HealPercent(float ratio)
        {
           
            float healAmount = ratio * _maxHealth;
            Heal(healAmount);
        }

        /// <summary>
        /// 피격 시 호출 (이펙트, 사운드 등)
        /// </summary>
        protected virtual void OnDamaged(AttackData attackData)
        {
            if (attackData != null)
            {
                switch (attackData.reactionType)
                {
                    case AttackReactionType.KnockBack:
                        MovementController.AddImpulse(attackData.attackDirection.normalized * attackData.knockbackForce);
                        break;

                    case AttackReactionType.Pull:
                        if (attackData.attacker != null)
                        {
                            Vector3 pullDir = (attackData.attacker.transform.position - transform.position).normalized;
                            pullDir.y = 0f;
                            MovementController.AddVelocity(pullDir * attackData.pullForce);
                        }
                        break;

                    case AttackReactionType.Airborne:
                    {
                        Vector3 launchDir = attackData.attackDirection.normalized;
                        launchDir.y = 0f;
                        MovementController.AddImpulse(launchDir * 5f + Vector3.up * attackData.airborneForce);
                        MovementController.Motor.ForceUnground();
                        break;
                    }

                    case AttackReactionType.Grab:
                        // Grab은 속도 적용 없이 State에서 행동 제한만 처리
                        break;
                }
            }

            string stateName = MovementController.CurrentState.StateName;
            if (stateName != "Hit" && stateName != "Grabbed")
            {
                if (MovementController.CurrentState.CanTransitionState("Hit"))
                {
                    if (attackData?.reactionType == AttackReactionType.Airborne)
                        MovementController.TransitionToState(new PlayerAirborneState(MovementController));
                    else if (attackData?.reactionType == AttackReactionType.Grab)
                        MovementController.TransitionToState(new PlayerGrabbedState(MovementController, attackData));
                    else
                        MovementController.TransitionToState(new PlayerHitState(MovementController, attackData));
                }

                CameraManager.Instance.StartShake("LiteHit");
            }

            if (TryGetSocket(ActorSocketType.Center, out var center))
            {   
                GameObjectManager.Instance.ShowFX(attackData.hitParticleName, center.position);
            }
            else
            {   
                GameObjectManager.Instance.ShowFX(attackData.hitParticleName, attackData.hitPoint);
            }
            _colorChanger.OnHit();
            
            Debug.Log($"[PlayerActor] 피격! HitPoint: {attackData?.hitPoint}");
        }
        
        /// <summary>
        /// 사망 시 호출
        /// </summary>
        protected virtual void OnDeath(AttackData attackData)
        {
            Debug.Log($"[PlayerActor] {gameObject.name} 사망!");
            
            GameHitStopManager.Instance.Execute(GameHitStopManager.HitStopIntensity.PlayerDie);

            CameraManager.Instance.StartShake("LiteHit");
            MovementController.TransitionToState(new PlayerDeathState(MovementController));
        }
    }
    // 애니메이션 이벤트 리시버
    public partial class PlayerActor : GameActor, IDamageable
    {
        public void Hit()
        {
            IInteractable target = GameObjectManager.Instance?.InteractionHandler?.CurrentClosestInteractable;
            if (target != null)
            {
                target.OnAnimationEvent(InteractionAnimEvent.OnHit, new PlayerInteractionEvent()
                {
                    value = Random.Range(10,50)
                });

                GameActor actor = target.GetActor();
                if (actor == null)
                {
                    return;
                }
                
                Vector3 targetPosition = actor.transform.position;
                var targetCollider = actor.GetComponent<Collider>();
                if (targetCollider != null)
                {
                    targetPosition.y += targetCollider.bounds.extents.y * 0.5f;
                }
                
                GameObjectManager.Instance.ShowFX("InteractionObjectHitFX", targetPosition);
            }
        }
        public void CatchFish()
        {
            IInteractable target = GameObjectManager.Instance?.InteractionHandler?.CurrentClosestInteractable;
            if (target != null)
            {
                target.OnAnimationEvent(InteractionAnimEvent.CatchFish, new PlayerInteractionEvent()
                {
                    value = 0
                });

                GameActor actor = target.GetActor();
                if (actor == null)
                {
                    return;
                }
                
                Vector3 targetPosition = actor.transform.position;
                GameObjectManager.Instance.ShowFX("InteractionObjectHitFX", targetPosition);
            }
        }
    }
}