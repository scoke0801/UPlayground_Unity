using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KinematicCharacterController;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UPlayGround.WorldBuilder.Core;
using UPlayGround.WorldBuilder.Data;

namespace UPlayGround.WorldBuilder.Editor
{
    public enum BakeSeverity { Info, Warning, Error }

    [Serializable]
    public sealed class WorldBakeIssue
    {
        public BakeSeverity severity;
        public string code, message, sourceGuid, elementId, fixHint;
        public Bounds worldBounds;
        public bool hasLocation;
    }

    [Serializable]
    public sealed class WorldBakeReport
    {
        public List<WorldBakeIssue> issues = new();
        public string warningApprover, warningReason;
        public bool HasErrors => issues.Any(i => i.severity == BakeSeverity.Error);
        public bool CanPublish => !HasErrors && (!issues.Any(i => i.severity == BakeSeverity.Warning) ||
            (!string.IsNullOrWhiteSpace(warningApprover) && !string.IsNullOrWhiteSpace(warningReason)));

        public void Add(string code, string message, string sourceGuid, string hint, string element = null, Vector3? position = null,
            BakeSeverity severity = BakeSeverity.Error)
        {
            issues.Add(new WorldBakeIssue { severity = severity, code = code, message = message, sourceGuid = sourceGuid,
                fixHint = hint, elementId = element, hasLocation = position.HasValue, worldBounds = new Bounds(position.GetValueOrDefault(), Vector3.zero) });
        }
    }

    public static class WorldBakeValidation
    {
        public static WorldBakeReport Validate(WorldDefinition definition, WorldNavigationLayout layout)
        {
            var report = new WorldBakeReport();
            string world = definition != null ? definition.WorldGuid : null;
            var scene = SceneManager.GetActiveScene();
            if (definition == null || EditorApplication.isPlayingOrWillChangePlaymode || !scene.IsValid() ||
                scene.path != AssetDatabase.GUIDToAssetPath(definition.PreviewSceneGuid))
            {
                report.Add("WB101", "대상 월드의 저작 씬을 Edit Mode에서 여세요.", world, "Definition과 연결된 씬을 엽니다.");
                return report;
            }
            if (scene.isDirty || EditorUtility.IsDirty(definition) || definition.ActiveSession != null || string.IsNullOrEmpty(definition.AppliedTerrainHash))
                report.Add("WB120", "저장되지 않은 저작 또는 활성 지형 편집 세션이 있습니다.", world, "저작 원본을 저장하고 Sculpt/RAW를 Capture 또는 Revert합니다.");
            if (report.HasErrors) return report;
            Terrain[,] terrains = null;
            TerrainBuildResult physical = null;
            Run("WB110", "Terrain 좌표·경계를 확인합니다.", () =>
            {
                terrains = WorldBuilderAuthoringAssets.FindTerrains(definition);
                for (int z = 0; z < terrains.GetLength(0); z++)
                for (int x = 0; x < terrains.GetLength(1); x++)
                {
                    var tile = terrains[z, x];
                    if (tile == null)
                    {
                        report.Add("WB110", "Terrain 타일이 빠졌습니다.", world, "지정된 월드 타일을 복원합니다.",
                            WorldNavigationBake.TileId(x, z), new Vector3(x * definition.Profile.TileCells, definition.Profile.OriginY, z * definition.Profile.TileCells));
                        continue;
                    }
                    if (!tile.isActiveAndEnabled || !tile.TryGetComponent<TerrainCollider>(out var collider) || !collider.enabled ||
                        collider.isTrigger || collider.terrainData != tile.terrainData)
                        report.Add("WB110", "원본 TerrainCollider가 표시 지형과 일치하지 않습니다.", world,
                            "TerrainCollider의 활성 상태와 TerrainData를 확인합니다.", tile.name, tile.transform.position);
                }
                if (report.HasErrors) return;
                physical = new WorldTerrainStateAdapter(definition.Profile, terrains).Read();
            });
            if (physical == null) return report;

            Run("WB120", "P02 스택과 native Terrain을 확인합니다.", () =>
            {
                using var terrain = new WorldTerrainAuthoringSession(definition, terrains);
                terrain.EnsureReadyForBake();
            });
            Run("WB200", "수동 배치에 충돌 또는 미캡처 변경이 있는지 확인합니다.", () =>
            {
                if (definition.PlacementCatalog == null && definition.PlacementRules.Count == 0 && definition.Placements.Count == 0) return;
                WorldPlacementAuthoring.EnsurePreviewClean(definition);
                var logical = TerrainOperationEvaluator.Build(definition.ReadSource(), definition.Profile.CreateGrid(), definition.ReadOperations());
                var placements = WorldPlacementAuthoring.Evaluate(definition, logical);
                foreach (var issue in placements.Diagnostics.Where(i => i.IsError || i.Reason == PlacementRejection.OrphanOverride))
                    report.Add(issue.Reason == PlacementRejection.OrphanOverride ? "WB201" : "WB200", issue.Reason.ToString(),
                        world, "Override 및 배치 규칙을 확인합니다.", issue.OverrideGuid ?? issue.CandidateId,
                        severity: issue.IsError ? BakeSeverity.Error : BakeSeverity.Warning);
                if (!placements.Records.Select(p => p.id).ToHashSet(StringComparer.Ordinal).SetEquals(definition.Placements.Select(p => p.id)) ||
                    placements.Records.Count != definition.Placements.Count)
                    throw new InvalidDataException("적용된 배치 ID가 현재 소스 평가와 다릅니다.");
            });
            if (definition.Planning != null)
                Run("WB210", "건물·교량·도로 미캡처 편집과 계획 상태를 확인합니다.", () =>
                {
                    WorldPlanningAuthoring.EnsurePreviewClean(definition, definition.Planning, null);
                    var evaluated = WorldPlanningAuthoring.Evaluate(definition, definition.Planning);
                    foreach (var d in evaluated.Diagnostics)
                        report.Add("WB210", d.message, world, "부지·소켓·필수 동선을 확인합니다.", d.elementGuid);
                });
            if (definition.WaterSystem != null)
                Run("WB410", "수계·폭포 원본과 적용 계층을 확인합니다.", () =>
                {
                    WorldWaterSceneBuilder.EnsureClean(definition);
                    WorldWaterAuthoring.ValidateTerrainChange(definition, physical, definition.Operations);
                    var water = WorldWaterAuthoring.Evaluate(definition, definition.WaterSystem);
                    if (water.HasErrors) throw new InvalidDataException("수계 원본에 적용되지 않은 충돌이 있습니다.");
                });
            if (definition.Ecology != null)
                Run("WB300", "동굴 피복·포털·식생 적용 상태를 확인합니다.", () =>
                {
                    WorldEcologyAuthoring.EnsureClean(definition, terrains);
                    CaveEvaluation.Build(definition, definition.Ecology, physical, definition.ReadOperations());
                });
            Run("WB500", "필수 경로와 실제 KCC/AI 프로필을 연결합니다.", () => ValidateLayout(definition, layout));
            report.Add("WB501", "미검사 · 저장한 Navigation의 필수 경로와 링크는 Bake 후 검사합니다.", world,
                "Bake 보고서의 경로 검사 결과를 확인합니다.", severity: BakeSeverity.Info);
            foreach (var root in scene.GetRootGameObjects())
            foreach (var node in root.GetComponentsInChildren<Transform>(true))
            {
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(node.gameObject) > 0)
                    report.Add("WB600", "Missing Script", world, "씬에서 누락된 컴포넌트를 확인합니다.", node.name, node.position);
                if (node.TryGetComponent<Renderer>(out var renderer))
                foreach (var material in renderer.sharedMaterials)
                    if (material == null || material.shader == null || !material.shader.isSupported || material.shader.name == "Hidden/InternalErrorShader")
                        report.Add("WB600", "지원되지 않거나 누락된 재질", world, "렌더러의 재질과 셰이더를 확인합니다.", node.name, node.position);
            }
            return report;

            void Run(string code, string hint, Action check)
            {
                try { check(); }
                catch (Exception error)
                {
                    if (error is CaveConflict cave) report.Add(code, error.Message, world, hint, cave.ElementId, cave.Position);
                    else report.Add(code, error.Message, world, hint);
                }
            }
        }

        internal static NavMeshBuildSettings ValidateLayout(WorldDefinition definition, WorldNavigationLayout layout)
        {
            if (layout == null || layout.worldGuid != definition.WorldGuid || !EditorUtility.IsPersistent(layout) || EditorUtility.IsDirty(layout))
                throw new InvalidDataException("같은 월드의 저장된 Navigation Layout이 필요합니다.");
            var agent = NavMesh.GetSettingsByID(layout.agentTypeId);
            if (agent.agentTypeID != layout.agentTypeId || agent.agentRadius <= 0 || agent.agentHeight <= 0 ||
                layout.playerPrefab == null || !EditorUtility.IsPersistent(layout.playerPrefab) || !layout.playerPrefab.GetComponentInChildren<KinematicCharacterMotor>(true) ||
                !float.IsFinite(layout.endpointTolerance) || layout.endpointTolerance <= 0 || !float.IsFinite(layout.sourceHalo) ||
                layout.sourceHalo < agent.agentRadius || layout.routes == null || layout.routes.Length == 0 || layout.crossings == null)
                throw new InvalidDataException("필수 동선, KCC, AI 설정, 경계 halo 또는 snap 허용값이 잘못됐습니다.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var crossing in layout.crossings)
            {
                PlacementIdentity.ValidateGuid(crossing.id);
                bool sameSurface = crossing.fromSurface == crossing.toSurface;
                bool localBridge = !string.IsNullOrEmpty(crossing.bridgeId) && string.IsNullOrEmpty(crossing.portalId) &&
                    (crossing.fromSurface?.StartsWith("tile_", StringComparison.Ordinal) ?? false) && crossing.from != crossing.to;
                if (!ids.Add(crossing.id) || (sameSurface && !localBridge) || string.IsNullOrWhiteSpace(crossing.fromSurface) ||
                    string.IsNullOrWhiteSpace(crossing.toSurface) || !float.IsFinite(crossing.width) || crossing.width < 0)
                    throw new InvalidDataException("Navigation 링크 ID, 양 끝 surface 또는 폭이 잘못됐습니다.");
                Finite(crossing.from); Finite(crossing.to);
            }
            foreach (var route in layout.routes)
            {
                PlacementIdentity.ValidateGuid(route.id);
                if (!ids.Add(route.id) || string.IsNullOrWhiteSpace(route.fromSurface) || string.IsNullOrWhiteSpace(route.toSurface) ||
                    (!string.IsNullOrEmpty(route.requiredCrossingId) && !layout.crossings.Any(c => c.id == route.requiredCrossingId)))
                    throw new InvalidDataException("필수 경로 ID, surface 또는 링크가 잘못됐습니다.");
                Finite(route.from); Finite(route.to);
            }
            if (definition.Planning != null)
            {
                foreach (var road in definition.Planning.roads)
                {
                    if (string.IsNullOrEmpty(road.requiredBridgeGuid)) continue;
                    var crossing = layout.crossings.SingleOrDefault(c => c.bridgeId == road.requiredBridgeGuid);
                    if (crossing == null || !layout.routes.Any(route => route.requiredCrossingId == crossing.id))
                        throw new InvalidDataException("필수 도로의 교량 링크와 이동 경로가 필요합니다 · " + road.roadGuid);
                }
                foreach (var poi in definition.Planning.pois.Where(poi => poi.isLocked))
                {
                    var point = new Vector3((float)poi.anchor.x, (float)poi.anchor.y, (float)poi.anchor.z);
                    if (!layout.routes.Any(route => Vector3.Distance(point, route.from) <= layout.endpointTolerance * 2 ||
                        Vector3.Distance(point, route.to) <= layout.endpointTolerance * 2))
                        throw new InvalidDataException("고정 POI를 잇는 필수 경로 끝점이 없습니다 · " + poi.guid);
                }
            }
            if (definition.Ecology != null)
            {
                if (layout.playerPrefab != definition.Ecology.playerPrefab)
                    throw new InvalidDataException("Navigation과 동굴 저작 평가에 같은 플레이어 KCC Prefab을 지정하세요.");
                var exterior = definition.Ecology.caves.SelectMany(cave => cave.modules.SelectMany(module =>
                    module.preset.sockets.Where(socket => socket.exterior).Select(socket =>
                        PlanningIdentity.Derive(definition.WorldGuid, cave.caveGuid, module.moduleId, socket.id)))).ToHashSet(StringComparer.Ordinal);
                var linked = layout.crossings.Where(c => !string.IsNullOrEmpty(c.portalId)).Select(c => c.portalId).ToArray();
                if (!exterior.SetEquals(linked) || exterior.Count != linked.Length)
                    throw new InvalidDataException("외부 동굴 포털과 Navigation 링크가 일대일로 연결돼야 합니다.");
            }
            return agent;
        }

        private static void Finite(Vector3 point) => PlacementIdentity.RequireFinite(point.x, point.y, point.z);
    }
}



