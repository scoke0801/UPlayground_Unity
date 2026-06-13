using System;
using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using UnityEngine;
using UnityEngine.InputSystem;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Path;
using UPlayGround.Animation;
using UPlayGround.Component;
using UPlayGround.Data;
using UPlayGround.Data.Combat;
using UPlayGround.Data.Event;
using UPlayGround.Data.Stat;
using UPlayGround.MovementController;
using UPlayGround.Input;
using UPlayGround.InputDefine;
using UPlayGround.Manager;
using UPlayGround.Manager.Handler;
using UPlayGround.Manager.Combat;
using UPlayGround.Combat;
using UPlayGround.State;
using UPlayGround.UI;
using Random = UnityEngine.Random;
using UPlayGround.AI.CombatDecision;

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
        private readonly Dictionary<CharacterActorType, float[]> _characterSkillCooldownMap = new();
        private bool _hasInitializedCharacterRuntime;

        [SerializeField] private PlayerEquipment  _equipment;
        [SerializeField] private PlayerCombat     _combat;
        [SerializeField] private PlayerSkillGauge _skillGauge;
        [SerializeField] private PlayerCombatWeaponStateController _combatWeaponStateController;
        [SerializeField] private FootIKController _footIK;
        [SerializeField] private PlayerBehaviorPredictor _behaviorPredictor;
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

        // 기본 Airborne 수치(7~8)는 피격 경직으로 처리하고, 전용 launch급 공격만 공중 상태로 보낸다.
        private const float MinAirborneStateForce = 10f;

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
        // 어시스트 패리(§4.3): 패리 윈도우 우선. 창 내 피격 시 패리 처리, 비소비 만료 시 즉시공격으로 폴백.
        private bool  _assistParryFallbackPending = false;
        private float _assistParryFallbackTime    = -999f;

        // 스왑 회피 카운터. 1차 구현은 등장 공격 데이터를 재사용한다.
        private bool         _swapEvadeQueued = false;
        private MonsterActor _swapEvadeTarget;
        private float        _swapEvadeInvincibleEndTime = -999f;
        private float        _swapEvadeCounterInputEndTime = -999f;

        // 경직 내성(Stagger Protection): 리액션 회복 직후 짧은 창 동안 약한 리액션(Light/Hit)을 무시한다.
        // 데미지는 그대로 적용되고 통제권만 보호 → 다인전 Hit→Idle(찰나)→Hit 재스턴 루프를 차단한다.
        private float        _staggerImmuneEndTime = -999f;
        // 회복 직후 부여 길이(초). 0.25~0.35 권장. 큰 한 방(Heavy/넉백 등)은 이 창에도 통과한다.
        public const float   StaggerImmunityDuration = 0.3f;

        // 등장 공격 (교체 직후 범위 내 적 존재 시 발동)
        private bool         _entryAttackQueued = false;
        private MonsterActor _entryAttackTarget;
        // PlayerAttackState 가 OnEnter에서 1회 소비하여 ExecuteEntryAttack 라우팅을 트리거.
        private bool         _isEntryAttackPending = false;
        private bool         _isSwapEvadeCounterAttackPending = false;
        private bool         _isSwapSpecialAttackPending = false;
        private bool         _isInputSuppressed = false;

        // 차지 공격 입력 추적
        private bool  _chargeAttackHeld;
        private float _chargeHoldTime;
        private const float ChargeThreshold = 0.3f; // 이 시간 이상 홀드 시 차지로 전환
        private InputCondition _interactionInputCondition;
        private InputCondition _guardInputCondition;

        // 직접 버튼이 연결된 스킬 슬롯만 보유 (Skill_1, Skill_2). 그 외 스킬은 연계로 발동.
        private List<InputCondition> _skillInputCondition = new List<InputCondition>
        {
            InputCondition.None, InputCondition.None,
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
        public bool                        IsInputSuppressed     => _isInputSuppressed;
        public bool                        IsSwapEvadeInvincible => Time.time <= _swapEvadeInvincibleEndTime;
        public bool                        IsSwapEvadeCounterAvailable => Time.time <= _swapEvadeCounterInputEndTime;
        public bool                        IsStaggerImmune       => Time.time <= _staggerImmuneEndTime;
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

            // base.Awake() 시점의 _animator.Init(this)는 InitComponents 이전이라
            // PlayerEquipment / PlayerCombat 참조를 null로 캡처한다. 컴포넌트 세팅이 끝난
            // 지금 한 번 더 Init을 호출해 캐시 참조를 채운다.
            _playerActorAnimator?.Init(this);
        }

        // RefreshForCharacter가 sibling 컴포넌트(_skillGauge / _combat 등)의 Awake 완료를
        // 전제하므로 Awake가 아닌 Start에서 호출한다. (Awake 순서는 보장되지 않는다.)
        protected override void Start()
        {
            base.Start();
            EnsureInitialCharacterModelInitialized();
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

            if (_isInputSuppressed)
            {
                ClearAllInputState();
                PlayerMovementPlayerController?.ClearInputAll();
                return;
            }

            if (_chargeAttackHeld)
                _chargeHoldTime += Time.deltaTime;

            // 어시스트 패리(§4.3) 폴백: 패리 창이 비소비로 만료되면 기존 어시스트 즉시공격으로 폴백.
            if (_assistParryFallbackPending && Time.time > _assistParryFallbackTime)
            {
                _assistParryFallbackPending = false;
                _swapAssistQueued = true;
            }

            // 스왑 회피 카운터는 등장 공격 데이터를 재사용하되, 일반 어시스트/등장 공격보다 우선한다.
            if (_swapEvadeQueued)
            {
                ConsumeSwapEvadeQueue();
            }
            // 교체 어시스트 공격 주입: PartyManager가 설정하면 다음 프레임 공격 입력으로 처리
            else if (_swapAssistQueued)
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
                SkillInput = new List<InputCondition>(_skillInputCondition),
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
            I.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.SkillAbility,     null,                    OnInputPerformedSkill_1,     null,                    null,             null,            layer);
            I.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.SkillUltimate,     null,                    OnInputPerformedSkill_2,     null,                    null,             null,            layer);
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
            I.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.SkillAbility,     null,                    OnInputPerformedSkill_1,     null);
            I.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.SkillUltimate,     null,                    OnInputPerformedSkill_2,     null);
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
                InputManager.Instance.InputBuffer.AddInput(PlayerAction.HeavyAttack, bufferTime: 0.24f);
                _heavyInputCondition = InputCondition.Pressed;
            }
            _chargeAttackHeld = false;
        }
        private void OnInputPerformedAttack(InputAction.CallbackContext obj)       => _attackInputCondition   = InputCondition.Pressed;
        private void OnInputPerformedEquipWeapon(InputAction.CallbackContext obj)  => _equipInputCondition    = InputCondition.Pressed;
        private void OnInputPerformedSkill_1(InputAction.CallbackContext obj)      => _skillInputCondition[0] = InputCondition.Pressed;
        private void OnInputPerformedSkill_2(InputAction.CallbackContext obj)      => _skillInputCondition[1] = InputCondition.Pressed;
        private void OnInputPerformedInteraction(InputAction.CallbackContext obj)
        {
            _interactionInputCondition = InputCondition.Pressed;

            if (GetCombat()?.FindSpecialBreakAttackTarget() != null)
                InputManager.Instance.InputBuffer.AddInput(PlayerAction.Interact, bufferTime: 0.15f);
        }
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

        private bool CanInputInteract()
        {
            if (GameObjectManager.Instance.CanInteract())
                return true;

            return GetCombat()?.FindSpecialBreakAttackTarget() != null;
        }

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

        public void SetInputSuppressed(bool suppressed)
        {
            _isInputSuppressed = suppressed;
            ClearAllInputState();
            PlayerMovementPlayerController?.ClearInputAll();
            InputManager.Instance?.InputBuffer?.Clear();
        }

        /// <summary>
        /// 교체 어시스트 공격을 다음 Update()에서 실행하도록 예약한다.
        /// PartyManager가 교체 성공 시 incoming 캐릭터에 호출.
        /// </summary>
        public void QueueSwapAssist() => _swapAssistQueued = true;

        /// <summary>
        /// 어시스트 스왑(§4.3) — 패리 윈도우 우선. 입장 캐릭터에 패리 창을 열고,
        /// 창이 비소비로 만료되면 기존 어시스트 즉시공격으로 폴백하도록 예약한다.
        /// PartyManager가 교체 성공 + 어시스트 조건일 때 호출.
        /// </summary>
        public void OpenAssistParryAndQueueFallback()
        {
            _combat.OpenAssistParryWindow();
            _assistParryFallbackPending = true;
            _assistParryFallbackTime    = Time.time + _combat.AssistParryWindowDuration;
        }

        public void BeginSwapEvadeIFrame(float duration)
        {
            _swapEvadeInvincibleEndTime = Time.time + Mathf.Max(0f, duration);
        }

        /// <summary>
        /// 경직 내성 창을 부여한다. 리액션 상태(Hit/Stun/Knockdown)가 Idle로 자연 종료될 때 호출.
        /// 창 동안 약한 리액션(Light/Hit)은 무시되어 연속 경직(스턴락)을 막는다.
        /// 데미지·무적과는 무관 — 데미지는 그대로 들어가고, 큰 리액션은 통과한다.
        /// </summary>
        public void GrantStaggerImmunity(float duration)
        {
            float end = Time.time + Mathf.Max(0f, duration);
            if (end > _staggerImmuneEndTime)
                _staggerImmuneEndTime = end;
        }

        public void QueueSwapEvade(MonsterActor target, float counterWindow)
        {
            _swapEvadeQueued = true;
            _swapEvadeTarget = target;
            _swapEvadeCounterInputEndTime = Time.time + Mathf.Max(0f, counterWindow);
        }

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

        public bool TryStartSwapSpecialAttack()
        {
            _isSwapSpecialAttackPending = true;

            bool entered = PlayerMovementPlayerController != null
                           && PlayerAttackState.TryEnter(PlayerMovementPlayerController);
            if (!entered)
            {
                _isSwapSpecialAttackPending = false;
            }

            return entered;
        }

        public bool TryStartEntryAttack()
        {
            _isEntryAttackPending = true;

            bool entered = PlayerMovementPlayerController != null
                           && PlayerAttackState.TryEnter(PlayerMovementPlayerController);
            if (!entered)
            {
                _isEntryAttackPending = false;
            }

            return entered;
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

            // §5.2 등장 변형 — 타깃 상태로 변형을 고르도록 combat에 전달(클리어 전에).
            _combat.SetPendingEntryTarget(_entryAttackTarget);

            _entryAttackQueued = false;
            _entryAttackTarget = null;
            TryStartEntryAttack();
        }

        private void ConsumeSwapEvadeQueue()
        {
            string state = MovementController?.CurrentState?.StateName;
            if (state == "Hit" || state == "Death" || state == "Grabbed" || state == "Knockdown")
            {
                _swapEvadeQueued = false;
                _swapEvadeTarget = null;
                return;
            }

            if (_swapEvadeTarget != null && _swapEvadeTarget.IsAlive())
            {
                Vector3 toTarget = _swapEvadeTarget.transform.position - transform.position;
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude > 0.0001f)
                    transform.rotation = Quaternion.LookRotation(toTarget);
            }

            PlaySwapEvadeFeedback(_swapEvadeTarget);

            _combat.SetPendingSwapAttackTarget(_swapEvadeTarget);
            _swapEvadeQueued = false;
            _swapEvadeTarget = null;
            TryStartSwapEvadeCounterAttack();
            Debug.Log("[PlayerActor] 스왑 회피 카운터 발동");
        }

        private void PlaySwapEvadeFeedback(MonsterActor target)
        {
            var party = PartyManager.Instance;
            if (party == null) return;

            Vector3 fxPos = TryGetSocket(party.SwapEvadeFxSocket, out var socket)
                ? socket.position
                : transform.position;
            fxPos += party.SwapEvadeFxOffset;

            if (party.SwapEvadeEnableHitStop && party.SwapEvadeHitStopDuration > 0f)
                GameCombatManager.Instance?.GameHitStop?.Execute(
                    party.SwapEvadeHitStopDuration,
                    party.SwapEvadeHitStopTimeScale);

            CameraManager.Instance?.CombatCamera?.PlayDodgeCounter(
                target != null ? target.transform : null,
                party.SwapEvadeCameraShakeKey);

            if (!string.IsNullOrWhiteSpace(party.SwapEvadeFxKey))
                GameObjectManager.Instance?.ShowFX(party.SwapEvadeFxKey, fxPos, transform.rotation);

            if (party.SwapEvadeSpawnDodgeVitalOrb)
                GameCombatManager.Instance?.GameVitalOrb?.TrySpawn(VitalOrbTrigger.Dodge, fxPos);
        }

        private bool TryStartSwapEvadeCounterAttack()
        {
            _isSwapEvadeCounterAttackPending = true;

            bool entered = PlayerMovementPlayerController != null
                           && PlayerAttackState.TryEnter(PlayerMovementPlayerController);
            if (!entered)
                _isSwapEvadeCounterAttackPending = false;

            return entered;
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

        public bool ConsumeSwapEvadeCounterAttackPending()
        {
            if (!_isSwapEvadeCounterAttackPending) return false;
            _isSwapEvadeCounterAttackPending = false;
            return true;
        }

        public bool ConsumeSwapSpecialAttackPending()
        {
            if (!_isSwapSpecialAttackPending) return false;
            _isSwapSpecialAttackPending = false;
            return true;
        }

        /// <summary> 등장 공격 대기 여부를 소비하지 않고 조회 (PlayerAttackState 진입 가능 판정용). </summary>
        public bool IsEntryAttackPending => _isEntryAttackPending;
        public bool IsSwapEvadeCounterAttackPending => _isSwapEvadeCounterAttackPending;
        public bool IsSwapSpecialAttackPending => _isSwapSpecialAttackPending;
    }

    // Component
    public partial class PlayerActor : GameActor, IDamageable
    {
        public PlayerEquipment GetPlayerEquipment() => _equipment;
        public PlayerCombat    GetCombat()          => _combat;

        // 연계 라우트 입력 토큰 스트림. 상태(대시/점프/공격)가 발동 확정 시 토큰을 push하고,
        // PlayerAttackState가 Resolve로 라우트를 판정한다. 상태 전환을 넘어 생존한다.
        private ComboInputTracker _comboInputTracker;
        public ComboInputTracker ComboInputTracker => _comboInputTracker ??= new ComboInputTracker();

        /// <summary>
        /// 모델 교체 시 PlayerSwapBehaviour가 호출.
        /// 이전 캐릭터 상태를 저장하고 새 캐릭터 데이터로 컴포넌트를 일괄 갱신한다.
        /// </summary>
        public void RefreshForCharacter(
            CharacterModelData data,
            ActorAnimator.MotionPlaybackSnapshot animationSnapshot = default)
        {
            CharacterActorType previousType = _characterActorType;
            float previousMaxHealth = _maxHealth;
            float previousCurrentHealth = _currentHealth;
            bool wasPreviousHealthFull = previousMaxHealth > 0f
                                         && previousCurrentHealth >= previousMaxHealth - 0.01f;

            // 현재 캐릭터 상태 저장. 씬 직렬화 값은 실제 활성 모델과 다를 수 있으므로
            // 런타임에서 한 번 이상 정상 초기화된 뒤에만 이전 캐릭터 상태로 인정한다.
            if (_hasInitializedCharacterRuntime && _characterActorType != CharacterActorType.None)
            {
                _characterHealthMap[_characterActorType] = _currentHealth;
                _characterSkillMap[_characterActorType]  = _skillGauge.CurrentGauge;
                _characterSkillCooldownMap[_characterActorType] = _skillGauge.GetCooldownRemainingSnapshot();
                _combat?.SaveComboState(_characterActorType);
            }

            _characterActorType = data.characterType;
            _hasInitializedCharacterRuntime = true;

            // 연계 토큰 스트림은 캐릭터 종속 — 교체 시 비운다(설계 §8).
            _comboInputTracker?.Clear();

            // 성장 스탯 적용 후 체력 복원 (처음 등장 시 최대치)
            _maxHealth = ApplyCharacterStats(data);
            if (_characterHealthMap.TryGetValue(data.characterType, out var hp))
            {
                // 초기화 순서상 기본 maxHp(100) 풀피 상태가 먼저 저장된 뒤 성장 스탯 maxHp(예: 120)가
                // 적용될 수 있다. 이전 max 기준 풀피였다면 새 max 기준 풀피로 유지한다.
                _currentHealth = previousType == data.characterType && wasPreviousHealthFull
                    ? _maxHealth
                    : Mathf.Clamp(hp, 0f, _maxHealth);
            }
            else
            {
                _currentHealth = _maxHealth;
            }

            // 스킬 게이지 복원
            _skillGauge.SetGauge(
                _characterSkillMap.TryGetValue(data.characterType, out var sg) ? sg : 0f);
            _skillGauge.SetCooldownRemainingSnapshot(
                _characterSkillCooldownMap.TryGetValue(data.characterType, out var cooldowns) ? cooldowns : null);

            // 활성 Model의 컴포넌트 참조 갱신
            _animator            = GetComponentInChildren<ActorAnimator>();
            
            _playerActorAnimator = _animator as PlayerActorAnimator;
            _equipment           = GetComponentInChildren<PlayerEquipment>();
            _equipment?.RefreshWeaponConstraintsFromModel();
            _equipment?.SetWeaponType(data.defaultWeaponType);
            
            // 애니메이터에 Actor 재주입 (PlayerEquipment 참조 포함)
            _playerActorAnimator?.Init(this);

            // 전투 컴포넌트 참조 갱신 + 공격 데이터 교체
            _combat.RefreshComponentReferences();
            var partyManager = PartyManager.Instance;
            _combat.RefreshAttackData(
                data.attackData,
                data.characterType,
                partyManager == null || partyManager.PreserveComboStatePerCharacter,
                partyManager != null ? partyManager.ComboStateMaxCarryTime : 1.8f);
            _combatWeaponStateController?.RefreshReferences();

            // 새 모델의 ParentConstraint 기본 weight는 prefab 세팅에 의존하므로,
            // 현재 전투 상태에 맞춰 weight + 플래그를 강제 동기화한다.
            _equipment?.ForceSyncMainWeaponState(_combat != null && _combat.IsInCombat);

            // 모델별 공용 소켓
            RefreshSockets(data);

            // 비주얼 효과 컴포넌트 재초기화
            _colorChanger.InitializeRendererData();
            _dissolveController.RefreshRenderers();

            // Foot IK
            _footIK.Refresh(data.AnimancerComponent?.Animator);

            // 모델 교체 전 재생 중이던 MotionSet이 있으면 같은 AnimKey의 진행률로 복원한다.
            // 초기화/복원 실패 시에는 기존처럼 Idle을 강제로 한 번 재생해 새 Animancer에 포즈를 적용한다.
            bool restoredAnimation = _playerActorAnimator != null
                                     && animationSnapshot.IsValid
                                     && _playerActorAnimator.RestorePlaybackSnapshot(animationSnapshot);
            if (!restoredAnimation)
            {
                PlayerMovementPlayerController?.TransitionToState(new PlayerIdleState(PlayerMovementPlayerController));
                _playerActorAnimator?.PlayMotion(AnimKey.Idle, 0f);
            }

            OnHpChanged?.Invoke(_currentHealth, _maxHealth);
        }

        private void RefreshSockets(CharacterModelData data)
        {
            _socketDict ??= new SerializedDictionary<ActorSocketType, Transform>();
            _socketDict.Clear();

            if (data?.SocketDict == null)
                return;

            foreach (var pair in data.SocketDict)
            {
                if (pair.Key == ActorSocketType.None || pair.Value == null)
                    continue;

                _socketDict[pair.Key] = pair.Value;
            }
        }

        private float ApplyCharacterStats(CharacterModelData data)
        {
            CharacterActorType type = data != null ? data.characterType : _characterActorType;
            IReadOnlyDictionary<StatType, float> growthStats = PartyManager.Instance?.GetGrowthStats(type);

            if (growthStats != null && growthStats.Count > 0)
            {
                Stats?.Init(null);
                foreach (KeyValuePair<StatType, float> pair in growthStats)
                    Stats?.SetBase(pair.Key, pair.Value);
                return Mathf.Max(1f, Stats != null ? Stats.MaxHealth : ActorStatSO.GetDefault(StatType.MaxHealth));
            }

            if (Definition != null && Definition.statData != null)
            {
                Stats?.Init(Definition.statData);
                return Mathf.Max(1f, Stats != null ? Stats.MaxHealth : Definition.statData.GetBase(StatType.MaxHealth));
            }

            Stats?.Init(null);
            return Mathf.Max(1f, Stats != null ? Stats.MaxHealth : ActorStatSO.GetDefault(StatType.MaxHealth));
        }

        /// <summary>
        /// 전투 중 레벨업 등으로 활성 캐릭터의 성장 스탯을 즉시 반영한다.
        /// 기둥 A: base 스탯만 교체(SetBase)하여 장비/버프 modifier를 보존한다. Init()을 호출하지 않는다.
        /// 레벨업 정책에 따라 HP/Poise는 풀 회복한다.
        /// </summary>
        public void RefreshGrowthStatsLive(IReadOnlyDictionary<StatType, float> growthStats)
        {
            if (growthStats == null || growthStats.Count == 0) return;

            // 다운된(HP 0) 활성 캐릭터는 레벨업으로 부활시키지 않는다(벤치 경로와 대칭).
            // 사망 중에는 스왑이 막혀 게임오버→리로드 시 ApplyCharacterStats가 커밋된 레벨로 스탯을 재구성하므로 손실 없음.
            if (!IsAlive()) return;

            foreach (KeyValuePair<StatType, float> pair in growthStats)
                Stats?.SetBase(pair.Key, pair.Value);   // Init() 미호출 → modifier 유지

            _maxHealth     = Mathf.Max(1f, Stats != null ? Stats.MaxHealth : _maxHealth);
            _currentHealth = _maxHealth;                // 풀 회복
            OnHpChanged?.Invoke(_currentHealth, _maxHealth);

            // Poise 풀 회복(브레이크 해제 포함). MaxPoise 성장은 SetBase가 이미 반영.
            GetComponent<PoiseStat>()?.RecoverFull();
        }

        /// <summary>
        /// 벤치(대기 중) 캐릭터가 레벨업했을 때의 갱신. 화면에 모델이 없으므로 스탯 컨테이너는 건드리지 않고,
        /// 저장된 현재 HP만 새 최대치로 풀 회복한다(다음 스왑 시 풀 HP로 등장). 기둥 B.
        /// </summary>
        public void UpdateBenchedGrowth(CharacterActorType type, IReadOnlyDictionary<StatType, float> growthStats)
        {
            if (type == CharacterActorType.None || type == _characterActorType) return;
            if (growthStats == null) return;

            // 기록이 없으면(한 번도 피해를 입지 않음) 이미 풀피로 취급되므로 손대지 않는다.
            // 다운된(HP 0) 멤버는 레벨업으로 부활시키지 않는다.
            if (!_characterHealthMap.TryGetValue(type, out float stored) || stored <= 0f) return;

            float newMax = growthStats.TryGetValue(StatType.MaxHealth, out float max)
                ? Mathf.Max(1f, max)
                : GetMaxHealthForCharacter(type);
            _characterHealthMap[type] = newMax;          // 살아있는 대기 멤버만 풀 회복
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

            IReadOnlyDictionary<StatType, float> growthStats = PartyManager.Instance?.GetGrowthStats(type);
            if (growthStats != null && growthStats.TryGetValue(StatType.MaxHealth, out float maxHealth))
                return Mathf.Max(1f, maxHealth);

            return Mathf.Max(1f, ActorStatSO.GetDefault(StatType.MaxHealth));
        }

        /// <summary>지정 캐릭터를 풀 회복. reviveDowned=true면 HP 0(다운) 멤버도 되살린다.</summary>
        public void HealCharacterToFull(CharacterActorType type, bool reviveDowned)
        {
            if (type == _characterActorType)
            {
                // 액티브: 다운 상태면 player는 이미 사망 플로우이므로 정상 케이스는 생존.
                // 풀 회복 (Heal은 IsAlive 가드 → 직접 세팅으로 부활까지 커버)
                if (!reviveDowned && !IsAlive()) return;
                float old = _currentHealth;
                _currentHealth = _maxHealth;
                if (_currentHealth > old)
                {
                    OnHpChanged?.Invoke(_currentHealth, _maxHealth);
                    UIManager.Instance.ShowDamageFloaterHeal(transform.position, _currentHealth - old);
                }
                return;
            }

            // 벤치: _characterHealthMap 직접 기록 (이벤트 안 나감 → HUD 별도 갱신 필요)
            float max = GetMaxHealthForCharacter(type);
            bool hasRecord = _characterHealthMap.TryGetValue(type, out float stored);
            if (!hasRecord) return;                       // 기록 없음 = 이미 풀피
            if (!reviveDowned && stored <= 0f) return;    // 부활 비활성 시 다운 제외
            _characterHealthMap[type] = max;
        }

        public float GetSkillGaugeForCharacter(CharacterActorType type)
        {
            if (type == _characterActorType) return _skillGauge != null ? _skillGauge.CurrentGauge : 0f;
            return _characterSkillMap.TryGetValue(type, out var gauge) ? gauge : 0f;
        }

        public float GetMaxSkillGaugeForCharacter(CharacterActorType type)
            => _skillGauge != null ? _skillGauge.MaxGauge : 1f;

        public bool IsSkillGaugeFullForCharacter(CharacterActorType type)
        {
            float max = GetMaxSkillGaugeForCharacter(type);
            return max > 0f && GetSkillGaugeForCharacter(type) >= max;
        }

        public bool CanUseSkillForCharacter(CharacterActorType type, int skillSlot)
        {
            if (type == CharacterActorType.None || _skillGauge == null) return false;

            if (type == _characterActorType)
                return _skillGauge.CanUseSkill(skillSlot);

            float cost = _skillGauge.GetSkillCost(skillSlot);
            if (float.IsInfinity(cost) || GetSkillGaugeForCharacter(type) < cost)
                return false;

            if (_characterSkillCooldownMap.TryGetValue(type, out var cooldowns)
                && cooldowns != null
                && (uint)skillSlot < (uint)cooldowns.Length
                && cooldowns[skillSlot] > 0f)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 파티 HUD의 궁극기 "준비" 표시용 판정. 실제 발동은 비용(<see cref="PlayerSkillGauge.CanUseSkill"/>)으로
        /// 게이트하지만, 글로우 같은 준비 연출은 게이지가 가득 찼을 때만 켠다
        /// (스킬바의 <c>UISkillSlot._showOnlyWhenGaugeFull</c>와 동일 의미). 쿨타임 중이면 false.
        /// </summary>
        public bool IsUltimateReadyForCharacter(CharacterActorType type)
            => IsSkillGaugeFullForCharacter(type)
               && CanUseSkillForCharacter(type, PlayerSkillGauge.UltimateSkillSlot);

        public void AddSkillGaugeForCharacter(CharacterActorType type, float amount)
        {
            if (type == CharacterActorType.None || amount <= 0f || _skillGauge == null) return;

            if (type == _characterActorType)
            {
                _skillGauge.AddGauge(amount);
                return;
            }

            float max = _skillGauge.MaxGauge;
            float current = _characterSkillMap.TryGetValue(type, out var gauge) ? gauge : 0f;
            _characterSkillMap[type] = Mathf.Clamp(current + amount, 0f, max);
        }

        public bool ConsumeFullSkillGaugeForCharacter(CharacterActorType type)
        {
            if (!IsSkillGaugeFullForCharacter(type)) return false;

            if (type == _characterActorType)
                _skillGauge.SetGauge(0f);
            else
                _characterSkillMap[type] = 0f;

            return true;
        }

        private void InitComponents()
        {
            if (_combat     == null) _combat     = GetComponent<PlayerCombat>();
            if (_equipment  == null) _equipment  = GetComponentInChildren<PlayerEquipment>();
            if (_skillGauge == null) _skillGauge = GetComponent<PlayerSkillGauge>();
            if (_combatWeaponStateController == null)
                _combatWeaponStateController = gameObject.GetOrAddComponent<PlayerCombatWeaponStateController>();
            if (_footIK     == null) _footIK     = GetComponent<FootIKController>();
            if (_swapBehaviour == null) _swapBehaviour = GetComponent<PlayerSwapBehaviour>();
            if (_behaviorPredictor == null) _behaviorPredictor = gameObject.GetOrAddComponent<PlayerBehaviorPredictor>();

            if (_skillGauge != null)
                _skillGauge.OnGaugeChanged += (cur, max) => OnSkillGaugeChanged?.Invoke(cur, max);
            // SetCombatStateProvider는 OnEnable/OnDisable에서 관리
        }

        public void EnsureCharacterRuntimeInitialized()
        {
            EnsureInitialCharacterModelInitialized();
        }

        private void EnsureInitialCharacterModelInitialized()
        {
            CharacterModelData activeModel = null;
            var models = GetComponentsInChildren<CharacterModelData>(true);
            for (int i = 0; i < models.Length; i++)
            {
                if (models[i] != null && models[i].gameObject.activeInHierarchy)
                {
                    activeModel = models[i];
                    break;
                }
            }

            if (activeModel == null)
                return;

            bool needsRefresh =
                !_hasInitializedCharacterRuntime ||
                _characterActorType != activeModel.characterType ||
                _equipment == null ||
                _equipment.GetMainWeaponType() != activeModel.defaultWeaponType;

            if (needsRefresh)
                RefreshForCharacter(activeModel);
        }
    }

    // IDamageable
    public partial class PlayerActor : GameActor, IDamageable
    {
        public void TakeDamage(AttackData attackData)
        {
            CombatResult combatResult = CombatResolutionPipeline.ResolvePlayerHit(
                this,
                attackData,
                CreatePlayerDefenseQuery());

            switch (combatResult.DefenseOutcome)
            {
                case DefenseOutcome.Guarded:
                    CombatResolutionPipeline.RecordIfObservable(combatResult);
                    if (MovementController.CurrentState is not PlayerGuardState guardState)
                        return;

                    guardState.OnAttackBlocked(attackData);

                    if (!_combat.IsGuarding)
                        OnGuardBrokenDamage(attackData);
                    return;

                case DefenseOutcome.Parried:
                    CombatResolutionPipeline.RecordIfObservable(combatResult);
                    OnParrySuccess(attackData);
                    return;

                case DefenseOutcome.PerfectDodged:
                    CombatResolutionPipeline.RecordIfObservable(combatResult);
                    TryPerfectDodge(attackData);
                    return;

                case DefenseOutcome.Invincible:
                    CombatResolutionPipeline.RecordIfObservable(combatResult);
                    TryDashEvadeFeedback(attackData);
                    return;
            }

            DamageResult damageResult = combatResult.Damage;
            float finalDamage = combatResult.FinalDamage;

            _currentHealth = MathF.Max(0, _currentHealth - finalDamage);
            OnHpChanged?.Invoke(_currentHealth, _maxHealth);
            _behaviorPredictor?.NotifyAction(PlayerActionToken.Hit);

            CombatFeedbackDispatcher.ShowDamageFloater(
                CombatFeedbackContext.FromCombatResult(combatResult, transform.position));

            if (_currentHealth <= 0)
            {
                CombatResolutionPipeline.RecordIfDamageApplied(
                    CombatResolutionPipeline.WithReaction(combatResult, ReactionDecision.None));
                OnDeath(attackData);
                return;
            }

            ReactionDecision reactionDecision = OnDamaged(attackData);
            CombatResolutionPipeline.RecordIfDamageApplied(
                CombatResolutionPipeline.WithReaction(combatResult, reactionDecision));
        }

        public bool      IsAlive()          => _currentHealth > 0;
        public Transform GetTransform()     => transform;
        public void      LockOn()           { }
        public void      UnLockOn()         { }
        public float     GetHealthPercent() => _currentHealth / _maxHealth;
        public float     GetCurrentHealth() => _currentHealth;

        public void SetInvincible(bool invincible) => _isInvincible = invincible;

        public bool CanTakeDamage()
            => IsAlive()
               && !_isInvincible
               && !IsSwapEvadeInvincible
               && !MovementController.CurrentState.GrantsInvincibility;

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

        private PlayerDefenseQuery CreatePlayerDefenseQuery()
        {
            bool alwaysParry = CheatManager.Instance?.IsAlwaysParryEnabled ?? false;
            bool isAttackState = MovementController.CurrentState.StateName == "Attack";
            bool isCurrentAttackParryCapable = _combat.CurrentAttackData?.attackKind == AttackKind.NormalAttack;

            return new PlayerDefenseQuery(
                _combat.IsGuarding,
                MovementController.CurrentState is PlayerGuardState,
                isAttackState,
                _combat.IsPossibleCollide,
                isCurrentAttackParryCapable,
                MovementController.CurrentState is PlayerDodgeState,
                _combat.IsPerfectDodgeWindow,
                CanTakeDamage(),
                alwaysParry,
                _combat.IsAssistParryWindow,
                Definition != null ? Definition.combatDefensePolicy : null);
        }

        private void OnParrySuccess(AttackData attackData)
        {
            // 어시스트 패리(§4.3)로 성립한 패리면 어시스트 창을 닫고 폴백(즉시공격)을 취소한다.
            // (일반 클래시 패리/퍼펙트 가드 반격창과 중복 발동 방지 = 보존 제약)
            if (_combat.IsAssistParryWindow)
            {
                _combat.CloseAssistParryWindow();
                _assistParryFallbackPending = false;
                Debug.Log("[PlayerActor] 어시스트 패리 성공!");
            }
            else
            {
                Debug.Log("[PlayerActor] 패리 성공!");
            }

            var defenseFeedback = GameCombatManager.Instance?.DefenseSuccessFeedback;

            // 패리 반격 창을 먼저 열어둬야 상태 전환 후 반격 입력을 받을 수 있다
            _combat.OpenParryCounterWindow(
                defenseFeedback?.GetCounterWindowDuration(DefenseSuccessType.Parry) ?? -1f);

            // 히트 감지를 즉시 비활성화해 이후 PerformHitDetection이 HitStop을 덮어쓰지 않도록 한다
            _combat.SetEnableCollision(false);

            // 공격 상태를 중단하고 Idle로 복귀 (패리 반격 창은 이미 열려 있으므로 다음 공격 입력 시 반격 발동)
            MovementController.TransitionToState(new PlayerIdleState(MovementController));

            Vector3 fxPos = TryGetSocket(ActorSocketType.Weapon, out var center)
                ? center.position
                : (attackData?.hitPoint ?? Vector3.zero) != Vector3.zero
                    ? attackData.hitPoint
                    : transform.position;

            // 공격자(몬스터) 경직
            if (attackData?.attacker is MonsterActor monster)
                monster.OnParried();

            defenseFeedback?.Play(
                DefenseSuccessType.Parry,
                new DefenseSuccessFeedbackContext(
                    this,
                    attackData?.attacker,
                    attackData,
                    fxPos,
                    _parryFxName));
        }

        /// <summary>
        /// 도지 중 피격 시도 시 호출. 퍼펙트 도지 판정 창 내면 보상 효과를 발동한다.
        /// </summary>
        private void TryPerfectDodge(AttackData attackData)
        {
            if (!_combat.IsPerfectDodgeWindow) return;

            // 퍼펙트 도지 성공 — 창 즉시 닫아 중복 발동 방지
            _combat.ClosePerfectDodgeWindow();

            var defenseFeedback = GameCombatManager.Instance?.DefenseSuccessFeedback;
            _combat.OpenDodgeCounterWindow(
                attackData,
                defenseFeedback?.GetCounterWindowDuration(DefenseSuccessType.PerfectDodge) ?? -1f);

            Vector3 feedbackPos = TryGetSocket(ActorSocketType.Center, out var center)
                ? center.position
                : transform.position;

            defenseFeedback?.Play(
                DefenseSuccessType.PerfectDodge,
                new DefenseSuccessFeedbackContext(
                    this,
                    attackData?.attacker,
                    attackData,
                    feedbackPos));

            Debug.Log("[PlayerActor] 퍼펙트 도지 성공!");
        }

        /// <summary>
        /// Dash로 적 공격을 회피했을 때 타임스케일/카메라 연출을 발동한다.
        /// 퍼펙트 도지 피드백 핸들러를 재사용하되, 대시는 반격 창을 열지 않는다(연출만).
        /// 회피 판정 자체는 Dash가 GrantsInvincibility라 DefenseOutcome.Invincible로 들어온다.
        /// </summary>
        private void TryDashEvadeFeedback(AttackData attackData)
        {
            if (MovementController.CurrentState is not PlayerDashState dashState) return;
            if (!dashState.TryConsumeEvadeFeedback()) return;

            var defenseFeedback = GameCombatManager.Instance?.DefenseSuccessFeedback;
            if (defenseFeedback == null) return;

            Vector3 feedbackPos = TryGetSocket(ActorSocketType.Center, out var center)
                ? center.position
                : transform.position;

            // 대시 회피는 포스트프로세스(볼륨) 플래시 없이 타임스케일 슬로우만 또렷하게 발동한다.
            defenseFeedback.PlayDashEvade(
                new DefenseSuccessFeedbackContext(
                    this,
                    attackData?.attacker,
                    attackData,
                    feedbackPos));

            Debug.Log("[PlayerActor] 대시 회피 피드백 발동!");
        }

        /// <summary>
        /// 가드 브레이크 시 호출.
        /// GuardBreakState가 경직·애니를 담당하므로 State 전환 없이 데미지·피드백만 처리한다.
        /// </summary>
        private void OnGuardBrokenDamage(AttackData attackData)
        {
            if (!CanTakeDamage()) return;

            CombatResult combatResult = CombatResolutionPipeline.ResolvePlayerGuardBreakDamage(this, attackData);
            DamageResult damageResult = combatResult.Damage;
            float finalDamage = combatResult.FinalDamage;

            _currentHealth = MathF.Max(0, _currentHealth - finalDamage);
            OnHpChanged?.Invoke(_currentHealth, _maxHealth);

            CombatFeedbackDispatcher.ShowDamageFloater(
                CombatFeedbackContext.FromCombatResult(combatResult, transform.position));
            CombatResolutionPipeline.RecordIfDamageApplied(combatResult);

            CameraManager.Instance.StartShake(_shakeKeyHeavyHit);

            Vector3 fxPos = TryGetSocket(ActorSocketType.Center, out var center)
                ? center.position
                : attackData.hitPoint;
            CombatFeedbackDispatcher.ShowHitFx(attackData.hitParticleName, fxPos);

            CombatFeedbackDispatcher.ApplyColorHit(_colorChanger);

            if (_currentHealth <= 0)
                OnDeath(attackData);
        }

        /// <summary>
        /// 피격 시 호출.
        /// 쉐이크 강도는 AttackReactionType으로 결정한다.
        /// </summary>
        protected virtual ReactionDecision OnDamaged(AttackData attackData)
        {
            // 슈퍼아머 체크: 한 단계 이상 차징 완료 시 물리 충격(밀려남) 및 상태 전환 무시
            bool hasSuperArmor = MovementController.CurrentState is PlayerChargeState chargeState &&
                                 chargeState.HasChargedAtLeastOneStage;
            bool suppressHitReaction = MovementController.CurrentState.SuppressesHitReaction;
            bool ignoreHitReaction = hasSuperArmor || suppressHitReaction;
            string stateName = MovementController.CurrentState.StateName;
            ReactionDecision reactionDecision = ReactionResolver.ResolvePlayerReaction(
                new PlayerReactionQuery(
                    ignoreHitReaction,
                    MovementController.CurrentState.CanTransitionState("Hit"),
                    stateName is "Hit" or "Grabbed",
                    ShouldEnterAirborneState(attackData),
                    IsStaggerImmune),
                attackData);

            if (reactionDecision.ShouldApplyForce && attackData != null)
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
                        Vector3 airborneVelocity = ShouldEnterAirborneState(attackData)
                            ? Vector3.up * attackData.airborneForce
                            : Vector3.zero;
                        MovementController.AddImpulse(
                            launchDir * attackData.knockbackForce + airborneVelocity,
                            attackData.knockbackDrag);
                        break;
                    }

                    case AttackReactionType.Grab:
                        break;
                }
            }

            if (reactionDecision.ShouldEnterState)
            {
                ApplyPlayerReactionState(reactionDecision.TargetState, attackData);
            }

            if (reactionDecision.ShouldPlayCameraFeedback)
            {
                bool isHeavyReaction = attackData?.reactionType is
                    AttackReactionType.Heavy or
                    AttackReactionType.KnockBack or
                    AttackReactionType.Airborne or
                    AttackReactionType.Knockdown or
                    AttackReactionType.Stun;

                CombatFeedbackDispatcher.ApplyPlayerDamagedCamera(
                    isHeavyReaction,
                    _shakeKeyHit,
                    _shakeKeyHeavyHit);
            }

            // 경직 내성으로 흡수된 약한 피격(Light/Hit)은 히트스톱도 생략한다.
            // 그러지 않으면 리액션은 억제돼도 LocalTimeScale이 freeze/clear로 깜빡여
            // 흡수 구간 조작감이 끊긴다("데미지 O·경직 X" 의도를 체감으로 완성).
            // 컬러 플래시·HitFx는 아래에서 그대로 유지해 피격 자체는 시각 피드백한다.
            bool absorbedByStaggerImmunity = IsStaggerImmune
                && attackData != null
                && ReactionResolver.IsMinorPlayerReaction(attackData.reactionType);
            if (!absorbedByStaggerImmunity)
                CombatFeedbackDispatcher.ApplyPlayerDamagedHitStop(attackData, this);

            Vector3 fxPos = TryGetSocket(ActorSocketType.Center, out var center)
                ? center.position
                : (attackData?.hitPoint ?? transform.position);
            CombatFeedbackDispatcher.ShowHitFx(attackData?.hitParticleName, fxPos);

            CombatFeedbackDispatcher.ApplyColorHit(_colorChanger);
            return reactionDecision;
        }

        private void ApplyPlayerReactionState(CombatReactionState reactionState, AttackData attackData)
        {
            switch (reactionState)
            {
                case CombatReactionState.Airborne:
                    MovementController.TransitionToState(new PlayerAirborneState(MovementController));
                    break;
                case CombatReactionState.Grabbed:
                    MovementController.TransitionToState(new PlayerGrabbedState(MovementController, attackData));
                    break;
                case CombatReactionState.Stun:
                    MovementController.TransitionToState(new PlayerStunState(MovementController, attackData));
                    break;
                case CombatReactionState.Knockdown:
                    MovementController.TransitionToState(new PlayerKnockdownState(MovementController, attackData));
                    break;
                case CombatReactionState.Hit:
                    MovementController.TransitionToState(new PlayerHitState(MovementController, attackData));
                    break;
            }
        }

        private bool ShouldEnterAirborneState(AttackData attackData)
        {
            if (attackData == null || attackData.reactionType != AttackReactionType.Airborne)
                return false;

            if (attackData.airborneForce >= MinAirborneStateForce)
                return true;

            return false;
        }

        /// <summary>
        /// 사망 시 호출.
        /// </summary>
        protected virtual void OnDeath(AttackData attackData)
        {
            Debug.Log($"[PlayerActor] {gameObject.name} 사망!");
            CombatTelemetrySession.NotifyPlayerDeath(this);
            ClearAllInputState();
            PlayerMovementPlayerController?.ClearInputAll();
            InputManager.Instance?.InputBuffer?.Clear();
            CombatFeedbackDispatcher.ApplyPlayerDeathFeedback(_shakeKeyDeath);
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
            _behaviorPredictor?.ResetHistory();
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
