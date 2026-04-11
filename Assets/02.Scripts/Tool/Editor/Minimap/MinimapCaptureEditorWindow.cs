#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.UI;

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
    private float   _captureWorldSize  = 200f;   // 캡처할 월드 범위 (한 변 길이)
    private float   _cameraHeight      = 150f;   // 캡처 카메라 높이
    private int     _textureResolution = 1024;   // 출력 해상도
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
    private static readonly int[] ResolutionPresets = { 256, 512, 1024, 2048, 4096 };

    // ── 스타일 캐시 ──────────────────────────────────────────
    private GUIStyle _headerStyle;
    private GUIStyle _sectionStyle;
    private bool     _stylesInitialized;

    // ─────────────────────────────────────────────────────────

    [MenuItem("UPlayGround/Minimap/Minimap Capture Editor")]
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

    private void OnGUI()
    {
        EnsureStyles();

        DrawHeader();
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
        _captureWorldSize = EditorGUILayout.FloatField("캡처 크기 (월드 유닛)", _captureWorldSize);
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
            $"캡처 범위: {_captureWorldSize} × {_captureWorldSize} 월드 유닛\n" +
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
        DrawSectionLabel("해상도");

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("텍스처 해상도");
        foreach (int res in ResolutionPresets)
        {
            bool selected = _textureResolution == res;
            GUI.backgroundColor = selected ? new Color(0.4f, 0.8f, 0.4f) : Color.white;
            if (GUILayout.Button($"{res}", GUILayout.Width(50f)))
                _textureResolution = res;
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.HelpBox(
            $"출력 해상도: {_textureResolution} × {_textureResolution} px\n" +
            $"월드 1유닛 = {_textureResolution / _captureWorldSize:F2} px",
            MessageType.None);
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

        DrawSectionLabel("미리보기");

        _showFullPreview = EditorGUILayout.Toggle("전체 크기 표시", _showFullPreview);

        float maxW = position.width - 20f;
        float displaySize = _showFullPreview ? maxW : Mathf.Min(maxW, 300f);

        Rect rect = GUILayoutUtility.GetRect(displaySize, displaySize);

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
            "· 캡처 범위(captureWorldSize)는 MinimapIconConfigSO 에도 저장됩니다.",
            MessageType.Info);

        EditorGUILayout.Space(8f);
        DrawSectionLabel("좌표 매핑");
        EditorGUILayout.HelpBox(
            "저장된 캡처 중심/범위를 MinimapIconConfigSO 가 보유하므로,\n" +
            "UI_Minimap 은 WorldToMapImagePos() 를 사용해\n" +
            "월드 XZ → 미니맵 픽셀 좌표를 정확하게 변환합니다.",
            MessageType.None);
    }

    // ── 캡처 로직 ────────────────────────────────────────────

    private void CapturePreview()
    {
        DestroyPreviewTexture();
        _previewTexture = RenderToTexture(_textureResolution);
        Repaint();
    }

    private void CaptureAndSave()
    {
        // 1. 렌더
        Texture2D tex = RenderToTexture(_textureResolution);
        if (tex == null)
        {
            EditorUtility.DisplayDialog("오류", "렌더링에 실패했습니다.", "확인");
            return;
        }

        // 2. 폴더 생성
        string fullDir = Path.Combine(Application.dataPath, _savePath.Replace("Assets/", ""));
        if (!Directory.Exists(fullDir))
            Directory.CreateDirectory(fullDir);

        // 3. 저장 (PNG: 투명 배경, JPG: 불투명 배경)
        string assetPath;
        string absPath;
        if (_transparentBg)
        {
            assetPath = $"{_savePath}/{_fileName}.png";
            absPath   = Path.Combine(Application.dataPath, assetPath.Replace("Assets/", ""));
            File.WriteAllBytes(absPath, tex.EncodeToPNG());
        }
        else
        {
            assetPath = $"{_savePath}/{_fileName}.jpg";
            absPath   = Path.Combine(Application.dataPath, assetPath.Replace("Assets/", ""));
            File.WriteAllBytes(absPath, tex.EncodeToJPG(95));
        }

        DestroyImmediate(tex);
        AssetDatabase.Refresh();

        // 4. 텍스처 임포트 설정 (Sprite)
        ConfigureTextureImporter(assetPath);
        AssetDatabase.Refresh();

        // 5. 프리뷰 갱신
        DestroyPreviewTexture();
        _previewTexture = RenderToTexture(Mathf.Min(_textureResolution, 512));
        Repaint();

        // 6. 자동 할당
        if (_autoAssign && _targetConfig != null)
            AssignToConfig(assetPath);

        // 7. 저장된 에셋 핑
        var savedAsset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
        EditorGUIUtility.PingObject(savedAsset);

        EditorUtility.DisplayDialog("완료",
            $"미니맵 캡처 저장 완료!\n{assetPath}", "확인");
    }

    private Texture2D RenderToTexture(int resolution)
    {
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
        _captureCamera.orthographicSize  = _captureWorldSize * 0.5f;
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

        // RenderTexture 생성 및 렌더
        var format = _transparentBg ? RenderTextureFormat.ARGB32 : RenderTextureFormat.Default;
        RenderTexture rt  = RenderTexture.GetTemporary(resolution, resolution, 24, format);
        RenderTexture prev = RenderTexture.active;

        _captureCamera.targetTexture = rt;
        _captureCamera.Render();
        _captureCamera.targetTexture = null;

        // Texture2D 복사
        RenderTexture.active = rt;
        var texFmt = _transparentBg ? TextureFormat.RGBA32 : TextureFormat.RGB24;
        Texture2D result = new Texture2D(resolution, resolution, texFmt, false);
        result.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
        result.Apply();

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);

        return result;
    }

    private void ConfigureTextureImporter(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) return;

        importer.textureType         = TextureImporterType.Sprite;
        importer.spriteImportMode    = SpriteImportMode.Single;
        importer.maxTextureSize      = _textureResolution;
        importer.mipmapEnabled       = false;
        importer.filterMode          = FilterMode.Bilinear;
        importer.alphaIsTransparency = _transparentBg;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);

        importer.SaveAndReimport();
    }

    private void AssignToConfig(string assetPath)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (sprite == null)
        {
            Debug.LogWarning($"[MinimapCapture] Sprite 로드 실패: {assetPath}");
            return;
        }

        Undo.RecordObject(_targetConfig, "Minimap Config 자동 할당");
        _targetConfig.backgroundSprite  = sprite;
        _targetConfig.captureCenter     = new Vector2(_captureCenter.x, _captureCenter.z);
        _targetConfig.captureWorldSize  = _captureWorldSize;
        _targetConfig.displayMode       = MinimapDisplayMode.MapImage;

        EditorUtility.SetDirty(_targetConfig);
        AssetDatabase.SaveAssets();

        Debug.Log($"[MinimapCapture] MinimapIconConfigSO 할당 완료 → {assetPath}");
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

        float half = _captureWorldSize * 0.5f;
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
        Handles.Label(c + Vector3.right * (half + 2f),
            $"캡처: {_captureWorldSize}×{_captureWorldSize}\n해상도: {_textureResolution}px");
    }

    private static void DrawRect(Vector3 center, float size)
    {
        float h = size * 0.5f;
        Vector3 bl = center + new Vector3(-h, 0,  h);
        Vector3 br = center + new Vector3( h, 0,  h);
        Vector3 tr = center + new Vector3( h, 0, -h);
        Vector3 tl = center + new Vector3(-h, 0, -h);
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
            DestroyImmediate(_previewTexture);
            _previewTexture = null;
        }
    }

    private void ResetToDefaults()
    {
        _captureCenter    = Vector3.zero;
        _captureWorldSize = 200f;
        _cameraHeight     = 150f;
        _textureResolution = 1024;
        _layerMask        = ~0;
        _clearColor       = new Color(0.1f, 0.1f, 0.1f, 1f);
        _transparentBg    = false;
        _savePath         = "Assets/10.Datas/UI/Minimap";
        _fileName         = "MinimapBackground";
        SceneView.RepaintAll();
    }
}
#endif
