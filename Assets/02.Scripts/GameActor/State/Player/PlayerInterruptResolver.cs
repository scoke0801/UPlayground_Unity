using UPlayGround.Data.EnumType;
using UPlayGround.Components;
using UPlayGround.InputDefine;
using UPlayGround.Manager;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    public enum PlayerInterruptFailReason
    {
        None,
        ControllerMissing,
        NoAllowedAction,
        NoBufferedInput,
        StateGuardRejected,
        ResourceNotEnough,
        MotionMissing,
    }

    /// <summary>
    /// 인터럽트(동작 캔슬) 마스크를 받아 입력 버퍼를 우선순위 순으로 소비하고 해당 상태로 전환한다.
    /// 공격 상태/차지 상태 등에서 공통으로 사용한다.
    ///
    /// 우선순위: Dodge → Jump → Dash → Guard → 공격타입(Light/Heavy/Skill) (첫 매칭에서 종료).
    /// 이동/방어 캔슬은 전용 상태로, 공격타입 캔슬은 PlayerAttackState.TryEnter로 라우팅한다.
    /// 새 캔슬 액션 추가 = PlayerInterruptAction 플래그 1개 + 여기 분기 1줄.
    /// 호출부(상태)가 캔슬 윈도우(콜리전 비활성 구간)로 게이트하므로 이 메서드는 마스크만 평가한다.
    /// </summary>
    public static class PlayerInterruptResolver
    {
        public static PlayerInterruptFailReason LastFailReason { get; private set; } = PlayerInterruptFailReason.None;
        public static string LastFailDetail { get; private set; } = string.Empty;

        /// <summary>
        /// 마스크에 포함된 액션의 입력이 버퍼에 있으면 소비하고 전환한다.
        /// 전환이 일어나면 true. (Dash는 입력을 소비했더라도 조건부 전환에 실패하면 false —
        /// 이 경우 호출부는 기존처럼 콤보 등 후속 로직으로 fall-through 한다.)
        ///
        /// allowGuardCancel: 가드는 hold(level) 입력이라 윈드업처럼 캔슬창이 열린 초반에도 항상 "켜져" 있다.
        /// 패리/카운터 반격(마스크에 Guard 포함)의 윈드업에서 가드를 쥔 채로 시작하면 반격이 곧바로 가드로
        /// 튕겨나가므로, 호출부가 "액티브 히트가 한 번이라도 발생했는지"로 게이트한다(리커버리/멀티히트 간격만 허용).
        /// </summary>
        public static bool TryInterrupt(PlayerMovementController controller, PlayerInterruptAction mask,
            bool allowGuardCancel = true)
        {
            LastFailReason = PlayerInterruptFailReason.None;
            LastFailDetail = string.Empty;

            if (controller == null)
            {
                SetFail(PlayerInterruptFailReason.ControllerMissing, "PlayerMovementController가 없습니다.");
                return false;
            }

            if (mask == PlayerInterruptAction.None)
            {
                SetFail(PlayerInterruptFailReason.NoAllowedAction, "허용된 인터럽트 액션이 없습니다.");
                return false;
            }

            var buffer = Svc.Input.InputBuffer;
            bool hadMatchingInput = false;

            if ((mask & PlayerInterruptAction.Dodge) != 0 &&
                buffer.ConsumeInput(PlayerAction.Dodge) != null)
            {
                controller.TransitionToState(new PlayerDodgeState(controller));
                return true;
            }

            if ((mask & PlayerInterruptAction.Jump) != 0 &&
                buffer.ConsumeInput(PlayerAction.Jump) != null)
            {
                controller.TransitionToState(new PlayerAirborneState(controller));
                return true;
            }

            if ((mask & PlayerInterruptAction.Dash) != 0 &&
                buffer.HasInput(PlayerAction.Dash))
            {
                hadMatchingInput = true;
                if (controller.TryTransitionToState(new PlayerDashState(controller)))
                {
                    buffer.ConsumeInput(PlayerAction.Dash);
                    return true;
                }

                SetFail(PlayerInterruptFailReason.StateGuardRejected, "Dash 상태 전환 가드가 거부했습니다.");
                return false;
            }

            // 가드는 '쥐고 있는' 입력이라 순간 press 버퍼에 잡히지 않는다(Guard는 InputBuffer에 추가되지 않음).
            // hold 상태를 직접 검사해, 캔슬창이 열리는 첫 프레임에 즉시 가드로 전환한다(Idle/GroundMove 패턴과 동일).
            if (allowGuardCancel &&
                (mask & PlayerInterruptAction.Guard) != 0 &&
                controller.HasGuardInput())
            {
                controller.TransitionToState(new PlayerGuardState(controller));
                return true;
            }

            // 공격타입 캔슬: 허용된 공격 입력만 공격 상태 재진입에 위임한다.
            // TryEnter에 강제 액션을 넘겨 다른 공격 버퍼가 남아 있어도 마스크 밖 타입으로 라우팅되지 않게 한다.
            var playerActor = controller.Actor as PlayerActor;
            bool heavy = (mask & PlayerInterruptAction.HeavyAttack) != 0 &&
                         buffer.HasInput(PlayerAction.HeavyAttack);
            bool light = (mask & PlayerInterruptAction.LightAttack) != 0 &&
                         buffer.HasInput(PlayerAction.Attack);
            bool hasSkillInput = (mask & PlayerInterruptAction.Skill) != 0 && HasAnySkillInput(controller);
            bool skill = hasSkillInput && HasUsableSkillInput(controller, playerActor);

            if (heavy && PlayerAttackState.TryEnter(controller, PlayerInterruptAction.HeavyAttack))
                return true;
            if (light && PlayerAttackState.TryEnter(controller, PlayerInterruptAction.LightAttack))
                return true;
            if (skill && PlayerAttackState.TryEnter(controller, PlayerInterruptAction.Skill))
                return true;

            hadMatchingInput |= heavy || light || hasSkillInput;

            if (hasSkillInput && !skill)
                SetFail(PlayerInterruptFailReason.ResourceNotEnough, "스킬 입력은 있으나 사용 가능한 게이지가 없습니다.");
            else if (heavy || light || skill)
                SetFail(PlayerInterruptFailReason.MotionMissing, "공격 상태 진입 조건 또는 모션 보유 조건이 실패했습니다.");
            else if (!hadMatchingInput)
                SetFail(PlayerInterruptFailReason.NoBufferedInput, "허용된 액션에 대응하는 입력이 없습니다.");

            return false;
        }

        private static void SetFail(PlayerInterruptFailReason reason, string detail)
        {
            LastFailReason = reason;
            LastFailDetail = detail;
        }

        private static bool HasAnySkillInput(PlayerMovementController controller)
        {
            for (int i = 0; i < PlayerAbilityResourceView.SkillSlotCount; i++)
            {
                if (controller.HasSkillInput(i)) return true;
            }
            return false;
        }

        /// <summary>
        /// 스킬 입력이 있고 게이지가 충분한지 확인. 게이지 검증을 여기서 하지 않으면
        /// 게이지 부족 시 TryEnter가 기본 약공으로 폴백해 Skill 마스크가 의도와 다르게 동작한다.
        /// </summary>
        private static bool HasUsableSkillInput(PlayerMovementController controller, PlayerActor playerActor)
        {
            var gauge = playerActor != null ? playerActor.SkillGauge : null;
            for (int i = 0; i < PlayerAbilityResourceView.SkillSlotCount; i++)
            {
                if (!controller.HasSkillInput(i)) continue;
                if (gauge != null && !gauge.CanUseSkill(i)) continue;
                return true;
            }
            return false;
        }
    }
}
