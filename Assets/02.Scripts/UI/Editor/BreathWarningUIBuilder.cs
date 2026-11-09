using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.Path;
using UPlayGround.EditorTools;
using UPlayGround.Manager;

namespace UPlayGround.UI.EditorTools
{
    /// <summary>잠수 경고 HUD 프리팹과 재질을 만들고 UIPrefabDatabase에 등록한다. 이미 있으면 참조만 갱신한다.</summary>
    public static class BreathWarningUIBuilder
    {
        private const string PrefabDir = "Assets/03.Prefabs/UI/HUD/BreathWarning";
        private const string PrefabPath = PrefabDir + "/UI_HUD_BreathWarning.prefab";
        private const string MaterialPath = "Assets/06.Shaders/UI/UnderwaterBreathOverlay.mat";
        private const string ShaderName = "UPlayGround/UI/UnderwaterBreathOverlay";
        private const string DatabaseKey = "HudBreathWarning";

        [UPlaygroundTool("UI.BreathWarning.Build")]
        public static void Build()
        {
            Shader shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogError($"[BreathWarningUIBuilder] 셰이더를 찾을 수 없습니다: {ShaderName}");
                return;
            }

            EnsureFolder(PrefabDir);
            Material material = LoadOrCreateMaterial(shader);
            GameObject prefab = BuildPrefab(material);
            if (prefab == null) return;

            bool registered = RegisterInDatabase(prefab);
            AssetDatabase.SaveAssets();
            Debug.Log($"[BreathWarningUIBuilder] 프리팹 {PrefabPath} 생성, DB 등록 {(registered ? "완료" : "실패")}");
        }

        private static Material LoadOrCreateMaterial(Shader shader)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material != null)
            {
                material.shader = shader;
                return material;
            }

            material = new Material(shader) { name = "UnderwaterBreathOverlay" };
            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }

        private static GameObject BuildPrefab(Material material)
        {
            // 루트: RectTransform + Canvas + UI_HUD_BreathWarning (UI_Base는 Canvas를 RequireComponent)
            var root = new GameObject("UI_HUD_BreathWarning", typeof(RectTransform));
            Stretch(root);
            root.AddComponent<Canvas>();
            var panel = root.AddComponent<UI_HUD_BreathWarning>();

            var overlayGo = new GameObject("Overlay", typeof(RectTransform));
            overlayGo.transform.SetParent(root.transform, false);
            Stretch(overlayGo);
            var overlay = overlayGo.AddComponent<Image>();
            overlay.material = material;
            overlay.color = Color.white;
            overlay.raycastTarget = false;
            overlay.enabled = false;

            var so = new SerializedObject(panel);
            so.FindProperty("_layer").enumValueIndex = (int)CanvasLayer.HUD;
            so.FindProperty("_overlay").objectReferenceValue = overlay;
            so.ApplyModifiedPropertiesWithoutUndo();

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool success);
            Object.DestroyImmediate(root);
            if (success) return saved;

            Debug.LogError($"[BreathWarningUIBuilder] 프리팹 저장 실패: {PrefabPath}");
            return null;
        }

        private static bool RegisterInDatabase(GameObject prefab)
        {
            string[] guids = AssetDatabase.FindAssets("t:UIPrefabDatabase");
            if (guids.Length == 0)
            {
                Debug.LogWarning("[BreathWarningUIBuilder] UIPrefabDatabase를 찾지 못했습니다. 수동 등록이 필요합니다.");
                return false;
            }

            var db = AssetDatabase.LoadAssetAtPath<UIPrefabDatabase>(AssetDatabase.GUIDToAssetPath(guids[0]));
            if (db == null) return false;

            if (db.HasKey(DatabaseKey)) db.RemovePrefab(DatabaseKey);
            db.AddPrefab(DatabaseKey, prefab, CanvasLayer.HUD, "잠수 중 수중 색조와 익사 경고 화면 효과");
            EditorUtility.SetDirty(db);
            return true;
        }

        private static void Stretch(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
