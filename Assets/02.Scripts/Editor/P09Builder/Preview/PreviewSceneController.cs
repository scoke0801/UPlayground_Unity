using UnityEditor;
using UnityEngine;

namespace Game.Editor.P09Builder
{
    internal sealed class PreviewSceneController : System.IDisposable
    {
        private const string ScenePreviewName = "__P09Builder_ScenePreview";

        private readonly PreviewRenderUtility _preview;
        private GameObject _instance;
        private Vector2 _drag;
        private int _lastVisualSignature;

        public float CameraFov { get; set; } = 30f;
        public Color BackgroundColor { get; set; } = new Color(0.18f, 0.18f, 0.18f, 1f);
        public string LastError { get; private set; }
        public bool HasInstance => _instance != null;

        public PreviewSceneController()
        {
            _preview = new PreviewRenderUtility();
            _preview.cameraFieldOfView = CameraFov;
            _preview.lights[0].intensity = 1.2f;
            _preview.lights[0].transform.rotation = Quaternion.Euler(35f, 35f, 0f);
            _preview.lights[1].intensity = 0.7f;
        }

        public void Rebuild(CharacterBuildConfig config, P09AssetCatalog catalog, bool force = false)
        {
            if (config == null)
            {
                LastError = "Config가 없습니다.";
                return;
            }

            var signature = GetVisualSignature(config);
            if (!force && _instance != null && _lastVisualSignature == signature)
                return;

            ClearInstance();
            _lastVisualSignature = signature;
            LastError = null;

            var basePath = PathConfig.GetBasePrefabPath(config.Sex, config.UseMagicaCloth);
            var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(basePath);
            if (basePrefab == null)
            {
                LastError = $"베이스 프리팹을 찾을 수 없습니다: {basePath}";
                return;
            }

            _instance = Object.Instantiate(basePrefab);
            _instance.name = "P09 Preview";
            _preview.AddSingleGO(_instance);

            if (catalog == null)
            {
                catalog = new P09AssetCatalog();
                catalog.Refresh();
            }

            AppearanceApplier.Apply(_instance, config, catalog);
            ApplyWeaponStep.Apply(_instance, config, catalog);
            FrameInstance();
        }

        public void Draw(Rect rect)
        {
            HandleDrag(rect);

            if (_instance == null)
            {
                EditorGUI.DrawRect(rect, BackgroundColor);
                var message = string.IsNullOrEmpty(LastError) ? "미리보기를 생성하세요." : LastError;
                EditorGUI.LabelField(rect, message, EditorStyles.centeredGreyMiniLabel);
                return;
            }

            _preview.cameraFieldOfView = CameraFov;
            _preview.BeginPreview(rect, GUIStyle.none);
            GL.Clear(true, true, BackgroundColor);
            PositionCamera();
            _preview.camera.Render();
            var texture = _preview.EndPreview();
            GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, false);
        }

        public void ResetView()
        {
            _drag = Vector2.zero;
            FrameInstance();
        }

        public void OpenInSceneView(CharacterBuildConfig config, P09AssetCatalog catalog)
        {
            ClearScenePreview();

            var basePath = PathConfig.GetBasePrefabPath(config.Sex, config.UseMagicaCloth);
            var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(basePath);
            if (basePrefab == null)
            {
                EditorUtility.DisplayDialog("SceneView 미리보기 실패", $"베이스 프리팹을 찾을 수 없습니다:\n{basePath}", "확인");
                return;
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            if (go == null)
            {
                EditorUtility.DisplayDialog("SceneView 미리보기 실패", "프리팹 인스턴스화에 실패했습니다.", "확인");
                return;
            }

            go.name = ScenePreviewName;
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
            AppearanceApplier.Apply(go, config, catalog);
            ApplyWeaponStep.Apply(go, config, catalog);

            Selection.activeGameObject = go;
            SceneView.lastActiveSceneView?.FrameSelected();
        }

        public void ClearInstance()
        {
            if (_instance == null) return;
            Object.DestroyImmediate(_instance);
            _instance = null;
        }

        public void Dispose()
        {
            ClearInstance();
            _preview.Cleanup();
        }

        private void HandleDrag(Rect rect)
        {
            var evt = Event.current;
            if (evt.type != EventType.MouseDrag || evt.button != 0 || !rect.Contains(evt.mousePosition))
                return;

            _drag += evt.delta;
            evt.Use();
        }

        private void FrameInstance()
        {
            if (_instance == null) return;
            _instance.transform.position = Vector3.zero;
            _instance.transform.rotation = Quaternion.identity;
        }

        private void PositionCamera()
        {
            var rotation = Quaternion.Euler(0f, _drag.x * 0.6f, 0f);
            _instance.transform.rotation = rotation;

            var camera = _preview.camera;
            camera.transform.position = new Vector3(0f, 1.15f, -4.2f);
            camera.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 1.05f, 0f) - camera.transform.position);
            camera.nearClipPlane = 0.03f;
            camera.farClipPlane = 100f;
        }

        private static void ClearScenePreview()
        {
            var existing = GameObject.Find(ScenePreviewName);
            if (existing != null)
                Object.DestroyImmediate(existing);
        }

        private static int GetVisualSignature(CharacterBuildConfig config)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (int)config.Sex;
                hash = hash * 31 + (config.UseMagicaCloth ? 1 : 0);
                hash = hash * 31 + GetId(config.BustSizeSo);
                hash = hash * 31 + GetId(config.HairStyleSo);
                hash = hash * 31 + GetId(config.HairColorSo);
                hash = hash * 31 + GetId(config.FaceTypeSo);
                hash = hash * 31 + GetId(config.EmotionSo);
                hash = hash * 31 + GetId(config.FacialHairSo);
                hash = hash * 31 + GetId(config.EyeColorSo);
                hash = hash * 31 + GetId(config.SkinColorSo);
                hash = hash * 31 + (config.UseWeaponGroup ? 1 : 0);
                hash = hash * 31 + GetId(config.WeaponGroupSo);
                hash = hash * 31 + GetId(config.SwordSo);
                hash = hash * 31 + GetId(config.ShieldSo);
                hash = hash * 31 + GetId(config.BowSo);
                hash = hash * 31 + GetId(config.StaffSo);
                hash = hash * 31 + (config.ShowArrows ? 1 : 0);

                foreach (var slot in BuilderArmorSlotExtensions.All)
                    hash = hash * 31 + GetId(config.ArmorSelections?.Get(slot));

                return hash;
            }
        }

        private static int GetId(Object obj)
        {
            return obj != null ? obj.GetInstanceID() : 0;
        }
    }
}
