using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UPlayGround.Data.Cycle;
using UPlayGround.Data.Quest;
using UPlayGround.Data.Save;
using Random = System.Random;

namespace UPlayGround.World.Generation
{
    public readonly struct CycleGenerationRoutePoint
    {
        public CycleGenerationRoutePoint(string id, Vector3 position)
        {
            Id = id;
            Position = position;
        }

        public string Id { get; }
        public Vector3 Position { get; }
    }

    public readonly struct CycleMonsterCandidate
    {
        public CycleMonsterCandidate(string actorId, int difficultyTier, int threatCost)
        {
            ActorId = actorId;
            DifficultyTier = Mathf.Clamp(difficultyTier, 0, 2);
            ThreatCost = Mathf.Max(1, threatCost);
        }

        public string ActorId { get; }
        public int DifficultyTier { get; }
        public int ThreatCost { get; }
    }

    public sealed class CycleWorldGenerationRequest
    {
        public string mapId;
        public int cycleIndex;
        public int seed;
        public Vector3 playerPosition;
        public IReadOnlyList<CycleGenerationRoutePoint> routePoints;
        public IReadOnlyList<CycleMonsterCandidate> monsterCandidates;
        public IReadOnlyList<int> lootItemIds;
        public CycleWorldAutoGenerationSettings settings;
    }

    /// <summary>
    /// Unity 씬이나 매니저에 의존하지 않는 사이클 콘텐츠 계획기.
    /// 입력을 안정 정렬하고 콘텐츠 종류별 독립 RNG를 사용해 같은 시드의 결과를 재현한다.
    /// </summary>
    public static class CycleWorldGenerationPlanner
    {
        // v2: 직선 실패 시 결정론적 측면 격자 우회 경로를 사용한다.
        public const int PlacementValidationVersion = 2;

        public static bool TryBuild(
            CycleWorldGenerationRequest request,
            Random encounterRandom,
            Random lootRandom,
            Random interactionRandom,
            out CycleGeneratedContentLayout layout,
            out string error)
        {
            layout = null;
            error = null;
            if (request == null)
            {
                error = "자동 생성 요청이 없습니다.";
                return false;
            }

            CycleWorldAutoGenerationSettings settings = request.settings ?? new CycleWorldAutoGenerationSettings();
            if (!settings.Validate(out error)) return false;
            if (!settings.enabled)
            {
                layout = new CycleGeneratedContentLayout();
                return true;
            }

            List<CycleGenerationRoutePoint> routes = request.routePoints?
                .Where(value => !string.IsNullOrWhiteSpace(value.Id))
                .OrderBy(value => value.Id, StringComparer.Ordinal)
                .ToList() ?? new List<CycleGenerationRoutePoint>();
            if (routes.Count == 0)
            {
                error = "자동 난이도 구역을 만들 보스/경로 마커가 없습니다.";
                return false;
            }

            if (settings.requireEveryRoutePerDifficultyZone &&
                HasInsufficientRouteCoverage(settings, routes.Count, out int zone, out int encounterCount))
            {
                error = $"난이도 구역 {zone}의 조우 수 {encounterCount}개로 지역 경로 {routes.Count}개를 모두 덮을 수 없습니다.";
                return false;
            }

            List<CycleMonsterCandidate> monsters = request.monsterCandidates?
                .Where(value => !string.IsNullOrWhiteSpace(value.ActorId) && value.ThreatCost > 0)
                .OrderBy(value => value.ActorId, StringComparer.Ordinal)
                .ToList() ?? new List<CycleMonsterCandidate>();
            if (monsters.Count == 0)
            {
                error = "ActorDatabase에 자동 배치 가능한 일반 몬스터가 없습니다.";
                return false;
            }

            encounterRandom ??= new Random(request.seed);
            lootRandom ??= new Random(request.seed ^ 0x51F15EED);
            interactionRandom ??= new Random(request.seed ^ 0x2C1B3C6D);

            string generationId = $"{request.mapId}:cycle:{request.cycleIndex}:seed:{request.seed}";
            layout = new CycleGeneratedContentLayout
            {
                placementValidationVersion = PlacementValidationVersion,
                generationId = generationId,
                questId = $"cycle:auto:{request.mapId}:{request.cycleIndex}:{request.seed}",
            };

            BuildEncounters(request, settings, routes, monsters, encounterRandom, layout.encounters);
            BuildLoot(request, settings, routes, lootRandom, layout.loot);
            BuildInteractions(request, settings, routes, interactionRandom, layout.interactions);
            return true;
        }

        private static void BuildEncounters(
            CycleWorldGenerationRequest request,
            CycleWorldAutoGenerationSettings settings,
            IReadOnlyList<CycleGenerationRoutePoint> routes,
            IReadOnlyList<CycleMonsterCandidate> candidates,
            Random random,
            ICollection<CycleGeneratedEncounterPlacement> output)
        {
            int[] counts = { settings.easyEncounterCount, settings.normalEncounterCount, settings.hardEncounterCount };
            int[] budgets = { settings.easyThreatBudget, settings.normalThreatBudget, settings.hardThreatBudget };
            for (int zone = 0; zone < counts.Length; zone++)
            {
                int count = Mathf.Max(0, counts[zone]);
                Vector2 routeRange = settings.GetDifficultyRouteRange(zone);
                for (int slot = 0; slot < count; slot++)
                {
                    CycleGenerationRoutePoint route = routes[(zone + slot) % routes.Count];
                    RoutePlacementIntent intent = ResolveStratifiedRoutePosition(
                        request.playerPosition,
                        route.Position,
                        routeRange.x,
                        routeRange.y,
                        settings.routeLateralJitter,
                        random,
                        slot,
                        count);
                    Vector3 anchor = intent.Position;
                    CycleGeneratedEncounterPlacement encounter = new()
                    {
                        encounterId = $"{request.mapId}:encounter:z{zone}:{slot}",
                        routeId = route.Id,
                        routeProgress = intent.Progress,
                        lateralOffset = intent.LateralOffset,
                        difficultyZone = zone,
                        threatBudget = budgets[zone],
                        anchorPosition = new SerializableVector3(anchor),
                    };

                    List<CycleMonsterCandidate> roster = BuildRoster(
                        candidates,
                        zone,
                        budgets[zone],
                        settings.maxMonstersPerEncounter,
                        random);
                    for (int memberIndex = 0; memberIndex < roster.Count; memberIndex++)
                    {
                        float angle = (360f / Mathf.Max(1, roster.Count)) * memberIndex + NextFloat(random, -24f, 24f);
                        float radius = NextFloat(random, 0.8f, settings.monsterSpreadRadius);
                        Vector3 offset = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * radius;
                        CycleMonsterCandidate member = roster[memberIndex];
                        encounter.monsters.Add(new CycleGeneratedMonsterPlacement
                        {
                            actorId = member.ActorId,
                            threatCost = member.ThreatCost,
                            localOffset = new SerializableVector3(offset),
                            position = new SerializableVector3(anchor + offset),
                            yaw = NextFloat(random, 0f, 360f),
                        });
                    }

                    output.Add(encounter);
                }
            }
        }

        private static List<CycleMonsterCandidate> BuildRoster(
            IReadOnlyList<CycleMonsterCandidate> candidates,
            int zone,
            int budget,
            int maxMembers,
            Random random)
        {
            int remaining = Mathf.Max(1, budget);
            List<CycleMonsterCandidate> result = new();
            while (remaining > 0 && result.Count < Mathf.Max(1, maxMembers))
            {
                List<CycleMonsterCandidate> affordable = candidates
                    .Where(value => value.ThreatCost <= remaining && IsCandidateAllowed(value, zone))
                    .ToList();
                if (affordable.Count == 0)
                    break;

                int preferredTier = Mathf.Clamp(zone, 0, 2);
                List<CycleMonsterCandidate> preferred = affordable
                    .Where(value => value.DifficultyTier == preferredTier)
                    .ToList();
                List<CycleMonsterCandidate> pool = preferred.Count > 0 && random.NextDouble() < 0.7
                    ? preferred
                    : affordable;
                CycleMonsterCandidate selected = pool[random.Next(pool.Count)];
                result.Add(selected);
                remaining -= selected.ThreatCost;
            }

            if (result.Count == 0)
            {
                List<CycleMonsterCandidate> allowed = candidates
                    .Where(value => IsCandidateAllowed(value, zone))
                    .ToList();
                CycleMonsterCandidate fallback = (allowed.Count > 0 ? allowed : candidates)
                    .OrderBy(value => value.ThreatCost)
                    .ThenBy(value => value.ActorId, StringComparer.Ordinal)
                    .FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(fallback.ActorId))
                    result.Add(fallback);
            }

            return result;
        }

        private static bool IsCandidateAllowed(CycleMonsterCandidate candidate, int zone)
        {
            return zone switch
            {
                0 => candidate.DifficultyTier <= 1,
                1 => candidate.DifficultyTier <= 2,
                _ => candidate.DifficultyTier >= 1,
            };
        }

        private static void BuildLoot(
            CycleWorldGenerationRequest request,
            CycleWorldAutoGenerationSettings settings,
            IReadOnlyList<CycleGenerationRoutePoint> routes,
            Random random,
            ICollection<CycleGeneratedLootPlacement> output)
        {
            List<int> itemIds = request.lootItemIds?
                .Where(value => value > 0)
                .Distinct()
                .OrderBy(value => value)
                .ToList() ?? new List<int>();
            if (itemIds.Count == 0) return;

            int count = Mathf.Max(0, settings.lootPickupCount);
            for (int slot = 0; slot < count; slot++)
            {
                CycleGenerationRoutePoint route = routes[slot % routes.Count];
                RoutePlacementIntent intent = ResolveStratifiedRoutePosition(
                    request.playerPosition,
                    route.Position,
                    settings.auxiliaryRouteMinProgress,
                    settings.auxiliaryRouteMaxProgress,
                    settings.routeLateralJitter * 0.65f,
                    random,
                    slot,
                    count);
                output.Add(new CycleGeneratedLootPlacement
                {
                    lootId = $"{request.mapId}:loot:{slot}",
                    routeId = route.Id,
                    routeProgress = intent.Progress,
                    lateralOffset = intent.LateralOffset,
                    itemId = itemIds[random.Next(itemIds.Count)],
                    count = Mathf.Max(1, settings.lootCountPerPickup),
                    position = new SerializableVector3(intent.Position),
                });
            }
        }

        private static void BuildInteractions(
            CycleWorldGenerationRequest request,
            CycleWorldAutoGenerationSettings settings,
            IReadOnlyList<CycleGenerationRoutePoint> routes,
            Random random,
            ICollection<CycleGeneratedInteractionPlacement> output)
        {
            int count = Mathf.Max(0, settings.interactionTargetCount);
            for (int slot = 0; slot < count; slot++)
            {
                CycleGenerationRoutePoint route = routes[(slot + 1) % routes.Count];
                RoutePlacementIntent intent = ResolveStratifiedRoutePosition(
                    request.playerPosition,
                    route.Position,
                    settings.auxiliaryRouteMinProgress,
                    settings.auxiliaryRouteMaxProgress,
                    settings.routeLateralJitter * 0.8f,
                    random,
                    slot,
                    count);
                output.Add(new CycleGeneratedInteractionPlacement
                {
                    interactionId = $"{request.mapId}:interaction:{slot}",
                    routeId = route.Id,
                    routeProgress = intent.Progress,
                    lateralOffset = intent.LateralOffset,
                    position = new SerializableVector3(intent.Position),
                });
            }
        }

        private static RoutePlacementIntent ResolveStratifiedRoutePosition(
            Vector3 origin,
            Vector3 destination,
            float minT,
            float maxT,
            float lateralJitter,
            Random random,
            int stratumIndex,
            int stratumCount)
        {
            int count = Mathf.Max(1, stratumCount);
            int index = Mathf.Clamp(stratumIndex, 0, count - 1);
            float stratumMin = Mathf.Lerp(minT, maxT, index / (float)count);
            float stratumMax = Mathf.Lerp(minT, maxT, (index + 1f) / count);
            float t = NextFloat(random, stratumMin, stratumMax);
            Vector3 direction = destination - origin;
            direction.y = 0f;
            Vector3 perpendicular = direction.sqrMagnitude > 0.001f
                ? Vector3.Cross(Vector3.up, direction.normalized)
                : Vector3.right;
            float lateral = NextFloat(
                random,
                -Mathf.Max(0f, lateralJitter),
                Mathf.Max(0f, lateralJitter));
            return new RoutePlacementIntent(
                Vector3.Lerp(origin, destination, t) + perpendicular * lateral,
                t,
                lateral);
        }

        private static bool HasInsufficientRouteCoverage(
            CycleWorldAutoGenerationSettings settings,
            int routeCount,
            out int difficultyZone,
            out int encounterCount)
        {
            int[] counts =
            {
                settings.easyEncounterCount,
                settings.normalEncounterCount,
                settings.hardEncounterCount,
            };
            for (int zone = 0; zone < counts.Length; zone++)
            {
                int count = Mathf.Max(0, counts[zone]);
                if (count == 0 || count >= routeCount) continue;
                difficultyZone = zone;
                encounterCount = count;
                return true;
            }

            difficultyZone = -1;
            encounterCount = 0;
            return false;
        }

        private readonly struct RoutePlacementIntent
        {
            public RoutePlacementIntent(Vector3 position, float progress, float lateralOffset)
            {
                Position = position;
                Progress = progress;
                LateralOffset = lateralOffset;
            }

            public Vector3 Position { get; }
            public float Progress { get; }
            public float LateralOffset { get; }
        }

        private static float NextFloat(Random random, float min, float max)
        {
            if (Mathf.Approximately(min, max)) return min;
            return min + (float)random.NextDouble() * (max - min);
        }
    }

    /// <summary>
    /// 저장 가능한 사이클 레이아웃에서 런타임 QuestSO를 만들기 위한 순수 데이터 초안.
    /// QuestDatabase에는 등록하지 않으며 같은 레이아웃이면 항상 같은 결과를 만든다.
    /// </summary>
    public sealed class CycleGeneratedQuestDraft
    {
        public string questId;
        public string questName;
        public string shortSummary;
        public string questDescription;
        public bool alreadyCompleted;
        public List<QuestObjectiveData> objectives = new();
    }

    public static class CycleGeneratedQuestAuthoringPlanner
    {
        public static bool TryBuild(
            CycleRunState run,
            CycleLayoutState layout,
            out CycleGeneratedQuestDraft draft,
            out string error)
        {
            draft = null;
            error = null;
            if (run == null)
            {
                error = "사이클 실행 상태가 없습니다.";
                return false;
            }

            CycleGeneratedContentLayout generated = layout?.generatedContent;
            if (generated == null)
            {
                error = "자동 생성 콘텐츠 레이아웃이 없습니다.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(generated.questId))
            {
                error = "자동 생성 퀘스트 ID가 비어 있습니다.";
                return false;
            }

            draft = new CycleGeneratedQuestDraft
            {
                questId = generated.questId,
                questName = $"사이클 {run.cycleIndex} 자동 검증",
                shortSummary = "자동 배치된 전투·루팅·상호작용·보스를 검증한다.",
                questDescription =
                    $"시드 {run.seed}로 생성된 {run.mapId} 사이클 월드의 전체 게임 흐름 검증 퀘스트.",
                alreadyCompleted = true,
            };

            int encounterCount = generated.encounters?.Count(value => value != null) ?? 0;
            if (encounterCount > 0)
            {
                draft.objectives.Add(CreateObjective(
                    "encounters",
                    "자동 생성 조우 완료",
                    QuestObjectiveType.EncounterClear,
                    encounterCount));
                draft.alreadyCompleted &= generated.encounters.All(value => value == null || value.cleared);
            }

            int bossCount = (layout.outerBosses?.Count(value => value != null) ?? 0) +
                            (layout.centralBoss != null ? 1 : 0);
            if (bossCount > 0)
            {
                draft.objectives.Add(CreateObjective(
                    "bosses",
                    "외곽 보스와 중앙 보스 처치",
                    QuestObjectiveType.CycleBossDefeat,
                    bossCount));
                draft.alreadyCompleted &=
                    (layout.outerBosses?.All(value => value == null || value.defeated) ?? true) &&
                    (layout.centralBoss == null || layout.centralBoss.defeated);
            }

            int lootCount = generated.loot?
                .Where(value => value != null)
                .Sum(value => Mathf.Max(1, value.count)) ?? 0;
            if (lootCount > 0)
            {
                draft.objectives.Add(CreateObjective(
                    "loot",
                    "자동 배치 루팅 아이템 획득",
                    QuestObjectiveType.CycleLootCollect,
                    lootCount));
                draft.alreadyCompleted &= generated.loot.All(value => value == null || value.collected);
            }

            int interactionCount = generated.interactions?.Count(value => value != null) ?? 0;
            if (interactionCount > 0)
            {
                draft.objectives.Add(CreateObjective(
                    "interactions",
                    "자동 배치 대상과 상호작용",
                    QuestObjectiveType.InteractionComplete,
                    interactionCount));
                draft.alreadyCompleted &= generated.interactions.All(value => value == null || value.completed);
            }

            if (draft.objectives.Count > 0) return true;

            draft = null;
            error = "자동 생성 퀘스트에 추가할 목표가 없습니다.";
            return false;
        }

        private static QuestObjectiveData CreateObjective(
            string suffix,
            string description,
            QuestObjectiveType type,
            int requiredCount)
        {
            return new QuestObjectiveData
            {
                objectiveId = $"cycle_auto_{suffix}",
                description = description,
                type = type,
                requiredCount = Mathf.Max(1, requiredCount),
            };
        }
    }
}
