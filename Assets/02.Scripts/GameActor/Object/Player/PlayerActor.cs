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

        [SerializeField] private PlayerEquipment  _equipment;
        [SerializeField] private PlayerCombat     _combat;
        [SerializeField] private PlayerSkillGauge _skillGauge;
        [SerializeField] private FootIKController _footIK;

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
            RegisterInputEvents();

            switch (_characterActorType)
            {
                case CharacterActorType.Bokusei: _equipment?.SetWeaponType(WeaponType.Katana);    break;
                case CharacterActorType.Honoka:  _equipment?.SetWeaponType(WeaponType.DoubleAxe); break;
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

            if (_chargeAttackHeld)
                _chargeHoldTime += Time.deltaTime;

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
        private void RegisterInputEvents()
        {
            if (!InputManager.Instance) return;

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
            if (!InputManager.Instance) return;

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
    }

    // Component
    public partial class PlayerActor : GameActor, IDamageable
    {
        public PlayerEquipment GetPlayerEquipment() => _equipment;
        public PlayerCombat    GetCombat()          => _combat;

        private void InitComponents()
        {
            if (_combat     == null) _combat     = GetComponent<PlayerCombat>();
            if (_equipment  == null) _equipment  = GetComponent<PlayerEquipment>();
            if (_skillGauge == null) _skillGauge = GetComponent<PlayerSkillGauge>();
            if (_footIK    == null) _footIK    = GetComponent<FootIKController>();

            if (_skillGauge != null)
                _skillGauge.OnGaugeChanged += (cur, max) => OnSkillGaugeChanged?.Invoke(cur, max);

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
                if (MovementController.CurrentState.StateName == "Dodge")
                    TryPerfectDodge(attackData);
                return;
            }

            float finalDamage = attackData.damage;
            
            if (attackData.criticalMultiplier > 1.0f)
            {
                finalDamage *= attackData.criticalMultiplier;
                Debug.Log($"[PlayerActor] 크리티컬 히트! 데미지: {finalDamage}");
            }

            _currentHealth = MathF.Max(0, _currentHealth - finalDamage);
            OnHpChanged?.Invoke(_currentHealth, _maxHealth);
            Debug.Log($"[PlayerActor] {gameObject.name}가 {finalDamage} 데미지 (남은 HP: {_currentHealth}/{_maxHealth})");

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
        {
            string s = MovementController.CurrentState.StateName;
            if (s == "Dodge" || s == "Dash" || s == "FinishAttack") return false;
            return IsAlive() && !_isInvincible;
        }

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
        /// Attack / Charge 상태에서 히트박스가 활성 상태일 때 피격되면 패리를 시도한다.
        /// CheatManager.IsAlwaysParryEnabled가 켜져 있으면 조건 없이 패리한다.
        /// </summary>
        private bool TryParry(AttackData attackData)
        {
            bool alwaysParry = CheatManager.Instance?.IsAlwaysParryEnabled ?? false;

            if (!alwaysParry)
            {
                string stateName = MovementController.CurrentState.StateName;
                if (stateName != "Attack" && stateName != "Charge")
                    return false;
                if (!_combat.IsPossibleCollide)
                    return false;
            }

            OnParrySuccess(attackData);
            return true;
        }

        private void OnParrySuccess(AttackData attackData)
        {
            Debug.Log("[PlayerActor] 패리 성공!");

            // 히트스톱
            GameHitStopManager.Instance.Execute(GameHitStopManager.HitStopIntensity.Heavy);

            // 카메라 피드백
            CameraManager.Instance.StartShake(_shakeKeyHeavyHit);
            if (attackData?.attackDirection != Vector3.zero)
                CameraManager.Instance.Punch(-(attackData?.attackDirection ?? Vector3.forward), 0.15f, 0.2f);

            // 패리 VFX
            Vector3 fxPos = (attackData?.hitPoint ?? Vector3.zero) != Vector3.zero
                ? attackData.hitPoint
                : transform.position;
            GameObjectManager.Instance.ShowFX(_parryFxName, fxPos);

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
            VitalOrbManager.Instance.TrySpawn(VitalOrbTrigger.Dodge, transform.position);

            // 히트스탑
            GameHitStopManager.Instance.Execute(GameHitStopManager.HitStopIntensity.PlayerGuard);

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
                        MovementController.AddImpulse(attackData.attackDirection.normalized * attackData.knockbackForce, attackData.knockbackDrag);
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
                        MovementController.AddImpulse(launchDir * attackData.knockbackForce + Vector3.up * attackData.airborneForce, attackData.knockbackDrag);
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
                    AttackReactionType.Heavy    or
                    AttackReactionType.KnockBack or
                    AttackReactionType.Airborne  or
                    AttackReactionType.Knockdown or
                    AttackReactionType.Stun;

                CameraManager.Instance.StartShake(isHeavyReaction ? _shakeKeyHeavyHit : _shakeKeyHit);
            }

            Vector3 fxPos = TryGetSocket(ActorSocketType.Center, out var center)
                ? center.position
                : (attackData?.hitPoint ?? transform.position);
            GameObjectManager.Instance.ShowFX(attackData?.hitParticleName, fxPos);

            _colorChanger.OnHit();
            Debug.Log($"[PlayerActor] 피격! HitPoint: {attackData?.hitPoint}");
        }

        /// <summary>
        /// 사망 시 호출.
        /// </summary>
        protected virtual void OnDeath(AttackData attackData)
        {
            Debug.Log($"[PlayerActor] {gameObject.name} 사망!");
            GameHitStopManager.Instance.Execute(GameHitStopManager.HitStopIntensity.PlayerDie);
            CameraManager.Instance.StartShake(_shakeKeyDeath);
            MovementController.TransitionToState(new PlayerDeathState(MovementController));
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
