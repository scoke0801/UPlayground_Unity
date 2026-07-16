using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Path;
using UPlayGround.Animation;
using UPlayGround.Combat;
using UPlayGround.Data;
using UPlayGround.Data.Combat;
using UPlayGround.Data.Party;
using UPlayGround.Manager;
using UPlayGround.Manager.Handler;
using UPlayGround.Manager.Combat;
using UPlayGround.UI;
using UPlayGround.Input;
using UPlayGround.Gameplay.Tag;
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
                   && CurrentComboIndex < GetComboLength(desiredState) - 1;
        }

        // ── Peek API (side-effect-free) ───────────────────────────────
        // PlayerAttackState 진입 가능 여부 판정용. CurrentComboIndex / _attackState /
        // _currentAttackData 등 어떠한 상태도 변경하지 않는다.

        /// <summary> 다음 일반 공격이 사용할 AnimKey를 미리 조회 (side effect 없음). </summary>
        public AnimKey PeekNormalAttackAnimKey(bool isCombo)
        {
            if (_attackData == null || _attackData.liteComboAttackList == null
                || _attackData.liteComboAttackList.Count == 0)
                return AnimKey.None;

            int nextIndex = PeekNextComboIndex(AttackState.NormalAttack, isCombo);
            return _attackData.liteComboAttackList[nextIndex]?.baseInfo?.animKey ?? AnimKey.None;
        }

        /// <summary> 다음 강 공격이 사용할 AnimKey를 미리 조회 (side effect 없음). </summary>
        public AnimKey PeekHeavyAttackAnimKey(bool isCombo)
        {
            if (_attackData == null || _attackData.heavyComboAttackList == null
                || _attackData.heavyComboAttackList.Count == 0)
                return AnimKey.None;

            int nextIndex = PeekNextComboIndex(AttackState.HeavyAttack, isCombo);
            return _attackData.heavyComboAttackList[nextIndex]?.baseInfo?.animKey ?? AnimKey.None;
        }

        /// <summary> 카운터 공격 AnimKey 조회 (ExecuteCounterAttack과 동일한 폴백 체인). </summary>
        public AnimKey PeekCounterAttackAnimKey()
        {
            var source = _attackData?.counterAttack?.baseInfo != null
                ? _attackData.counterAttack
                : (_attackData != null && _attackData.heavyComboAttackList.Count > 0
                    ? _attackData.heavyComboAttackList[0]
                    : null);
            return source?.baseInfo?.animKey ?? AnimKey.None;
        }

        /// <summary> 등장 공격 AnimKey 조회 (ExecuteEntryAttack과 동일한 변형/폴백 체인). </summary>
        public AnimKey PeekEntryAttackAnimKey()
        {
            return SelectEntryAttackInfo()?.baseInfo?.animKey ?? AnimKey.None;
        }

        /// <summary> 스왑 회피 카운터 AnimKey 조회 (ExecuteSwapEvadeCounterAttack과 동일한 폴백 체인). </summary>
        public AnimKey PeekSwapEvadeCounterAttackAnimKey()
        {
            var source = _attackData?.swapEvadeCounterAttack?.baseInfo != null
                ? _attackData.swapEvadeCounterAttack
                : (_attackData?.entryAttack?.baseInfo != null
                    ? _attackData.entryAttack
                    : (_attackData != null && _attackData.liteComboAttackList.Count > 0
                        ? _attackData.liteComboAttackList[0]
                        : null));
            return source?.baseInfo?.animKey ?? AnimKey.None;
        }

        /// <summary> 풀 게이지 교체 특수 공격 AnimKey 조회 (ExecuteSwapSpecialAttack과 동일한 폴백 체인). </summary>
        public AnimKey PeekSwapSpecialAttackAnimKey()
        {
            var source = _attackData?.swapSpecialAttack?.baseInfo != null
                ? _attackData.swapSpecialAttack
                : (_attackData != null && _attackData.skillAttackList.Count > 0 && _attackData.skillAttackList[0]?.baseInfo != null
                    ? _attackData.skillAttackList[0]
                    : (_attackData?.entryAttack?.baseInfo != null ? _attackData.entryAttack : null));
            return source?.baseInfo?.animKey ?? AnimKey.None;
        }

        /// <summary> 패리 반격 AnimKey 조회 (ExecuteParryCounterAttack과 동일한 폴백 체인). </summary>
        public AnimKey PeekParryCounterAttackAnimKey()
        {
            var source = _attackData?.parryCounterAttack?.baseInfo != null
                ? _attackData.parryCounterAttack
                : (_attackData?.counterAttack?.baseInfo != null
                    ? _attackData.counterAttack
                    : (_attackData != null && _attackData.heavyComboAttackList.Count > 0
                        ? _attackData.heavyComboAttackList[0]
                        : null));
            return source?.baseInfo?.animKey ?? AnimKey.None;
        }

        /// <summary> 스킬 공격 AnimKey 조회. 인덱스가 범위 밖이면 None. </summary>
        public AnimKey PeekSkillAttackAnimKey(int skillIndex)
        {
            return TryResolveSkill(skillIndex, out PlayerSkillResolveResult resolved)
                ? resolved.AnimKey
                : AnimKey.None;
        }

        private bool TryResolveSkill(int skillIndex, out PlayerSkillResolveResult resolved)
        {
            resolved = default;
            if (!IsSkillUnlocked(skillIndex))
                return false;

            PlayerSkillContext context = CreateSkillContext();
            return PlayerSkillResolver.TryResolve(_attackData, skillIndex, context, out resolved);
        }

        private bool IsSkillUnlocked(int skillIndex)
        {
            if (!PlayerSkillGauge.IsValidSkillSlot(skillIndex) || PartyManager.Instance == null)
                return true;

            GrowthSkillType skillType = skillIndex == PlayerSkillGauge.AbilitySkillSlot
                ? GrowthSkillType.Ability
                : GrowthSkillType.Ultimate;
            return PartyManager.Instance.IsSkillUnlocked(
                PartyManager.Instance.ActiveCharacterType,
                skillType);
        }

        private PlayerSkillContext CreateSkillContext()
        {
            bool isGrounded = _playerActor == null
                              || _playerActor.PlayerController == null
                              || _playerActor.PlayerController.Motor == null
                              || _playerActor.PlayerController.Motor.GroundingStatus.IsStableOnGround;
            var gauge = _playerActor != null ? _playerActor.SkillGauge : null;
            return new PlayerSkillContext(
                isGrounded,
                _playerActor != null ? _playerActor.Tags : null,
                gauge != null ? gauge.CurrentGauge : 0f,
                gauge != null ? gauge.MaxGauge : 0f);
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
                state.LastAttackAnimKey));
            _comboCharacterType = characterType;
        }

        /// <summary>
        /// 캐릭터 교체 시 공격 데이터 SO를 교체하고, 캐릭터별 콤보 상태를 복원한다.
        /// </summary>
        public void RefreshAttackData(
            PlayerAttackDataSO newData,
            CharacterActorType characterType,
            bool preserveComboState = true,
            float comboStateMaxCarryTime = 1.8f)
        {
            if (_comboCharacterType != CharacterActorType.None && _comboCharacterType != characterType)
                SaveComboState(_comboCharacterType);

            _attackData = newData;
            _comboCharacterType = characterType;

            // 캐릭터별 연계 토큰 간격을 트래커에 반영.
            if (_playerActor != null && newData != null)
                _playerActor.ComboInputTracker.LinkWindow = newData.comboLinkWindow;

            if (preserveComboState && TryRestoreComboState(characterType, comboStateMaxCarryTime))
                return;

            ResetCombo();
        }

        public void RefreshAttackData(PlayerAttackDataSO newData)
        {
            RefreshAttackData(newData, _comboCharacterType, false);
        }

        public void ResetCombo()
        {
            ResetCombo(true, true);
        }

        /// <summary>
        /// 콤보 인덱스/윈도우/태그/입력버퍼는 초기화하되 약·강 체인 분기 메모리(_normalComboIndex/_heavyComboIndex)는 보존한다.
        /// 공격 상태 재진입(크로스타입 캔슬 등)에서 호출 — 진짜 콤보 종료가 아니므로 분기 진행도를 잇기 위함.
        /// </summary>
        public void ResetComboPreserveChains()
        {
            ResetCombo(true, false);
        }

        private void ResetCombo(bool clearInputBuffer, bool resetChains)
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
            if (clearInputBuffer)
                InputManager.Instance.InputBuffer.Clear();
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
                LastAttackAnimKey = _currentAttackData != null ? _currentAttackData.animKey : AnimKey.None,
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
                LastAttackAnimKey = snapshot.LastAnimKey,
            };

            _attackState = state.AttackState;
            CurrentComboIndex = Mathf.Clamp(state.CurrentComboIndex, 0, Mathf.Max(0, GetComboLength(_attackState) - 1));
            _normalComboIndex = Mathf.Clamp(state.NormalComboIndex, -1, GetComboLength(AttackState.NormalAttack) - 1);
            _heavyComboIndex  = Mathf.Clamp(state.HeavyComboIndex,  -1, GetComboLength(AttackState.HeavyAttack) - 1);
            LastAttackTime = state.LastAttackTime;
            if (state.CanCombo || (state.LastAttackAnimKey != AnimKey.None && GetComboLength(_attackState) > 1))
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

            int dataLength = attackState switch
            {
                AttackState.NormalAttack => _attackData.liteComboAttackList?.Count ?? 0,
                AttackState.HeavyAttack  => _attackData.heavyComboAttackList?.Count ?? 0,
                AttackState.JumpAttack   => _attackData.jumpAttackList?.Count ?? 0,
                AttackState.DashAttack   => _attackData.dashAttackList?.Count ?? 0,
                AttackState.SkillAttack  => _attackData.skillAttackList?.Count ?? 0,
                AttackState.ChargeAttack => 0,
                _                        => 0,
            };

            GrowthComboType? comboType = attackState switch
            {
                AttackState.NormalAttack => GrowthComboType.Light,
                AttackState.HeavyAttack => GrowthComboType.Heavy,
                _ => null,
            };
            if (!comboType.HasValue || dataLength <= 1 || PartyManager.Instance == null)
                return dataLength;

            CharacterActorType type = PartyManager.Instance.ActiveCharacterType;
            return PartyManager.Instance.GetUnlockedComboLength(type, comboType.Value, dataLength);
        }

        private void ApplyComboTags()
        {
            _playerActor.Tags?.RemoveTag(GameplayTagId.Combo_Light);
            _playerActor.Tags?.RemoveTag(GameplayTagId.Combo_Heavy);

            if (CurrentComboIndex <= 0) return;

            if (_attackState == AttackState.NormalAttack)
                _playerActor.Tags?.AddTag(GameplayTagId.Combo_Light);
            else if (_attackState == AttackState.HeavyAttack)
                _playerActor.Tags?.AddTag(GameplayTagId.Combo_Heavy);
        }
        #endregion
    }
}
