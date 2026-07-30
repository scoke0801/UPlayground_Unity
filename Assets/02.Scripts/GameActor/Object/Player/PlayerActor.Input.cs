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
using UPlayGround.State;
using UPlayGround.InputDefine;
using UPlayGround.Manager;
using UPlayGround.Combat;
using UPlayGround.UI;
using Random = UnityEngine.Random;
using UPlayGround.AI.CombatDecision;

namespace UPlayGround
{
    // Input 처리
    public partial class PlayerActor : GameActor, IDamageable
    {
        private bool _isInputRegistered;

        private void RegisterInputEvents()
        {
            if (InputMgr == null || _isInputRegistered) return;
            _isInputRegistered = true;

            InputLayer layer = InputLayer.Level_0;
            var I = InputMgr;

            I.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Move,        OnInputMove,             OnInputMove,                 OnInputMove,             null,             OnMoveCanceled,  layer);
            I.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Jump,        null,                    OnInputPerformedJump,        null,                    null,             null,            layer);
            I.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Walk,        null,                    OnInputPerformedWalk,        null,                    null,             null,            layer);
            I.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Sprint,      null,                    OnInputPerformedSprint,      null,                    null,             null,            layer);
            I.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Crouching,   null,                    OnInputPerformedCrouching,   null,                    null,             null,            layer);
            I.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Dodge,       null,                    OnInputPerformedDodge,       null,                    null,             null,            layer);
            I.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Dash,        null,                    OnInputPerformedDash,        null,                    null,             null,            layer);
            I.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Attack,      null,                    OnInputPerformedAttack,      null,                    null,             null,            layer);
            I.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.HeavyAttack, OnHeavyAttackStarted,    OnInputPerformedHeavyAttack, OnHeavyAttackCanceled,   null,             null,            layer);
            I.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.SkillAbility, OnInputStartedSkill_1, OnInputPerformedSkill_1, OnInputCanceledSkill_1, null, null, layer);
            I.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.SkillUltimate, OnInputStartedSkill_2, OnInputPerformedSkill_2, OnInputCanceledSkill_2, null, null, layer);
            I.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.ElementBuff, OnInputStartedElementalImbue, OnInputPerformedElementalImbue, OnInputCanceledElementalImbue, null, null, layer);
            I.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Equip,       null,                    OnInputPerformedEquipWeapon, null,                    null,             null,            layer);
            I.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Interact,    null,                    OnInputPerformedInteraction, null,                    CanInputInteract, null,            layer);
            I.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Guard,       OnInputStartedGuard,     null,                        OnInputFinishedGuard,    null,             null,            layer);
        }

        private void UnRegisterInputEvents()
        {
            if (InputMgr == null || !_isInputRegistered) return;
            _isInputRegistered = false;

            var I = InputMgr;
            I.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Move,        OnInputMove,             OnInputMove,                 OnInputMove);
            I.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Jump,        null,                    OnInputPerformedJump,        null);
            I.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Walk,        null,                    OnInputPerformedWalk,        null);
            I.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Sprint,      null,                    OnInputPerformedSprint,      null);
            I.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Crouching,   null,                    OnInputPerformedCrouching,   null);
            I.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Dodge,       null,                    OnInputPerformedDodge,       null);
            I.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Dash,        null,                    OnInputPerformedDash,        null);
            I.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Attack,      null,                    OnInputPerformedAttack,      null);
            I.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.HeavyAttack, OnHeavyAttackStarted,    OnInputPerformedHeavyAttack, OnHeavyAttackCanceled);
            I.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.SkillAbility, OnInputStartedSkill_1, OnInputPerformedSkill_1, OnInputCanceledSkill_1);
            I.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.SkillUltimate, OnInputStartedSkill_2, OnInputPerformedSkill_2, OnInputCanceledSkill_2);
            I.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.ElementBuff, OnInputStartedElementalImbue, OnInputPerformedElementalImbue, OnInputCanceledElementalImbue);
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
        {
            MoveAnimType = MoveAnimType == BaseMoveAnimType.Walk
                ? BaseMoveAnimType.Run
                : BaseMoveAnimType.Walk;
            PlayerController.SetAutoSprintArmed(
                MoveAnimType == BaseMoveAnimType.Run);
        }
        private void OnInputPerformedSprint(InputAction.CallbackContext obj)
        {
            if ((PlayerController.CurrentState?.StateTags & ActorStateTag.Locomotion) != 0)
            {
                MoveAnimType = MoveAnimType == BaseMoveAnimType.Sprint ? BaseMoveAnimType.Run : BaseMoveAnimType.Sprint;
                PlayerController.SetAutoSprintArmed(MoveAnimType != BaseMoveAnimType.Sprint);
            }
        }
        private void OnInputPerformedHeavyAttack(InputAction.CallbackContext obj)
        {
            // InputManager가 performed 시점에 버퍼에 자동 추가하므로 즉시 제거.
            // 짧은 누름(일반 강공격)인지 긴 누름(차지)인지는 canceled에서 판별 후 재추가.
            InputMgr.InputBuffer.ConsumeInput(PlayerAction.HeavyAttack);
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
                InputMgr.InputBuffer.AddInput(PlayerAction.HeavyAttack, bufferTime: 0.24f);
                _heavyInputCondition = InputCondition.Pressed;
            }
            _chargeAttackHeld = false;
        }
        private void OnInputPerformedAttack(InputAction.CallbackContext obj)       => _attackInputCondition   = InputCondition.Pressed;
        private void OnInputPerformedEquipWeapon(InputAction.CallbackContext obj)  => _equipInputCondition    = InputCondition.Pressed;
        private void OnInputStartedSkill_1(InputAction.CallbackContext obj) => SetSkillHeld(0, true);
        private void OnInputPerformedSkill_1(InputAction.CallbackContext obj)      => _skillInputCondition[0] = InputCondition.Pressed;
        private void OnInputCanceledSkill_1(InputAction.CallbackContext obj) => SetSkillReleased(0);
        private void OnInputStartedSkill_2(InputAction.CallbackContext obj) => SetSkillHeld(1, true);
        private void OnInputPerformedSkill_2(InputAction.CallbackContext obj)
        {
            // 궁극기 시퀀스 에셋이 연결돼 있으면 전용 실행기를 우선 사용한다.
            // 아직 에셋이 없는 캐릭터는 기존 Skill 2 상태 경로를 그대로 유지한다.
            if (GetCombat()?.RequestUltimate() == true)
                return;

            _skillInputCondition[1] = InputCondition.Pressed;
        }
        private void OnInputCanceledSkill_2(InputAction.CallbackContext obj) => SetSkillReleased(1);
        private void OnInputStartedElementalImbue(InputAction.CallbackContext obj)
            => SetSkillHeld((int)PlayerSkillSlot.ElementalImbue, true);
        private void OnInputPerformedElementalImbue(InputAction.CallbackContext obj)
            => _skillInputCondition[(int)PlayerSkillSlot.ElementalImbue] = InputCondition.Pressed;
        private void OnInputCanceledElementalImbue(InputAction.CallbackContext obj)
            => SetSkillReleased((int)PlayerSkillSlot.ElementalImbue);

        private void SetSkillHeld(int slot, bool held)
        {
            if ((uint)slot >= (uint)_skillInputHeld.Count)
                return;
            _skillInputHeld[slot] = held;
        }

        private void SetSkillReleased(int slot)
        {
            if ((uint)slot >= (uint)_skillInputHeld.Count)
                return;
            _skillInputHeld[slot] = false;
            _skillInputCondition[slot] = InputCondition.Canceled;
        }
        private void OnInputPerformedInteraction(InputAction.CallbackContext obj)
        {
            _interactionInputCondition = InputCondition.Pressed;

            if (GetCombat()?.FindSpecialBreakAttackTarget() != null)
                InputMgr.InputBuffer.AddInput(PlayerAction.Interact, bufferTime: 0.15f);
        }
        private void OnInputStartedGuard(InputAction.CallbackContext obj)
        {
            _guardInputCondition = InputCondition.None;
            _guardChordConsumed = false;
            _guardInputStartedAt = Time.unscaledTime;

            // 키보드 V는 다른 액션의 조합키가 아니므로 즉시 가드한다.
            // 게임패드 LB는 회피/궁극기/퀵슬롯 조합키이므로 짧게 판별을 유예한다.
            _guardInputPending = obj.control?.device is Gamepad;
            if (!_guardInputPending)
                _guardInputCondition = InputCondition.Pressed;
        }

        private void OnInputFinishedGuard(InputAction.CallbackContext obj)
        {
            _guardInputCondition = InputCondition.None;
            _guardInputPending = false;
            _guardChordConsumed = false;
        }

        private void ResolveGuardChordInput()
        {
            if (!_guardInputPending)
                return;

            Gamepad gamepad = Gamepad.current;
            bool chordPressed = gamepad != null
                                && gamepad.leftShoulder.isPressed
                                && (gamepad.rightShoulder.isPressed
                                    || gamepad.rightTrigger.isPressed
                                    || gamepad.dpad.up.isPressed
                                    || gamepad.dpad.down.isPressed
                                    || gamepad.dpad.left.isPressed
                                    || gamepad.dpad.right.isPressed);

            if (chordPressed)
            {
                _guardChordConsumed = true;
                _guardInputCondition = InputCondition.None;
                return;
            }

            if (!_guardChordConsumed
                && Time.unscaledTime - _guardInputStartedAt >= GuardChordResolveDelay)
            {
                _guardInputCondition = InputCondition.Pressed;
            }
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

        private bool CanInputInteract()
        {
            if (GetCombat()?.FindSpecialBreakAttackTarget() != null)
                return true;

            if (GameObjectMgr.InteractionHandler?.CurrentClosestInteractable?.IsInteracting() == true)
                return true;

            return GameObjectMgr.CanInteract();
        }

        public bool CanStartInteraction()
        {
            return GameObjectMgr != null && GameObjectMgr.CanInteract();
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
            _guardInputPending         = false;
            _guardChordConsumed        = false;
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
            InputMgr?.InputBuffer?.Clear();
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
            var party = Svc.Party;
            if (party == null) return;

            Vector3 fxPos = TryGetSocket(party.SwapEvadeFxSocket, out var socket)
                ? socket.position
                : transform.position;
            fxPos += party.SwapEvadeFxOffset;

            if (party.SwapEvadeEnableHitStop && party.SwapEvadeHitStopDuration > 0f)
                GameCombatMgr?.ExecuteHitStop(
                    party.SwapEvadeHitStopDuration,
                    party.SwapEvadeHitStopTimeScale);

            CameraMgr?.CombatCamera?.PlayDodgeCounter(
                target != null ? target.transform : null,
                party.SwapEvadeCameraShakeKey);

            if (!string.IsNullOrWhiteSpace(party.SwapEvadeFxKey))
                GameObjectMgr?.ShowFX(party.SwapEvadeFxKey, fxPos, transform.rotation);

            if (party.SwapEvadeSpawnDodgeVitalOrb)
                GameCombatMgr?.TrySpawnVitalOrb(VitalOrbTrigger.Dodge, fxPos);
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
}
