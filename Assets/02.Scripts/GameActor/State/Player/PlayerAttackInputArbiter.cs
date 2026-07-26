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
    /// (강 입력은 release 시점에 0.24s로 버퍼링되고, 액티브 히트 구간에서는 InputBuffer 만료가
    ///  정지되므로 오래된 강 입력이 그대로 살아남는다.)
    ///
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
            var buffer = Svc.Input?.InputBuffer;
            if (buffer == null) return false;

            var heavy = buffer.PeekInput(PlayerAction.HeavyAttack);
            if (heavy == null) return false;

            var light = buffer.PeekInput(PlayerAction.Attack);
            return light == null || heavy.Timestamp >= light.Timestamp;
        }

        /// <summary>
        /// 약/강 중 승자를 소비한다. 패자 입력은 그대로 두어 후속 콤보 선입력으로 쓰이게 한다.
        /// 둘 다 없으면 false를 반환하고 아무것도 소비하지 않는다.
        /// </summary>
        public static bool TryConsumeAttackInput(out bool isHeavy)
        {
            isHeavy = false;

            var buffer = Svc.Input?.InputBuffer;
            if (buffer == null) return false;

            var heavy = buffer.PeekInput(PlayerAction.HeavyAttack);
            var light = buffer.PeekInput(PlayerAction.Attack);

            if (heavy == null && light == null) return false;

            isHeavy = light == null || (heavy != null && heavy.Timestamp >= light.Timestamp);
            buffer.ConsumeInput(isHeavy ? PlayerAction.HeavyAttack : PlayerAction.Attack);
            return true;
        }

        /// <summary>
        /// 강공격이 승자일 때만 소비한다. 약 입력은 건드리지 않는다.
        /// 약 입력을 여기서 소비하면 안 되는 경로(승자가 약이면 Idle/Move로 빠져 Idle이 다시 처리)에 쓴다.
        /// </summary>
        public static bool TryConsumeHeavyIfPreferred()
        {
            if (!IsHeavyPreferred()) return false;

            Svc.Input.InputBuffer.ConsumeInput(PlayerAction.HeavyAttack);
            return true;
        }
    }
}
