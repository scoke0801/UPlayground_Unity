using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UPlayGround.UI.Editor
{
    /// <summary>기존 설정 화면의 스위치 스타일로 카메라 보조 옵션을 증분 적용한다.</summary>
    public static class UICameraAssistSetupTool
    {
        private const string PrefabPath = "Assets/03.Prefabs/UI/Scene/UI_Scene_SettingMenu.prefab";

        /// <summary>기존 컨트롤 순서와 참조를 보존하면서 독립 카메라 옵션 두 개를 연결한다.</summary>
        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/UI/설정/카메라 보조 옵션 적용")]
        public static void Apply()
        {
            if (EditorApplication.isCompiling || EditorUtility.scriptCompilationFailed)
                throw new InvalidOperationException("컴파일 오류를 해결한 뒤 설정 화면을 갱신해야 합니다.");
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                UISettingPageGamePlay page = root.GetComponentInChildren<UISettingPageGamePlay>(true);
                if (page == null)
                    throw new InvalidOperationException("게임플레이 설정 페이지를 찾지 못했습니다.");
                Transform content = page.transform.Find("Viewport/ScrollContent") ?? page.transform;
                Transform source = content.Find("Row_전투 진동");
                if (source == null)
                    throw new InvalidOperationException("복제할 전투 진동 행을 찾지 못했습니다.");
                Transform legacy = content.Find("Row_타겟 보정");
                if (legacy != null)
                {
                    Transform duplicate = content.Find("Row_공격 시 카메라 보조");
                    if (duplicate != null)
                        UnityEngine.Object.DestroyImmediate(duplicate.gameObject);
                    legacy.name = "Row_공격 시 카메라 보조";
                }
                UISwitchButton hit = EnsureRow(content, source, "공격 시 카메라 보조", source.GetSiblingIndex() - 1);
                UISwitchButton movement = EnsureRow(content, source, "이동 시 카메라 정렬", source.GetSiblingIndex() + 1);
                var serialized = new SerializedObject(page);
                serialized.FindProperty("_hitCameraAssist").objectReferenceValue = hit;
                serialized.FindProperty("_movementCameraRecentering").objectReferenceValue = movement;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EnsureScrollablePage(page);
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(page.gameObject) != 0)
                    throw new InvalidOperationException("설정 페이지에 누락된 스크립트가 있습니다.");
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("[카메라 보조] 공격 보조·이동 정렬 옵션을 설정 화면에 연결했습니다.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>늘어난 옵션이 하단 버튼을 가리지 않도록 기존 레이아웃을 스크롤 안으로 옮긴다.</summary>
        public static void EnsureScrollablePage(UISettingPageGamePlay page)
        {
            if (page.GetComponent<ScrollRect>() != null)
                return;
            VerticalLayoutGroup layout = page.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
                throw new InvalidOperationException("설정 페이지의 세로 레이아웃이 없습니다.");
            var children = new Transform[page.transform.childCount];
            for (int i = 0; i < children.Length; i++)
                children[i] = page.transform.GetChild(i);
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(page.transform, false);
            var viewportRect = (RectTransform)viewport.transform;
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = viewportRect.offsetMax = Vector2.zero;
            var content = new GameObject("ScrollContent", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var contentRect = (RectTransform)content.transform;
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = Vector2.one;
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = Vector2.zero;
            contentRect.anchoredPosition = Vector2.zero;
            EditorUtility.CopySerialized(layout, content.GetComponent<VerticalLayoutGroup>());
            UnityEngine.Object.DestroyImmediate(layout);
            foreach (Transform child in children)
                child.SetParent(content.transform, false);
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            ScrollRect scroll = page.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewportRect;
            scroll.content = contentRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;
        }

        private static UISwitchButton EnsureRow(Transform parent, Transform source, string label, int index)
        {
            string rowName = "Row_" + label;
            Transform row = parent.Find(rowName);
            if (row == null)
            {
                row = UnityEngine.Object.Instantiate(source.gameObject, parent).transform;
                row.name = rowName;
            }
            row.SetSiblingIndex(index);
            TMP_Text text = row.Find("Label")?.GetComponent<TMP_Text>();
            UISwitchButton control = row.GetComponentInChildren<UISwitchButton>(true);
            if (text == null || control == null)
                throw new InvalidOperationException("카메라 옵션 행의 라벨 또는 스위치가 없습니다.");
            text.text = label;
            return control;
        }
    }
}
