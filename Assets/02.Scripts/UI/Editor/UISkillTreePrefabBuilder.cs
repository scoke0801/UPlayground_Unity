#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.Path;
using UPlayGround.Manager;

namespace UPlayGround.UI.Growth.EditorTools
{
    public static class UISkillTreePrefabBuilder
    {
        public const string PrefabPath = "Assets/03.Prefabs/UI/Scene/Growth/UI_Scene_SkillTree.prefab";
        private const string DatabasePath = "Assets/10.Datas/Path/UIPrefabDatabase.asset";
        private const string NodeFramePath =
            "Assets/ExternalAssets/UI/Artsystack - Fantasy RPG GUI/ResourcesData/Sprites/components/circle_slot_01.png";

        private static readonly string[] TransactionAssetPaths =
        {
            PrefabPath,
            DatabasePath,
            UPlayGround.UI.EditorTools.UIMenuPanelPrefabBuilder.PrefabPath,
        };

        [UPlayGround.EditorTools.UPlaygroundTool(
            "UPlayGround/UI/스킬 트리 프리팹 생성")]
        public static void Build()
        {
            AssetSnapshot[] snapshots = CaptureSnapshots();
            try
            {
                BuildAssets();
            }
            catch (System.Exception buildException)
            {
                try
                {
                    RestoreSnapshots(snapshots);
                }
                catch (System.Exception restoreException)
                {
                    throw new System.AggregateException(
                        "스킬 트리 UI 생성 실패 후 에셋 복구에도 실패했습니다.",
                        buildException,
                        restoreException);
                }

                throw;
            }
        }

        private static void BuildAssets()
        {
            EnsureFolder("Assets/03.Prefabs/UI/Scene/Growth");
            bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null;
            GameObject root = exists
                ? PrefabUtility.LoadPrefabContents(PrefabPath)
                : new GameObject(
                    "UI_Scene_SkillTree",
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster),
                    typeof(UI_Scene_SkillTree));
            try
            {
                RectTransform rootRect = root.GetComponent<RectTransform>();
                rootRect.localScale = Vector3.one;
                rootRect.localRotation = Quaternion.identity;
                rootRect.anchorMin = Vector2.zero;
                rootRect.anchorMax = Vector2.one;
                rootRect.anchoredPosition = Vector2.zero;
                rootRect.offsetMin = Vector2.zero;
                rootRect.offsetMax = Vector2.zero;

                Canvas canvas = root.GetComponent<Canvas>() ?? root.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.overrideSorting = false;
                CanvasScaler scaler = root.GetComponent<CanvasScaler>() ?? root.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
                if (root.GetComponent<GraphicRaycaster>() == null)
                    root.AddComponent<GraphicRaycaster>();
                UI_Scene_SkillTree tree = root.GetComponent<UI_Scene_SkillTree>() ?? root.AddComponent<UI_Scene_SkillTree>();
                AssignStyleSprites(tree);
                tree.RebuildEditorPreview();
                if (PrefabUtility.SaveAsPrefabAsset(root, PrefabPath) == null)
                    throw new System.InvalidOperationException($"스킬 트리 프리팹 저장 실패: {PrefabPath}");
            }
            finally
            {
                if (exists) PrefabUtility.UnloadPrefabContents(root);
                else Object.DestroyImmediate(root);
            }

            Register(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
            UPlayGround.UI.EditorTools.UIMenuPanelPrefabBuilder.Build();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[UISkillTreePrefabBuilder] SkillTree 프리팹 계층, UI DB, 메뉴 진입점 등록 완료");
        }

        private static AssetSnapshot[] CaptureSnapshots()
        {
            var snapshots = new AssetSnapshot[TransactionAssetPaths.Length];
            for (int i = 0; i < TransactionAssetPaths.Length; i++)
                snapshots[i] = new AssetSnapshot(TransactionAssetPaths[i]);
            return snapshots;
        }

        private static void RestoreSnapshots(AssetSnapshot[] snapshots)
        {
            for (int i = 0; i < snapshots.Length; i++)
                snapshots[i].RestoreFiles();

            const ImportAssetOptions importOptions =
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate;
            AssetDatabase.Refresh(importOptions);
            for (int i = 0; i < snapshots.Length; i++)
            {
                if (System.IO.File.Exists(snapshots[i].AssetPath))
                    AssetDatabase.ImportAsset(snapshots[i].AssetPath, importOptions);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(importOptions);
        }

        private sealed class AssetSnapshot
        {
            public string AssetPath { get; }

            private readonly byte[] _assetBytes;
            private readonly byte[] _metaBytes;

            public AssetSnapshot(string assetPath)
            {
                AssetPath = assetPath;
                _assetBytes = ReadIfExists(assetPath);
                _metaBytes = ReadIfExists(assetPath + ".meta");
            }

            public void RestoreFiles()
            {
                RestoreFile(AssetPath, _assetBytes);
                RestoreFile(AssetPath + ".meta", _metaBytes);
            }

            private static byte[] ReadIfExists(string path) =>
                System.IO.File.Exists(path) ? System.IO.File.ReadAllBytes(path) : null;

            private static void RestoreFile(string path, byte[] bytes)
            {
                if (bytes != null)
                {
                    System.IO.File.WriteAllBytes(path, bytes);
                }
                else if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                }
            }
        }

        private static void AssignStyleSprites(UI_Scene_SkillTree tree)
        {
            var serialized = new SerializedObject(tree);
            serialized.FindProperty("_nodeFrameSprite").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Sprite>(NodeFramePath);
            serialized.FindProperty("_actionButtonSprite").objectReferenceValue =
                AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Register(GameObject prefab)
        {
            if (prefab == null)
                throw new System.InvalidOperationException($"스킬 트리 프리팹 로드 실패: {PrefabPath}");

            UIPrefabDatabase database = AssetDatabase.LoadAssetAtPath<UIPrefabDatabase>(DatabasePath);
            if (database == null) throw new System.InvalidOperationException($"UI DB 없음: {DatabasePath}");
            var serialized = new SerializedObject(database);
            SerializedProperty entries = serialized.FindProperty("prefabs");
            SerializedProperty target = null;
            for (int i = 0; i < entries.arraySize; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("key").stringValue == UI_Scene_SkillTree.UIKey)
                {
                    target = entry;
                    break;
                }
            }
            if (target == null)
            {
                entries.InsertArrayElementAtIndex(entries.arraySize);
                target = entries.GetArrayElementAtIndex(entries.arraySize - 1);
            }
            target.FindPropertyRelative("key").stringValue = UI_Scene_SkillTree.UIKey;
            target.FindPropertyRelative("prefab").objectReferenceValue = prefab;
            target.FindPropertyRelative("defaultLayer").enumValueIndex =
                System.Array.IndexOf(System.Enum.GetValues(typeof(CanvasLayer)), CanvasLayer.Popup);
            target.FindPropertyRelative("description").stringValue = "캐릭터 고정 스킬 트리";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);
        }

        private static void EnsureFolder(string path)
        {
            string current = "Assets";
            string[] parts = path.Split('/');
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
