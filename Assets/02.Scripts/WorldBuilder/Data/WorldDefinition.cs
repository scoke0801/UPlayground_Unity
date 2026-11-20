using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UPlayGround.WorldBuilder.Core;

namespace UPlayGround.WorldBuilder.Data
{
    /// <summary>불변 Base와 순서가 명시된 저작 명령을 파생 Terrain에서 분리한다.</summary>
    [CreateAssetMenu(fileName = "WorldDefinition", menuName = "UPlayGround/WorldBuilder/World Definition")]
    public sealed class WorldDefinition : ScriptableObject
    {
        [SerializeField] private string _worldGuid;
        [SerializeField] private WorldProfile _profile;
        [SerializeField] private TextAsset _baseHeight;
        [SerializeField] private string _baseSha256;
        [SerializeField] private string _sourceRevision;
        [SerializeField] private string _previewSceneGuid;
        [SerializeField] private List<TerrainOperationRecord> _operations = new List<TerrainOperationRecord>();
        [SerializeReference] private WorldEditSessionRecord _activeSession;
        [SerializeField] private string _appliedTerrainHash;
        [SerializeField] private string _appliedStackHash;
        [SerializeField] private WorldDirtyFlags _dirtyLayers;
        [SerializeField] private int _dirtyX, _dirtyZ, _dirtyWidth, _dirtyHeight;

        // P02의 sourceRevision은 Base·좌표만 식별한다. 배치 버전은 별도로 유지한다.
        [SerializeField] private int _placementSeed, _generationEpoch;
        [SerializeField] private PlacementCatalog _placementCatalog;
        [SerializeField] private List<PlacementRuleAsset> _placementRules = new List<PlacementRuleAsset>();
        [SerializeField] private List<PlacementOverride> _placementOverrides = new List<PlacementOverride>();
        [SerializeField] private List<PlacementReservation> _placementReservations = new List<PlacementReservation>();
        [SerializeField] private List<PlacementRecord> _placements = new List<PlacementRecord>();
        [SerializeField] private string _placementHash;
        [SerializeField] private int _appliedPlacementSeed, _appliedGenerationEpoch;
        [SerializeField] private WorldPlan _planning;
        public WorldPlan Planning => _planning;
        [SerializeField] private WorldEcology _ecology;
        [SerializeField] private string _ecologyOwner;
        [SerializeField] private CaveDryVolume[] _ecologyDryVolumes;
        [SerializeField] private TerrainLayer[] _ecologyLayers;
        public WorldEcology Ecology => _ecology;
        public string EcologyOwner => _ecologyOwner;
        public IReadOnlyList<CaveDryVolume> EcologyDryVolumes => _ecologyDryVolumes ?? Array.Empty<CaveDryVolume>();
        public IReadOnlyList<TerrainLayer> EcologyLayers => _ecologyLayers ?? Array.Empty<TerrainLayer>();

        public void SetEcology(WorldEcology source, IReadOnlyList<PlacementReservation> reservations, CaveDryVolume[] dryVolumes)
        {
            PlacementIdentity.ValidateGuid(source.ecologyGuid);
            if (source.worldGuid != _worldGuid || source.ecologyGuid == _worldGuid ||
                source.ecologyGuid == _planning?.planGuid || source.ecologyGuid == _waterSystem?.systemGuid ||
                (_ecology != null && (_ecology != source || _ecologyOwner != source.ecologyGuid)))
                throw new InvalidOperationException("EcologyIdentityConflict: 월드 또는 연산 소유자가 다릅니다.");
            var copy = new List<PlacementReservation>();
            foreach (var reservation in _placementReservations)
                if (reservation.ownerPlanGuid != source.ecologyGuid) copy.Add(reservation.Clone());
            foreach (var reservation in reservations) copy.Add(reservation.Clone());
            _placementReservations = copy;
            _ecology = source;
            _ecologyOwner = source.ecologyGuid;
            _ecologyDryVolumes = dryVolumes;
            var layers = new List<TerrainLayer>(EcologyLayers);
            if (layers.Count == 0) layers.Add(source.defaultLayer);
            else if (!layers.Contains(source.defaultLayer)) layers.Add(source.defaultLayer);
            var biomes = new List<BiomePreset>(source.biomes);
            biomes.Sort((a, b) => string.CompareOrdinal(a.biomeId, b.biomeId));
            foreach (var biome in biomes) if (!layers.Contains(biome.layer)) layers.Add(biome.layer);
            _ecologyLayers = layers.ToArray();
        }

        [SerializeField] private WaterSystemDefinition _waterSystem;
        public WaterSystemDefinition WaterSystem => _waterSystem;
        /// <summary>수계 원본 교체로 소유 연산을 잃지 않도록 한 월드의 원본을 유지한다.</summary>
        public void SetWaterSystem(WaterSystemDefinition source)
        {
            source.ValidateIdentity(_worldGuid);
            if (_ecology != null && source.systemGuid == _ecologyOwner) throw new InvalidOperationException("WaterIdentityConflict: 생태와 수계 소유자가 충돌합니다.");
            if (_planning != null && source.systemGuid == _planning.planGuid) throw new InvalidOperationException("WaterIdentityConflict: 계획과 수계의 연산 소유자 GUID가 충돌합니다.");
            if (_waterSystem != null && _waterSystem != source) throw new InvalidOperationException("WaterIdentityConflict: 연결된 수계 원본을 자동 교체하지 않습니다.");
            _waterSystem = source;
            var reservations = new List<PlacementReservation>();
            foreach (var reservation in _placementReservations) if (reservation.ownerPlanGuid != source.systemGuid) reservations.Add(reservation.Clone());
            var bodies = new List<WaterSystemDefinition.BodyRecord>(source.bodies); bodies.Sort((a, b) => string.CompareOrdinal(a.waterGuid, b.waterGuid));
            foreach (var record in bodies)
            {
                var body = record.ToCore(); var polygon = body.kind == WaterBodyKind.River ? WaterGeometry.RiverBanks(body, body.width / 2) : body.boundary;
                double minX = double.PositiveInfinity, minZ = double.PositiveInfinity, maxX = double.NegativeInfinity, maxZ = double.NegativeInfinity;
                foreach (var p in polygon) { minX = Math.Min(minX, p.x); minZ = Math.Min(minZ, p.z); maxX = Math.Max(maxX, p.x); maxZ = Math.Max(maxZ, p.z); }
                reservations.Add(new PlacementReservation { guid = PlanningIdentity.Derive(_worldGuid, source.systemGuid, body.guid, "water-placement"), ownerPlanGuid = source.systemGuid,
                    minX = minX, minZ = minZ, maxX = maxX, maxZ = maxZ, boundary = (PlanPoint[])polygon.Clone() });
            }
            _placementReservations = reservations;
        }

        /// <summary>계획 소유 예약만 교체하고 사용자가 저작한 기존 예약을 보존한다.</summary>
        public void SetPlanning(WorldPlan planning, IReadOnlyList<PlacementReservation> reservations)
        {
            if (planning == null) throw new ArgumentNullException(nameof(planning));
            PlacementIdentity.ValidateGuid(planning.planGuid);
            if (_ecology != null && planning.planGuid == _ecologyOwner) throw new InvalidOperationException("PlanningIdentityConflict: 생태와 계획 소유자가 충돌합니다.");
            if (_waterSystem != null && planning.planGuid == _waterSystem.systemGuid) throw new InvalidOperationException("PlanningIdentityConflict: 수계와 계획의 연산 소유자 GUID가 충돌합니다.");
            if (_planning != null && _planning != planning) throw new InvalidOperationException("이미 연결된 계획 자산은 명시적 이관 없이 교체할 수 없습니다.");
            var copy = new List<PlacementReservation>();
            foreach (var reservation in _placementReservations) if (reservation.ownerPlanGuid != planning.planGuid) copy.Add(reservation.Clone());
            foreach (var reservation in reservations) { var item = reservation.Clone(); item.ownerPlanGuid = planning.planGuid; copy.Add(item); }
            _planning = planning; _placementReservations = copy;
            _dirtyLayers |= DirtyDependencyGraph.Expand(WorldDirtyFlags.Structures);
        }
        public int PlacementSeed => _placementSeed;
        public int GenerationEpoch => _generationEpoch;
        public PlacementCatalog PlacementCatalog => _placementCatalog;
        public IReadOnlyList<PlacementRuleAsset> PlacementRules => _placementRules;
        public IReadOnlyList<PlacementOverride> PlacementOverrides => _placementOverrides;
        public IReadOnlyList<PlacementReservation> PlacementReservations => _placementReservations;
        public IReadOnlyList<PlacementRecord> Placements => _placements;
        public string PlacementHash => _placementHash;

        /// <summary>Inspector 직접 편집도 seed/epoch 계약을 우회하지 못하게 검사한다.</summary>
        public void ValidatePlacementVersion()
        {
            if (!string.IsNullOrEmpty(_placementHash) && (_generationEpoch < _appliedGenerationEpoch
                || (_placementSeed != _appliedPlacementSeed && _generationEpoch <= _appliedGenerationEpoch)))
                throw new InvalidOperationException("SeedEpochConflict: seed 변경에는 epoch 증가가 필요합니다.");
        }

        /// <summary>seed 변경은 epoch 증가를 동반하며 기존 Override·지형 원본을 초기화하지 않는다.</summary>
        public void SetPlacementVersion(int seed, int epoch)
        {
            if (epoch < _generationEpoch || epoch < 0 || (seed != _placementSeed && epoch <= _generationEpoch)) throw new ArgumentException("seed 변경에는 epoch 증가가 필요합니다.");
            _placementSeed = seed; _generationEpoch = epoch;
            _dirtyLayers |= WorldDirtyFlags.Placement;
        }
        /// <summary>Editor 트랜잭션 안에서만 저작 의도 스냅샷을 교체한다.</summary>
        public void SetPlacementOverrides(IReadOnlyList<PlacementOverride> overrides)
        {
            var copy = new List<PlacementOverride>();
            foreach (var item in overrides) copy.Add(item.Clone());
            _placementOverrides = copy;
            _dirtyLayers |= WorldDirtyFlags.Placement;
        }
        /// <summary>검증된 파생 배치를 저장하고 배치 Dirty만 소비한다.</summary>
        public void SetPlacements(PlacementBuildResult result)
        {
            if (result.HasErrors) throw new InvalidOperationException("배치 오류가 있어 기존 결과를 보존합니다.");
            var copy = new List<PlacementRecord>();
            foreach (var item in result.Records) copy.Add(item.Clone());
            _placements = copy; _placementHash = result.ComputeHash();
            _appliedPlacementSeed = _placementSeed; _appliedGenerationEpoch = _generationEpoch;
            _dirtyLayers &= ~WorldDirtyFlags.Placement;
        }

        public string WorldGuid => _worldGuid;
        public WorldProfile Profile => _profile;
        public string SourceRevision => _sourceRevision;
        public string PreviewSceneGuid => _previewSceneGuid;
        public IReadOnlyList<TerrainOperationRecord> Operations => _operations;
        public WorldEditSessionRecord ActiveSession => _activeSession;
        public string AppliedTerrainHash => _appliedTerrainHash;
        public string AppliedStackHash => _appliedStackHash;
        public WorldDirtyFlags DirtyLayers => _dirtyLayers;
        public SampleRect? DirtyRegion => _dirtyWidth > 0 && _dirtyHeight > 0 ? new SampleRect(_dirtyX, _dirtyZ, _dirtyWidth, _dirtyHeight) : (SampleRect?)null;

        /// <summary>새 저작 원본을 한 번만 연결한다. Base 교체는 별도 이관 절차가 필요하다.</summary>
        public void Initialize(string worldGuid, WorldProfile profile, TextAsset source, string sceneGuid)
        {
            if (!string.IsNullOrEmpty(_worldGuid)) throw new InvalidOperationException("이미 초기화한 월드의 Base를 교체할 수 없습니다.");
            if (!Guid.TryParseExact(worldGuid, "N", out _) || profile == null || source == null) throw new ArgumentException("월드 식별자·프로필·원본이 필요합니다.");
            var grid = profile.CreateGrid();
            byte[] bytes = source.bytes;
            _ = Raw16HeightmapCodec.Decode(bytes, grid.SampleCount, grid.SampleCount, HeightmapRowOrigin.South);
            _worldGuid = worldGuid;
            _profile = profile;
            _baseHeight = source;
            _baseSha256 = HashBytes(bytes);
            _sourceRevision = ComputeSourceRevision(_baseSha256);
            _previewSceneGuid = sceneGuid;
        }

        /// <summary>기록된 원본과 좌표 의미가 유지될 때만 Base를 제공한다.</summary>
        public HeightField ReadSource()
        {
            if (_profile == null || _baseHeight == null)
                throw new InvalidOperationException("SourceRevisionConflict: Base 또는 월드 좌표 규격이 바뀌었습니다.");
            byte[] bytes = _baseHeight.bytes;
            string hash = HashBytes(bytes);
            if (hash != _baseSha256 || _sourceRevision != ComputeSourceRevision(hash))
                throw new InvalidOperationException("SourceRevisionConflict: Base 또는 월드 좌표 규격이 바뀌었습니다.");
            return Raw16HeightmapCodec.Decode(bytes, _profile.SampleCount, _profile.SampleCount, HeightmapRowOrigin.South);
        }

        /// <summary>저장된 패치 무결성을 검사하고 불변 수치 명령으로 변환한다.</summary>
        public List<TerrainOperation> ReadOperations()
        {
            var result = new List<TerrainOperation>(_operations.Count);
            foreach (var record in _operations) result.Add(record.ToCore(_sourceRevision));
            return result;
        }

        /// <summary>Undo 기록은 Editor가 소유하고 데이터는 명령 스냅샷만 교체한다.</summary>
        public void SetOperations(IReadOnlyList<TerrainOperationRecord> operations)
        {
            var copy = new List<TerrainOperationRecord>(operations.Count);
            foreach (var operation in operations)
            {
                _ = operation.ToCore(_sourceRevision);
                copy.Add(operation.Clone());
            }
            _operations = copy;
        }

        /// <summary>리로드 후에도 외부 편집 기준을 복원할 수 있게 세션 참조를 기록한다.</summary>
        public void SetSession(WorldEditSessionRecord session) => _activeSession = session;

        /// <summary>파생 지형의 실제 내용과 명령 버전을 함께 기록한다.</summary>
        public void SetAppliedState(string terrainHash, string stackHash, WorldDirtyFlags dirtyLayers, SampleRect? dirtyRegion = null)
        {
            _appliedTerrainHash = terrainHash;
            _appliedStackHash = stackHash;
            _dirtyLayers = dirtyLayers;
            if (dirtyRegion.HasValue)
            {
                var region = DirtyRegion.HasValue ? SampleRect.Union(DirtyRegion.Value, dirtyRegion.Value) : dirtyRegion.Value;
                _dirtyX = region.X; _dirtyZ = region.Z; _dirtyWidth = region.Width; _dirtyHeight = region.Height;
            }
        }

        private string ComputeSourceRevision(string baseHash)
        {
            var grid = _profile.CreateGrid();
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(_worldGuid);
                writer.Write(baseHash);
                writer.Write(grid.SampleCount);
                writer.Write(grid.WorldSize);
                writer.Write(grid.OriginY);
                writer.Write(grid.VerticalRange);
                writer.Write(_profile.TileCells);
                writer.Flush();
                return HashBytes(stream.ToArray());
            }
        }

        /// <summary>외부 blob과 메타데이터의 결합을 확인하는 SHA-256 식별자.</summary>
        public static string HashBytes(byte[] bytes) => TerrainSnapshotCodec.ComputeSha256(bytes);
    }

    /// <summary>큰 패치는 별도 불변 바이너리 자산으로 보관해 SO Undo 비용을 제한한다.</summary>
    [Serializable]
    public sealed class TerrainOperationRecord
    {
        public string operationGuid;
        public string ownerPlanGuid, ownerElementGuid;
        public TerrainOperationKind kind;
        public int x, z, width, height;
        public float value;
        public int stage = 40, priority, sequence;
        public bool enabled = true, automatic;
        public int radius = 1, iterations = 1;
        public TerrainProtection protection = TerrainProtection.Height;
        public TextAsset patch;
        public string patchSha256, sourceRevision;

        /// <summary>메타데이터와 blob의 크기·내용·원본 버전을 모두 검사한다.</summary>
        public TerrainOperation ToCore(string currentRevision)
        {
            float[] values = null, masks = null;
            if (!string.IsNullOrEmpty(sourceRevision) && sourceRevision != currentRevision) throw new InvalidOperationException("SourceRevisionConflict: 패치의 원본 의미가 다릅니다.");
            if (patch != null)
            {
                if (WorldDefinition.HashBytes(patch.bytes) != patchSha256) throw new InvalidDataException("패치 checksum이 다릅니다.");
                TerrainSnapshotCodec.DecodePatch(patch.bytes, out int patchWidth, out int patchHeight, out values, out masks);
                if (patchWidth != width || patchHeight != height) throw new InvalidDataException("패치 영역이 다릅니다.");
            }
            else if (!string.IsNullOrEmpty(patchSha256)) throw new InvalidDataException("패치 자산 참조가 누락됐습니다.");
            return new TerrainOperation(operationGuid, kind, new SampleRect(x, z, width, height), value, stage, priority, sequence, enabled, automatic, radius, iterations, protection, values, masks);
        }

        public TerrainOperationRecord Clone() => (TerrainOperationRecord)MemberwiseClone();
    }

    public enum WorldEditSessionKind { Sculpt, Raw }

    /// <summary>Final을 Base로 바꾸지 않고 캡처 시점과 내보내기 기준을 보존한다.</summary>
    [Serializable]
    public sealed class WorldEditSessionRecord
    {
        public string sessionGuid, sourceRevision, operationStackHash;
        public WorldEditSessionKind kind;
        public TextAsset snapshot, exportedRaw;
        public string snapshotSha256, exportedRawSha256;
    }
}
