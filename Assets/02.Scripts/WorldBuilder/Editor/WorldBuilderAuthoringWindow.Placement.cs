using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UPlayGround.WorldBuilder.Core;
using UPlayGround.WorldBuilder.Data;

namespace UPlayGround.WorldBuilder.Editor
{
    public sealed partial class WorldBuilderAuthoringWindow
    {
        private string _placementTarget = "";
        private Vector3 _placementPosition, _placementScale = Vector3.one;
        private float _placementYaw;
        private bool _placementTerrainAnchor;
        private float _placementHeightOffset, _placementSupportDelta = 2;
        private string _placementPrefabGuid = "";
        private Rect _excludeBounds = new Rect(0, 0, 16, 16);
        private int _newSeed, _newEpoch;
        private VisualElement _placementDiagnostics;

        private void CreatePlacementUI(VisualElement parent)
        {
            var section = new Foldout { text = "P03 · 공통 배치", value = false }; parent.Add(section);
            section.Add(new HelpBox("WorldDefinition Inspector에서 Catalog·Rules·Reservations를 연결하세요. P03은 Terrain 지지·수직/yaw 배치입니다. 수계·바이옴·Physics 중요 물체 검사는 후속 단계입니다.", HelpBoxMessageType.Info));
            AddButton(section, "배치 Full / Dirty 재생성", () => PlacementRun(() => WorldPlacementAuthoring.Regenerate(_definition)));
            var target = new TextField("대상 배치 ID"); target.RegisterValueChangedCallback(e => _placementTarget = e.newValue); section.Add(target);
            AddButton(section, "선택 객체 ID 읽기", () =>
            {
                var instance = Selection.activeGameObject == null ? null : Selection.activeGameObject.GetComponent<WorldPlacementInstance>();
                if (instance == null || instance.definition != _definition) throw new InvalidOperationException("현재 월드의 배치 객체를 선택하세요.");
                _placementTarget = instance.record.id; target.SetValueWithoutNotify(_placementTarget);
            });
            var position = new Vector3Field("월드 위치 XYZ(m)") { value = _placementPosition }; position.RegisterValueChangedCallback(e => _placementPosition = e.newValue); section.Add(position);
            var yaw = new FloatField("Y 회전(도)") { value = _placementYaw }; yaw.RegisterValueChangedCallback(e => _placementYaw = e.newValue); section.Add(yaw);
            var scale = new Vector3Field("배율 XYZ") { value = _placementScale }; scale.RegisterValueChangedCallback(e => _placementScale = e.newValue); section.Add(scale);
            var anchor = new Toggle("Terrain 재접지 (끄면 절대 XYZ)") { value = _placementTerrainAnchor }; anchor.RegisterValueChangedCallback(e => _placementTerrainAnchor = e.newValue); section.Add(anchor);
            var offset = new FloatField("Terrain 높이 offset(m)") { value = _placementHeightOffset }; offset.RegisterValueChangedCallback(e => _placementHeightOffset = e.newValue); section.Add(offset);
            var support = new FloatField("허용 지지면 높이 차(m)") { value = _placementSupportDelta }; support.RegisterValueChangedCallback(e => _placementSupportDelta = e.newValue); section.Add(support);
            AddButton(section, "대상 변환 읽기", () =>
            {
                var p = FindPlacement(); _placementPosition = new Vector3((float)p.x, (float)p.y, (float)p.z); _placementYaw = (float)p.yaw; _placementScale = new Vector3((float)p.scaleX, (float)p.scaleY, (float)p.scaleZ);
                position.SetValueWithoutNotify(_placementPosition); yaw.SetValueWithoutNotify(_placementYaw); scale.SetValueWithoutNotify(_placementScale);
                _placementTerrainAnchor = p.anchor == PlacementAnchor.Terrain; _placementHeightOffset = (float)p.heightOffset; _placementSupportDelta = (float)p.maxSupportDelta;
                anchor.SetValueWithoutNotify(_placementTerrainAnchor); offset.SetValueWithoutNotify(_placementHeightOffset); support.SetValueWithoutNotify(_placementSupportDelta);
            });
            AddButton(section, "Pin (현재 월드 XYZ 고정)", () => PlacementRun(() => WorldPlacementAuthoring.ApplyOverride(_definition, WorldPlacementAuthoring.CreatePin(FindPlacement()))));
            AddButton(section, "Delete (현재 epoch tombstone)", () => ApplyTarget(PlacementOverrideKind.Delete));
            AddButton(section, "Move / Rotate / Scale 적용", () => ApplyTarget(PlacementOverrideKind.Transform));
            var prefab = new TextField("Catalog 프리팹 GUID"); prefab.RegisterValueChangedCallback(e => _placementPrefabGuid = e.newValue); section.Add(prefab);
            AddButton(section, "ReplacePrefab 적용", () => ApplyTarget(PlacementOverrideKind.ReplacePrefab));
            AddButton(section, "Manual Placement 추가 (절대 위치)", () =>
            {
                string id = Guid.NewGuid().ToString("N");
                var p = new PlacementRecord { id = id, sourceIdentity = "manual:" + _definition.WorldGuid + ":" + id, prefabGuid = _placementPrefabGuid,
                    epoch = _definition.GenerationEpoch, seed = _definition.PlacementSeed, isFixed = true, anchor = PlacementAnchor.Absolute, maxSupportDelta = _placementSupportDelta };
                WriteTransform(p);
                PlacementRun(() => WorldPlacementAuthoring.ApplyOverride(_definition, new PlacementOverride { overrideGuid = Guid.NewGuid().ToString("N"), kind = PlacementOverrideKind.Manual, placement = p }));
            });
            var exclude = new RectField("제외 영역 X/Z/너비/높이(m)") { value = _excludeBounds }; exclude.RegisterValueChangedCallback(e => _excludeBounds = e.newValue); section.Add(exclude);
            AddButton(section, "ExcludeRegion 추가 (모든 규칙)", () => PlacementRun(() => WorldPlacementAuthoring.ApplyOverride(_definition, new PlacementOverride {
                overrideGuid = Guid.NewGuid().ToString("N"), kind = PlacementOverrideKind.ExcludeRegion, minX = _excludeBounds.xMin, minZ = _excludeBounds.yMin, maxX = _excludeBounds.xMax, maxZ = _excludeBounds.yMax })));
            var seed = new IntegerField("새 seed"); seed.RegisterValueChangedCallback(e => _newSeed = e.newValue); section.Add(seed);
            var epoch = new IntegerField("새 epoch"); epoch.RegisterValueChangedCallback(e => _newEpoch = e.newValue); section.Add(epoch);
            AddButton(section, "seed / epoch 명시 변경", () => PlacementRun(() => WorldPlacementAuthoring.ChangeVersion(_definition, _newSeed, _newEpoch)));
            _placementDiagnostics = new VisualElement(); section.Add(_placementDiagnostics);
        }
        private PlacementRecord FindPlacement()
        {
            if (_definition != null) foreach (var p in _definition.Placements) if (p.id == _placementTarget) return p;
            throw new InvalidOperationException("대상 배치 ID가 없습니다.");
        }
        private void WriteTransform(PlacementRecord p)
        {
            p.x = _placementPosition.x; p.y = _placementPosition.y; p.z = _placementPosition.z; p.yaw = _placementYaw;
            p.scaleX = _placementScale.x; p.scaleY = _placementScale.y; p.scaleZ = _placementScale.z;
            p.anchor = _placementTerrainAnchor ? PlacementAnchor.Terrain : PlacementAnchor.Absolute; p.heightOffset = _placementHeightOffset; p.maxSupportDelta = _placementSupportDelta;
        }
        private void ApplyTarget(PlacementOverrideKind kind)
        {
            var p = FindPlacement(); var edit = WorldPlacementAuthoring.CreateTargetOverride(p, kind);
            if (kind == PlacementOverrideKind.Transform) WriteTransform(edit.placement);
            if (kind == PlacementOverrideKind.ReplacePrefab) edit.prefabGuid = _placementPrefabGuid;
            PlacementRun(() => WorldPlacementAuthoring.ApplyOverride(_definition, edit));
        }
        private void PlacementRun(Func<PlacementBuildResult> action)
        {
            try { action(); }
            finally
            {
                _placementDiagnostics.Clear();
                var result = WorldPlacementAuthoring.LastResult;
                if (result != null)
                {
                    _placementDiagnostics.Add(new Label("출력 " + result.Records.Count + " / 진단 " + result.Diagnostics.Count));
                    foreach (PlacementRejection reason in Enum.GetValues(typeof(PlacementRejection)))
                    { int count = result.Count(reason); if (count > 0) _placementDiagnostics.Add(new Label(reason + ": " + count)); }
                    var details = new Foldout { text = "후보・Orphan 상세", value = false };
                    int shown = 0;
                    foreach (var d in result.Diagnostics)
                    {
                        if (shown++ >= 256) break;
                        details.Add(new Label((d.IsError ? "오류 " : "") + d.Reason + " / " + d.RuleGuid + " / " + d.CandidateId + " / " + d.OverrideGuid));
                    }
                    if (result.Diagnostics.Count > 256) details.Add(new Label("후보 상세는 앞 256건 표시. 전체 개수는 위 집계를 확인하세요."));
                    _placementDiagnostics.Add(details);
                }
                if (_definition != null) foreach (var edit in _definition.PlacementOverrides)
                {
                    string id = edit.overrideGuid;
                    _placementDiagnostics.Add(new Button(() => Run(() => PlacementRun(() => WorldPlacementAuthoring.RemoveOverride(_definition, id)))) { text = edit.kind + " " + id + " 삭제" });
                }
            }
        }
    }
}

