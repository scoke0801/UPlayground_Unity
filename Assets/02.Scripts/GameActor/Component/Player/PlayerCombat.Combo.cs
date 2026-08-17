using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UPlayGround.Data.EnumType;
using UPlayGround.Ability.Core;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Data.Path;
using UPlayGround.Animation;
using UPlayGround.Combat;
using UPlayGround.Data;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Combat;
using UPlayGround.Data.Party;
using UPlayGround.Manager;
using UPlayGround.UI;
using UPlayGround.Input;
using UPlayGround.Gameplay.Tag;
using UPlayGround.Gameplay.Ability;
using UPlayGround.MovementController;
using UPlayGround.Debugging;

namespace UPlayGround.Components
{
    public partial class PlayerCombat : PlayerActorComponent, UPlayGround.Combat.ICombatCollisionExecutor, IDebugGizmoProvider
    {
        #region Combo

        private bool CanContinueCombo()
        {
            int length = GetComboLength(_attackState);
            return CurrentComboIndex < length - 1;
        }

        public void OpenComboWindow()
        {
            _comboController?.OpenWindow();
            _actionRunner?.HandleTimelineEvent(CombatTimelineEventType.ComboWindowOpened, _currentAttackData?.hitPhaseIndex ?? 0);
        }

        public void CloseComboWindow()
        {
            _comboController?.CloseWindow();
            _actionRunner?.HandleTimelineEvent(CombatTimelineEventType.ComboWindowClosed, _currentAttackData?.hitPhaseIndex ?? 0);
        }

        private void HandleAttackStartedForRunner(AttackData attackData)
            => _actionRunner?.StartAction(attackData);

        public bool CanUseStoredCombo(bool isHeavyAttack)
        {
            AttackState desiredState = isHeavyAttack ? AttackState.HeavyAttack : AttackState.NormalAttack;
            return CanCombo
                   && _attackState == desiredState
                   && CanContinueStoredCombo(isHeavyAttack);
        }

        /// <summary>
        /// 현재 약/강 체인에 실제 다음 타격이 남아 있는지 반환한다.
        /// 막타 뒤 입력을 같은 AttackState 안에서 0번으로 래핑하지 않고, 완주한 체인을
        /// 종료한 뒤 새 공격 상태로 시작하기 위한 시퀀스 경계 판정이다.
        /// </summary>
        public bool CanContinueStoredCombo(bool isHeavyAttack)
        {
            AttackState desiredState = isHeavyAttack ? AttackState.HeavyAttack : AttackState.NormalAttack;
            int storedIndex = isHeavyAttack ? _heavyComboIndex : _normalComboIndex;
            int length = GetComboLength(desiredState);
            return storedIndex >= 0 && storedIndex < length - 1;
        }

        // ── Peek API (side-effect-free) ───────────────────────────────
        // PlayerAttackState 진입 가능 여부 판정용. CurrentComboIndex / _attackState /
        // _currentAttackData 등 어떠한 상태도 변경하지 않는다.

        /// <summary>다음 일반 공격 모션을 미리 조회한다.</summary>
        public MotionSetAsset PeekNormalAttackMotion(bool isCombo)
        {
            if (_attackData == null || _attackData.liteComboAttackList == null
                || _attackData.liteComboAttackList.Count == 0)
                return null;

            int nextIndex = PeekNextComboIndex(AttackState.NormalAttack, isCombo);
            return ResolveAttackMotion(_attackData.liteComboAttackList[nextIndex]);
        }

        public MotionSetAsset PeekHeavyAttackMotion(bool isCombo)
        {
            if (_attackData == null || _attackData.heavyComboAttackList == null
                || _attackData.heavyComboAttackList.Count == 0)
                return null;

            int nextIndex = PeekNextComboIndex(AttackState.HeavyAttack, isCombo);
            if (!CanPayAttackAbilityCost(
                    _attackData.heavyComboAbilities,
                    nextIndex))
                return null;
            return ResolveAttackMotion(_attackData.heavyComboAttackList[nextIndex]);
        }

        public MotionSetAsset PeekCounterAttackMotion()
        {
            var source = _attackData?.counterAttack?.baseInfo != null
                ? _attackData.counterAttack
                : (_attackData != null && _attackData.heavyComboAttackList.Count > 0
                    ? _attackData.heavyComboAttackList[0]
                    : null);
            return ResolveAttackMotion(source);
        }

        public MotionSetAsset PeekEntryAttackMotion()
        {
            return ResolveAttackMotion(SelectEntryAttackInfo());
        }

        public MotionSetAsset PeekSwapEvadeCounterAttackMotion()
        {
            var source = _attackData?.swapEvadeCounterAttack?.baseInfo != null
                ? _attackData.swapEvadeCounterAttack
                : (_attackData?.entryAttack?.baseInfo != null
                    ? _attackData.entryAttack
                    : (_attackData != null && _attackData.liteComboAttackList.Count > 0
                        ? _attackData.liteComboAttackList[0]
                        : null));
            return ResolveAttackMotion(source);
        }

        public MotionSetAsset PeekSwapSpecialAttackMotion()
        {
            var source = _attackData?.swapSpecialAttack?.baseInfo != null
                ? _attackData.swapSpecialAttack
                : null;
            return ResolveAttackMotion(source);
        }

        public MotionSetAsset PeekParryCounterAttackMotion()
        {
            var source = _attackData?.parryCounterAttack?.baseInfo != null
                ? _attackData.parryCounterAttack
                : (_attackData?.counterAttack?.baseInfo != null
                    ? _attackData.counterAttack
                    : (_attackData != null && _attackData.heavyComboAttackList.Count > 0
                        ? _attackData.heavyComboAttackList[0]
                        : null));
            return ResolveAttackMotion(source);
        }

        public MotionSetAsset PeekSkillAttackMotion(int skillIndex)
        {
            return TryResolveSkill(skillIndex, out _, out MotionSetAsset motion) ? motion : null;
        }

        private bool TryResolveSkill(
            int skillIndex,
            out AbilityAttackInfo attackInfo,
            out MotionSetAsset motionAsset)
        {
            attackInfo = null;
            motionAsset = null;
            if (_playerActor?.Abilities == null
                || !System.Enum.IsDefined(typeof(PlayerSkillSlot), skillIndex))
                return false;

            AbilityActivationResult result = _playerActor.Abilities.EvaluatePlayerSlot(
                (PlayerSkillSlot)skillIndex,
                IsGroundedForSkill(),
                null,
                out AbilityVariantDefinition variant);
            if (result != AbilityActivationResult.Success
                || !UPlayGroundAbilityPayloadResolver.TryResolveAttackInfo(
                    variant,
                    out attackInfo)
                || !ActorAbilityMotionResolver.TryResolve(
                    _playerActor,
                    attackInfo,
                    out motionAsset))
                return false;

            return attackInfo?.baseInfo != null && motionAsset != null;
        }

        private MotionSetAsset ResolveAttackMotion(AbilityAttackInfo attackInfo)
        {
            return ActorAbilityMotionResolver.TryResolve(
                _playerActor,
                attackInfo,
                out MotionSetAsset motionAsset)
                ? motionAsset
                : null;
        }

        private bool IsGroundedForSkill()
        {
            return _playerActor == null
                   || _playerActor.PlayerController == null
                   || _playerActor.PlayerController.Motor == null
                   || _playerActor.PlayerController.Motor.GroundingStatus.IsStableOnGround;
        }

        /// <summary>
        /// 다음 콤보 인덱스를 미리 계산 (인덱스를 변경하지 않음).
        /// 해당 체인의 보존 인덱스(_normalComboIndex/_heavyComboIndex)를 기준으로 Execute와 동일한 규칙으로 예측한다.
        /// 미시작(-1) 또는 isCombo==false 또는 끝까지 진행했으면 0, 그 외 보존 인덱스+1.
        /// (크로스타입 전환 시에도 Execute가 상대 체인을 리셋하지 않으므로 peek도 보존 인덱스를 따라야 일치한다.)
        /// </summary>
        private int PeekNextComboIndex(AttackState desiredState, bool isCombo)
        {
            int length = GetComboLength(desiredState);
            if (length <= 0) return 0;

            int baseIndex = desiredState switch
            {
                AttackState.NormalAttack => _normalComboIndex,
                AttackState.HeavyAttack  => _heavyComboIndex,
                _                        => CurrentComboIndex,
            };

            bool canContinue = baseIndex >= 0 && baseIndex < length - 1;
            int nextIndex = (isCombo && canContinue) ? baseIndex + 1 : 0;
            return Mathf.Clamp(nextIndex, 0, length - 1);
        }
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// 모델 교체 직전 현재 캐릭터의 조작 콤보 상태를 저장한다.
        /// 잔류 러너의 후속 히트는 이 상태를 갱신하지 않는다.
        /// </summary>
        public void SaveComboState(CharacterActorType characterType)
        {
            if (characterType == CharacterActorType.None) return;

            CharacterComboState state = CaptureComboState();
            _comboController?.Save(characterType, new PlayerComboController.Snapshot(
                state.CurrentComboIndex,
                state.NormalComboIndex,
                state.HeavyComboIndex,
                state.LastAttackTime,
                state.CanCombo,
                (int)state.AttackState,
                state.HadAttackMotion));
            _comboCharacterType = characterType;
        }

        /// <summary>
        /// 캐릭터 교체 시 Ability 전투 로드아웃을 교체하고, 캐릭터별 콤보 상태를 복원한다.
        /// </summary>
        public void RefreshAbilitySet(
            AbilitySetSO abilitySet,
            CharacterActorType characterType,
            bool preserveComboState = true,
            float comboStateMaxCarryTime = 1.8f)
        {
            if (_comboCharacterType != CharacterActorType.None && _comboCharacterType != characterType)
                SaveComboState(_comboCharacterType);

            _abilitySet = abilitySet;
            _attackData = PlayerCombatAbilityDataView.Build(abilitySet);
            _comboCharacterType = characterType;

            // 캐릭터별 연계 토큰 간격을 트래커에 반영.
            if (_playerActor != null && _attackData != null)
                _playerActor.ComboInputTracker.LinkWindow = _attackData.comboLinkWindow;

            if (preserveComboState && TryRestoreComboState(characterType, comboStateMaxCarryTime))
                return;

            ResetCombo();
        }

        public void RefreshAbilitySet(AbilitySetSO abilitySet)
        {
            RefreshAbilitySet(abilitySet, _comboCharacterType, false);
        }

        public void ResetCombo()
        {
            ResetComboState(true);
        }

        /// <summary>
        /// 콤보 인덱스/윈도우/태그는 초기화하되 약·강 체인 분기 메모리(_normalComboIndex/_heavyComboIndex)는 보존한다.
        /// 공격 상태 재진입(크로스타입 캔슬 등)에서 호출 — 진짜 콤보 종료가 아니므로 분기 진행도를 잇기 위함.
        /// </summary>
        public void ResetComboPreserveChains()
        {
            ResetComboState(false);
        }

        /// <summary>
        /// PlayerCombat이 소유하는 콤보 진행 상태만 초기화한다.
        /// 입력 버퍼는 InputManager의 소유물이므로 여기서 비우지 않는다. 상태 전환 직전에 들어온
        /// 다음 공격·회피 입력까지 전역 삭제하면 막타 경계에서 조작이 끊긴다.
        /// </summary>
        private void ResetComboState(bool resetChains)
        {
            LastAttackTime    = Time.time;
            CurrentComboIndex = 0;
            if (resetChains)
            {
                // 약/강 체인 보존 인덱스 초기화 — 콤보가 실제로 끝나는 경로에서만(피격/타임아웃/Idle 복귀/점프 등).
                _normalComboIndex = -1;
                _heavyComboIndex  = -1;
            }
            _comboController?.ResetWindow();
            ApplyComboTags();
            OnComboReset?.Invoke();
        }

        private CharacterComboState CaptureComboState()
        {
            return new CharacterComboState
            {
                CurrentComboIndex = CurrentComboIndex,
                NormalComboIndex = _normalComboIndex,
                HeavyComboIndex = _heavyComboIndex,
                LastAttackTime = LastAttackTime,
                CanCombo = CanCombo,
                AttackState = _attackState,
                HadAttackMotion = _currentAttackData?.motionAsset != null,
            };
        }

        private bool TryRestoreComboState(CharacterActorType characterType, float maxCarryTime)
        {
            if (characterType == CharacterActorType.None)
                return false;

            if (_comboController == null
                || !_comboController.TryRestore(characterType, maxCarryTime, out PlayerComboController.Snapshot snapshot))
                return false;

            var state = new CharacterComboState
            {
                CurrentComboIndex = snapshot.CurrentIndex,
                NormalComboIndex = snapshot.NormalIndex,
                HeavyComboIndex = snapshot.HeavyIndex,
                LastAttackTime = snapshot.LastAttackTime,
                CanCombo = snapshot.CanCombo,
                AttackState = (AttackState)snapshot.AttackState,
                HadAttackMotion = snapshot.HadAttackMotion,
            };

            _attackState = state.AttackState;
            CurrentComboIndex = Mathf.Clamp(state.CurrentComboIndex, 0, Mathf.Max(0, GetComboLength(_attackState) - 1));
            _normalComboIndex = Mathf.Clamp(state.NormalComboIndex, -1, GetComboLength(AttackState.NormalAttack) - 1);
            _heavyComboIndex  = Mathf.Clamp(state.HeavyComboIndex,  -1, GetComboLength(AttackState.HeavyAttack) - 1);
            LastAttackTime = state.LastAttackTime;
            if (state.CanCombo || (state.HadAttackMotion && GetComboLength(_attackState) > 1))
                _comboController.OpenWindow();
            else
                _comboController.CloseWindow();
            ApplyComboTags();
            return true;
        }

        private void RestoreComboState(CharacterComboState state)
        {
            _attackState = state.AttackState;
            CurrentComboIndex = state.CurrentComboIndex;
            _normalComboIndex = state.NormalComboIndex;
            _heavyComboIndex  = state.HeavyComboIndex;
            LastAttackTime = state.LastAttackTime;
            if (state.CanCombo)
                _comboController?.OpenWindow();
            else
                _comboController?.CloseWindow();
            ApplyComboTags();
        }

        private int GetComboLength(AttackState attackState)
        {
            if (_attackData == null) return 0;

            return attackState switch
            {
                AttackState.NormalAttack => GetUnlockedComboLength(
                    _attackData.liteComboAttackList,
                    _attackData.liteComboAbilities),
                AttackState.HeavyAttack  => GetUnlockedComboLength(
                    _attackData.heavyComboAttackList,
                    _attackData.heavyComboAbilities),
                AttackState.JumpAttack   => _attackData.jumpAttackList?.Count ?? 0,
                AttackState.DashAttack   => _attackData.dashAttackList?.Count ?? 0,
                AttackState.SkillAttack  => _attackData.skillAttackList?.Count ?? 0,
                AttackState.ChargeAttack => 0,
                _                        => 0,
            };
        }

        private int GetUnlockedComboLength(
            IReadOnlyList<AbilityAttackInfo> attacks,
            IReadOnlyList<GameplayAbilitySO> abilities)
        {
            int count = attacks?.Count ?? 0;
            if (count == 0 || abilities == null || abilities.Count != count)
                return count;

            for (int i = 0; i < count; i++)
            {
                GameplayAbilitySO ability = abilities[i];
                if (ability != null
                    && Svc.Party?.IsAbilityUnlocked(
                        _playerActor.CharacterType,
                        ability.abilityId) == false)
                    return i;
            }
            return count;
        }

        private void ApplyComboTags()
        {
            _playerActor.Tags?.RemoveTag(GameplayTags.Combo_Light);
            _playerActor.Tags?.RemoveTag(GameplayTags.Combo_Heavy);

            if (CurrentComboIndex <= 0) return;

            if (_attackState == AttackState.NormalAttack)
                _playerActor.Tags?.AddTag(GameplayTags.Combo_Light);
            else if (_attackState == AttackState.HeavyAttack)
                _playerActor.Tags?.AddTag(GameplayTags.Combo_Heavy);
        }
        #endregion
    }
}
