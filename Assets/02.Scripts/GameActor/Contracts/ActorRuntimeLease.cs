using System;

namespace UPlayGround
{
    /// <summary>
    /// 액터 런타임 상태 홀드를 한 번만 해제하는 공용 리스.
    /// 중첩 홀드를 카운트로 관리하는 코드가 해제 콜백만 다르게 쓰는 경우가 반복되므로 공용화했다.
    /// </summary>
    public sealed class ActorRuntimeLease : IDisposable
    {
        private Action _release;

        public ActorRuntimeLease(Action release)
        {
            _release = release;
        }

        public void Dispose()
        {
            Action release = _release;
            _release = null;
            release?.Invoke();
        }
    }
}
