using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.Rendering.Universal;

public class ActorScreenshotTool : EditorWindow
{
    private GameObject sourceActor;
    private GameObject previewInstance;
    private Camera renderCamera;

    // Actor Transform
    private Vector3 actorPosition = Vector3.zero;
    private Vector3 actorRotation = Vector3.zero;
    private Vector3 actorScale = Vector3.one;

    // Orbit Camera
    private float _orbitYaw = 0f;
    private float _orbitPitch = 15f;
    private float _orbitDistance = 3f;
    private Vector3 _orbitTarget = new Vector3(0f, 1f, 0f);
    private float cameraFOV = 60f;

    // Screenshot Settings
    private int imageWidth = 512;
    private int imageHeight = 512;
    private Color backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    private bool transparentBackground = true;
    private string savePath = "Assets/Screenshots";

    // Preview
    private Texture2D previewTexture;
    private bool autoUpdate = true;
    private Rect _previewRect;

    // Lighting
    private Light previewLight;

    // 체커보드 텍스처 캐시
    private Texture2D _checkerboardTex;
    private const int CHECKER_SIZE = 16;

    // 격리용 레이어
    private const int PREVIEW_LAYER = 31;

    [MenuItem("UPlayGround/유틸/액터 스크린샷 도구")]
    public static void ShowWindow()
    {
        GetWindow<ActorScreenshotTool>("Actor Screenshot");
    }

    private void OnEnable()
    {
        InitializeRenderCamera();
    }

    private void OnDisable()
    {
        CleanupResources();
    }

    private void OnGUI()
    {
        HandleOrbitInput();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Actor Screenshot Tool", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        DrawActorSelection();

        if (sourceActor != null)
        {
            EditorGUILayout.Space(5);
            DrawTransformControls();

            EditorGUILayout.Space(10);
            DrawCameraControls();

            EditorGUILayout.Space(10);
            DrawScreenshotSettings();

            EditorGUILayout.Space(10);
            DrawPreview();

            EditorGUILayout.Space(10);
            DrawSaveButton();
        }
    }

    // ── 오빗 마우스 입력 ────────────────────────────────────────────

    private void HandleOrbitInput()
    {
        if (previewTexture == null) return;

        Event e = Event.current;
        if (!_previewRect.Contains(e.mousePosition)) return;

        if (e.type == EventType.MouseDrag && e.button == 0)
        {
            _orbitYaw += e.delta.x * 0.5f;
            _orbitPitch -= e.delta.y * 0.5f;
            _orbitPitch = Mathf.Clamp(_orbitPitch, -89f, 89f);
            if (autoUpdate) UpdatePreview();
            e.Use();
        }
        else if (e.type == EventType.ScrollWheel)
        {
            _orbitDistance += e.delta.y * 0.1f;
            _orbitDistance = Mathf.Clamp(_orbitDistance, 0.3f, 30f);
            if (autoUpdate) UpdatePreview();
            e.Use();
        }
    }

    // ── 초기화 / 정리 ───────────────────────────────────────────────

    private void InitializeRenderCamera()
    {
        var camObj = new GameObject("_ActorScreenshotCamera");
        camObj.hideFlags = HideFlags.HideAndDontSave;
        camObj.layer = PREVIEW_LAYER;

        renderCamera = camObj.AddComponent<Camera>();
        renderCamera.enabled = false;
        renderCamera.fieldOfView = cameraFOV;
        renderCamera.clearFlags = CameraClearFlags.SolidColor;
        renderCamera.backgroundColor = backgroundColor;
        renderCamera.nearClipPlane = 0.01f;
        renderCamera.farClipPlane = 100f;
        renderCamera.cullingMask = 1 << PREVIEW_LAYER;

        // URP 카메라 설정
        var urpData = camObj.AddComponent<UniversalAdditionalCameraData>();
        urpData.renderType = CameraRenderType.Base;
        urpData.renderShadows = false;

        var lightObj = new GameObject("_ActorScreenshotLight");
        lightObj.hideFlags = HideFlags.HideAndDontSave;
        lightObj.layer = PREVIEW_LAYER;

        previewLight = lightObj.AddComponent<Light>();
        previewLight.type = LightType.Directional;
        previewLight.intensity = 1f;
        previewLight.transform.eulerAngles = new Vector3(50, -30, 0);
        previewLight.cullingMask = 1 << PREVIEW_LAYER;
    }

    private void CleanupResources()
    {
        DestroyPreviewInstance();

        if (renderCamera != null)
        {
            DestroyImmediate(renderCamera.gameObject);
            renderCamera = null;
        }
        if (previewLight != null)
        {
            DestroyImmediate(previewLight.gameObject);
            previewLight = null;
        }
        if (previewTexture != null)
        {
            DestroyImmediate(previewTexture);
            previewTexture = null;
        }
        if (_checkerboardTex != null)
        {
            DestroyImmediate(_checkerboardTex);
            _checkerboardTex = null;
        }
    }

    // ── UI 섹션 ─────────────────────────────────────────────────────

    private void DrawActorSelection()
    {
        EditorGUI.BeginChangeCheck();
        sourceActor = (GameObject)EditorGUILayout.ObjectField("Target Actor", sourceActor, typeof(GameObject), true);

        if (EditorGUI.EndChangeCheck())
        {
            if (sourceActor != null)
            {
                CreatePreviewInstance();
                ResetTransform();
                FrameActor();
            }
            else
            {
                DestroyPreviewInstance();
            }
        }
    }

    private void DrawTransformControls()
    {
        EditorGUILayout.LabelField("Actor Transform", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        actorPosition = EditorGUILayout.Vector3Field("Position", actorPosition);
        actorRotation = EditorGUILayout.Vector3Field("Rotation", actorRotation);
        actorScale = EditorGUILayout.Vector3Field("Scale", actorScale);

        if (EditorGUI.EndChangeCheck())
        {
            ApplyActorTransform();
            if (autoUpdate) UpdatePreview();
        }

        if (GUILayout.Button("Reset Transform"))
            ResetTransform();
    }

    private void DrawCameraControls()
    {
        EditorGUILayout.LabelField("Camera Settings", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        _orbitYaw      = EditorGUILayout.Slider("Yaw (수평)", _orbitYaw, -180f, 180f);
        _orbitPitch    = EditorGUILayout.Slider("Pitch (수직)", _orbitPitch, -89f, 89f);
        _orbitDistance = EditorGUILayout.Slider("Distance (거리)", _orbitDistance, 0.3f, 30f);
        _orbitTarget   = EditorGUILayout.Vector3Field("Orbit Target", _orbitTarget);
        cameraFOV      = EditorGUILayout.Slider("Field of View", cameraFOV, 10f, 120f);

        if (EditorGUI.EndChangeCheck())
        {
            if (autoUpdate) UpdatePreview();
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Front")) SetOrbitPreset(0f, 15f);
        if (GUILayout.Button("Side"))  SetOrbitPreset(90f, 15f);
        if (GUILayout.Button("Back"))  SetOrbitPreset(180f, 15f);
        if (GUILayout.Button("Top"))   SetOrbitPreset(0f, 89f);
        if (GUILayout.Button("Frame")) FrameActor();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.HelpBox("프리뷰에서 좌클릭 드래그로 회전 / 스크롤로 줌", MessageType.None);
    }

    private void DrawScreenshotSettings()
    {
        EditorGUILayout.LabelField("Screenshot Settings", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        imageWidth  = EditorGUILayout.IntField("Width", imageWidth);
        imageHeight = EditorGUILayout.IntField("Height", imageHeight);

        transparentBackground = EditorGUILayout.Toggle("Transparent Background", transparentBackground);

        EditorGUI.BeginDisabledGroup(transparentBackground);
        backgroundColor = EditorGUILayout.ColorField("Background Color", backgroundColor);
        EditorGUI.EndDisabledGroup();

        if (EditorGUI.EndChangeCheck())
        {
            imageWidth  = Mathf.Clamp(imageWidth, 64, 8192);
            imageHeight = Mathf.Clamp(imageHeight, 64, 8192);

            if (renderCamera != null)
                renderCamera.backgroundColor = transparentBackground ? new Color(0, 0, 0, 0) : backgroundColor;

            if (autoUpdate) UpdatePreview();
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("512x512"))   SetResolution(512, 512);
        if (GUILayout.Button("1024x1024")) SetResolution(1024, 1024);
        if (GUILayout.Button("1920x1080")) SetResolution(1920, 1080);
        EditorGUILayout.EndHorizontal();

        savePath   = EditorGUILayout.TextField("Save Path", savePath);
        autoUpdate = EditorGUILayout.Toggle("Auto Update", autoUpdate);
    }

    private void DrawPreview()
    {
        EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

        if (GUILayout.Button("Update Preview", GUILayout.Height(30)))
            UpdatePreview();

        if (previewTexture != null)
        {
            float aspect = (float)imageWidth / imageHeight;
            float w = Mathf.Min(position.width - 20f, 600f * aspect);
            float h = w / aspect;

            _previewRect = GUILayoutUtility.GetRect(w, h);
            _previewRect.width = w;

            if (transparentBackground)
                DrawCheckerboard(_previewRect);

            EditorGUI.DrawPreviewTexture(_previewRect, previewTexture, null, ScaleMode.ScaleToFit);
            EditorGUILayout.HelpBox(
                $"Yaw {_orbitYaw:F1}°  Pitch {_orbitPitch:F1}°  Dist {_orbitDistance:F2}",
                MessageType.None);
        }
    }

    private void DrawSaveButton()
    {
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Save Screenshot", GUILayout.Height(40)))
            SaveScreenshot();
        GUI.backgroundColor = Color.white;
    }

    // ── 체커보드 ────────────────────────────────────────────────────

    private void DrawCheckerboard(Rect rect)
    {
        if (_checkerboardTex == null)
            _checkerboardTex = CreateCheckerboardTexture();

        GUI.DrawTextureWithTexCoords(rect, _checkerboardTex,
            new Rect(0, 0, rect.width / CHECKER_SIZE, rect.height / CHECKER_SIZE));
    }

    private Texture2D CreateCheckerboardTexture()
    {
        int size = CHECKER_SIZE * 2;
        var tex = new Texture2D(size, size, TextureFormat.RGB24, false)
        {
            hideFlags  = HideFlags.HideAndDontSave,
            wrapMode   = TextureWrapMode.Repeat,
            filterMode = FilterMode.Point
        };

        Color light = new Color(0.72f, 0.72f, 0.72f);
        Color dark  = new Color(0.48f, 0.48f, 0.48f);

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, ((x / CHECKER_SIZE + y / CHECKER_SIZE) % 2 == 0) ? light : dark);

        tex.Apply();
        return tex;
    }

    // ── 인스턴스 / 트랜스폼 ─────────────────────────────────────────

    private void CreatePreviewInstance()
    {
        DestroyPreviewInstance();

        if (sourceActor == null) return;

        previewInstance = Instantiate(sourceActor);
        previewInstance.hideFlags = HideFlags.HideAndDontSave;
        SetLayerRecursively(previewInstance, PREVIEW_LAYER);
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    private void DestroyPreviewInstance()
    {
        if (previewInstance != null)
        {
            DestroyImmediate(previewInstance);
            previewInstance = null;
        }
    }

    private void ApplyActorTransform()
    {
        if (previewInstance == null) return;
        previewInstance.transform.position    = actorPosition;
        previewInstance.transform.eulerAngles = actorRotation;
        previewInstance.transform.localScale  = actorScale;
    }

    private void ResetTransform()
    {
        actorPosition = Vector3.zero;
        actorRotation = Vector3.zero;
        actorScale    = Vector3.one;
        ApplyActorTransform();
        if (autoUpdate) UpdatePreview();
    }

    // ── 카메라 헬퍼 ─────────────────────────────────────────────────

    private void SetOrbitPreset(float yaw, float pitch)
    {
        _orbitYaw   = yaw;
        _orbitPitch = pitch;
        if (autoUpdate) UpdatePreview();
    }

    private void FrameActor()
    {
        if (previewInstance == null) return;

        var renderers = previewInstance.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds bounds = renderers[0].bounds;
        foreach (var r in renderers)
            bounds.Encapsulate(r.bounds);

        _orbitTarget   = bounds.center;
        _orbitDistance = Mathf.Max(bounds.extents.magnitude * 2.5f, 0.5f);
        if (autoUpdate) UpdatePreview();
    }

    private Vector3 GetOrbitCameraPosition()
    {
        float pitch = _orbitPitch * Mathf.Deg2Rad;
        float yaw   = _orbitYaw   * Mathf.Deg2Rad;
        float cosP  = Mathf.Cos(pitch);
        return _orbitTarget + new Vector3(
            cosP * Mathf.Sin(yaw),
            Mathf.Sin(pitch),
            -cosP * Mathf.Cos(yaw)
        ) * _orbitDistance;
    }

    private void SetResolution(int width, int height)
    {
        imageWidth  = width;
        imageHeight = height;
        if (autoUpdate) UpdatePreview();
    }

    // ── 렌더링 ──────────────────────────────────────────────────────

    private void UpdatePreview()
    {
        if (previewInstance == null || renderCamera == null) return;

        ApplyActorTransform();
        RenderToTexture(imageWidth, imageHeight, ref previewTexture);
        Repaint();
    }

    private void RenderToTexture(int width, int height, ref Texture2D targetTexture)
    {
        RenderTexture rt          = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
        RenderTexture previousRT  = RenderTexture.active;

        renderCamera.transform.position = GetOrbitCameraPosition();
        renderCamera.transform.LookAt(_orbitTarget);
        renderCamera.fieldOfView   = cameraFOV;
        renderCamera.backgroundColor = transparentBackground ? new Color(0, 0, 0, 0) : backgroundColor;
        renderCamera.targetTexture = rt;
        renderCamera.cullingMask   = 1 << PREVIEW_LAYER;

        renderCamera.Render();

        RenderTexture.active = rt;

        if (targetTexture == null || targetTexture.width != width || targetTexture.height != height)
        {
            if (targetTexture != null) DestroyImmediate(targetTexture);
            targetTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        }

        targetTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        targetTexture.Apply();

        RenderTexture.active       = previousRT;
        renderCamera.targetTexture = null;
        RenderTexture.ReleaseTemporary(rt);
    }

    // ── 저장 ────────────────────────────────────────────────────────

    private void SaveScreenshot()
    {
        if (sourceActor == null)
        {
            EditorUtility.DisplayDialog("Error", "Target Actor가 선택되지 않았습니다.", "OK");
            return;
        }
        if (previewInstance == null)
        {
            EditorUtility.DisplayDialog("Error", "Preview Instance가 생성되지 않았습니다.", "OK");
            return;
        }

        Texture2D screenshot = null;
        RenderToTexture(imageWidth, imageHeight, ref screenshot);

        if (!Directory.Exists(savePath))
            Directory.CreateDirectory(savePath);

        // 파일명 충돌 방지: 중복 시 자동 넘버링
        string baseName = sourceActor.name;
        int index = 0;
        string fullPath;
        do
        {
            string suffix = index == 0 ? "" : $"_{index}";
            fullPath = Path.Combine(savePath, $"{baseName}{suffix}.png");
            index++;
        } while (File.Exists(fullPath));

        File.WriteAllBytes(fullPath, screenshot.EncodeToPNG());
        DestroyImmediate(screenshot);

        AssetDatabase.Refresh();

        string label = transparentBackground ? "투명 배경 스크린샷" : "스크린샷";
        EditorUtility.DisplayDialog("Success", $"{label}이 저장되었습니다.\n{fullPath}", "OK");
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(fullPath));
    }
}
