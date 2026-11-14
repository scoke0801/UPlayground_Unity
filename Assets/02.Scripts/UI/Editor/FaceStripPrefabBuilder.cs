using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.EditorTools;
using UPlayGround.Manager;

namespace UPlayGround.UI.EditorTools
{
    /// <summary>구매 패널 스프라이트를 사용하는 고정 얼굴 패널 프리팹을 저작한다.</summary>
    public static class FaceStripPrefabBuilder
    {
        public const string PrefabPath = "Assets/03.Prefabs/UI/Common/UIFaceStrip.prefab";
        private const string PanelGuid = "ad3487ad9dad74ad59fefcc2dbc34a20";
        private const string PanelPath = "Assets/ExternalAssets/UI/Layer Lab/GUI Pro-FantasyRPG/ResourcesData/Sprites/Component/Frame/CardFrame_02_Bg.png";

        /// <summary>DB 등록과 분리해 얼굴 패널 프리팹만 생성한다.</summary>
        [UPlaygroundTool("UI.FaceStrip.Build")]
        public static void Build()
        {
            string path = AssetDatabase.GUIDToAssetPath(PanelGuid);
            if (string.IsNullOrEmpty(path))
                path = PanelPath;
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                throw new InvalidOperationException($"얼굴 패널 원본 스프라이트가 없습니다: {path}");

            // 재실행은 기존 프리팹의 수동 편집을 덮어쓰지 않는다.
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
                return;
            if (File.Exists(PrefabPath) || !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(PrefabPath)))
                throw new InvalidOperationException($"기존 파일을 덮어쓸 수 없습니다: {PrefabPath}");

            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            var root = new GameObject(nameof(UIFaceStrip), typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(CanvasGroup));
            try
            {
                Canvas canvas = root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = (int)CanvasLayer.HUD + 2;
                CanvasScaler scaler = root.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
                scaler.referenceResolution = new Vector2(2560f, 1440f);
                CanvasGroup group = root.GetComponent<CanvasGroup>();
                group.alpha = 0f;
                group.interactable = false;
                group.blocksRaycasts = false;

                Image background = CreateImage("Panel", root.transform, sprite);
                RectTransform panel = background.rectTransform;
                panel.anchorMin = new Vector2(0f, 0.45f);
                panel.anchorMax = new Vector2(0.2f, 0.65f);
                panel.pivot = new Vector2(0f, 0.5f);
                panel.offsetMin = Vector2.zero;
                panel.offsetMax = Vector2.zero;
                background.type = Image.Type.Sliced;
                background.color = new Color(0.1f, 0.16f, 0.25f, 0.55f);

                Image face = CreateImage("Face", panel, null);
                face.preserveAspect = true;
                face.rectTransform.anchorMin = new Vector2(-0.15f, -0.15f);
                face.rectTransform.anchorMax = new Vector2(1.15f, 1.15f);
                face.rectTransform.offsetMin = Vector2.zero;
                face.rectTransform.offsetMax = Vector2.zero;
                face.color = Color.clear;

                Image flash = CreateImage("Flash", panel, sprite);
                flash.type = Image.Type.Sliced;
                flash.color = Color.clear;

                UIFaceStrip strip = root.AddComponent<UIFaceStrip>();
                var serialized = new SerializedObject(strip);
                serialized.FindProperty("_panel").objectReferenceValue = panel;
                serialized.FindProperty("_group").objectReferenceValue = group;
                serialized.FindProperty("_background").objectReferenceValue = background;
                serialized.FindProperty("_backgroundOpacity").floatValue = 0.55f;
                serialized.FindProperty("_face").objectReferenceValue = face;
                serialized.FindProperty("_flash").objectReferenceValue = flash;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool success);
                if (!success)
                    throw new InvalidOperationException($"얼굴 패널 프리팹 저장 실패: {PrefabPath}");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite)
        {
            var instance = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)instance.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = instance.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }
    }
}
