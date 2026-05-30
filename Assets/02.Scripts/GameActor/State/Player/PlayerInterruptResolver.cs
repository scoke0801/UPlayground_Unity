using UPlayGround.Data.EnumType;
using UPlayGround.InputDefine;
using UPlayGround.Manager;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
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
        /// <summary>
        /// 마스크에 포함된 액션의 입력이 버퍼에 있으면 소비하고 전환한다.
        /// 전환이 일어나면 true. (Dash는 입력을 소비했더라도 조건부 전환에 실패하면 false —
        /// 이 경우 호출부는 기존처럼 콤보 등 후속 로직으로 fall-through 한다.)
        /// </summary>
        public static bool TryInterrupt(PlayerMovementController controller, PlayerInterruptAction mask)
        {
            if (controller == null || mask == PlayerInterruptAction.None)
                return false;

            var buffer = InputManager.Instance.InputBuffer;

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
                buffer.ConsumeInput(PlayerAction.Dash) != null)
            {
                // 대시는 조건부 전환. 실패 시 입력은 소비되었지만 전환은 일어나지 않는다(기존 동작 보존).
                return controller.TryTransitionToState(new PlayerDashState(controller));
            }

            if ((mask & PlayerInterruptAction.Guard) != 0 &&
                buffer.ConsumeInput(PlayerAction.Guard) != null)
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
            bool skill = (mask & PlayerInterruptAction.Skill) != 0 &&
                         HasUsableSkillInput(controller, playerActor);

            if (heavy && PlayerAttackState.TryEnter(controller, PlayerInterruptAction.HeavyAttack))
                return true;
            if (light && PlayerAttackState.TryEnter(controller, PlayerInterruptAction.LightAttack))
                return true;
            if (skill && PlayerAttackState.TryEnter(controller, PlayerInterruptAction.Skill))
                return true;

            return false;
        }

        /// <summary>
        /// 스킬 입력이 있고 게이지가 충분한지 확인. 게이지 검증을 여기서 하지 않으면
        /// 게이지 부족 시 TryEnter가 기본 약공으로 폴백해 Skill 마스크가 의도와 다르게 동작한다.
        /// </summary>
        private static bool HasUsableSkillInput(PlayerMovementController controller, PlayerActor playerActor)
        {
            var gauge = playerActor != null ? playerActor.SkillGauge : null;
            for (int i = 0; i < 10; i++)
            {
                if (!controller.HasSkillInput(i)) continue;
                if (gauge != null && !gauge.CanUseSkill(i)) continue;
                return true;
            }
            return false;
        }
    }
}
