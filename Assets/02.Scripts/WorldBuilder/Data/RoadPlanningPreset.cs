using UnityEngine;
using UPlayGround.WorldBuilder.Core;

namespace UPlayGround.WorldBuilder.Data
{
    /// <summary>보행 폭·식생 제외 폭·곡률·탐색 비용과 도로 표현을 데이터로 관리한다.</summary>
    [CreateAssetMenu(fileName = "RoadPlanningPreset", menuName = "UPlayGround/WorldBuilder/Road Preset")]
    public sealed class RoadPlanningPreset : ScriptableObject
    {
        public Material material;
        public float width = 4, exclusionWidth = 8, falloff = 6, maxSlope = 12, maxCutFill = 3, sampleStep = 1, searchStep = 2, slopeCost = 4, earthworkCost = 1, maxCurvature = 1, uvRepeat = 4, surfaceOffset = .025f;
        public int maxVisited = 250000;
        public RoadOptions ToCore() => new RoadOptions { Width = width, ExclusionWidth = exclusionWidth, Falloff = falloff, MaxSlope = maxSlope, MaxCutFill = maxCutFill, SampleStep = sampleStep,
            SearchStep = searchStep, SlopeCost = slopeCost, EarthworkCost = earthworkCost, MaxCurvature = maxCurvature, UvRepeat = uvRepeat, SurfaceOffset = surfaceOffset, MaxVisited = maxVisited };
    }
}
