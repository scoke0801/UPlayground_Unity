using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Data.Cycle
{
    [System.Serializable]
    public sealed class CycleWorldAutoGenerationSettings
    {
        [Header("기능 스위치")]
        [Tooltip("사이클 시작 시 일반 조우, 루팅, 상호작용 자동 생성을 전체 활성화합니다.")]
        public bool enabled = true;

        [Tooltip("자동 배치 콘텐츠를 추적하는 런타임 검증 퀘스트를 자동 생성·수락합니다.")]
        public bool generateValidationQuest = true;

        [Header("난이도 구역별 조우")]
        [Min(0)] public int easyEncounterCount = 2;
        [Min(0)] public int normalEncounterCount = 3;
        [Min(0)] public int hardEncounterCount = 2;
        [Min(1)] public int easyThreatBudget = 4;
        [Min(1)] public int normalThreatBudget = 7;
        [Min(1)] public int hardThreatBudget = 11;
        [Min(1)] public int maxMonstersPerEncounter = 5;

        [Header("지역별 난이도 분포")]
        [Tooltip("활성 난이도 구역마다 모든 지역 경로에 최소 한 조우를 배치합니다. 각 구역 조우 수가 경로 수보다 적으면 생성을 거부합니다.")]
        public bool requireEveryRoutePerDifficultyZone;

        [Tooltip("플레이어에서 지역 앵커까지의 경로 중 쉬움 조우가 배치될 정규화 범위입니다.")]
        [Range(0f, 1f)] public float easyRouteMinProgress = 0.18f;
        [Range(0f, 1f)] public float easyRouteMaxProgress = 0.42f;

        [Tooltip("플레이어에서 지역 앵커까지의 경로 중 보통 조우가 배치될 정규화 범위입니다.")]
        [Range(0f, 1f)] public float normalRouteMinProgress = 0.46f;
        [Range(0f, 1f)] public float normalRouteMaxProgress = 0.68f;

        [Tooltip("플레이어에서 지역 앵커까지의 경로 중 어려움 조우가 배치될 정규화 범위입니다.")]
        [Range(0f, 1f)] public float hardRouteMinProgress = 0.72f;
        [Range(0f, 1f)] public float hardRouteMaxProgress = 0.94f;

        [Tooltip("루팅·상호작용을 분산할 전체 경로 진행률 범위입니다.")]
        [Range(0f, 1f)] public float auxiliaryRouteMinProgress = 0.16f;
        [Range(0f, 1f)] public float auxiliaryRouteMaxProgress = 0.92f;

        [Tooltip("자동 조우 후보에서 제외할 Actor ID입니다. 현재 Motion 매핑이 미완료된 몬스터를 기본 제외합니다.")]
        public List<string> excludedMonsterActorIds = new()
        {
            "Dryad",
            "Training_Dummy",
        };

        [Header("부가 콘텐츠")]
        [Min(0)] public int lootPickupCount = 6;
        [Min(0)] public int interactionTargetCount = 3;
        [Min(1)] public int lootCountPerPickup = 1;

        [Header("배치")]
        [Min(0f)] public float routeLateralJitter = 10f;
        [Min(0.5f)] public float monsterSpreadRadius = 3.5f;
        [Min(0f)] public float bossExclusionRadius = 14f;

        [Header("NavMesh 없는 KCC 지면 배치")]
        [Tooltip("배치 지면으로 인정할 레이어입니다. 기본값은 Ground입니다.")]
        public LayerMask placementGroundLayers = 1 << 3;

        [Tooltip("수직 표면 검사에 포함할 레이어입니다. 최상단 충돌체가 Ground Terrain이 아니면 거부합니다.")]
        public LayerMask placementSurfaceLayers =
            (1 << 0) | (1 << 2) | (1 << 3) | (1 << 4) | (1 << 9);

        [Tooltip("KCC 캡슐과 경로를 막는 레이어입니다. Ground Terrain은 별도 지면 검사로 처리합니다.")]
        public LayerMask placementObstacleLayers =
            (1 << 0) | (1 << 2) | (1 << 4) | (1 << 9);

        [Tooltip("Collider가 없는 물 표면도 배치에서 제외하기 위한 명시적 Material 목록입니다.")]
        public List<Material> excludedSurfaceMaterials = new();

        [Min(0f)] public float placementSearchRadius = 12f;
        [Min(0.5f)] public float placementSearchStep = 2f;
        [Min(1f)] public float groundProbeUpDistance = 8f;
        [Min(1f)] public float groundProbeDownDistance = 8f;
        [Tooltip("자동 생성 콘텐츠의 배치 후보를 Terrain에 투영할 수 있는 최대 거리입니다. 경로 앵커는 XZ 목표로 취급해 Ground Probe 범위에서 지면을 찾고, 중간 표본은 이전 Terrain 높이를 연속 추종합니다.")]
        [Min(0.05f)] public float maxGroundProjectionDistance = 4f;
        [Range(0f, 89f)] public float maxGroundSlopeAngle = 58f;
        [Min(0f)] public float maxGroundStepHeight = 0.45f;
        [Min(0.25f)] public float pathSampleSpacing = 1f;

        [Tooltip("직선 경로가 막혔을 때 탐색할 최대 측면 거리입니다.")]
        [Min(0.5f)] public float routeDetourMaxOffset = 12f;

        [Tooltip("결정론적 측면 우회 격자의 전진·측면 간격입니다.")]
        [Min(0.5f)] public float routeDetourStep = 2f;

        [Min(0.1f)] public float routeClearanceRadius = 0.5f;
        [Min(0.2f)] public float routeClearanceHeight = 2f;
        [Min(0f)] public float memberClearanceGap = 0.15f;

        public int TotalEncounterCount =>
            Mathf.Max(0, easyEncounterCount) +
            Mathf.Max(0, normalEncounterCount) +
            Mathf.Max(0, hardEncounterCount);

        public Vector2 GetDifficultyRouteRange(int difficultyZone)
        {
            return difficultyZone switch
            {
                <= 0 => new Vector2(easyRouteMinProgress, easyRouteMaxProgress),
                1 => new Vector2(normalRouteMinProgress, normalRouteMaxProgress),
                _ => new Vector2(hardRouteMinProgress, hardRouteMaxProgress),
            };
        }

        public bool Validate(out string error)
        {
            if (!enabled)
            {
                error = null;
                return true;
            }

            if (TotalEncounterCount <= 0)
            {
                error = "자동 생성 조우 수가 모두 0입니다.";
                return false;
            }

            if (easyThreatBudget <= 0 || normalThreatBudget < easyThreatBudget || hardThreatBudget < normalThreatBudget)
            {
                error = "위협 예산은 쉬움 > 보통 > 어려움 순으로 감소하지 않아야 하며 모두 1 이상이어야 합니다.";
                return false;
            }

            if (!IsNormalizedRange(easyRouteMinProgress, easyRouteMaxProgress) ||
                !IsNormalizedRange(normalRouteMinProgress, normalRouteMaxProgress) ||
                !IsNormalizedRange(hardRouteMinProgress, hardRouteMaxProgress) ||
                !IsNormalizedRange(auxiliaryRouteMinProgress, auxiliaryRouteMaxProgress) ||
                easyRouteMaxProgress > normalRouteMinProgress ||
                normalRouteMaxProgress > hardRouteMinProgress)
            {
                error = "쉬움·보통·어려움 경로 범위는 0~1 안에서 서로 겹치지 않고 가까운 곳부터 먼 곳 순서여야 합니다.";
                return false;
            }

            if (maxMonstersPerEncounter <= 0 || lootPickupCount < 0 || interactionTargetCount < 0 || lootCountPerPickup <= 0)
            {
                error = "자동 생성 수량 설정이 유효하지 않습니다.";
                return false;
            }

            if (placementGroundLayers.value == 0 ||
                (placementSurfaceLayers.value & placementGroundLayers.value) != placementGroundLayers.value ||
                placementObstacleLayers.value == 0)
            {
                error = "자동 생성 배치 레이어 설정이 유효하지 않습니다.";
                return false;
            }

            if (excludedSurfaceMaterials != null && excludedSurfaceMaterials.Count > 0)
            {
                for (int i = 0; i < excludedSurfaceMaterials.Count; i++)
                {
                    if (excludedSurfaceMaterials[i] != null) continue;
                    error = $"자동 생성 배치 제외 Material 참조가 유실되었습니다: index {i}";
                    return false;
                }

                const int exclusionProxyLayer = 1 << 2;
                if ((placementSurfaceLayers.value & exclusionProxyLayer) == 0 ||
                    (placementObstacleLayers.value & exclusionProxyLayer) == 0)
                {
                    error = "배치 제외 Material 프록시용 Ignore Raycast 레이어가 표면·장애물 Mask에 모두 포함되어야 합니다.";
                    return false;
                }
            }

            if (placementSearchRadius < 0f || placementSearchStep < 0.5f ||
                groundProbeUpDistance < 1f || groundProbeDownDistance < 1f ||
                maxGroundProjectionDistance <= 0f || maxGroundSlopeAngle < 0f || maxGroundSlopeAngle >= 90f ||
                maxGroundStepHeight < 0f || pathSampleSpacing < 0.25f ||
                routeDetourStep < 0.5f || routeDetourMaxOffset < routeDetourStep)
            {
                error = "자동 생성 KCC 지면·경로 검사 설정이 유효하지 않습니다.";
                return false;
            }

            if (routeClearanceRadius <= 0f || routeClearanceHeight < routeClearanceRadius * 2f || memberClearanceGap < 0f)
            {
                error = "자동 생성 KCC 캡슐 여유 설정이 유효하지 않습니다.";
                return false;
            }

            error = null;
            return true;
        }

        private static bool IsNormalizedRange(float min, float max)
        {
            return min >= 0f && max <= 1f && min < max;
        }
    }

    [CreateAssetMenu(fileName = "CycleWorldConfig", menuName = "UPlayGround/사이클/월드 설정")]
    public sealed class CycleWorldConfigSO : ScriptableObject
    {
        public string mapId;
        [Tooltip("P0 고정 플레이어 시작점의 CycleSpawnPoint.spawnId. 비어 있거나 씬에서 찾지 못하면 레이아웃 생성을 실패시킵니다.")]
        public string fixedPlayerSpawnId;
        public List<string> outerBossActorIds = new();
        public List<string> centralBossActorIds = new();
        [Min(1)] public int outerBossCount = 3;
        [Min(1)] public int maxSameSectorBossCount = 1;
        [Min(1)] public int baseMonsterLevel = 1;
        [Tooltip("별도 저작 작업 없이 사이클 검증용 일반 콘텐츠를 생성하는 설정입니다.")]
        public CycleWorldAutoGenerationSettings autoGeneration = new();

        public CycleWorldAutoGenerationSettings AutoGeneration =>
            autoGeneration ??= new CycleWorldAutoGenerationSettings();

        public bool Validate(out string error)
        {
            if (string.IsNullOrWhiteSpace(mapId)) { error = "mapId가 비어 있습니다."; return false; }
            if (string.IsNullOrWhiteSpace(fixedPlayerSpawnId)) { error = "fixedPlayerSpawnId가 비어 있습니다."; return false; }
            if (outerBossActorIds == null || outerBossActorIds.Count == 0) { error = "외곽 보스 풀이 비어 있습니다."; return false; }
            if (centralBossActorIds == null || centralBossActorIds.Count == 0) { error = "중앙 보스 풀이 비어 있습니다."; return false; }
            if (outerBossCount <= 0 || maxSameSectorBossCount <= 0) { error = "보스 수와 섹터 제한은 1 이상이어야 합니다."; return false; }
            if (!AutoGeneration.Validate(out error)) return false;
            error = null;
            return true;
        }
    }
}
