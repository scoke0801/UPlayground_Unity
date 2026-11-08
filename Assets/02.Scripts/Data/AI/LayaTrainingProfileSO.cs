using UnityEngine;
using UPlayGround.Data.Actor;
using UPlayGround.AI.CombatDecision;

namespace UPlayGround.Data.AI
{
    public enum LayaTrainingMode { Native, Shadow, RemoteIntent }

    /// <summary>훈련장 원격 의도 실험의 대상과 시간·보관 예산.</summary>
    [CreateAssetMenu(menuName = "UPlayGround/AI/Laya 훈련 프로필")]
    public sealed class LayaTrainingProfileSO : ScriptableObject
    {
        public ActorDefinitionSO definition;
        public CombatIntent[] supportedIntents = { CombatIntent.Attack, CombatIntent.Punish,
            CombatIntent.Pressure, CombatIntent.Chase, CombatIntent.Retreat, CombatIntent.KeepDistance };
        [Min(0.1f)] public float requestInterval = 0.5f;
        [Min(0.1f)] public float validitySeconds = 0.75f;
        [Min(0.1f)] public float timeoutSeconds = 1f;
        [Min(1)] public int failureThreshold = 3;
        [Min(1f)] public float reconnectSeconds = 5f;
        [Min(8)] public int historyCapacity = 512;
        [Min(65536)] public int bodyBudgetBytes = 8388608;
        [Min(1)] public float displayHz = 5f;
        public float startDistance = 4f;
        public string model = "aac6fef/laya-multilingual-mlx";
        public string backend = "laya-mlx";
        public string revision = "ba40c87fcb357f1643d04d71323af9cdc3b9e591";
        public string configHash = "e98b441f42d45aa39c026cb3e7f2a0e0212406a76375370fbe70bbdefe6831d3";
        public string descriptionVersion = "runtime-approach-v1";
    }
}
