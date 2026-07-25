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

            // 2) 폴더의 파일명을 명명 규칙으로 파싱해 controlPath → 에셋 경로 표를 만든다.
            //    Kenney 명명 규칙을 따르는 새 PNG가 폴더에 추가되면 코드 수정 없이 이 표에 자동 반영된다.
            var byBasename = ScanTextures(folder);
            bool isPlayStation = target == AutoLinkTarget.PlayStation;
            var inferred = isKeyboardMouse
                ? InferKeyboardMouseIcons(byBasename)
                : InferGamepadIcons(byBasename, isPlayStation);

            var cpToPath = new Dictionary<string, string>();
            var texturePaths = new HashSet<string>();
            var unmatched = new List<string>();

            foreach (var cp in controlPaths)
            {
                if (inferred.TryGetValue(cp, out var assetPath))
                {
                    cpToPath[cp] = assetPath;
                    texturePaths.Add(assetPath);
                }
                else
                {
                    unmatched.Add(cp);
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

        // ─────────────────────── 파일명 → controlPath 추론 규칙 ───────────────────────
        // Kenney 아이콘은 "{디바이스}_{토큰}.png" 형태로 명명된다. 디바이스 접두사를 뗀
        // 토큰을 아래 표로 controlPath에 매핑한다. 폴더에 새 PNG가 추가돼도 토큰이
        // 이미 표에 있으면(또는 규칙에 맞으면) 코드 수정 없이 자동으로 연결된다.
        // 버튼의 의미(A ↔ 크로스 등)는 디바이스 고유 지식이라 표로만 표현 가능하다.
        // 같은 controlPath에 여러 토큰이 매핑될 경우 선언 순서가 우선순위다(먼저 나온 표준 표기 우선).
        private static readonly Dictionary<string, string> GamepadTokenToControlPath = new()
        {
            ["button_a"] = "buttonSouth", ["button_cross"] = "buttonSouth",
            ["button_b"] = "buttonEast", ["button_circle"] = "buttonEast",
            ["button_x"] = "buttonWest", ["button_square"] = "buttonWest",
            ["button_y"] = "buttonNorth", ["button_triangle"] = "buttonNorth",
            ["dpad_up"] = "dpad/up", ["dpad_down"] = "dpad/down",
            ["dpad_left"] = "dpad/left", ["dpad_right"] = "dpad/right",
            ["lb"] = "leftShoulder", ["trigger_l1"] = "leftShoulder",
            ["rb"] = "rightShoulder", ["trigger_r1"] = "rightShoulder",
            ["lt"] = "leftTrigger", ["trigger_l2"] = "leftTrigger",
            ["rt"] = "rightTrigger", ["trigger_r2"] = "rightTrigger",
            ["stick_l_press"] = "leftStickPress", ["button_l3"] = "leftStickPress",
            ["stick_r_press"] = "rightStickPress", ["button_r3"] = "rightStickPress",
            ["stick_r_right"] = "rightStick/right",
            ["stick_r_left"] = "rightStick/left",
        };

        // keyboard_/mouse_ 토큰 중 InputSystem controlPath 이름과 다른 예외만 등록한다.
        // 나머지(space/shift/ctrl/tab/escape/alt/enter/a~z/0~9 등)는 Kenney 토큰이 곧
        // controlPath라 표가 필요 없다 — 새 단일 키 아이콘이 추가돼도 자동 인식된다.
        private static readonly Dictionary<string, string> KeyboardTokenAlias = new()
        {
            ["tilde"] = "backquote",
            ["arrow_up"] = "upArrow",
            ["arrow_down"] = "downArrow",
            ["arrow_left"] = "leftArrow",
            ["arrow_right"] = "rightArrow",
        };

        private static readonly Dictionary<string, string> MouseTokenToControlPath = new()
        {
            ["left"] = "leftButton",
            ["right"] = "rightButton",
            ["middle"] = "middleButton",
            ["scroll"] = "middleButton",
        };

        /// <summary>
        /// 폴더 스캔 결과(파일명(소문자) → 에셋 경로)에서 키보드/마우스 controlPath를 추론한다.
        /// </summary>
        private static Dictionary<string, string> InferKeyboardMouseIcons(Dictionary<string, string> byBasename)
        {
            var result = new Dictionary<string, string>();
            foreach (var kv in byBasename)
            {
                string cp = InferKeyboardMouseControlPath(kv.Key);
                if (cp == null || result.ContainsKey(cp))
                    continue; // 충돌 시 먼저 발견된(사전순) 파일 유지
                result[cp] = kv.Value;
            }
            return result;
        }

        private static string InferKeyboardMouseControlPath(string basenameLower)
        {
            if (basenameLower.StartsWith("mouse_", StringComparison.Ordinal))
            {
                string token = basenameLower.Substring("mouse_".Length);
                return MouseTokenToControlPath.TryGetValue(token, out var cp) ? cp : null;
            }

            if (basenameLower.StartsWith("keyboard_", StringComparison.Ordinal))
            {
                string token = basenameLower.Substring("keyboard_".Length);
                if (KeyboardTokenAlias.TryGetValue(token, out var alias))
                    return alias;
                if (token.Length == 1 && char.IsLetterOrDigit(token[0]))
                    return token;
                if (token.IndexOf('_') < 0)
                    return token; // 단일 토큰(space, shift, ctrl 등)은 Kenney 이름 = controlPath
                return null; // arrows_all, backspace_icon_alternative 등 장식/변형 아이콘은 규칙 밖
            }

            return null;
        }

        /// <summary>
        /// 폴더 스캔 결과에서 게임패드 controlPath를 추론한다. 같은 controlPath에 여러 후보
        /// 파일이 있으면 GamepadTokenToControlPath의 선언 순서를 우선순위로 사용한다.
        /// </summary>
        private static Dictionary<string, string> InferGamepadIcons(Dictionary<string, string> byBasename, bool isPlayStation)
        {
            string prefix = isPlayStation ? "playstation_" : "xbox_";

            var tokenPriority = new Dictionary<string, int>();
            int priorityIndex = 0;
            foreach (var token in GamepadTokenToControlPath.Keys)
                tokenPriority[token] = priorityIndex++;

            var best = new Dictionary<string, (string path, int priority)>();
            foreach (var kv in byBasename)
            {
                if (!kv.Key.StartsWith(prefix, StringComparison.Ordinal))
                    continue;

                string token = kv.Key.Substring(prefix.Length);
                if (!GamepadTokenToControlPath.TryGetValue(token, out var cp))
                    continue;

                int priority = tokenPriority[token];
                if (!best.TryGetValue(cp, out var existing) || priority < existing.priority)
                    best[cp] = (kv.Value, priority);
            }

            var result = new Dictionary<string, string>();
            foreach (var kv in best)
                result[kv.Key] = kv.Value.path;
            return result;
        }
    }

    [CustomEditor(typeof(InputGlyphDataSO))]
    public class InputGlyphDataSOEditor : UnityEditor.Editor
    {
        private static readonly (string field, string label)[] Categories =
        {
            ("_keyboardMouseGlyphs", "키보드 / 마우스"),
            ("_gamepadGlyphs", "게임패드 (제네릭)"),
            ("_xboxGlyphs", "Xbox 오버라이드"),
            ("_playStationGlyphs", "PlayStation 오버라이드"),
            ("_switchGlyphs", "Switch 오버라이드"),
        };

        private bool _overwriteExisting;
        private readonly Dictionary<string, bool> _foldouts = new();

        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(
                "PlayerInputActions 에셋에서 controlPath를 자동 추출하고, InputIcon 폴더의 " +
                "Kenney 스프라이트를 파일명 규칙으로 파싱해 controlPath에 자동 연결합니다. " +
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
            EditorGUILayout.LabelField("controlPath ↔ 아이콘 매핑", EditorStyles.boldLabel);

            serializedObject.Update();
            foreach (var (field, label) in Categories)
                DrawGlyphCategory(field, label);
            serializedObject.ApplyModifiedProperties();
        }

        // controlPath와 실제 연결된 스프라이트 썸네일을 나란히 보여준다.
        // 목록만으로는 어떤 InputAction에 어떤 아이콘이 매핑됐는지 눈으로 확인하기 어려워 추가.
        private void DrawGlyphCategory(string fieldName, string label)
        {
            var listProp = serializedObject.FindProperty(fieldName);
            if (listProp == null || listProp.arraySize == 0)
                return;

            if (!_foldouts.TryGetValue(fieldName, out bool open))
                open = fieldName == "_keyboardMouseGlyphs";

            open = EditorGUILayout.Foldout(open, $"{label} ({listProp.arraySize})", true);
            _foldouts[fieldName] = open;
            if (!open)
                return;

            EditorGUI.indentLevel++;
            for (int i = 0; i < listProp.arraySize; i++)
            {
                var element = listProp.GetArrayElementAtIndex(i);
                var controlPathProp = element.FindPropertyRelative("controlPath");
                var spriteProp = element.FindPropertyRelative("sprite");

                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawSpriteThumbnail(spriteProp.objectReferenceValue as Sprite);
                    EditorGUILayout.LabelField(controlPathProp.stringValue, GUILayout.Width(150));
                    EditorGUILayout.PropertyField(spriteProp, GUIContent.none);
                }
            }
            EditorGUI.indentLevel--;
        }

        private void DrawSpriteThumbnail(Sprite sprite)
        {
            var rect = GUILayoutUtility.GetRect(28, 28, GUILayout.Width(28));
            if (sprite == null)
            {
                EditorGUI.DrawRect(rect, new Color(0f, 0f, 0f, 0.15f));
                return;
            }

            var preview = AssetPreview.GetAssetPreview(sprite);
            if (preview == null)
            {
                if (AssetPreview.IsLoadingAssetPreview(sprite.GetInstanceID()))
                    Repaint(); // 프리뷰가 비동기로 준비되면 다시 그려서 채운다
                preview = AssetPreview.GetMiniThumbnail(sprite);
            }

            if (preview != null)
                GUI.DrawTexture(rect, preview, ScaleMode.ScaleToFit);
        }
    }
}
