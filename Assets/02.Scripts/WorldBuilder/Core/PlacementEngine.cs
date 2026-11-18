using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UPlayGround.WorldBuilder.Core
{
    /// <summary>전역 후보를 만든 뒤 canonical 순서로 공간 충돌과 Override를 해결한다.</summary>
    public static class PlacementEngine
    {
        public static PlacementBuildResult Build(string worldGuid, int epoch, int seed, WorldGrid grid, TerrainBuildResult terrainResult, double tileSize,
            IReadOnlyList<PlacementRule> rules, IReadOnlyList<PlacementPrefab> prefabs, IReadOnlyList<PlacementOverride> overrides,
            IReadOnlyList<PlacementReservation> reservations, IReadOnlyList<int> tileOrder = null, bool parallel = false)
        {
            PlacementIdentity.ValidateGuid(worldGuid);
            PlacementIdentity.RequireFinite(tileSize);
            if (epoch < 0 || tileSize <= 0 || grid == null || grid.WorldSize / tileSize != Math.Floor(grid.WorldSize / tileSize)) throw new ArgumentException("epoch·타일 규격이 잘못됐습니다.");
            int count = checked((int)(grid.WorldSize / tileSize));
            var catalog = ValidatePrefabs(prefabs);
            var ruleMap = ValidateRules(rules);
            foreach (var rule in rules)
                if (Math.Pow(Math.Ceiling(grid.WorldSize / rule.CellSize), 2) * rule.Slots > 1000000)
                    throw new ArgumentException("규칙당 후보 한도 1,000,000을 초과했습니다. 격자 간격을 늘리세요.");
            ValidateOverrides(overrides);
            ValidateReservations(reservations, grid);
            var order = ValidateOrder(tileOrder, count * count);
            var parts = new List<Candidate>[order.Count];
            Action<int> generate = i => parts[i] = Generate(worldGuid, epoch, seed, grid, tileSize, count, order[i], rules, catalog);
            if (parallel) Parallel.For(0, order.Count, generate); else for (int i = 0; i < order.Count; i++) generate(i);
            var candidates = new List<Candidate>();
            foreach (var part in parts) candidates.AddRange(part);
            candidates.Sort((a, b) => string.CompareOrdinal(a.Record.sourceIdentity, b.Record.sourceIdentity));
            var result = new PlacementBuildResult();
            var identities = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var candidate in candidates) RegisterIdentity(candidate.Record, identities, result);
            if (result.HasErrors) return result;
            var fixedRecords = new List<PlacementRecord>();
            var byTarget = new Dictionary<string, List<PlacementOverride>>(StringComparer.Ordinal);
            var fixedIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in overrides)
            {
                if (item.kind == PlacementOverrideKind.Manual || item.kind == PlacementOverrideKind.Pin)
                {
                    var p = item.placement.Clone(); p.isFixed = true;
                    if (item.kind == PlacementOverrideKind.Pin && (item.epoch != epoch || item.seed != seed || !ruleMap.ContainsKey(p.ruleGuid ?? "") || !identities.ContainsKey(p.id)))
                        result.Reject(p.ruleGuid, p.id, PlacementRejection.OrphanOverride, false, item.overrideGuid);
                    if (!fixedIds.Add(p.id) || (identities.TryGetValue(p.id, out var source) && source != p.sourceIdentity))
                        result.Reject(p.ruleGuid, p.id, PlacementRejection.DuplicateId, true);
                    else if (!identities.ContainsKey(p.id)) identities.Add(p.id, p.sourceIdentity);
                    fixedRecords.Add(p);
                }
            }
            foreach (var item in overrides)
            {
                if (item.kind == PlacementOverrideKind.ExcludeRegion || item.kind == PlacementOverrideKind.Manual || item.kind == PlacementOverrideKind.Pin) continue;
                if ((!fixedIds.Contains(item.targetId) && (item.epoch != epoch || item.seed != seed)) || !identities.TryGetValue(item.targetId, out var identity) || identity != item.targetSourceIdentity)
                { result.Reject(null, item.targetId, PlacementRejection.OrphanOverride, false, item.overrideGuid); continue; }
                if (!byTarget.TryGetValue(item.targetId, out var list)) byTarget.Add(item.targetId, list = new List<PlacementOverride>());
                list.Add(item);
            }
            var retainedFixed = new List<PlacementRecord>();
            foreach (var p in fixedRecords)
            {
                if (byTarget.TryGetValue(p.id, out var edits))
                {
                    if (edits.Exists(value => value.kind == PlacementOverrideKind.Delete)) continue;
                    ApplyEdits(p, edits);
                }
                retainedFixed.Add(p);
            }
            fixedRecords = retainedFixed;
            var automatic = new List<PlacementRecord>();
            foreach (var candidate in candidates)
            {
                var p = candidate.Record;
                if (fixedIds.Contains(p.id)) continue;
                PlacementRejection reason = candidate.Rejection;
                if (byTarget.TryGetValue(p.id, out var edits))
                {
                    if (edits.Exists(value => value.kind == PlacementOverrideKind.Delete)) continue;
                    ApplyEdits(p, edits);
                    if (reason != PlacementRejection.None)
                    {
                        foreach (var edit in edits) result.Reject(p.ruleGuid, p.id, PlacementRejection.OrphanOverride, false, edit.overrideGuid);
                        continue;
                    }
                }
                if (reason != PlacementRejection.None) { result.Reject(p.ruleGuid, p.id, reason); continue; }
                automatic.Add(p);
            }
            var terrain = new PlacementTerrain(grid, terrainResult);

            Evaluate(terrain, grid, tileSize, count, automatic, fixedRecords, catalog, ruleMap, overrides, reservations, result);
            return result;
        }

        private sealed class Candidate { public PlacementRecord Record; public PlacementRejection Rejection; }

        private static void ApplyEdits(PlacementRecord p, List<PlacementOverride> edits)
        {
            foreach (var edit in edits)
            {
                if (edit.kind == PlacementOverrideKind.Transform)
                {
                    var t = edit.placement;
                    p.x = t.x; p.y = t.y; p.z = t.z; p.yaw = t.yaw;
                    p.scaleX = t.scaleX; p.scaleY = t.scaleY; p.scaleZ = t.scaleZ;
                    p.anchor = t.anchor; p.heightOffset = t.heightOffset; p.surfaceId = t.surfaceId; p.maxSupportDelta = t.maxSupportDelta;
                }
                if (edit.kind == PlacementOverrideKind.ReplacePrefab) p.prefabGuid = edit.prefabGuid;
            }
        }

        private static List<Candidate> Generate(string world, int epoch, int seed, WorldGrid grid, double tileSize, int tileCount, int tile,
            IReadOnlyList<PlacementRule> rules, Dictionary<string, PlacementPrefab> prefabs)
        {
            var result = new List<Candidate>();
            int tx = tile % tileCount, tz = tile / tileCount;
            foreach (var rule in rules)
            {
                int endX = (int)Math.Ceiling(Math.Min(grid.WorldSize, (tx + 1) * tileSize) / rule.CellSize);
                int endZ = (int)Math.Ceiling(Math.Min(grid.WorldSize, (tz + 1) * tileSize) / rule.CellSize);
                for (int z = (int)Math.Ceiling(tz * tileSize / rule.CellSize); z < endZ; z++)
                for (int x = (int)Math.Ceiling(tx * tileSize / rule.CellSize); x < endX; x++)
                for (int slot = 0; slot < rule.Slots; slot++)
                {
                    byte[] key = PlacementIdentity.Encode(world, epoch, rule.LayerGuid, rule.RuleGuid, x, z, slot);
                    byte[] random = PlacementIdentity.RandomKey(key, seed, rule.SeedSalt);
                    var p = new PlacementRecord { sourceIdentity = Convert.ToBase64String(key), layerGuid = rule.LayerGuid, ruleGuid = rule.RuleGuid,
                        prefabGuid = rule.PrefabGuid, epoch = epoch, seed = seed, spacing = rule.Spacing, maxSupportDelta = rule.MaxSupportDelta, surfaceId = rule.SurfaceId,
                        x = (x + .5 + (PlacementIdentity.Unit(random, 0) - .5) * rule.Jitter) * rule.CellSize,
                        z = (z + .5 + (PlacementIdentity.Unit(random, 4) - .5) * rule.Jitter) * rule.CellSize, yaw = PlacementIdentity.Unit(random, 8) * 360 };
                    p.id = PlacementIdentity.Id(p.sourceIdentity);
                    p.scaleX = p.scaleY = p.scaleZ = rule.MinScale + (rule.MaxScale - rule.MinScale) * PlacementIdentity.Unit(random, 12);
                    var reason = PlacementRejection.None;
                    if (!rule.Enabled) reason = PlacementRejection.RuleDisabled;
                    else if (p.x < rule.MinX || p.x >= rule.MaxX || p.z < rule.MinZ || p.z >= rule.MaxZ || p.x > grid.WorldSize || p.z > grid.WorldSize) reason = PlacementRejection.OutsideRegion;
                    else if (PlacementIdentity.Unit(random, 16) >= rule.Density) reason = PlacementRejection.DensityRejected;
                    result.Add(new Candidate { Record = p, Rejection = reason });
                }
            }
            return result;
        }

        private static void Evaluate(PlacementTerrain terrain, WorldGrid grid, double tileSize, int tileCount, List<PlacementRecord> automatic,
            List<PlacementRecord> fixedRecords, Dictionary<string, PlacementPrefab> prefabs, Dictionary<string, PlacementRule> rules,
            IReadOnlyList<PlacementOverride> overrides, IReadOnlyList<PlacementReservation> reservations, PlacementBuildResult result)
        {
            var spatial = new PlacementSpatialHash(tileSize);
            fixedRecords.Sort((a, b) => string.CompareOrdinal(a.id, b.id));
            foreach (var p in fixedRecords)
            {
                var reason = Prepare(p, prefabs, terrain, grid);
                if (reason == PlacementRejection.None) reason = terrain.CheckSupport(p);
                if (reason == PlacementRejection.None) reason = spatial.Check(p);
                if (reason != PlacementRejection.None) result.Reject(p.ruleGuid, p.id, PlacementRejection.ManualConflict, true);
                SetOwner(p, tileSize, tileCount); result.Records.Add(p); spatial.Add(p);
            }
            foreach (var p in automatic)
            {
                var rule = rules[p.ruleGuid];
                var reason = Prepare(p, prefabs, terrain, grid);
                if (reason == PlacementRejection.None && (terrain.Height(p.x, p.z) < rule.MinHeight || terrain.Height(p.x, p.z) > rule.MaxHeight)) reason = PlacementRejection.HeightRejected;
                if (reason == PlacementRejection.None && terrain.Slope(p.x, p.z) > rule.MaxSlope) reason = PlacementRejection.SlopeRejected;
                if (reason == PlacementRejection.None) reason = terrain.CheckSupport(p);
                if (reason == PlacementRejection.None && IsReserved(p, rule, overrides, reservations)) reason = PlacementRejection.ReservedArea;
                if (reason == PlacementRejection.None) reason = spatial.Check(p);
                if (reason != PlacementRejection.None) { result.Reject(p.ruleGuid, p.id, reason, HasTransformEdit(p.id, overrides)); continue; }
                SetOwner(p, tileSize, tileCount); result.Records.Add(p); spatial.Add(p);
            }
            foreach (var rule in rules.Values)
            {
                int actual = result.Records.FindAll(p => p.ruleGuid == rule.RuleGuid).Count;
                if (rule.Enabled && actual < Math.Max(1, rule.MinimumCount)) result.Reject(rule.RuleGuid, null, PlacementRejection.TargetCount, rule.Mandatory);
            }
            result.Records.Sort((a, b) => string.CompareOrdinal(a.id, b.id));
            result.Diagnostics.Sort((a, b) => string.CompareOrdinal((a.RuleGuid ?? "") + (a.CandidateId ?? "") + a.Reason + a.OverrideGuid, (b.RuleGuid ?? "") + (b.CandidateId ?? "") + b.Reason + b.OverrideGuid));
        }
        private static bool HasTransformEdit(string id, IReadOnlyList<PlacementOverride> overrides)
        {
            foreach (var edit in overrides) if (edit.targetId == id && (edit.kind == PlacementOverrideKind.ReplacePrefab || edit.kind == PlacementOverrideKind.Transform)) return true;
            return false;
        }
        private static PlacementRejection Prepare(PlacementRecord p, Dictionary<string, PlacementPrefab> prefabs, PlacementTerrain terrain, WorldGrid grid)
        {
            if (!prefabs.TryGetValue(p.prefabGuid ?? "", out var prefab)) return PlacementRejection.MissingPrefab;
            ValidateTransform(p);
            if (!prefab.SupportsNonUniformScale && (p.scaleX != p.scaleY || p.scaleX != p.scaleZ)) return PlacementRejection.ManualConflict;
            p.halfX = prefab.HalfX * p.scaleX; p.halfZ = prefab.HalfZ * p.scaleZ;
            if (p.x < 0 || p.z < 0 || p.x > grid.WorldSize || p.z > grid.WorldSize) return PlacementRejection.OutsideRegion;
            if (p.anchor == PlacementAnchor.Terrain) p.y = terrain.Height(p.x, p.z) + p.heightOffset + prefab.PivotHeight * p.scaleY;
            else if (Math.Abs(p.y - prefab.PivotHeight * p.scaleY - terrain.Height(p.x, p.z) - p.heightOffset) > p.maxSupportDelta) return PlacementRejection.Unsupported;
            return PlacementRejection.None;
        }
        private static void SetOwner(PlacementRecord p, double tileSize, int count)
        { p.ownerX = Math.Max(0, Math.Min(count - 1, (int)Math.Floor(p.x / tileSize))); p.ownerZ = Math.Max(0, Math.Min(count - 1, (int)Math.Floor(p.z / tileSize))); }
        private static bool IsReserved(PlacementRecord p, PlacementRule rule, IReadOnlyList<PlacementOverride> edits, IReadOnlyList<PlacementReservation> reservations)
        {
            foreach (var r in reservations)
            {
                if (r.boundary != null) { if (PlanningGeometry.Overlaps(PlanningGeometry.Footprint(p), r.boundary)) return true; }
                else if (PlacementSpatialHash.Overlaps(p, PlacementSpatialHash.Rectangle(r.minX, r.minZ, r.maxX, r.maxZ), 0, 0)) return true;
            }
            foreach (var r in edits) if (r.kind == PlacementOverrideKind.ExcludeRegion && (string.IsNullOrEmpty(r.ruleTag) || r.ruleTag == rule.Tag)
                && PlacementSpatialHash.Overlaps(p, PlacementSpatialHash.Rectangle(r.minX, r.minZ, r.maxX, r.maxZ), 0, 0)) return true;
            return false;
        }
        private static void RegisterIdentity(PlacementRecord p, Dictionary<string, string> identities, PlacementBuildResult result)
        {
            if (identities.ContainsKey(p.id)) result.Reject(p.ruleGuid, p.id, PlacementRejection.DuplicateId, true);
            else identities.Add(p.id, p.sourceIdentity);
        }
        private static Dictionary<string, PlacementPrefab> ValidatePrefabs(IReadOnlyList<PlacementPrefab> prefabs)
        {
            var result = new Dictionary<string, PlacementPrefab>(StringComparer.Ordinal);
            foreach (var p in prefabs)
            {
                PlacementIdentity.ValidateGuid(p.Guid); PlacementIdentity.RequireFinite(p.HalfX, p.HalfZ, p.PivotHeight);
                if (p.HalfX <= 0 || p.HalfZ <= 0) throw new ArgumentException("footprint는 양수여야 합니다.");
                result.Add(p.Guid, p);
            }
            return result;
        }
        private static Dictionary<string, PlacementRule> ValidateRules(IReadOnlyList<PlacementRule> rules)
        {
            var result = new Dictionary<string, PlacementRule>(StringComparer.Ordinal);
            foreach (var r in rules)
            {
                PlacementIdentity.ValidateGuid(r.LayerGuid); PlacementIdentity.ValidateGuid(r.RuleGuid); PlacementIdentity.ValidateGuid(r.PrefabGuid);
                PlacementIdentity.RequireFinite(r.CellSize, r.Jitter, r.Density, r.MinHeight, r.MaxHeight, r.MaxSlope, r.Spacing, r.MinScale, r.MaxScale, r.MaxSupportDelta);
                ValidateRect(r.MinX, r.MinZ, r.MaxX, r.MaxZ);
                if (r.CellSize <= 0 || r.Slots < 1 || r.Slots > 64 || r.Jitter < 0 || r.Jitter > 1 || r.Density < 0 || r.Density > 1 || r.MinHeight > r.MaxHeight || r.MaxSlope < 0 || r.MaxSlope > 90
                    || r.Spacing < 0 || r.MinScale <= 0 || r.MaxScale < r.MinScale || r.MaxSupportDelta < 0 || r.MinimumCount < 0) throw new ArgumentException("배치 규칙 범위가 잘못됐습니다.");
                result.Add(r.RuleGuid, r);
            }
            return result;
        }
        private static List<int> ValidateOrder(IReadOnlyList<int> order, int count)
        {
            var result = new List<int>(); var seen = new HashSet<int>();
            if (order == null) { for (int i = 0; i < count; i++) result.Add(i); return result; }
            foreach (int value in order) { if (value < 0 || value >= count || !seen.Add(value)) throw new ArgumentException("타일 순서는 전체 순열이어야 합니다."); result.Add(value); }
            if (result.Count != count) throw new ArgumentException("타일 순서에 누락이 있습니다.");
            return result;
        }
        private static void ValidateOverrides(IReadOnlyList<PlacementOverride> overrides)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal); var targets = new HashSet<string>(StringComparer.Ordinal);
            foreach (var edit in overrides)
            {
                PlacementIdentity.ValidateGuid(edit.overrideGuid);
                if (!ids.Add(edit.overrideGuid) || !Enum.IsDefined(typeof(PlacementOverrideKind), edit.kind)) throw new ArgumentException("중복/손상 Override입니다.");
                if (edit.kind == PlacementOverrideKind.ExcludeRegion) { ValidateRect(edit.minX, edit.minZ, edit.maxX, edit.maxZ); continue; }
                if (edit.kind == PlacementOverrideKind.Manual || edit.kind == PlacementOverrideKind.Pin)
                {
                    if (edit.placement == null) throw new ArgumentException("고정 배치 스냅샷이 필요합니다.");
                    ValidateTransform(edit.placement); PlacementIdentity.ValidateGuid(edit.placement.id);
                    if (string.IsNullOrEmpty(edit.placement.sourceIdentity)) throw new ArgumentException("sourceIdentity가 필요합니다.");
                    if (edit.kind == PlacementOverrideKind.Manual && (!edit.placement.sourceIdentity.StartsWith("manual:", StringComparison.Ordinal) || !MatchesIdentity(edit.placement.id, edit.placement.sourceIdentity)))
                        throw new ArgumentException("수동 UUID와 sourceIdentity가 다릅니다.");
                    if (edit.kind == PlacementOverrideKind.Pin && PlacementIdentity.Id(edit.placement.sourceIdentity) != edit.placement.id) throw new ArgumentException("Pin 정체성이 다릅니다.");
                    continue;
                }
                PlacementIdentity.ValidateGuid(edit.targetId);
                if (!MatchesIdentity(edit.targetId, edit.targetSourceIdentity)) throw new ArgumentException("대상 정체성이 다릅니다.");
                if (!targets.Add(edit.targetId + ":" + edit.kind)) throw new ArgumentException("동일 대상·종류 Override가 중복됐습니다.");
                if (edit.kind == PlacementOverrideKind.Transform) { if (edit.placement == null) throw new ArgumentException("변환값이 필요합니다."); ValidateTransform(edit.placement); }
                if (edit.kind == PlacementOverrideKind.ReplacePrefab) PlacementIdentity.ValidateGuid(edit.prefabGuid);
            }
        }
        private static bool MatchesIdentity(string id, string source)
        {
            if (string.IsNullOrEmpty(source)) return false;
            if (source.StartsWith("manual:", StringComparison.Ordinal)) return source.EndsWith(":" + id, StringComparison.Ordinal);
            return PlacementIdentity.Id(source) == id;
        }
        private static void ValidateTransform(PlacementRecord p)
        {
            PlacementIdentity.RequireFinite(p.x, p.y, p.z, p.yaw, p.scaleX, p.scaleY, p.scaleZ, p.heightOffset, p.spacing, p.maxSupportDelta);
            if (p.scaleX <= 0 || p.scaleY <= 0 || p.scaleZ <= 0 || p.spacing < 0 || p.maxSupportDelta < 0 || !Enum.IsDefined(typeof(PlacementAnchor), p.anchor)) throw new ArgumentException("변환·anchor 범위가 잘못됐습니다.");
        }
        private static void ValidateReservations(IReadOnlyList<PlacementReservation> reservations, WorldGrid grid)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var r in reservations) { PlacementIdentity.ValidateGuid(r.guid); if (!ids.Add(r.guid)) throw new ArgumentException("예약 ID 중복입니다."); ValidateRect(r.minX, r.minZ, r.maxX, r.maxZ); if (r.boundary != null) PlanningGeometry.ValidatePolygon(r.boundary, grid); }
        }
        private static void ValidateRect(double x0, double z0, double x1, double z1)
        { PlacementIdentity.RequireFinite(x0, z0, x1, z1); if (x1 <= x0 || z1 <= z0) throw new ArgumentException("영역 크기는 양수여야 합니다."); }
    }
}


