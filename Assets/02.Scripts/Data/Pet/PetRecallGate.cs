using System;

namespace UPlayGround.Data.Pet
{
    /// <summary>연속 원거리 이탈과 복귀 유예만 일반 지상 워프를 허용한다.</summary>
    public sealed class PetRecallGate
    {
        private float _farSeconds;
        private float _cooldown;
        public bool Tick(float distance, bool canRecall, float deltaTime, PetFollowProfileSO profile)
        {
            _cooldown = Math.Max(0f, _cooldown - deltaTime);
            _farSeconds = canRecall && distance >= profile.returnDistance ? _farSeconds + deltaTime : 0f;
            return canRecall && _farSeconds >= profile.recallDelay && _cooldown <= 0f;
        }
        public void ResetContinuity() => _farSeconds = 0f;
        public void MarkRecalled(PetFollowProfileSO profile)
        { _farSeconds = 0f; _cooldown = profile.recallCooldown; }
    }
}
