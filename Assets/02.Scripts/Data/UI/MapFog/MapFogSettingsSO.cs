using UnityEngine;

namespace UPlayGround.Data.UI
{
    /// <summary>지도 탐험 안개의 공통 설정. 지도별 예외는 <see cref="MinimapIconConfigSO"/>에 둔다.</summary>
    [CreateAssetMenu(fileName = "MapFogSettings", menuName = "UPlayGround/UI/Map Fog Settings")]
    public sealed class MapFogSettingsSO : ScriptableObject
    {
        [Tooltip("안개 색·구름 무늬를 정하는 재질 (UPlayGround/UI/MapFog 셰이더)")]
        public Material fogMaterial;

        [Header("밝혀지는 반경 (지도 크기 비례)")]
        [Tooltip("지도 긴 변 길이에 곱하는 비율")]
        [Range(0.005f, 0.2f)] public float revealRadiusRatio = 0.03f;
        [Min(1f)] public float minRevealRadius = 18f;
        [Min(1f)] public float maxRevealRadius = 90f;

        [Header("격자")]
        [Tooltip("지도 긴 변을 나누는 최대 칸 수. 클수록 경계가 정교하고 저장·텍스처가 커진다.")]
        [Range(64, 1024)] public int maxCellsPerSide = 384;
        [Min(0.5f)] public float minCellSize = 2f;

        [Header("기록")]
        [Tooltip("플레이어 위치를 확인하는 간격(초)")]
        [Min(0.05f)] public float recordInterval = 0.2f;

        public float ResolveRevealRadius(MinimapIconConfigSO config, float mapLongSide)
        {
            if (config != null && config.fogRevealRadius > 0f) return config.fogRevealRadius;
            return Mathf.Clamp(mapLongSide * revealRadiusRatio,
                minRevealRadius, Mathf.Max(minRevealRadius, maxRevealRadius));
        }

        public float ResolveCellSize(float mapLongSide) =>
            Mathf.Max(minCellSize, mapLongSide / Mathf.Max(1, maxCellsPerSide));
    }
}
