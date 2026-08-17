using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UPlayGround.InputDefine;
using UPlayGround.Manager;
using UPlayGround.UI.InputPrompt;

namespace UPlayGround.UI.EditorTools
{
    /// <summary>
    /// UI 입력 계약(필수 액션·장치별 바인딩·물리 경로 중복·글리프 데이터)을 검증한다.
    /// 프리팹 구조는 다루지 않고 입력 에셋과 글리프 리소스만 대상으로 한다.
    /// </summary>
    public static class UIInputContractValidator
    {
        public const string GlyphDataPath = "Assets/10.Datas/UI/Input/InputGlyphData.asset";

        private const string InputActionsPath =
            "Assets/Resources/Input/PlayerInputActions.inputactions";

        [MenuItem("Tools/UI/Input Prompt/입력 계약 검증")]
        public static void ValidateAllMenu()
        {
            ValidateAll(logResult: true);
        }

        public static UIInputContractReport ValidateAll(bool logResult = false)
        {
            var report = new UIInputContractReport();
            ValidateInputActionAsset(report);
            ValidateGlyphData(report);

            if (logResult)
            {
                if (report.IsValid)
                    Debug.Log($"[InputContract] 검증 성공\n{report}");
                else
                    Debug.LogError($"[InputContract] 검증 실패\n{report}");
            }

            return report;
        }

        private static void ValidateInputActionAsset(UIInputContractReport report)
        {
            InputActionAsset asset =
                AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (asset == null)
            {
                report.AddError($"InputActionAsset 누락: {InputActionsPath}");
                return;
            }

            InputActionMap ui = asset.FindActionMap(InputMapNames.UI, false);
            if (ui == null)
            {
                report.AddError($"액션 맵 누락: {InputMapNames.UI}");
                return;
            }

            var required = new[]
            {
                (UIAction.Navigate, true, true),
                (UIAction.Submit, true, true),
                (UIAction.Cancel, true, true),
                (UIAction.MainTabPrevious, false, true),
                (UIAction.MainTabNext, false, true),
                (UIAction.SubTabPrevious, false, true),
                (UIAction.SubTabNext, false, true),
            };

            foreach ((string actionName, bool keyboardMouse, bool gamepad) in required)
            {
                InputAction action = ui.FindAction(actionName, false);
                if (action == null)
                {
                    report.AddError($"UI 액션 누락: {actionName}");
                    continue;
                }

                if (keyboardMouse
                    && !InputPromptAvailability.HasBindingFor(
                        action,
                        ActiveInputDevice.KeyboardMouse))
                {
                    report.AddError($"키보드·마우스 바인딩 누락: UI/{actionName}");
                }

                if (gamepad
                    && !InputPromptAvailability.HasBindingFor(
                        action,
                        ActiveInputDevice.Gamepad))
                {
                    report.AddError($"게임패드 바인딩 누락: UI/{actionName}");
                }
            }

            foreach (InputActionMap map in asset.actionMaps)
            {
                foreach (InputAction action in map.actions)
                {
                    foreach (InputBinding binding in action.bindings)
                    {
                        if (binding.id == Guid.Empty)
                            report.AddError($"binding GUID 누락: {map.name}/{action.name}");
                        if (string.IsNullOrWhiteSpace(binding.path))
                            report.AddError($"빈 binding path: {map.name}/{action.name}/{binding.id}");
                    }
                }
            }

            ValidatePhysicalPathCollisions(asset, report);

            string[] requiredSchemes = { "Keyboard&Mouse", "Gamepad" };
            foreach (string scheme in requiredSchemes)
            {
                if (!asset.controlSchemes.Any(value =>
                        string.Equals(value.name, scheme, StringComparison.Ordinal)))
                {
                    report.AddError($"Control Scheme 누락: {scheme}");
                }
            }
        }

        private static readonly Dictionary<string, HashSet<string>>
            AllowedPhysicalPathSharing = BuildAllowedPhysicalPathSharing();

        private static Dictionary<string, HashSet<string>> BuildAllowedPhysicalPathSharing()
        {
            var result = new Dictionary<string, HashSet<string>>(
                StringComparer.OrdinalIgnoreCase);

            void Add(string map, string path, params string[] actions)
            {
                result[$"{map}|{path}"] =
                    new HashSet<string>(actions, StringComparer.Ordinal);
            }

            Add(InputMapNames.PlayerAction, "<Gamepad>/leftShoulder",
                PlayerAction.Dodge,
                PlayerAction.SkillUltimate,
                PlayerAction.Guard,
                PlayerAction.QuickSlot_Left,
                PlayerAction.QuickSlot_Down,
                PlayerAction.QuickSlot_Right,
                PlayerAction.QuickSlot_Up);
            Add(InputMapNames.PlayerAction, "<Gamepad>/rightShoulder",
                PlayerAction.Dodge,
                PlayerAction.ElementBuff);
            Add(InputMapNames.PlayerAction, "<Gamepad>/rightTrigger",
                PlayerAction.Interact,
                PlayerAction.SkillUltimate);
            Add(InputMapNames.PlayerAction, "<Gamepad>/dpad/up",
                PlayerAction.CharacterSwap_1,
                PlayerAction.QuickSlot_Up);
            Add(InputMapNames.PlayerAction, "<Gamepad>/dpad/right",
                PlayerAction.CharacterSwap_2,
                PlayerAction.QuickSlot_Right);
            Add(InputMapNames.PlayerAction, "<Gamepad>/dpad/down",
                PlayerAction.CharacterSwap_3,
                PlayerAction.QuickSlot_Down);
            Add(InputMapNames.PlayerAction, "<Gamepad>/dpad/left",
                PlayerAction.CharacterSwap_4,
                PlayerAction.QuickSlot_Left);

            Add(InputMapNames.UI, "<Gamepad>/leftStick",
                UIAction.Navigate,
                "CursorMove");
            Add(InputMapNames.UI, "<Gamepad>/buttonSouth",
                UIAction.Submit,
                "CursorClick");
            Add(InputMapNames.UI, "<Keyboard>/space",
                UIAction.Submit,
                UIAction.DialogueNext);
            Add(InputMapNames.UI, "<Gamepad>/rightTrigger",
                UIAction.DialogueSkip,
                UIAction.MainTabNext);
            Add(InputMapNames.UI, "<Gamepad>/leftShoulder",
                UIAction.DialogueBacklog,
                UIAction.SubTabPrevious);

            return result;
        }

        private static void ValidatePhysicalPathCollisions(
            InputActionAsset asset,
            UIInputContractReport report)
        {
            foreach (InputActionMap map in asset.actionMaps)
            {
                var owners = new Dictionary<string, HashSet<string>>(
                    StringComparer.OrdinalIgnoreCase);
                foreach (InputAction action in map.actions)
                {
                    foreach (InputBinding binding in action.bindings)
                    {
                        if (binding.isComposite || string.IsNullOrWhiteSpace(binding.path))
                            continue;

                        string path = binding.path.Trim();
                        if (!owners.TryGetValue(path, out HashSet<string> actions))
                        {
                            actions = new HashSet<string>(StringComparer.Ordinal);
                            owners.Add(path, actions);
                        }
                        actions.Add(action.name);
                    }
                }

                foreach ((string path, HashSet<string> actions) in owners)
                {
                    if (actions.Count <= 1)
                        continue;

                    string key = $"{map.name}|{path}";
                    if (AllowedPhysicalPathSharing.TryGetValue(
                            key,
                            out HashSet<string> allowed)
                        && allowed.SetEquals(actions))
                    {
                        continue;
                    }

                    report.AddError(
                        $"허용되지 않은 물리 경로 중복: {map.name}/{path} = " +
                        string.Join(", ", actions.OrderBy(value => value)));
                }
            }
        }

        private static void ValidateGlyphData(UIInputContractReport report)
        {
            InputGlyphDataSO data =
                AssetDatabase.LoadAssetAtPath<InputGlyphDataSO>(GlyphDataPath);
            if (data == null)
            {
                report.AddError($"글리프 데이터 누락: {GlyphDataPath}");
                return;
            }

            string[] keyboardPaths = { "escape", "enter", "space" };
            string[] gamepadPaths =
            {
                "leftTrigger",
                "rightTrigger",
                "leftShoulder",
                "rightShoulder",
                "buttonSouth",
                "buttonEast",
            };

            foreach (string path in keyboardPaths)
            {
                if (!data.TryResolve(
                        ActiveInputDevice.KeyboardMouse,
                        GamepadBrand.Generic,
                        path,
                        out Sprite sprite)
                    || sprite == null)
                {
                    report.AddError($"키보드 글리프 누락: {path}");
                }
            }

            foreach (string path in gamepadPaths)
            {
                foreach (GamepadBrand brand in new[]
                         {
                             GamepadBrand.Generic,
                             GamepadBrand.Xbox,
                             GamepadBrand.PlayStation,
                             GamepadBrand.Switch,
                         })
                {
                    if (!data.TryResolve(
                            ActiveInputDevice.Gamepad,
                            brand,
                            path,
                            out Sprite sprite)
                        || sprite == null)
                    {
                        report.AddError($"게임패드 글리프 누락: {brand}/{path}");
                    }
                }
            }

            var serialized = new SerializedObject(data);
            string[] facePaths =
            {
                "buttonSouth",
                "buttonEast",
                "buttonWest",
                "buttonNorth",
            };
            ValidateDirectBrandGlyphs(
                serialized.FindProperty("_xboxGlyphs"),
                "Xbox",
                facePaths,
                report);
            ValidateDirectBrandGlyphs(
                serialized.FindProperty("_playStationGlyphs"),
                "PlayStation",
                facePaths,
                report);
            ValidateDirectBrandGlyphs(
                serialized.FindProperty("_switchGlyphs"),
                "Switch",
                facePaths,
                report);
        }

        private static void ValidateDirectBrandGlyphs(
            SerializedProperty entries,
            string brand,
            IReadOnlyList<string> requiredPaths,
            UIInputContractReport report)
        {
            var mapped = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < entries.arraySize; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                string path = entry.FindPropertyRelative("controlPath").stringValue;
                UnityEngine.Object sprite =
                    entry.FindPropertyRelative("sprite").objectReferenceValue;
                if (!string.IsNullOrWhiteSpace(path) && sprite != null)
                    mapped.Add(path);
            }

            foreach (string path in requiredPaths)
            {
                if (!mapped.Contains(path))
                    report.AddError($"브랜드 전용 얼굴 버튼 글리프 누락: {brand}/{path}");
            }
        }
    }

    /// <summary>UI 입력 계약 검증 결과를 모아 보고한다.</summary>
    public sealed class UIInputContractReport
    {
        private readonly List<string> _errors = new();

        public IReadOnlyList<string> Errors => _errors;
        public bool IsValid => _errors.Count == 0;

        internal void AddError(string message) => _errors.Add(message);

        public override string ToString()
        {
            var lines = new List<string> { $"valid={IsValid}", $"errors={_errors.Count}" };
            lines.AddRange(_errors.Select(error => $"ERROR: {error}"));
            return string.Join(Environment.NewLine, lines);
        }
    }
}
