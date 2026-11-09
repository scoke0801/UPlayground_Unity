using UnityEngine;
using UPlayGround.State;

namespace UPlayGround
{
    public partial class PlayerActor
    {
        /// <summary>머리가 수면 아래인지. 잠수 경고 연출과 스태미나 소모가 같은 기준을 쓴다.</summary>
        public bool IsUnderwater => (MovementController?.CurrentState as PlayerSwimState)?.IsUnderwater == true;

        /// <summary>남은 숨(스태미나) 비율 0~1. 스태미나가 없으면 가득 찬 것으로 본다.</summary>
        public float BreathRatio => Stamina?.Ratio ?? 1f;

        /// <summary>
        /// 잠수 중 스태미나가 바닥나 익사한다. 무적·방어 판정을 거치지 않는 환경 즉사이므로
        /// 피격 경로 대신 생명력을 직접 소진시키고 일반 사망 흐름을 탄다.
        /// </summary>
        public void Drown()
        {
            if (!IsAlive()) return;

            AbilitySystem.ApplyResolvedDamage(_currentHealth + _maxHealth);
            if (_currentHealth > 0f) _currentHealth = 0f;
            OnHpChanged?.Invoke(_currentHealth, _maxHealth);
            OnDeath(null);
        }
    }
}
