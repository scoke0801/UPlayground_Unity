#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.UI;

namespace UPlayGround.Tool.Editor
{
    /// <summary>
    /// 미니맵 캡처 에디터 창.
    /// 메뉴: UPlayGround / Minimap / Minimap Capture Editor
    ///
    /// ■ 기능
    ///   · 직교 카메라로 씬을 탑다운 촬영 → PNG 저장 → Sprite 생성
    ///   · 캡처 중심·범위를 씬 뷰 Gizmo로 실시간 시각화
    ///   · 저장 후 MinimapIconConfigSO에 자동 할당 (선택)
    ///   · URP/레거시 렌더 파이프라인 공통 지원 (카메라 직접 렌더)
    /// </summary>
    public class MinimapCaptureEditorWindow : EditorWindow
    {
        // ── 탭 ───────────────────────────────────────────────────
        private enum Tab { Capture, Settings, Help }
        private Tab _currentTab = Tab.Capture;

        // ── 캡처 파라미터 ────────────────────────────────────────
        private Vector3 _captureCenter     = Vector3.zero;
        private Vector2 _captureWorldSize  = new(200f, 200f); // 캡처할 월드 범위 (X=가로, Y=세로)
        private float   _cameraHeight      = 150f;   // 캡처 카메라 높이
        private int     _textureWidth      = 4096;   // 출력 가로 해상도
        private int     _textureHeight     = 4096;   // 출력 세로 해상도
        private LayerMask _layerMask       = ~0;     // 캡처할 레이어
        private Color   _clearColor        = new Color(0.1f, 0.1f, 0.1f, 1f);
        private bool    _transparentBg     = false;
        private float   _cameraNear        = 0.1f;
        private float   _cameraFar         = 500f;

        // ── 저장 ─────────────────────────────────────────────────
        private string  _savePath          = "Assets/10.Datas/UI/Minimap";
        private string  _fileName          = "MinimapBackground";

        // ── 자동 할당 ─────────────────────────────────────────────
        private MinimapIconConfigSO _targetConfig;
        private bool                _autoAssign = true;

        // ── 프리뷰 ───────────────────────────────────────────────
        private Texture2D _previewTexture;
        private Vector2   _previewScroll;
        private bool      _showFullPreview = false;

        // ── 카메라 ───────────────────────────────────────────────
        private Camera    _captureCamera;
        private const string CAMERA_OBJ_NAME = "_MinimapCaptureCamera";

        // ── 씬 Gizmo ─────────────────────────────────────────────
        private bool _showGizmo = true;

        // ── 해상도 프리셋 ─────────────────────────────────────────
        private static readonly int[] ResolutionPresetsStandard  = { 256, 512, 1024, 2048, 4096 };
        private static readonly int[] ResolutionPresetsHighEnd   = { 8192, 16384 };
        private bool _useCustomResolution = false;
        private Vector2Int _customResolution = new(4096, 4096);

        // ── 스타일 캐시 ──────────────────────────────────────────
        private GUIStyle _headerStyle;
        private GUIStyle _sectionStyle;
        private bool     _stylesInitialized;

        // ── LOD 강제 설정 ─────────────────────────────────────────
        private bool  _forceLOD         = true;   // 캡처 중 최고 LOD 강제
        private float _lodBiasOverride  = 1000f;  // 강제 적용할 lodBias 값

        // ─────────────────────────────────────────────────────────

        /// <summary>미니맵 촬영과 출력 품질을 조정하는 창을 연다.</summary>
        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/월드/미니맵/미니맵 캡처 에디터")]
        public static void ShowWindow()
        {
            var window = GetWindow<MinimapCaptureEditorWindow>("Minimap Capture");
            window.minSize = new Vector2(420f, 600f);
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            InitCaptureCamera();

            // 씬 뷰 카메라 기준으로 초기 중심 자동 설정
            if (SceneView.lastActiveSceneView != null)
            {
                var sv = SceneView.lastActiveSceneView;
                _captureCenter = new Vector3(sv.pivot.x, 0f, sv.pivot.z);
            }
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            DestroyCaptureCamera();
            DestroyPreviewTexture();
        }

        // ── 메인 GUI ─────────────────────────────────────────────

        public void CreateGUI()
        {
            UPlayGround.EditorTools.UPlaygroundEditorUX.BuildLegacyWindow(
                rootVisualElement, "미니맵 캡처",
                "씬 캡처 범위·렌더 설정·출력과 Sprite 자동 연결을 단계별로 구성합니다.",
                "d_SceneViewCamera", DrawLegacyGUI, "up-minimap-capture");
        }

        private void DrawLegacyGUI()
        {
            EnsureStyles();
            DrawTabs();

            EditorGUILayout.Space(4f);

            switch (_currentTab)
            {
                case Tab.Capture:  DrawCaptureTab();  break;
                case Tab.Settings: DrawSettingsTab(); break;
                case Tab.Help:     DrawHelpTab();     break;
            }
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Minimap Capture Editor", _headerStyle);
            DrawSeparator();
        }

        private void DrawTabs()
        {
            EditorGUILayout.BeginHorizontal();
            DrawTabButton("캡처", Tab.Capture);
            DrawTabButton("설정", Tab.Settings);
            DrawTabButton("도움말", Tab.Help);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawTabButton(string label, Tab tab)
        {
            bool isSelected = _currentTab == tab;
            GUI.backgroundColor = isSelected ? new Color(0.4f, 0.6f, 1f) : Color.white;
            if (GUILayout.Button(label, GUILayout.Height(26f)))
                _currentTab = tab;
            GUI.backgroundColor = Color.white;
        }

        // ── 캡처 탭 ──────────────────────────────────────────────

        private void DrawCaptureTab()
        {
            _previewScroll = EditorGUILayout.BeginScrollView(_previewScroll);

            DrawCaptureArea();
            EditorGUILayout.Space(8f);
            DrawCameraSettings();
            EditorGUILayout.Space(8f);
            DrawResolutionSettings();
            EditorGUILayout.Space(8f);
            DrawOutputSettings();
            EditorGUILayout.Space(8f);
            DrawAutoAssignSection();
            EditorGUILayout.Space(12f);
            DrawActionButtons();
            EditorGUILayout.Space(8f);
            DrawPreviewSection();

            EditorGUILayout.EndScrollView();
        }

        private void DrawCaptureArea()
        {
            DrawSectionLabel("캡처 영역");

            EditorGUI.BeginChangeCheck();
            _captureCenter = EditorGUILayout.Vector3Field("월드 중심", _captureCenter);
            if (EditorGUI.EndChangeCheck())
                SceneView.RepaintAll();

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            _captureWorldSize = EditorGUILayout.Vector2Field("캡처 크기 W/H (월드)", _captureWorldSize);
            _captureWorldSize.x = Mathf.Max(1f, _captureWorldSize.x);
            _captureWorldSize.y = Mathf.Max(1f, _captureWorldSize.y);
            if (EditorGUI.EndChangeCheck())
                SceneView.RepaintAll();

            if (GUILayout.Button("씬 뷰 중심", GUILayout.Width(80f)))
            {
                if (SceneView.lastActiveSceneView != null)
                {
                    var sv = SceneView.lastActiveSceneView;
                    _captureCenter = new Vector3(sv.pivot.x, 0f, sv.pivot.z);
                    SceneView.RepaintAll();
                }
            }
            EditorGUILayout.EndHorizontal();

            _showGizmo = EditorGUILayout.Toggle("씬 뷰 Gizmo 표시", _showGizmo);

            EditorGUILayout.HelpBox(
                $"캡처 범위: {_captureWorldSize.x} × {_captureWorldSize.y} 월드 유닛\n" +
                $"중심: ({_captureCenter.x:F1}, {_captureCenter.z:F1})",
                MessageType.None);
        }

        private void DrawCameraSettings()
        {
            DrawSectionLabel("카메라");

            _cameraHeight = EditorGUILayout.FloatField("카메라 높이 (Y)", _cameraHeight);

            EditorGUILayout.BeginHorizontal();
            _transparentBg = EditorGUILayout.Toggle("투명 배경", _transparentBg);
            EditorGUI.BeginDisabledGroup(_transparentBg);
            _clearColor = EditorGUILayout.ColorField("배경색", _clearColor);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            _layerMask  = LayerMaskField("캡처 레이어", _layerMask);
            _cameraNear = EditorGUILayout.FloatField("Near Clip", _cameraNear);
            _cameraFar  = EditorGUILayout.FloatField("Far Clip", _cameraFar);
        }

        private void DrawResolutionSettings()
        {
            DrawSectionLabel("해상도 (BC7 압축을 위해 4px 단위)");
            if (GUILayout.Button("월드 비율에 맞추기 (긴 변 유지)"))
            {
                int longSide = Mathf.Max(_textureWidth, _textureHeight);
                float scale = longSide / Mathf.Max(_captureWorldSize.x, _captureWorldSize.y);
                _customResolution = new Vector2Int(Mathf.RoundToInt(_captureWorldSize.x * scale),
                    Mathf.RoundToInt(_captureWorldSize.y * scale));
                SetTextureSize(_customResolution.x, _customResolution.y);
                _useCustomResolution = true;
            }
            if (_targetConfig != null)
            {
                float visiblePixels = _textureHeight / Mathf.Max(1f, _targetConfig.mapZoom);
                EditorGUILayout.HelpBox($"현재 줌에서 HUD 세로에 사용되는 원본: {visiblePixels:F0}px. " +
                    "실제 화면의 미니맵 픽셀 크기보다 작으면 해상도를 높이세요.", MessageType.Info);
            }

            // ── 표준 해상도 행 ────────────────────────────────────
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("표준");
            foreach (int res in ResolutionPresetsStandard)
            {
                bool selected = !_useCustomResolution && _textureWidth == res && _textureHeight == res;
                GUI.backgroundColor = selected ? new Color(0.4f, 0.8f, 0.4f) : Color.white;
                if (GUILayout.Button(res >= 1024 ? $"{res/1024}K" : $"{res}", GUILayout.Width(46f)))
                {
                    SetTextureSize(res, res);
                    _useCustomResolution = false;
                }
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            // ── 고해상도 행 ───────────────────────────────────────
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("고해상도");
            foreach (int res in ResolutionPresetsHighEnd)
            {
                bool selected = !_useCustomResolution && _textureWidth == res && _textureHeight == res;
                GUI.backgroundColor = selected ? new Color(1f, 0.7f, 0.3f) : Color.white;
                if (GUILayout.Button($"{res/1024}K", GUILayout.Width(46f)))
                {
                    SetTextureSize(res, res);
                    _useCustomResolution = false;
                }
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            // ── 직접 입력 행 ──────────────────────────────────────
            EditorGUILayout.BeginHorizontal();
            _useCustomResolution = EditorGUILayout.Toggle("직접 입력", _useCustomResolution, GUILayout.Width(150f));
            EditorGUI.BeginDisabledGroup(!_useCustomResolution);
            _customResolution = EditorGUILayout.Vector2IntField("W/H", _customResolution);
            if (_useCustomResolution)
            {
                // 2의 거듭제곱으로 스냅
                if (GUILayout.Button("2^n 스냅", GUILayout.Width(60f)))
                    _customResolution = new Vector2Int(
                        NextPowerOfTwo(_customResolution.x),
                        NextPowerOfTwo(_customResolution.y));
                SetTextureSize(_customResolution.x, _customResolution.y);
                _customResolution = new Vector2Int(_textureWidth, _textureHeight);
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            // ── 경고 / 정보 ───────────────────────────────────────
            int maxResolution = Mathf.Max(_textureWidth, _textureHeight);
            // 4x MSAA 색상·깊이, resolve와 CPU/GPU 복사본을 포함한 대략적인 최대 버퍼 크기.
            long memMB = (long)_textureWidth * _textureHeight * 44 / (1024 * 1024);

            if (maxResolution >= 8192)
            {
                EditorGUILayout.HelpBox(
                    $"⚠ {_textureWidth} × {_textureHeight}px 캡처 — 버퍼 약 {memMB} MB + 씬 리소스 필요. GPU에 따라 실패할 수 있습니다.",
                    MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    $"출력 해상도: {_textureWidth} × {_textureHeight} px\n" +
                    $"월드 1유닛 = X {_textureWidth / _captureWorldSize.x:F2} px / Y {_textureHeight / _captureWorldSize.y:F2} px",
                    MessageType.None);
            }
            // ── LOD 강제 ─────────────────────────────────────────
            EditorGUILayout.BeginHorizontal();
            _forceLOD = EditorGUILayout.Toggle(
                new GUIContent("LOD 강제 (권장)",
                    "캡처 중에만 QualitySettings.lodBias를 높여 높은 세부 LOD 사용을 유도.\n" +
                    "캡처 후 자동 복원됩니다."),
                _forceLOD);
            EditorGUI.BeginDisabledGroup(!_forceLOD);
            EditorGUILayout.LabelField("lodBias 값", GUILayout.Width(70f));
            _lodBiasOverride = EditorGUILayout.FloatField(_lodBiasOverride, GUILayout.Width(60f));
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            if (_forceLOD)
            {
                int lodCount = Object.FindObjectsByType<LODGroup>(FindObjectsSortMode.None).Length;
                int terrainCount = Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None).Length;
                EditorGUILayout.HelpBox(
                    $"씬 내 LODGroup {lodCount}개 / Terrain {terrainCount}개 → 캡처 중 최고 품질 강제",
                    MessageType.Info);
            }

            EditorGUILayout.Space(4f);
        }

        private static int NextPowerOfTwo(int v)
        {
            if (v <= 0) return 64;
            v--;
            v |= v >> 1; v |= v >> 2; v |= v >> 4; v |= v >> 8; v |= v >> 16;
            return v + 1;
        }

        private void SetTextureSize(int width, int height)
        {
            // BC7 블록 크기에 맞지 않는 원본은 Unity가 비압축 텍스처로 가져온다.
            _textureWidth  = Mathf.Clamp(Mathf.RoundToInt(width / 4f) * 4, 64, 16384);
            _textureHeight = Mathf.Clamp(Mathf.RoundToInt(height / 4f) * 4, 64, 16384);
        }

        private Vector2Int GetPreviewTextureSize(int maxLongSide)
        {
            int longest = Mathf.Max(_textureWidth, _textureHeight);
            if (longest <= maxLongSide)
                return new Vector2Int(_textureWidth, _textureHeight);

            float scale = (float)maxLongSide / longest;
            return new Vector2Int(
                Mathf.Max(1, Mathf.RoundToInt(_textureWidth * scale)),
                Mathf.Max(1, Mathf.RoundToInt(_textureHeight * scale)));
        }

        private void DrawOutputSettings()
        {
            DrawSectionLabel("저장 경로");

            EditorGUILayout.BeginHorizontal();
            _savePath = EditorGUILayout.TextField("폴더", _savePath);
            if (GUILayout.Button("...", GUILayout.Width(30f)))
            {
                string selected = EditorUtility.OpenFolderPanel("저장 폴더 선택", _savePath, "");
                if (!string.IsNullOrEmpty(selected))
                {
                    // 절대 경로 → Assets 상대 경로로 변환
                    if (selected.StartsWith(Application.dataPath))
                        _savePath = "Assets" + selected.Substring(Application.dataPath.Length);
                    else
                        _savePath = selected;
                }
            }
            EditorGUILayout.EndHorizontal();

            _fileName = EditorGUILayout.TextField("파일명", _fileName);
            EditorGUILayout.LabelField("저장 경로", $"{_savePath}/{_fileName}.png",
                EditorStyles.miniLabel);
        }

        private void DrawAutoAssignSection()
        {
            DrawSectionLabel("자동 할당");

            _autoAssign = EditorGUILayout.Toggle("MinimapIconConfigSO에 자동 할당", _autoAssign);
            if (_autoAssign)
            {
                _targetConfig = (MinimapIconConfigSO)EditorGUILayout.ObjectField(
                    "대상 Config", _targetConfig, typeof(MinimapIconConfigSO), false);

                if (_targetConfig == null)
                    EditorGUILayout.HelpBox("MinimapIconConfigSO를 연결하세요.", MessageType.Warning);
                else if (GUILayout.Button("Config에서 캡처 영역과 저장 위치 불러오기"))
                {
                    _captureCenter.x = _targetConfig.captureCenter.x;
                    _captureCenter.z = _targetConfig.captureCenter.y;
                    Vector2 size = _targetConfig.captureWorldSizeXY;
                    _captureWorldSize = size.x > 0f && size.y > 0f
                        ? size : Vector2.one * Mathf.Max(1f, _targetConfig.captureWorldSize);
                    if (_targetConfig.backgroundSprite != null)
                    {
                        string path = AssetDatabase.GetAssetPath(_targetConfig.backgroundSprite);
                        _savePath = Path.GetDirectoryName(path)?.Replace('\\', '/');
                        _fileName = Path.GetFileNameWithoutExtension(path);
                    }
                    SceneView.RepaintAll();
                }
            }
        }

        private void DrawActionButtons()
        {
            EditorGUILayout.BeginHorizontal();

            // 미리보기 버튼
            GUI.backgroundColor = new Color(0.6f, 0.8f, 1f);
            if (GUILayout.Button("미리보기 렌더", GUILayout.Height(36f)))
                CapturePreview();
            GUI.backgroundColor = Color.white;

            // 저장 버튼
            GUI.backgroundColor = new Color(0.4f, 0.9f, 0.4f);
            if (GUILayout.Button("캡처 & 저장", GUILayout.Height(36f)))
                CaptureAndSave();
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
        }

        private void DrawPreviewSection()
        {
            if (_previewTexture == null) return;

            DrawSectionLabel(EditorUtility.IsPersistent(_previewTexture) ? "저장된 이미지 (실제 임포트 결과)" : "구도 미리보기 (저장 품질과 별개)");

            _showFullPreview = EditorGUILayout.Toggle("원본 1:1 픽셀 확인", _showFullPreview);

            float maxW = position.width - 20f;
            float displayW = _showFullPreview ? _previewTexture.width : Mathf.Min(maxW, 300f);
            float aspect = _previewTexture.height > 0
                ? (float)_previewTexture.width / _previewTexture.height
                : 1f;
            float displayH = displayW / Mathf.Max(aspect, 0.01f);

            Rect rect = GUILayoutUtility.GetRect(displayW, displayH);

            if (_transparentBg)
                DrawCheckerboard(rect);

            EditorGUI.DrawPreviewTexture(rect, _previewTexture, null, ScaleMode.ScaleToFit);

            EditorGUILayout.LabelField(
                $"{_previewTexture.width} × {_previewTexture.height} px",
                EditorStyles.centeredGreyMiniLabel);
        }

        // ── 설정 탭 ──────────────────────────────────────────────

        private void DrawSettingsTab()
        {
            EditorGUILayout.Space(4f);
            DrawSectionLabel("카메라 오브젝트");

            if (_captureCamera == null)
            {
                EditorGUILayout.HelpBox("캡처 카메라가 없습니다. 아래 버튼으로 재생성하세요.", MessageType.Warning);
                if (GUILayout.Button("카메라 재생성"))
                    InitCaptureCamera();
            }
            else
            {
                EditorGUILayout.LabelField("카메라 상태", "준비 완료", EditorStyles.boldLabel);
                if (GUILayout.Button("카메라 오브젝트 선택"))
                    Selection.activeGameObject = _captureCamera.gameObject;
            }

            EditorGUILayout.Space(8f);
            DrawSectionLabel("캡처 파라미터 초기화");
            if (GUILayout.Button("기본값으로 초기화"))
                ResetToDefaults();
        }

        // ── 도움말 탭 ────────────────────────────────────────────

        private void DrawHelpTab()
        {
            EditorGUILayout.Space(4f);

            EditorGUILayout.HelpBox(
                "■ 사용 순서\n" +
                "1. '캡처 영역' 에서 월드 중심과 크기 설정\n" +
                "2. 씬 뷰 Gizmo로 캡처 범위 확인\n" +
                "3. '미리보기 렌더' 로 결과 확인\n" +
                "4. '캡처 & 저장' 클릭 → PNG 저장\n" +
                "5. MinimapIconConfigSO 에 backgroundSprite 자동 할당\n\n" +
                "■ 주의사항\n" +
                "· URP 프로젝트에서는 카메라가 URP Renderer를 사용합니다.\n" +
                "· 투명 배경은 PNG 알파 채널로 저장됩니다.\n" +
                "· 캡처 범위(captureWorldSizeXY)는 MinimapIconConfigSO 에도 저장됩니다.",
                MessageType.Info);

            EditorGUILayout.Space(8f);
            DrawSectionLabel("좌표 매핑");
            EditorGUILayout.HelpBox(
                "저장된 캡처 중심/범위를 MinimapIconConfigSO 가 보유하므로,\n" +
                "UI_HUD_Minimap 은 WorldToMapImagePos() 를 사용해\n" +
                "월드 XZ → 미니맵 픽셀 좌표를 정확하게 변환합니다.",
                MessageType.None);
        }

        // ── 캡처 로직 ────────────────────────────────────────────

        private void CapturePreview()
        {
            DestroyPreviewTexture();
            Vector2Int size = GetPreviewTextureSize(1024);
            _previewTexture = RenderToTexture(size.x, size.y);
            Repaint();
        }

        private void CaptureAndSave()
        {
            string assetPath = $"{_savePath.TrimEnd('/')}/{_fileName}.png";
            string fullPath = Path.GetFullPath(assetPath);
            string assetsRoot = Path.GetFullPath(Application.dataPath) + Path.DirectorySeparatorChar;
            if (!fullPath.StartsWith(assetsRoot, System.StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(_fileName) || _fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                EditorUtility.DisplayDialog("저장 경로 오류", "Assets 내부 폴더와 유효한 파일명을 지정하세요.", "확인");
                return;
            }

            Texture2D texture = null;
            try
            {
                texture = RenderToTexture(_textureWidth, _textureHeight);
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                File.WriteAllBytes(fullPath, texture.EncodeToPNG());
                Sprite sprite = MinimapCaptureUtility.ImportSprite(assetPath,
                    new Vector2Int(texture.width, texture.height), _transparentBg);
                if (_autoAssign && _targetConfig != null) AssignToConfig(assetPath);
                DestroyPreviewTexture();
                // 저장 이후에는 다시 낮은 해상도로 촬영하지 않고 실제 임포트 결과를 보여준다.
                _previewTexture = sprite.texture;
                Repaint();
                EditorGUIUtility.PingObject(sprite);
                EditorUtility.DisplayDialog("완료", $"미니맵 저장 완료: {sprite.texture.width} × {sprite.texture.height}px\n{assetPath}", "확인");
            }
            finally
            {
                if (texture != null) DestroyImmediate(texture);
            }
        }

        private Texture2D RenderToTexture(int width, int height)
        {
            width  = Mathf.Clamp(width, 1, 16384);
            height = Mathf.Clamp(height, 1, 16384);

            if (_captureCamera == null)
            {
                InitCaptureCamera();
                if (_captureCamera == null) return null;
            }

            // 카메라 파라미터 설정
            _captureCamera.transform.position = new Vector3(
                _captureCenter.x, _captureCenter.y + _cameraHeight, _captureCenter.z);
            _captureCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            _captureCamera.orthographic      = true;
            _captureCamera.orthographicSize  = _captureWorldSize.y * 0.5f;
            _captureCamera.aspect            = _captureWorldSize.x / _captureWorldSize.y;
            _captureCamera.nearClipPlane     = _cameraNear;
            _captureCamera.farClipPlane      = _cameraFar;
            _captureCamera.cullingMask       = _layerMask;

            if (_transparentBg)
            {
                _captureCamera.clearFlags       = CameraClearFlags.SolidColor;
                _captureCamera.backgroundColor  = new Color(0f, 0f, 0f, 0f);
            }
            else
            {
                _captureCamera.clearFlags       = CameraClearFlags.SolidColor;
                _captureCamera.backgroundColor  = _clearColor;
            }

            return MinimapCaptureUtility.Render(_captureCamera, new Vector2Int(width, height), _forceLOD, _lodBiasOverride);
        }

        private void AssignToConfig(string assetPath)
        {
            Undo.RecordObject(_targetConfig, "Minimap Config 자동 할당");

            // 좌표 데이터는 스프라이트 로드 성공 여부와 무관하게 항상 저장
            _targetConfig.captureCenter    = new Vector2(_captureCenter.x, _captureCenter.z);
            _targetConfig.captureWorldSize = Mathf.Max(_captureWorldSize.x, _captureWorldSize.y);
            _targetConfig.captureWorldSizeXY = _captureWorldSize;

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite != null)
                _targetConfig.backgroundSprite = sprite;
            else
                Debug.LogWarning($"[MinimapCapture] Sprite 로드 실패 (좌표 데이터는 저장됨): {assetPath}");

            EditorUtility.SetDirty(_targetConfig);
            AssetDatabase.SaveAssets();

            Debug.Log($"[MinimapCapture] MinimapIconConfigSO 할당 완료 → center=({_captureCenter.x:F2}, {_captureCenter.z:F2}), size={_captureWorldSize.x}x{_captureWorldSize.y}");
        }

        // ── 카메라 관리 ──────────────────────────────────────────

        private void InitCaptureCamera()
        {
            var existing = GameObject.Find(CAMERA_OBJ_NAME);
            if (existing != null)
            {
                _captureCamera = existing.GetComponent<Camera>();
                if (_captureCamera != null) return;
                DestroyImmediate(existing);
            }

            var go = new GameObject(CAMERA_OBJ_NAME);
            go.hideFlags        = HideFlags.HideAndDontSave;
            _captureCamera      = go.AddComponent<Camera>();
            _captureCamera.enabled = false;
        }

        private void DestroyCaptureCamera()
        {
            if (_captureCamera != null)
            {
                DestroyImmediate(_captureCamera.gameObject);
                _captureCamera = null;
            }
        }

        // ── 씬 Gizmo ─────────────────────────────────────────────

        private void OnSceneGUI(SceneView sceneView)
        {
            if (!_showGizmo) return;

            float halfW = _captureWorldSize.x * 0.5f;
            Vector3 c  = new Vector3(_captureCenter.x, _captureCenter.y, _captureCenter.z);

            // 캡처 영역 테두리
            Handles.color = new Color(0.2f, 0.9f, 0.2f, 0.9f);
            DrawRect(c, _captureWorldSize);

            // 캡처 높이 시각화 선
            Handles.color = new Color(0.2f, 0.9f, 0.2f, 0.4f);
            Vector3 camPos = c + Vector3.up * _cameraHeight;
            Handles.DrawDottedLine(c, camPos, 4f);
            Handles.DrawWireDisc(camPos, Vector3.up, 3f);

            // 중심 표시
            Handles.color = Color.yellow;
            Handles.DrawWireDisc(c, Vector3.up, 2f);

            // 핸들로 중심 드래그
            EditorGUI.BeginChangeCheck();
            Vector3 newCenter = Handles.PositionHandle(c, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                _captureCenter = new Vector3(newCenter.x, _captureCenter.y, newCenter.z);
                Repaint();
            }

            // 라벨
            Handles.Label(c + Vector3.right * (halfW + 2f),
                $"캡처: {_captureWorldSize.x}×{_captureWorldSize.y}\n해상도: {_textureWidth}×{_textureHeight}px");
        }

        private static void DrawRect(Vector3 center, Vector2 size)
        {
            float halfW = size.x * 0.5f;
            float halfH = size.y * 0.5f;
            Vector3 bl = center + new Vector3(-halfW, 0,  halfH);
            Vector3 br = center + new Vector3( halfW, 0,  halfH);
            Vector3 tr = center + new Vector3( halfW, 0, -halfH);
            Vector3 tl = center + new Vector3(-halfW, 0, -halfH);
            Handles.DrawPolyLine(bl, br, tr, tl, bl);
        }

        // ── 유틸리티 ─────────────────────────────────────────────

        private static LayerMask LayerMaskField(string label, LayerMask mask)
        {
            // LayerMask를 문자열 배열로 변환해 표시
            string[] layerNames = new string[32];
            for (int i = 0; i < 32; i++)
                layerNames[i] = LayerMask.LayerToName(i);

            int flags = 0;
            for (int i = 0; i < 32; i++)
                if ((mask.value & (1 << i)) != 0) flags |= (1 << i);

            flags = EditorGUILayout.MaskField(label, flags,
                System.Array.ConvertAll(layerNames, n => string.IsNullOrEmpty(n) ? $"Layer {layerNames.Length}" : n));
            return flags;
        }

        private static void DrawCheckerboard(Rect rect)
        {
            int checkSize = 12;
            Color light = new Color(0.72f, 0.72f, 0.72f);
            Color dark  = new Color(0.48f, 0.48f, 0.48f);
            for (float y = rect.y; y < rect.yMax; y += checkSize)
            for (float x = rect.x; x < rect.xMax; x += checkSize)
            {
                bool isLight = (((int)((x - rect.x) / checkSize) + (int)((y - rect.y) / checkSize)) % 2) == 0;
                EditorGUI.DrawRect(new Rect(x, y, checkSize, checkSize), isLight ? light : dark);
            }
        }

        private static void DrawSeparator()
        {
            EditorGUILayout.Space(2f);
            Rect r = GUILayoutUtility.GetRect(1f, 1f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(r, new Color(0.3f, 0.3f, 0.3f));
            EditorGUILayout.Space(4f);
        }

        private void DrawSectionLabel(string label)
        {
            EditorGUILayout.LabelField(label, _sectionStyle);
        }

        private void EnsureStyles()
        {
            if (_stylesInitialized) return;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize  = 15,
                alignment = TextAnchor.MiddleCenter,
            };

            _sectionStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 11,
                normal   = { textColor = new Color(0.7f, 0.85f, 1f) },
            };

            _stylesInitialized = true;
        }

        private void DestroyPreviewTexture()
        {
            if (_previewTexture != null)
            {
                if (!EditorUtility.IsPersistent(_previewTexture)) DestroyImmediate(_previewTexture);
                _previewTexture = null;
            }
        }

        private void ResetToDefaults()
        {
            _captureCenter       = Vector3.zero;
            _captureWorldSize    = new Vector2(200f, 200f);
            _cameraHeight        = 150f;
            SetTextureSize(4096, 4096);
            _useCustomResolution = false;
            _customResolution    = new Vector2Int(4096, 4096);
            _layerMask           = ~0;
            _clearColor       = new Color(0.1f, 0.1f, 0.1f, 1f);
            _transparentBg    = false;
            _savePath         = "Assets/10.Datas/UI/Minimap";
            _fileName         = "MinimapBackground";
            SceneView.RepaintAll();
        }
    }
    #endif
}
