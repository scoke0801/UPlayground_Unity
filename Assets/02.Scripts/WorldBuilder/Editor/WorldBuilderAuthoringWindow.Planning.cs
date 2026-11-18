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
        [SerializeField] private WorldPlan _planningAsset;
        private ObjectField _planningField;
        private VisualElement _planningDiagnostics;
        private string _poiGuid;
        private Vector3 _poiAnchor;
        private void CreatePlanningUI(VisualElement parent)
        {
            var section = new Foldout { text = "P04 · 계획·건물·도로·교량", value = false }; parent.Add(section);
            section.Add(new HelpBox("WorldPlan의 Region·POI·예약과 프리셋을 저작합니다. 고도는 제안 후 명시 확정하며, 수계는 계획 수위 입력까지만 검사합니다. Spline 편집 후 캡처/확정하세요.", HelpBoxMessageType.Info));
            _planningField = new ObjectField("계획 원본 SO") { objectType = typeof(WorldPlan), allowSceneObjects = false, value = _definition != null && _definition.Planning != null ? _definition.Planning : _planningAsset };
            _planningField.RegisterValueChangedCallback(e => _planningAsset = e.newValue as WorldPlan); section.Add(_planningField);
            AddButton(section, "현재 월드의 새 계획 SO 만들기/연결", () =>
            {
                if (_definition == null) throw new InvalidOperationException("저작 정의를 먼저 연결하세요.");
                _planningAsset = WorldPlanningAuthoring.CreatePlan(_definition); _planningField.SetValueWithoutNotify(_planningAsset); Selection.activeObject = _planningAsset;
            });
            AddButton(section, "계획 검증·Dirty/Full 재생성", () => PlanRun(() => WorldPlanningAuthoring.Regenerate(_definition, _planningAsset != null ? _planningAsset : _definition.Planning)));
            AddButton(section, "계획 SO Inspector 열기", () => { if (_planningAsset == null) throw new InvalidOperationException("계획 SO를 선택하세요."); Selection.activeObject = _planningAsset; EditorGUIUtility.PingObject(_planningAsset); });
            var guid = new TextField("POI GUID"); guid.RegisterValueChangedCallback(e => _poiGuid = e.newValue); section.Add(guid);
            var anchor = new Vector3Field("POI 앵커 XYZ(m)"); anchor.RegisterValueChangedCallback(e => _poiAnchor = e.newValue); section.Add(anchor);
            AddButton(section, "POI 앵커 읽기", () =>
            {
                var plan = _definition.Planning ?? throw new InvalidOperationException("계획을 먼저 적용하세요.");
                var poi = plan.pois.Find(p => p.guid == _poiGuid) ?? throw new InvalidOperationException("POI GUID가 없습니다.");
                _poiAnchor = new Vector3((float)poi.anchor.x, (float)poi.anchor.y, (float)poi.anchor.z); anchor.SetValueWithoutNotify(_poiAnchor);
            });
            AddButton(section, "POI 앵커 이동·고도 확정", () => PlanRun(() => WorldPlanningAuthoring.MovePoi(_definition, _poiGuid, WorldPlan.Point(_poiAnchor))));
            AddButton(section, "부지 고도 제안 읽기 (미적용)", () =>
            {
                Require().EnsureReadyForBake(); var proposed = WorldPlanningAuthoring.ProposePoiElevation(_definition, _poiGuid, WorldPlan.Point(_poiAnchor));
                _poiAnchor = new Vector3((float)proposed.x, (float)proposed.y, (float)proposed.z); anchor.SetValueWithoutNotify(_poiAnchor);
            });
            AddButton(section, "선택 도로 Spline 캡처/Lock", () =>
            {
                var preview = Selection.activeGameObject == null ? null : Selection.activeGameObject.GetComponentInParent<WorldRoadPreview>();
                if (preview == null) throw new InvalidOperationException("현재 월드의 도로 계층을 선택하세요.");
                PlanRun(() => WorldPlanningAuthoring.CaptureSpline(_definition, preview.roadGuid));
            });
            AddButton(section, "계획 구조물·도로 전체 삭제 (Undo 가능)", () => PlanRun(() => WorldPlanningAuthoring.ClearGeneratedPlan(_definition)));
            _planningDiagnostics = new VisualElement(); section.Add(_planningDiagnostics);
        }
        private void PlanRun(Func<WorldPlanningResult> action)
        {
            try { action(); Bind(); }
            finally
            {
                _planningDiagnostics.Clear(); var result = WorldPlanningAuthoring.LastResult;
                if (result != null)
                {
                    _planningDiagnostics.Add(new Label("구조물 " + result.Structures.Count + " / 도로 " + result.Roads.Count + " / 오류 " + result.Diagnostics.Count));
                    foreach (var issue in result.Diagnostics) _planningDiagnostics.Add(new Label(issue.elementGuid + " · " + issue.issue + " · " + issue.message));
                }
            }
        }
    }
}
