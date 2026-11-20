using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UPlayGround.WorldBuilder.Data;

namespace UPlayGround.WorldBuilder.Editor
{
    public sealed partial class WorldBuilderAuthoringWindow
    {
        [SerializeField] private WaterSystemDefinition _waterAsset;
        private string _waterGuid;
        private float _waterLevel;
        private Vector3 _waterOffset;
        private void CreateWaterUI(VisualElement parent)
        {
            var section = new Foldout { text = "P05 · 수계·수면·폭포", value = false }; parent.Add(section);
            section.Add(new HelpBox("수계 SO에서 경로·경계·프리셋을 저작합니다. 확정 경로는 재생성으로 바꾸지 않습니다. P04 수계 ID는 그대로 연결하며 미해결 참조는 오류입니다.", HelpBoxMessageType.Info));
            var source = new ObjectField("수계 원본 SO") { objectType = typeof(WaterSystemDefinition), allowSceneObjects = false, value = _definition != null ? _definition.WaterSystem : _waterAsset };
            source.RegisterValueChangedCallback(e => _waterAsset = e.newValue as WaterSystemDefinition); section.Add(source);
            AddButton(section, "수계 SO Inspector 열기", () => Selection.activeObject = WaterSource());
            var guid = new TextField("waterGuid"); guid.RegisterValueChangedCallback(e => _waterGuid = e.newValue); section.Add(guid);
            var level = new FloatField("수위 worldY(m)"); level.RegisterValueChangedCallback(e => _waterLevel = e.newValue); section.Add(level);
            var offset = new Vector3Field("이동량 XYZ(m)"); offset.RegisterValueChangedCallback(e => _waterOffset = e.newValue); section.Add(offset);
            AddButton(section, "수계 검증·재생성", () => { WorldWaterAuthoring.Regenerate(_definition, WaterSource()); Bind(); });
            AddButton(section, "수위 변경 (Undo)", () =>
            {
                WorldWaterAuthoring.SetLevel(_definition, WaterSource(), _waterGuid, _waterLevel); Bind();
            });
            AddButton(section, "수계 이동 (Undo)", () =>
            {
                WorldWaterAuthoring.Move(_definition, WaterSource(), _waterGuid, WorldPlan.Point(_waterOffset)); Bind();
            });
            AddButton(section, "선택 수계 삭제 (Undo)", () => { var water = WaterSource(); WorldWaterAuthoring.Edit(_definition, water, () => { if (water.bodies.RemoveAll(b => b.waterGuid == _waterGuid) != 1) throw new InvalidOperationException("수계 ID가 없거나 중복입니다."); }); Bind(); });
            AddButton(section, "강 경로 제안·확정/Lock (Undo)", () =>
            {
                var water = WaterSource(); var proposal = WorldWaterAuthoring.ProposeRoute(_definition, water, _waterGuid);
                WorldWaterAuthoring.Edit(_definition, water, () => { var body = water.bodies.Find(b => b.waterGuid == _waterGuid); body.route = proposal; body.isRouteLocked = true; }); Bind();
            });
        }
        private WaterSystemDefinition WaterSource() => _waterAsset != null ? _waterAsset : _definition != null && _definition.WaterSystem != null ? _definition.WaterSystem : throw new InvalidOperationException("수계 SO를 연결하세요.");
    }
}
