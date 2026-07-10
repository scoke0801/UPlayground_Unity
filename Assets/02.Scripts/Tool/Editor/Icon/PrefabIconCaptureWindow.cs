#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UPlayGround.Tool.Editor.Icon
{
    public sealed class PrefabIconCaptureWindow : EditorWindow
    {
        private const int PreviewLayer = 31;
        private const int MinTextureSize = 64;
        private const int MaxTextureSize = 8192;
        private const int CheckerSize = 16;

        [SerializeField] private List<GameObject> _prefabs = new();
        [SerializeField] private int _selectedIndex;
        [SerializeField] private int _iconSize = 512;
        [SerializeField] private string _savePath = "Assets/10.Datas/UI/Icons";
        [SerializeField] private string _filePrefix = "";
        [SerializeField] private string _fileSuffix = "_Icon";
        [SerializeField] private bool _transparentBackground = true;
        [SerializeField] private Color _backgroundColor = new(0.16f, 0.16f, 0.16f, 1f);
        [SerializeField] private bool _useOrthographic = true;
        [SerializeField] private float _fieldOfView = 30f;
        [SerializeField] private float _padding = 1.12f;
        [SerializeField] private float _yaw = 35f;
        [SerializeField] private float _pitch = 18f;
        [SerializeField] private float _roll;
        [SerializeField] private Vector3 _modelRotation = Vector3.zero;
        [SerializeField] private Vector3 _modelOffset = Vector3.zero;
        [SerializeField] private float _modelScale = 1f;
        [SerializeField] private float _keyLightIntensity = 1.2f;
        [SerializeField] private float _fillLightIntensity = 0.55f;
        [SerializeField] private bool _autoUpdatePreview = true;
        [SerializeField] private bool _overwriteExisting = true;

        private Camera _camera;
        private Light _keyLight;
        private Light _fillLight;
        private GameObject _previewInstance;
        private Texture2D _previewTexture;
        private Texture2D _checkerTexture;
        private Vector2 _prefabScroll;
        private Rect _previewRect;
        private string _lastMessage;

        [MenuItem("UPlayGround/유틸/아이콘/3D 프리팹 아이콘 생성기", priority = 120)]
        public static void Open()
        {
            var window = GetWindow<PrefabIconCaptureWindow>("Prefab Icon Capture");
            window.minSize = new Vector2(640f, 640f);
            window.Show();
        }

        private void OnEnable()
        {
            EnsureCameraRig();
            RebuildPreviewInstance();
        }

        private void OnDisable()
        {
            Cleanup();
        }

        private void OnGUI()
        {
            HandlePreviewInput();

            EditorGUILayout.Space(8f);
            DrawHeader();
            EditorGUILayout.Space(8f);
            DrawPrefabList();
            EditorGUILayout.Space(8f);
            DrawCaptureSettings();
            EditorGUILayout.Space(8f);
            DrawCameraSettings();
            EditorGUILayout.Space(8f);
            DrawPreview();
            EditorGUILayout.Space(8f);
            DrawActions();
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("3D 프리팹 아이콘 생성기", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("프리팹을 임시 렌더 씬에 배치한 뒤 정사각 PNG로 캡처하고 Sprite 아이콘 import 설정을 적용합니다.", MessageType.Info);
        }

        private void DrawPrefabList()
        {
            EditorGUILayout.LabelField("대상 프리팹", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("선택 항목 추가", GUILayout.Height(24f)))
                    AddSelection();

                using (new EditorGUI.DisabledScope(_prefabs.Count == 0))
                {
                    if (GUILayout.Button("목록 비우기", GUILayout.Height(24f)))
                    {
                        _prefabs.Clear();
                        _selectedIndex = 0;
                        RebuildPreviewInstance();
                    }
                }
            }

            int removeIndex = -1;
            _prefabScroll = EditorGUILayout.BeginScrollView(_prefabScroll, GUILayout.MinHeight(92f), GUILayout.MaxHeight(150f));
            for (int i = 0; i < _prefabs.Count; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    bool selected = i == _selectedIndex;
                    if (GUILayout.Toggle(selected, (i + 1).ToString(), EditorStyles.radioButton, GUILayout.Width(32f)) != selected)
                    {
                        _selectedIndex = i;
                        RebuildPreviewInstance();
                    }

                    EditorGUI.BeginChangeCheck();
                    _prefabs[i] = (GameObject)EditorGUILayout.ObjectField(_prefabs[i], typeof(GameObject), false);
                    if (EditorGUI.EndChangeCheck() && i == _selectedIndex)
                        RebuildPreviewInstance();

                    if (GUILayout.Button("X", GUILayout.Width(24f)))
                        removeIndex = i;
                }
            }
            EditorGUILayout.EndScrollView();

            if (removeIndex >= 0)
            {
                _prefabs.RemoveAt(removeIndex);
                _selectedIndex = Mathf.Clamp(_selectedIndex, 0, Mathf.Max(0, _prefabs.Count - 1));
                RebuildPreviewInstance();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("빈 슬롯 추가"))
                    _prefabs.Add(null);

                using (new EditorGUI.DisabledScope(CurrentPrefab == null))
                {
                    if (GUILayout.Button("현재 프리팹 핑"))
                        EditorGUIUtility.PingObject(CurrentPrefab);
                }
            }
        }

        private void DrawCaptureSettings()
        {
            EditorGUILayout.LabelField("저장", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            _iconSize = EditorGUILayout.IntField("아이콘 크기", _iconSize);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("256")) _iconSize = 256;
                if (GUILayout.Button("512")) _iconSize = 512;
                if (GUILayout.Button("1024")) _iconSize = 1024;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                _savePath = EditorGUILayout.TextField("저장 폴더", _savePath);
                if (GUILayout.Button("...", GUILayout.Width(30f)))
                    PickSaveFolder();
            }

            _filePrefix = EditorGUILayout.TextField("파일 접두사", _filePrefix);
            _fileSuffix = EditorGUILayout.TextField("파일 접미사", _fileSuffix);
            _overwriteExisting = EditorGUILayout.Toggle("기존 파일 덮어쓰기", _overwriteExisting);
            _transparentBackground = EditorGUILayout.Toggle("투명 배경", _transparentBackground);
            using (new EditorGUI.DisabledScope(_transparentBackground))
                _backgroundColor = EditorGUILayout.ColorField("배경색", _backgroundColor);

            if (EditorGUI.EndChangeCheck())
            {
                _iconSize = Mathf.Clamp(_iconSize, MinTextureSize, MaxTextureSize);
                if (_autoUpdatePreview) UpdatePreview();
            }
        }

        private void DrawCameraSettings()
        {
            EditorGUILayout.LabelField("촬영", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            _useOrthographic = EditorGUILayout.Toggle("정사영", _useOrthographic);
            using (new EditorGUI.DisabledScope(_useOrthographic))
                _fieldOfView = EditorGUILayout.Slider("FOV", _fieldOfView, 10f, 80f);

            _padding = EditorGUILayout.Slider("여백", _padding, 1f, 1.8f);
            _yaw = EditorGUILayout.Slider("Yaw", _yaw, -180f, 180f);
            _pitch = EditorGUILayout.Slider("Pitch", _pitch, -75f, 75f);
            _roll = EditorGUILayout.Slider("Roll", _roll, -45f, 45f);
            _modelRotation = EditorGUILayout.Vector3Field("모델 회전", _modelRotation);
            _modelOffset = EditorGUILayout.Vector3Field("모델 오프셋", _modelOffset);
            _modelScale = EditorGUILayout.Slider("모델 스케일", _modelScale, 0.1f, 5f);
            _keyLightIntensity = EditorGUILayout.Slider("키 라이트", _keyLightIntensity, 0f, 4f);
            _fillLightIntensity = EditorGUILayout.Slider("필 라이트", _fillLightIntensity, 0f, 4f);
            _autoUpdatePreview = EditorGUILayout.Toggle("자동 미리보기 갱신", _autoUpdatePreview);

            if (EditorGUI.EndChangeCheck())
            {
                ApplyInstanceTransform();
                if (_autoUpdatePreview) UpdatePreview();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("정면")) SetAngles(0f, 12f);
                if (GUILayout.Button("3/4")) SetAngles(35f, 18f);
                if (GUILayout.Button("측면")) SetAngles(90f, 10f);
                if (GUILayout.Button("위")) SetAngles(0f, 65f);
            }
        }

        private void DrawPreview()
        {
            EditorGUILayout.LabelField("미리보기", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(CurrentPrefab == null))
                {
                    if (GUILayout.Button("미리보기 갱신", GUILayout.Height(26f)))
                        UpdatePreview();
                }

                if (GUILayout.Button("뷰 초기화", GUILayout.Height(26f)))
                {
                    _yaw = 35f;
                    _pitch = 18f;
                    _roll = 0f;
                    _modelRotation = Vector3.zero;
                    _modelOffset = Vector3.zero;
                    _modelScale = 1f;
                    ApplyInstanceTransform();
                    UpdatePreview();
                }
            }

            float size = Mathf.Min(position.width - 24f, 420f);
            _previewRect = GUILayoutUtility.GetRect(size, size, GUILayout.ExpandWidth(false));

            if (_transparentBackground)
                DrawCheckerboard(_previewRect);
            else
                EditorGUI.DrawRect(_previewRect, _backgroundColor);

            if (_previewTexture != null)
                GUI.DrawTexture(_previewRect, _previewTexture, ScaleMode.ScaleToFit, true);
            else
                EditorGUI.LabelField(_previewRect, "프리팹을 추가하면 미리보기가 표시됩니다.", EditorStyles.centeredGreyMiniLabel);

            if (!string.IsNullOrEmpty(_lastMessage))
                EditorGUILayout.HelpBox(_lastMessage, MessageType.None);
        }

        private void DrawActions()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(CurrentPrefab == null))
                {
                    if (GUILayout.Button("현재 프리팹 저장", GUILayout.Height(36f)))
                        CaptureCurrent();
                }

                using (new EditorGUI.DisabledScope(_prefabs.Count == 0))
                {
                    if (GUILayout.Button("전체 저장", GUILayout.Height(36f)))
                        CaptureAll();
                }
            }
        }

        private GameObject CurrentPrefab
        {
            get
            {
                if (_prefabs == null || _prefabs.Count == 0) return null;
                _selectedIndex = Mathf.Clamp(_selectedIndex, 0, _prefabs.Count - 1);
                return _prefabs[_selectedIndex];
            }
        }

        private void AddSelection()
        {
            foreach (var selected in Selection.gameObjects)
            {
                var prefab = PrefabUtility.GetCorrespondingObjectFromSource(selected);
                if (prefab == null && PrefabUtility.IsPartOfPrefabAsset(selected))
                    prefab = selected;
                if (prefab == null)
                    prefab = selected;

                if (prefab != null && !_prefabs.Contains(prefab))
                    _prefabs.Add(prefab);
            }

            if (_prefabs.Count > 0)
            {
                _selectedIndex = Mathf.Clamp(_selectedIndex, 0, _prefabs.Count - 1);
                RebuildPreviewInstance();
            }
        }

        private void PickSaveFolder()
        {
            string startPath = _savePath.StartsWith("Assets") ? Path.Combine(Directory.GetCurrentDirectory(), _savePath) : _savePath;
            string selected = EditorUtility.OpenFolderPanel("아이콘 저장 폴더", startPath, "");
            if (string.IsNullOrEmpty(selected)) return;

            string dataPath = Application.dataPath.Replace("\\", "/");
            selected = selected.Replace("\\", "/");
            _savePath = selected.StartsWith(dataPath) ? "Assets" + selected.Substring(dataPath.Length) : selected;
        }

        private void SetAngles(float yaw, float pitch)
        {
            _yaw = yaw;
            _pitch = pitch;
            if (_autoUpdatePreview) UpdatePreview();
        }

        private void HandlePreviewInput()
        {
            if (_previewRect.width <= 0f || !_previewRect.Contains(Event.current.mousePosition))
                return;

            var evt = Event.current;
            if (evt.type == EventType.MouseDrag && evt.button == 0)
            {
                _yaw += evt.delta.x * 0.5f;
                _pitch = Mathf.Clamp(_pitch - evt.delta.y * 0.5f, -75f, 75f);
                UpdatePreview();
                evt.Use();
            }
            else if (evt.type == EventType.ScrollWheel)
            {
                _padding = Mathf.Clamp(_padding + evt.delta.y * 0.01f, 1f, 1.8f);
                UpdatePreview();
                evt.Use();
            }
        }

        private void EnsureCameraRig()
        {
            if (_camera != null) return;

            var cameraObject = new GameObject("_PrefabIconCaptureCamera")
            {
                hideFlags = HideFlags.HideAndDontSave,
                layer = PreviewLayer
            };

            _camera = cameraObject.AddComponent<Camera>();
            _camera.enabled = false;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.nearClipPlane = 0.01f;
            _camera.farClipPlane = 1000f;
            _camera.cullingMask = 1 << PreviewLayer;

            var urpData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            urpData.renderType = CameraRenderType.Base;
            urpData.renderShadows = false;

            _keyLight = CreateLight("_PrefabIconKeyLight", 45f, -35f, _keyLightIntensity);
            _fillLight = CreateLight("_PrefabIconFillLight", 25f, 145f, _fillLightIntensity);
        }

        private static Light CreateLight(string name, float pitch, float yaw, float intensity)
        {
            var lightObject = new GameObject(name)
            {
                hideFlags = HideFlags.HideAndDontSave,
                layer = PreviewLayer
            };

            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            light.intensity = intensity;
            light.cullingMask = 1 << PreviewLayer;
            return light;
        }

        private void RebuildPreviewInstance()
        {
            DestroyPreviewInstance();

            var prefab = CurrentPrefab;
            if (prefab == null)
            {
                DestroyPreviewTexture();
                return;
            }

            _previewInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (_previewInstance == null)
                _previewInstance = Instantiate(prefab);

            _previewInstance.name = prefab.name + "_IconPreview";
            _previewInstance.hideFlags = HideFlags.HideAndDontSave;
            SetLayerRecursively(_previewInstance, PreviewLayer);
            ApplyInstanceTransform();
            UpdatePreview();
        }

        private void ApplyInstanceTransform()
        {
            if (_previewInstance == null) return;

            _previewInstance.transform.position = _modelOffset;
            _previewInstance.transform.rotation = Quaternion.Euler(_modelRotation);
            _previewInstance.transform.localScale = Vector3.one * Mathf.Max(0.01f, _modelScale);
        }

        private static void SetLayerRecursively(GameObject target, int layer)
        {
            target.layer = layer;
            foreach (Transform child in target.transform)
                SetLayerRecursively(child.gameObject, layer);
        }

        private Bounds CalculateBounds()
        {
            if (_previewInstance == null)
                return new Bounds(Vector3.zero, Vector3.one);

            var renderers = _previewInstance.GetComponentsInChildren<Renderer>(false);
            if (renderers.Length == 0)
                return new Bounds(_previewInstance.transform.position, Vector3.one);

            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return bounds;
        }

        private void PositionCamera(Bounds bounds)
        {
            EnsureCameraRig();

            var target = bounds.center;
            var extents = bounds.extents;
            float radius = Mathf.Max(extents.magnitude, 0.25f);
            var orbit = Quaternion.Euler(_pitch, _yaw, _roll);
            var direction = orbit * Vector3.back;
            float distance = Mathf.Max(radius * 2.8f * _padding, 0.5f);

            _camera.transform.position = target + direction * distance;
            _camera.transform.rotation = Quaternion.LookRotation(target - _camera.transform.position, orbit * Vector3.up);
            _camera.orthographic = _useOrthographic;
            _camera.fieldOfView = _fieldOfView;
            _camera.backgroundColor = _transparentBackground ? new Color(0f, 0f, 0f, 0f) : _backgroundColor;
            _camera.nearClipPlane = 0.01f;
            _camera.farClipPlane = Mathf.Max(100f, distance + radius * 4f);

            if (_useOrthographic)
                _camera.orthographicSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z) * 0.5f * _padding;

            if (_keyLight != null) _keyLight.intensity = _keyLightIntensity;
            if (_fillLight != null) _fillLight.intensity = _fillLightIntensity;
        }

        private void UpdatePreview()
        {
            if (CurrentPrefab == null || _previewInstance == null)
                return;

            RenderToTexture(_iconSize, ref _previewTexture);
            Repaint();
        }

        private void RenderToTexture(int size, ref Texture2D targetTexture)
        {
            EnsureCameraRig();
            ApplyInstanceTransform();
            PositionCamera(CalculateBounds());

            var format = RenderTextureFormat.ARGB32;
            RenderTexture renderTexture = RenderTexture.GetTemporary(size, size, 24, format);
            RenderTexture previous = RenderTexture.active;

            _camera.targetTexture = renderTexture;
            _camera.Render();
            _camera.targetTexture = null;

            RenderTexture.active = renderTexture;

            if (targetTexture == null || targetTexture.width != size || targetTexture.height != size)
            {
                if (targetTexture != null)
                    DestroyImmediate(targetTexture);
                targetTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
            }

            targetTexture.ReadPixels(new Rect(0, 0, size, size), 0, 0);
            targetTexture.Apply();

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTexture);
        }

        private void CaptureCurrent()
        {
            var prefab = CurrentPrefab;
            if (prefab == null) return;

            string assetPath = CapturePrefab(prefab);
            if (string.IsNullOrEmpty(assetPath)) return;

            var savedAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            if (savedAsset != null)
                EditorGUIUtility.PingObject(savedAsset);

            _lastMessage = $"저장 완료: {assetPath}";
            Repaint();
        }

        private void CaptureAll()
        {
            int savedCount = 0;
            try
            {
                for (int i = 0; i < _prefabs.Count; i++)
                {
                    var prefab = _prefabs[i];
                    if (prefab == null) continue;

                    _selectedIndex = i;
                    RebuildPreviewInstance();
                    if (!string.IsNullOrEmpty(CapturePrefab(prefab)))
                        savedCount++;
                }
            }
            finally
            {
                AssetDatabase.Refresh();
            }

            _lastMessage = $"전체 저장 완료: {savedCount}개";
            EditorUtility.DisplayDialog("프리팹 아이콘 생성", $"{savedCount}개 아이콘 저장 완료", "확인");
        }

        private string CapturePrefab(GameObject prefab)
        {
            EnsureValidSaveDirectory();

            Texture2D captured = null;
            RenderToTexture(_iconSize, ref captured);
            if (captured == null) return null;

            string assetPath = GetAssetPath(prefab);
            string fullPath = ToAbsolutePath(assetPath);
            File.WriteAllBytes(fullPath, captured.EncodeToPNG());
            DestroyImmediate(captured);

            AssetDatabase.ImportAsset(assetPath);
            ConfigureTextureImporter(assetPath);
            return assetPath;
        }

        private void EnsureValidSaveDirectory()
        {
            if (string.IsNullOrWhiteSpace(_savePath))
                _savePath = "Assets/10.Datas/UI/Icons";

            Directory.CreateDirectory(ToAbsolutePath(_savePath));
        }

        private string GetAssetPath(GameObject prefab)
        {
            string safeName = MakeSafeFileName(_filePrefix + prefab.name + _fileSuffix);
            string assetPath = $"{_savePath.TrimEnd('/', '\\')}/{safeName}.png";
            if (_overwriteExisting)
                return assetPath;

            int index = 1;
            string candidate = assetPath;
            while (File.Exists(ToAbsolutePath(candidate)))
            {
                candidate = $"{_savePath.TrimEnd('/', '\\')}/{safeName}_{index}.png";
                index++;
            }

            return candidate;
        }

        private static string MakeSafeFileName(string value)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars())
                value = value.Replace(invalid, '_');
            return value;
        }

        private static string ToAbsolutePath(string path)
        {
            if (Path.IsPathRooted(path))
                return path;

            if (path.StartsWith("Assets"))
                return Path.Combine(Application.dataPath, path.Substring("Assets".Length).TrimStart('/', '\\'));

            return Path.Combine(Directory.GetCurrentDirectory(), path);
        }

        private void ConfigureTextureImporter(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.maxTextureSize = Mathf.Min(_iconSize, 16384);
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.alphaIsTransparency = _transparentBackground;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        private void DrawCheckerboard(Rect rect)
        {
            if (_checkerTexture == null)
                _checkerTexture = CreateCheckerTexture();

            GUI.DrawTextureWithTexCoords(rect, _checkerTexture, new Rect(0f, 0f, rect.width / CheckerSize, rect.height / CheckerSize));
        }

        private static Texture2D CreateCheckerTexture()
        {
            int size = CheckerSize * 2;
            var texture = new Texture2D(size, size, TextureFormat.RGB24, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Point
            };

            Color light = new(0.72f, 0.72f, 0.72f);
            Color dark = new(0.48f, 0.48f, 0.48f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isLight = (x / CheckerSize + y / CheckerSize) % 2 == 0;
                    texture.SetPixel(x, y, isLight ? light : dark);
                }
            }

            texture.Apply();
            return texture;
        }

        private void DestroyPreviewInstance()
        {
            if (_previewInstance == null) return;
            DestroyImmediate(_previewInstance);
            _previewInstance = null;
        }

        private void DestroyPreviewTexture()
        {
            if (_previewTexture == null) return;
            DestroyImmediate(_previewTexture);
            _previewTexture = null;
        }

        private void Cleanup()
        {
            DestroyPreviewInstance();
            DestroyPreviewTexture();

            if (_checkerTexture != null)
            {
                DestroyImmediate(_checkerTexture);
                _checkerTexture = null;
            }

            if (_camera != null)
            {
                DestroyImmediate(_camera.gameObject);
                _camera = null;
            }

            if (_keyLight != null)
            {
                DestroyImmediate(_keyLight.gameObject);
                _keyLight = null;
            }

            if (_fillLight != null)
            {
                DestroyImmediate(_fillLight.gameObject);
                _fillLight = null;
            }
        }
    }
}
#endif
