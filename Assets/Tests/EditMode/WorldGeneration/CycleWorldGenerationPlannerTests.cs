using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Cycle;
using UPlayGround.Data.Quest;
namespace UPlayGround.World.Generation.Tests
{
    public sealed class CycleWorldGenerationPlannerTests
    {
        [Test]
        public void LakeOfLifeConfig_UsesNavMeshFreeKccGroundPlacement()
        {
            const string path = "Assets/10.Datas/Cycle/P0/CycleWorld_lakeoflife.asset";
            CycleWorldConfigSO config = AssetDatabase.LoadAssetAtPath<CycleWorldConfigSO>(path);

            Assert.That(config, Is.Not.Null, path);
            Assert.That(config.AutoGeneration.enabled, Is.True);
            Assert.That(config.AutoGeneration.generateValidationQuest, Is.True);
            Assert.That(config.AutoGeneration.placementGroundLayers.value, Is.EqualTo(1 << 3));
            Assert.That(config.AutoGeneration.excludedSurfaceMaterials.Count, Is.EqualTo(4));
            Assert.That(config.AutoGeneration.excludedSurfaceMaterials.All(value => value != null), Is.True);
            Assert.That(config.AutoGeneration.placementSurfaceLayers.value & (1 << 2), Is.Not.Zero);
            Assert.That(config.AutoGeneration.placementObstacleLayers.value & (1 << 2), Is.Not.Zero);
            Assert.That(config.AutoGeneration.routeDetourMaxOffset, Is.EqualTo(12f));
            Assert.That(config.AutoGeneration.routeDetourStep, Is.EqualTo(2f));
            Assert.That(config.AutoGeneration.requireEveryRoutePerDifficultyZone, Is.True);
            Assert.That(config.AutoGeneration.easyEncounterCount, Is.EqualTo(4));
            Assert.That(config.AutoGeneration.normalEncounterCount, Is.EqualTo(4));
            Assert.That(config.AutoGeneration.hardEncounterCount, Is.EqualTo(4));
            Assert.That(config.AutoGeneration.easyRouteMaxProgress,
                Is.LessThanOrEqualTo(config.AutoGeneration.normalRouteMinProgress));
            Assert.That(config.AutoGeneration.normalRouteMaxProgress,
                Is.LessThanOrEqualTo(config.AutoGeneration.hardRouteMinProgress));
            Assert.That(config.Validate(out string error), Is.True, error);
        }

        [Test]
        public void RegionalDifficultyDistribution_CoversEveryRouteAndUsesStratifiedProgress()
        {
            CycleWorldGenerationRequest request = CreateRequest();
            request.settings.easyEncounterCount = request.routePoints.Count;
            request.settings.normalEncounterCount = request.routePoints.Count;
            request.settings.hardEncounterCount = request.routePoints.Count;
            request.settings.requireEveryRoutePerDifficultyZone = true;
            request.settings.lootPickupCount = 6;
            request.settings.interactionTargetCount = 6;

            Assert.That(TryBuild(request, out CycleGeneratedContentLayout layout), Is.True);
            string[] expectedRoutes = request.routePoints.Select(value => value.Id).OrderBy(value => value).ToArray();
            for (int zone = 0; zone < 3; zone++)
            {
                CycleGeneratedEncounterPlacement[] encounters = layout.encounters
                    .Where(value => value.difficultyZone == zone)
                    .ToArray();
                Assert.That(encounters.Select(value => value.routeId).OrderBy(value => value),
                    Is.EqualTo(expectedRoutes));

                Vector2 range = request.settings.GetDifficultyRouteRange(zone);
                Assert.That(encounters.All(value => value.routeProgress >= range.x && value.routeProgress <= range.y),
                    Is.True);
                Assert.That(encounters.Select(value => value.routeProgress), Is.Ordered.Ascending);
            }

            Assert.That(layout.loot.Select(value => value.routeProgress), Is.Ordered.Ascending);
            Assert.That(layout.interactions.Select(value => value.routeProgress), Is.Ordered.Ascending);
            Assert.That(layout.loot.Select(value => value.routeId).Distinct().Count(),
                Is.EqualTo(request.routePoints.Count));
            Assert.That(layout.interactions.Select(value => value.routeId).Distinct().Count(),
                Is.EqualTo(request.routePoints.Count));
        }

        [Test]
        public void RequiredRegionalCoverage_WhenZoneCountIsTooSmall_FailsExplicitly()
        {
            CycleWorldGenerationRequest request = CreateRequest();
            request.settings.requireEveryRoutePerDifficultyZone = true;

            bool success = CycleWorldGenerationPlanner.TryBuild(
                request,
                null,
                null,
                null,
                out _,
                out string error);

            Assert.That(success, Is.False);
            Assert.That(error, Does.Contain("모두 덮을 수 없습니다"));
        }

        [Test]
        public void GeneratedPlacements_PreserveRouteIntentForGroundProjection()
        {
            CycleWorldGenerationRequest request = CreateRequest();
            Assert.That(TryBuild(request, out CycleGeneratedContentLayout layout), Is.True);

            Assert.That(
                layout.placementValidationVersion,
                Is.EqualTo(CycleWorldGenerationPlanner.PlacementValidationVersion));
            Assert.That(layout.encounters.All(value => !string.IsNullOrWhiteSpace(value.routeId)), Is.True);
            Assert.That(layout.encounters.All(value => value.routeProgress is >= 0f and <= 1f), Is.True);
            Assert.That(layout.loot.All(value => !string.IsNullOrWhiteSpace(value.routeId)), Is.True);
            Assert.That(layout.interactions.All(value => !string.IsNullOrWhiteSpace(value.routeId)), Is.True);

            foreach (CycleGeneratedEncounterPlacement encounter in layout.encounters)
            {
                Vector3 anchor = encounter.anchorPosition.ToVector3();
                foreach (CycleGeneratedMonsterPlacement member in encounter.monsters)
                {
                    Vector3 expected = anchor + member.localOffset.ToVector3();
                    Assert.That(member.position.x, Is.EqualTo(expected.x).Within(0.0001f));
                    Assert.That(member.position.y, Is.EqualTo(expected.y).Within(0.0001f));
                    Assert.That(member.position.z, Is.EqualTo(expected.z).Within(0.0001f));
                }
            }
        }

        [Test]
        public void SameSeed_ProducesSamePlan()
        {
            CycleWorldGenerationRequest request = CreateRequest();

            Assert.That(TryBuild(request, out CycleGeneratedContentLayout first), Is.True);
            Assert.That(TryBuild(request, out CycleGeneratedContentLayout second), Is.True);

            Assert.That(JsonUtility.ToJson(second), Is.EqualTo(JsonUtility.ToJson(first)));
        }

        [Test]
        public void DifferentSeed_ChangesGeneratedPlan()
        {
            CycleWorldGenerationRequest firstRequest = CreateRequest();
            CycleWorldGenerationRequest secondRequest = CreateRequest();
            secondRequest.seed++;

            Assert.That(TryBuild(firstRequest, out CycleGeneratedContentLayout first), Is.True);
            Assert.That(TryBuild(secondRequest, out CycleGeneratedContentLayout second), Is.True);

            Assert.That(JsonUtility.ToJson(second), Is.Not.EqualTo(JsonUtility.ToJson(first)));
        }

        [Test]
        public void LootCountChange_DoesNotChangeEncounterPlan()
        {
            CycleWorldGenerationRequest firstRequest = CreateRequest();
            CycleWorldGenerationRequest secondRequest = CreateRequest();
            secondRequest.settings.lootPickupCount = 1;

            Assert.That(TryBuild(firstRequest, out CycleGeneratedContentLayout first), Is.True);
            Assert.That(TryBuild(secondRequest, out CycleGeneratedContentLayout second), Is.True);

            Assert.That(EncounterSignature(second), Is.EqualTo(EncounterSignature(first)));
            Assert.That(second.loot.Count, Is.Not.EqualTo(first.loot.Count));
        }

        [Test]
        public void DifficultyZones_UseNonDecreasingThreatBudgets()
        {
            CycleWorldGenerationRequest request = CreateRequest();
            Assert.That(TryBuild(request, out CycleGeneratedContentLayout layout), Is.True);

            int[] budgets = layout.encounters
                .OrderBy(value => value.difficultyZone)
                .Select(value => value.threatBudget)
                .ToArray();
            Assert.That(budgets, Is.EqualTo(new[] { 4, 7, 11 }));
            Assert.That(layout.encounters.All(value => value.monsters.Count > 0), Is.True);
            Assert.That(
                layout.encounters.All(value =>
                    value.monsters.Sum(monster => monster.threatCost)
                    <= value.threatBudget),
                Is.True);
        }

        [Test]
        public void QuestAuthoring_UsesSavedLayoutCountsAndCycleIdentity()
        {
            CycleWorldGenerationRequest request = CreateRequest();
            Assert.That(TryBuild(request, out CycleGeneratedContentLayout generated), Is.True);
            CycleLayoutState layout = CreateLayout(generated);
            CycleRunState run = new()
            {
                mapId = request.mapId,
                cycleIndex = request.cycleIndex,
                seed = request.seed,
            };

            Assert.That(
                CycleGeneratedQuestAuthoringPlanner.TryBuild(run, layout, out CycleGeneratedQuestDraft draft, out string error),
                Is.True,
                error);

            Assert.That(draft.questId, Is.EqualTo("cycle:auto:test_map:1:777"));
            Assert.That(draft.questDescription, Does.Contain("test_map").And.Contain("777"));
            Assert.That(draft.alreadyCompleted, Is.False);
            Assert.That(draft.objectives.Select(value => value.type), Is.EqualTo(new[]
            {
                QuestObjectiveType.EncounterClear,
                QuestObjectiveType.CycleBossDefeat,
                QuestObjectiveType.CycleLootCollect,
                QuestObjectiveType.InteractionComplete,
            }));
            Assert.That(draft.objectives.Select(value => value.requiredCount), Is.EqualTo(new[] { 3, 4, 4, 2 }));
        }

        [Test]
        public void QuestAuthoring_WhenSavedLayoutIsComplete_DoesNotRequireRecompletion()
        {
            CycleWorldGenerationRequest request = CreateRequest();
            Assert.That(TryBuild(request, out CycleGeneratedContentLayout generated), Is.True);
            CycleLayoutState layout = CreateLayout(generated);
            foreach (CycleGeneratedEncounterPlacement encounter in generated.encounters) encounter.cleared = true;
            foreach (CycleGeneratedLootPlacement loot in generated.loot) loot.collected = true;
            foreach (CycleGeneratedInteractionPlacement interaction in generated.interactions) interaction.completed = true;
            foreach (CycleBossPlacement boss in layout.outerBosses) boss.defeated = true;
            layout.centralBoss.defeated = true;

            Assert.That(
                CycleGeneratedQuestAuthoringPlanner.TryBuild(
                    new CycleRunState { mapId = request.mapId, cycleIndex = request.cycleIndex, seed = request.seed },
                    layout,
                    out CycleGeneratedQuestDraft draft,
                    out string error),
                Is.True,
                error);
            Assert.That(draft.alreadyCompleted, Is.True);
        }

        private static bool TryBuild(CycleWorldGenerationRequest request, out CycleGeneratedContentLayout layout)
        {
            return CycleWorldGenerationPlanner.TryBuild(
                request,
                null,
                null,
                null,
                out layout,
                out _);
        }

        private static string EncounterSignature(CycleGeneratedContentLayout layout)
        {
            return string.Join(
                "|",
                layout.encounters.Select(encounter =>
                    $"{encounter.encounterId}:{encounter.anchorPosition.x:R}:{encounter.anchorPosition.z:R}:" +
                    string.Join(",", encounter.monsters.Select(monster =>
                        $"{monster.actorId}@{monster.position.x:R},{monster.position.z:R},{monster.yaw:R}"))));
        }

        private static CycleWorldGenerationRequest CreateRequest()
        {
            return new CycleWorldGenerationRequest
            {
                mapId = "test_map",
                cycleIndex = 1,
                seed = 777,
                playerPosition = Vector3.zero,
                routePoints = new List<CycleGenerationRoutePoint>
                {
                    new("outer_a", new Vector3(80f, 0f, 20f)),
                    new("outer_b", new Vector3(-40f, 0f, 70f)),
                    new("central", new Vector3(0f, 0f, 120f)),
                },
                monsterCandidates = new List<CycleMonsterCandidate>
                {
                    new("weak", 0, 1),
                    new("normal", 1, 2),
                    new("elite", 2, 5),
                },
                lootItemIds = new[] { 10, 20, 30 },
                settings = new CycleWorldAutoGenerationSettings
                {
                    easyEncounterCount = 1,
                    normalEncounterCount = 1,
                    hardEncounterCount = 1,
                    easyThreatBudget = 4,
                    normalThreatBudget = 7,
                    hardThreatBudget = 11,
                    maxMonstersPerEncounter = 5,
                    lootPickupCount = 4,
                    interactionTargetCount = 2,
                },
            };
        }

        private static CycleLayoutState CreateLayout(CycleGeneratedContentLayout generated)
        {
            return new CycleLayoutState
            {
                generatedContent = generated,
                outerBosses = new List<CycleBossPlacement>
                {
                    new() { spawnId = "outer_a" },
                    new() { spawnId = "outer_b" },
                    new() { spawnId = "outer_c" },
                },
                centralBoss = new CycleBossPlacement { spawnId = "central", isCentral = true },
            };
        }
    }
}
