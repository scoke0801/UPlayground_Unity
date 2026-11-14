using System.Collections.Generic;
using UPlayGround.Data.ReactiveEnvironment;

namespace UPlayGround.Combat
{
    /// <summary>한 투사체의 접촉·폭발·장판이 대상별 단발 원소를 중복 공급하지 않게 한다.</summary>
    public sealed class EnvironmentHitImpulseBudget
    {
        private readonly HashSet<IDamageable> _targets = new();
        public void Clear() => _targets.Clear();
        public EnvironmentHitPayload Consume(IDamageable target, EnvironmentHitPayload payload)
        {
            if (_targets.Add(target)) return payload;
            payload.heatImpulse = 0f;
            payload.waterImpulse = 0f;
            return payload;
        }
    }
}
