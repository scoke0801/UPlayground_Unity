using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UPlayGround.EditorTools;
using UPlayGround.WorldBuilder.Core;
using UPlayGround.WorldBuilder.Data;

namespace UPlayGround.WorldBuilder.Editor
{
    /// <summary>단계별 저작·캡처·외부 RAW 왕복을 한 곳에서 실행하는 작업 창.</summary>
    public sealed class WorldBuilderAuthoringWindow : EditorWindow
    {
        [SerializeField] private WorldDefinition _definition;
        private WorldTerrainAuthoringSession _session;
        private HelpBox _status;
        private VisualElement _operations;
        private ObjectField _definitionField;
        private RectInt _bounds = new RectInt(1000, 1000, 64, 64);
        private float _value = 1;
        private int _falloff, _radius = 1, _iterations = 1;
        private bool _absoluteRaw;

        [UPlaygroundTool("WorldBuilder.Authoring")]
        public static void Open() => GetWindow<WorldBuilderAuthoringWindow>("WorldBuilder");

        public void CreateGUI()
        {
            rootVisualElement.Clear();
            UPlaygroundEditorUX.PrepareRoot(rootVisualElement);
            var scroll = new ScrollView();
            rootVisualElement.Add(scroll);
            scroll.Add(UPlaygroundEditorUX.CreateHeader("WorldBuilder · 지형 저작", "Base를 보존하고 수정 명령을 쌓습니다. 브러시는 Sculpt 시작 후 사용하세요.", "Terrain Icon", "P02"));
            _status = new HelpBox("P01 미리보기 씬을 열고 저작 정의를 연결하세요.", HelpBoxMessageType.Info);
            scroll.Add(_status);
            _definitionField = new ObjectField("저작 정의") { objectType = typeof(WorldDefinition), value = _definition, allowSceneObjects = false };
            _definitionField.RegisterValueChangedCallback(change => Run(() => { _definition = change.newValue as WorldDefinition; Bind(); }));
            scroll.Add(_definitionField);
            AddButton(scroll, "현재 미리보기의 저작 정의 만들기/연결", () =>
            {
                _definition = WorldBuilderAuthoringAssets.CreateForPreview(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                _definitionField.SetValueWithoutNotify(_definition);
                Bind();
            });
            var bounds = new RectIntField("표본 영역 X/Z/너비/높이") { value = _bounds };
            bounds.RegisterValueChangedCallback(change => _bounds = change.newValue);
            scroll.Add(bounds);
            var value = new FloatField("높이 변화/목표 고도(m)") { value = _value };
            value.RegisterValueChangedCallback(change => _value = change.newValue);
            scroll.Add(value);
            var falloff = new IntegerField("가장자리 Falloff(표본)") { value = _falloff };
            falloff.RegisterValueChangedCallback(change => _falloff = change.newValue);
            scroll.Add(falloff);
            var radius = new IntegerField("Smooth 반경(1~8)") { value = _radius };
            radius.RegisterValueChangedCallback(change => _radius = change.newValue);
            scroll.Add(radius);
            var iterations = new IntegerField("Smooth 반복(1~16)") { value = _iterations };
            iterations.RegisterValueChangedCallback(change => _iterations = change.newValue);
            scroll.Add(iterations);
            AddButton(scroll, "높이 더하기", () => Add(TerrainOperationKind.AddDelta));
            AddButton(scroll, "목표 고도 고정", () => Add(TerrainOperationKind.SetAbsolute));
            AddButton(scroll, "평탄화", () => Add(TerrainOperationKind.Flatten));
            AddButton(scroll, "Smooth", () => Add(TerrainOperationKind.Smooth));
            AddButton(scroll, "HoleCut 추가(셀 영역)", () => Add(TerrainOperationKind.HoleCut));
            AddButton(scroll, "자동 높이·Hole 연산 보호", () => Add(TerrainOperationKind.ProtectRegion));
            AddButton(scroll, "Sculpt 시작", () => Require().BeginSculpt());
            AddButton(scroll, "Sculpt/미캡처 수정 캡처", () => Require().CaptureSculpt());
            AddButton(scroll, "세션 기준 RAW 내보내기", () =>
            {
                string path = EditorUtility.SaveFilePanel("R16 RAW 내보내기", "output/WorldBuilder", "terrain-edit", "raw");
                if (!string.IsNullOrEmpty(path)) Require().ExportRaw(path);
            });
            var absolute = new Toggle("RAW 변경 부분을 절대 높이로 캡처") { value = _absoluteRaw };
            absolute.RegisterValueChangedCallback(change => _absoluteRaw = change.newValue);
            scroll.Add(absolute);
            AddButton(scroll, "편집 RAW 가져오기", () =>
            {
                string path = EditorUtility.OpenFilePanel("R16 RAW 가져오기", "output/WorldBuilder", "raw");
                if (!string.IsNullOrEmpty(path)) Require().ImportRaw(path, _absoluteRaw);
            });
            AddButton(scroll, "명령에서 재생성", () => Require().Regenerate());
            AddButton(scroll, "미캡처 수정·세션 버리기", () => Require().RevertUncaptured());
            AddButton(scroll, "Bake 입력 준비 검사", () => Require().EnsureReadyForBake());
            _operations = new VisualElement();
            scroll.Add(_operations);
            if (_definition != null) Run(Bind);
        }

        private void Add(TerrainOperationKind kind)
        {
            if (_falloff < 0) throw new ArgumentOutOfRangeException(nameof(_falloff));
            var session = Require();
            int sequence = 0;
            foreach (var item in _definition.Operations) sequence = Math.Max(sequence, item.sequence + 1);
            var record = new TerrainOperationRecord { operationGuid = Guid.NewGuid().ToString("N"), kind = kind, x = _bounds.x, z = _bounds.y, width = _bounds.width, height = _bounds.height,
                value = _value, sequence = sequence, stage = kind == TerrainOperationKind.HoleCut ? 50 : 40, radius = _radius, iterations = _iterations, protection = TerrainProtection.Height | TerrainProtection.Holes };
            if (_falloff == 0) session.AddOperation(record);
            else
            {
                var masks = new float[checked(_bounds.width * _bounds.height)];
                for (int z = 0; z < _bounds.height; z++)
                for (int x = 0; x < _bounds.width; x++)
                {
                    float edge = Math.Min(Math.Min(x, _bounds.width - 1 - x), Math.Min(z, _bounds.height - 1 - z));
                    float weight = Mathf.Clamp01(edge / _falloff);
                    masks[z * _bounds.width + x] = weight * weight * (3 - 2 * weight);
                }
                session.AddMaskedOperation(record, masks);
            }
        }

        private void Bind()
        {
            _session?.Dispose();
            _session = null;
            if (_definition != null) _session = new WorldTerrainAuthoringSession(_definition, WorldBuilderAuthoringAssets.FindTerrains(_definition));
            RefreshOperations();
        }

        private WorldTerrainAuthoringSession Require() => _session ?? throw new InvalidOperationException("저작 정의와 열린 미리보기를 먼저 연결하세요.");
        private void AddButton(VisualElement parent, string label, Action action) => parent.Add(new Button(() => Run(action)) { text = label });

        private void Run(Action action)
        {
            try
            {
                action();
                _status.text = _definition == null ? "저작 정의를 연결하세요." : "작업 반영. 세션: " + (_definition.ActiveSession == null ? "없음" : _definition.ActiveSession.kind.ToString()) + " / Dirty: " + _definition.DirtyLayers;
                _status.messageType = HelpBoxMessageType.Info;
                RefreshOperations();
            }
            catch (Exception exception) { _status.text = exception.ToString(); _status.messageType = HelpBoxMessageType.Error; }
        }

        private void RefreshOperations()
        {
            if (_operations == null) return;
            _operations.Clear();
            if (_definition == null) return;
            for (int i = 0; i < _definition.Operations.Count; i++)
            {
                int index = i;
                var record = _definition.Operations[i];
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };
                var toggle = new Toggle(record.kind + " · " + record.operationGuid.Substring(0, Math.Min(8, record.operationGuid.Length))) { value = record.enabled };
                toggle.RegisterValueChangedCallback(change => Run(() => { var copy = _definition.Operations[index].Clone(); copy.enabled = change.newValue; Require().ReplaceOperation(index, copy); }));
                row.Add(toggle);
                row.Add(new Button(() => Run(() => Require().ReplaceOperation(index, null))) { text = "삭제" });
                _operations.Add(row);
            }
            if (_session != null && _session.LastError != null && _status != null) { _status.text = _session.LastError; _status.messageType = HelpBoxMessageType.Error; }
        }

        private void OnEnable() => Undo.undoRedoPerformed += RefreshOperations;
        private void OnDisable() { Undo.undoRedoPerformed -= RefreshOperations; _session?.Dispose(); _session = null; }
    }
}
