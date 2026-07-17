using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UPlayGround.InputDefine;
using UPlayGround.Manager;

namespace UPlayGround.UI.InputPrompt
{
    /// <summary>
    /// 글리프 1개 단위. 스프라이트가 있으면 그것을, 없으면 폴백 텍스트(원문 표시 문자열)를 쓴다.
    /// </summary>
    public readonly struct GlyphPart
    {
        public readonly Sprite Sprite;
        public readonly string Text;

        private GlyphPart(Sprite sprite, string text)
        {
            Sprite = sprite;
            Text = text;
        }

        public bool HasSprite => Sprite != null;

        public static GlyphPart Of(Sprite sprite, string text) => new(sprite, text);
        public static GlyphPart TextOnly(string text) => new(null, text);
    }

    /// <summary>
    /// 글리프 해석 결과. 단일 바인딩은 파트 1개, 복합 바인딩(예: Dodge = L1+R1)은 파트 N개를 담는다.
    /// </summary>
    public readonly struct InputGlyphResult
    {
        public readonly bool IsValid;                 // 액션/바인딩을 찾았는가
        public readonly IReadOnlyList<GlyphPart> Parts; // 항상 1개 이상

        private InputGlyphResult(bool isValid, IReadOnlyList<GlyphPart> parts)
        {
            IsValid = isValid;
            Parts = parts;
        }

        public int Count => Parts?.Count ?? 0;
        public GlyphPart Primary => Count > 0 ? Parts[0] : default;

        public static InputGlyphResult Of(IReadOnlyList<GlyphPart> parts) => new(true, parts);
        public static InputGlyphResult Missing(string actionName) =>
            new(false, new[] { GlyphPart.TextOnly(actionName) });
    }

    /// <summary>
    /// (맵, 액션) + 활성 디바이스(+게임패드 브랜드) → 표시할 글리프를 해석한다.
    ///
    /// 컨트롤 스킴 그룹에 의존하지 않는다. 액션의 바인딩을 순회하며 디바이스 레이아웃이
    /// 활성 디바이스와 맞는 바인딩을 골라, 표준 API GetBindingDisplayString으로 controlPath를 얻는다.
    /// 복합 바인딩(OneModifier/2DVector)은 파트별 글리프 리스트로 반환한다.
    /// → 자산(.inputactions) 무수정으로 동작하며, 리바인딩 결과도 자동 반영된다.
    /// </summary>
    public static class InputGlyphResolver
    {
        public static InputGlyphResult Resolve(string mapName, string actionName,
            ActiveInputDevice device, GamepadBrand brand, InputGlyphDataSO glyphData)
        {
            IInputService inputManager = Svc.Input;
            if (inputManager == null)
                return InputGlyphResult.Missing(actionName);

            InputAction action = inputManager.GetAction(mapName, actionName);
            return ResolveAction(action, device, brand, glyphData, actionName);
        }

        /// <summary>제네릭 브랜드 단축 오버로드.</summary>
        public static InputGlyphResult Resolve(string mapName, string actionName,
            ActiveInputDevice device, InputGlyphDataSO glyphData)
            => Resolve(mapName, actionName, device, GamepadBrand.Generic, glyphData);

        /// <summary>
        /// IInputService(런타임 씬) 없이 <see cref="InputAction"/>을 직접 받아 해석한다.
        /// 에디터 미리보기 등 액션을 외부에서 확보한 경우에 쓴다. 바인딩 메타데이터만 읽으므로
        /// 액션이 Enable 상태가 아니어도 동작한다.
        /// </summary>
        /// <param name="fallbackName">액션/바인딩을 못 찾았을 때 폴백 텍스트로 쓸 이름.</param>
        public static InputGlyphResult ResolveAction(InputAction action,
            ActiveInputDevice device, GamepadBrand brand, InputGlyphDataSO glyphData,
            string fallbackName = null)
        {
            if (action == null)
                return InputGlyphResult.Missing(fallbackName ?? "?");

            int bindingIndex = FindBindingIndexForDevice(action, device);
            if (bindingIndex < 0)
                return InputGlyphResult.Missing(fallbackName ?? action.name);

            return BuildResult(action, bindingIndex, device, brand, glyphData);
        }

        private static InputGlyphResult BuildResult(InputAction action, int bindingIndex,
            ActiveInputDevice device, GamepadBrand brand, InputGlyphDataSO glyphData)
        {
            var bindings = action.bindings;
            var parts = new List<GlyphPart>();

            if (bindings[bindingIndex].isComposite)
            {
                // 복합 바인딩: 뒤따르는 파트 바인딩들을 순서대로(모디파이어 → 바인딩) 글리프화.
                for (int i = bindingIndex + 1; i < bindings.Count && bindings[i].isPartOfComposite; i++)
                    parts.Add(MakePart(action, i, device, brand, glyphData));
            }
            else
            {
                parts.Add(MakePart(action, bindingIndex, device, brand, glyphData));
            }

            return parts.Count > 0 ? InputGlyphResult.Of(parts) : InputGlyphResult.Missing(action.name);
        }

        private static GlyphPart MakePart(InputAction action, int bindingIndex,
            ActiveInputDevice device, GamepadBrand brand, InputGlyphDataSO glyphData)
        {
            // 표시 텍스트는 디바이스별 명칭(예: PS의 "Share")을 반영하도록 표준 API를 쓴다.
            string display = action.GetBindingDisplayString(bindingIndex,
                out string _ /*deviceLayout*/, out string _ /*controlPath*/);

            // 조회 키는 GetBindingDisplayString의 out controlPath가 아니라 바인딩 effectivePath에서 뽑는다.
            // out controlPath는 연결된 실제 디바이스에 해석되어 select 같은 버튼은 생성기 키("select")와 어긋날 수 있다.
            // 글리프 데이터 생성기(InputGlyphDataGenerator)와 동일하게 effectivePath 세그먼트를 쓰면 생성·조회 키가 항상 일치한다.
            string lookupKey = ToControlPathSegment(action.bindings[bindingIndex].effectivePath);

            if (glyphData != null && glyphData.TryResolve(device, brand, lookupKey, out Sprite sprite))
                return GlyphPart.Of(sprite, display);

            // 매핑되지 않은 키는 회색 박스가 아니라 원문 텍스트로 노출해 누락을 가시화.
            return GlyphPart.TextOnly(display);
        }

        // "<Keyboard>/1" → "1", "<Gamepad>/dpad/up" → "dpad/up", "<Gamepad>/select" → "select"
        // InputGlyphDataGenerator.ToControlPathSegment와 동일 규칙(디바이스 prefix 제거). 키 일관성을 위해 한 규칙을 양쪽에서 쓴다.
        private static string ToControlPathSegment(string effectivePath)
        {
            if (string.IsNullOrEmpty(effectivePath))
                return string.Empty;
            int i = effectivePath.IndexOf(">/", StringComparison.Ordinal);
            return i >= 0 ? effectivePath.Substring(i + 2) : effectivePath;
        }

        // 활성 디바이스에 맞는 바인딩 인덱스(단순 또는 복합). 복합은 첫 파트의 디바이스로 판정한다.
        private static int FindBindingIndexForDevice(InputAction action, ActiveInputDevice device)
        {
            var bindings = action.bindings;
            for (int i = 0; i < bindings.Count; i++)
            {
                var b = bindings[i];
                if (b.isPartOfComposite)
                    continue; // 파트는 부모(복합)를 통해 처리

                if (b.isComposite)
                {
                    if (CompositeMatchesDevice(bindings, i, device))
                        return i;
                    continue;
                }

                string path = b.effectivePath;
                if (string.IsNullOrEmpty(path))
                    continue;

                if (MatchesDevice(path, device))
                    return i;
            }
            return -1;
        }

        // 복합 바인딩의 디바이스 판정: 첫 번째 파트의 effectivePath 디바이스로 결정.
        private static bool CompositeMatchesDevice(IReadOnlyList<InputBinding> bindings,
            int compositeIndex, ActiveInputDevice device)
        {
            for (int i = compositeIndex + 1; i < bindings.Count && bindings[i].isPartOfComposite; i++)
            {
                string path = bindings[i].effectivePath;
                if (!string.IsNullOrEmpty(path))
                    return MatchesDevice(path, device);
            }
            return false;
        }

        // effectivePath 예: "<Keyboard>/1", "<Mouse>/leftButton", "<Gamepad>/buttonWest"
        private static bool MatchesDevice(string effectivePath, ActiveInputDevice device)
        {
            bool isGamepad =
                effectivePath.StartsWith("<Gamepad>", StringComparison.Ordinal) ||
                effectivePath.StartsWith("<DualShockGamepad>", StringComparison.Ordinal) ||
                effectivePath.StartsWith("<XInputController>", StringComparison.Ordinal);

            return device == ActiveInputDevice.Gamepad ? isGamepad : !isGamepad;
        }
    }
}
