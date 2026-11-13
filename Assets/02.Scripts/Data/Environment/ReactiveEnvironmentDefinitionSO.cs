using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Core.ReactiveEnvironment;
using UPlayGround.Data.Item;

namespace UPlayGround.Data.ReactiveEnvironment
{
    public enum EnvironmentSurfaceBackend { LegacyTint, ReactiveToon }
    /// <summary>외형과 별개로 환경의 반응·파괴 허용·원인별 보상을 저작한다.</summary>
    [CreateAssetMenu(menuName = "UPlayGround/Environment/반응형 환경 정의")]
    public sealed class ReactiveEnvironmentDefinitionSO : ScriptableObject
    {
        public string definitionId;
        [Min(1)] public int schemaVersion = 1;
        [Min(0.01f)] public float tickSeconds = 0.2f;
        public EnvironmentReactionParameters reaction = new();
        public List<ItemDropList> impactDrops = new();
        public List<ItemDropList> fireDrops = new();
        public Color wetTint = new(0.55f, 0.65f, 0.7f);
        public Color charTint = new(0.12f, 0.1f, 0.08f);
        [Min(0)] public float spreadRadius;
        [Min(0)] public float spreadHeatPerSecond;
        [Range(0, 1)] public float windInfluence = 0.5f;
        public LayerMask fireBlockerLayers;
        public int surfaceLayerId;
        public EnvironmentSurfaceBackend surfaceBackend;
        public EnvironmentDestructionRecipeSO destruction;
        public EnvironmentPresentationProfileSO presentation;

        /// <summary>누락 정책을 자동 기본값으로 보충하지 않고 실패시킨다.</summary>
        public bool IsValid() => !string.IsNullOrWhiteSpace(definitionId) && schemaVersion == 1
            && EnvironmentReaction.IsNonNegativeFinite(tickSeconds) && tickSeconds > 0
            && reaction != null && reaction.IsValid()
            && EnvironmentReaction.IsNonNegativeFinite(spreadRadius)
            && EnvironmentReaction.IsNonNegativeFinite(spreadHeatPerSecond)
            && EnvironmentReaction.IsNonNegativeFinite(windInfluence) && windInfluence <= 1
            && (spreadRadius == 0 || spreadHeatPerSecond == 0 || fireBlockerLayers.value != 0)
            && (destruction == null || destruction.IsValid) && (presentation == null || presentation.IsValid);
    }
}
