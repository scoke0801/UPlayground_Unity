using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UPlayGround.WorldBuilder.Core;
using UPlayGround.WorldBuilder.Data;

namespace UPlayGround.WorldBuilder.Editor
{
    internal sealed class WorldVegetationEvaluation
    {
        private const int BiomeWeightSalt = 0x42494F4D;

        internal sealed class Tile
        {
            public float[,,] Alpha;
            public int[][,] Details;
            public readonly List<TreeInstance> Trees = new();
        }

        public Tile[,] Tiles;
        public TerrainLayer[] Layers;
        public TreePrototype[] TreePrototypes;
        public DetailPrototype[] DetailPrototypes;
        public readonly List<EcologyTreeRecord> Trees = new();

        internal static List<PlacementRule> PrefabRules(WorldDefinition definition, TerrainBuildResult terrain,
            out Func<PlacementRecord, PlacementRule, PlacementRejection> filter)
        {
            var source = definition.Ecology;
            var mask = new WorldEcologyMask(definition, terrain);
            var biomes = source.biomes.OrderByDescending(b => b.priority).ThenBy(b => b.biomeId, StringComparer.Ordinal).ToArray();
            var authored = biomes.ToDictionary(b => b, b => b.mask == null || b.mask.Length == 0 ? null :
                b.mask.Select(p => new PlanPoint(p.x, 0, p.y)).ToArray());
            var byRule = source.vegetation.Where(v => v.path == VegetationPath.Prefab).ToDictionary(v => v.placementRule.ruleGuid);
            var rules = new List<PlacementRule>();
            foreach (var item in byRule.Values)
            {
                var rule = item.placementRule.ToCore();
                rule.Density = item.density;
                rule.Spacing = item.spacing;
                rule.MinScale = item.scale.x;
                rule.MaxScale = item.scale.y;
                rules.Add(rule);
            }
            filter = (candidate, rule) =>
            {
                if (rule == null || !byRule.TryGetValue(rule.RuleGuid, out var preset))
                    return candidate.isFixed ? mask.CheckNewFixedPlacementConstraints(candidate, source.ecologyGuid) : PlacementRejection.None;
                var clearance = candidate.Clone();
                clearance.halfX = Math.Max(clearance.halfX, preset.footprintRadius * candidate.scaleX);
                clearance.halfZ = Math.Max(clearance.halfZ, preset.footprintRadius * candidate.scaleZ);
                var rejection = mask.Check(clearance, rule.Tag);
                if (rejection != PlacementRejection.None) return rejection;
                if (source.biomeBlendDistance > 0 && candidate.isFixed) return PlacementRejection.None;
                double weight = VegetationWeight(preset.biome, candidate.x, candidate.z, source, biomes, authored, mask);
                if (weight <= 0) return PlacementRejection.OutsideRegion;
                if (weight >= 1) return PlacementRejection.None;
                byte[] key = Convert.FromBase64String(candidate.sourceIdentity);
                double probability = PlacementIdentity.Unit(PlacementIdentity.RandomKey(key, source.seed, BiomeWeightSalt), 0);
                return probability < weight ? PlacementRejection.None : PlacementRejection.OutsideRegion;
            };
            return rules;
        }

        public static WorldVegetationEvaluation Build(WorldDefinition definition, WorldEcology source, TerrainBuildResult terrain, IReadOnlyList<int> tileOrder = null)
        {
            var grid = definition.Profile.CreateGrid();
            var mask = new WorldEcologyMask(definition, terrain);
            var biomes = source.biomes.OrderByDescending(b => b.priority).ThenBy(b => b.biomeId, StringComparer.Ordinal).ToArray();
            var vegetation = source.vegetation.OrderBy(v => v.prototypeId, StringComparer.Ordinal).ToArray();
            var authoredMasks = biomes.ToDictionary(b => b, b => b.mask == null || b.mask.Length == 0 ? null :
                b.mask.Select(p => new PlanPoint(p.x, 0, p.y)).ToArray());
            var layers = definition.EcologyLayers.ToArray();
            int defaultIndex = Array.IndexOf(layers, source.defaultLayer);
            if (defaultIndex < 0 || biomes.Any(b => !layers.Contains(b.layer)))
                throw new InvalidOperationException("새 지표 레이어를 P06에서 먼저 적용하세요.");
            var biomeLayers = biomes.Select(b => Array.IndexOf(layers, b.layer)).ToArray();
            var trees = vegetation.Where(v => v.path == VegetationPath.TerrainTree).ToArray();
            var details = vegetation.Where(v => v.path == VegetationPath.Detail).ToArray();
            int count = definition.Profile.TileCount, alpha = source.alphaResolution, detail = source.detailResolution;
            double tileSize = grid.WorldSize / count;
            var output = new WorldVegetationEvaluation {
                Tiles = new Tile[count, count], Layers = layers,
                TreePrototypes = trees.Select(v => new TreePrototype { prefab = v.prefab }).ToArray(),
                DetailPrototypes = details.Select(v => new DetailPrototype {
                    prototype = v.prefab, usePrototypeMesh = true, useInstancing = true, renderMode = DetailRenderMode.VertexLit,
                    minHeight = v.scale.x, maxHeight = v.scale.y, minWidth = v.scale.x, maxWidth = v.scale.y,
                    healthyColor = Color.white, dryColor = Color.white
                }).ToArray()
            };
            var order = tileOrder ?? Enumerable.Range(0, count * count).ToArray();
            if (order.Count != count * count || order.Distinct().Count() != count * count || order.Any(i => i < 0 || i >= count * count))
                throw new ArgumentException("타일 순서가 완전한 순열이어야 합니다.");
            var occupied = new PlacementSpatialHash(Math.Min(tileSize, 16));
            foreach (var prop in definition.Placements) occupied.Add(prop);
            foreach (int index in order)
            {
                int tx = index % count, tz = index / count;
                var tile = new Tile { Alpha = new float[alpha, alpha, layers.Length], Details = new int[details.Length][,] };
                output.Tiles[tz, tx] = tile;
                double stride = tileSize / (alpha - 1);
                for (int z = 0; z < alpha; z++)
                for (int x = 0; x < alpha; x++)
                {
                    double wx = Math.Min(grid.WorldSize, (tx * (alpha - 1) + x) * stride);
                    double wz = Math.Min(grid.WorldSize, (tz * (alpha - 1) + z) * stride);
                    if (source.biomeBlendDistance > 0)
                    {
                        BlendSurface(tile.Alpha, x, z, wx, wz, source.biomeBlendDistance, defaultIndex, biomes, biomeLayers, authoredMasks);
                        continue;
                    }
                    var selected = Select(wx, wz, source.humidity, biomes, authoredMasks, mask);
                    tile.Alpha[z, x, selected == null ? defaultIndex : Array.IndexOf(layers, selected.layer)] = 1;
                }
                for (int i = 0; i < details.Length; i++)
                {
                    var v = details[i];
                    var map = tile.Details[i] = new int[detail, detail];
                    if (v.density <= 0) continue;
                    for (int z = 0; z < detail; z++)
                    for (int x = 0; x < detail; x++)
                    {
                        int gx = tx * detail + x, gz = tz * detail + z;
                        double wx = (gx + .5) * tileSize / detail, wz = (gz + .5) * tileSize / detail;
                        double weight = VegetationWeight(v.biome, wx, wz, source, biomes, authoredMasks, mask);
                        if (weight <= 0) continue;
                        double radius = Math.Max(v.footprintRadius * v.scale.y, tileSize / detail * .5);
                        var candidate = Footprint(wx, wz, radius);
                        if (mask.Check(candidate) != PlacementRejection.None || occupied.Check(candidate) != PlacementRejection.None) continue;
                        double expected = v.density * weight * Math.Min(16, Math.Pow(tileSize / (detail * v.spacing), 2));
                        var key = PlacementIdentity.Encode(definition.WorldGuid, definition.GenerationEpoch, v.biome.biomeId, v.prototypeId, gx, gz, 0);
                        map[z, x] = (int)expected + (PlacementIdentity.Unit(PlacementIdentity.RandomKey(key, source.seed, 0), 0) < expected % 1 ? 1 : 0);
                    }
                }
            }

            for (int i = 0; i < trees.Length; i++)
            {
                var v = trees[i];
                int cells = (int)Math.Ceiling(grid.WorldSize / v.spacing);
                for (int z = 0; z < cells; z++)
                for (int x = 0; x < cells; x++)
                {
                    var key = PlacementIdentity.Encode(definition.WorldGuid, definition.GenerationEpoch, v.biome.biomeId, v.prototypeId, x, z, 0);
                    var random = PlacementIdentity.RandomKey(key, source.seed, 0);
                    if (PlacementIdentity.Unit(random, 0) >= v.density) continue;
                    double wx = (x + .25 + .5 * PlacementIdentity.Unit(random, 4)) * v.spacing;
                    double wz = (z + .25 + .5 * PlacementIdentity.Unit(random, 8)) * v.spacing;
                    if (wx >= grid.WorldSize || wz >= grid.WorldSize) continue;
                    double weight = VegetationWeight(v.biome, wx, wz, source, biomes, authoredMasks, mask);
                    if (PlacementIdentity.Unit(random, 0) >= v.density * weight) continue;
                    float scale = Mathf.Lerp(v.scale.x, v.scale.y, (float)PlacementIdentity.Unit(random, 12));
                    var candidate = Footprint(wx, wz, v.footprintRadius * scale);
                    string identity = Convert.ToBase64String(key);
                    candidate.id = PlacementIdentity.Id(identity);
                    candidate.spacing = v.spacing * .5;
                    if (mask.Check(candidate) != PlacementRejection.None || occupied.Check(candidate) != PlacementRejection.None) continue;
                    int tx = (int)(wx / tileSize), tz = (int)(wz / tileSize);
                    double y = mask.Height(wx, wz);
                    output.Tiles[tz, tx].Trees.Add(new TreeInstance {
                        position = new Vector3((float)(wx / tileSize - tx), (float)((y - grid.OriginY) / grid.VerticalRange), (float)(wz / tileSize - tz)),
                        prototypeIndex = i, rotation = (float)(PlacementIdentity.Unit(random, 16) * Math.PI * 2),
                        heightScale = scale, widthScale = scale, color = Color.white, lightmapColor = Color.white
                    });
                    output.Trees.Add(new EcologyTreeRecord { id = candidate.id, sourceIdentity = identity, prototypeId = v.prototypeId,
                        worldPosition = new Vector3((float)wx, (float)y, (float)wz) });
                    occupied.Add(candidate);
                }
            }
            return output;
        }

        private static void BlendSurface(float[,,] alpha, int x, int z, double worldX, double worldZ, float blendDistance,
            int defaultIndex, BiomePreset[] biomes, int[] layerIndices, Dictionary<BiomePreset, PlanPoint[]> masks)
        {
            // 지표는 편집 가능한 지역 경계를 따르고, 수계·도로 예약은 배치 밀도에서 적용한다.
            double sum = 0, maximum = 0;
            for (int i = 0; i < biomes.Length; i++)
            {
                double weight = RegionWeight(masks[biomes[i]], worldX, worldZ, blendDistance);
                alpha[z, x, layerIndices[i]] += (float)weight;
                sum += weight;
                maximum = Math.Max(maximum, weight);
            }
            double fallback = 1 - maximum;
            alpha[z, x, defaultIndex] += (float)fallback;
            // Unity의 8비트 control map으로 저장한 뒤에도 다섯 레이어의 합이 유지되게 한다.
            double cumulative = 0;
            int previous = 0;
            for (int i = 0; i < alpha.GetLength(2); i++)
            {
                cumulative += alpha[z, x, i];
                int rounded = (int)Math.Round(cumulative * 255 / (sum + fallback));
                alpha[z, x, i] = (rounded - previous) / 255f;
                previous = rounded;
            }
        }

        private static double RegionWeight(PlanPoint[] polygon, double x, double z, float blendDistance)
        {
            if (polygon == null) return 0;
            if (PlanningGeometry.Contains(polygon, x, z)) return 1;
            double distance = blendDistance;
            var point = new PlanPoint(x, 0, z);
            for (int i = 0; i < polygon.Length; i++)
                distance = Math.Min(distance, PlanningGeometry.DistanceToSegment(point, polygon[i], polygon[(i + 1) % polygon.Length], out _));
            return PlanningGeometry.SmoothFalloff(distance, blendDistance);
        }

        private static double VegetationWeight(BiomePreset target, double x, double z, WorldEcology source, BiomePreset[] biomes,
            Dictionary<BiomePreset, PlanPoint[]> authoredMasks, WorldEcologyMask mask)
        {
            if (source.biomeBlendDistance <= 0)
                return Select(x, z, source.humidity, biomes, authoredMasks, mask) == target ? 1 : 0;
            if (mask.Occupied(x, z)) return 0;

            double targetWeight = RegionWeight(authoredMasks[target], x, z, source.biomeBlendDistance);
            if (targetWeight <= 0 || !MatchesTerrain(target, mask.Height(x, z), mask.Slope(x, z), source.humidity, mask.ShoreDistance(x, z)))
                return 0;

            double sum = 0, maximum = 0;
            foreach (var biome in biomes)
            {
                double weight = biome == target ? targetWeight : RegionWeight(authoredMasks[biome], x, z, source.biomeBlendDistance);
                sum += weight;
                maximum = Math.Max(maximum, weight);
            }
            return targetWeight / (sum + 1 - maximum);
        }

        internal static BiomePreset Select(double x, double z, float humidity, BiomePreset[] biomes,
            Dictionary<BiomePreset, PlanPoint[]> authoredMasks, WorldEcologyMask mask)
        {
            if (mask.Occupied(x, z)) return null;
            double height = mask.Height(x, z), slope = mask.Slope(x, z), shore = mask.ShoreDistance(x, z);
            foreach (var biome in biomes)
                if (MatchesTerrain(biome, height, slope, humidity, shore) &&
                    authoredMasks[biome] != null && PlanningGeometry.Contains(authoredMasks[biome], x, z))
                    return biome;
            return null;
        }

        private static bool MatchesTerrain(BiomePreset biome, double height, double slope, float humidity, double shore) =>
            In(height, biome.height) && In(slope, biome.slope) && In(humidity, biome.humidity) &&
            (double.IsPositiveInfinity(shore) ? biome.shoreDistance.y >= 10000 : In(shore, biome.shoreDistance));

        private static bool In(double value, Vector2 interval) => value >= interval.x && value <= interval.y;
        private static PlacementRecord Footprint(double x, double z, double radius) =>
            new() { x = x, z = z, halfX = radius, halfZ = radius, maxSupportDelta = double.PositiveInfinity };
    }
}

