using System;
using UnityEngine;
using UPlayGround.Core.ReactiveEnvironment;

namespace UPlayGround.Data.ReactiveEnvironment
{
    /// <summary>GAS HitPhase가 소유하는 명시적 환경 충격량. 피해 DU와 원소량을 분리한다.</summary>
    [Serializable]
    public struct EnvironmentHitPayload
    {
        public EnvironmentImpactKind kind;
        [Min(0)] public float heatImpulse;
        [Min(0)] public float waterImpulse;
        /// <summary>이미 확정한 원소 예산을 실제 발사 수에 나누며 속성 부여를 다시 조회하지 않는다.</summary>
        public EnvironmentHitPayload DividedBy(int deliveries)
        {
            var payload = this;
            int count = Mathf.Max(1, deliveries);
            payload.heatImpulse /= count;
            payload.waterImpulse /= count;
            return payload;
        }

        public bool HasElements => heatImpulse > 0 || waterImpulse > 0;
        public bool IsValid => Enum.IsDefined(typeof(EnvironmentImpactKind), kind)
            && EnvironmentReaction.IsNonNegativeFinite(heatImpulse)
            && EnvironmentReaction.IsNonNegativeFinite(waterImpulse);
    }
}
