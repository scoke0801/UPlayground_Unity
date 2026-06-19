using UnityEngine;
using UPlayGround.Component;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 차지 공격 상태
    ///
    /// 흐름:
    ///   1. OnEnter: chargeAttackList[0]의 AnimKey로 MotionSet 재생
    ///   2. MotionSet 내 첫 번째 InfiniteLoop 구간에서 차지 포즈 대기 (Stage 0)
    ///   3. InfiniteLoop 진입 시점부터 chargeRatio(0→1) 누적
    ///   4. chargeRatio가 StageThresholds[stageIndex]를 초과하면 BreakInfiniteLoop →
    ///      MotionSet이 다음 InfiniteLoop까지 자동 진행 (Stage 1, 2 ...)
    ///   5. 버튼 뗌 또는 최대 차지 도달 → ExecuteChargeAttack(stageIndex) + BreakInfiniteLoop
    ///   6. 루프 해제 후 애니메이션이 공격 구간으로 진행 → OnMotionSetCompleted → 상태 종료
    ///
    /// 취소:
    ///   - chargeInterruptActions 마스크에 포함된 입력(기본 Dodge) 시 해당 상태로 전환
    ///     (BreakInfiniteLoop는 OnExit에서 처리)
    ///   - 피격(Hit) 시 CanTransitionState → true (BreakInfiniteLoop는 OnExit에서 처리)
    /// </summary>
    public class PlayerChargeState : PlayerActorState
    {
        public override string StateName => "Charge";
        protected override ActorStateTag StateTagsCore => ActorStateTag.Combat;

        private PlayerCombat _combat;
        private PlayerEquipment _equipment;

        // 차지 시간 (InfiniteLoop 진입 후 카운트)
        private float _chargeTime;
        private float _chargeRatio;
        private bool  _isInLoop;      // InfiniteLoop 구간에 진입했는지
        private bool  _isFired;       // BreakInfiniteLoop 한 번만 호출되도록
        private bool  _releasedBeforeLoop; // 루프 진입 전에 이미 버튼을 뗐는지

        private const float MaxChargeTime = 1.5f; // 풀 차지까지 걸리는 시간 (초)

        // 스테이지 전환 임계값. OnEnter에서 PlayerAttackDataSO 기준으로 초기화.
        private float[] _stageThresholds = System.Array.Empty<float>();

        // 락온/소프트 타겟 (회전 보정용)
        private Transform _softRotationTarget;

        public PlayerChargeState(ActorMovementController controller) : base(controller) { }

        /// <summary>
        /// 한 단계 이상 차징 완료 여부.
        /// InfiniteLoopStageIndex >= 1 이면 첫 번째 임계값을 넘어 슈퍼아머 조건 충족.
        /// </summary>
        public bool HasChargedAtLeastOneStage =>
            _isInLoop && gameActor != null &&
            gameActor.Animator.InfiniteLoopStageIndex >= 1;

        // 한 단계 이상 차징 중이면 Hit/Airborne/Grabbed 전환 차단 (슈퍼아머)
        // 그 외(Dodge 등)는 항상 허용
        public override bool CanTransitionState(string stateName)
        {
            if (HasChargedAtLeastOneStage &&
                (stateName == "Hit" || stateName == "Airborne" || stateName == "Grabbed"))
                return false;
            return true;
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            // 차지 중 FootIK를 끄지 않는다. IK on/off 전환이 발 스냅 원인. (PlayerAttackState 참고)

            _combat             = playerActor.GetCombat();
            _equipment          = playerActor.GetPlayerEquipment();
            _equipment?.SetMainWeaponDrawn(true);
            _chargeTime         = 0f;
            _chargeRatio        = 0f;
            _isInLoop           = false;
            _isFired            = false;
            _releasedBeforeLoop = false;
            _stageThresholds    = _combat.GetChargeStageThresholds();

            playerActor.Animator.ApplyRootMotion(true);

            // chargeAttackList[0]의 AnimKey로 애니메이션 재생
            // 해당 애니메이션에는 반드시 InfiniteLoop LoopEvent가 포함되어야 함
            var attackData = _combat.GetFirstChargeAttackAnimKey();
            if (attackData == AnimKey.None)
            {
                ExitToIdle();
                return;
            }

            var animState = gameActor.Animator.PlayMotion(attackData, 0.15f);
            if (animState == null)
            {
                ExitToIdle();
                return;
            }

            gameActor.Animator.OnMotionSetCompleted += OnAttackAnimEnd;

            // 소프트 회전 타겟 등록
            _softRotationTarget = CameraManager.Instance.GetLockOnTarget();
        }

        public override void OnExit(GameActorState toState)
        {
            // 진입 시 끄지 않으므로 다시 켤 필요 없음.

            gameActor.Animator.OnMotionSetCompleted -= OnAttackAnimEnd;

            // 피격 등 강제 전환 시 남은 모든 InfiniteLoop 차단
            gameActor.Animator.BreakAllInfiniteLoops();

            _combat.SetEnableCollision(false);
            _combat.ClearHitTargets();
            ActorWeaponTrailController.StopAttackTrails(_equipment != null ? _equipment : playerActor);

            playerActor.Animator.ApplyRootMotion(false);
            _softRotationTarget = null;
            base.OnExit(toState);
        }

        public override void UpdateState(float deltaTime)
        {
            // ── 취소: 데이터(chargeInterruptActions) + 캔슬 윈도우(콜리전 비활성)로 제어 ──
            // 차지 루프는 콜리전이 강제 비활성이라 윈도우가 열려 회피 캔슬이 유지되고,
            // 발동 스윙(콜리전 활성) 중에는 캔슬되지 않는다.
            if (_combat.IsCancelWindowOpen
                && PlayerInterruptResolver.TryInterrupt(playerController, _combat.GetChargeInterruptActions()))
                return;

            // IsChargeAttackHeld: 버튼을 현재 누르고 있는지 (threshold 초과 여부 포함)
            // 버튼을 뗀 순간부터 false가 되므로 1프레임 플래그보다 신뢰성 높음
            bool isHeld = playerController.IsChargeAttackHeld();

            // ── 루프 진입 전에 버튼을 이미 뗐다면 기록 ────────────────
            if (!_isInLoop && !isHeld)
                _releasedBeforeLoop = true;

            // ── InfiniteLoop 진입 감지 ──────────────────────────────────
            if (!_isInLoop && gameActor.Animator.IsInfiniteLooping)
            {
                _isInLoop   = true;
                _chargeTime = 0f;

                // 루프 도달 전에 이미 뗐으면 최소 차지로 즉시 발동
                if (_releasedBeforeLoop)
                {
                    FireChargeAttack();
                    return;
                }
            }

            // ── 루프 대기 중 충돌 강제 비활성화 (BeginCollision 이벤트 오발 방지) ──
            if (_isInLoop && !_isFired)
                _combat.SetEnableCollision(false);

            // ── 차지 비율 누적 및 발동 ──────────────────────────────────
            if (_isInLoop && !_isFired)
            {
                // InfiniteLoop 안에서만 차지 시간 누적
                // 루프 간 이동 구간(애니메이션 전환 중)에서는 누적하지 않아
                // 루프 도달 전에 _chargeRatio >= 1.0f 로 조기 발동되는 문제 방지
                if (gameActor.Animator.IsInfiniteLooping)
                {
                    _chargeTime  += deltaTime;
                    _chargeRatio  = Mathf.Clamp01(_chargeTime / MaxChargeTime);

                    // ── 스테이지 전환: 홀드 중 임계값 도달 시 다음 InfiniteLoop로 진행 ──
                    int stageIndex = gameActor.Animator.InfiniteLoopStageIndex;
                    if (isHeld
                        && stageIndex < _stageThresholds.Length
                        && _chargeRatio >= _stageThresholds[stageIndex])
                    {
                        gameActor.Animator.BreakInfiniteLoop();
                        return;
                    }

                    // 풀 차지 자동 발동 (루프 안에서만)
                    if (_chargeRatio >= 1.0f)
                    {
                        PlayFullChargeVfx();
                        FireChargeAttack();
                        return;
                    }
                }

                // 버튼을 뗐을 때는 루프 여부 무관하게 발동
                if (!isHeld)
                {
                    FireChargeAttack();
                }
            }
        }

        private void PlayFullChargeVfx()
        {
            var (key, socket, offset) = _combat.GetFullChargeVfxData();
            if (string.IsNullOrEmpty(key)) return;

            Vector3 pos = gameActor.TryGetSocket(socket, out Transform socketTM)
                ? socketTM.position + offset
                : gameActor.transform.position + offset;
            
            GameObjectManager.Instance.ShowFX(key, pos, gameActor.transform.rotation);
        }

        private void FireChargeAttack()
        {
            if (_isFired) return;
            _isFired = true;

            // 현재 InfiniteLoop 단계로 공격 데이터 확정
            int stageIndex = gameActor.Animator.InfiniteLoopStageIndex;
            var attackData = _combat.ExecuteChargeAttack(stageIndex, _chargeRatio);
            _combat.ClearHitTargets();
            ActorWeaponTrailController.StartAttackTrails(_equipment != null ? _equipment : playerActor);

            // 락온 없을 때: 발동 후 방향 보정을 위한 소프트 타겟 확보
            // → UpdateRotation의 _softRotationTarget 추적이 이어지도록
            if (CameraManager.Instance.GetLockOnTarget() == null && attackData != null)
            {
                _softRotationTarget = _combat.FindAttackSnapTarget(
                    attackData.hitRange, attackData.hitAngle, false);
            }

            // 현재 및 이후의 모든 InfiniteLoop 차단 → 애니메이션이 공격 구간으로 진행
            gameActor.Animator.BreakAllInfiniteLoops();

            // 히트 판정은 애니메이션 이벤트(BeginCollision)에서 활성화됨
        }

        private void OnAttackAnimEnd()
        {
            _combat.SetEnableCollision(false);
            _combat.ClearHitTargets();
            ExitToIdle();
        }

        private void ExitToIdle()
        {
            _combat.ResetCombo();
            if (playerController.HasMoveInput())
                controller.TransitionToState(new PlayerGroundMoveState(controller));
            else
                controller.TransitionToState(new PlayerIdleState(controller));
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            // InfiniteLoop 중(차지 대기)에는 제자리 고정
            // 루프 해제 후(실제 공격)에는 루트모션 적용
            if (_isInLoop && !_isFired)
            {
                currentVelocity = Vector3.zero;
                return;
            }

            currentVelocity = gameActor.Animator.DeltaPosition / deltaTime;
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            Transform rotTarget = CameraManager.Instance.GetLockOnTarget() ?? _softRotationTarget;

            if (rotTarget != null)
            {
                Vector3 dir = rotTarget.position - gameActor.transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
                    currentRotation = Quaternion.Slerp(currentRotation, targetRot, deltaTime * 10f);
                    currentRotation = currentRotation.normalized;
                }
                return;
            }

            Vector3 moveInput = playerController.MoveInputVector;
            if (moveInput.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveInput.normalized);
                currentRotation = Quaternion.Slerp(currentRotation, targetRot, deltaTime * 8f);
                currentRotation = currentRotation.normalized;
            }
        }

        /// <summary> 현재 차지 비율 (0~1). UI 게이지 등에 활용 가능 </summary>
        public float ChargeRatio => _chargeRatio;
    }
}
