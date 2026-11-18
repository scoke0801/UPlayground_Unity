using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UPlayGround.WorldBuilder.Core;
using UPlayGround.WorldBuilder.Data;

namespace UPlayGround.WorldBuilder.Editor
{
    /// <summary>저작 명령의 트랜잭션·외부 편집 기준·네이티브 지형 복구를 소유한다.</summary>
    public sealed class WorldTerrainAuthoringSession : IDisposable
    {
        private readonly WorldDefinition _definition;
        private readonly WorldTerrainStateAdapter _terrain;
        private TerrainBuildResult _logical;
        private string _logicalStackHash;
        private string _lastAppliedHash;
        private bool _suppressUndo;
        public WorldDefinition Definition => _definition;
        public string LastError { get; private set; }
        public WorldTerrainStateAdapter Terrain => _terrain;

        public WorldTerrainAuthoringSession(WorldDefinition definition, Terrain[,] terrains)
        {
            _definition = definition != null ? definition : throw new ArgumentNullException(nameof(definition));
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode에서 저작하세요.");
            _terrain = new WorldTerrainStateAdapter(definition.Profile, terrains);
            _logical = Build();
            _logicalStackHash = CurrentStackHash();
            _lastAppliedHash = definition.AppliedTerrainHash;
            if (string.IsNullOrEmpty(_lastAppliedHash))
            {
                var actual = _terrain.Read();
                float tolerance = definition.Profile.VerticalRange / 32766f + .000001f;
                for (int z = 0; z < actual.SampleCount; z++)
                for (int x = 0; x < actual.SampleCount; x++)
                    if (Math.Abs(actual.GetHeight(z, x) - _logical.GetHeight(z, x)) > tolerance) throw new InvalidDataException("UncapturedTerrainEdits: 초기 지형이 Base와 다릅니다.");
                for (int z = 0; z < actual.SampleCount - 1; z++)
                for (int x = 0; x < actual.SampleCount - 1; x++)
                    if (actual.GetSurface(z, x) != _logical.GetSurface(z, x)) throw new InvalidDataException("UncapturedTerrainEdits: 초기 Hole이 다릅니다.");
                RecordApplied(actual, WorldDirtyFlags.None);
            }
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        /// <summary>미캡처 변경이나 세션 충돌은 Bake 준비 판정으로 통과시키지 않는다.</summary>
        public void EnsureReadyForBake()
        {
            EnsureClean();
            EnsureStackApplied();
            if (_definition.ActiveSession != null) throw new InvalidOperationException("EditSessionActive: Capture 또는 Revert가 필요합니다.");
            _ = Build();
        }

        /// <summary>기존 Final을 기준으로 Unity 브러시 편집을 시작한다.</summary>
        public void BeginSculpt() => BeginSession(WorldEditSessionKind.Sculpt, null);

        /// <summary>R16 파일과 세션 기준을 함께 내보내며 기존 파일은 덮어쓰지 않는다.</summary>
        public void ExportRaw(string path)
        {
            if (!string.Equals(Path.GetExtension(path), ".raw", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("R16 RAW 경로가 필요합니다.");
            if (File.Exists(path) || File.Exists(path + ".json")) throw new IOException("기존 내보내기를 덮어쓸 수 없습니다.");
            var physical = _terrain.Read();
            var normalized = new float[physical.SampleCount, physical.SampleCount];
            for (int z = 0; z < physical.SampleCount; z++)
            for (int x = 0; x < physical.SampleCount; x++) normalized[z, x] = (physical.GetHeight(z, x) - _definition.Profile.OriginY) / _definition.Profile.VerticalRange;
            byte[] raw = Raw16HeightmapCodec.Encode(Raw16HeightmapCodec.FromNormalized(normalized), HeightmapRowOrigin.South);
            BeginSession(WorldEditSessionKind.Raw, raw);
            try
            {
                string directory = Path.GetDirectoryName(Path.GetFullPath(path));
                Directory.CreateDirectory(directory);
                using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write)) stream.Write(raw, 0, raw.Length);
                var record = _definition.ActiveSession;
                var metadata = new ExportMetadata { worldGuid = _definition.WorldGuid, sourceRevision = record.sourceRevision, operationStackHash = record.operationStackHash, sessionGuid = record.sessionGuid,
                    exportedHeightHash = record.exportedRawSha256, sampleCount = _definition.Profile.SampleCount, worldSize = _definition.Profile.WorldSize, originY = _definition.Profile.OriginY, verticalRange = _definition.Profile.VerticalRange };
                byte[] metadataBytes = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(metadata, true));
                using (var stream = new FileStream(path + ".json", FileMode.CreateNew, FileAccess.Write)) stream.Write(metadataBytes, 0, metadataBytes.Length);
            }
            catch
            {
                // 실패한 외부 파일은 덮어쓰거나 지우지 않는다. 활성 기준을 남겨 명시적으로 재시도/취소한다.
                throw;
            }
        }

        /// <summary>외부 RAW는 저장된 export 기준과만 비교하고 Base로 교체하지 않는다.</summary>
        public void ImportRaw(string path, bool absolute)
        {
            var session = RequireSession(WorldEditSessionKind.Raw);
            EnsureClean();
            if (!string.Equals(Path.GetExtension(path), ".raw", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("RAW 입력만 허용합니다.");
            int n = _definition.Profile.SampleCount;
            if (!File.Exists(path + ".json")) throw new InvalidDataException("ExportSessionConflict: RAW 옆의 내보내기 메타데이터가 필요합니다.");
            var metadata = JsonUtility.FromJson<ExportMetadata>(File.ReadAllText(path + ".json"));
            if (metadata == null || metadata.schemaVersion != 1 || metadata.worldGuid != _definition.WorldGuid || metadata.sourceRevision != session.sourceRevision ||
                metadata.operationStackHash != session.operationStackHash || metadata.sessionGuid != session.sessionGuid || metadata.exportedHeightHash != session.exportedRawSha256 ||
                metadata.sampleCount != n || metadata.encoding != "R16_UNORM" || metadata.byteOrder != "little-endian" || metadata.row0 != "south" ||
                metadata.worldSize != _definition.Profile.WorldSize || metadata.originY != _definition.Profile.OriginY || metadata.verticalRange != _definition.Profile.VerticalRange)
                throw new InvalidDataException("ExportSessionConflict: 다른 월드·버전·행 방향의 RAW 내보내기입니다.");
            var edited = Raw16HeightmapCodec.Decode(File.ReadAllBytes(path), n, n, HeightmapRowOrigin.South);
            if (session.exportedRaw == null || WorldDefinition.HashBytes(session.exportedRaw.bytes) != session.exportedRawSha256) throw new InvalidDataException("ExportSessionConflict: 내보내기 기준이 바뀌었습니다.");
            var exported = Raw16HeightmapCodec.Decode(session.exportedRaw.bytes, n, n, HeightmapRowOrigin.South);
            var heights = new float[n * n];
            var masks = new float[heights.Length];
            var grid = _definition.Profile.CreateGrid();
            for (int z = 0; z < n; z++)
            for (int x = 0; x < n; x++)
            {
                int index = z * n + x;
                masks[index] = edited[z, x] == exported[z, x] ? 0 : 1;
                heights[index] = absolute ? (float)grid.SampleToHeight(edited[z, x]) : (float)((edited[z, x] - exported[z, x]) / 65535.0 * grid.VerticalRange);
            }
            CommitCaptured(heights, masks, null, absolute ? TerrainOperationKind.SetAbsolute : TerrainOperationKind.AddDelta);
        }

        /// <summary>현재 편집은 세션의 G40 스냅샷과 비교한다. 미시작 편집은 적용 캐시로 명시 캡처한다.</summary>
        public void CaptureSculpt()
        {
            if (_definition.ActiveSession == null)
            {
                _ = _definition.ReadSource();
                EnsureStackApplied();
                string cachePath = CachePath(_definition.AppliedTerrainHash);
                if (!File.Exists(cachePath)) throw new InvalidOperationException("CaptureBaselineMissing: 적용 기준 캐시가 없습니다. Revert로 복구하세요.");
                var baseline = TerrainSnapshotCodec.Decode(File.ReadAllBytes(cachePath));
                if (baseline.ComputeHash() != _definition.AppliedTerrainHash) throw new InvalidDataException("적용 기준 캐시가 손상됐습니다.");
                SaveSession(WorldEditSessionKind.Sculpt, baseline, null);
            }
            var session = RequireSession(WorldEditSessionKind.Sculpt);
            var captured = ReadSessionSnapshot(session);
            var edited = _terrain.Read(captured);
            int n = captured.SampleCount;
            var values = new float[n * n];
            var masks = new float[values.Length];
            var cuts = new float[(n - 1) * (n - 1)];
            for (int z = 0; z < n; z++)
            for (int x = 0; x < n; x++)
            {
                int index = z * n + x;
                values[index] = edited.GetHeight(z, x) - captured.GetHeight(z, x);
                masks[index] = values[index] == 0 ? 0 : 1;
            }
            for (int z = 0; z < n - 1; z++)
            for (int x = 0; x < n - 1; x++)
            {
                if (!captured.GetSurface(z, x) && edited.GetSurface(z, x)) throw new InvalidOperationException("HoleOwnershipConflict: 기존 HoleCut 명령을 삭제해서 복원하세요.");
                if (captured.GetSurface(z, x) && !edited.GetSurface(z, x)) cuts[z * (n - 1) + x] = 1;
            }
            CommitCaptured(values, masks, cuts, TerrainOperationKind.AddDelta);
        }

        /// <summary>균일 명령 추가도 전체 검증 후 한 Undo 그룹으로 확정한다.</summary>
        public void AddOperation(TerrainOperationRecord record)
        {
            EnsureClean();
            EnsureStackApplied();
            var records = CopyRecords();
            records.Add(record.Clone());
            Commit(records, record.ToCore(_definition.SourceRevision).Bounds, false, "지형 명령 추가");
        }

        /// <summary>Falloff 마스크도 SO 배열 대신 불변 영역 패치로 저장한다.</summary>
        public void AddMaskedOperation(TerrainOperationRecord record, float[] masks)
        {
            var created = new List<string>();
            try
            {
                var values = new float[checked(record.width * record.height)];
                for (int i = 0; i < values.Length; i++) values[i] = record.value;
                byte[] bytes = TerrainSnapshotCodec.EncodePatch(record.width, record.height, values, masks);
                var copy = record.Clone();
                copy.patch = SaveBlob("Patches", bytes, created);
                copy.patchSha256 = WorldDefinition.HashBytes(bytes);
                copy.sourceRevision = _definition.SourceRevision;
                AddOperation(copy);
            }
            catch { foreach (string path in created) AssetDatabase.DeleteAsset(path); throw; }
        }

        /// <summary>삭제·활성·이동 변경은 old/new 영역의 합집합을 다시 평가한다.</summary>
        public void ReplaceOperation(int index, TerrainOperationRecord replacement)
        {
            EnsureClean();
            EnsureStackApplied();
            var records = CopyRecords();
            var old = records[index].ToCore(_definition.SourceRevision).Bounds;
            SampleRect dirty = old;
            if (replacement == null) records.RemoveAt(index);
            else { records[index] = replacement.Clone(); dirty = SampleRect.Union(old, replacement.ToCore(_definition.SourceRevision).Bounds); }
            Commit(records, dirty, false, "지형 명령 변경");
        }

        /// <summary>명령 변경을 재평가하되 미캡처 Terrain은 자동 덮어쓰지 않는다.</summary>
        public void Regenerate()
        {
            EnsureClean();
            if (_definition.ActiveSession != null) throw new InvalidOperationException("EditSessionActive: 먼저 Capture/Revert하세요.");
            Commit(CopyRecords(), SampleRect.Full(_definition.Profile.SampleCount), false, "지형 재생성");
        }

        /// <summary>사용자가 선택한 경우에만 미캡처 변경과 활성 세션을 버린다.</summary>
        public void RevertUncaptured() => Commit(CopyRecords(), SampleRect.Full(_definition.Profile.SampleCount), true, "미캡처 지형 되돌리기");

        private void BeginSession(WorldEditSessionKind kind, byte[] raw)
        {
            EnsureClean();
            EnsureStackApplied();
            if (_definition.ActiveSession != null) throw new InvalidOperationException("편집 세션이 이미 열려 있습니다.");
            SaveSession(kind, _terrain.Read(), raw);
        }

        private void SaveSession(WorldEditSessionKind kind, TerrainBuildResult baseline, byte[] raw)
        {
            var created = new List<string>();
            try
            {
                byte[] snapshot = TerrainSnapshotCodec.Encode(baseline);
                var record = new WorldEditSessionRecord { sessionGuid = Guid.NewGuid().ToString("N"), kind = kind, sourceRevision = _definition.SourceRevision,
                    operationStackHash = CurrentStackHash(), snapshot = SaveBlob("Sessions", snapshot, created), snapshotSha256 = WorldDefinition.HashBytes(snapshot) };
                if (raw != null) { record.exportedRaw = SaveBlob("Sessions", raw, created); record.exportedRawSha256 = WorldDefinition.HashBytes(raw); }
                Undo.IncrementCurrentGroup();
                Undo.RecordObject(_definition, "지형 편집 세션 시작");
                _definition.SetSession(record);
                Undo.FlushUndoRecordObjects();
                SaveDefinition();
            }
            catch { foreach (string path in created) AssetDatabase.DeleteAsset(path); throw; }
        }

        private WorldEditSessionRecord RequireSession(WorldEditSessionKind kind)
        {
            _ = _definition.ReadSource();
            var session = _definition.ActiveSession;
            if (session == null || session.kind != kind) throw new InvalidOperationException("해당 편집 세션이 없습니다.");
            if (session.sourceRevision != _definition.SourceRevision || session.operationStackHash != CurrentStackHash()) throw new InvalidOperationException("EditSessionConflict: 편집 중 원본 또는 레이어가 바뀌었습니다.");
            _ = ReadSessionSnapshot(session);
            return session;
        }

        private static TerrainBuildResult ReadSessionSnapshot(WorldEditSessionRecord session)
        {
            if (session.snapshot == null || WorldDefinition.HashBytes(session.snapshot.bytes) != session.snapshotSha256) throw new InvalidDataException("세션 스냅샷이 손상됐습니다.");
            return TerrainSnapshotCodec.Decode(session.snapshot.bytes);
        }

        private void CommitCaptured(float[] values, float[] masks, float[] cuts, TerrainOperationKind kind)
        {
            var created = new List<string>();
            var records = CopyRecords();
            int n = _definition.Profile.SampleCount;
            SampleRect? dirty = null;
            try
            {
                AddPatch(records, kind, n, values, masks, created, ref dirty);
                if (cuts != null) AddPatch(records, TerrainOperationKind.HoleCut, n - 1, new float[cuts.Length], cuts, created, ref dirty);
                Commit(records, dirty ?? SampleRect.Full(n), true, "지형 편집 캡처");
            }
            catch { foreach (string path in created) AssetDatabase.DeleteAsset(path); throw; }
        }

        private void AddPatch(List<TerrainOperationRecord> records, TerrainOperationKind kind, int n, float[] values, float[] masks, List<string> created, ref SampleRect? dirty)
        {
            int x0 = n, z0 = n, x1 = -1, z1 = -1;
            for (int z = 0; z < n; z++)
            for (int x = 0; x < n; x++) if (masks[z * n + x] != 0) { x0 = Math.Min(x0, x); z0 = Math.Min(z0, z); x1 = Math.Max(x1, x); z1 = Math.Max(z1, z); }
            if (x1 < x0) return;
            var bounds = new SampleRect(x0, z0, x1 - x0 + 1, z1 - z0 + 1);
            var patchValues = new float[bounds.Width * bounds.Height];
            var patchMasks = new float[patchValues.Length];
            for (int z = z0; z <= z1; z++)
            for (int x = x0; x <= x1; x++) { int index = (z - z0) * bounds.Width + x - x0; patchValues[index] = values[z * n + x]; patchMasks[index] = masks[z * n + x]; }
            byte[] bytes = TerrainSnapshotCodec.EncodePatch(bounds.Width, bounds.Height, patchValues, patchMasks);
            int sequence = 0;
            int stage = kind == TerrainOperationKind.HoleCut ? 50 : 40, priority = 0;
            foreach (var record in records)
            {
                sequence = Math.Max(sequence, checked(record.sequence + 1));
                if (record.kind != TerrainOperationKind.ProtectRegion && (record.kind == TerrainOperationKind.HoleCut) == (kind == TerrainOperationKind.HoleCut)) stage = Math.Max(stage, record.stage);
            }
            foreach (var record in records) if (record.stage == stage) priority = Math.Max(priority, record.priority);
            records.Add(new TerrainOperationRecord { operationGuid = Guid.NewGuid().ToString("N"), kind = kind, x = x0, z = z0, width = bounds.Width, height = bounds.Height,
                sequence = sequence, stage = stage, priority = priority, patch = SaveBlob("Patches", bytes, created), patchSha256 = WorldDefinition.HashBytes(bytes), sourceRevision = _definition.SourceRevision });
            dirty = dirty.HasValue ? SampleRect.Union(dirty.Value, bounds) : bounds;
        }

        /// <summary>지정 계획의 파생 연산만 바꾸고 Sculpt·다른 저작 레이어를 같은 트랜잭션에서 보존한다.</summary>
        public void ReplaceOwnedOperations(string ownerGuid, IReadOnlyList<TerrainOperationRecord> replacements, Action applyPlanningData)
        {
            PlacementIdentity.ValidateGuid(ownerGuid); EnsureReadyForBake();
            var records = new List<TerrainOperationRecord>(); SampleRect? dirty = null;
            foreach (var record in _definition.Operations)
            {
                if (record.ownerPlanGuid != ownerGuid) records.Add(record.Clone());
                else dirty = dirty.HasValue ? SampleRect.Union(dirty.Value, record.ToCore(_definition.SourceRevision).Bounds) : record.ToCore(_definition.SourceRevision).Bounds;
            }
            foreach (var record in replacements)
            {
                if (record.ownerPlanGuid != ownerGuid) throw new ArgumentException("계획 연산 소유자가 다릅니다.");
                var bounds = record.ToCore(_definition.SourceRevision).Bounds;
                records.Add(record.Clone()); dirty = dirty.HasValue ? SampleRect.Union(dirty.Value, bounds) : bounds;
            }
            Commit(records, dirty ?? SampleRect.Full(_definition.Profile.SampleCount), false, "월드 계획 적용", applyPlanningData);
        }

        private void Commit(List<TerrainOperationRecord> records, SampleRect dirty, bool clearSession, string label, Action applyPlanningData = null)
        {
            var source = _definition.ReadSource();
            var operations = new List<TerrainOperation>();
            foreach (var record in records) operations.Add(record.ToCore(_definition.SourceRevision));
            int n = _definition.Profile.SampleCount;
            var result = TerrainOperationEvaluator.Rebuild(source, _definition.Profile.CreateGrid(), operations, _logical, dirty);
            dirty = dirty.Expand(DirtyDependencyGraph.GetHeightHalo(operations), n, n);
            var before = _terrain.CaptureTiles();
            var oldLogical = _logical;
            string oldStackHash = _logicalStackHash;
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            _suppressUndo = true;
            try
            {
                Undo.RegisterCompleteObjectUndo(_definition, label);
                applyPlanningData?.Invoke();
                _definition.SetOperations(records);
                if (clearSession) _definition.SetSession(null);
                _terrain.Apply(result, dirty);
                WorldDirtyFlags changed = WorldDirtyFlags.Height | WorldDirtyFlags.Holes;
                RecordApplied(_terrain.Read(), _definition.DirtyLayers | DirtyDependencyGraph.Expand(changed), dirty);
                WorldPlacementAuthoring.RebuildInTransaction(_definition, result);
                SaveDefinition();
                _logical = result;
                _logicalStackHash = CurrentStackHash();
                SaveTerrains();
                Undo.FlushUndoRecordObjects();
                Undo.CollapseUndoOperations(group);
                LastError = null;
            }
            catch
            {
                Undo.RevertAllDownToGroup(group);
                WorldTerrainStateAdapter.RestoreTiles(before);
                _logical = oldLogical;
                _logicalStackHash = oldStackHash;
                _lastAppliedHash = _definition.AppliedTerrainHash;
                SaveTerrains();
                SaveDefinition();
                throw;
            }
            finally { _suppressUndo = false; }
        }

        private void OnUndoRedo()
        {
            if (_suppressUndo || _definition == null) return;
            try
            {
                string stackHash = CurrentStackHash();
                // 전역 Undo 알림에는 다른 객체와 Unity 기본 브러시도 포함된다.
                if (stackHash == _logicalStackHash) return;
                if (_terrain.Read().ComputeHash() == _definition.AppliedTerrainHash)
                {
                    _logical = Build(); _logicalStackHash = stackHash; _lastAppliedHash = _definition.AppliedTerrainHash; return;
                }
                if (_terrain.Read().ComputeHash() != _lastAppliedHash) throw new InvalidOperationException("UncapturedTerrainEdits: Undo가 외부 편집을 덮어쓸 수 없습니다.");
                _logical = Build();
                string targetCache = CachePath(_definition.AppliedTerrainHash);
                var target = File.Exists(targetCache) ? TerrainSnapshotCodec.Decode(File.ReadAllBytes(targetCache)) : _logical;
                if (File.Exists(targetCache) && target.ComputeHash() != _definition.AppliedTerrainHash) throw new InvalidDataException("Undo 적용 스냅샷이 손상됐습니다.");
                _terrain.Apply(target);
                RecordApplied(_terrain.Read(), _definition.DirtyLayers | DirtyDependencyGraph.Expand(WorldDirtyFlags.Height | WorldDirtyFlags.Holes), SampleRect.Full(_definition.Profile.SampleCount));
                SaveTerrains();
                _logicalStackHash = stackHash;
                LastError = null;
            }
            catch (Exception exception) { LastError = exception.Message; Debug.LogException(exception); }
        }

        private void EnsureClean()
        {
            _ = _definition.ReadSource();
            if (_terrain.Read().ComputeHash() != _definition.AppliedTerrainHash) throw new InvalidOperationException("UncapturedTerrainEdits: Capture/Revert 전에는 Regenerate/Bake할 수 없습니다.");
        }

        private void EnsureStackApplied()
        {
            if (_definition.AppliedStackHash != CurrentStackHash()) throw new InvalidOperationException("OperationStackChanged: 먼저 레이어를 재생성하거나 세션 충돌을 해결하세요.");
            // 다른 정상 저작 세션이 적용한 계획 패치를 현재 창의 수치 캐시에도 반영한다.
            if (_logicalStackHash != CurrentStackHash()) { _logical = Build(); _logicalStackHash = CurrentStackHash(); }
        }

        private void RecordApplied(TerrainBuildResult physical, WorldDirtyFlags dirty, SampleRect? region = null)
        {
            _lastAppliedHash = physical.ComputeHash();
            string cache = CachePath(_lastAppliedHash);
            Directory.CreateDirectory(Path.GetDirectoryName(cache));
            if (!File.Exists(cache)) File.WriteAllBytes(cache, TerrainSnapshotCodec.Encode(physical));
            _definition.SetAppliedState(_lastAppliedHash, CurrentStackHash(), dirty, region);
            if (_definition.Planning != null) WorldPlanningUndoRestore.Track(_definition);
            SaveDefinition();
        }

        private string CachePath(string hash) => "Library/WorldBuilder/" + _definition.WorldGuid + "/Applied/" + hash + ".bin";
        private TerrainBuildResult Build() => TerrainOperationEvaluator.Build(_definition.ReadSource(), _definition.Profile.CreateGrid(), _definition.ReadOperations());
        private string CurrentStackHash() => TerrainOperationEvaluator.ComputeStackHash(_definition.ReadOperations());
        private List<TerrainOperationRecord> CopyRecords() { var result = new List<TerrainOperationRecord>(); foreach (var record in _definition.Operations) result.Add(record.Clone()); return result; }
        private void SaveDefinition() { EditorUtility.SetDirty(_definition); AssetDatabase.SaveAssetIfDirty(_definition); }
        private void SaveTerrains() { foreach (var terrain in _terrain.Terrains) AssetDatabase.SaveAssetIfDirty(terrain.terrainData); }

        private TextAsset SaveBlob(string category, byte[] bytes, List<string> created)
        {
            string folder = "Assets/WorldBuilder/Worlds/" + _definition.WorldGuid + "/Authoring/" + category;
            WorldBuilderAuthoringAssets.EnsureFolder(folder);
            string path = folder + "/" + Guid.NewGuid().ToString("N") + ".bytes";
            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write)) stream.Write(bytes, 0, bytes.Length);
            created.Add(path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            if (asset == null) throw new IOException("저작 바이너리 임포트에 실패했습니다.");
            return asset;
        }

        public void Dispose() => Undo.undoRedoPerformed -= OnUndoRedo;

        [Serializable] private sealed class ExportMetadata
        {
            public int schemaVersion = 1, sampleCount;
            public string worldGuid, sourceRevision, operationStackHash, sessionGuid, exportedHeightHash;
            public string encoding = "R16_UNORM", byteOrder = "little-endian", row0 = "south";
            public float worldSize, originY, verticalRange;
        }
    }
}
