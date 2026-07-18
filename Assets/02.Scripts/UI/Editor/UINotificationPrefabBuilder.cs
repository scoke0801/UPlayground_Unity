using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.Path;
using UPlayGround.Manager;

namespace UPlayGround.UI.HUD.Notification.EditorTools
{
    public static class UINotificationPrefabBuilder
    {
        private const string MainPrefabPath = "Assets/03.Prefabs/UI/HUD/Notification/UI_Notification.prefab";
        private const string EntryPrefabPath = "Assets/03.Prefabs/UI/HUD/Notification/UI_NotificationEntry.prefab";
        private const string DatabasePath = "Assets/10.Datas/Path/UIPrefabDatabase.asset";
        private const string NotificationKey = "Notification";

        private static readonly Color EntryBg = new Color(0.06f, 0.07f, 0.09f, 0.88f);
        private static readonly Color IconBg = new Color(0.13f, 0.16f, 0.20f, 0.95f);
        private static readonly Color TextMain = new Color(0.94f, 0.96f, 0.98f, 1f);
        private static readonly Color TextSub = new Color(0.72f, 0.76f, 0.82f, 1f);
        private static readonly Color Accent = new Color(0.35f, 0.75f, 0.95f, 1f);

        private static Sprite UISprite => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        [MenuItem("UPlayGround/UI/프리팹 빌드/Notification")]
        public static void Build()
        {
            EnsureDirectory(Path.GetDirectoryName(MainPrefabPath));

            var entryPrefab = BuildEntryPrefab();
            var root = BuildMainPrefab(entryPrefab);
            RegisterDatabase(root);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = root.gameObject;
            Debug.Log("[NotificationBuilder] UI_Notification 프리팹 생성 및 DB 등록 완료.");
        }

        private static UI_NotificationEntry BuildEntryPrefab()
        {
            var go = NewUI("UI_NotificationEntry", null);
            var entry = go.AddComponent<UI_NotificationEntry>();
            var group = go.AddComponent<CanvasGroup>();
            AddImage(go, EntryBg, UISprite, true);
            SetHeight(go, 92);

            var hlg = AddHLG(go, 12, 0);
            hlg.padding = new RectOffset(0, 14, 10, 10);
            hlg.childAlignment = TextAnchor.MiddleCenter;

            var accent = NewUI("Accent", go.transform);
            SetWidth(accent, 6);
            var accentImage = AddImage(accent, Accent, UISprite, true);

            var iconFrame = NewUI("IconFrame", go.transform);
            SetWidth(iconFrame, 56);
            AddImage(iconFrame, IconBg, UISprite, true);
            var iconGo = NewUI("Icon", iconFrame.transform);
            InsetStretch(Rt(iconGo), 8);
            var icon = AddImage(iconGo, Color.white);
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            var textCol = NewUI("Text", go.transform);
            AddFlexibleW(textCol, 1f);
            AddVLG(textCol, 2, 0).childAlignment = TextAnchor.MiddleLeft;
            var title = AddText(NewUI("Title", textCol.transform), "알림", 20, TextMain, TextAlignmentOptions.Left);
            SetHeight(title.gameObject, 28);
            var message = AddText(NewUI("Message", textCol.transform), "시스템 메시지", 17, TextSub, TextAlignmentOptions.Left);
            SetHeight(message.gameObject, 28);

            var so = new SerializedObject(entry);
            SetRef(so, "_canvasGroup", group);
            SetRef(so, "_accentImage", accentImage);
            SetRef(so, "_iconImage", icon);
            SetRef(so, "_titleText", title);
            SetRef(so, "_messageText", message);
            so.ApplyModifiedPropertiesWithoutUndo();

            var asset = PrefabUtility.SaveAsPrefabAsset(go, EntryPrefabPath);
            UnityEngine.Object.DestroyImmediate(go);
            return asset.GetComponent<UI_NotificationEntry>();
        }

        private static UI_Notification BuildMainPrefab(UI_NotificationEntry entryPrefab)
        {
            var go = NewUI("UI_Notification", null);
            var canvas = go.AddComponent<Canvas>();
            canvas.overrideSorting = false;
            go.AddComponent<CanvasGroup>();
            go.AddComponent<GraphicRaycaster>();
            var notification = go.AddComponent<UI_Notification>();
            Stretch(go);

            var content = NewUI("Content", go.transform);
            var contentRt = Rt(content);
            contentRt.anchorMin = new Vector2(0.5f, 1f);
            contentRt.anchorMax = new Vector2(0.5f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.sizeDelta = new Vector2(840, 0);
            contentRt.anchoredPosition = new Vector2(0, -112);
            var layout = AddVLG(content, 8, 0);
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var so = new SerializedObject(notification);
            SetRef(so, "_entryPrefab", entryPrefab);
            SetRef(so, "_content", content.transform);
            SetInt(so, "_maxVisibleEntries", 4);
            so.ApplyModifiedPropertiesWithoutUndo();

            var asset = PrefabUtility.SaveAsPrefabAsset(go, MainPrefabPath);
            UnityEngine.Object.DestroyImmediate(go);
            return asset.GetComponent<UI_Notification>();
        }

        private static void RegisterDatabase(UI_Notification notification)
        {
            var db = AssetDatabase.LoadAssetAtPath<UIPrefabDatabase>(DatabasePath);
            if (db == null || notification == null)
            {
                Debug.LogWarning("[NotificationBuilder] UIPrefabDatabase를 찾지 못해 프리팹 DB 등록은 건너뜁니다.");
                return;
            }

            db.RemovePrefab(NotificationKey);
            db.AddPrefab(NotificationKey, notification.gameObject, CanvasLayer.HUD, "시스템/퀘스트/파티 획득 Notification");
            EditorUtility.SetDirty(db);
        }

        private static void EnsureDirectory(string path)
        {
            if (string.IsNullOrEmpty(path) || Directory.Exists(path))
                return;

            Directory.CreateDirectory(path);
        }

        private static RectTransform Rt(GameObject go) => go.GetComponent<RectTransform>();

        private static GameObject NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            if (parent != null)
                go.transform.SetParent(parent, false);
            return go;
        }

        private static void Stretch(GameObject go)
        {
            var rt = Rt(go);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void InsetStretch(RectTransform rt, float inset)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }

        private static Image AddImage(GameObject go, Color color, Sprite sprite = null, bool sliced = false)
        {
            var img = go.AddComponent<Image>();
            img.color = color;
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            }
            return img;
        }

        private static TextMeshProUGUI AddText(GameObject go, string text, float size, Color color, TextAlignmentOptions align)
        {
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false;
            if (TMP_Settings.defaultFontAsset != null)
                t.font = TMP_Settings.defaultFontAsset;
            return t;
        }

        private static VerticalLayoutGroup AddVLG(GameObject go, float spacing, int pad)
        {
            var v = go.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.padding = new RectOffset(pad, pad, pad, pad);
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            return v;
        }

        private static HorizontalLayoutGroup AddHLG(GameObject go, float spacing, int pad)
        {
            var h = go.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.padding = new RectOffset(pad, pad, pad, pad);
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = true;
            return h;
        }

        private static void SetHeight(GameObject go, float height)
        {
            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            le.minHeight = height;
            le.preferredHeight = height;
            le.flexibleHeight = 0f;
        }

        private static void SetWidth(GameObject go, float width)
        {
            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            le.minWidth = width;
            le.preferredWidth = width;
            le.flexibleWidth = 0f;
        }

        private static void AddFlexibleW(GameObject go, float flex)
        {
            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            le.flexibleWidth = flex;
        }

        private static void SetRef(SerializedObject so, string propName, UnityEngine.Object value)
        {
            var p = so.FindProperty(propName);
            if (p == null)
            {
                Debug.LogWarning($"[NotificationBuilder] 직렬화 프로퍼티를 찾을 수 없음: {propName}");
                return;
            }
            p.objectReferenceValue = value;
        }

        private static void SetInt(SerializedObject so, string propName, int value)
        {
            var p = so.FindProperty(propName);
            if (p != null)
                p.intValue = value;
        }
    }
}
