using UnityEngine;
using UPlayGround.WorldBuilder.Core;

namespace UPlayGround.WorldBuilder.Data
{
    /// <summary>P04 생성 계층의 월드·계획 소유권과 저작 당시 결과를 기록한다.</summary>
    public sealed class WorldPlanningPreview : MonoBehaviour
    {
        public WorldDefinition definition;
        public WorldPlan plan;
        public string appliedHash;
    }
}
