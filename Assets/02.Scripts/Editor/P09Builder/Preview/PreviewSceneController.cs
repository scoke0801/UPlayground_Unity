using UnityEditor;
using UnityEngine;

namespace UPlayGround.Editor.P09Builder
{
    internal sealed class PreviewSceneController : System.IDisposable
    {
        private const string ScenePreviewName = "__P09Builder_ScenePreview";

        private readonly PreviewRenderUtility _preview;
        private GameObject _instance;
        private Vector2 _drag;
        private int _lastVisualSignature;

        public float CameraFov { get; set; } = 30f;
        public float VerticalOffset { get; set; }
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

            // Variant 프리팹의 m_RemovedGameObjects 등 오버라이드를 정확히 반영하기 위해
            // 빌드 파이프라인과 동일하게 PrefabUtility.InstantiatePrefab + Unpack 경로를 사용한다.
            _instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            if (_instance == null)
            {
                LastError = "프리팹 인스턴스화에 실패했습니다.";
                return;
            }
            PrefabUtility.UnpackPrefabInstance(_instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
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
            PositionCamera(rect);
            _preview.camera.Render();
            var texture = _preview.EndPreview();
            GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, false);
        }

        public void ResetView()
        {
            _drag = Vector2.zero;
            VerticalOffset = 0f;
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
            _instance.transform.position = new Vector3(0f, VerticalOffset, 0f);
            _instance.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        }

        private void PositionCamera(Rect rect)
        {
            var rotation = Quaternion.Euler(0f, 180f + _drag.x * 0.6f, 0f);
            _instance.transform.position = new Vector3(0f, VerticalOffset, 0f);
            _instance.transform.rotation = rotation;

            var bounds = CalculateRendererBounds(_instance);
            var target = bounds.center;
            var verticalSize = Mathf.Max(bounds.size.y, 0.5f);
            var horizontalSize = Mathf.Max(bounds.size.x, bounds.size.z, 0.5f);
            var aspect = Mathf.Max(rect.width / Mathf.Max(rect.height, 1f), 0.1f);
            var verticalFov = CameraFov * Mathf.Deg2Rad;
            var horizontalFov = 2f * Mathf.Atan(Mathf.Tan(verticalFov * 0.5f) * aspect);
            const float FillRatio = 0.88f;
            var distanceByHeight = (verticalSize * 0.5f) / Mathf.Tan(verticalFov * 0.5f) / FillRatio;
            var distanceByWidth = (horizontalSize * 0.5f) / Mathf.Tan(horizontalFov * 0.5f) / FillRatio;
            var distance = Mathf.Max(distanceByHeight, distanceByWidth, 1.2f);

            var camera = _preview.camera;
            camera.transform.position = target + new Vector3(0f, 0f, -distance);
            camera.transform.rotation = Quaternion.LookRotation(target - camera.transform.position);
            camera.nearClipPlane = 0.03f;
            camera.farClipPlane = Mathf.Max(100f, distance + bounds.extents.magnitude + 10f);
        }

        private static Bounds CalculateRendererBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(includeInactive: false);
            if (renderers == null || renderers.Length == 0)
                return new Bounds(root.transform.position + Vector3.up, Vector3.one * 2f);

            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return bounds;
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
                hash = hash * 31 + config.FacialHairId;
                hash = hash * 31 + GetId(config.EyeColorSo);
                hash = hash * 31 + GetId(config.SkinColorSo);
                hash = hash * 31 + (config.UseWeaponGroup ? 1 : 0);
                hash = hash * 31 + GetId(config.WeaponGroupSo);
                hash = hash * 31 + GetId(config.SwordSo);
                hash = hash * 31 + GetId(config.SubSwordSo);
                hash = hash * 31 + GetId(config.GreatSwordSo);
                hash = hash * 31 + GetId(config.ShieldSo);
                hash = hash * 31 + GetId(config.BowSo);
                hash = hash * 31 + GetId(config.StaffSo);
                hash = hash * 31 + GetId(config.SpearSo);
                hash = hash * 31 + GetId(config.DualAxeSo);
                hash = hash * 31 + GetId(config.WhipSo);
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
