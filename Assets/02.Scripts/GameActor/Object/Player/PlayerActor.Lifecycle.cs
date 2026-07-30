using System;
using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using UnityEngine;
using UnityEngine.InputSystem;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Path;
using UPlayGround.Animation;
using UPlayGround.Components;
using UPlayGround.Data;
using UPlayGround.Data.Combat;
using UPlayGround.Data.Event;
using UPlayGround.Data.Stat;
using UPlayGround.MovementController;
using UPlayGround.Input;
using UPlayGround.InputDefine;
using UPlayGround.Manager;
using UPlayGround.Combat;
using UPlayGround.State;
using UPlayGround.UI;
using Random = UnityEngine.Random;
using UPlayGround.AI.CombatDecision;

namespace UPlayGround
{
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
            CameraMgr?.SetCombatStateProvider(() => _combat != null && _combat.IsInCombat);
        }

        private void OnDisable()
        {
            UnRegisterInputEvents();
            CameraMgr?.SetCombatStateProvider(null);
            ClearAllInputState();
        }

        protected override void OnDestroy()
        {
            // OnDisable이 먼저 호출되므로 여기서는 추가 정리만 담당
            UnRegisterInputEvents();
            CameraMgr?.SetCombatStateProvider(null);
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
            bool isInteractHeld = InputMgr != null
                                  && InputMgr.GetAction(InputMapNames.PlayerAction, PlayerAction.Interact, out InputAction interactAction)
                                  && interactAction.IsPressed();
            ResolveGuardChordInput();

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
                InteractHeld     = isInteractHeld,
                GuardInput       = _guardInputCondition,
                DashInput        = _dashInputCondition,
                ChargeAttackHeld = _chargeAttackHeld && _chargeHoldTime >= ChargeThreshold,
                ChargeHoldTime   = _chargeHoldTime,
                SkillInput = CreateSkillInputSnapshot(),
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

        private List<InputCondition> CreateSkillInputSnapshot()
        {
            var snapshot = new List<InputCondition>(_skillInputCondition.Count);
            for (int i = 0; i < _skillInputCondition.Count; i++)
            {
                InputCondition state = _skillInputCondition[i];
                snapshot.Add(
                    state == InputCondition.None && _skillInputHeld[i]
                        ? InputCondition.Handled
                        : state);
            }
            return snapshot;
        }

        #endregion
    }
}
