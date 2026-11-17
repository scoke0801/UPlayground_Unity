using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
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
            _ = Raw16HeightmapCodec.Decode(source.bytes, grid.SampleCount, grid.SampleCount, HeightmapRowOrigin.South);
            _worldGuid = worldGuid;
            _profile = profile;
            _baseHeight = source;
            _baseSha256 = HashBytes(source.bytes);
            _sourceRevision = ComputeSourceRevision();
            _previewSceneGuid = sceneGuid;
        }

        /// <summary>기록된 원본과 좌표 의미가 유지될 때만 Base를 제공한다.</summary>
        public HeightField ReadSource()
        {
            if (_profile == null || _baseHeight == null || _sourceRevision != ComputeSourceRevision() || HashBytes(_baseHeight.bytes) != _baseSha256)
                throw new InvalidOperationException("SourceRevisionConflict: Base 또는 월드 좌표 규격이 바뀌었습니다.");
            return Raw16HeightmapCodec.Decode(_baseHeight.bytes, _profile.SampleCount, _profile.SampleCount, HeightmapRowOrigin.South);
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

        private string ComputeSourceRevision()
        {
            var grid = _profile.CreateGrid();
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(_worldGuid);
                writer.Write(HashBytes(_baseHeight.bytes));
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
        public static string HashBytes(byte[] bytes)
        {
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }
    }

    /// <summary>큰 패치는 별도 불변 바이너리 자산으로 보관해 SO Undo 비용을 제한한다.</summary>
    [Serializable]
    public sealed class TerrainOperationRecord
    {
        public string operationGuid;
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
