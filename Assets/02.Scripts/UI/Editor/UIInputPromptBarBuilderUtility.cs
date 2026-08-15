using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.InputDefine;
using UPlayGround.UI.InputPrompt;

namespace UPlayGround.UI.EditorTools
{
    /// <summary>
    /// UI 프리팹 빌더들이 동일한 장치 반응형 프롬프트 바를 생성하도록 하는 공용 유틸리티.
    /// </summary>
    public static class UIInputPromptBarBuilderUtility
    {
        public const string GlyphDataPath = "Assets/10.Datas/UI/Input/InputGlyphData.asset";

        public readonly struct PromptSpec
        {
            public readonly string MapName;
            public readonly string ActionName;
            public readonly string Label;
            public readonly DevicePromptFilter Filter;

            public PromptSpec(
                string actionName,
                string label,
                DevicePromptFilter filter = DevicePromptFilter.Any,
                string mapName = InputMapNames.UI)
            {
                MapName = mapName;
                ActionName = actionName;
                Label = label;
                Filter = filter;
            }
        }

        public static UIInputPromptBar AddBar(
            Transform parent,
            string name,
            float preferredHeight,
            params PromptSpec[] prompts)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            var go = new GameObject(
                name,
                typeof(RectTransform),
                typeof(LayoutElement),
                typeof(UIInputPromptBar));
            go.transform.SetParent(parent, false);

            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            LayoutElement layout = go.GetComponent<LayoutElement>();
            layout.preferredHeight = preferredHeight;
            layout.flexibleWidth = 1f;

            UIInputPromptBar bar = go.GetComponent<UIInputPromptBar>();
            ConfigureBar(bar, preferredHeight, prompts);
            return bar;
        }

        public static UIInputPromptBar FindOrAddBar(
            Transform parent,
            string name,
            float preferredHeight,
            params PromptSpec[] prompts)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            Transform existing = parent.Find(name);
            UIInputPromptBar bar = existing != null
                ? existing.GetComponent<UIInputPromptBar>()
                : null;
            if (bar == null)
                return AddBar(parent, name, preferredHeight, prompts);

            ConfigureBar(bar, preferredHeight, prompts);
            return bar;
        }

        public static void ConfigureBar(
            UIInputPromptBar bar,
            float preferredHeight,
            params PromptSpec[] prompts)
        {
            if (bar == null)
                throw new ArgumentNullException(nameof(bar));

            LayoutElement layout = bar.GetComponent<LayoutElement>();
            if (layout == null)
                layout = bar.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = preferredHeight;
            layout.flexibleWidth = 1f;

            var serialized = new SerializedObject(bar);
            serialized.FindProperty("_glyphData").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<InputGlyphDataSO>(GlyphDataPath);

            SerializedProperty entries = serialized.FindProperty("_entries");
            entries.arraySize = prompts?.Length ?? 0;
            for (int i = 0; i < entries.arraySize; i++)
            {
                PromptSpec prompt = prompts[i];
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("mapName").stringValue = prompt.MapName;
                entry.FindPropertyRelative("actionName").stringValue = prompt.ActionName;
                entry.FindPropertyRelative("label").stringValue = prompt.Label;
                entry.FindPropertyRelative("deviceFilter").enumValueIndex = (int)prompt.Filter;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(bar);
        }

        public static UIInputPromptBar AddMainAndSubNavigationBar(
            Transform parent,
            string subPreviousLabel,
            string subNextLabel)
        {
            return AddBar(
                parent,
                "NavigationPromptBar",
                38f,
                new PromptSpec(UIAction.MainTabPrevious, "이전 메뉴"),
                new PromptSpec(UIAction.MainTabNext, "다음 메뉴"),
                new PromptSpec(UIAction.SubTabPrevious, subPreviousLabel),
                new PromptSpec(UIAction.SubTabNext, subNextLabel),
                new PromptSpec(UIAction.Submit, "확인"),
                new PromptSpec(UIAction.Cancel, "뒤로"));
        }

        public static UIInputPromptBar AddMainNavigationBar(Transform parent)
        {
            return AddBar(
                parent,
                "MainNavigationPromptBar",
                38f,
                new PromptSpec(UIAction.MainTabPrevious, "이전 메뉴"),
                new PromptSpec(UIAction.MainTabNext, "다음 메뉴"),
                new PromptSpec(UIAction.Submit, "확인"),
                new PromptSpec(UIAction.Cancel, "뒤로"));
        }

        public static UIInputPromptBar AddSubmitCancelBar(
            Transform parent,
            string submitLabel = "확인",
            string cancelLabel = "뒤로")
        {
            return AddBar(
                parent,
                "CommonPromptBar",
                42f,
                new PromptSpec(UIAction.Submit, submitLabel),
                new PromptSpec(UIAction.Cancel, cancelLabel));
        }
    }
}
