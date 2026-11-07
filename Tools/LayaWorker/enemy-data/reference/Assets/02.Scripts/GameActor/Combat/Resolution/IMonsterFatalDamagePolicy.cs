using System;

namespace UPlayGround.Combat
{
    public enum MonsterFatalDamageResolution
    {
        Unhandled = 0,
        Prevented = 1,
        Incapacitated = 2,
    }

    /// <summary>사망 보상 경로에 진입하기 전 필수 액터의 치명 피해를 다른 수명 상태로 전환한다.</summary>
    public interface IMonsterFatalDamagePolicy
    {
        MonsterFatalDamageResolution ResolveFatalDamage(
            MonsterActor victim,
            in HitRequest request,
            float requestedDamage,
            out float appliedDamage);
    }

    internal sealed class MonsterFatalDamagePolicyLease : IDisposable
    {
        private Action _release;

        public MonsterFatalDamagePolicyLease(Action release) => _release = release;

        public void Dispose()
        {
            Action release = _release;
            _release = null;
            release?.Invoke();
        }
    }
}
