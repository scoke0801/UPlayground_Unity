using UPlayGround.InputDefine;
using UPlayGround.Manager;

namespace UPlayGround.State
{
    /// <summary>
    /// 약(Attack)/강(HeavyAttack) 선입력이 버퍼에 동시에 남아 있을 때 어느 쪽을 실행할지 중재한다.
    ///
    /// 기존에는 진입 지점마다 "강 먼저 검사"(PlayerInterruptResolver, PlayerAttackState.OnEnter) 또는
    /// "약 먼저 검사"(콤보 윈도우)로 우선순위가 제각각이었고, 그 결과 버퍼에 남은 강 입력이
    /// 나중에 누른 약 입력을 이기고 강공격이 나가는 문제가 있었다.
    /// 여기서는 항상 "더 최근에 누른 쪽"이 이긴다. 한쪽만 있으면 그쪽이 이긴다.
    /// </summary>
    public static class PlayerAttackInputArbiter
    {
        /// <summary>
        /// 강공격이 승자인지. 강 입력이 있고, 약 입력이 없거나 강 입력이 더 최근일 때 true.
        /// 소비하지 않는다.
        /// </summary>
        public static bool IsHeavyPreferred()
        {
            return TryPeekAttackInput(out bool isHeavy) && isHeavy;
        }

        /// <summary>
        /// 약/강 중 다음 실행 후보를 소비하지 않고 조회한다.
        /// 콤보 종료 경계에서 입력을 보존한 채 새 체인 진입 가능성을 검사할 때 사용한다.
        /// </summary>
        public static bool TryPeekAttackInput(out bool isHeavy)
        {
            isHeavy = false;

            var buffer = Svc.Input?.InputBuffer;
            if (buffer == null) return false;

            var heavy = buffer.PeekInput(PlayerAction.HeavyAttack);
            var light = buffer.PeekInput(PlayerAction.Attack);
            if (heavy == null && light == null) return false;

            isHeavy = light == null || (heavy != null && heavy.Sequence > light.Sequence);
            return true;
        }

        /// <summary>
        /// 약/강 중 승자를 반환하고 두 타입의 대기 입력을 모두 소비한다.
        /// 둘 다 없으면 false를 반환하고 아무것도 소비하지 않는다.
        /// </summary>
        public static bool TryConsumeAttackInput(out bool isHeavy)
        {
            isHeavy = false;

            if (!TryPeekAttackInput(out isHeavy)) return false;

            var buffer = Svc.Input.InputBuffer;
            // 공격 상태는 "다음 공격" 의도를 하나만 소유한다.
            // 승자만 소비하고 오래된 반대 타입을 남기면 입력을 멈춘 뒤에
            // 잔류 공격이 자동 실행되므로 두 타입을 함께 제거한다.
            buffer.ConsumeInput(PlayerAction.Attack);
            buffer.ConsumeInput(PlayerAction.HeavyAttack);
            return true;
        }

    }
}
