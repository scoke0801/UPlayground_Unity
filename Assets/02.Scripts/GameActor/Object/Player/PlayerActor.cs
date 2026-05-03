using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Path;
using UPlayGround.Animation;
using UPlayGround.Component;
using UPlayGround.Data;
using UPlayGround.Data.Combat;
using UPlayGround.Data.Event;
using UPlayGround.MovementController;
using UPlayGround.Input;
using UPlayGround.InputDefine;
using UPlayGround.Manager;
using UPlayGround.Manager.Handler;
using UPlayGround.Manager.Combat;
using UPlayGround.State;
using UPlayGround.UI;
using Random = UnityEngine.Random;

namespace UPlayGround
{
    public partial class PlayerActor : GameActor, IDamageable
    {
        [SerializeField] private float _interactionRadius;
        [SerializeField] private LayerMask _interactionLayer;

        [SerializeField] private float _maxHealth     = 100f;
        [SerializeField] private float _currentHealth = 100f;
        [SerializeField] private bool  _isInvincible  = false;

        // 교체 시 캐릭터별 체력·스킬 게이지 저장소
        private readonly Dictionary<CharacterActorType, float> _characterHealthMap = new();
        private readonly Dictionary<CharacterActorType, float> _characterSkillMap  = new();

        [SerializeField] private PlayerEquipment  _equipment;
        [SerializeField] private PlayerCombat     _combat;
        [SerializeField] private PlayerSkillGauge _skillGauge;
        [SerializeField] private PlayerCombatWeaponStateController _combatWeaponStateController;
        [SerializeField] private FootIKController _footIK;
        private PlayerSwapBehaviour _swapBehaviour;

        [Header("Hit Shake Keys")]
        [Tooltip("일반 피격 쉐이크")]
        [SerializeField] private CameraShakeIdType _shakeKeyHit      = CameraShakeIdType.PlayerHit;
        [Tooltip("Heavy / KnockBack / Airborne 피격 쉐이크")]
        [SerializeField] private CameraShakeIdType _shakeKeyHeavyHit = CameraShakeIdType.PlayerHeavyHit;
        [Tooltip("사망 쉐이크")]
        [SerializeField] private CameraShakeIdType _shakeKeyDeath    = CameraShakeIdType.PlayerDeath;

        [Header("Parry")]
        [Tooltip("패리 성공 시 재생할 VFX 이름")]
        [SerializeField] private string _parryFxName = "ParryFX";

        public event Action<float, float> OnHpChanged;
        public event Action<float, float> OnSkillGaugeChanged;

        protected PlayerMovementController PlayerMovementPlayerController;

        private Camera              _camera;
        private PlayerActorAnimator _playerActorAnimator;

        private Vector2        _currentMoveInput;
        private InputCondition _jumpInputCondition;
        private InputCondition _crouchInputCondition;
        private InputCondition _dodgeInputCondition;
        private InputCondition _dashInputCondition;
        private InputCondition _attackInputCondition;
        private InputCondition _heavyInputCondition;
        private InputCondition _equipInputCondition;

        // 교체 어시스트
        private bool _swapAssistQueued = false;

        // 등장 공격 (교체 직후 범위 내 적 존재 시 발동)
        private bool         _entryAttackQueued = false;
        private MonsterActor _entryAttackTarget;
        // PlayerAttackState 가 OnEnter에서 1회 소비하여 ExecuteEntryAttack 라우팅을 트리거.
        private bool         _isEntryAttackPending = false;

        // 차지 공격 입력 추적
        private bool  _chargeAttackHeld;
        private float _chargeHoldTime;
        private const float ChargeThreshold = 0.3f; // 이 시간 이상 홀드 시 차지로 전환
        private InputCondition _interactionInputCondition;
        private InputCondition _guardInputCondition;

        private List<InputCondition> _skillInputCondition = new List<InputCondition>
        {
            InputCondition.None, InputCondition.None, InputCondition.None, InputCondition.None, InputCondition.None,
            InputCondition.None, InputCondition.None, InputCondition.None, InputCondition.None, InputCondition.None,
        };

        public override ActorAnimator      Animator              => _playerActorAnimator;
        public PlayerMovementController    PlayerController       => PlayerMovementPlayerController;
        public float                       InteractionRadius      => _interactionRadius;
        public LayerMask                   InteractionLayer       => _interactionLayer;
        public bool                        IsEquippedRightWeapon => _equipment.IsMainWeaponEquipped;
        public bool                        IsEquippedLeftWeapon  => _equipment.IsSubWeaponEquipped;
        public bool                        IsInCombat            => _combat?.IsInCombat ?? false;
        public float                       MaxHealth             => _maxHealth;
        public float                       CurrentHealth         => _currentHealth;
        public PlayerSkillGauge            SkillGauge            => _skillGauge;
        public FootIKController            FootIK                => _footIK;
    }

    public partial class PlayerActor : GameActor, IDamageable
    {
        #region Mono

        protected override void Awake()
        {
            base.Awake();

            _actorType = ActorType.Player | ActorType.Combat;
            _camera    = Camera.main;
            PlayerMovementPlayerController = MovementController as PlayerMovementController;
            _playerActorAnimator           = _animator as PlayerActorAnimator;

            InitComponents();
        }

        private void OnEnable()
        {
            RegisterInputEvents();
            CameraManager.Instance?.SetCombatStateProvider(() => _combat != null && _combat.IsInCombat);
        }

        private void OnDisable()
        {
            UnRegisterInputEvents();
            CameraManager.Instance?.SetCombatStateProvider(null);
            ClearAllInputState();
        }

        protected override void OnDestroy()
        {
            // OnDisable이 먼저 호출되므로 여기서는 추가 정리만 담당
            UnRegisterInputEvents();
            CameraManager.Instance?.SetCombatStateProvider(null);
            base.OnDestroy();
        }

        private void Update()
        {
            if (MovementController == null) return;

            if (_chargeAttackHeld)
                _chargeHoldTime += Time.deltaTime;

            // 교체 어시스트 공격 주입: PartyManager가 설정하면 다음 프레임 공격 입력으로 처리
            if (_swapAssistQueued)
            {
                _attackInputCondition = InputCondition.Pressed;
                _swapAssistQueued = false;
            }
            // 등장 공격 주입: PartyManager가 교체 후 범위 내 적 존재 시 설정
            else if (_entryAttackQueued)
            {
                ConsumeEntryAttackQueue();
            }

            Quaternion cameraRotation = _camera != null ? _camera.transform.rotation : Quaternion.identity;

            PlayerMovementPlayerController.SetInputs(new PlayerCharacterInputs
            {
                MoveInput        = _currentMoveInput,
                CameraRotation   = cameraRotation,
                CrouchInput      = _crouchInputCondition,
                JumpInput        = _jumpInputCondition,
                DodgeInput       = _dodgeInputCondition,
                AttackInput      = _attackInputCondition,
                HeavyAttackInput = _heavyInputCondition,
                EquipInput       = _equipInputCondition,
                InteractInput    = _interactionInputCondition,
                GuardInput       = _guardInputCondition,
                DashInput        = _dashInputCondition,
                ChargeAttackHeld = _chargeAttackHeld && _chargeHoldTime >= ChargeThreshold,
                ChargeHoldTime   = _chargeHoldTime,
                SkillInput = new List<InputCondition>
                {
                    _skillInputCondition[0], _skillInputCondition[1], _skillInputCondition[2],
                    _skillInputCondition[3], _skillInputCondition[4], _skillInputCondition[5],
                    _skillInputCondition[6], _skillInputCondition[7], _skillInputCondition[8],
                    _skillInputCondition[9],
                },
            });

            _dodgeInputCondition       = InputCondition.None;
            _dashInputCondition        = InputCondition.None;
            _attackInputCondition      = InputCondition.None;
            _heavyInputCondition       = InputCondition.None;
            _equipInputCondition       = InputCondition.None;
            _interactionInputCondition = InputCondition.None;
            for (int i = 0; i < _skillInputCondition.Count; ++i)
                _skillInputCondition[i] = InputCondition.None;
        }

        #endregion
    }

    // Input 처리
    public partial class PlayerActor : GameActor, IDamageable
    {
        private bool _isInputRegistered;

        private void RegisterInputEvents()
        {
            if (!InputManager.Instance || _isInputRegistered) return;
            _isInputRegistered = true;

            InputLayer layer = InputLayer.Level_0;
            var I = InputManager.Instance;

            I.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Move,        OnInputMove,             OnInputMove,                 OnInputMove,             null,             OnMoveCanceled,  layer);
            I.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Jump,        null,                    OnInputPerformedJump,        null,                    null,             null,            layer);
            I.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Walk,        null,                    OnInputPerformedWalk,        null,                    null,             null,            layer);
            I.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Sprint,      null,                    OnInputPerformedSprint,      null,                    null,             null,            layer);
            I.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Crouching,   null,                    OnInputPerformedCrouching,   null,                    null,             null,            layer);
            I.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Dodge,       null,                    OnInputPerformedDodge,       null,                    null,             null,            layer);
            I.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Dash,        null,                    OnInputPerformedDash,        null,                    null,             null,            layer);
            I.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Attack,      null,                    OnInputPerformedAttack,      null,                    null,             null,            layer);
            I.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.HeavyAttack, OnHeavyAttackStarted,    OnInputPerformedHeavyAttack, OnHeavyAttackCanceled,   null,             null,            layer);
            I.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Skill_1,     null,                    OnInputPerformedSkill_1,     null,                    null,             null,            layer);
            I.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Skill_2,     null,                    OnInputPerformedSkill_2,     null,                    null,             null,            layer);
            I.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Skill_3,     null,                    OnInputPerformedSkill_3,     null,                    null,             null,            layer);
            I.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Skill_4,     null,                    OnInputPerformedSkill_4,     null,                    null,             null,            layer);
            I.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Skill_5,     null,                    OnInputPerformedSkill_5,     null,                    null,             null,            layer);
            I.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Skill_6,     null,                    OnInputPerformedSkill_6,     null,                    null,             null,            layer);
            I.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Skill_7,     null,                    OnInputPerformedSkill_7,     null,                    null,             null,            layer);
            I.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Skill_8,     null,                    OnInputPerformedSkill_8,     null,                    null,             null,            layer);
            I.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Skill_9,     null,                    OnInputPerformedSkill_9,     null,                    null,             null,            layer);
            I.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Equip,       null,                    OnInputPerformedEquipWeapon, null,                    null,             null,            layer);
            I.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Interact,    null,                    OnInputPerformedInteraction, null,                    CanInputInteract, null,            layer);
            I.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Guard,       OnInputStartedGuard,     null,                        OnInputFinishedGuard,    null,             null,            layer);
        }

        private void UnRegisterInputEvents()
        {
            if (!InputManager.Instance || !_isInputRegistered) return;
            _isInputRegistered = false;

            var I = InputManager.Instance;
            I.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Move,        OnInputMove,             OnInputMove,                 OnInputMove);
            I.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Jump,        null,                    OnInputPerformedJump,        null);
            I.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Walk,        null,                    OnInputPerformedWalk,        null);
            I.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Sprint,      null,                    OnInputPerformedSprint,      null);
            I.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Crouching,   null,                    OnInputPerformedCrouching,   null);
            I.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Dodge,       null,                    OnInputPerformedDodge,       null);
            I.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Dash,        null,                    OnInputPerformedDash,        null);
            I.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Attack,      null,                    OnInputPerformedAttack,      null);
            I.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.HeavyAttack, OnHeavyAttackStarted,    OnInputPerformedHeavyAttack, OnHeavyAttackCanceled);
            I.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Skill_1,     null,                    OnInputPerformedSkill_1,     null);
            I.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Skill_2,     null,                    OnInputPerformedSkill_2,     null);
            I.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Skill_3,     null,                    OnInputPerformedSkill_3,     null);
            I.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Skill_4,     null,                    OnInputPerformedSkill_4,     null);
            I.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Skill_5,     null,                    OnInputPerformedSkill_5,     null);
            I.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Skill_6,     null,                    OnInputPerformedSkill_6,     null);
            I.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Skill_7,     null,                    OnInputPerformedSkill_7,     null);
            I.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Skill_8,     null,                    OnInputPerformedSkill_8,     null);
            I.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Skill_9,     null,                    OnInputPerformedSkill_9,     null);
            I.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Equip,       null,                    OnInputPerformedEquipWeapon, null);
            I.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Interact,    null,                    OnInputPerformedInteraction, null);
            I.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Guard,       OnInputStartedGuard,     null,                        OnInputFinishedGuard);
        }

        #region Input Callbacks

        private void OnInputMove(InputAction.CallbackContext obj)         => _currentMoveInput = obj.ReadValue<Vector2>();
        private void OnMoveCanceled()                                      { _currentMoveInput = Vector2.zero; PlayerMovementPlayerController.ClearInputAll(); }
        private void OnInputPerformedJump(InputAction.CallbackContext obj) => _jumpInputCondition = InputCondition.Pressed;
        private void OnInputPerformedCrouching(InputAction.CallbackContext obj)
            => _crouchInputCondition = _crouchInputCondition == InputCondition.Pressed ? InputCondition.None : InputCondition.Pressed;
        private void OnInputPerformedDodge(InputAction.CallbackContext obj)        => _dodgeInputCondition        = InputCondition.Pressed;
        private void OnInputPerformedDash(InputAction.CallbackContext obj)         => _dashInputCondition         = InputCondition.Pressed;
        private void OnInputPerformedWalk(InputAction.CallbackContext obj)
            => MoveAnimType = MoveAnimType == BaseMoveAnimType.Walk ? BaseMoveAnimType.Run : BaseMoveAnimType.Walk;
        private void OnInputPerformedSprint(InputAction.CallbackContext obj)
        {
            if (MovementController.CurrentState.StateName == "GroundMove")
                MoveAnimType = MoveAnimType == BaseMoveAnimType.Sprint ? BaseMoveAnimType.Run : BaseMoveAnimType.Sprint;
        }
        private void OnInputPerformedHeavyAttack(InputAction.CallbackContext obj)
        {
            // InputManager가 performed 시점에 버퍼에 자동 추가하므로 즉시 제거.
            // 짧은 누름(일반 강공격)인지 긴 누름(차지)인지는 canceled에서 판별 후 재추가.
            InputManager.Instance.InputBuffer.ConsumeInput(PlayerAction.HeavyAttack);
        }

        private void OnHeavyAttackStarted(InputAction.CallbackContext obj)
        {
            _chargeHoldTime   = 0f;
            _chargeAttackHeld = true;
        }

        private void OnHeavyAttackCanceled(InputAction.CallbackContext obj)
        {
            if (_chargeAttackHeld && _chargeHoldTime < ChargeThreshold)
            {
                // 짧은 누름 → 일반 강공격으로 처리 (버퍼에 재추가)
                InputManager.Instance.InputBuffer.AddInput(PlayerAction.HeavyAttack);
                _heavyInputCondition = InputCondition.Pressed;
            }
            _chargeAttackHeld = false;
        }
        private void OnInputPerformedAttack(InputAction.CallbackContext obj)       => _attackInputCondition   = InputCondition.Pressed;
        private void OnInputPerformedEquipWeapon(InputAction.CallbackContext obj)  => _equipInputCondition    = InputCondition.Pressed;
        private void OnInputPerformedSkill_1(InputAction.CallbackContext obj)      => _skillInputCondition[0] = InputCondition.Pressed;
        private void OnInputPerformedSkill_2(InputAction.CallbackContext obj)      => _skillInputCondition[1] = InputCondition.Pressed;
        private void OnInputPerformedSkill_3(InputAction.CallbackContext obj)      => _skillInputCondition[2] = InputCondition.Pressed;
        private void OnInputPerformedSkill_4(InputAction.CallbackContext obj)      => _skillInputCondition[3] = InputCondition.Pressed;
        private void OnInputPerformedSkill_5(InputAction.CallbackContext obj)      => _skillInputCondition[4] = InputCondition.Pressed;
        private void OnInputPerformedSkill_6(InputAction.CallbackContext obj)      => _skillInputCondition[5] = InputCondition.Pressed;
        private void OnInputPerformedSkill_7(InputAction.CallbackContext obj)      => _skillInputCondition[6] = InputCondition.Pressed;
        private void OnInputPerformedSkill_8(InputAction.CallbackContext obj)      => _skillInputCondition[7] = InputCondition.Pressed;
        private void OnInputPerformedSkill_9(InputAction.CallbackContext obj)      => _skillInputCondition[8] = InputCondition.Pressed;
        private void OnInputPerformedInteraction(InputAction.CallbackContext obj)  => _interactionInputCondition = InputCondition.Pressed;
        private void OnInputStartedGuard(InputAction.CallbackContext obj)          => _guardInputCondition = InputCondition.Pressed;
        private void OnInputFinishedGuard(InputAction.CallbackContext obj)         => _guardInputCondition = InputCondition.None;

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

        private bool CanInputInteract() => GameObjectManager.Instance.CanInteract();

        private void ClearAllInputState()
        {
            _currentMoveInput          = Vector2.zero;
            _jumpInputCondition        = InputCondition.None;
            _crouchInputCondition      = InputCondition.None;
            _dodgeInputCondition       = InputCondition.None;
            _dashInputCondition        = InputCondition.None;
            _attackInputCondition      = InputCondition.None;
            _heavyInputCondition       = InputCondition.None;
            _equipInputCondition       = InputCondition.None;
            _interactionInputCondition = InputCondition.None;
            _guardInputCondition       = InputCondition.None;
            _chargeAttackHeld          = false;
            _chargeHoldTime            = 0f;
            for (int i = 0; i < _skillInputCondition.Count; ++i)
                _skillInputCondition[i] = InputCondition.None;
        }

        /// <summary>
        /// 교체 어시스트 공격을 다음 Update()에서 실행하도록 예약한다.
        /// PartyManager가 교체 성공 시 incoming 캐릭터에 호출.
        /// </summary>
        public void QueueSwapAssist() => _swapAssistQueued = true;

        /// <summary>
        /// 등장 공격을 다음 Update()에서 실행하도록 예약한다.
        /// PartyManager가 교체 성공 + 범위 내 적 존재 시 호출.
        /// 어시스트와는 배타적으로만 동작한다 (PartyManager가 보장).
        /// </summary>
        public void QueueEntryAttack(MonsterActor target)
        {
            _entryAttackQueued = true;
            _entryAttackTarget = target;
        }

        /// <summary>
        /// 큐에 쌓인 등장 공격을 소비한다. 무력화 상태이면 폐기.
        /// </summary>
        private void ConsumeEntryAttackQueue()
        {
            string state = MovementController?.CurrentState?.StateName;
            if (state == "Hit" || state == "Death" || state == "Grabbed" || state == "Knockdown")
            {
                _entryAttackQueued = false;
                _entryAttackTarget = null;
                return;
            }

            // 가장 가까운 적 방향으로 회전 스냅
            if (_entryAttackTarget != null && _entryAttackTarget.IsAlive())
            {
                Vector3 toTarget = _entryAttackTarget.transform.position - transform.position;
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude > 0.0001f)
                    transform.rotation = Quaternion.LookRotation(toTarget);
            }

            _attackInputCondition  = InputCondition.Pressed;
            _isEntryAttackPending  = true;
            _entryAttackQueued     = false;
            _entryAttackTarget     = null;
        }

        /// <summary>
        /// PlayerAttackState.OnEnter 가 호출. true면 이번 공격을 등장 공격으로 처리.
        /// 한 번 호출되면 자동으로 false로 리셋된다.
        /// </summary>
        public bool ConsumeEntryAttackPending()
        {
            if (!_isEntryAttackPending) return false;
            _isEntryAttackPending = false;
            return true;
        }
    }

    // Component
    public partial class PlayerActor : GameActor, IDamageable
    {
        public PlayerEquipment GetPlayerEquipment() => _equipment;
        public PlayerCombat    GetCombat()          => _combat;

        /// <summary>
        /// 모델 교체 시 PlayerSwapBehaviour가 호출.
        /// 이전 캐릭터 상태를 저장하고 새 캐릭터 데이터로 컴포넌트를 일괄 갱신한다.
        /// </summary>
        public void RefreshForCharacter(CharacterModelData data)
        {
            // 현재 캐릭터 상태 저장 (최초 호출 시에는 None이므로 스킵)
            if (_characterActorType != CharacterActorType.None)
            {
                _characterHealthMap[_characterActorType] = _currentHealth;
                _characterSkillMap[_characterActorType]  = _skillGauge.CurrentGauge;
            }

            _characterActorType = data.characterType;

            // 체력 복원 (처음 등장 시 최대치)
            _maxHealth     = data.maxHealth;
            _currentHealth = _characterHealthMap.TryGetValue(data.characterType, out var hp)
                             ? hp : data.maxHealth;

            // 스킬 게이지 복원
            _skillGauge.SetGauge(
                _characterSkillMap.TryGetValue(data.characterType, out var sg) ? sg : 0f);

            // 활성 Model의 컴포넌트 참조 갱신
            _animator            = GetComponentInChildren<ActorAnimator>();
            
            _playerActorAnimator = _animator as PlayerActorAnimator;
            _equipment           = GetComponentInChildren<PlayerEquipment>();
            _equipment?.RefreshWeaponConstraintsFromModel();

            if(_characterActorType == CharacterActorType.Bokusei)
                _equipment.SetWeaponType(WeaponType.Katana);
            else
            {
                _equipment.SetWeaponType(WeaponType.NoWeapon);
            }
            
            // 애니메이터에 Actor 재주입 (PlayerEquipment 참조 포함)
            _playerActorAnimator?.Init(this);

            // 전투 컴포넌트 참조 갱신 + 공격 데이터 교체
            _combat.RefreshComponentReferences();
            _combat.RefreshAttackData(data.attackData);
            _combatWeaponStateController?.RefreshReferences();

            // 새 모델의 ParentConstraint 기본 weight는 prefab 세팅에 의존하므로,
            // 현재 전투 상태에 맞춰 weight + 플래그를 강제 동기화한다.
            _equipment?.ForceSyncMainWeaponState(_combat != null && _combat.IsInCombat);

            // 소켓
            RefreshSockets(data);

            // 비주얼 효과 컴포넌트 재초기화
            _colorChanger.InitializeRendererData();
            _dissolveController.RefreshRenderers();

            // Foot IK
            _footIK.Refresh(data.AnimancerComponent?.Animator);

            // 새 AnimancerComponent에 즉시 애니메이션 적용: Idle로 강제 전환
            // 현재 상태가 이미 PlayerIdleState인 경우 TransitionToState의 같은 타입 가드(ActorMovementController.cs:149)
            // 때문에 OnEnter가 호출되지 않아 새 애니메이터에 PlayMotion이 안 걸린다. 직접 한 번 더 재생한다.
            PlayerMovementPlayerController?.TransitionToState(new PlayerIdleState(PlayerMovementPlayerController));
            _playerActorAnimator?.PlayMotion(AnimKey.Idle, 0f);

            OnHpChanged?.Invoke(_currentHealth, _maxHealth);
        }

        private void RefreshSockets(CharacterModelData data)
        {
            if (data.RightHandSocket != null) _socketDict[ActorSocketType.RightHand] = data.RightHandSocket;
            if (data.LeftHandSocket  != null) _socketDict[ActorSocketType.LeftHand]  = data.LeftHandSocket;
            if (data.CenterSocket    != null) _socketDict[ActorSocketType.Center]    = data.CenterSocket;
            if (data.HeadSocket      != null) _socketDict[ActorSocketType.Head]      = data.HeadSocket;
        }

        /// <summary>
        /// 지정 캐릭터의 현재 체력 반환. 한 번도 활성화된 적 없으면 최대 체력으로 취급한다.
        /// </summary>
        public float GetHealthForCharacter(CharacterActorType type)
        {
            if (type == _characterActorType) return _currentHealth;
            return _characterHealthMap.TryGetValue(type, out var hp) ? hp : GetMaxHealthForCharacter(type);
        }

        public bool HasHealthRecordForCharacter(CharacterActorType type)
            => type == _characterActorType || _characterHealthMap.ContainsKey(type);

        /// <summary>
        /// 지정 캐릭터의 최대 체력 반환. 현재 캐릭터가 아니면 PlayerSwapBehaviour의 모델 데이터에서 조회한다.
        /// </summary>
        public float GetMaxHealthForCharacter(CharacterActorType type)
        {
            if (type == _characterActorType) return _maxHealth;

            if (_swapBehaviour == null) _swapBehaviour = GetComponent<PlayerSwapBehaviour>();
            var modelData = _swapBehaviour?.GetModelData(type);
            return modelData != null ? modelData.maxHealth : 1f;
        }

        public float GetSkillGaugeForCharacter(CharacterActorType type)
        {
            if (type == _characterActorType) return _skillGauge != null ? _skillGauge.CurrentGauge : 0f;
            return _characterSkillMap.TryGetValue(type, out var gauge) ? gauge : 0f;
        }

        public float GetMaxSkillGaugeForCharacter(CharacterActorType type)
            => _skillGauge != null ? _skillGauge.MaxGauge : 1f;

        private void InitComponents()
        {
            if (_combat     == null) _combat     = GetComponent<PlayerCombat>();
            if (_equipment  == null) _equipment  = GetComponentInChildren<PlayerEquipment>();
            if (_skillGauge == null) _skillGauge = GetComponent<PlayerSkillGauge>();
            if (_combatWeaponStateController == null)
                _combatWeaponStateController = gameObject.GetOrAddComponent<PlayerCombatWeaponStateController>();
            if (_footIK     == null) _footIK     = GetComponent<FootIKController>();
            if (_swapBehaviour == null) _swapBehaviour = GetComponent<PlayerSwapBehaviour>();

            if (_skillGauge != null)
                _skillGauge.OnGaugeChanged += (cur, max) => OnSkillGaugeChanged?.Invoke(cur, max);
            // SetCombatStateProvider는 OnEnable/OnDisable에서 관리
        }
    }

    // IDamageable
    public partial class PlayerActor : GameActor, IDamageable
    {
        public void TakeDamage(AttackData attackData)
        {
            if (_combat.IsGuarding)
            {
                if (MovementController.CurrentState is PlayerGuardState guardState)
                {
                    guardState.OnAttackBlocked(attackData);

                    if (!_combat.IsGuarding)
                        OnGuardBrokenDamage(attackData);

                    return;
                }
            }

            // 패리: Attack / Charge 상태에서 히트박스가 활성화된 동안 피격 시 발동
            if (TryParry(attackData))
                return;

            if (!CanTakeDamage())
            {
                // 도지 중 피격 시도 → 퍼펙트 도지 창 내면 보상 효과 발동
                if (MovementController.CurrentState is PlayerDodgeState)
                    TryPerfectDodge(attackData);
                return;
            }

            float finalDamage = attackData.damage;
            
            if (attackData.criticalMultiplier > 1.0f)
            {
                finalDamage *= attackData.criticalMultiplier;
            }

            _currentHealth = MathF.Max(0, _currentHealth - finalDamage);
            OnHpChanged?.Invoke(_currentHealth, _maxHealth);
           
            Vector3 floaterPos = attackData.hitPoint != Vector3.zero
                ? attackData.hitPoint
                : transform.position;
            UIManager.Instance.ShowDamageFloater(floaterPos, finalDamage, FloatStyle.PlayerDamage);

            OnDamaged(attackData);

            if (_currentHealth <= 0)
                OnDeath(attackData);
        }

        public bool      IsAlive()          => _currentHealth > 0;
        public Transform GetTransform()     => transform;
        public void      LockOn()           { }
        public void      UnLockOn()         { }
        public float     GetHealthPercent() => _currentHealth / _maxHealth;
        public float     GetCurrentHealth() => _currentHealth;

        public void SetInvincible(bool invincible) => _isInvincible = invincible;

        public bool CanTakeDamage()
            => IsAlive() && !_isInvincible && !MovementController.CurrentState.GrantsInvincibility;

        public void Heal(float amount)
        {
            if (!IsAlive()) return;
            float old = _currentHealth;
            _currentHealth = Mathf.Min(_currentHealth + amount, _maxHealth);
            if (_currentHealth > old)
            {
                OnHpChanged?.Invoke(_currentHealth, _maxHealth);
                UIManager.Instance.ShowDamageFloaterHeal(transform.position, _currentHealth - old);
            }
        }

        public void HealPercent(float ratio) => Heal(ratio * _maxHealth);

        /// <summary>
        /// Attack 상태에서 히트박스가 활성 상태이고 약공격(NormalAttack) 중일 때 피격되면 패리를 시도한다.
        /// CheatManager.IsAlwaysParryEnabled가 켜져 있으면 조건 없이 패리한다.
        /// </summary>
        private bool TryParry(AttackData attackData)
        {
            bool alwaysParry = CheatManager.Instance?.IsAlwaysParryEnabled ?? false;

            if (!alwaysParry)
            {
                string stateName = MovementController.CurrentState.StateName;
                if (stateName != "Attack")
                    return false;
                if (!_combat.IsPossibleCollide)
                    return false;
                // 약공격(NormalAttack)만 패리 가능 — 강공격·차지·스킬은 패리 불가
                if (_combat.CurrentAttackData?.attackKind != AttackKind.NormalAttack)
                    return false;
            }

            OnParrySuccess(attackData);
            return true;
        }

        private void OnParrySuccess(AttackData attackData)
        {
            Debug.Log("[PlayerActor] 패리 성공!");

            // 패리 반격 창을 먼저 열어둬야 상태 전환 후 반격 입력을 받을 수 있다
            _combat.OpenParryCounterWindow();

            // 히트 감지를 즉시 비활성화해 이후 PerformHitDetection이 HitStop을 덮어쓰지 않도록 한다
            _combat.SetEnableCollision(false);

            // 공격 상태를 중단하고 Idle로 복귀 (패리 반격 창은 이미 열려 있으므로 다음 공격 입력 시 반격 발동)
            MovementController.TransitionToState(new PlayerIdleState(MovementController));

            // 히트스톱 (퍼펙트 가드와 동일한 슬로우 연출)
            GameCombatManager.Instance.GameHitStop.Execute(GameHitStopHandler.HitStopIntensity.PlayerGuard);

            // 카메라 피드백
            CameraManager.Instance?.StartShake(_shakeKeyHeavyHit);
            CameraManager.Instance?.PlayEffect(PlayerGuardState.PerfectGuardFOVData);
            if (attackData?.attackDirection != Vector3.zero)
                CameraManager.Instance?.Punch(-(attackData?.attackDirection ?? Vector3.forward), 0.15f, 0.2f);

            // 패리 VFX
            Vector3 fxPos = TryGetSocket(ActorSocketType.Weapon, out var center)
                ? center.position
                : (attackData?.hitPoint ?? Vector3.zero) != Vector3.zero
                    ? attackData.hitPoint
                    : transform.position;

            GameObjectManager.Instance.ShowFX(_parryFxName, fxPos);

            // 바이탈 오브
            GameCombatManager.Instance.GameVitalOrb.TrySpawn(VitalOrbTrigger.PerfectGuard, fxPos);

            // 공격자(몬스터) 경직
            if (attackData?.attacker is MonsterActor monster)
                monster.OnParried();
        }

        /// <summary>
        /// 도지 중 피격 시도 시 호출. 퍼펙트 도지 판정 창 내면 보상 효과를 발동한다.
        /// </summary>
        private void TryPerfectDodge(AttackData attackData)
        {
            if (!_combat.IsPerfectDodgeWindow) return;

            // 퍼펙트 도지 성공 — 창 즉시 닫아 중복 발동 방지
            _combat.ClosePerfectDodgeWindow();

            // VitalOrb 보상 스폰
            GameCombatManager.Instance.GameVitalOrb.TrySpawn(VitalOrbTrigger.Dodge, transform.position);

            // 히트스탑
            GameCombatManager.Instance.GameHitStop.Execute(GameHitStopHandler.HitStopIntensity.PlayerGuard);

            // 카메라 피드백
            CameraManager.Instance.StartShake(_shakeKeyHit);
            if (attackData?.attackDirection != Vector3.zero)
                CameraManager.Instance.Punch(-(attackData?.attackDirection ?? Vector3.forward), 0.06f, 0.1f);

            Debug.Log("[PlayerActor] 퍼펙트 도지 성공!");
        }

        /// <summary>
        /// 가드 브레이크 시 호출.
        /// GuardBreakState가 경직·애니를 담당하므로 State 전환 없이 데미지·피드백만 처리한다.
        /// </summary>
        private void OnGuardBrokenDamage(AttackData attackData)
        {
            if (!CanTakeDamage()) return;

            _currentHealth = MathF.Max(0, _currentHealth - attackData.damage);
            OnHpChanged?.Invoke(_currentHealth, _maxHealth);

            Vector3 floaterPos = attackData.hitPoint != Vector3.zero
                ? attackData.hitPoint
                : transform.position;
            UIManager.Instance.ShowDamageFloater(floaterPos, attackData.damage, FloatStyle.PlayerDamage);

            CameraManager.Instance.StartShake(_shakeKeyHeavyHit);

            Vector3 fxPos = TryGetSocket(ActorSocketType.Center, out var center)
                ? center.position
                : attackData.hitPoint;
            GameObjectManager.Instance.ShowFX(attackData.hitParticleName, fxPos);

            _colorChanger.OnHit();

            if (_currentHealth <= 0)
                OnDeath(attackData);
        }

        /// <summary>
        /// 피격 시 호출.
        /// 쉐이크 강도는 AttackReactionType으로 결정한다.
        /// </summary>
        protected virtual void OnDamaged(AttackData attackData)
        {
            // 슈퍼아머 체크: 한 단계 이상 차징 완료 시 물리 충격(밀려남) 및 상태 전환 무시
            bool hasSuperArmor = MovementController.CurrentState is PlayerChargeState chargeState &&
                                 chargeState.HasChargedAtLeastOneStage;

            if (!hasSuperArmor && attackData != null)
            {
                switch (attackData.reactionType)
                {
                    case AttackReactionType.KnockBack:
                        MovementController.AddImpulse(attackData.attackDirection.normalized * attackData.knockbackForce,
                            attackData.knockbackDrag);
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
                        MovementController.AddImpulse(
                            launchDir * attackData.knockbackForce + Vector3.up * attackData.airborneForce,
                            attackData.knockbackDrag);
                        MovementController.Motor.ForceUnground();
                        break;
                    }

                    case AttackReactionType.Grab:
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

                bool isHeavyReaction = attackData?.reactionType is
                    AttackReactionType.Heavy or
                    AttackReactionType.KnockBack or
                    AttackReactionType.Airborne or
                    AttackReactionType.Knockdown or
                    AttackReactionType.Stun;

                CameraManager.Instance.StartShake(isHeavyReaction ? _shakeKeyHeavyHit : _shakeKeyHit);
            }

            Vector3 fxPos = TryGetSocket(ActorSocketType.Center, out var center)
                ? center.position
                : (attackData?.hitPoint ?? transform.position);
            GameObjectManager.Instance.ShowFX(attackData?.hitParticleName, fxPos);

            _colorChanger.OnHit();
        }

        /// <summary>
        /// 사망 시 호출.
        /// </summary>
        protected virtual void OnDeath(AttackData attackData)
        {
            Debug.Log($"[PlayerActor] {gameObject.name} 사망!");
            GameCombatManager.Instance.GameHitStop.Execute(GameHitStopHandler.HitStopIntensity.PlayerDie);
            CameraManager.Instance.StartShake(_shakeKeyDeath);
            MovementController.TransitionToState(new PlayerDeathState(MovementController));
        }

        /// <summary>
        /// 지정 위치/회전으로 부활한다.
        /// healPercent: 회복할 HP 비율 (0~1). 기본값 1 = 최대 HP 전체 회복.
        /// </summary>
        public void Respawn(Vector3 position, Quaternion rotation, float healPercent = 1f)
        {
            _currentHealth = _maxHealth * Mathf.Clamp01(healPercent);
            OnHpChanged?.Invoke(_currentHealth, _maxHealth);

            var motor = ActorController?.Motor;
            if (motor != null)
                motor.SetPositionAndRotation(position, rotation);
            else
                transform.SetPositionAndRotation(position, rotation);

            MovementController.TransitionToState(new PlayerIdleState(MovementController));
            CameraManager.Instance?.SnapToTarget(position);

            Debug.Log($"[PlayerActor] {gameObject.name} 부활 — 위치: {position}");
        }
    }

    // 애니메이션 이벤트 리시버
    public partial class PlayerActor : GameActor, IDamageable
    {
        public void Hit()
        {
            IInteractable target = GameObjectManager.Instance?.InteractionHandler?.CurrentClosestInteractable;
            if (target == null) return;

            target.OnAnimationEvent(InteractionAnimEvent.OnHit, new PlayerInteractionEvent { value = Random.Range(10, 50) });

            GameActor actor = target.GetActor();
            if (actor == null) return;

            Vector3 pos = actor.transform.position;
            var col = actor.GetComponent<Collider>();
            if (col != null) pos.y += col.bounds.extents.y * 0.5f;
            GameObjectManager.Instance.ShowFX(FXKeyType.InteractionObjectHitFX, pos);
        }

        public void CatchFish()
        {
            IInteractable target = GameObjectManager.Instance?.InteractionHandler?.CurrentClosestInteractable;
            if (target == null) return;

            target.OnAnimationEvent(InteractionAnimEvent.CatchFish, new PlayerInteractionEvent { value = 0 });

            GameActor actor = target.GetActor();
            if (actor == null) return;
            GameObjectManager.Instance.ShowFX(FXKeyType.InteractionObjectHitFX, actor.transform.position);
        }
    }
}
