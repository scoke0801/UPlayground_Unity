using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UPlayGround.InputDefine;
using UPlayGround.UI.InputPrompt;

namespace UPlayGround.UI.InputPrompt.EditorTools
{
    /// <summary>
    /// PlayerInputActions 에셋을 근거로 controlPath 목록을 자동 추출해
    /// InputGlyphDataSO를 생성/동기화하는 에디터 툴.
    ///
    /// - 메뉴: UPlayGround/입력/글리프 데이터 생성·동기화
    /// - InputGlyphDataSO 인스펙터의 "에셋에서 controlPath 동기화" 버튼
    ///
    /// controlPath는 InputAction.GetBindingDisplayString이 돌려주는 형태와 동일하게
    /// 디바이스 prefix를 제거한 세그먼트로 추출한다(예: "1", "leftButton", "dpad/up").
    /// </summary>
    public static class InputGlyphDataGenerator
    {
        private const string AssetPath = "Assets/Resources/Input/PlayerInputActions.inputactions";
        private const string DefaultGlyphDataDir = "Assets/10.Datas/Input";
        private const string DefaultGlyphDataPath = DefaultGlyphDataDir + "/InputGlyphData.asset";

        // 레거시 테스트 전용 맵. 글리프 소스에서 제외(동일 물리 버튼 중복 방지).
        private const string LegacyGamepadMapName = "Gamepad";

        // 키캡 글리프로 표현하기 부적합한 순수 아날로그/포인터 controlPath.
        // (Move/Look/Zoom 등에 쓰이는 마우스 이동·휠·스틱 전체 축. 제외해도 폴백 텍스트로 표시됨)
        // 주의: "rightStick/right" 같은 방향 세그먼트는 LockOnSwitch에 쓰이는 이산 입력이라 제외하지 않는다.
        private static readonly HashSet<string> ExcludedSegments = new HashSet<string>
        {
            "delta",      // <Mouse>/delta   — 마우스 이동량 (Look)
            "position",   // <Mouse>/position — 커서 위치 (UI Point)
            "scroll",     // <Mouse>/scroll  — 휠 (Zoom)
            "leftStick",  // <Gamepad>/leftStick  — 이동 스틱 (Move)
            "rightStick", // <Gamepad>/rightStick — 시점 스틱 (Look)
        };

        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/입력/글리프 데이터 생성·동기화")]
        public static void CreateOrSync()
        {
            var inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(AssetPath);
            if (inputAsset == null)
            {
                EditorUtility.DisplayDialog("입력 글리프 데이터",
                    $"InputActionAsset을 찾을 수 없습니다:\n{AssetPath}", "확인");
                return;
            }

            ExtractControlPaths(inputAsset, out var kmPaths, out var gpPaths);

            var glyphData = FindOrCreateGlyphData();
            Undo.RecordObject(glyphData, "Sync Input Glyph Control Paths");
            glyphData.EditorSyncControlPaths(kmPaths, gpPaths);
            EditorUtility.SetDirty(glyphData);
            AssetDatabase.SaveAssets();

            Selection.activeObject = glyphData;
            EditorGUIUtility.PingObject(glyphData);

            Debug.Log($"[InputGlyphData] 동기화 완료 — 키보드/마우스 {kmPaths.Count}개, 게임패드 {gpPaths.Count}개 controlPath. " +
                      "스프라이트를 인스펙터에서 할당하세요.");
        }

        /// <summary>
        /// 인스펙터 버튼 등에서 호출. 대상 SO를 직접 받아 키보드/마우스 + 제네릭 게임패드를 동기화한다.
        /// </summary>
        public static void SyncInto(InputGlyphDataSO glyphData)
        {
            var inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(AssetPath);
            if (inputAsset == null || glyphData == null)
                return;

            ExtractControlPaths(inputAsset, out var kmPaths, out var gpPaths);
            Undo.RecordObject(glyphData, "Sync Input Glyph Control Paths");
            glyphData.EditorSyncControlPaths(kmPaths, gpPaths);
            EditorUtility.SetDirty(glyphData);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// 옵트인: 특정 브랜드 오버라이드 리스트를 게임패드 controlPath로 채운다.
        /// 해당 브랜드 전용 아트가 있을 때만 사용. (없으면 제네릭으로 폴백되므로 채울 필요 없음)
        /// </summary>
        public static void SyncBrandInto(InputGlyphDataSO glyphData, GamepadBrand brand)
        {
            var inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(AssetPath);
            if (inputAsset == null || glyphData == null)
                return;

            ExtractControlPaths(inputAsset, out _, out var gpPaths);
            Undo.RecordObject(glyphData, "Sync Input Glyph Brand Control Paths");
            glyphData.EditorSyncBrandControlPaths(brand, gpPaths);
            EditorUtility.SetDirty(glyphData);
            AssetDatabase.SaveAssets();
        }

        // 에셋의 모든 맵(레거시 Gamepad 제외)을 순회하며 단순 바인딩의 controlPath를
        // 디바이스별로 중복 없이(등장 순서 보존) 수집한다.
        private static void ExtractControlPaths(InputActionAsset asset,
            out List<string> keyboardMouse, out List<string> gamepad)
        {
            var km = new List<string>();
            var gp = new List<string>();
            var kmSeen = new HashSet<string>();
            var gpSeen = new HashSet<string>();

            foreach (var map in asset.actionMaps)
            {
                if (map.name == LegacyGamepadMapName)
                    continue;

                foreach (var binding in map.bindings)
                {
                    if (binding.isComposite || binding.isPartOfComposite)
                        continue; // 컴포지트(2DVector/OneModifier)는 키캡 1개로 부적합

                    string path = binding.effectivePath;
                    if (string.IsNullOrEmpty(path) || !path.StartsWith("<", StringComparison.Ordinal))
                        continue;

                    string segment = ToControlPathSegment(path);
                    if (string.IsNullOrEmpty(segment) || ExcludedSegments.Contains(segment))
                        continue; // 순수 아날로그/포인터 입력 제외

                    if (path.StartsWith("<Gamepad>", StringComparison.Ordinal) ||
                        path.StartsWith("<DualShockGamepad>", StringComparison.Ordinal) ||
                        path.StartsWith("<XInputController>", StringComparison.Ordinal))
                    {
                        if (gpSeen.Add(segment)) gp.Add(segment);
                    }
                    else if (path.StartsWith("<Keyboard>", StringComparison.Ordinal) ||
                             path.StartsWith("<Mouse>", StringComparison.Ordinal))
                    {
                        if (kmSeen.Add(segment)) km.Add(segment);
                    }
                }
            }

            keyboardMouse = km;
            gamepad = gp;
        }

        // "<Keyboard>/1" -> "1", "<Gamepad>/dpad/up" -> "dpad/up"
        private static string ToControlPathSegment(string fullPath)
        {
            int i = fullPath.IndexOf(">/", StringComparison.Ordinal);
            return i >= 0 ? fullPath.Substring(i + 2) : fullPath;
        }

        private static InputGlyphDataSO FindOrCreateGlyphData()
        {
            string[] guids = AssetDatabase.FindAssets("t:InputGlyphDataSO");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<InputGlyphDataSO>(path);
            }

            // 없으면 기본 경로에 생성
            if (!AssetDatabase.IsValidFolder(DefaultGlyphDataDir))
                AssetDatabase.CreateFolder("Assets/10.Datas", "Input");

            var asset = ScriptableObject.CreateInstance<InputGlyphDataSO>();
            AssetDatabase.CreateAsset(asset, DefaultGlyphDataPath);
            Debug.Log($"[InputGlyphData] 새 에셋 생성: {DefaultGlyphDataPath}");
            return asset;
        }

        // ────────────────────────────── 스프라이트 자동 연결 ──────────────────────────────

        private const string IconRoot = "Assets/ExternalAssets/UI/InputIcon";

        public enum AutoLinkTarget { KeyboardMouse, GenericGamepad, Xbox, PlayStation }

        // 자동 연결 대상별 스프라이트 폴더(Default = 표준 해상도 PNG. Double/Vector 중복 회피).
        // 제네릭 폴백은 Kenney "Generic" 폴더가 추상 아이콘뿐이라 controlPath 키에 매핑 불가 → Xbox 아트를 사용.
        private static string FolderFor(AutoLinkTarget target)
        {
            switch (target)
            {
                case AutoLinkTarget.KeyboardMouse: return IconRoot + "/Keyboard & Mouse/Default";
                case AutoLinkTarget.GenericGamepad: return IconRoot + "/Xbox Series/Default";
                case AutoLinkTarget.Xbox: return IconRoot + "/Xbox Series/Default";
                case AutoLinkTarget.PlayStation: return IconRoot + "/PlayStation Series/Default";
                default: return null;
            }
        }

        private static InputGlyphDataSO.GlyphCategory CategoryFor(AutoLinkTarget target)
        {
            switch (target)
            {
                case AutoLinkTarget.KeyboardMouse: return InputGlyphDataSO.GlyphCategory.KeyboardMouse;
                case AutoLinkTarget.GenericGamepad: return InputGlyphDataSO.GlyphCategory.Gamepad;
                case AutoLinkTarget.Xbox: return InputGlyphDataSO.GlyphCategory.Xbox;
                case AutoLinkTarget.PlayStation: return InputGlyphDataSO.GlyphCategory.PlayStation;
                default: return InputGlyphDataSO.GlyphCategory.Gamepad;
            }
        }

        /// <summary>
        /// 대상 폴더의 스프라이트를 controlPath에 맞춰 자동 연결한다.
        /// 1) 해당 카테고리 controlPath 동기화 → 2) Kenney 파일명 매핑 → 3) 텍스처를 Sprite 타입으로 변환 → 4) 할당.
        /// </summary>
        public static int AutoLinkSprites(InputGlyphDataSO glyphData, AutoLinkTarget target, bool overwriteExisting)
        {
            var inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(AssetPath);
            if (inputAsset == null || glyphData == null)
                return 0;

            string folder = FolderFor(target);
            if (string.IsNullOrEmpty(folder) || !AssetDatabase.IsValidFolder(folder))
            {
                Debug.LogWarning($"[InputGlyphData] 스프라이트 폴더를 찾을 수 없습니다: {folder}");
                return 0;
            }

            ExtractControlPaths(inputAsset, out var kmPaths, out var gpPaths);
            Undo.RecordObject(glyphData, "Auto-Link Input Glyph Sprites");

            // 1) 엔트리(controlPath)부터 보장 — 비어 있으면 채울 대상이 없으므로.
            bool isKeyboardMouse = target == AutoLinkTarget.KeyboardMouse;
            var controlPaths = isKeyboardMouse ? kmPaths : gpPaths;
            switch (target)
            {
                case AutoLinkTarget.KeyboardMouse:
                case AutoLinkTarget.GenericGamepad:
                    glyphData.EditorSyncControlPaths(kmPaths, gpPaths);
                    break;
                case AutoLinkTarget.Xbox:
                    glyphData.EditorSyncBrandControlPaths(GamepadBrand.Xbox, gpPaths);
                    break;
                case AutoLinkTarget.PlayStation:
                    glyphData.EditorSyncBrandControlPaths(GamepadBrand.PlayStation, gpPaths);
                    break;
            }

            // 2) controlPath → Kenney 파일명 → 에셋 경로
            var byBasename = ScanTextures(folder);
            var cpToPath = new Dictionary<string, string>();
            var texturePaths = new HashSet<string>();
            var unmatched = new List<string>();

            foreach (var cp in controlPaths)
            {
                string basename = isKeyboardMouse ? MapKeyboardMouse(cp) : MapGamepad(target, cp);
                if (basename != null && byBasename.TryGetValue(basename.ToLowerInvariant(), out var assetPath))
                {
                    cpToPath[cp] = assetPath;
                    texturePaths.Add(assetPath);
                }
                else
                {
                    unmatched.Add(basename != null ? $"{cp}(\"{basename}\")" : cp);
                }
            }

            // 3) 매칭된 텍스처를 Sprite 타입으로 변환(아니면 LoadAsset<Sprite>가 null).
            int converted = EnsureSpriteImport(texturePaths);

            // 4) Sprite 로드 → controlPath별 할당
            var cpToSprite = new Dictionary<string, Sprite>();
            foreach (var kv in cpToPath)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(kv.Value);
                if (sprite != null)
                    cpToSprite[kv.Key] = sprite;
                else
                    unmatched.Add($"{kv.Key}(스프라이트 로드 실패)");
            }

            int assigned = glyphData.EditorAssignSprites(CategoryFor(target), cpToSprite, overwriteExisting);
            EditorUtility.SetDirty(glyphData);
            AssetDatabase.SaveAssets();

            Debug.Log($"[InputGlyphData] 자동연결({target}) — 할당 {assigned}/{controlPaths.Count}, " +
                      $"Sprite 변환 {converted}건." +
                      (unmatched.Count > 0 ? $" 미매칭: {string.Join(", ", unmatched)}" : ""));
            return assigned;
        }

        // 폴더 내 텍스처의 파일명(소문자, 확장자 제외) → 에셋 경로 인덱스.
        private static Dictionary<string, string> ScanTextures(string folder)
        {
            var dict = new Dictionary<string, string>();
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string name = System.IO.Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                if (!dict.ContainsKey(name))
                    dict[name] = path;
            }
            return dict;
        }

        // 텍스처 임포트 타입을 Sprite(2D/UI)로 보장. 변환한 개수를 반환.
        private static int EnsureSpriteImport(HashSet<string> texturePaths)
        {
            int converted = 0;
            foreach (var path in texturePaths)
            {
                if (AssetImporter.GetAtPath(path) is TextureImporter importer &&
                    importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.SaveAndReimport();
                    converted++;
                }
            }
            return converted;
        }

        // controlPath → Kenney 키보드/마우스 파일명.
        private static string MapKeyboardMouse(string controlPath)
        {
            switch (controlPath)
            {
                case "space": return "keyboard_space";
                case "shift": return "keyboard_shift";
                case "ctrl": return "keyboard_ctrl";
                case "tab": return "keyboard_tab";
                case "escape": return "keyboard_escape";
                case "alt": return "keyboard_alt";
                case "enter": return "keyboard_enter";
                case "backquote": return "keyboard_tilde";
                case "leftButton": return "mouse_left";
                case "rightButton": return "mouse_right";
                case "middleButton": return "mouse_scroll";
            }
            if (controlPath.Length == 1 && char.IsLetterOrDigit(controlPath[0]))
                return "keyboard_" + controlPath.ToLowerInvariant();
            return null;
        }

        // controlPath → Kenney 게임패드 파일명. PlayStation은 PS 표기, 그 외(Xbox/제네릭)는 Xbox 표기.
        private static string MapGamepad(AutoLinkTarget target, string controlPath)
        {
            bool ps = target == AutoLinkTarget.PlayStation;
            switch (controlPath)
            {
                case "buttonSouth": return ps ? "playstation_button_cross" : "xbox_button_a";
                case "buttonEast": return ps ? "playstation_button_circle" : "xbox_button_b";
                case "buttonWest": return ps ? "playstation_button_square" : "xbox_button_x";
                case "buttonNorth": return ps ? "playstation_button_triangle" : "xbox_button_y";
                case "dpad/up": return ps ? "playstation_dpad_up" : "xbox_dpad_up";
                case "dpad/down": return ps ? "playstation_dpad_down" : "xbox_dpad_down";
                case "dpad/left": return ps ? "playstation_dpad_left" : "xbox_dpad_left";
                case "dpad/right": return ps ? "playstation_dpad_right" : "xbox_dpad_right";
                case "leftShoulder": return ps ? "playstation_trigger_l1" : "xbox_lb";
                case "rightShoulder": return ps ? "playstation_trigger_r1" : "xbox_rb";
                case "leftTrigger": return ps ? "playstation_trigger_l2" : "xbox_lt";
                case "rightTrigger": return ps ? "playstation_trigger_r2" : "xbox_rt";
                case "leftStickPress": return ps ? "playstation_button_l3" : "xbox_stick_l_press";
                case "rightStickPress": return ps ? "playstation_button_r3" : "xbox_stick_r_press";
                case "rightStick/right": return ps ? "playstation_stick_r_right" : "xbox_stick_r_right";
                case "rightStick/left": return ps ? "playstation_stick_r_left" : "xbox_stick_r_left";
            }
            return null;
        }
    }

    [CustomEditor(typeof(InputGlyphDataSO))]
    public class InputGlyphDataSOEditor : UnityEditor.Editor
    {
        private bool _overwriteExisting;

        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(
                "PlayerInputActions 에셋에서 controlPath를 자동 추출하고, InputIcon 폴더의 " +
                "Kenney 스프라이트를 controlPath에 맞춰 자동 연결합니다. " +
                "기존 스프라이트 할당은 controlPath 기준으로 보존됩니다.",
                MessageType.Info);

            var glyphData = (InputGlyphDataSO)target;

            if (GUILayout.Button("controlPath만 동기화 (스프라이트 비움)"))
            {
                InputGlyphDataGenerator.SyncInto(glyphData);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("스프라이트 자동 연결 (Assets/ExternalAssets/UI/InputIcon)",
                EditorStyles.miniBoldLabel);
            _overwriteExisting = EditorGUILayout.Toggle("기존 스프라이트 덮어쓰기", _overwriteExisting);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("키보드/마우스"))
                    InputGlyphDataGenerator.AutoLinkSprites(glyphData,
                        InputGlyphDataGenerator.AutoLinkTarget.KeyboardMouse, _overwriteExisting);
                if (GUILayout.Button("제네릭 패드 (Xbox 스타일)"))
                    InputGlyphDataGenerator.AutoLinkSprites(glyphData,
                        InputGlyphDataGenerator.AutoLinkTarget.GenericGamepad, _overwriteExisting);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Xbox 오버라이드"))
                    InputGlyphDataGenerator.AutoLinkSprites(glyphData,
                        InputGlyphDataGenerator.AutoLinkTarget.Xbox, _overwriteExisting);
                if (GUILayout.Button("PlayStation 오버라이드"))
                    InputGlyphDataGenerator.AutoLinkSprites(glyphData,
                        InputGlyphDataGenerator.AutoLinkTarget.PlayStation, _overwriteExisting);
            }

            if (GUILayout.Button("전체 자동 연결 (키보드/마우스 + 제네릭 + PlayStation)"))
            {
                InputGlyphDataGenerator.AutoLinkSprites(glyphData,
                    InputGlyphDataGenerator.AutoLinkTarget.KeyboardMouse, _overwriteExisting);
                InputGlyphDataGenerator.AutoLinkSprites(glyphData,
                    InputGlyphDataGenerator.AutoLinkTarget.GenericGamepad, _overwriteExisting);
                InputGlyphDataGenerator.AutoLinkSprites(glyphData,
                    InputGlyphDataGenerator.AutoLinkTarget.PlayStation, _overwriteExisting);
            }

            EditorGUILayout.Space();
            DrawDefaultInspector();
        }
    }
}
