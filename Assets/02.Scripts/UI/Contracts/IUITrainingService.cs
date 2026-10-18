using System.Collections.Generic;
using UPlayGround.Data.Ability;
using UPlayGround.Data.UI;
using UPlayGround.Manager;
using UnityEngine;

namespace UPlayGround.UI
{
    public enum TrainingEncounter { Single, Mixed, Boss }

    /// <summary>훈련 화면이 씬에 요청하는 전투 준비 계약.</summary>
    public interface IUITrainingService : IGameService
    {
        TrainingGroundConfigSO Config { get; }
        bool HasEncounter { get; }
        bool IsInvincible { get; }
        bool IsEnemyStopped { get; }
        IReadOnlyList<MonsterActor> Opponents { get; }
        void Configure(TrainingGroundConfigSO config);
        void SetInvincible(bool value);
        void SetEnemyStopped(bool value);
        bool TryStart(TrainingEncounter kind, int tier, int weapon, int boss, float distance,
            Vector2[] formation, out string message);
        void SetPracticeAttack(MonsterActor actor, GameplayAbilitySO ability);
        void Restart();
    }
}
