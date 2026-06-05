using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UPlayGround.InputDefine;
using UPlayGround.UI.InputPrompt;

namespace UPlayGround.UI.InputPrompt.EditorTools
{
    /// <summary>
    /// UI_InputPromptIcon 인스펙터에 "글리프 미리보기" 패널을 추가하는 커스텀 에디터.
    ///
    /// - 런타임 InputManager/씬 없이 PlayerInputActions 에셋을 직접 로드해 해석한다.
    /// - 선택한 디바이스(키보드+마우스 ↔ 게임패드) / 게임패드 브랜드 기준으로 글리프를 그린다.
    /// - 미리보기는 인스펙터 GUI에만 그린다. 대상의 Image/Label 컴포넌트를 전혀 건드리지 않으므로
    ///   프리팹/씬에 절대 저장되지 않는다. (요구: "미리보기만, 저장 안 됨")
    /// </summary>
    [CustomEditor(typeof(UI_InputPromptIcon))]
    public class UI_InputPromptIconEditor : UnityEditor.Editor
    {
        // 글리프 데이터 생성 툴과 동일 경로. 에디터에서는 AssetDatabase로 직접 로드한다.
        private const string InputAssetPath = "Assets/Resources/Input/PlayerInputActions.inputactions";

        private const string PrefDeviceKey = "UPlayGround.InputPromptPreview.Device";
        private const string PrefBrandKey = "UPlayGround.InputPromptPreview.Brand";

        private SerializedProperty _mapNameProp;
        private SerializedProperty _actionNameProp;
        private SerializedProperty _glyphDataProp;

        private ActiveInputDevice _device;
        private GamepadBrand _brand;

        private InputActionAsset _cachedAsset;

        private void OnEnable()
        {
            _mapNameProp = serializedObject.FindProperty("_mapName");
            _actionNameProp = serializedObject.FindProperty("_actionName");
            _glyphDataProp = serializedObject.FindProperty("_glyphData");

            _device = (ActiveInputDevice)EditorPrefs.GetInt(PrefDeviceKey, (int)ActiveInputDevice.KeyboardMouse);
            _brand = (GamepadBrand)EditorPrefs.GetInt(PrefBrandKey, (int)GamepadBrand.Generic);
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("글리프 미리보기 (저장되지 않음)", EditorStyles.boldLabel);

            using (new EditorGUI.IndentLevelScope())
            {
                DrawPreviewControls();
                DrawPreview();
            }
        }

        private void DrawPreviewControls()
        {
            EditorGUI.BeginChangeCheck();
            _device = (ActiveInputDevice)EditorGUILayout.EnumPopup("디바이스", _device);

            using (new EditorGUI.DisabledScope(_device != ActiveInputDevice.Gamepad))
                _brand = (GamepadBrand)EditorGUILayout.EnumPopup("게임패드 브랜드", _brand);

            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetInt(PrefDeviceKey, (int)_device);
                EditorPrefs.SetInt(PrefBrandKey, (int)_brand);
            }
        }

        private void DrawPreview()
        {
            InputActionAsset asset = LoadAsset();
            if (asset == null)
            {
                EditorGUILayout.HelpBox(
                    $"InputActionAsset을 찾을 수 없습니다:\n{InputAssetPath}", MessageType.Warning);
                return;
            }

            string mapName = _mapNameProp.stringValue;
            string actionName = _actionNameProp.stringValue;

            InputAction action = asset.FindActionMap(mapName)?.FindAction(actionName);
            if (action == null)
            {
                EditorGUILayout.HelpBox(
                    $"액션을 찾을 수 없습니다: [{mapName}] / [{actionName}]", MessageType.Warning);
                return;
            }

            var glyphData = _glyphDataProp.objectReferenceValue as InputGlyphDataSO;
            GamepadBrand brand = _device == ActiveInputDevice.Gamepad ? _brand : GamepadBrand.Generic;

            InputGlyphResult result = InputGlyphResolver.ResolveAction(
                action, _device, brand, glyphData, actionName);

            if (glyphData == null)
                EditorGUILayout.HelpBox(
                    "InputGlyphDataSO가 비어 있어 폴백 텍스트로만 표시됩니다.", MessageType.Info);

            DrawParts(result);
        }

        private void DrawParts(in InputGlyphResult result)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                for (int i = 0; i < result.Count; i++)
                {
                    if (i > 0)
                        GUILayout.Label("+", GUILayout.Width(14f), GUILayout.Height(48f));

                    DrawPart(result.Parts[i]);
                }
                GUILayout.FlexibleSpace();
            }
        }

        private void DrawPart(in GlyphPart part)
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(64f)))
            {
                Rect box = GUILayoutUtility.GetRect(48f, 48f, GUILayout.Width(48f), GUILayout.Height(48f));
                if (part.HasSprite)
                    DrawSprite(box, part.Sprite);
                else
                    EditorGUI.DrawRect(box, new Color(0f, 0f, 0f, 0.15f));

                // 스프라이트가 있어도 어떤 키인지 캡션으로 같이 보여준다.
                GUILayout.Label(part.Text ?? string.Empty, EditorStyles.miniLabel, GUILayout.Width(60f));
            }
        }

        // 아틀라스에 포함된 스프라이트도 올바른 영역만 그리도록 texCoords를 계산한다.
        private static void DrawSprite(Rect position, Sprite sprite)
        {
            Texture2D tex = sprite.texture;
            if (tex == null)
                return;

            Rect r = sprite.rect;
            var texCoords = new Rect(
                r.x / tex.width,
                r.y / tex.height,
                r.width / tex.width,
                r.height / tex.height);

            GUI.DrawTextureWithTexCoords(position, tex, texCoords, alphaBlend: true);
        }

        private InputActionAsset LoadAsset()
        {
            if (_cachedAsset == null)
                _cachedAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath);
            return _cachedAsset;
        }
    }
}
