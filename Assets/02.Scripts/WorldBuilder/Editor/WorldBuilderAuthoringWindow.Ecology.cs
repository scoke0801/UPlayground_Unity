using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UPlayGround.WorldBuilder.Data;

namespace UPlayGround.WorldBuilder.Editor
{
    public sealed partial class WorldBuilderAuthoringWindow
    {
        [SerializeField] private WorldEcology _ecologyAsset;

        private void CreateEcologyUI(VisualElement parent)
        {
            var section = new Foldout { text = "P06 · 동굴·바이옴·식생", value = false };
            parent.Add(section);
            var field = new ObjectField("생태 원본 SO") {
                objectType = typeof(WorldEcology), allowSceneObjects = false, value = _definition?.Ecology ?? _ecologyAsset
            };
            field.RegisterValueChangedCallback(e => _ecologyAsset = e.newValue as WorldEcology);
            section.Add(field);
            WorldEcology Source() => _definition?.Ecology ?? _ecologyAsset;
            AddButton(section, "생태 원본 Inspector 열기", () => Selection.activeObject = Source());
            var id = new TextField("동굴 ID");
            var offset = new Vector3Field("이동량 XYZ(m)");
            section.Add(id);
            section.Add(offset);
            AddButton(section, "동굴·바이옴·식생 검증 및 재생성", () => { WorldEcologyAuthoring.Regenerate(_definition, Source()); Bind(); });
            AddButton(section, "선택 동굴 이동 (Undo)", () => { WorldEcologyAuthoring.Move(_definition, Source(), id.value, offset.value); Bind(); });
            AddButton(section, "선택 동굴 삭제 (Undo)", () => { WorldEcologyAuthoring.Delete(_definition, Source(), id.value); Bind(); });
        }
    }
}

