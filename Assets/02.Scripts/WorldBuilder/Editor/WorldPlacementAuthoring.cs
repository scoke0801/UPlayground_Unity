using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UPlayGround.WorldBuilder.Core;
using UPlayGround.WorldBuilder.Data;

namespace UPlayGround.WorldBuilder.Editor
{
    /// <summary>공통 배치 평가와 장면 적용을 하나의 Undo 트랜잭션으로 연결한다.</summary>
    public static class WorldPlacementAuthoring
    {
        public static PlacementBuildResult LastResult { get; private set; }
        public static PlacementBuildResult Evaluate(WorldDefinition definition, TerrainBuildResult terrain, IReadOnlyList<PlacementOverride> edits = null)
        {
            definition.ValidatePlacementVersion();
            if (definition.PlacementCatalog == null) throw new InvalidOperationException("배치 catalog를 연결하세요.");
            ValidateCatalog(definition.PlacementCatalog);
            var rules = new List<PlacementRule>();
            foreach (var rule in definition.PlacementRules)
            {
                if (rule == null) throw new InvalidOperationException("배치 규칙 참조가 누락됐습니다.");
                rules.Add(rule.ToCore());
            }
            var grid = definition.Profile.CreateGrid();
            LastResult = PlacementEngine.Build(definition.WorldGuid, definition.GenerationEpoch, definition.PlacementSeed, grid, terrain,
                definition.Profile.TileCells * grid.SampleSpacing, rules, definition.PlacementCatalog.ReadPrefabs(), edits ?? definition.PlacementOverrides, definition.PlacementReservations);
            return LastResult;
        }

        /// <summary>Dirty는 충돌 연쇄를 놓치지 않도록 전역 재평가한다. 지형 원본·세션에는 쓰지 않는다.</summary>
        public static PlacementBuildResult Regenerate(WorldDefinition definition)
        {
            return Commit(definition, null, null, null);
        }
        /// <summary>기존 tombstone·Pin을 유지한 채 seed와 epoch만 명시 변경한다.</summary>
        public static PlacementBuildResult ChangeVersion(WorldDefinition definition, int seed, int epoch) => Commit(definition, null, seed, epoch);

        /// <summary>추가·교체할 Override는 대상 ID와 종류를 기준으로 갱신한다.</summary>
        public static PlacementBuildResult ApplyOverride(WorldDefinition definition, PlacementOverride edit)
        {
            var edits = new List<PlacementOverride>();
            foreach (var existing in definition.PlacementOverrides)
            {
                bool replace = existing.overrideGuid == edit.overrideGuid || (!string.IsNullOrEmpty(edit.targetId) && existing.targetId == edit.targetId && existing.kind == edit.kind);
                if (!replace) edits.Add(existing.Clone());
            }
            edits.Add(edit.Clone()); return Commit(definition, edits, null, null);
        }
        public static PlacementBuildResult RemoveOverride(WorldDefinition definition, string overrideGuid)
        {
            var edits = new List<PlacementOverride>();
            foreach (var existing in definition.PlacementOverrides) if (existing.overrideGuid != overrideGuid) edits.Add(existing.Clone());
            return Commit(definition, edits, null, null);
        }

        private static PlacementBuildResult Commit(WorldDefinition definition, List<PlacementOverride> edits, int? seed, int? epoch)
        {
            EnsureScene(definition);
            using (var session = new WorldTerrainAuthoringSession(definition, WorldBuilderAuthoringAssets.FindTerrains(definition))) session.EnsureReadyForBake();
            EnsurePreviewClean(definition);
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
            try
            {
                Undo.RegisterCompleteObjectUndo(definition, "월드 배치 변경");
                if (seed.HasValue) definition.SetPlacementVersion(seed.Value, epoch.Value);
                if (edits != null) definition.SetPlacementOverrides(edits);
                var terrain = TerrainOperationEvaluator.Build(definition.ReadSource(), definition.Profile.CreateGrid(), definition.ReadOperations());
                var result = Evaluate(definition, terrain);
                ApplyInTransaction(definition, result);
                EditorUtility.SetDirty(definition); AssetDatabase.SaveAssetIfDirty(definition);
                Undo.FlushUndoRecordObjects(); Undo.CollapseUndoOperations(group);
                return result;
            }
            catch
            {
                Undo.RevertAllDownToGroup(group);
                EditorUtility.SetDirty(definition); AssetDatabase.SaveAssetIfDirty(definition);
                throw;
            }
        }

        /// <summary>P02 지형 명령과 같은 Undo 그룹에서 후속 배치를 갱신한다.</summary>
        public static void RebuildInTransaction(WorldDefinition definition, TerrainBuildResult terrain)
        {
            if (definition.PlacementCatalog == null && definition.PlacementRules.Count == 0 && definition.Placements.Count == 0) return;
            EnsureScene(definition); EnsurePreviewClean(definition);
            ApplyInTransaction(definition, Evaluate(definition, terrain));
        }
        private static void ApplyInTransaction(WorldDefinition definition, PlacementBuildResult result)
        {
            if (result.HasErrors) throw new InvalidOperationException("PlacementConflict: 배치 오류 " + result.Diagnostics.FindAll(d => d.IsError).Count + "건. 기존 결과를 복원했습니다. 후보 진단을 확인하세요.");
            // 객체 생성 등록은 기존 객체의 기록을 끝내므로 SO의 최종 상태를 먼저 확정한다.
            definition.SetPlacements(result);
            Undo.FlushUndoRecordObjects();
            foreach (var root in FindRoots(definition)) Undo.DestroyObjectImmediate(root.gameObject);
            var container = new GameObject("WorldBuilder Placements");
            Undo.RegisterCreatedObjectUndo(container, "월드 배치 생성");
            var marker = Undo.AddComponent<WorldPlacementRoot>(container);
            Undo.RegisterCompleteObjectUndo(marker, "배치 소유 월드 기록"); marker.definition = definition;
            foreach (var p in result.Records)
            {
                var prefab = definition.PlacementCatalog.Resolve(p.prefabGuid);
                var go = PrefabUtility.InstantiatePrefab(prefab, container.transform) as GameObject;
                if (go == null) throw new InvalidOperationException("프리팹 생성에 실패했습니다.");
                Undo.RegisterCreatedObjectUndo(go, "월드 배치 생성");
                Undo.RegisterFullObjectHierarchyUndo(go, "배치 변환 기록");
                go.transform.SetPositionAndRotation(new Vector3((float)p.x, (float)p.y, (float)p.z), Quaternion.Euler(0, (float)p.yaw, 0));
                go.transform.localScale = new Vector3((float)p.scaleX, (float)p.scaleY, (float)p.scaleZ);
                var instance = Undo.AddComponent<WorldPlacementInstance>(go);
                Undo.RegisterCompleteObjectUndo(instance, "배치 정체성 기록"); instance.definition = definition; instance.record = p.Clone();
            }
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
        public static PlacementOverride CreateTargetOverride(PlacementRecord target, PlacementOverrideKind kind)
        {
            return new PlacementOverride { overrideGuid = Guid.NewGuid().ToString("N"), kind = kind, targetId = target.id,
                targetSourceIdentity = target.sourceIdentity, epoch = target.epoch, seed = target.seed, placement = target.Clone() };
        }
        public static PlacementOverride CreatePin(PlacementRecord target)
        {
            var edit = CreateTargetOverride(target, PlacementOverrideKind.Pin);
            edit.placement.isFixed = true; edit.placement.anchor = PlacementAnchor.Absolute; edit.placement.lineage = target.sourceIdentity;
            return edit;
        }
        private static void ValidateCatalog(PlacementCatalog catalog)
        {
            foreach (var entry in catalog.Entries)
            {
                if (entry == null || entry.prefab == null || !PrefabUtility.IsPartOfPrefabAsset(entry.prefab)
                    || AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(entry.prefab)) != entry.prefabGuid)
                    throw new InvalidOperationException("MissingPrefab: catalog GUID와 실제 프리팹 참조가 다릅니다.");
                if (entry.prefab.GetComponentInChildren<WorldPlacementInstance>(true) != null || entry.prefab.GetComponentInChildren<WorldPlacementRoot>(true) != null)
                    throw new InvalidOperationException("배치 소유 마커가 들어 있는 프리팹은 catalog에 사용할 수 없습니다.");
            }
        }
        private static void EnsureScene(WorldDefinition definition)
        {
            if (definition == null || EditorApplication.isPlayingOrWillChangePlaymode || AssetDatabase.AssetPathToGUID(SceneManager.GetActiveScene().path) != definition.PreviewSceneGuid)
                throw new InvalidOperationException("해당 WorldDefinition의 미리보기 씬을 Edit Mode에서 여세요.");
        }
        private static List<WorldPlacementRoot> FindRoots(WorldDefinition definition)
        {
            var result = new List<WorldPlacementRoot>();
            foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (var marker in go.GetComponentsInChildren<WorldPlacementRoot>(true)) if (marker.definition == definition) result.Add(marker);
            return result;
        }
        private static void EnsurePreviewClean(WorldDefinition definition)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var expected = new Dictionary<string, PlacementRecord>(StringComparer.Ordinal);
            foreach (var p in definition.Placements) expected.Add(p.id, p);
            foreach (var root in FindRoots(definition))
            {
                if (root.transform.position != Vector3.zero || root.transform.rotation != Quaternion.identity || root.transform.localScale != Vector3.one
                    || root.GetComponents<Component>().Length != 2)
                    throw new InvalidOperationException("UncapturedPlacementEdits: 생성 컨테이너 변경을 먼저 Undo하세요.");
                foreach (Transform child in root.transform)
                    if (child.GetComponent<WorldPlacementInstance>() == null)
                        throw new InvalidOperationException("UncapturedPlacementEdits: 생성 컨테이너에 추가한 객체를 별도 루트로 옮기거나 Undo하세요.");
            foreach (var instance in root.GetComponentsInChildren<WorldPlacementInstance>(true))
            {
                var p = instance.record;
                if (instance.definition != definition || p == null || !seen.Add(p.id)) throw new InvalidOperationException("배치 미리보기 정체성이 손상됐습니다.");
                if (!expected.TryGetValue(p.id, out var saved) || JsonUtility.ToJson(p) != JsonUtility.ToJson(saved))
                    throw new InvalidOperationException("UncapturedPlacementEdits: 배치 마커 데이터 변경을 먼저 Undo하세요.");
                var t = instance.transform;
                if (Vector3.Distance(t.position, new Vector3((float)p.x, (float)p.y, (float)p.z)) > .001f || Quaternion.Angle(t.rotation, Quaternion.Euler(0, (float)p.yaw, 0)) > .01f
                    || Vector3.Distance(t.localScale, new Vector3((float)p.scaleX, (float)p.scaleY, (float)p.scaleZ)) > .001f)
                    throw new InvalidOperationException("UncapturedPlacementEdits: 장면 변환을 되돌린 뒤 배치 창의 변환값으로 적용하세요.");
                EnsurePrefabUnmodified(instance.gameObject, instance);
            }
            }
            if (seen.Count != definition.Placements.Count) throw new InvalidOperationException("UncapturedPlacementEdits: 미리보기 객체 삭제/누락을 먼저 Undo하세요. 삭제는 배치 창에서 기록하세요.");
        }

        /// <summary>생성한 프리팹의 사용자 내부 편집이 재생성에서 유실되지 않게 검사한다.</summary>
        public static void EnsurePrefabUnmodified(GameObject instance, Component allowedMarker)
        {
            var prefab = PrefabUtility.GetCorrespondingObjectFromSource(instance);
            if (prefab == null) throw new InvalidOperationException("UncapturedPlacementEdits: 프리팹 연결이 끊겼습니다.");
            if (PrefabUtility.GetAddedGameObjects(instance).Count != 0 || PrefabUtility.GetRemovedComponents(instance).Count != 0
                || PrefabUtility.GetRemovedGameObjects(instance).Count != 0)
                throw new InvalidOperationException("UncapturedPlacementEdits: 프리팹 계층 변경을 먼저 Undo하세요.");
            foreach (var added in PrefabUtility.GetAddedComponents(instance))
                if (added.instanceComponent != allowedMarker) throw new InvalidOperationException("UncapturedPlacementEdits: 프리팹 컴포넌트 추가를 먼저 Undo하세요.");
            var modifications = PrefabUtility.GetPropertyModifications(instance);
            if (modifications == null) return;
            foreach (var modification in modifications)
            {
                if (PrefabUtility.IsDefaultOverride(modification) && instance.name == prefab.name) continue;
                if (modification.target == prefab.transform && (modification.propertyPath.StartsWith("m_LocalPosition.", StringComparison.Ordinal)
                    || modification.propertyPath.StartsWith("m_LocalRotation.", StringComparison.Ordinal) || modification.propertyPath.StartsWith("m_LocalScale.", StringComparison.Ordinal)
                    || modification.propertyPath.StartsWith("m_LocalEulerAnglesHint.", StringComparison.Ordinal))) continue;
                throw new InvalidOperationException("UncapturedPlacementEdits: 프리팹 내부 속성 변경을 먼저 Undo하세요. " + modification.propertyPath);
            }
        }
    }
}
