using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UPlayGround.Components;

namespace UPlayGround.Editor.P09Builder
{
    /// <summary>외부 모델의 장비 구성을 미리 보고 P09 입력용 모델을 생성한다.</summary>
    public sealed class ExternalCharacterBuilderWindow : EditorWindow
    {
        public const string ToolId = "UPlayGround/캐릭터/P09/외부 모델 장비 빌더";
        [SerializeField] private ExternalCharacterProfile _profile;
        private PreviewRenderUtility _preview;
        private GameObject _instance;
        private int[] _selections;
        private VisualElement _slotPanel;
        private Label _status;
        private TextField _outputName;
        private float _yaw = 20f;

        /// <summary>통합 툴 런처에서 외부 모델 장비 빌더를 연다.</summary>
        [UPlayGround.EditorTools.UPlaygroundTool(ToolId)]
        public static void Open() => GetWindow<ExternalCharacterBuilderWindow>("외부 모델 장비 빌더").Show();

        private void CreateGUI()
        {
            minSize = new Vector2(760, 620);
            var toolbar = new Toolbar();
            toolbar.Add(new ToolbarButton(CreateProfileFromSelection) { text = "선택 모델 → 프로필" });
            toolbar.Add(new ToolbarButton(RebuildPreview) { text = "미리보기 갱신" });
            toolbar.Add(new ToolbarButton(() => Selection.activeObject = _profile) { text = "프로필 편집" });
            rootVisualElement.Add(toolbar);
            var profileField = new ObjectField("모델 / 장비 프로필") { objectType = typeof(ExternalCharacterProfile), allowSceneObjects = false, value = _profile };
            profileField.RegisterValueChangedCallback(evt => { _profile = evt.newValue as ExternalCharacterProfile; RebuildPreview(); });
            rootVisualElement.Add(profileField);
            rootVisualElement.Add(new HelpBox("원본 얼굴·재질·스킨을 유지합니다. 같은 모델의 의상과 본 부착 장신구를 슬롯으로 조합합니다. 생성한 모델은 P09 빌더에서 NPC로 구성할 수 있습니다.", HelpBoxMessageType.Info));
            var split = new TwoPaneSplitView(0, 260, TwoPaneSplitViewOrientation.Horizontal) { style = { flexGrow = 1 } };
            _slotPanel = new ScrollView();
            split.Add(_slotPanel);
            var right = new VisualElement { style = { flexGrow = 1 } };
            var angle = new Slider("회전", -180, 180) { value = _yaw };
            angle.RegisterValueChangedCallback(evt => { _yaw = evt.newValue; Repaint(); });
            right.Add(angle);
            var canvas = new IMGUIContainer(() => DrawPreview(GUILayoutUtility.GetRect(100, 100, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))) { style = { flexGrow = 1 } };
            right.Add(canvas);
            split.Add(right);
            rootVisualElement.Add(split);
            _outputName = new TextField("새 프리팹 이름") { value = _profile != null ? _profile.name : "ExternalCharacter" };
            rootVisualElement.Add(_outputName);
            rootVisualElement.Add(new Button(Export) { text = "새 프리팹 생성 → P09 빌더에서 열기" });
            _status = new Label { style = { whiteSpace = WhiteSpace.Normal } };
            rootVisualElement.Add(_status);
            RebuildPreview();
        }

        private void RebuildPreview()
        {
            ClearPreview();
            _slotPanel?.Clear();
            if (_profile == null || _slotPanel == null) return;
            try
            {
                _preview = new PreviewRenderUtility();
                _instance = ExternalCharacterAssembler.CreateInstance(_profile, _preview.camera.scene);
                _preview.AddSingleGO(_instance);
                _selections = _profile.slots.Select(s => s.defaultIndex).ToArray();
                var visuals = _instance.GetComponent<CharacterEquipmentVisuals>();
                for (int i = 0; i < _profile.slots.Length; i++)
                {
                    int index = i;
                    var slot = _profile.slots[i];
                    var field = new DropdownField(slot.id, slot.variants.Select(v => v.id).ToList(), slot.defaultIndex);
                    field.RegisterValueChangedCallback(evt =>
                    {
                        if (!visuals.TryEquip(slot.id, evt.newValue)) return;
                        _selections[index] = field.index;
                        Repaint();
                    });
                    _slotPanel.Add(field);
                }
                _outputName.value = _profile.name;
                _status.text = "모델·장비 연결 검사 통과. 외형과 동작은 미리보기 및 Play Mode에서 확인하세요.";
            }
            catch (Exception exception) { ClearPreview(); _status.text = exception.Message; }
        }

        private void Export()
        {
            try
            {
                string name = _outputName.value.Trim();
                if (name.Length == 0 || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                    throw new InvalidOperationException("프리팹 이름을 올바르게 입력하세요.");
                var prefab = ExternalCharacterAssembler.Build(_profile, ExternalCharacterAssembler.OutputFolder + "/" + name + ".prefab", _selections);
                Selection.activeObject = prefab;
                P09CharacterPrefabBuilderWindow.OpenWithExternalModel(prefab);
                _status.text = "새 모델 프리팹 생성 완료. P09 빌더에서 Actor 종류와 데이터를 선택하세요.";
            }
            catch (Exception exception) { _status.text = exception.Message; }
        }

        private void CreateProfileFromSelection()
        {
            try
            {
                var source = Selection.activeObject as GameObject;
                ExternalCharacterAssembler.ValidateSource(source);
                string path = EditorUtility.SaveFilePanelInProject("장비 프로필 저장", source.name + "_Equipment", "asset", "새 프로필 경로");
                if (string.IsNullOrEmpty(path)) return;
                if (File.Exists(path)) throw new InvalidOperationException("기존 프로필은 덮어쓰지 않습니다.");
                var profile = CreateInstance<ExternalCharacterProfile>();
                profile.sourcePrefab = source;
                profile.authoringNotes = "rendererPaths는 원본 루트 기준입니다. 의상에 맞는 몸 가림 메시를 같은 슬롯 외형에 포함하세요. 다른 체형의 의상은 Blender에서 별도로 피팅해야 합니다.\n\n사용 가능한 경로:\n" +
                    string.Join("\n", source.GetComponentsInChildren<Renderer>(true).Select(r => AnimationUtility.CalculateTransformPath(r.transform, source.transform)));
                AssetDatabase.CreateAsset(profile, path);
                _profile = profile;
                Selection.activeObject = profile;
                rootVisualElement.Clear();
                CreateGUI();
            }
            catch (Exception exception) { _status.text = exception.Message; }
        }

        private void DrawPreview(Rect rect)
        {
            if (_instance == null || _preview == null || rect.width < 1 || rect.height < 1) return;
            _instance.transform.rotation = Quaternion.Euler(0, _yaw, 0);
            var renderers = _instance.GetComponentsInChildren<Renderer>().Where(r => r.enabled).ToArray();
            if (renderers.Length == 0) return;
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            var camera = _preview.camera;
            camera.fieldOfView = 30;
            float span = Mathf.Max(bounds.size.y, bounds.size.x / Mathf.Max(0.1f, rect.width / rect.height));
            camera.transform.position = bounds.center + Vector3.forward * (span * 2.1f);
            camera.transform.LookAt(bounds.center);
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100;
            _preview.lights[0].intensity = 1;
            _preview.lights[0].transform.rotation = Quaternion.Euler(35, 150, 0);
            _preview.lights[1].intensity = 0.6f;
            _preview.ambientColor = new Color(0.5f, 0.5f, 0.5f);
            _preview.BeginPreview(rect, GUIStyle.none);
            _preview.Render(true);
            GUI.DrawTexture(rect, _preview.EndPreview(), ScaleMode.StretchToFill, false);
        }

        private void OnDisable() => ClearPreview();

        private void ClearPreview()
        {
            _preview?.Cleanup();
            _preview = null;
            _instance = null;
        }
    }
}
