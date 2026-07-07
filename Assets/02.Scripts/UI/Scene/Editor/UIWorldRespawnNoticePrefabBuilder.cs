using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Manager;

namespace UPlayGround.UI.World.EditorTools
{
    /// <summary>
    /// 몬스터 재스폰 안내 UI(UI_WorldRespawnNotice) 프리팹을 코드로 생성/재구성하는 에디터 툴.
    ///
    /// - 프리팹이 없으면 새로 만들고, 있으면 루트/스크립트(guid)는 유지한 채 자식 계층만 재구성한다.
    /// - 얕은 전체 화면 암전 + 상단 안내 문구 구성. 재실행 가능(idempotent).
    /// - 입력 차단/일시정지 목적이 아니므로 Button·입력 레이어 상승 요소는 넣지 않는다.
    /// - 빌드 후 UIPrefabDatabase에 Key "WorldRespawnNotice"로 수동 등록해야 한다.
    /// </summary>
    public static class UIWorldRespawnNoticePrefabBuilder
    {
        private const string PrefabDir = "Assets/03.Prefabs/UI/Scene/World";
        private const string PrefabPath = PrefabDir + "/UI_WorldRespawnNotice.prefab";

        private static readonly Color Dim      = new Color(0f, 0f, 0f, 0.28f);
        private static readonly Color TextMain = new Color(0.92f, 0.90f, 0.82f, 1f);

        [MenuItem("UPlayGround/UI/월드 재스폰 안내 프리팹 빌드")]
        public static void Build()
        {
            EnsureFolder(PrefabDir);

            GameObject root;
            bool isNew = !File.Exists(PrefabPath);

            if (isNew)
            {
                root = new GameObject("UI_WorldRespawnNotice",
                    typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
                root.AddComponent<UI_WorldRespawnNotice>();
            }
            else
            {
                root = PrefabUtility.LoadPrefabContents(PrefabPath);
            }

            try
            {
                var notice = root.GetComponent<UI_WorldRespawnNotice>();
                if (notice == null)
                {
                    Debug.LogError("[WorldRespawnNoticeBuilder] 루트에 UI_WorldRespawnNotice 컴포넌트가 없습니다. 중단.");
                    return;
                }

                ClearChildren(root.transform);

                // 얕은 전체 화면 암전
                var dim = NewUI("Dim", root.transform);
                Stretch(dim);
                var dimImage = dim.AddComponent<Image>();
                dimImage.color = Dim;
                dimImage.raycastTarget = false;

                // 상단 안내 문구
                var message = NewUI("MessageText", root.transform);
                var messageRt = message.GetComponent<RectTransform>();
                messageRt.anchorMin = new Vector2(0.5f, 1f);
                messageRt.anchorMax = new Vector2(0.5f, 1f);
                messageRt.pivot = new Vector2(0.5f, 1f);
                messageRt.anchoredPosition = new Vector2(0f, -160f);
                messageRt.sizeDelta = new Vector2(1400f, 72f);

                var messageText = message.AddComponent<TextMeshProUGUI>();
                messageText.text = "쓰러졌던 마물이 다시 움직이기 시작했습니다.";
                messageText.fontSize = 34;
                messageText.color = TextMain;
                messageText.alignment = TextAlignmentOptions.Center;
                messageText.raycastTarget = false;

                // 문구 가독성용 밑줄 장식
                var underline = NewUI("Underline", message.transform);
                var underlineRt = underline.GetComponent<RectTransform>();
                underlineRt.anchorMin = new Vector2(0.5f, 0f);
                underlineRt.anchorMax = new Vector2(0.5f, 0f);
                underlineRt.pivot = new Vector2(0.5f, 1f);
                underlineRt.anchoredPosition = new Vector2(0f, -6f);
                underlineRt.sizeDelta = new Vector2(520f, 2f);
                var underlineImage = underline.AddComponent<Image>();
                underlineImage.color = new Color(TextMain.r, TextMain.g, TextMain.b, 0.45f);
                underlineImage.raycastTarget = false;

                // 초기 상태: 투명 (연출은 ShowNotice가 시작)
                var canvasGroup = root.GetComponent<CanvasGroup>();
                if (canvasGroup == null) canvasGroup = root.AddComponent<CanvasGroup>();
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;

                // ── 필드 연결 ──
                var so = new SerializedObject(notice);
                SetRef(so, "_messageText", messageText);
                SetEnum(so, "_layer", (int)CanvasLayer.HUD);
                SetBool(so, "_canCloseWithEsc", false);
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);

                Debug.Log($"[WorldRespawnNoticeBuilder] UI_WorldRespawnNotice 프리팹 생성 완료: {PrefabPath}\n" +
                          "UIPrefabDatabase에 Key 'WorldRespawnNotice' / Default Layer 'HUD'로 등록하세요.");
            }
            finally
            {
                if (isNew)
                    UnityEngine.Object.DestroyImmediate(root);
                else
                    PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        }

        // ──────────────────────────────────────────────────────────
        #region 헬퍼

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(leaf)) return;

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        private static void ClearChildren(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(root.GetChild(i).gameObject);
        }

        private static GameObject NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void Stretch(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void SetRef(SerializedObject so, string propertyName, UnityEngine.Object value)
        {
            var property = so.FindProperty(propertyName);
            if (property != null) property.objectReferenceValue = value;
            else Debug.LogWarning($"[WorldRespawnNoticeBuilder] 직렬화 필드 '{propertyName}'을 찾지 못했습니다.");
        }

        private static void SetEnum(SerializedObject so, string propertyName, int value)
        {
            var property = so.FindProperty(propertyName);
            if (property != null) property.intValue = value;
        }

        private static void SetBool(SerializedObject so, string propertyName, bool value)
        {
            var property = so.FindProperty(propertyName);
            if (property != null) property.boolValue = value;
        }

        #endregion
    }
}
