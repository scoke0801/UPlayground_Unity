using UnityEngine;
using UnityEditor;
using System.IO;

public class ActorScreenshotTool : EditorWindow
{
    private GameObject sourceActor;
    private GameObject previewInstance;
    
    private Camera renderCamera;
    
    // Actor Transform
    private Vector3 actorPosition = Vector3.zero;
    private Vector3 actorRotation = Vector3.zero;
    private Vector3 actorScale = Vector3.one;
    
    // Camera Settings
    private Vector3 cameraPosition = new Vector3(0, 1, -3);
    private Vector3 cameraRotation = new Vector3(10, 0, 0);
    private float cameraFOV = 60f;
    
    // Screenshot Settings
    private int imageWidth = 512;
    private int imageHeight = 512;
    private Color backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    private bool transparentBackground = true; // 투명 배경 옵션
    private string savePath = "Assets/Screenshots";
    
    // Preview
    private Texture2D previewTexture;
    private bool autoUpdate = true;
    
    // Lighting
    private Light previewLight;
    
    // 격리용 레이어
    private const int PREVIEW_LAYER = 31;

    [MenuItem("Tools/Actor Screenshot Tool")]
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

    private void InitializeRenderCamera()
    {
        GameObject camObj = GameObject.Find("_ActorScreenshotCamera");
        if (camObj == null)
        {
            camObj = new GameObject("_ActorScreenshotCamera");
            camObj.hideFlags = HideFlags.HideAndDontSave;
            camObj.layer = PREVIEW_LAYER;
        }
        
        renderCamera = camObj.GetComponent<Camera>();
        if (renderCamera == null)
        {
            renderCamera = camObj.AddComponent<Camera>();
        }
        
        renderCamera.enabled = false;
        renderCamera.fieldOfView = cameraFOV;
        renderCamera.clearFlags = CameraClearFlags.SolidColor;
        renderCamera.backgroundColor = backgroundColor;
        renderCamera.nearClipPlane = 0.01f;
        renderCamera.farClipPlane = 100f;
        renderCamera.cullingMask = 1 << PREVIEW_LAYER;
        
        // 라이트 추가
        GameObject lightObj = GameObject.Find("_ActorScreenshotLight");
        if (lightObj == null)
        {
            lightObj = new GameObject("_ActorScreenshotLight");
            lightObj.hideFlags = HideFlags.HideAndDontSave;
            lightObj.layer = PREVIEW_LAYER;
        }
        
        previewLight = lightObj.GetComponent<Light>();
        if (previewLight == null)
        {
            previewLight = lightObj.AddComponent<Light>();
        }
        
        previewLight.type = LightType.Directional;
        previewLight.intensity = 1f;
        previewLight.transform.eulerAngles = new Vector3(50, -30, 0);
        previewLight.cullingMask = 1 << PREVIEW_LAYER;
    }

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
                UpdatePreview();
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
        {
            ResetTransform();
        }
    }

    private void DrawCameraControls()
    {
        EditorGUILayout.LabelField("Camera Settings", EditorStyles.boldLabel);
        
        EditorGUI.BeginChangeCheck();
        
        cameraPosition = EditorGUILayout.Vector3Field("Camera Position", cameraPosition);
        cameraRotation = EditorGUILayout.Vector3Field("Camera Rotation", cameraRotation);
        cameraFOV = EditorGUILayout.Slider("Field of View", cameraFOV, 10f, 120f);
        
        if (EditorGUI.EndChangeCheck())
        {
            if (autoUpdate) UpdatePreview();
        }
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Front")) SetCameraPreset(new Vector3(0, 1, -3), new Vector3(10, 0, 0));
        if (GUILayout.Button("Side")) SetCameraPreset(new Vector3(3, 1, 0), new Vector3(10, -90, 0));
        if (GUILayout.Button("Top")) SetCameraPreset(new Vector3(0, 5, 0), new Vector3(90, 0, 0));
        EditorGUILayout.EndHorizontal();
    }

    private void DrawScreenshotSettings()
    {
        EditorGUILayout.LabelField("Screenshot Settings", EditorStyles.boldLabel);
        
        EditorGUI.BeginChangeCheck();
        
        imageWidth = EditorGUILayout.IntField("Width", imageWidth);
        imageHeight = EditorGUILayout.IntField("Height", imageHeight);
        
        // 투명 배경 토글
        transparentBackground = EditorGUILayout.Toggle("Transparent Background", transparentBackground);
        
        // 투명 배경이 아닐 때만 배경색 설정
        EditorGUI.BeginDisabledGroup(transparentBackground);
        backgroundColor = EditorGUILayout.ColorField("Background Color", backgroundColor);
        EditorGUI.EndDisabledGroup();
        
        if (EditorGUI.EndChangeCheck())
        {
            imageWidth = Mathf.Clamp(imageWidth, 64, 8192);
            imageHeight = Mathf.Clamp(imageHeight, 64, 8192);
            
            if (renderCamera != null)
            {
                if (transparentBackground)
                {
                    renderCamera.backgroundColor = new Color(0, 0, 0, 0);
                }
                else
                {
                    renderCamera.backgroundColor = backgroundColor;
                }
            }
            
            if (autoUpdate) UpdatePreview();
        }
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("1920x1080")) SetResolution(1920, 1080);
        if (GUILayout.Button("1280x720")) SetResolution(1280, 720);
        if (GUILayout.Button("2048x2048")) SetResolution(2048, 2048);
        EditorGUILayout.EndHorizontal();
        
        savePath = EditorGUILayout.TextField("Save Path", savePath);
        autoUpdate = EditorGUILayout.Toggle("Auto Update", autoUpdate);
    }

    private void DrawPreview()
    {
        EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Update Preview", GUILayout.Height(30)))
        {
            UpdatePreview();
        }
        
        if (previewTexture != null)
        {
            float aspectRatio = (float)imageWidth / imageHeight;
            float previewWidth = position.width - 20;
            float previewHeight = previewWidth / aspectRatio;
            
            previewHeight = Mathf.Min(previewHeight, 600);
            previewWidth = previewHeight * aspectRatio;
            
            Rect previewRect = GUILayoutUtility.GetRect(previewWidth, previewHeight);
            
            // 투명 배경일 때 체크보드 패턴 그리기
            if (transparentBackground)
            {
                DrawCheckerboard(previewRect);
            }
            
            EditorGUI.DrawPreviewTexture(previewRect, previewTexture, null, ScaleMode.ScaleToFit);
        }
    }

    private void DrawCheckerboard(Rect rect)
    {
        // 체크보드 패턴으로 투명 영역 표시
        int checkSize = 10;
        Color lightGray = new Color(0.7f, 0.7f, 0.7f);
        Color darkGray = new Color(0.5f, 0.5f, 0.5f);
        
        for (int y = 0; y < rect.height; y += checkSize)
        {
            for (int x = 0; x < rect.width; x += checkSize)
            {
                bool isLight = ((x / checkSize) + (y / checkSize)) % 2 == 0;
                EditorGUI.DrawRect(new Rect(rect.x + x, rect.y + y, checkSize, checkSize), 
                    isLight ? lightGray : darkGray);
            }
        }
    }

    private void DrawSaveButton()
    {
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Save Screenshot", GUILayout.Height(40)))
        {
            SaveScreenshot();
        }
        GUI.backgroundColor = Color.white;
    }

    private void CreatePreviewInstance()
    {
        DestroyPreviewInstance();
        
        if (sourceActor != null)
        {
            previewInstance = Instantiate(sourceActor);
            previewInstance.hideFlags = HideFlags.HideAndDontSave;
            
            SetLayerRecursively(previewInstance, PREVIEW_LAYER);
        }
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
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
        if (previewInstance != null)
        {
            previewInstance.transform.position = actorPosition;
            previewInstance.transform.eulerAngles = actorRotation;
            previewInstance.transform.localScale = actorScale;
        }
    }

    private void ResetTransform()
    {
        actorPosition = Vector3.zero;
        actorRotation = Vector3.zero;
        actorScale = Vector3.one;
        ApplyActorTransform();
        if (autoUpdate) UpdatePreview();
    }

    private void SetCameraPreset(Vector3 position, Vector3 rotation)
    {
        cameraPosition = position;
        cameraRotation = rotation;
        if (autoUpdate) UpdatePreview();
    }

    private void SetResolution(int width, int height)
    {
        imageWidth = width;
        imageHeight = height;
        if (autoUpdate) UpdatePreview();
    }

    private void UpdatePreview()
    {
        if (previewInstance == null || renderCamera == null) return;
        
        // 기존 인스턴스 삭제하고 새로 생성
        CreatePreviewInstance();
        ApplyActorTransform();
        
        RenderToTexture(imageWidth, imageHeight, ref previewTexture);
        Repaint();
    }

    private void RenderToTexture(int width, int height, ref Texture2D targetTexture)
    {
        // 투명 배경 지원을 위해 ARGB32 사용
        RenderTexture renderTexture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
        RenderTexture previousRT = RenderTexture.active;
        
        // 카메라 설정
        renderCamera.transform.position = cameraPosition;
        renderCamera.transform.eulerAngles = cameraRotation;
        renderCamera.fieldOfView = cameraFOV;
        
        // 투명 배경 설정
        if (transparentBackground)
        {
            renderCamera.backgroundColor = new Color(0, 0, 0, 0);
        }
        else
        {
            renderCamera.backgroundColor = backgroundColor;
        }
        
        renderCamera.targetTexture = renderTexture;
        renderCamera.cullingMask = 1 << PREVIEW_LAYER;
        
        // 렌더링
        renderCamera.Render();
        
        // Texture2D로 복사 (RGBA32로 알파 채널 지원)
        RenderTexture.active = renderTexture;
        
        if (targetTexture == null || targetTexture.width != width || targetTexture.height != height)
        {
            if (targetTexture != null)
            {
                DestroyImmediate(targetTexture);
            }
            // RGBA32로 알파 채널 포함
            targetTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        }
        
        targetTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        targetTexture.Apply();
        
        // 정리
        RenderTexture.active = previousRT;
        renderCamera.targetTexture = null;
        RenderTexture.ReleaseTemporary(renderTexture);
    }

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
        
        // 고해상도로 렌더링
        Texture2D screenshot = null;
        RenderToTexture(imageWidth, imageHeight, ref screenshot);
        
        // 저장
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }
        
        string fileName = $"{sourceActor.name}.png";
        string fullPath = Path.Combine(savePath, fileName);
        
        byte[] bytes = screenshot.EncodeToPNG();
        File.WriteAllBytes(fullPath, bytes);
        
        DestroyImmediate(screenshot);
        
        AssetDatabase.Refresh();
        
        string message = transparentBackground 
            ? $"투명 배경 스크린샷이 저장되었습니다.\n{fullPath}" 
            : $"스크린샷이 저장되었습니다.\n{fullPath}";
        
        EditorUtility.DisplayDialog("Success", message, "OK");
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(fullPath));
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
    }
}